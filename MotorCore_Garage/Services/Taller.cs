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
        public const string NombrePorDefecto = "MotorCode Garage";
        // Número de WhatsApp del taller con código de país y sin "+" (ej. 5215512345678). Vacío = botón deshabilitado.
        public const string WhatsAppEmpresa = "524624804230";
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
        }

        public static bool HayCuentas() => Db.Conexion.Table<Usuario>().Any();

        // Registro inicial: crea el taller y la cuenta de administrador, y deja la sesión iniciada.
        public static string? CrearCuentaInicial(string nombreTaller, string usuario, string clave)
        {
            if (HayCuentas()) return "Ya existe una cuenta registrada";
            var db = Db.Conexion;
            db.RunInTransaction(() =>
            {
                GuardarConfig("NombreTaller", nombreTaller);
                db.Insert(new Usuario
                {
                    Username = usuario.ToLowerInvariant(),
                    PasswordHash = Seguridad.Generar(clave),
                    Rol = Rol.Administrador
                });
            });
            NombreTaller = nombreTaller;
            Iniciar(usuario.ToLowerInvariant(), Rol.Administrador);
            ClaveSesion = clave;
            Bitacorar("Taller registrado · cuenta de administrador creada");
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
            if (error is null) ClaveSesion = clave;
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
            if (nombre.Length < 3) return "El usuario debe tener al menos 3 caracteres";
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
            if (nombre.Length < 3) return "El usuario debe tener al menos 3 caracteres";
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

        public static void AgregarCita(Cita cita)
        {
            Db.Conexion.Insert(cita);
            Citas.Add(cita);
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
                var tieneCita = Citas.Any(c => c.VehiculoId == v.Id && c.Fecha >= DateTime.Today);
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

        public static int CitasEn(DateTime dia) => Citas.Count(c => c.Fecha.Date == dia.Date);

        public static int CitasEnHorario(DateTime fecha, string hora)
        {
            var h = int.Parse(hora.Substring(0, 2));
            return Citas.Count(c => c.Fecha.Date == fecha.Date && c.Fecha.Hour == h);
        }

        public static bool HorarioLleno(DateTime fecha, string hora) => CitasEnHorario(fecha, hora) >= Bahias;

        // Ocupación del día como fracción de todas las bahías y horarios disponibles.
        public static double Saturacion(DateTime dia) => Math.Min(1.0, CitasEn(dia) / (double)(Horarios.Length * Bahias));
    }
}
