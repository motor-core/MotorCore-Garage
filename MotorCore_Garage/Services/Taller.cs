using MotorCore_Garage.Logica;
using MotorCore_Garage.Models;

namespace MotorCore_Garage.Services
{
    // Estado del taller: carga los datos de SQLite en memoria y escribe cada cambio en la base local.
    public static class Taller
    {
        public const int KmPorDia = 50;            // Ritmo promedio para estimar fechas
        public const int UmbralProximo = ReglasMantenimiento.UmbralProximo;     // Km restantes para marcar "próximo"
        public static readonly string[] Horarios = { "08:00", "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00" };

        public static Rol RolActual { get; private set; } = Rol.Administrador;
        public static string UsuarioActual { get; private set; } = "";
        public static int ClienteActualId { get; private set; }
        public static bool EsCliente => RolActual == Rol.Cliente;
        public const string NombrePorDefecto = "Taller Mecánico Sánchez";
        // Número de WhatsApp del taller con código de país y sin "+" (ej. 5215512345678). Vacío = botón deshabilitado.
        // Número de WhatsApp del taller para las citas, con lada de país (52...). Lo edita el administrador.
        public static string WhatsAppEmpresa { get; private set; } = "524624804230";
        public static string NombreTaller { get; private set; } = NombrePorDefecto;

        static int _bahias = 4;
        public static int Bahias
        {
            get => _bahias;
            set { _bahias = value; GuardarConfig("Bahias", value.ToString()); }
        }

        public static List<ComponenteDef> Catalogo { get; } = new();
        public static List<Cliente> Clientes { get; } = new();
        public static List<Vehiculo> Vehiculos { get; } = new();
        public static List<Cita> Citas { get; } = new();
        public static List<RegistroServicio> Servicios { get; } = new();
        public static List<EntradaBitacora> Bitacora { get; } = new();

        // Horas administrativas por semana (las últimas cuatro) frente a la meta de 3 h.
        public static double[] HorasAdministrativas { get; } = { 2.5, 3.5, 4.0, 2.0 };
        public const double MetaHorasSemana = 3.0;

        // ---------- Carga y cuentas ----------

        public static void Cargar()
        {
            var db = Db.Conexion;
            Catalogo.Clear(); Clientes.Clear(); Vehiculos.Clear(); Citas.Clear(); Servicios.Clear(); Bitacora.Clear(); Mantenimientos.Clear();

            Catalogo.AddRange(db.Table<ComponenteDef>().OrderBy(c => c.Id).ToList());
            Clientes.AddRange(db.Table<Cliente>().OrderBy(c => c.Id).ToList());
            Vehiculos.AddRange(db.Table<Vehiculo>().OrderBy(v => v.Id).ToList());

            var ultimos = db.Table<ComponenteUltimo>().ToList();
            foreach (var v in Vehiculos)
                foreach (var u in ultimos.Where(u => u.VehiculoId == v.Id))
                    v.UltimoServicioKm[u.Componente] = u.KmUltimo;

            Mantenimientos.AddRange(db.Table<Mantenimiento>().OrderBy(m => m.Id).ToList());
            Citas.AddRange(db.Table<Cita>().OrderBy(c => c.Fecha).ToList());
            Servicios.AddRange(db.Table<RegistroServicio>().OrderByDescending(s => s.Fecha).ToList());
            Bitacora.AddRange(db.Table<EntradaBitacora>().OrderByDescending(b => b.Id).Take(200).ToList());

            var config = db.Table<Configuracion>().ToList();
            if (int.TryParse(config.FirstOrDefault(c => c.Clave == "Bahias")?.Valor, out var bahias)) _bahias = bahias;
            NombreTaller = config.FirstOrDefault(c => c.Clave == "NombreTaller")?.Valor is { Length: > 0 } nombre ? nombre : NombrePorDefecto;
            if (config.FirstOrDefault(c => c.Clave == "WhatsApp")?.Valor is { Length: > 0 } whats) WhatsAppEmpresa = whats;
        }

        public static bool HayCuentas() => Db.Conexion.Table<Usuario>().Any();

        // true si el registro inicial encontró que el correo del administrador ya tenía cuenta en Firebase.
        public static bool CorreoYaRegistrado { get; private set; }

