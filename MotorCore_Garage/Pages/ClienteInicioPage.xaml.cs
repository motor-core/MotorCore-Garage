using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class ClienteInicioPage : ContentPage
    {
        public ClienteInicioPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var clienteId = Taller.ClienteActualId;
            var cliente = Taller.ClientePorId(clienteId);
            Saludo.Text = cliente is null ? "Hola" : $"Hola, {cliente.Nombre.Split(' ')[0]}";

            var vehiculos = Taller.VehiculosDeCliente(clienteId).ToList();
            var proximas = Taller.CitasDeCliente(clienteId)
                .Where(c => c.Fecha.Date >= DateTime.Today)
                .OrderBy(c => c.Fecha)
                .ToList();

            NumVehiculos.Text = vehiculos.Count.ToString();
            NumCitas.Text = proximas.Count.ToString();
            Resumen.Text = vehiculos.Count switch
            {
                0 => "Aún no tienes vehículos registrados",
                1 => "1 vehículo registrado",
                _ => $"{vehiculos.Count} vehículos registrados"
            };

            ProximaCita.Children.Clear();
            ProximaCita.Children.Add(proximas.Count == 0
                ? (View)Vistas.SinDatos("No tienes citas programadas")
                : Vistas.CitaCard(proximas[0]));

            // Avisos del sistema para mantenimientos vencidos o próximos (una vez por día cada uno).
            Notificaciones.PedirPermiso();
            Notificaciones.RevisarMantenimientos(clienteId);

            Pendientes.Children.Clear();
            var pendientes = Taller.PendientesDeCliente(clienteId);
            if (pendientes.Count == 0)
                Pendientes.Children.Add(Vistas.SinDatos("Todo al día. Aquí aparecerán los mantenimientos que toque hacer."));
            foreach (var p in pendientes.Take(3))
                Pendientes.Children.Add(FilaPendiente(p));

            Vehiculos.Children.Clear();
            if (vehiculos.Count == 0)
                Vehiculos.Children.Add(Vistas.SinDatos("Registra tu vehículo para programar sus mantenimientos."));
            foreach (var v in vehiculos)
            {
                var id = v.Id;
                Vehiculos.Children.Add(Vistas.VehiculoCard(v,
                    () => Shell.Current.GoToAsync("mantenimientos", new Dictionary<string, object> { ["VehiculoId"] = id })));
            }
        }

        // Fila compacta: nombre del mantenimiento, vehículo y cuánto falta o cuánto pasó.
        static Border FilaPendiente(EstadoMantenimiento p)
        {
            var vehiculo = Taller.VehiculoPorId(p.Mantenimiento.VehiculoId);
            var textos = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textos.Add(new Label { Text = p.Mantenimiento.Nombre, Style = (Style)Application.Current!.Resources["CuerpoStrong"] });
            textos.Add(new Label { Text = $"{vehiculo?.Placa} · {Notificaciones.Describir(p)}", Style = (Style)Application.Current!.Resources["Pie"] });

            var fila = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
            fila.Add(textos);
            fila.Add(Ui.ChipDesgaste(p.Estado), 1, 0);
            return Vistas.Tarjeta(fila);
        }

        async void OnAgendar(object? sender, EventArgs e) => await Vistas.AgendarCitaWhatsApp();
    }
}
