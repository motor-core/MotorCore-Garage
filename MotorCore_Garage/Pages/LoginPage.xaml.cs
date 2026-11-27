using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class LoginPage : ContentPage
    {
        // Tipo de acceso elegido al entrar: false = monitoreo del taller, true = cliente.
        bool _cliente;

        public LoginPage()
        {
            InitializeComponent();
            Ui.ConectarVerClave(Contrasena, VerContrasena);
            Ui.ConectarVerClave(ClaveNueva, VerClaveNueva);
            Ui.ConectarVerClave(ConfirmarNueva, VerConfirmarNueva);
            Seleccionar(false);
            VerEntrar();
        }

        // Selector Entrar / Registrarse.
        void OnVerEntrar(object? sender, EventArgs e) => VerEntrar();

        void OnVerRegistro(object? sender, EventArgs e) => VerRegistro();

        void VerEntrar()
        {
            PanelEntrar.IsVisible = true;
            PanelOpcionesEntrar.IsVisible = true;
            PanelRegistro.IsVisible = false;
            EstiloOpcion(OpcionEntrar, true);
            EstiloOpcion(OpcionRegistrar, false);
            Error.IsVisible = false;
        }

        void VerRegistro()
        {
            PanelEntrar.IsVisible = false;
            PanelRegistro.IsVisible = true;
            PanelOpcionesEntrar.IsVisible = false;
            EstiloOpcion(OpcionEntrar, false);
            EstiloOpcion(OpcionRegistrar, true);
            Error.IsVisible = false;
        }

        void OnModoMonitor(object? sender, EventArgs e) => Seleccionar(false);

        void OnModoCliente(object? sender, EventArgs e) => Seleccionar(true);

        // Interruptor Monitor / Cliente: la opción activa se rellena con el color principal.
        static void EstiloOpcion(Border opcion, bool activa)
        {
            var etiqueta = (Label)opcion.Content!;
            var recursos = Application.Current!.Resources;

            opcion.BackgroundColor = activa ? (Color)recursos["Primary"] : Colors.Transparent;
            etiqueta.TextColor = (Color)recursos[activa ? "White" : "Gray400"];
            etiqueta.FontAttributes = activa ? FontAttributes.Bold : FontAttributes.None;
        }

        void Seleccionar(bool cliente)
        {
            _cliente = cliente;
            EstiloOpcion(OpcionMonitor, !cliente);
            EstiloOpcion(OpcionCliente, cliente);
            AyudaModo.Text = cliente
                ? "Usa la cuenta que creaste o la que el taller te dio."
                : "Para administradores y mecánicos del taller.";
            Error.IsVisible = false;
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

        // Cliente nuevo: crea el cliente y su cuenta, y entra directo como cliente.
        void OnRegistrar(object? sender, EventArgs e)
        {
            var usuario = UsuarioNuevo.Text?.Trim() ?? "";
            var clave = ClaveNueva.Text ?? "";

            if (clave != (ConfirmarNueva.Text ?? ""))
            {
                Mostrar("Las contraseñas no coinciden");
                return;
            }

            var error = Taller.CrearClienteConAcceso(NombreCliente.Text ?? "", Telefono.Text?.Trim() ?? "", usuario, clave);
            if (error is not null)
            {
                Mostrar(error);
                return;
            }

            Taller.Autenticar(usuario, clave, esCliente: true);
            Navegacion.IrAlInicio();
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
