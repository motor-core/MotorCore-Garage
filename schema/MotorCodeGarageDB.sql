-- =========================================================================
-- MotorCode Garage - Esquema de la base de datos local (SQLite)
--
-- La app crea estas tablas automáticamente al abrir la base de datos
-- (MotorCore_Garage/Services/Db.cs, con sqlite-net-pcl). Este archivo documenta
-- el mismo esquema y puede ejecutarse en una base nueva:
--     sqlite3 motorcore.db3 < MotorCodeGarageDB.sql
--
-- Ubicación en el teléfono: FileSystem.AppDataDirectory/motorcore.db3
--
-- Convenciones de tipos (sqlite-net):
--   string -> TEXT · int -> INTEGER · DateTime -> TEXT (ISO 8601)
--   enum   -> INTEGER · decimal -> NUMERIC
--
-- Las relaciones (ClienteId, VehiculoId) se validan en la aplicación; no hay
-- claves foráneas declaradas.
-- Diagrama y descripción de cada tabla: docs/BASE_DE_DATOS.md
-- =========================================================================

-- Cuentas de acceso. Rol: 0 = Administrador, 1 = Mecanico, 2 = Cliente.
-- ClienteId solo aplica a Rol = 2: cliente al que pertenece la cuenta (0 = ninguno).
-- PasswordHash guarda "sal:hash" en Base64 (PBKDF2-SHA256, 100 000 iteraciones).
CREATE TABLE IF NOT EXISTS Usuarios (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT    NOT NULL UNIQUE,              -- en minúsculas, máx. 50
    PasswordHash TEXT    NOT NULL,
    Rol          INTEGER NOT NULL DEFAULT 0,
    ClienteId    INTEGER NOT NULL DEFAULT 0            -- Clientes.Id cuando Rol = 2
);

-- Ajustes del taller (clave/valor). Claves usadas:
--   NombreTaller  nombre del taller
--   Bahias        número de bahías (1 a 10)
--   WhatsApp      número del taller para citas por WhatsApp (con lada 52)
CREATE TABLE IF NOT EXISTS Configuracion (
    Clave TEXT PRIMARY KEY,
    Valor TEXT NOT NULL
);

-- Clientes del taller. Teléfono: 10 dígitos.
CREATE TABLE IF NOT EXISTS Clientes (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre       TEXT    NOT NULL,
    Telefono     TEXT    NOT NULL,
    ClienteDesde INTEGER NOT NULL                      -- año de alta
);

-- Vehículos. ClienteId apunta a Clientes.Id.
CREATE TABLE IF NOT EXISTS Vehiculos (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    ClienteId  INTEGER NOT NULL,
    Placa      TEXT    NOT NULL UNIQUE,
    Marca      TEXT    NOT NULL,
    Modelo     TEXT    NOT NULL,
    Anio       INTEGER NOT NULL,
    Vin        TEXT    NOT NULL DEFAULT '',
    KmActual   INTEGER NOT NULL DEFAULT 0,
    Notas      TEXT    NOT NULL DEFAULT ''
);

-- Kilometraje del último servicio de cada componente de cada vehículo.
-- Es el dato que alimenta el cálculo de desgaste (Taller.Estados).
CREATE TABLE IF NOT EXISTS ComponenteUltimo (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    VehiculoId  INTEGER NOT NULL,
    Componente  TEXT    NOT NULL,                     -- coincide con ComponentesCatalogo.Nombre
    KmUltimo    INTEGER NOT NULL,
    UNIQUE (VehiculoId, Componente)
);

-- Catálogo de componentes y su intervalo de servicio en km (editable en "Más").
CREATE TABLE IF NOT EXISTS ComponentesCatalogo (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre      TEXT    NOT NULL UNIQUE,
    IntervaloKm INTEGER NOT NULL
);

-- Citas de la agenda. Prioridad: 0 = Normal, 1 = Prioritaria, 2 = Urgente.
-- ClienteId y VehiculoId apuntan a Clientes.Id y Vehiculos.Id.
CREATE TABLE IF NOT EXISTS Citas (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Fecha      TEXT    NOT NULL,                       -- fecha y hora de la cita
    ClienteId  INTEGER NOT NULL,
    VehiculoId INTEGER NOT NULL,
    Servicio   TEXT    NOT NULL,
    Prioridad  INTEGER NOT NULL DEFAULT 0,
    Estado     TEXT    NOT NULL DEFAULT 'Programada'   -- Programada, Recibido, En proceso, Listo, Entregado
);

-- Servicios realizados. Componentes: lista separada por comas de los componentes atendidos.
CREATE TABLE IF NOT EXISTS HistorialServicios (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    VehiculoId    INTEGER NOT NULL,
    Fecha         TEXT    NOT NULL,
    Km            INTEGER NOT NULL,
    Servicio      TEXT    NOT NULL,
    Componentes   TEXT    NOT NULL DEFAULT '',
    Costo         NUMERIC NOT NULL DEFAULT 0,
    Mecanico      TEXT    NOT NULL DEFAULT '',
    Observaciones TEXT    NOT NULL DEFAULT ''
);

-- Mantenimientos que el cliente programa para su vehículo (por ejemplo, aceite cada 5,000 km o 6 meses).
-- IntervaloKm = 0 o IntervaloMeses = 0 significa que ese criterio no aplica.
CREATE TABLE IF NOT EXISTS Mantenimientos (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    VehiculoId      INTEGER NOT NULL,
    Nombre          TEXT    NOT NULL DEFAULT '',        -- máx. 80
    IntervaloKm     INTEGER NOT NULL DEFAULT 0,
    IntervaloMeses  INTEGER NOT NULL DEFAULT 0,
    KmUltimo        INTEGER NOT NULL DEFAULT 0,         -- kilometraje de la última vez que se hizo
    FechaUltima     TEXT    NOT NULL,                   -- fecha de la última vez que se hizo
    UltimoAviso     TEXT    NOT NULL                    -- último día en que se notificó (DateTime.MinValue si nunca)
);

-- Bitácora de actividad del taller (inicios de sesión, registros, cambios).
CREATE TABLE IF NOT EXISTS Bitacora (
    Id    INTEGER PRIMARY KEY AUTOINCREMENT,
    Fecha TEXT NOT NULL,
    Texto TEXT NOT NULL
);

-- Datos iniciales del catálogo (la app los inserta solo si la tabla está vacía).
INSERT INTO ComponentesCatalogo (Nombre, IntervaloKm) VALUES
    ('Aceite de motor',        5000),
    ('Filtros',               10000),
    ('Frenos',                30000),
    ('Llantas',               50000),
    ('Batería',               40000),
    ('Bujías',                40000),
    ('Banda de distribución', 80000),
    ('Suspensión',            60000);
