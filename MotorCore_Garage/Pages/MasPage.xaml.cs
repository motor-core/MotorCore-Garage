using System.Text.Json;
using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    public partial class MasPage : ContentPage
    {
        public MasPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Usuario.Text = Taller.UsuarioActual;
            Rol.Text = Taller.RolActual == Models.Rol.Administrador ? "Administrador" : "Mecánico";

            var admin = Taller.RolActual == Models.Rol.Administrador;
            BotonReportes.IsVisible = admin;
            BotonMecanico.IsVisible = admin;
            AyudaEquipo.Text = admin
                ? "Toca un mecánico para eliminar su cuenta."
                : "Solo el administrador puede agregar o eliminar cuentas.";

            MostrarBahias();
            BotonMenosBahias.IsEnabled = admin;
            BotonMasBahias.IsEnabled = admin;

            MostrarEquipo();

            Catalogo.Children.Clear();
            foreach (var c in Taller.Catalogo)
            {
                var fila = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(120)) }, ColumnSpacing = 12 };
                fila.Add(new Label { Text = c.Nombre, Style = (Style)Application.Current!.Resources["Cuerpo"], VerticalOptions = LayoutOptions.Center });
                var entrada = new Entry { Text = c.IntervaloKm.ToString(), Keyboard = Keyboard.Numeric, FontSize = 16, IsEnabled = admin, HorizontalTextAlignment = TextAlignment.End };
                entrada.Completed += (_, _) => GuardarIntervalo(c, entrada);
                entrada.Unfocused += (_, _) => GuardarIntervalo(c, entrada);
                fila.Add(entrada, 1, 0);
                Catalogo.Children.Add(fila);
            }
        }

        // Lista de cuentas del taller. Solo el administrador puede tocar un mecánico para eliminarlo.
        void MostrarEquipo()
        {
            var admin = Taller.RolActual == Models.Rol.Administrador;
            Equipo.Children.Clear();

            foreach (var cuenta in Taller.CuentasDelTaller())
            {
                if (Equipo.Children.Count > 0)
                    Equipo.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Ui.Recurso("Outline") });

                var esYo = cuenta.Username == Taller.UsuarioActual;
                var esAdmin = cuenta.Rol == Models.Rol.Administrador;

                var textos = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                textos.Add(new Label { Text = esYo ? $"{cuenta.Username} (tú)" : cuenta.Username, Style = (Style)Application.Current!.Resources["CuerpoStrong"] });
                textos.Add(new Label { Text = esAdmin ? "Administrador" : "Mecánico", Style = (Style)Application.Current!.Resources["Pie"] });

                var fila = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8, Padding = new Thickness(16, 12) };
                fila.Add(textos);
                fila.Add(esAdmin
                    ? Ui.Chip("Admin", Ui.Recurso("Primary"), Ui.Recurso("White"))
                    : Ui.Chip("Mecánico", Ui.Recurso("Outline"), Ui.Recurso("Ink")), 1, 0);

                if (admin && !esAdmin)
                {
                    var cuentaAEliminar = cuenta;
                    fila.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await EliminarMecanico(cuentaAEliminar)) });
                }
                Equipo.Children.Add(fila);
            }
        }

        async Task EliminarMecanico(Models.Usuario cuenta)
        {
            var confirmar = await DisplayAlertAsync("Eliminar mecánico", $"¿Eliminar la cuenta «{cuenta.Username}»? Ya no podrá iniciar sesión.", "Eliminar", "Cancelar");
            if (!confirmar) return;

            var error = Taller.EliminarCuentaMecanico(cuenta);
            if (error is not null)
            {
                await DisplayAlertAsync("No se pudo eliminar", error, "OK");
                return;
            }
            MostrarEquipo();
        }

        async void OnAgregarMecanico(object? sender, EventArgs e)
        {
            var usuario = await DisplayPromptAsync("Nuevo mecánico", "Usuario para iniciar sesión", "Siguiente", "Cancelar",
                placeholder: "Ej. mramirez", maxLength: 50);
            if (string.IsNullOrWhiteSpace(usuario)) return;

            var clave = await DisplayPromptAsync("Nuevo mecánico", "Contraseña (mínimo 6 caracteres)", "Crear cuenta", "Cancelar", maxLength: 50);
            if (string.IsNullOrWhiteSpace(clave)) return;

            var error = await Taller.CrearCuentaMecanicoAsync(usuario, clave);
            if (error is not null)
            {
                await DisplayAlertAsync("No se pudo crear la cuenta", error, "OK");
                return;
            }

            MostrarEquipo();
            await DisplayAlertAsync("Cuenta creada", $"«{usuario.Trim().ToLowerInvariant()}» ya puede iniciar sesión como mecánico.", "OK");
        }

        void GuardarIntervalo(ComponenteDef c, Entry entrada)
        {
            if (int.TryParse(entrada.Text, out var km) && km > 0)
            {
                c.IntervaloKm = km;
                Taller.GuardarIntervalo(c);
            }
            else
            {
                entrada.Text = c.IntervaloKm.ToString();
            }
        }

        void MostrarBahias()
        {
            ValorBahias.Text = Taller.Bahias.ToString();
            TextoBahias.Text = $"{Taller.Bahias} bahías en el taller";
        }

        void OnMenosBahias(object? sender, EventArgs e) => CambiarBahias(-1);

        void OnMasBahias(object? sender, EventArgs e) => CambiarBahias(1);

        // Mínimo 1 y máximo 10 bahías, como antes con el control de pasos.
        void CambiarBahias(int cambio)
        {
            var nuevo = Math.Clamp(Taller.Bahias + cambio, 1, 10);
            if (nuevo == Taller.Bahias) return;
            Taller.Bahias = nuevo;
            MostrarBahias();
        }

        async void OnClientes(object? sender, EventArgs e) => await Shell.Current.GoToAsync("clientes");

        async void OnReportes(object? sender, EventArgs e) => await Shell.Current.GoToAsync("reportes");

        async void OnRespaldo(object? sender, EventArgs e)
        {
            var ruta = Path.Combine(FileSystem.AppDataDirectory, $"respaldo_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            var datos = new
            {
                Clientes = Taller.Clientes,
                Vehiculos = Taller.Vehiculos,
                Citas = Taller.Citas,
                Servicios = Taller.Servicios,
                Catalogo = Taller.Catalogo
            };
            await File.WriteAllTextAsync(ruta, JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true }));
            Taller.Bitacorar("creó un respaldo");
            await DisplayAlertAsync("Respaldo listo", $"Se guardó en:\n{ruta}", "Entendido");
        }

        async void OnCerrarSesion(object? sender, EventArgs e)
        {
            var confirmar = await DisplayAlertAsync("Cerrar sesión", "¿Seguro que quieres salir?", "Salir", "Cancelar");
            if (!confirmar) return;
            Taller.CerrarSesion();
            Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage());
        }
    }
}
