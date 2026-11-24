using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class LoginPage : ContentPage
    {
        // Tipo de acceso elegido: false = monitoreo del taller, true = cliente.
        bool _cliente;

        public LoginPage()
        {
            InitializeComponent();
            Seleccionar(false);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Encabezado.Text = Taller.NombreTaller;
        }

        void OnModoMonitor(object? sender, EventArgs e) => Seleccionar(false);

        void OnModoCliente(object? sender, EventArgs e) => Seleccionar(true);

        void Seleccionar(bool cliente)
        {
            _cliente = cliente;
            Ui.EstiloSeleccion(BotonModoMonitor, !cliente);
            Ui.EstiloSeleccion(BotonModoCliente, cliente);
            AyudaModo.Text = cliente
                ? "Usa la cuenta que el taller creó para ti, o crea la tuya."
                : "Para administradores y mecánicos del taller.";
            SeccionCuentaCliente.IsVisible = cliente;
            Error.IsVisible = false;
        }

        void OnMostrarChanged(object? sender, CheckedChangedEventArgs e)
        {
            Contrasena.IsPassword = !e.Value;
        }

        async void OnEntrar(object? sender, EventArgs e)
        {
            var usuario = Usuario.Text?.Trim() ?? "";
            var clave = Contrasena.Text ?? "";

            if (usuario.Length == 0 || clave.Length == 0)
            {
                Mostrar("Escribe usuario y contraseña");
                return;
            }

            // Monitoreo entra con Firebase (y cuenta local sin conexión); clientes siguen con su cuenta local.
            var error = _cliente
                ? Taller.Autenticar(usuario, clave, esCliente: true)
                : await Taller.AutenticarMonitorAsync(usuario, clave);
            if (error is not null)
            {
                Mostrar(error);
                return;
            }

            Navegacion.IrAlInicio();
        }

        // Registro de cliente: se abre sobre esta pantalla y regresa aquí si ya hay cuenta.
        async void OnCrearCuenta(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistroPage());
        }

        async void OnAgendarWhatsApp(object? sender, EventArgs e)
        {
            if (!await Vistas.AgendarCitaWhatsApp())
                Mostrar("El número de WhatsApp del taller aún no está configurado");
        }

        void Mostrar(string mensaje)
        {
            Error.Text = mensaje;
            Error.IsVisible = true;
        }
    }
}
