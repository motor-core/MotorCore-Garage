using System.Globalization;
using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    [QueryProperty(nameof(VehiculoId), "VehiculoId")]
    public partial class RegistrarServicioPage : ContentPage
    {
        List<Vehiculo> _vehiculos = new();
        readonly List<(CheckBox Caja, ComponenteDef Componente)> _componentes = new();

        public RegistrarServicioPage()
        {
            InitializeComponent();
            Fecha.MaximumDate = DateTime.Today;
            Fecha.Date = DateTime.Today;
            Mecanico.Text = Taller.UsuarioActual;

            foreach (var c in Taller.Catalogo)
            {
                var caja = new CheckBox { Color = Ui.Recurso("Primary") };
                caja.CheckedChanged += (_, _) => ActualizarEfecto();
                _componentes.Add((caja, c));

                var fila = new HorizontalStackLayout { Spacing = 10 };
                fila.Add(caja);
                fila.Add(new Label
                {
                    Text = $"{c.Nombre} · cada {c.IntervaloKm:N0} km",
                    Style = (Style)Application.Current!.Resources["Cuerpo"],
                    VerticalTextAlignment = TextAlignment.Center
                });
                ListaComponentes.Add(fila);
            }

            Recargar();
            ActualizarEfecto();
        }

        void Recargar()
        {
            _vehiculos = Taller.Vehiculos.ToList();
            VehiculoPicker.ItemsSource = _vehiculos.Select(v => $"{v.Titulo} · {v.Placa}").ToList();
        }

        // Llega desde el expediente: preselecciona el vehículo.
        public int VehiculoId
        {
            set
            {
                Recargar();
                var indice = _vehiculos.FindIndex(v => v.Id == value);
                if (indice >= 0) VehiculoPicker.SelectedIndex = indice;
            }
        }

        Vehiculo? Seleccionado => VehiculoPicker.SelectedIndex >= 0 ? _vehiculos[VehiculoPicker.SelectedIndex] : null;

        void OnVehiculo(object? sender, EventArgs e)
        {
            var v = Seleccionado;
            UltimoKm.Text = v is null ? "" : $"Kilometraje registrado: {v.KmActual:N0} km";
            Km.Placeholder = v is null ? "0" : $"Mínimo {v.KmActual:N0}";
            ErrorLabel(false);
        }

        // Muestra qué intervalos se reiniciarán con los componentes marcados.
        void ActualizarEfecto()
        {
            var marcados = _componentes.Where(x => x.Caja.IsChecked).Select(x => x.Componente.Nombre).ToList();
            Efecto.Text = marcados.Count == 0
                ? "Marca los componentes atendidos: su contador de desgaste se reinicia en el kilometraje del servicio."
                : $"Se reiniciarán: {string.Join(", ", marcados)}";
        }

        async void OnGuardar(object? sender, EventArgs e)
        {
            var v = Seleccionado;
            if (v is null)
            {
                Mostrar("Elige el vehículo");
                return;
            }
            if (!int.TryParse(Km.Text, out var km))
            {
                Mostrar("Escribe el kilometraje del servicio en números");
                return;
            }
            if (km < v.KmActual)
            {
                Mostrar($"El kilometraje no puede ser menor a {v.KmActual:N0} km");
                return;
            }
            var trabajo = (Trabajo.Text ?? "").Trim();
            if (trabajo.Length == 0)
            {
                Mostrar("Describe el trabajo realizado");
                return;
            }
            var componentes = _componentes.Where(x => x.Caja.IsChecked).Select(x => x.Componente).ToList();
            if (componentes.Count == 0)
            {
                Mostrar("Marca al menos un componente atendido");
                return;
            }

            var textoCosto = (Costo.Text ?? "").Trim();
            decimal costo = 0m;
            if (textoCosto.Length > 0 && (!decimal.TryParse(textoCosto, NumberStyles.Number, CultureInfo.CurrentCulture, out costo) || costo < 0))
            {
                Mostrar("El costo debe ser un número igual o mayor a 0");
                return;
            }
            var servicio = new RegistroServicio
            {
                VehiculoId = v.Id,
                Fecha = Fecha.Date ?? DateTime.Today,
                Km = km,
                Servicio = trabajo,
                Costo = costo,
                Mecanico = (Mecanico.Text ?? "").Trim(),
                Observaciones = (Observaciones.Text ?? "").Trim()
            };

            var error = Taller.RegistrarServicio(servicio, componentes);
            if (error is not null)
            {
                Mostrar(error);
                return;
            }

            await ModalPage.Aviso(this, "Servicio registrado", $"{trabajo} · {v.Placa}", "Listo");
            await Shell.Current.GoToAsync("..");
        }

        void Mostrar(string mensaje) => ErrorLabel(true, mensaje);

        void ErrorLabel(bool visible, string mensaje = "")
        {
            Error.Text = mensaje;
            Error.IsVisible = visible;
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
