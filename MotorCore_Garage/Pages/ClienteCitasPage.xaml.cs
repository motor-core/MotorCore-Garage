using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class ClienteCitasPage : ContentPage
    {
        public ClienteCitasPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var citas = Taller.CitasDeCliente(Taller.ClienteActualId).ToList();
            var proximas = citas.Where(c => c.Fecha.Date >= DateTime.Today).OrderBy(c => c.Fecha).ToList();
            var pasadas = citas.Where(c => c.Fecha.Date < DateTime.Today).OrderByDescending(c => c.Fecha).ToList();

            Proximas.Children.Clear();
            if (proximas.Count == 0) Proximas.Children.Add(Vistas.SinDatos("No tienes citas próximas"));
            foreach (var c in proximas) Proximas.Children.Add(Vistas.CitaCard(c));

            // El historial solo aparece cuando hay citas pasadas.
            SeccionPasadas.IsVisible = pasadas.Count > 0;
            Pasadas.Children.Clear();
            foreach (var c in pasadas) Pasadas.Children.Add(Vistas.CitaCard(c));
        }

        async void OnAgendar(object? sender, EventArgs e) => await Vistas.AgendarCitaWhatsApp();
    }
}
