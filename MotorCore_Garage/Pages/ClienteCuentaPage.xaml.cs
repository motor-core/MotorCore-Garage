using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class ClienteCuentaPage : ContentPage
    {
        public ClienteCuentaPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var cliente = Taller.ClientePorId(Taller.ClienteActualId);
            NombreDelTaller.Text = Taller.NombreTaller;
            Nombre.Text = cliente?.Nombre ?? "—";
            Telefono.Text = cliente?.Telefono ?? "—";
            Usuario.Text = $"Usuario: {Taller.UsuarioActual}";

            var partes = (cliente?.Nombre ?? "?").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Iniciales.Text = string.Concat(partes.Take(2).Select(p => p[0])).ToUpperInvariant();
        }

        // Mismo mensaje que el botón de WhatsApp de Inicio: abre el chat del taller con el texto de agendar cita.
        async void OnWhatsApp(object? sender, EventArgs e)
        {
            if (!await Vistas.AgendarCitaWhatsApp())
                await DisplayAlertAsync("WhatsApp", "El número del taller aún no está configurado.", "OK");
        }

        async void OnCerrarSesion(object? sender, EventArgs e)
        {
            var confirmar = await DisplayAlertAsync("Cerrar sesión", "¿Seguro que quieres salir?", "Salir", "Cancelar");
            if (!confirmar) return;
            Taller.CerrarSesion();
            Navegacion.IrAlLogin();
        }
    }
}
