using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // Mantenimientos programados de un vehículo: cada uno con su periodo, su estado y el botón para marcarlo como hecho.
    [QueryProperty(nameof(VehiculoId), "VehiculoId")]
    public partial class ClienteMantenimientosPage : ContentPage
    {
        int _vehiculoId;

        public ClienteMantenimientosPage()
        {
            InitializeComponent();
        }

        public int VehiculoId
        {
            set => _vehiculoId = value;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Mostrar();
        }

        void Mostrar()
        {
            var vehiculo = Taller.VehiculoPorId(_vehiculoId);
            if (vehiculo is null) return;

            Subtitulo.Text = $"{vehiculo.Titulo} · {vehiculo.Placa} · {vehiculo.KmActual:N0} km";
            Lista.Children.Clear();

            var estados = Taller.MantenimientosDeVehiculo(vehiculo.Id)
                .Select(m => Taller.EstadoDe(m, vehiculo))
                .OrderByDescending(e => e.Estado)
                .ThenBy(e => e.Mantenimiento.Nombre)
                .ToList();

            if (estados.Count == 0)
            {
                Lista.Children.Add(Vistas.SinDatos("Aún no programas mantenimientos. Empieza con el cambio de aceite."));
                return;
            }
            foreach (var e in estados)
                Lista.Children.Add(Tarjeta(e, vehiculo));
        }

        Border Tarjeta(EstadoMantenimiento estado, Vehiculo vehiculo)
        {
            var m = estado.Mantenimiento;
            var contenido = new VerticalStackLayout { Spacing = 8 };

            var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
            top.Add(new Label { Text = m.Nombre, Style = (Style)Application.Current!.Resources["CuerpoStrong"] });
            top.Add(Ui.ChipDesgaste(estado.Estado), 1, 0);
            contenido.Add(top);

            contenido.Add(new Label { Text = Periodo(m), Style = (Style)Application.Current!.Resources["Pie"] });
            contenido.Add(new Label { Text = $"Último: {m.FechaUltima:dd MMM yyyy} · {m.KmUltimo:N0} km", Style = (Style)Application.Current!.Resources["Pie"] });
            contenido.Add(new Label { Text = Notificaciones.Describir(estado), Style = (Style)Application.Current!.Resources["CuerpoStrong"], FontSize = 14 });

            var acciones = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 8 };
            var hecho = new Button { Text = "Marcar como hecho", Style = (Style)Application.Current!.Resources["BotonPrimario"], HeightRequest = 44, FontSize = 14 };
            hecho.Clicked += async (_, _) => await MarcarHecho(m, vehiculo);
            var eliminar = new Button { Text = "Eliminar", Style = (Style)Application.Current!.Resources["BotonSecundario"], HeightRequest = 44, FontSize = 14 };
            eliminar.Clicked += async (_, _) => await Eliminar(m);
            acciones.Add(hecho);
            acciones.Add(eliminar, 1, 0);
            contenido.Add(acciones);

            return Vistas.Tarjeta(contenido);
        }

        static string Periodo(Mantenimiento m)
        {
            var partes = new List<string>();
            if (m.IntervaloKm > 0) partes.Add($"cada {m.IntervaloKm:N0} km");
            if (m.IntervaloMeses > 0) partes.Add($"cada {m.IntervaloMeses} meses");
            return "Periodo: " + string.Join(" o ", partes);
        }

        async Task MarcarHecho(Mantenimiento m, Vehiculo vehiculo)
        {
            var texto = await ModalPage.Pedir(this, "Marcar como hecho", "Kilometraje al momento del servicio", "Guardar", "Cancelar",
                placeholder: vehiculo.KmActual.ToString(), valorInicial: vehiculo.KmActual.ToString(), teclado: Keyboard.Numeric);
            if (texto is null) return;

            if (!int.TryParse(texto.Trim(), out var km) || km < vehiculo.KmActual)
            {
                await ModalPage.Aviso(this, "Kilometraje no válido", $"Escribe un número igual o mayor a {vehiculo.KmActual:N0} km.");
                return;
            }

            Taller.MarcarRealizado(m, km);
            Mostrar();
        }

        async Task Eliminar(Mantenimiento m)
        {
            var confirmar = await ModalPage.Confirmar(this, "Eliminar mantenimiento", $"¿Eliminar «{m.Nombre}»? Ya no recibirás avisos de este servicio.", "Eliminar", "Cancelar");
            if (!confirmar) return;
            Taller.EliminarMantenimiento(m);
            Mostrar();
        }

        async void OnAgregar(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("nuevomantenimiento", new Dictionary<string, object> { ["VehiculoId"] = _vehiculoId });
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
