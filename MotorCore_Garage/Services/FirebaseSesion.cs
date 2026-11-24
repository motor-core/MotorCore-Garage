using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace MotorCore_Garage.Services
{
    // Cuentas de monitoreo en Firebase: el usuario se convierte en correo interno (usuario@motorcore.app)
    // y su rol vive en Firestore, en usuarios/{uid}. Los clientes no usan Firebase.
    public static class FirebaseSesion
    {
        const string Dominio = "motorcore.app";

        public static string CorreoDe(string usuario) => $"{usuario.Trim().ToLowerInvariant()}@{Dominio}";

        // Devuelve true si Firebase aceptó el acceso. Sin conexión o con datos incorrectos devuelve false.
        public static async Task<bool> EntrarAsync(string usuario, string clave)
        {
            try
            {
                await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(CorreoDe(usuario), clave, false);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task SalirAsync()
        {
            try { await CrossFirebaseAuth.Current.SignOutAsync(); }
            catch (Exception) { /* sin conexión: la sesión local se cierra igual */ }
        }

        // Rol del usuario firmado (admin o mecanico) según Firestore. Null si la cuenta no tiene rol asignado.
        public static async Task<string?> RolAsync()
        {
            var uid = CrossFirebaseAuth.Current.CurrentUser?.Uid;
            if (uid is null) return null;
            try
            {
                var doc = await CrossFirebaseFirestore.Current
                    .GetDocument($"usuarios/{uid}")
                    .GetDocumentSnapshotAsync<UsuarioFirebase>(Source.Default);
                return doc.Data?.Rol;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Crea la cuenta de un mecánico en Firebase y su rol en Firestore.
        // Firebase inicia sesión con la cuenta nueva, así que se vuelve a entrar como el administrador.
        public static async Task<bool> CrearMecanicoAsync(string usuario, string clave, string adminUsuario, string adminClave)
        {
            try
            {
                var auth = CrossFirebaseAuth.Current;
                var nuevo = await auth.CreateUserAsync(CorreoDe(usuario), clave);
                var uid = nuevo.Uid;

                await auth.SignOutAsync();
                if (!await EntrarAsync(adminUsuario, adminClave)) return false;

                await CrossFirebaseFirestore.Current
                    .GetDocument($"usuarios/{uid}")
                    .SetDataAsync(new Dictionary<object, object> { ["usuario"] = usuario.Trim().ToLowerInvariant(), ["rol"] = "mecanico" });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // Documento de usuarios/{uid} en Firestore.
    public class UsuarioFirebase
    {
        public string Usuario { get; set; } = "";
        public string Rol { get; set; } = "";
    }
}
