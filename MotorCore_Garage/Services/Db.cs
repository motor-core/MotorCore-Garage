using SQLite;

namespace MotorCore_Garage.Services
{
    // Base de datos local SQLite. Vive en el almacenamiento privado de la app del teléfono.
    public static class Db
    {
        static SQLiteConnection? _conexion;

        public static SQLiteConnection Conexion => _conexion ??= Abrir();

        static SQLiteConnection Abrir()
        {
            var ruta = Path.Combine(FileSystem.AppDataDirectory, "motorcore.db3");
            var conexion = new SQLiteConnection(ruta, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
            conexion.CreateTable<Models.Usuario>();
            // Migración: bases creadas antes de las cuentas de cliente no tienen la columna ClienteId.
            if (!conexion.GetTableInfo("Usuarios").Any(c => c.Name == "ClienteId"))
                conexion.Execute("ALTER TABLE Usuarios ADD COLUMN ClienteId INTEGER NOT NULL DEFAULT 0");
            conexion.CreateTable<Models.Configuracion>();
            conexion.CreateTable<Models.Cliente>();
            conexion.CreateTable<Models.Vehiculo>();
            conexion.CreateTable<Models.ComponenteUltimo>();
            conexion.CreateTable<Models.ComponenteDef>();
            conexion.CreateTable<Models.Cita>();
            conexion.CreateTable<Models.RegistroServicio>();
            conexion.CreateTable<Models.EntradaBitacora>();
            conexion.CreateTable<Models.Mantenimiento>();

            // Catálogo de componentes con intervalos por defecto (solo la primera vez).
            if (conexion.Table<Models.ComponenteDef>().Count() == 0)
            {
                conexion.InsertAll(new[]
                {
                    new Models.ComponenteDef { Nombre = "Aceite de motor", IntervaloKm = 5000 },
                    new Models.ComponenteDef { Nombre = "Filtros", IntervaloKm = 10000 },
                    new Models.ComponenteDef { Nombre = "Frenos", IntervaloKm = 30000 },
                    new Models.ComponenteDef { Nombre = "Llantas", IntervaloKm = 50000 },
                    new Models.ComponenteDef { Nombre = "Batería", IntervaloKm = 40000 },
                    new Models.ComponenteDef { Nombre = "Bujías", IntervaloKm = 40000 },
                    new Models.ComponenteDef { Nombre = "Banda de distribución", IntervaloKm = 80000 },
                    new Models.ComponenteDef { Nombre = "Suspensión", IntervaloKm = 60000 }
                });
            }
            return conexion;
        }
    }
}
