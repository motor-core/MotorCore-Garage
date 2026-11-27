namespace MotorCore_Garage.Pages
{
    // Modal con bordes redondeados que reemplaza los diálogos nativos, que no permiten cambiar su forma.
    public partial class ModalPage : ContentPage
    {
        readonly TaskCompletionSource<(int Boton, string? Texto)> _resultado = new();

        ModalPage(string titulo, string? mensaje, string[] botones, bool conEntrada,
                  string? placeholder, string? valorInicial, Keyboard? teclado, int maxLength, bool esClave)
        {
            InitializeComponent();
            Titulo.Text = titulo;
            Mensaje.Text = mensaje;
            Mensaje.IsVisible = mensaje is not null;

            if (conEntrada)
            {
                Entrada.IsVisible = true;
                Entrada.Placeholder = placeholder;
                Entrada.Text = valorInicial;
                Entrada.Keyboard = teclado ?? Keyboard.Default;
                Entrada.MaxLength = maxLength;
                Entrada.IsPassword = esClave;
            }

            ConstruirAcciones(botones);
        }

        public static Task Aviso(Page origen, string titulo, string mensaje, string boton = "OK") =>
            Abrir(origen, titulo, mensaje, [boton], false, null, null, null, int.MaxValue);

        public static async Task<bool> Confirmar(Page origen, string titulo, string mensaje, string aceptar, string cancelar)
        {
            var (boton, _) = await Abrir(origen, titulo, mensaje, [aceptar, cancelar], false, null, null, null, int.MaxValue);
            return boton == 0;
        }

        public static async Task<string?> Pedir(Page origen, string titulo, string mensaje, string aceptar, string cancelar,
                                                string? placeholder = null, string? valorInicial = null,
                                                Keyboard? teclado = null, int maxLength = int.MaxValue, bool esClave = false)
        {
            var (boton, texto) = await Abrir(origen, titulo, mensaje, [aceptar, cancelar], true, placeholder, valorInicial, teclado, maxLength, esClave);
            return boton == 0 ? texto : null;
        }

        // Lista de opciones; devuelve la elegida o null si se cancela.
        public static async Task<string?> Opcion(Page origen, string titulo, params string[] opciones)
        {
            var (boton, _) = await Abrir(origen, titulo, null, [.. opciones, "Cancelar"], false, null, null, null, int.MaxValue);
            return boton >= 0 && boton < opciones.Length ? opciones[boton] : null;
        }

        static async Task<(int Boton, string? Texto)> Abrir(Page origen, string titulo, string? mensaje, string[] botones, bool conEntrada,
                                                            string? placeholder, string? valorInicial, Keyboard? teclado, int maxLength,
                                                            bool esClave = false)
        {
            var pagina = new ModalPage(titulo, mensaje, botones, conEntrada, placeholder, valorInicial, teclado, maxLength, esClave);
            await origen.Navigation.PushModalAsync(pagina);
            return await pagina._resultado.Task;
        }

        // Hasta dos botones van en fila; más opciones van apiladas. El primero es el principal.
        void ConstruirAcciones(string[] botones)
        {
            var enFila = botones.Length <= 2;
            for (var i = 0; i < botones.Length; i++)
            {
                var indice = i;
                var boton = new Button
                {
                    Text = botones[i],
                    Style = (Style)Application.Current!.Resources[i == 0 ? "BotonPrimario" : "BotonSecundario"]
                };
                boton.Clicked += async (_, _) => await Cerrar(indice);

                if (enFila)
                {
                    Acciones.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                    Grid.SetColumn(boton, i);
                }
                else
                {
                    Acciones.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    Grid.SetRow(boton, i);
                }
                Acciones.Add(boton);
            }
        }

        async Task Cerrar(int boton)
        {
            var texto = Entrada.IsVisible ? Entrada.Text : null;
            // Cerrar primero y completar después: la continuación del llamador puede navegar (p. ej. GoToAsync)
            // y ese cambio ya cierra el modal, así que el pop no debe ejecutarse sobre una pila vacía.
            if (Navigation.ModalStack.Contains(this))
                await Navigation.PopModalAsync();
            _resultado.TrySetResult((boton, texto));
        }

        async void OnEntrada(object? sender, EventArgs e) => await Cerrar(0);

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Entrada.IsVisible) Entrada.Focus();
        }

        // El botón Atrás del teléfono cierra el modal sin elegir nada.
        protected override bool OnBackButtonPressed()
        {
            _resultado.TrySetResult((-1, null));
            return base.OnBackButtonPressed();
        }
    }
}
