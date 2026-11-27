using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class NuevaCitaPage : ContentPage
    {
        public static readonly string[] TiposServicio =
        {
            "Cambio de aceite", "Afinación mayor", "Revisión de frenos",
            "Cambio de llantas", "Revisión general", "Cambio de batería", "Suspensión"
        };

        List<Vehiculo> _vehiculos = new();
        string? _hora;
        readonly List<Button> _botonesHora = new();

        public NuevaCitaPage()
        {
            InitializeComponent();
            Cliente.ItemsSource = Taller.Clientes.Select(c => c.Nombre).ToList();
            Servicio.ItemsSource = TiposServicio;
            Prioridad.ItemsSource = Enum.GetNames<Prioridad>();
            Prioridad.SelectedIndex = 0;
            Fecha.MinimumDate = DateTime.Today;
            Fecha.Date = DateTime.Today;
            ConstruirHorarios();
        }

        // El DatePicker puede devolver null; en ese caso se usa hoy.
        DateTime FechaSel => Fecha.Date ?? DateTime.Today;

        void ConstruirHorarios()
        {
            for (int i = 0; i < Taller.Horarios.Length; i++)
            {
                var hora = Taller.Horarios[i];
                var boton = new Button { Text = hora, FontSize = 14, HeightRequest = 48, CornerRadius = 12, Padding = 0 };
                boton.Clicked += (_, _) => { _hora = hora; ActualizarHorarios(); };
                _botonesHora.Add(boton);
                Horarios.Add(boton, i % 4, i / 4);
            }
            ActualizarHorarios();
        }

        void ActualizarHorarios()
        {
            for (int i = 0; i < _botonesHora.Count; i++)
            {
                var hora = Taller.Horarios[i];
                var boton = _botonesHora[i];
                var lleno = Taller.HorarioLleno(FechaSel, hora);
                var pasado = Taller.ErrorCita(FechaSel.AddHours(int.Parse(hora[..2]))) is not null && !lleno;
                var noDisponible = lleno || pasado;
                boton.IsEnabled = !noDisponible;
                var seleccionado = hora == _hora && !noDisponible;
                boton.BackgroundColor = seleccionado ? Ui.Recurso("Primary") : Ui.Recurso("Card");
                boton.TextColor = seleccionado ? Ui.Recurso("White") : noDisponible ? Ui.Recurso("InkMuted") : Ui.Recurso("Ink");
                boton.BorderColor = Ui.Recurso("Outline");
                boton.BorderWidth = 1;
                boton.Text = lleno ? $"{hora}\nLleno" : pasado ? $"{hora}\nPasado" : hora;
            }
        }

        void OnFecha(object? sender, DateChangedEventArgs e)
        {
            if (_hora is not null && Taller.ErrorCita(FechaSel.AddHours(int.Parse(_hora[..2]))) is not null) _hora = null;
            ActualizarHorarios();
        }

        void OnCliente(object? sender, EventArgs e)
        {
            if (Cliente.SelectedIndex < 0) return;
            var cliente = Taller.Clientes[Cliente.SelectedIndex];
            _vehiculos = Taller.VehiculosDeCliente(cliente.Id).ToList();
            Vehiculo.ItemsSource = _vehiculos.Select(v => $"{v.Titulo} · {v.Placa}").ToList();
            Vehiculo.SelectedIndex = -1;
        }

        async void OnConfirmar(object? sender, EventArgs e)
        {
            var valido = true;
            valido &= Validar(ErrorCliente, Cliente.SelectedIndex < 0, "Elige un cliente");
            valido &= Validar(ErrorVehiculo, Vehiculo.SelectedIndex < 0, "Elige un vehículo del cliente");
            valido &= Validar(ErrorServicio, Servicio.SelectedIndex < 0, "Elige el tipo de servicio");
            valido &= Validar(ErrorHora, _hora is null, "Elige un turno disponible");
            if (!valido) return;

            var cliente = Taller.Clientes[Cliente.SelectedIndex];
            var vehiculo = _vehiculos[Vehiculo.SelectedIndex];
            var inicio = FechaSel.AddHours(int.Parse(_hora![..2]));
            var prioridad = (Prioridad)Prioridad.SelectedIndex;

            var error = Taller.AgregarCita(new Cita
            {
                Fecha = inicio,
                ClienteId = cliente.Id,
                VehiculoId = vehiculo.Id,
                Servicio = TiposServicio[Servicio.SelectedIndex],
                Prioridad = prioridad
            });
            if (error is not null)
            {
                Validar(ErrorHora, true, error);
                return;
            }
            Taller.Bitacorar($"agendó cita de {TiposServicio[Servicio.SelectedIndex]} para {vehiculo.Placa}");

            await Ui.Snackbar(Snack, SnackTexto, "Cita guardada", 1200);
            await Shell.Current.GoToAsync("..");
        }

        static bool Validar(Label error, bool fallo, string mensaje)
        {
            error.Text = mensaje;
            error.IsVisible = fallo;
            return !fallo;
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
