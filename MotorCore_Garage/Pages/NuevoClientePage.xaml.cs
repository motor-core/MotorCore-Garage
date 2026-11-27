using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class NuevoClientePage : ContentPage
    {
        public NuevoClientePage()
        {
            InitializeComponent();
        }

        async void OnGuardar(object? sender, EventArgs e)
        {
            var nombre = Nombre.Text?.Trim() ?? "";
            var telefono = new string((Telefono.Text ?? "").Where(char.IsDigit).ToArray());

            if (nombre.Length < 3)
            {
                Mostrar("Escribe el nombre completo del cliente");
                return;
            }
            if (telefono.Length != 10)
            {
                Mostrar("El teléfono debe tener 10 dígitos");
                return;
            }

            var cliente = new Cliente { Nombre = nombre, Telefono = telefono };
            Taller.AgregarCliente(cliente);

            var registrarVehiculo = await ModalPage.Confirmar(this, "Cliente guardado", $"¿Registrar un vehículo de {nombre} ahora?",
                "Sí, registrar vehículo", "Más tarde");

            await Shell.Current.GoToAsync("..");
            if (registrarVehiculo)
                await Shell.Current.GoToAsync("nuevovehiculo", new Dictionary<string, object> { ["ClienteId"] = cliente.Id });
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
