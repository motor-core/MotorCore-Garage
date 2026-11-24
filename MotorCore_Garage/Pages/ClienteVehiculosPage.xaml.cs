using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class ClienteVehiculosPage : ContentPage
    {
        public ClienteVehiculosPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Lista.Children.Clear();
            var vehiculos = Taller.VehiculosDeCliente(Taller.ClienteActualId).ToList();
            Vacio.IsVisible = vehiculos.Count == 0;
            foreach (var v in vehiculos)
            {
                var id = v.Id;
                Lista.Children.Add(Vistas.VehiculoCard(v,
                    () => Shell.Current.GoToAsync("mantenimientos", new Dictionary<string, object> { ["VehiculoId"] = id })));
            }
        }

        async void OnRegistrar(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("clientenuevovehiculo");
        }
    }
}
