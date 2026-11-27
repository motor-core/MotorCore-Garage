using Microsoft.Maui.Graphics;
using MauiIcons.Core;
using MauiIcons.Material;
using MotorCore_Garage.Models;
using Microsoft.Maui.Controls.Shapes;

namespace MotorCore_Garage.Services
{
    // Ayudas visuales compartidas: chips de desgaste y prioridad, y el aviso tipo snackbar.
    public static class Ui
    {
        public static Color Recurso(string clave) => (Color)Application.Current!.Resources[clave];

        // Botón de ojo junto a un campo de contraseña: alterna entre ocultar y mostrar lo escrito.
        public static void ConectarVerClave(Entry campo, Button boton)
        {
            void Pintar() => boton.ImageSource = (campo.IsPassword ? MaterialIcons.Visibility : MaterialIcons.VisibilityOff)
                .ToImageSource(Recurso("InkMuted"), 22);
            Pintar();
            boton.Clicked += (_, _) =>
            {
                campo.IsPassword = !campo.IsPassword;
                Pintar();
            };
        }

        public static Border Chip(string texto, Color fondo, Color letra, string? icono = null)
        {
            var etiqueta = new Label
            {
                Text = icono is null ? texto : $"{icono} {texto}",
                TextColor = letra,
                FontFamily = "NotoSansSemibold",
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            return new Border
            {
                Style = (Style)Application.Current!.Resources["Chip"],
                BackgroundColor = fondo,
                Content = etiqueta
            };
        }

        public static Border ChipDesgaste(Desgaste estado) => estado switch
        {
            Desgaste.Ok => Chip("Buen estado", Recurso("WearOk"), Recurso("White"), "✔"),
            Desgaste.Proximo => Chip("Próximo", Recurso("WearSoon"), Recurso("Ink"), "◷"),
            _ => Chip("Vencido", Recurso("WearDue"), Recurso("White"), "⚠")
        };

        public static string TextoDesgaste(Desgaste estado) => estado switch
        {
            Desgaste.Ok => "Buen estado",
            Desgaste.Proximo => "Próximo",
            _ => "Vencido"
        };

        public static Color ColorDesgaste(Desgaste estado) => estado switch
        {
            Desgaste.Ok => Recurso("WearOk"),
            Desgaste.Proximo => Recurso("WearSoon"),
            _ => Recurso("WearDue")
        };

        public static Border ChipPrioridad(Prioridad p) => p switch
        {
            Prioridad.Urgente => Chip("Urgente", Recurso("WearDue"), Recurso("White"), "⚠"),
            Prioridad.Prioritaria => Chip("Prioritaria", Recurso("WearSoon"), Recurso("Ink"), "◷"),
            _ => Chip("Normal", Recurso("Outline"), Recurso("Ink"))
        };

        // Botón de selección (p. ej. Monitor / Cliente): activo con relleno naranja, inactivo con contorno.
        public static void EstiloSeleccion(Button boton, bool activo)
        {
            boton.BackgroundColor = activo ? Recurso("Primary") : Colors.Transparent;
            boton.TextColor = activo ? Recurso("White") : Recurso("Primary");
            boton.BorderColor = Recurso("Primary");
            boton.BorderWidth = activo ? 0 : 1;
        }

        // Muestra un mensaje breve en la parte inferior de la pantalla (snackbar).
        public static async Task Snackbar(Border barra, Label texto, string mensaje, int milisegundos = 2500)
        {
            texto.Text = mensaje;
            barra.IsVisible = true;
            barra.Opacity = 0;
            await barra.FadeToAsync(1, 200);
            await Task.Delay(milisegundos);
            await barra.FadeToAsync(0, 200);
            barra.IsVisible = false;
        }
    }
}
