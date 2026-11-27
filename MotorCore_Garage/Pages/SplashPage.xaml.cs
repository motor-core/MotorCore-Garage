using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class SplashPage : ContentPage
    {
        bool _iniciado;

        public SplashPage()
        {
            InitializeComponent();
        }

        // Primera vez: registro del taller. Después: inicio de sesión.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_iniciado) return;
            _iniciado = true;

            await Task.Delay(1500);
            Taller.Cargar();
            Page destino = Taller.HayCuentas() ? new LoginPage() : new RegistroPage();
            Application.Current!.Windows[0].Page = new NavigationPage(destino);
        }
    }
}
