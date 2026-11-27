using MotorCore_Garage.Models;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Pages
{
    [QueryProperty(nameof(ClienteId), "ClienteId")]
    public partial class ClienteDetallePage : ContentPage
    {
        Cliente? _cliente;

        public ClienteDetallePage()
        {
            InitializeComponent();
        }

        public int ClienteId
        {
            set => Cargar(Taller.ClientePorId(value));
        }

        void Cargar(Cliente? cliente)
        {
            _cliente = cliente;
            if (cliente is null) return;

            var partes = cliente.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Iniciales.Text = string.Concat(partes.Take(2).Select(p => p[0])).ToUpperInvariant();
            Nombre.Text = cliente.Nombre;
            Detalle.Text = $"Tel. {cliente.Telefono} · Cliente desde {cliente.ClienteDesde}";
            BotonAcceso.IsVisible = !Taller.TieneAcceso(cliente.Id);

            Vehiculos.Children.Clear();
            foreach (var v in Taller.VehiculosDeCliente(cliente.Id))
            {
                var id = v.Id;
                Vehiculos.Children.Add(Vistas.VehiculoCard(v,
                    () => Shell.Current.GoToAsync("expediente", new Dictionary<string, object> { ["VehiculoId"] = id })));
            }
        }

        async void OnLlamar(object? sender, EventArgs e)
        {
            if (_cliente is not null) await Vistas.Llamar(_cliente);
        }

        async void OnWhatsApp(object? sender, EventArgs e)
        {
            if (_cliente is not null) await Vistas.WhatsApp(_cliente);
        }

        // El administrador crea el usuario y la contraseña con los que el cliente entra a la versión de cliente.
        async void OnCrearAcceso(object? sender, EventArgs e)
        {
            if (_cliente is null) return;

            var usuario = await ModalPage.Pedir(this, "Cuenta del cliente", "Usuario para iniciar sesión", "Siguiente", "Cancelar",
                placeholder: "Ej. jramirez", maxLength: 50);
            if (string.IsNullOrWhiteSpace(usuario)) return;

            var clave = await ModalPage.Pedir(this, "Cuenta del cliente", "Contraseña (mínimo 6 caracteres)", "Crear cuenta", "Cancelar",
                maxLength: 50, esClave: true);
            if (string.IsNullOrWhiteSpace(clave)) return;

            var error = Taller.CrearAccesoCliente(_cliente.Id, usuario, clave);
            if (error is not null)
            {
                await ModalPage.Aviso(this, "No se pudo crear la cuenta", error);
                return;
            }

            BotonAcceso.IsVisible = false;
            await ModalPage.Aviso(this, "Cuenta creada", $"{_cliente.Nombre} ya puede entrar con el usuario «{usuario.Trim()}».");
        }

        async void OnVolver(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
