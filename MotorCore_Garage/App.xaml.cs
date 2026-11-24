using MotorCore_Garage.Pages;

namespace MotorCore_Garage
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Rutas globales: las usan tanto la versión de monitoreo como la de cliente.
            Routing.RegisterRoute("nuevacita", typeof(NuevaCitaPage));
            Routing.RegisterRoute("recepcion", typeof(RecepcionPage));
            Routing.RegisterRoute("cliente", typeof(ClienteDetallePage));
            Routing.RegisterRoute("expediente", typeof(ExpedientePage));
            Routing.RegisterRoute("kilometraje", typeof(RegistrarKmPage));
            Routing.RegisterRoute("clientes", typeof(ClientesPage));
            Routing.RegisterRoute("reportes", typeof(ReportesPage));
            Routing.RegisterRoute("nuevocliente", typeof(NuevoClientePage));
            Routing.RegisterRoute("nuevovehiculo", typeof(NuevoVehiculoPage));
            Routing.RegisterRoute("servicio", typeof(RegistrarServicioPage));
            Routing.RegisterRoute("clientenuevovehiculo", typeof(ClienteNuevoVehiculoPage));
            Routing.RegisterRoute("mantenimientos", typeof(ClienteMantenimientosPage));
            Routing.RegisterRoute("nuevomantenimiento", typeof(ClienteNuevoMantenimientoPage));
        }

        // Inicia con splash y login fuera de Shell; al entrar se cambia a la navegación con pestañas.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new SplashPage()));
        }
    }
}
