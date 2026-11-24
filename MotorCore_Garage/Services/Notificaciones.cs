using Microsoft.Maui.ApplicationModel;
using MotorCore_Garage.Models;

namespace MotorCore_Garage.Services
{
    // Avisos del sistema para los mantenimientos del cliente. En Android usa el canal de notificaciones nativo.
    public static class Notificaciones
    {
        const string Canal = "mantenimientos";

        // Android 13 o superior exige permiso para mostrar notificaciones.
        public static void PedirPermiso()
        {
#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(33) && Platform.CurrentActivity is { } actividad)
                actividad.RequestPermissions(new[] { "android.permission.POST_NOTIFICATIONS" }, 1001);
#endif
        }

        // Avisa una vez por día de cada mantenimiento próximo o vencido del cliente.
        public static void RevisarMantenimientos(int clienteId)
        {
            foreach (var estado in Taller.PendientesDeCliente(clienteId))
            {
                var m = estado.Mantenimiento;
                if (m.UltimoAviso.Date == DateTime.Today) continue;

                var placa = Taller.VehiculoPorId(m.VehiculoId)?.Placa;
                var titulo = estado.Estado == Desgaste.Vencido ? $"{m.Nombre}: vencido" : $"{m.Nombre}: próximo";
                Enviar(m.Id, titulo, $"{placa} · {Describir(estado)}");

                m.UltimoAviso = DateTime.Today;
                Db.Conexion.Update(m);
            }
        }

        public static string Describir(EstadoMantenimiento e)
        {
            var partes = new List<string>();
            if (e.Mantenimiento.IntervaloKm > 0)
                partes.Add(e.KmRestantes > 0 ? $"faltan {e.KmRestantes:N0} km" : $"pasó {-e.KmRestantes:N0} km");
            if (e.FechaVence is { } fecha)
                partes.Add($"vence el {fecha:dd MMM yyyy}");
            return string.Join(" · ", partes);
        }

        static void Enviar(int id, string titulo, string texto)
        {
#if ANDROID
            var contexto = global::Android.App.Application.Context;
            var manager = (global::Android.App.NotificationManager?)contexto.GetSystemService(global::Android.Content.Context.NotificationService);
            if (manager is null) return;

            var conCanal = OperatingSystem.IsAndroidVersionAtLeast(26);
            if (conCanal)
                manager.CreateNotificationChannel(new global::Android.App.NotificationChannel(Canal, "Mantenimientos", global::Android.App.NotificationImportance.Default));

            var aviso = conCanal
                ? new global::Android.App.Notification.Builder(contexto, Canal)
                : new global::Android.App.Notification.Builder(contexto);
            var notificacion = aviso
                .SetContentTitle(titulo)
                .SetContentText(texto)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true)
                .Build();
            manager.Notify(id, notificacion);
#endif
        }
    }
}
