using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // Plantillas con los periodos habituales: al elegir una, se llenan sus km y meses (el cliente puede cambiarlos).
    [QueryProperty(nameof(VehiculoId), "VehiculoId")]
    public partial class ClienteNuevoMantenimientoPage : ContentPage
    {
        static readonly (string Nombre, int Km, int Meses)[] Plantillas =
        {
            ("Cambio de aceite", 5000, 6),
            ("Filtro de aire", 15000, 12),
            ("Frenos", 30000, 24),
            ("Llantas (rotación)", 10000, 6),
            ("Batería", 40000, 36),
            ("Alineación y balanceo", 10000, 6),
            ("Otro", 0, 0)
        };

        int _vehiculoId;

        public ClienteNuevoMantenimientoPage()
        {
            InitializeComponent();
            Tipo.ItemsSource = Plantillas.Select(p => p.Nombre).ToList();
            Fecha.MaximumDate = DateTime.Today;
            Fecha.Date = DateTime.Today;
        }

        public int VehiculoId
        {
            set => _vehiculoId = value;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var vehiculo = Taller.VehiculoPorId(_vehiculoId);
            if (vehiculo is not null && string.IsNullOrWhiteSpace(KmUltimo.Text))
                KmUltimo.Text = vehiculo.KmActual.ToString();
        }

        void OnTipo(object? sender, EventArgs e)
        {
            if (Tipo.SelectedIndex < 0) return;
            var plantilla = Plantillas[Tipo.SelectedIndex];
            Nombre.Text = plantilla.Nombre == "Otro" ? "" : plantilla.Nombre;
            KmCada.Text = plantilla.Km.ToString();
            MesesCada.Text = plantilla.Meses.ToString();
        }

        async void OnGuardar(object? sender, EventArgs e)
        {
            var vehiculo = Taller.VehiculoPorId(_vehiculoId);
            var nombre = Nombre.Text?.Trim() ?? "";
            var kmCada = int.TryParse(KmCada.Text, out var km) ? km : 0;
            var mesesCada = int.TryParse(MesesCada.Text, out var meses) ? meses : 0;
            var kmUltimo = int.TryParse(KmUltimo.Text, out var kmU) ? kmU : -1;

            var error =
                vehiculo is null ? "No encontramos el vehículo"
                : nombre.Length == 0 ? "Escribe el nombre del mantenimiento"
                : kmCada < 0 || mesesCada < 0 ? "Los periodos no pueden ser negativos"
                : kmCada == 0 && mesesCada == 0 ? "Indica cada cuántos km o cada cuántos meses"
                : kmUltimo < 0 ? "Escribe el kilometraje de la última vez"
                : kmUltimo > vehiculo.KmActual ? $"No puede ser mayor al kilometraje actual ({vehiculo.KmActual:N0} km)"
                : Fecha.Date > DateTime.Today ? "La fecha no puede ser futura"
                : null;

            if (error is not null)
            {
                Error.Text = error;
                Error.IsVisible = true;
                return;
            }

            Taller.AgregarMantenimiento(new Mantenimiento
            {
                VehiculoId = _vehiculoId,
                Nombre = nombre,
                IntervaloKm = kmCada,
                IntervaloMeses = mesesCada,
                KmUltimo = kmUltimo,
                FechaUltima = Fecha.Date ?? DateTime.Today
            });
            await Shell.Current.GoToAsync("..");
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
