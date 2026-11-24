using System.Security.Cryptography;

namespace MotorCore_Garage.Services
{
    // Contraseñas guardadas como PBKDF2-SHA256 con sal aleatoria: "sal:hash" en Base64.
    public static class Seguridad
    {
        const int Iteraciones = 100_000;
        const int TamanoHash = 32;

        public static string Generar(string clave)
        {
            var sal = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, TamanoHash);
            return $"{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";
        }

        public static bool Verificar(string clave, string almacenado)
        {
            var partes = almacenado.Split(':');
            if (partes.Length != 2) return false;
            var sal = Convert.FromBase64String(partes[0]);
            var esperado = Convert.FromBase64String(partes[1]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
    }
}