        // Registro inicial: crea el taller y la cuenta de administrador (local y, si hay conexión, en Firebase),
        // y deja la sesión iniciada.
        public static async Task<string?> CrearCuentaInicial(string usuario, string clave)
        {
            if (HayCuentas()) return "Ya existe una cuenta registrada";
            var db = Db.Conexion;
            db.RunInTransaction(() =>
            {
                GuardarConfig("NombreTaller", NombrePorDefecto);
                db.Insert(new Usuario
                {
                    Username = usuario.ToLowerInvariant(),
                    PasswordHash = Seguridad.Generar(clave),
                    Rol = Rol.Administrador
                });
            });
            NombreTaller = NombrePorDefecto;
            Iniciar(usuario.ToLowerInvariant(), Rol.Administrador);
            ClaveSesion = clave;
            Bitacorar("Taller registrado · cuenta de administrador creada");
            switch (await FirebaseSesion.CrearAdministradorAsync(usuario.ToLowerInvariant(), clave))
            {
                case ResultadoCuenta.Creada:
                    Bitacorar("cuenta de administrador creada en Firebase");
                    break;
                case ResultadoCuenta.YaExiste:
                    CorreoYaRegistrado = true;
                    Bitacorar("el correo de administrador ya tenía cuenta en Firebase");
                    break;
                default:
                    Bitacorar("cuenta de administrador quedó solo en este teléfono (sin conexión a Firebase)");
                    break;
            }
            return null;
        }

        // Contraseña del monitor en esta sesión. Solo vive en memoria: hace falta para volver a entrar como admin
        // después de crear un mecánico en Firebase, y se borra al cerrar sesión.
        public static string? ClaveSesion { get; private set; }

        // Monitoreo: primero Firebase (con el rol de Firestore). Si no hay conexión o la cuenta no está en Firebase,
        // se usa la cuenta guardada en este teléfono.
        public static async Task<string?> AutenticarMonitorAsync(string usuario, string clave)
        {
            if (await FirebaseSesion.EntrarAsync(usuario, clave))
            {
                var rol = await FirebaseSesion.RolAsync();
                if (rol is "admin" or "mecanico")
                {
                    Iniciar(usuario.Trim().ToLowerInvariant(), rol == "admin" ? Rol.Administrador : Rol.Mecanico);
                    ClaveSesion = clave;
                    Bitacorar("inició sesión (Firebase)");
                    return null;
                }
                await FirebaseSesion.SalirAsync();
            }

            var error = Autenticar(usuario, clave, esCliente: false);
            if (error is null)
            {
                ClaveSesion = clave;
                // La cuenta de administrador se creó sin conexión: al entrar con conexión se completa en Firebase.
                if (RolActual == Rol.Administrador && await FirebaseSesion.CrearAdministradorAsync(UsuarioActual, clave) == ResultadoCuenta.Creada)
                    Bitacorar("cuenta de administrador creada en Firebase");
            }
            return error;
        }

        // esCliente indica el tipo que eligió el usuario en la pantalla de entrada; debe coincidir con la cuenta.
        public static string? Autenticar(string usuario, string clave, bool esCliente)
        {
            var nombre = usuario.Trim().ToLowerInvariant();
            var cuenta = Db.Conexion.Table<Usuario>().FirstOrDefault(u => u.Username == nombre);
            if (cuenta is null || !Seguridad.Verificar(clave, cuenta.PasswordHash))
                return "Usuario o contraseña incorrectos";
            if ((cuenta.Rol == Rol.Cliente) != esCliente)
                return esCliente
                    ? "Esta cuenta es de monitoreo. Cambia a «Monitor» para entrar"
                    : "Esta cuenta es de cliente. Cambia a «Cliente» para entrar";
            Iniciar(cuenta.Username, cuenta.Rol, cuenta.ClienteId);
            Bitacorar("inició sesión");
            return null;
        }

        public static void CerrarSesion()
        {
            Bitacorar("cerró sesión");
            UsuarioActual = "";
            ClienteActualId = 0;
            ClaveSesion = null;
            _ = FirebaseSesion.SalirAsync();
        }

        static void Iniciar(string usuario, Rol rol, int clienteId = 0)
        {
            UsuarioActual = usuario;
            RolActual = rol;
            ClienteActualId = clienteId;
        }

