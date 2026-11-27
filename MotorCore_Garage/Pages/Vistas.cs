using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material;
using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // Tarjetas que se repiten en varias pantallas.
    public static class Vistas
    {
        static Style Estilo(string clave) => (Style)Application.Current!.Resources[clave];

        public static Border Tarjeta(View contenido, Action? alTocar = null)
        {
            var b = new Border { Style = Estilo("Tarjeta"), Content = contenido };
            if (alTocar is not null)
                b.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(alTocar) });
            return b;
        }

        public static ProgressBar Barra(int porcentaje, Desgaste estado) => new()
        {
            Progress = porcentaje / 100.0,
            ProgressColor = Ui.ColorDesgaste(estado),
            HeightRequest = 8
        };

        public static Border CitaCard(Cita c, Action? alTocar = null)
        {
            var cliente = Taller.ClientePorId(c.ClienteId);
            var vehiculo = Taller.VehiculoPorId(c.VehiculoId);

            var fila = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 12
            };
            // Icono de calendario arriba de la hora.
            var hora = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            hora.Add(new Image { Source = MaterialIcons.CalendarMonth.ToImageSource(Ui.Recurso("Primary"), 20), WidthRequest = 20, HeightRequest = 20 });
            hora.Add(new Label { Text = c.Fecha.ToString("HH:mm"), Style = Estilo("CuerpoStrong"), TextColor = Ui.Recurso("Primary"), HorizontalOptions = LayoutOptions.Center });
            fila.Add(hora);

            var textos = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textos.Add(new Label { Text = cliente?.Nombre ?? "", Style = Estilo("CuerpoStrong") });
            textos.Add(new Label { Text = vehiculo is null ? "" : $"{vehiculo.Titulo} · {vehiculo.Placa}", Style = Estilo("Pie") });
            textos.Add(new Label { Text = c.Servicio, Style = Estilo("Cuerpo"), FontSize = 14 });
            if (c.Estado == "Cancelada")
                textos.Add(new Label { Text = "Cancelada", Style = Estilo("Pie"), TextColor = Ui.Recurso("WearDue") });
            else if (c.Estado == "Solicitada")
                textos.Add(new Label { Text = "Pendiente de confirmación", Style = Estilo("Pie"), TextColor = Ui.Recurso("WearSoon") });
            fila.Add(textos, 1, 0);
            fila.Add(Ui.ChipPrioridad(c.Prioridad), 2, 0);
            return Tarjeta(fila, alTocar);
        }

        public static Border AlertaCard(Alerta a, Action agendar, Action contactar)
        {
            var contenido = new VerticalStackLayout { Spacing = 8 };

            var top = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 8
            };
            var textos = new VerticalStackLayout { Spacing = 2 };
            textos.Add(new Label { Text = $"{a.Vehiculo.Titulo} · {a.Vehiculo.Placa}", Style = Estilo("CuerpoStrong") });
            textos.Add(new Label { Text = $"{a.Cliente.Nombre} · {a.Componente.Nombre}", Style = Estilo("Pie"), FontSize = 14 });
            top.Add(textos);
            top.Add(Ui.ChipDesgaste(a.Componente.Estado), 1, 0);
            contenido.Add(top);

            contenido.Add(new Label { Text = TextoRestante(a.Componente), Style = Estilo("Pie") });
            contenido.Add(new Label { Text = $"Fecha estimada: {a.FechaEstimada:dd MMM yyyy}", Style = Estilo("Pie") });
            contenido.Add(Barra(a.Componente.Porcentaje, a.Componente.Estado));

            var acciones = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 8 };
            var agendarBtn = new Button { Text = "Agendar cita", Style = Estilo("BotonPrimario"), HeightRequest = 48, FontSize = 14 };
            agendarBtn.Clicked += (_, _) => agendar();
            var contactarBtn = new Button { Text = "Contactar", Style = Estilo("BotonSecundario"), HeightRequest = 48, FontSize = 14 };
            contactarBtn.Clicked += (_, _) => contactar();
            acciones.Add(agendarBtn);
            acciones.Add(contactarBtn, 1, 0);
            contenido.Add(acciones);

            return Tarjeta(contenido);
        }

        public static Border VehiculoCard(Vehiculo v, Action alTocar)
        {
            var peor = Peor(v);
            var contenido = new VerticalStackLayout { Spacing = 8 };

            var top = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                ColumnSpacing = 8
            };
            var textos = new VerticalStackLayout { Spacing = 2 };
            textos.Add(new Label { Text = v.Placa, Style = Estilo("Etiqueta") });
            textos.Add(new Label { Text = v.Titulo, Style = Estilo("Seccion") });
            top.Add(textos);
            top.Add(Ui.ChipDesgaste(peor.Estado), 1, 0);
            contenido.Add(top);

            contenido.Add(new Label { Text = $"{v.KmActual:N0} km · Cliente: {Taller.ClientePorId(v.ClienteId)?.Nombre}", Style = Estilo("Pie") });
            contenido.Add(Barra(peor.Porcentaje, peor.Estado));
            return Tarjeta(contenido, alTocar);
        }

        public static Grid ComponenteFila(EstadoComponente e)
        {
            var fila = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
                RowSpacing = 6
            };
            fila.Add(new Label { Text = e.Nombre, Style = Estilo("CuerpoStrong") });
            fila.Add(Ui.ChipDesgaste(e.Estado), 1, 0);
            fila.Add(new Label { Text = TextoRestante(e), Style = Estilo("Pie") }, 0, 1);
            fila.Add(new Label { Text = $"{e.Porcentaje}% de vida útil", Style = Estilo("Pie") }, 1, 1);
            var barra = Barra(e.Porcentaje, e.Estado);
            fila.Add(barra, 0, 2);
            Grid.SetColumnSpan(barra, 2);
            return fila;
        }

        // Texto de lista vacía, con el mismo estilo en todas las pantallas.
        public static Label SinDatos(string texto) => new() { Text = texto, Style = Estilo("EstadoVacio") };

        public static string TextoRestante(EstadoComponente e) =>
            e.KmRestantes > 0 ? $"Faltan {e.KmRestantes:N0} km" : $"Vencido hace {-e.KmRestantes:N0} km";

        // El componente con peor estado define el color de la tarjeta del vehículo.
        public static EstadoComponente Peor(Vehiculo v) =>
            Taller.Estados(v).OrderByDescending(e => e.Estado).ThenBy(e => e.KmRestantes).First();

        public static async Task Llamar(Cliente c)
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(c.Telefono);
            await Task.CompletedTask;
        }

        public static Task WhatsApp(Cliente c) =>
            Launcher.Default.OpenAsync($"https://wa.me/52{c.Telefono}");

        // Abre WhatsApp con el número del taller. Devuelve false si el número aún no está configurado.
        public static async Task<bool> AgendarCitaWhatsApp()
        {
            if (string.IsNullOrWhiteSpace(Taller.WhatsAppEmpresa)) return false;
            var texto = Uri.EscapeDataString(MensajeCita());
            await Launcher.Default.OpenAsync($"https://wa.me/{Taller.WhatsAppEmpresa}?text={texto}");
            return true;
        }

        // Mensaje para pedir cita. Si hay un cliente con sesión, incluye su nombre y su vehículo cuando tiene uno solo.
        static string MensajeCita()
        {
            // Formato de WhatsApp: *texto* en negrita.
            var lineas = new List<string> { "Hola, buen día. Quisiera agendar una cita de servicio en *Motor Core*." };

            if (Taller.EsCliente && Taller.ClientePorId(Taller.ClienteActualId) is { } cliente)
            {
                var datos = new List<string> { $"*Cliente:* {cliente.Nombre}" };
                var vehiculos = Taller.VehiculosDeCliente(cliente.Id).ToList();
                if (vehiculos.Count == 1)
                {
                    datos.Add($"*Vehículo:* {vehiculos[0].Titulo}");
                    datos.Add($"*Placa:* {vehiculos[0].Placa}");
                }
                lineas.Add(string.Join("\n", datos));
            }

            lineas.Add("¿Me compartes los horarios disponibles? Gracias.");
            return string.Join("\n\n", lineas);
        }

        public static async Task Contactar(Page pagina, Cliente c)
        {
            var opcion = await ModalPage.Opcion(pagina, $"Contactar a {c.Nombre}", "Llamar", "WhatsApp");
            if (opcion == "Llamar") await Llamar(c);
            else if (opcion == "WhatsApp") await WhatsApp(c);
        }
    }
}
