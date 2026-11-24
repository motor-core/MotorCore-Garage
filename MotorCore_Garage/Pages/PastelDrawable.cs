using Microsoft.Maui.Graphics;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    // Una rebanada de la gráfica de pastel: nombre, cantidad y color.
    public record Porcion(string Nombre, double Valor, Color Color);

    // Gráfica de pastel. Si no hay datos dibuja un círculo gris vacío.
    public class PastelDrawable : IDrawable
    {
        readonly List<Porcion> _porciones;

        public PastelDrawable(List<Porcion> porciones)
        {
            _porciones = porciones;
        }

        public void Draw(ICanvas canvas, RectF d)
        {
            var radio = Math.Min(d.Width, d.Height) / 2 - 4;
            var centro = d.Center;
            var total = _porciones.Sum(p => p.Valor);

            if (total <= 0)
            {
                canvas.FillColor = Ui.Recurso("Outline");
                canvas.FillCircle(centro, radio);
                return;
            }

            var area = new RectF(centro.X - radio, centro.Y - radio, radio * 2, radio * 2);
            float inicio = -90; // empieza arriba, en las 12
            foreach (var p in _porciones.Where(p => p.Valor > 0))
            {
                canvas.FillColor = p.Color;
                // Una sola categoría ocupa todo el círculo; un arco de 360° no se dibuja completo.
                if (p.Valor >= total)
                {
                    canvas.FillCircle(centro, radio);
                    break;
                }
                var barrido = (float)(p.Valor / total * 360);
                canvas.FillArc(area.Left, area.Top, area.Width, area.Height, inicio, barrido, true);
                inicio += barrido;
            }
        }

        // Leyenda con color, nombre, cantidad y porcentaje de cada rebanada.
        public static VerticalStackLayout Leyenda(List<Porcion> porciones)
        {
            var total = porciones.Sum(p => p.Valor);
            var leyenda = new VerticalStackLayout { Spacing = 6 };
            if (total <= 0)
            {
                leyenda.Add(Vistas.SinDatos("Sin datos todavía"));
                return leyenda;
            }

            foreach (var p in porciones)
            {
                var fila = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
                fila.Add(new BoxView { WidthRequest = 12, HeightRequest = 12, CornerRadius = 6, BackgroundColor = p.Color, VerticalOptions = LayoutOptions.Center });
                fila.Add(new Label { Text = p.Nombre, Style = (Style)Application.Current!.Resources["Pie"], FontSize = 13, VerticalOptions = LayoutOptions.Center }, 1, 0);
                fila.Add(new Label { Text = $"{p.Valor:N0} · {p.Valor / total:P0}", Style = (Style)Application.Current!.Resources["Etiqueta"], FontSize = 12, VerticalOptions = LayoutOptions.Center }, 2, 0);
                leyenda.Add(fila);
            }
            return leyenda;
        }
    }
}
