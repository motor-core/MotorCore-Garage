using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class RegistroPage : ContentPage
    {
        // Sin cuentas: solo se puede registrar el taller. Con cuentas: solo el acceso de cliente.
        readonly bool _hayCuentas;
        readonly List<Models.Cliente> _clientes;
        bool _cliente;

        public RegistroPage()
        {
            InitializeComponent();
            Logo.Drawable = new LogotipoDrawable();

            _hayCuentas = Taller.HayCuentas();
            _clientes = Taller.Clientes.Where(c => !Taller.TieneAcceso(c.Id)).ToList();
            ClienteLista.ItemsSource = _clientes.Select(c => c.Nombre).ToList();

            BotonModoMonitor.IsEnabled = !_hayCuentas;
            BotonModoCliente.IsEnabled = _hayCuentas;
            BotonVolver.IsVisible = _hayCuentas;
            Seleccionar(_hayCuentas);
        }

        void OnModoMonitor(object? sender, EventArgs e) => Seleccionar(false);

        void OnModoCliente(object? sender, EventArgs e) => Seleccionar(true);

        void Seleccionar(bool cliente)
        {
            _cliente = cliente;
            Ui.EstiloSeleccion(BotonModoMonitor, !cliente);
            Ui.EstiloSeleccion(BotonModoCliente, cliente);
            CampoTaller.IsVisible = !cliente;
            CampoCliente.IsVisible = cliente;

            Titulo.Text = cliente ? "Crea tu cuenta" : "Registra tu taller";
            Subtitulo.Text = cliente ? "Elige tu nombre y crea tu usuario" : "Primer uso: crea la cuenta de administrador";
            BotonCrear.Text = cliente ? "Crear cuenta y entrar" : "Crear taller y entrar";

            AyudaModo.Text = !cliente
                ? "Solo se registra una vez. Después, los mecánicos los agrega el administrador."
                : _clientes.Count == 0
                    ? "El taller aún no tiene clientes sin cuenta. Pide que te den de alta."
                    : "Tu nombre debe estar en la lista que te dio el taller.";
            Error.IsVisible = false;
        }

        void OnMostrarChanged(object? sender, CheckedChangedEventArgs e)
        {
            Clave.IsPassword = !e.Value;
            Confirmar.IsPassword = !e.Value;
        }

        void OnCrear(object? sender, EventArgs e)
        {
            var taller = NombreTaller.Text?.Trim() ?? "";
            var usuario = UsuarioNuevo.Text?.Trim() ?? "";
            var clave = Clave.Text ?? "";
            var confirmar = Confirmar.Text ?? "";
            var indice = ClienteLista.SelectedIndex;

            var error =
                _cliente && (indice < 0 || indice >= _clientes.Count) ? "Elige tu nombre en la lista"
                : !_cliente && taller.Length == 0 ? "Escribe el nombre del taller"
                : usuario.Length < 3 ? "El usuario debe tener al menos 3 caracteres"
                : !usuario.All(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-') ? "El usuario solo puede tener letras, números, punto, guion o guion bajo"
                : clave.Length < 6 ? "La contraseña debe tener al menos 6 caracteres"
                : clave != confirmar ? "Las contraseñas no coinciden"
                : null;

            if (error is not null)
            {
                Mostrar(error);
                return;
            }

            var fallo = _cliente
                ? Taller.CrearAccesoCliente(_clientes[indice].Id, usuario, clave)
                : Taller.CrearCuentaInicial(taller, usuario, clave);
            if (fallo is not null)
            {
                Mostrar(fallo);
                return;
            }

            // El registro de cliente no inicia sesión por sí solo: entra con la cuenta recién creada.
            if (_cliente)
            {
                var entrada = Taller.Autenticar(usuario, clave, esCliente: true);
                if (entrada is not null)
                {
                    Mostrar(entrada);
                    return;
                }
            }

            Navegacion.IrAlInicio();
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        void Mostrar(string mensaje)
        {
            Error.Text = mensaje;
            Error.IsVisible = true;
        }
    }
}
