using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // El cliente solicita una cita nueva, o cambia la fecha de una existente (posponer).
    [QueryProperty(nameof(CitaId), "citaId")]
    public partial class ClienteNuevaCitaPage : ContentPage
    {
        int _citaId;
        List<Vehiculo> _vehiculos = new();
        Cita? _cita;
        string? _hora;
        readonly List<Button> _botonesHora = new();

        public ClienteNuevaCitaPage()
        {
            InitializeComponent();
            Servicio.ItemsSource = NuevaCitaPage.TiposServicio;
            Fecha.MinimumDate = DateTime.Today;
            Fecha.Date = DateTime.Today;
            ConstruirHorarios();
        }

        // 0 = cita nueva; un número distinto de cero = cita existente que se pospone.
        public int CitaId
        {
            set => _citaId = value;
        }

        // El DatePicker puede devolver null; en ese caso se usa hoy.
        DateTime FechaSel => Fecha.Date ?? DateTime.Today;

        // Al posponer, la propia cita no debe contar contra su turno.
        int? Excluir => _citaId == 0 ? null : _citaId;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vehiculos = Taller.VehiculosDeCliente(Taller.ClienteActualId).ToList();
            Vehiculo.ItemsSource = _vehiculos.Select(v => $"{v.Titulo} · {v.Placa}").ToList();

            _cita = _citaId == 0 ? null : Taller.CitasDeCliente(Taller.ClienteActualId).FirstOrDefault(c => c.Id == _citaId);
            if (_cita is null) return;

            // Posponer: el vehículo y el servicio no cambian, solo la fecha y el turno.
            Vehiculo.SelectedIndex = _vehiculos.FindIndex(v => v.Id == _cita.VehiculoId);
            Vehiculo.IsEnabled = false;
            Servicio.SelectedIndex = Array.IndexOf(NuevaCitaPage.TiposServicio, _cita.Servicio);
            Servicio.IsEnabled = false;
            Fecha.Date = _cita.Fecha.Date;
            _hora = $"{_cita.Fecha.Hour:00}:00";
            TituloPagina.Text = "Posponer cita";
            BotonConfirmar.Text = "Guardar nueva fecha";
            ActualizarHorarios();
        }

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

        // Un turno no se puede elegir si ya pasó o si la bahía está llena.
        void ActualizarHorarios()
        {
            for (int i = 0; i < _botonesHora.Count; i++)
            {
                var hora = Taller.Horarios[i];
                var boton = _botonesHora[i];
                var lleno = Taller.HorarioLleno(FechaSel, hora, Excluir);
                var pasado = !lleno && Taller.ErrorCita(FechaSel.AddHours(int.Parse(hora[..2])), Excluir) is not null;
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
            if (_hora is not null && Taller.ErrorCita(FechaSel.AddHours(int.Parse(_hora[..2])), Excluir) is not null) _hora = null;
            ActualizarHorarios();
        }

        async void OnConfirmar(object? sender, EventArgs e)
        {
            var valido = true;
            valido &= Validar(ErrorVehiculo, _cita is null && Vehiculo.SelectedIndex < 0, "Elige uno de tus vehículos");
            valido &= Validar(ErrorServicio, _cita is null && Servicio.SelectedIndex < 0, "Elige el tipo de servicio");
            valido &= Validar(ErrorHora, _hora is null, "Elige un turno disponible");
            if (!valido) return;

            var inicio = FechaSel.AddHours(int.Parse(_hora![..2]));
            string? error;
            if (_cita is null)
            {
                var vehiculo = _vehiculos[Vehiculo.SelectedIndex];
                var servicio = NuevaCitaPage.TiposServicio[Servicio.SelectedIndex];
                error = Taller.AgregarCita(new Cita
                {
                    Fecha = inicio,
                    ClienteId = Taller.ClienteActualId,
                    VehiculoId = vehiculo.Id,
                    Servicio = servicio
                });
                if (error is null) Taller.Bitacorar($"solicitó cita de {servicio} para {vehiculo.Placa}");
            }
            else
            {
                error = Taller.PosponerCita(_cita, inicio);
            }

            if (error is not null)
            {
                Validar(ErrorHora, true, error);
                return;
            }

            await Ui.Snackbar(Snack, SnackTexto, _cita is null ? "Cita solicitada" : "Cita pospuesta", 1200);
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
