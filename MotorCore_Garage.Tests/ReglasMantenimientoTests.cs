using Xunit;
using MotorCore_Garage.Logica;
using MotorCore_Garage.Models;

namespace MotorCore_Garage.Tests
{
    public class ReglasMantenimientoTests
    {
        static readonly DateTime Hoy = new(2026, 11, 24);

        static Vehiculo Auto(int kmActual) => new() { KmActual = kmActual };

        [Fact]
        public void Aceite_al_dia_por_km()
        {
            var m = new Mantenimiento { IntervaloKm = 5000, KmUltimo = 0, FechaUltima = Hoy };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(2000), Hoy);

            Assert.Equal(Desgaste.Ok, estado.Estado);
            Assert.Equal(3000, estado.KmRestantes);
        }

        [Fact]
        public void Aceite_proximo_cuando_faltan_menos_de_mil_km()
        {
            var m = new Mantenimiento { IntervaloKm = 5000, KmUltimo = 0, FechaUltima = Hoy };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(4500), Hoy);

            Assert.Equal(Desgaste.Proximo, estado.Estado);
        }

        [Fact]
        public void Aceite_vencido_al_pasar_el_intervalo()
        {
            var m = new Mantenimiento { IntervaloKm = 5000, KmUltimo = 0, FechaUltima = Hoy };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(5200), Hoy);

            Assert.Equal(Desgaste.Vencido, estado.Estado);
            Assert.Equal(-200, estado.KmRestantes);
        }

        [Fact]
        public void Vence_por_meses_aunque_no_se_hayan_recorrido_los_km()
        {
            var m = new Mantenimiento { IntervaloMeses = 6, FechaUltima = Hoy.AddMonths(-7) };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(100), Hoy);

            Assert.Equal(Desgaste.Vencido, estado.Estado);
        }

        [Fact]
        public void Proximo_cuando_la_fecha_vence_en_menos_de_30_dias()
        {
            var m = new Mantenimiento { IntervaloMeses = 6, FechaUltima = Hoy.AddMonths(-6).AddDays(10) };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(100), Hoy);

            Assert.Equal(Desgaste.Proximo, estado.Estado);
        }

        [Fact]
        public void Lo_que_llegue_primero_manda()
        {
            // Por km todavía está al día, pero por tiempo ya venció.
            var m = new Mantenimiento { IntervaloKm = 50000, IntervaloMeses = 6, KmUltimo = 0, FechaUltima = Hoy.AddMonths(-7) };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(1000), Hoy);

            Assert.Equal(Desgaste.Vencido, estado.Estado);
        }

        [Fact]
        public void Sin_intervalo_en_km_no_cuenta_los_km()
        {
            var m = new Mantenimiento { IntervaloKm = 0, IntervaloMeses = 12, FechaUltima = Hoy };

            var estado = ReglasMantenimiento.EstadoDe(m, Auto(900000), Hoy);

            Assert.Equal(Desgaste.Ok, estado.Estado);
            Assert.Equal(0, estado.KmRestantes);
        }
    }
}
