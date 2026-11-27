using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    [QueryProperty(nameof(ClienteId), "ClienteId")]
    public partial class NuevoVehiculoPage : ContentPage
    {
        List<Cliente> _clientes = new();

        public NuevoVehiculoPage()
        {
            InitializeComponent();
            Anio.Text = DateTime.Today.Year.ToString();
            Recargar();
        }

        void Recargar()
        {
            _clientes = Taller.Clientes.ToList();
            ClientePicker.ItemsSource = _clientes.Select(c => c.Nombre).ToList();
        }

        // Llega desde "Nuevo cliente": preselecciona al propietario.
        public int ClienteId
        {
            set
            {
                Recargar();
                var indice = _clientes.FindIndex(c => c.Id == value);
                if (indice >= 0) ClientePicker.SelectedIndex = indice;
            }
        }

        async void OnGuardar(object? sender, EventArgs e)
        {
            if (ClientePicker.SelectedIndex < 0)
            {
                Mostrar(_clientes.Count == 0 ? "Primero registra un cliente" : "Elige el cliente propietario");
                return;
            }

            var placa = (Placa.Text ?? "").Trim().ToUpperInvariant();
            var marca = (Marca.Text ?? "").Trim();
            var modelo = (Modelo.Text ?? "").Trim();

            if (Taller.ErrorPlaca(placa) is { } errorPlaca)
            {
                Mostrar(errorPlaca);
                return;
            }
            if (Taller.ErrorVin((Vin.Text ?? "").Trim()) is { } errorVin)
            {
                Mostrar(errorVin);
                return;
            }
            if (Taller.PlacaRegistrada(placa))
            {
                Mostrar("Ya existe un vehículo con esa placa");
                return;
            }
            if (marca.Length == 0 || modelo.Length == 0)
            {
                Mostrar("Escribe la marca y el modelo");
                return;
            }
            if (!int.TryParse(Anio.Text, out var anio) || anio < 1950 || anio > DateTime.Today.Year + 1)
            {
                Mostrar("El año no es válido");
                return;
            }
            if (!int.TryParse(Km.Text, out var km) || km < 0)
            {
                Mostrar("Escribe el kilometraje actual en números");
                return;
            }

            Taller.AgregarVehiculo(new Vehiculo
            {
                ClienteId = _clientes[ClientePicker.SelectedIndex].Id,
                Placa = placa,
                Marca = marca,
                Modelo = modelo,
                Anio = anio,
                KmActual = km,
                Vin = (Vin.Text ?? "").Trim().ToUpperInvariant()
            });

            await ModalPage.Aviso(this, "Vehículo registrado", $"{marca} {modelo} · {placa}", "Entendido");
            await Shell.Current.GoToAsync("..");
        }

        void Mostrar(string mensaje)
        {
            Error.Text = mensaje;
            Error.IsVisible = true;
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
