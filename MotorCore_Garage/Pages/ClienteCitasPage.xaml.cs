using MotorCore_Garage.Models;
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
            Refrescar();
        }

        void Refrescar()
        {
            // Las canceladas no son próximas; aparecen en el historial para que el cliente vea que se canceló.
            var citas = Taller.CitasDeCliente(Taller.ClienteActualId).ToList();
            var proximas = citas.Where(c => c.Estado != "Cancelada" && c.Fecha.Date >= DateTime.Today).OrderBy(c => c.Fecha).ToList();
            var pasadas = citas.Where(c => !proximas.Contains(c)).OrderByDescending(c => c.Fecha).ToList();

            Proximas.Children.Clear();
            if (proximas.Count == 0) Proximas.Children.Add(Vistas.SinDatos("No tienes citas próximas"));
            foreach (var c in proximas) Proximas.Children.Add(Vistas.CitaCard(c, () => OpcionesCita(c)));

            // El historial solo aparece cuando hay citas pasadas o canceladas.
            SeccionPasadas.IsVisible = pasadas.Count > 0;
            Pasadas.Children.Clear();
            foreach (var c in pasadas) Pasadas.Children.Add(Vistas.CitaCard(c));
        }

        // Posponer o cancelar una cita programada que todavía no empieza.
        async void OpcionesCita(Cita cita)
        {
            var opcion = await ModalPage.Opcion(this, "Tu cita", "Posponer", "Cancelar cita");
            if (opcion == "Posponer")
            {
                await Shell.Current.GoToAsync($"clientenuevacita?citaId={cita.Id}");
            }
            else if (opcion == "Cancelar cita")
            {
                var confirmar = await ModalPage.Confirmar(this, "Cancelar cita", $"¿Cancelar la cita de {cita.Servicio} del {cita.Fecha:dd/MM HH:mm}?", "Cancelar cita", "Volver");
                if (!confirmar) return;

                var error = Taller.CancelarCita(cita);
                if (error is not null)
                {
                    await ModalPage.Aviso(this, "No se pudo cancelar", error);
                    return;
                }
                Refrescar();
            }
        }

        async void OnSolicitar(object? sender, EventArgs e) => await Shell.Current.GoToAsync("clientenuevacita");

        async void OnAgendar(object? sender, EventArgs e) => await Vistas.AgendarCitaWhatsApp();
    }
}
