using SQLite;

namespace MotorCore_Garage.Models
{
    // Administrador y Mecánico usan la versión de monitoreo; Cliente usa la versión de cliente.
    public enum Rol { Administrador, Mecanico, Cliente }

    public enum Prioridad { Normal, Prioritaria, Urgente }

    public enum Desgaste { Ok, Proximo, Vencido }

    public enum GrupoAlerta { Vencidas, Proximas, Programadas }

    // ---- Tablas de la base de datos local (SQLite) ----

    [Table("Usuarios")]
    public class Usuario
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Unique, MaxLength(50)] public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public Rol Rol { get; set; } = Rol.Administrador;
        public int ClienteId { get; set; }   // Solo para Rol.Cliente: cliente al que pertenece la cuenta (0 = ninguno)
    }

    [Table("Configuracion")]
    public class Configuracion
    {
        [PrimaryKey] public string Clave { get; set; } = "";
        public string Valor { get; set; } = "";
    }

    [Table("Clientes")]
    public class Cliente
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [MaxLength(120)] public string Nombre { get; set; } = "";
        [MaxLength(20)] public string Telefono { get; set; } = "";
        public int ClienteDesde { get; set; } = DateTime.Today.Year;
    }

    [Table("Vehiculos")]
    public class Vehiculo
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public int ClienteId { get; set; }
        [Unique, MaxLength(15)] public string Placa { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Anio { get; set; }
        [MaxLength(40)] public string Vin { get; set; } = "";
        public int KmActual { get; set; }
        public string Notas { get; set; } = "";

        // Kilometraje registrado en el último servicio de cada componente (se guarda en ComponenteUltimo).
        [Ignore] public Dictionary<string, int> UltimoServicioKm { get; } = new();

        [Ignore] public string Titulo => $"{Marca} {Modelo} {Anio}";
    }

    [Table("ComponenteUltimo")]
    public class ComponenteUltimo
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed(Name = "ux_vehiculo_componente", Order = 1, Unique = true)] public int VehiculoId { get; set; }
        [Indexed(Name = "ux_vehiculo_componente", Order = 2, Unique = true)] public string Componente { get; set; } = "";
        public int KmUltimo { get; set; }
    }

    [Table("ComponentesCatalogo")]
    public class ComponenteDef
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Unique] public string Nombre { get; set; } = "";
        public int IntervaloKm { get; set; }
    }

    [Table("Citas")]
    public class Cita
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int ClienteId { get; set; }
        public int VehiculoId { get; set; }
        public string Servicio { get; set; } = "";
        public Prioridad Prioridad { get; set; } = Prioridad.Normal;
        public string Estado { get; set; } = "Programada";
    }

    [Table("HistorialServicios")]
    public class RegistroServicio
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public int VehiculoId { get; set; }
        public DateTime Fecha { get; set; }
        public int Km { get; set; }
        public string Servicio { get; set; } = "";
        public string Componentes { get; set; } = "";
        public decimal Costo { get; set; }
        public string Mecanico { get; set; } = "";
        public string Observaciones { get; set; } = "";
    }

    [Table("Bitacora")]
    public class EntradaBitacora
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Texto { get; set; } = "";
    }

    // Mantenimiento que el cliente programa para su vehículo (por ejemplo, cambio de aceite cada 5,000 km o 6 meses).
    [Table("Mantenimientos")]
    public class Mantenimiento
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public int VehiculoId { get; set; }
        [MaxLength(80)] public string Nombre { get; set; } = "";
        public int IntervaloKm { get; set; }          // 0 = sin límite por kilómetros
        public int IntervaloMeses { get; set; }       // 0 = sin límite por tiempo
        public int KmUltimo { get; set; }             // Kilometraje de la última vez que se hizo
        public DateTime FechaUltima { get; set; } = DateTime.Today;
        public DateTime UltimoAviso { get; set; } = DateTime.MinValue; // Último día en que se notificó
    }

    // ---- Modelos de presentación (no se guardan) ----

    public record EstadoComponente(string Nombre, int Porcentaje, int KmRestantes, Desgaste Estado);

    public record EstadoMantenimiento(Mantenimiento Mantenimiento, int KmRestantes, DateTime? FechaVence, Desgaste Estado);

    public record Alerta(Vehiculo Vehiculo, Cliente Cliente, EstadoComponente Componente, DateTime FechaEstimada, GrupoAlerta Grupo);
}
