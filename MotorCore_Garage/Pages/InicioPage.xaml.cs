using System.Globalization;
using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class InicioPage : ContentPage
    {
        static readonly CultureInfo Mexico = new("es-MX");

        public InicioPage()
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
            var hoy = DateTime.Today;
            var hora = DateTime.Now.Hour;
            var saludo = hora < 12 ? "Buenos días" : hora < 19 ? "Buenas tardes" : "Buenas noches";
            Encabezado.Text = Taller.NombreTaller;
            Saludo.Text = $"{saludo}, {Taller.UsuarioActual}";
            var fecha = hoy.ToString("dddd d 'de' MMMM", Mexico);
            Fecha.Text = char.ToUpper(fecha[0]) + fecha[1..];

            var alertas = Taller.Alertas();
            CitasHoy.Text = Taller.CitasEn(hoy).ToString();
            EnTaller.Text = Taller.Citas.Count(c => c.Fecha.Date == hoy && (c.Estado == "Recibido" || c.Estado == "En proceso")).ToString();
            Urgentes.Text = alertas.Count(a => a.Componente.Estado == Desgaste.Vencido).ToString();

            var ocupadas = Taller.Citas.Count(c => c.Fecha.Date == hoy && c.Estado == "En proceso");
            Bahias.Text = $"{ocupadas} de {Taller.Bahias}";
            BarraBahias.Progress = Math.Min(1.0, ocupadas / (double)Taller.Bahias);

            // Gráfica del estado de desgaste del parque: cada vehículo cuenta por su componente peor.
            var porVehiculo = new List<Porcion>
            {
                new("Buen estado", Taller.Vehiculos.Count(v => Vistas.Peor(v).Estado == Desgaste.Ok), Ui.Recurso("WearOk")),
                new("Próximo", Taller.Vehiculos.Count(v => Vistas.Peor(v).Estado == Desgaste.Proximo), Ui.Recurso("WearSoon")),
                new("Vencido", Taller.Vehiculos.Count(v => Vistas.Peor(v).Estado == Desgaste.Vencido), Ui.Recurso("WearDue")),
            };
            PastelVehiculos.Drawable = new PastelDrawable(porVehiculo);
            PastelVehiculos.Invalidate();
            LeyendaVehiculos.Children.Clear();
            LeyendaVehiculos.Children.Add(PastelDrawable.Leyenda(porVehiculo));

            // Gráfica de las citas de hoy según su estado en recepción.
            var citasHoy = Taller.Citas.Where(c => c.Fecha.Date == hoy).ToList();
            var porCita = new (string Estado, string Color)[]
            {
                ("Programada", "Gray400"), ("Recibido", "Primary"), ("En proceso", "WearSoon"), ("Listo", "WearOk"), ("Entregado", "InkMuted")
            }.Select(e => new Porcion(e.Estado, citasHoy.Count(c => c.Estado == e.Estado), Ui.Recurso(e.Color))).ToList();
            PastelCitas.Drawable = new PastelDrawable(porCita);
            PastelCitas.Invalidate();
            LeyendaCitas.Children.Clear();
            LeyendaCitas.Children.Add(PastelDrawable.Leyenda(porCita));

            ListaCitas.Children.Clear();
            var citas = Taller.Citas.Where(c => c.Fecha.Date == hoy).OrderBy(c => c.Fecha).ToList();
            if (citas.Count == 0)
                ListaCitas.Children.Add(new Label { Text = "Sin citas para hoy", Style = (Style)Application.Current!.Resources["Pie"] });
            foreach (var c in citas)
                ListaCitas.Children.Add(Vistas.CitaCard(c));

            ListaAlertas.Children.Clear();
            var principales = alertas.Take(3).ToList();
            if (principales.Count == 0)
                ListaAlertas.Children.Add(new Label { Text = Taller.Vehiculos.Count == 0 ? "Registra un cliente y su vehículo para ver alertas" : "Sin alertas de mantenimiento", Style = (Style)Application.Current!.Resources["Pie"] });
            foreach (var a in principales)
                ListaAlertas.Children.Add(Vistas.AlertaCard(a,
                    agendar: () => Shell.Current.GoToAsync("nuevacita"),
                    contactar: async () => await Vistas.Contactar(this, a.Cliente)));
        }

        // Abre la hoja de acciones: el fondo se atenúa y el panel sube desde abajo.
        async void OnFab(object? sender, EventArgs e)
        {
            Acciones.Opacity = 0;
            Acciones.IsVisible = true;
            Hoja.TranslationY = 400;
            await Task.WhenAll(Acciones.FadeTo(1, 180), Hoja.TranslateTo(0, 0, 220, Easing.CubicOut));
        }

        async void OnCerrarAcciones(object? sender, EventArgs e) => await CerrarAcciones();

        // Cada opción lleva en AutomationId la ruta de la pantalla a la que va.
        async void OnAccion(object? sender, TappedEventArgs e)
        {
            var ruta = ((View)sender!).AutomationId;
            await CerrarAcciones();
            await Shell.Current.GoToAsync(ruta);
        }

        async Task CerrarAcciones()
        {
            if (!Acciones.IsVisible) return;
            await Task.WhenAll(Acciones.FadeTo(0, 150), Hoja.TranslateTo(0, 400, 180, Easing.CubicIn));
            Acciones.IsVisible = false;
        }

        // El botón Atrás del teléfono cierra la hoja antes de salir de la pantalla.
        protected override bool OnBackButtonPressed()
        {
            if (!Acciones.IsVisible) return base.OnBackButtonPressed();
            _ = CerrarAcciones();
            return true;
        }
    }
}
