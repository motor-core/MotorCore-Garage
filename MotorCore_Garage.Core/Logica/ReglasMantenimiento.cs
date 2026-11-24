using MotorCore_Garage.Models;

namespace MotorCore_Garage.Logica
{
    // Reglas de los mantenimientos del cliente. Sin acceso a la base ni a la pantalla, para poder probarlas.
    public static class ReglasMantenimiento
    {
        public const int UmbralProximo = 1000;   // Km restantes para marcar "próximo"
        public const int DiasProximo = 30;       // Días antes de la fecha para marcar "próximo"

        // Vence por kilómetros (si tiene intervalo en km) o por tiempo (si tiene meses). Lo que llegue primero manda.
        public static EstadoMantenimiento EstadoDe(Mantenimiento m, Vehiculo v, DateTime hoy)
        {
            int? kmRestantes = m.IntervaloKm > 0 ? m.KmUltimo + m.IntervaloKm - v.KmActual : null;
            DateTime? fechaVence = m.IntervaloMeses > 0 ? m.FechaUltima.AddMonths(m.IntervaloMeses) : null;

            var vencido = kmRestantes is <= 0 || fechaVence is { } fv && fv.Date <= hoy.Date;
            var proximo = kmRestantes is { } kr && kr <= UmbralProximo || fechaVence is { } fp && fp.Date <= hoy.Date.AddDays(DiasProximo);

            var estado = vencido ? Desgaste.Vencido : proximo ? Desgaste.Proximo : Desgaste.Ok;
            return new EstadoMantenimiento(m, kmRestantes ?? 0, fechaVence, estado);
        }
    }
}
