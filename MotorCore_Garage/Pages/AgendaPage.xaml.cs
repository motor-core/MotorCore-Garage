using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class AgendaPage : ContentPage
    {
        static readonly CultureInfo Mexico = new("es-MX");
        DateTime _dia = DateTime.Today;

        public AgendaPage()
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
            ConstruirSemana();
            var fecha = _dia.ToString("dddd d 'de' MMMM", Mexico);
            Titulo.Text = char.ToUpper(fecha[0]) + fecha[1..];

            var sat = Taller.Saturacion(_dia);
            var (texto, estado) = sat switch
            {
                < 0.4 => ("Baja", Desgaste.Ok),
                < 0.75 => ("Media", Desgaste.Proximo),
                _ => ("Alta", Desgaste.Vencido)
            };
            Saturacion.Content = new Label
            {
                Text = $"{texto} · {Taller.CitasEn(_dia)} citas",
                FontFamily = "NotoSansSemibold",
                FontSize = 12,
                TextColor = estado == Desgaste.Proximo ? Ui.Recurso("Ink") : Ui.Recurso("White")
            };
            Saturacion.BackgroundColor = Ui.ColorDesgaste(estado);

            ConstruirBloques();
        }

        void ConstruirSemana()
        {
            Semana.Children.Clear();
            Semana.ColumnDefinitions.Clear();
            var offset = ((int)_dia.DayOfWeek + 6) % 7;
            var lunes = _dia.AddDays(-offset);

            for (int i = 0; i < 7; i++)
            {
                var d = lunes.AddDays(i);
                Semana.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                var sat = Taller.Saturacion(d);
                var punto = sat < 0.4 ? "WearOk" : sat < 0.75 ? "WearSoon" : "WearDue";
                var seleccionado = d.Date == _dia.Date;

                var letra = d.ToString("ddd", Mexico).Substring(0, 1).ToUpperInvariant();
                var caja = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Fill };
                caja.Add(new Label { Text = letra, FontSize = 12, HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = seleccionado ? Ui.Recurso("White") : Ui.Recurso("InkMuted") });
                // Días no seleccionados: gris claro (un tono más oscuro que el blanco del día activo).
                var oscuro = Application.Current!.RequestedTheme == AppTheme.Dark;
                caja.Add(new Label { Text = d.Day.ToString(), FontFamily = "NotoSansSemibold", FontSize = 16,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = seleccionado ? Ui.Recurso("White") : Ui.Recurso(oscuro ? "Gray200" : "Gray400") });
                caja.Add(new BoxView { WidthRequest = 8, HeightRequest = 8, CornerRadius = 4,
                    BackgroundColor = Ui.Recurso(punto), HorizontalOptions = LayoutOptions.Center });

                var borde = new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(0, 8),
                    BackgroundColor = seleccionado ? Ui.Recurso("Primary") : Colors.Transparent,
                    Content = caja
                };
                var fecha = d;
                borde.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { _dia = fecha; Refrescar(); }) });
                Semana.Add(borde, i, 0);
            }
        }

        void ConstruirBloques()
        {
            Bloques.Children.Clear();
            foreach (var hora in Taller.Horarios)
            {
                var h = int.Parse(hora[..2]);
                var fila = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(56)), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 8 };
                fila.Add(new Label { Text = hora, Style = (Style)Application.Current!.Resources["CuerpoStrong"], Margin = new Thickness(0, 12, 0, 0) });

                var contenido = new VerticalStackLayout { Spacing = 8 };
                var citas = Taller.Citas.Where(c => c.Fecha.Date == _dia.Date && c.Fecha.Hour == h && c.Estado != "Cancelada").OrderBy(c => c.Fecha).ToList();
                if (citas.Count == 0)
                {
                    contenido.Add(new Label { Text = "Libre", Style = (Style)Application.Current!.Resources["Pie"], Margin = new Thickness(0, 12, 0, 0) });
                }
                foreach (var c in citas)
                    contenido.Add(Vistas.CitaCard(c, () => AlTocarCita(c)));

                var lleno = Taller.HorarioLleno(_dia, hora);
                if (lleno && citas.Count > 0)
                    contenido.Add(new Label { Text = "Turno lleno", Style = (Style)Application.Current!.Resources["Pie"], TextColor = Ui.Recurso("WearDue") });

                fila.Add(contenido, 1, 0);
                Bloques.Children.Add(fila);
            }
        }

        // Una solicitud del cliente se confirma o rechaza aquí; cualquier otra cita abre la recepción.
        async void AlTocarCita(Cita cita)
        {
            if (cita.Estado != "Solicitada")
            {
                await Shell.Current.GoToAsync("recepcion");
                return;
            }

            var opcion = await ModalPage.Opcion(this, "Solicitud de cita", "Confirmar", "Rechazar");
            string? error = null;
            if (opcion == "Confirmar") error = Taller.ConfirmarCita(cita);
            else if (opcion == "Rechazar") error = Taller.CancelarCita(cita);
            else return;

            if (error is not null)
            {
                await ModalPage.Aviso(this, "No se pudo completar", error);
                return;
            }
            Refrescar();
        }

        async void OnNuevaCita(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("nuevacita");
        }
    }
}