        // Cuentas de acceso para clientes: el administrador crea un usuario ligado a un cliente existente.
        public static bool TieneAcceso(int clienteId) =>
            Db.Conexion.Table<Usuario>().Any(u => u.ClienteId == clienteId);

        public static string? CrearAccesoCliente(int clienteId, string usuario, string clave)
        {
            var nombre = usuario.Trim().ToLowerInvariant();
            if (ClientePorId(clienteId) is null) return "Cliente no encontrado";
            if (ErrorUsuario(nombre) is { } errorUsuario) return errorUsuario;
            if (clave.Length < 6) return "La contraseña debe tener al menos 6 caracteres";
            if (TieneAcceso(clienteId)) return "Este cliente ya tiene una cuenta de acceso";
            if (Db.Conexion.Table<Usuario>().Any(u => u.Username == nombre)) return "Ese usuario ya existe";

            Db.Conexion.Insert(new Usuario
            {
                Username = nombre,
                PasswordHash = Seguridad.Generar(clave),
                Rol = Rol.Cliente,
                ClienteId = clienteId
            });
            Bitacorar($"creó la cuenta de acceso «{nombre}» para {ClientePorId(clienteId)!.Nombre}");
            return null;
        }

        // Cuentas del taller (administrador y mecánicos). Las cuentas de cliente se gestionan desde su ficha.
        public static List<Usuario> CuentasDelTaller() =>
            Db.Conexion.Table<Usuario>().ToList()
                .Where(u => u.Rol != Rol.Cliente)
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.Username)
                .ToList();

        // Crea el mecánico en este teléfono y, si hay conexión, también en Firebase.
        // Si Firebase falla, la cuenta queda solo local y se anota en la bitácora.
        public static async Task<string?> CrearCuentaMecanicoAsync(string usuario, string clave)
        {
            var nombre = usuario.Trim().ToLowerInvariant();
            if (ErrorUsuario(nombre) is { } errorUsuario) return errorUsuario;
            if (clave.Length < 6) return "La contraseña debe tener al menos 6 caracteres";
            if (Db.Conexion.Table<Usuario>().Any(u => u.Username == nombre)) return "Ese usuario ya existe";

            Db.Conexion.Insert(new Usuario
            {
                Username = nombre,
                PasswordHash = Seguridad.Generar(clave),
                Rol = Rol.Mecanico
            });
            Bitacorar($"creó la cuenta de mecánico «{nombre}»");

            if (ClaveSesion is null
                || !await FirebaseSesion.CrearMecanicoAsync(nombre, clave, UsuarioActual, ClaveSesion))
                Bitacorar($"«{nombre}» quedó solo en este teléfono (sin conexión a Firebase)");
            return null;
        }

        // Solo se pueden eliminar cuentas de mecánico; el administrador no puede borrarse a sí mismo.
        public static string? EliminarCuentaMecanico(Usuario cuenta)
        {
            if (cuenta.Rol != Rol.Mecanico) return "Solo se pueden eliminar cuentas de mecánico";
            Db.Conexion.Delete<Usuario>(cuenta.Id);
            Bitacorar($"eliminó la cuenta de mecánico «{cuenta.Username}»");
            return null;
        }

        // Cambia el usuario y, si se escribe, la contraseña de un mecánico. Una clave vacía conserva la actual.
        public static string? EditarCuentaMecanico(Usuario cuenta, string usuario, string? clave)
        {
            if (RolActual != Rol.Administrador) return "Solo el administrador puede editar cuentas";
            if (cuenta.Rol != Rol.Mecanico) return "Solo se pueden editar cuentas de mecánico";

            var nombre = usuario.Trim().ToLowerInvariant();
            if (ErrorUsuario(nombre) is { } errorUsuario) return errorUsuario;
            if (!string.IsNullOrEmpty(clave) && clave.Length < 6) return "La contraseña debe tener al menos 6 caracteres";
            if (Db.Conexion.Table<Usuario>().Any(u => u.Username == nombre && u.Id != cuenta.Id)) return "Ese usuario ya existe";

            var anterior = cuenta.Username;
            cuenta.Username = nombre;
            if (!string.IsNullOrEmpty(clave)) cuenta.PasswordHash = Seguridad.Generar(clave);
            Db.Conexion.Update(cuenta);
            Bitacorar(anterior == nombre
                ? $"editó la cuenta de mecánico «{nombre}»"
                : $"cambió el usuario de mecánico «{anterior}» a «{nombre}»");
            return null;
        }

