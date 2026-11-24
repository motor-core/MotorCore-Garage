using Xunit;
using MotorCore_Garage.Services;

namespace MotorCore_Garage.Tests
{
    public class SeguridadTests
    {
        [Fact]
        public void La_contraseña_no_se_guarda_en_texto_plano()
        {
            var almacenado = Seguridad.Generar("clave-secreta");

            Assert.DoesNotContain("clave-secreta", almacenado);
        }

        [Fact]
        public void Verificar_acepta_la_clave_correcta()
        {
            var almacenado = Seguridad.Generar("clave-secreta");

            Assert.True(Seguridad.Verificar("clave-secreta", almacenado));
        }

        [Fact]
        public void Verificar_rechaza_otra_clave()
        {
            var almacenado = Seguridad.Generar("clave-secreta");

            Assert.False(Seguridad.Verificar("otra-clave", almacenado));
        }

        [Fact]
        public void Cada_hash_usa_una_sal_distinta()
        {
            var primero = Seguridad.Generar("misma-clave");
            var segundo = Seguridad.Generar("misma-clave");

            Assert.NotEqual(primero, segundo);
        }

        [Theory]
        [InlineData("")]
        [InlineData("sin-separador")]
        public void Verificar_rechaza_un_valor_mal_formado(string almacenado)
        {
            Assert.False(Seguridad.Verificar("cualquiera", almacenado));
        }
    }
}
