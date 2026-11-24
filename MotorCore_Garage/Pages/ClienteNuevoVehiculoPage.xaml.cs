using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // El cliente registra su propio vehículo; queda ligado a su cuenta.
    public partial class ClienteNuevoVehiculoPage : ContentPage
    {
        public ClienteNuevoVehiculoPage()
        {
            InitializeComponent();
        }

        async void OnGuardar(object? sender, EventArgs e)
        {
            var placa = Placa.Text?.Trim().ToUpperInvariant() ?? "";
            var marca = Marca.Text?.Trim() ?? "";
            var modelo = Modelo.Text?.Trim() ?? "";

            var error =
                placa.Length == 0 ? "Escribe la placa"
                : Taller.PlacaRegistrada(placa) ? "Esa placa ya está registrada"
                : marca.Length == 0 || modelo.Length == 0 ? "Escribe la marca y el modelo"
                : !int.TryParse(Anio.Text, out var anio) || anio < 1950 || anio > DateTime.Today.Year + 1 ? "Revisa el año del vehículo"
                : !int.TryParse(Km.Text, out var km) || km < 0 ? "Escribe el kilometraje actual"
                : null;

            if (error is not null)
            {
                Mostrar(error);
                return;
            }

            int.TryParse(Anio.Text, out var anioValido);
            int.TryParse(Km.Text, out var kmValido);
            Taller.AgregarVehiculo(new Vehiculo
            {
                ClienteId = Taller.ClienteActualId,
                Placa = placa,
                Marca = marca,
                Modelo = modelo,
                Anio = anioValido,
                KmActual = kmValido
            });
            await Shell.Current.GoToAsync("..");
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        void Mostrar(string mensaje)
        {
            Error.Text = mensaje;
            Error.IsVisible = true;
        }
    }
}