        // Solo el administrador cambia el número. Acepta 10 dígitos (se agrega 52) o el número completo con 52.
        public static string? GuardarWhatsApp(string entrada)
        {
            if (RolActual != Rol.Administrador) return "Solo el administrador puede cambiar el número";

            var digitos = new string(entrada.Where(char.IsDigit).ToArray());
            var numero = digitos.Length == 10 ? "52" + digitos : digitos;
            if (numero.Length != 12 || !numero.StartsWith("52")) return "Escribe un número de 10 dígitos de México";

            GuardarConfig("WhatsApp", numero);
            WhatsAppEmpresa = numero;
            Bitacorar($"cambió el número de WhatsApp de citas a {numero}");
            return null;
        }

        // Validaciones compartidas de los formularios. Devuelven el mensaje de error, o null si el dato es válido.
        public static string? ErrorUsuario(string usuario)
        {
            var n = usuario.Trim().ToLowerInvariant();
            return n.Length < 3 ? "El usuario debe tener al menos 3 caracteres"
                : n.Length > 50 ? "El usuario no puede tener más de 50 caracteres"
                : !n.All(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-') ? "El usuario solo puede tener letras, números, punto, guion o guion bajo"
                : null;
        }

        public static string? ErrorPlaca(string placa)
        {
            var p = placa.Trim().ToUpperInvariant();
            return p.Length < 5 ? "La placa debe tener al menos 5 caracteres"
                : p.Length > 15 ? "La placa no puede tener más de 15 caracteres"
                : !p.All(ch => char.IsLetterOrDigit(ch) || ch == '-') ? "La placa solo puede tener letras, números y guiones"
                : null;
        }

        // El VIN es opcional; si se escribe, son 17 caracteres alfanuméricos sin I, O ni Q.
        public static string? ErrorVin(string vin)
        {
            var v = vin.Trim().ToUpperInvariant();
            if (v.Length == 0) return null;
            return v.Length != 17 ? "El VIN debe tener 17 caracteres"
                : !v.All(char.IsLetterOrDigit) || v.Any(ch => ch is 'I' or 'O' or 'Q') ? "El VIN solo puede tener letras y números, sin I, O ni Q"
                : null;
        }

        // Registro del cliente desde la pantalla de entrada: crea el cliente y su cuenta de acceso.
        // Todo se valida antes de guardar, para no dejar un cliente sin usuario.
        public static string? CrearClienteConAcceso(string nombre, string telefono, string usuario, string clave)
        {
            var nombreLimpio = nombre.Trim();
            var nombreUsuario = usuario.Trim().ToLowerInvariant();
            var error =
                nombreLimpio.Length == 0 ? "Escribe tu nombre"
                : telefono.Length != 10 || !telefono.All(char.IsDigit) ? "El teléfono debe tener 10 dígitos"
                : ErrorUsuario(nombreUsuario) is { } errorUsuario ? errorUsuario
                : clave.Length < 6 ? "La contraseña debe tener al menos 6 caracteres"
                : Db.Conexion.Table<Usuario>().Any(u => u.Username == nombreUsuario) ? "Ese usuario ya existe"
                : null;
            if (error is not null) return error;

            var cliente = new Cliente { Nombre = nombreLimpio, Telefono = telefono };
            AgregarCliente(cliente);
            return CrearAccesoCliente(cliente.Id, nombreUsuario, clave);
        }

        // ---------- Mantenimientos del cliente ----------

        public static List<Mantenimiento> Mantenimientos { get; } = new();

        public static IEnumerable<Mantenimiento> MantenimientosDeVehiculo(int vehiculoId) =>
            Mantenimientos.Where(m => m.VehiculoId == vehiculoId);

        // Vence por kilómetros (si tiene intervalo en km) o por tiempo (si tiene meses). Lo que llegue primero manda.
        public static EstadoMantenimiento EstadoDe(Mantenimiento m, Vehiculo v) =>
            ReglasMantenimiento.EstadoDe(m, v, DateTime.Today);

        // Pendientes (próximos o vencidos) de todos los vehículos de un cliente.
        public static List<EstadoMantenimiento> PendientesDeCliente(int clienteId) =>
            Vehiculos.Where(v => v.ClienteId == clienteId)
                .SelectMany(v => MantenimientosDeVehiculo(v.Id).Select(m => EstadoDe(m, v)))
                .Where(e => e.Estado != Desgaste.Ok)
                .OrderByDescending(e => e.Estado)
                .ToList();

        public static void AgregarMantenimiento(Mantenimiento m)
        {
            Db.Conexion.Insert(m);
            Mantenimientos.Add(m);
            Bitacorar($"programó «{m.Nombre}» para el vehículo {VehiculoPorId(m.VehiculoId)?.Placa}");
        }

        // Registra que el mantenimiento se hizo: reinicia el periodo desde ese kilometraje y fecha.
        public static void MarcarRealizado(Mantenimiento m, int km)
        {
            var vehiculo = VehiculoPorId(m.VehiculoId)!;
            m.KmUltimo = Math.Max(km, vehiculo.KmActual);
            m.FechaUltima = DateTime.Today;
            m.UltimoAviso = DateTime.MinValue;
            if (km > vehiculo.KmActual)
            {
                vehiculo.KmActual = km;
                Db.Conexion.Update(vehiculo);
            }
            Db.Conexion.Update(m);
            Bitacorar($"registró «{m.Nombre}» realizado en {vehiculo.Placa} ({km:N0} km)");
        }

        public static void EliminarMantenimiento(Mantenimiento m)
        {
            Db.Conexion.Delete(m);
            Mantenimientos.Remove(m);
            Bitacorar($"eliminó «{m.Nombre}»");
        }

        // ---------- Escritura (cada método guarda en SQLite y actualiza la memoria) ----------

        public static void Bitacorar(string texto)
        {
            var entrada = new EntradaBitacora { Texto = $"{UsuarioActual} {texto}".Trim(), Fecha = DateTime.Now };
            Db.Conexion.Insert(entrada);
            Bitacora.Insert(0, entrada);
        }

        static void GuardarConfig(string clave, string valor) =>
            Db.Conexion.InsertOrReplace(new Configuracion { Clave = clave, Valor = valor });

        public static void GuardarIntervalo(ComponenteDef componente)
        {
            Db.Conexion.Update(componente);
            Bitacorar($"cambió el intervalo de {componente.Nombre} a {componente.IntervaloKm:N0} km");
        }

        public static void AgregarCliente(Cliente cliente)
        {
            Db.Conexion.Insert(cliente);
            Clientes.Add(cliente);
            Bitacorar($"registró al cliente {cliente.Nombre}");
        }

        // Al registrar un vehículo se asume que está al día: cada componente parte de su kilometraje actual.
        public static void AgregarVehiculo(Vehiculo vehiculo)
        {
            var db = Db.Conexion;
            db.RunInTransaction(() =>
            {
                db.Insert(vehiculo);
                foreach (var c in Catalogo) GuardarUltimo(vehiculo, c.Nombre, vehiculo.KmActual);
            });
            Vehiculos.Add(vehiculo);
            Bitacorar($"registró el vehículo {vehiculo.Placa}");
        }

        public static void GuardarVehiculo(Vehiculo vehiculo) => Db.Conexion.Update(vehiculo);

        // Valida y guarda un nuevo kilometraje. Devuelve el mensaje de error o null si se guardó.
        public static string? RegistrarKilometraje(Vehiculo v, int km)
        {
            if (km < 0) return "El kilometraje no puede ser negativo";
            if (km < v.KmActual) return $"El kilometraje no puede ser menor al último registrado ({v.KmActual:N0} km)";
            v.KmActual = km;
            Db.Conexion.Update(v);
            Bitacorar($"registró {km:N0} km en {v.Placa}");
            return null;
        }

        // Guarda un servicio realizado y reinicia el contador de los componentes atendidos.
        public static string? RegistrarServicio(RegistroServicio servicio, IReadOnlyCollection<ComponenteDef> componentes)
        {
            var v = VehiculoPorId(servicio.VehiculoId);
            if (v is null) return "Vehículo no encontrado";
            if (componentes.Count == 0) return "Elige al menos un componente atendido";
            if (servicio.Km < v.KmActual) return $"El kilometraje no puede ser menor al último registrado ({v.KmActual:N0} km)";

            var db = Db.Conexion;
            servicio.Componentes = string.Join(", ", componentes.Select(c => c.Nombre));
            db.RunInTransaction(() =>
            {
                db.Insert(servicio);
                v.KmActual = servicio.Km;
                db.Update(v);
                foreach (var c in componentes) GuardarUltimo(v, c.Nombre, servicio.Km);
            });
            Servicios.Insert(0, servicio);
            Bitacorar($"registró servicio «{servicio.Servicio}» en {v.Placa}");
            return null;
        }

        static void GuardarUltimo(Vehiculo v, string componente, int km)
        {
            var db = Db.Conexion;
            var fila = db.Table<ComponenteUltimo>().FirstOrDefault(u => u.VehiculoId == v.Id && u.Componente == componente);
            if (fila is null) db.Insert(new ComponenteUltimo { VehiculoId = v.Id, Componente = componente, KmUltimo = km });
            else { fila.KmUltimo = km; db.Update(fila); }
            v.UltimoServicioKm[componente] = km;
        }

        // Valida el turno de una cita: día y hora no pasados, hora de la lista y bahía libre.
        // excluirId es la cita que se está moviendo, para que no cuente contra sí misma.
        public static string? ErrorCita(DateTime inicio, int? excluirId = null)
        {
            if (inicio.Date < DateTime.Today) return "No puedes agendar en un día que ya pasó";
            if (inicio <= DateTime.Now) return "Ese horario ya pasó. Elige otro turno";
            if (!Horarios.Any(h => int.Parse(h[..2]) == inicio.Hour)) return "Elige un turno de la lista";
            if (HorarioLleno(inicio, $"{inicio.Hour:00}:00", excluirId)) return "Ese turno ya está lleno";
            return null;
        }

        // Guarda una cita nueva. Un cliente solo puede agendar para sus propios vehículos, y su cita queda
        // como solicitud hasta que el taller la confirme. El turno ya cuenta como ocupado.
        public static string? AgregarCita(Cita cita)
        {
            if (EsCliente && cita.ClienteId != ClienteActualId) return "Esa cita no es tuya";
            if (ErrorCita(cita.Fecha) is { } error) return error;
            if (EsCliente) cita.Estado = "Solicitada";
            Db.Conexion.Insert(cita);
            Citas.Add(cita);
            return null;
        }

        // El taller confirma una solicitud del cliente: pasa a Programada.
        public static string? ConfirmarCita(Cita cita)
        {
            if (cita.Estado != "Solicitada" || cita.Fecha <= DateTime.Now) return "Esta cita ya no se puede confirmar";
            cita.Estado = "Programada";
            GuardarCita(cita);
            Bitacorar($"confirmó la cita de {cita.Servicio} del {cita.Fecha:dd/MM HH:mm}");
            return null;
        }

        // Una cita se puede cambiar o cancelar mientras esté pendiente o programada y todavía no empiece.
        // Un cliente solo puede tocar sus propias citas.
        static string? ErrorModificarCita(Cita cita)
        {
            if (EsCliente && cita.ClienteId != ClienteActualId) return "Esa cita no es tuya";
            if (cita.Estado is not ("Programada" or "Solicitada") || cita.Fecha <= DateTime.Now) return "Esta cita ya no se puede modificar";
            return null;
        }

        public static string? CancelarCita(Cita cita)
        {
            if (ErrorModificarCita(cita) is { } error) return error;
            cita.Estado = "Cancelada";
            GuardarCita(cita);
            Bitacorar($"canceló la cita de {cita.Servicio} del {cita.Fecha:dd/MM HH:mm}");
            return null;
        }

        public static string? PosponerCita(Cita cita, DateTime nueva)
        {
            if (ErrorModificarCita(cita) is { } error) return error;
            if (ErrorCita(nueva, cita.Id) is { } errorTurno) return errorTurno;
            var anterior = cita.Fecha;
            cita.Fecha = nueva;
            GuardarCita(cita);
            Bitacorar($"pospuso la cita de {cita.Servicio} del {anterior:dd/MM HH:mm} al {nueva:dd/MM HH:mm}");
            return null;
        }

        public static void GuardarCita(Cita cita) => Db.Conexion.Update(cita);

        // ---------- Consultas ----------

        public static ComponenteDef? Componente(string nombre) => Catalogo.FirstOrDefault(c => c.Nombre == nombre);

        public static Cliente? ClientePorId(int id) => Clientes.FirstOrDefault(c => c.Id == id);

        public static Vehiculo? VehiculoPorId(int id) => Vehiculos.FirstOrDefault(v => v.Id == id);

        public static bool PlacaRegistrada(string placa) =>
            Vehiculos.Any(v => v.Placa.Equals(placa, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<Vehiculo> VehiculosDeCliente(int clienteId) => Vehiculos.Where(v => v.ClienteId == clienteId);

        public static IEnumerable<Cita> CitasDeCliente(int clienteId) => Citas.Where(c => c.ClienteId == clienteId);

        // Un cliente solo puede ver sus propios vehículos.
        public static bool VehiculoEsDelUsuario(Vehiculo v) => !EsCliente || v.ClienteId == ClienteActualId;

        public static IEnumerable<EstadoComponente> Estados(Vehiculo v)
        {
            foreach (var c in Catalogo)
            {
                var ultimo = v.UltimoServicioKm.TryGetValue(c.Nombre, out var k) ? k : v.KmActual;
                var usado = Math.Max(0, v.KmActual - ultimo);
                var restante = c.IntervaloKm - usado;
                var porcentaje = (int)Math.Clamp(usado * 100.0 / c.IntervaloKm, 0, 100);
                var estado = restante <= 0 ? Desgaste.Vencido : restante < UmbralProximo ? Desgaste.Proximo : Desgaste.Ok;
                yield return new EstadoComponente(c.Nombre, 100 - porcentaje, restante, estado);
            }
        }

        public static List<Alerta> Alertas()
        {
            var lista = new List<Alerta>();
            foreach (var v in Vehiculos)
            {
                var cliente = ClientePorId(v.ClienteId);
                if (cliente is null) continue;
                var tieneCita = Citas.Any(c => c.VehiculoId == v.Id && c.Fecha >= DateTime.Today && c.Estado != "Cancelada");
                foreach (var e in Estados(v).Where(e => e.Estado != Desgaste.Ok))
                {
                    var grupo = tieneCita ? GrupoAlerta.Programadas : e.Estado == Desgaste.Vencido ? GrupoAlerta.Vencidas : GrupoAlerta.Proximas;
                    var fecha = DateTime.Today.AddDays(Math.Max(0, e.KmRestantes) / KmPorDia);
                    lista.Add(new Alerta(v, cliente, e, fecha, grupo));
                }
            }
            return lista.OrderBy(a => a.Grupo).ThenBy(a => a.Componente.KmRestantes).ToList();
        }

        // Kilometraje del vehículo en el tiempo: cada servicio y el valor actual.
        public static List<(DateTime Fecha, int Km)> Kilometrajes(Vehiculo v)
        {
            var puntos = Servicios.Where(s => s.VehiculoId == v.Id).Select(s => (s.Fecha, s.Km)).ToList();
            puntos.Add((DateTime.Today, v.KmActual));
            return puntos.OrderBy(p => p.Fecha).ToList();
        }

        public static List<RegistroServicio> Historial(Vehiculo v) =>
            Servicios.Where(s => s.VehiculoId == v.Id).OrderByDescending(s => s.Fecha).ToList();

        // Las citas canceladas no ocupan bahía ni cuentan en la ocupación del día.
        public static int CitasEn(DateTime dia) => Citas.Count(c => c.Fecha.Date == dia.Date && c.Estado != "Cancelada");

        public static int CitasEnHorario(DateTime fecha, string hora, int? excluirId = null)
        {
            var h = int.Parse(hora.Substring(0, 2));
            return Citas.Count(c => c.Fecha.Date == fecha.Date && c.Fecha.Hour == h && c.Estado != "Cancelada" && c.Id != excluirId);
        }

        public static bool HorarioLleno(DateTime fecha, string hora, int? excluirId = null) => CitasEnHorario(fecha, hora, excluirId) >= Bahias;

        // Ocupación del día como fracción de todas las bahías y horarios disponibles.
        public static double Saturacion(DateTime dia) => Math.Min(1.0, CitasEn(dia) / (double)(Horarios.Length * Bahias));
    }
}
