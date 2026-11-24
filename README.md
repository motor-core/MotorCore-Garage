# MotorCore Garage

Sistema de gestión de mantenimiento automotriz para talleres. Lleva el control de los clientes, sus vehículos y los servicios realizados, y calcula cuándo toca cada mantenimiento según el kilometraje.

La app funciona **sin conexión**: todos los datos se guardan en una base de datos SQLite dentro del teléfono.

> **Estado del proyecto:** primera versión funcional en modo local. Incluye registro inicial, inicio de sesión, alta de clientes, vehículos y servicios, agenda y alertas de mantenimiento. Ver [Estado actual y hoja de ruta](#estado-actual-y-hoja-de-ruta).

---

## Tabla de contenidos

- [Funcionalidades](#funcionalidades)
- [Tecnologías](#tecnologías)
- [Requisitos](#requisitos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Primer uso](#primer-uso)
- [Base de datos](#base-de-datos)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Cálculo de desgaste y alertas](#cálculo-de-desgaste-y-alertas)
- [Sistema de diseño](#sistema-de-diseño)
- [Estado actual y hoja de ruta](#estado-actual-y-hoja-de-ruta)
- [Contribuir](#contribuir)
- [Licencia](#licencia)

---

## Funcionalidades

La navegación principal está en la barra de pestañas inferior.

### Acceso
- **Registro inicial:** en el primer uso se crea el taller (nombre) y la cuenta de administrador (usuario y contraseña de al menos 6 caracteres). Entra directamente a la app.
- **Inicio de sesión:** usuario y contraseña. Las contraseñas se guardan con PBKDF2-SHA256 y sal, nunca en texto plano.

### Inicio
- Saludo, fecha y resumen del día: citas de hoy, vehículos en taller, alertas urgentes y ocupación de bahías.
- Citas del día y las alertas de mantenimiento más importantes.
- Botón **+** con acciones rápidas: nuevo cliente, nuevo vehículo, registrar servicio, nueva cita, recepción de vehículo y registrar kilometraje.

### Clientes y vehículos
- **Nuevo cliente:** nombre y teléfono (10 dígitos). Al guardar, ofrece registrar de inmediato un vehículo de ese cliente.
- **Nuevo vehículo:** cliente propietario, placa (única), marca, modelo, año, kilometraje actual y VIN opcional.
- **Vehículos:** listado con filtro por estado de desgaste y búsqueda por placa o modelo.
- **Expediente del vehículo:** estado de cada componente, historial de servicios, kilometraje en el tiempo, notas y acceso para registrar kilometraje o servicios.
- **Clientes:** búsqueda por nombre, teléfono o placa, y detalle con sus vehículos.

### Servicios y kilometraje
- **Registrar servicio:** vehículo, fecha, kilometraje, trabajo realizado, componentes atendidos, costo, mecánico y observaciones. Los componentes marcados reinician su contador de desgaste.
- **Registrar kilometraje:** muestra cómo cambiarán las alertas antes de guardar. El kilometraje no puede bajar.

### Agenda y recepción
- Agenda semanal con citas por horario y por bahía.
- **Nueva cita:** cliente, vehículo, tipo de servicio, prioridad y horario disponible.
- **Recepción:** cambia el estado de la cita (recibido, en proceso, etc.).

### Alertas y reportes
- Alertas agrupadas en vencidas, próximas y programadas (cuando ya hay una cita para ese vehículo).
- **Más:** equipo del taller (alta y baja de cuentas de mecánico), configuración de bahías, intervalos del catálogo de componentes, respaldo en JSON y cierre de sesión. Los reportes son para administradores.

---

## Tecnologías

| Área | Tecnología |
|---|---|
| Framework | [.NET MAUI](https://dotnet.microsoft.com/apps/maui) sobre .NET 10 |
| Lenguaje | C# (nullable habilitado, implicit usings) |
| UI | XAML con compilación en tiempo de build (`MauiXamlInflator=SourceGen`) |
| Navegación | Shell (`TabBar` + rutas registradas en `AppShell.xaml.cs`) |
| Base de datos | SQLite local (sin conexión) y Firebase (Auth y Firestore) con [sqlite-net-pcl](https://github.com/praeclarum/sqlite-net) y `SQLitePCLRaw.bundle_e_sqlite3` |
| Seguridad | PBKDF2-SHA256 para contraseñas (`System.Security.Cryptography`) |
| Tipografía | Open Sans (Regular y Semibold) |
| Plataformas | Android (API 23+), iOS y Mac Catalyst (15.0+), Windows (10.0.17763+) |

---

## Requisitos

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (probado con 10.0.401).
- Carga de trabajo de MAUI:
  ```bash
  dotnet workload install maui
  ```
  En Linux solo se puede compilar el target de Android. Para iOS / Mac Catalyst se requiere macOS con Xcode, y para Windows, Windows con el SDK de Windows 10/11.
- **Android:** Android SDK (instalado por Visual Studio o por `dotnet workload`) y un emulador o dispositivo con depuración USB.
- **Visual Studio 2022** con la carga de trabajo *Desarrollo de .NET Multiplataforma con la interfaz de usuario de la aplicación* (.NET MAUI), o **Visual Studio Code** con la extensión *C# Dev Kit*.

---

## Instalación y ejecución

1. Clona el repositorio:
   ```bash
   git clone https://github.com/OscarGermanDiaz/MotorCore-Garage.git
   cd MotorCore-Garage
   ```

2. Restaura las dependencias:
   ```bash
   dotnet restore MotorCore_Garage.slnx
   ```

3. Compila y ejecuta en Android:
   ```bash
   dotnet build MotorCore_Garage/MotorCore_Garage.csproj -f net10.0-android
   dotnet build MotorCore_Garage/MotorCore_Garage.csproj -f net10.0-android -t:Run
   ```

   En Windows también puedes ejecutar:
   ```bash
   dotnet build MotorCore_Garage/MotorCore_Garage.csproj -f net10.0-windows10.0.19041.0 -t:Run
   ```

4. Alternativamente, abre `MotorCore_Garage.slnx` en Visual Studio, selecciona el dispositivo de destino y presiona **F5**.

En Windows el `.csproj` incluye los targets de Windows; en macOS, los de iOS y Mac Catalyst; en Linux, solo Android. Para forzar un target usa `-f <tfm>`.

---

## Primer uso

1. Al abrir la app por primera vez aparece **Registra tu taller**. Escribe el nombre del taller y crea el usuario administrador.
2. La app entra directamente al inicio con esa sesión.
3. Después de cerrar sesión (*Más → Cerrar sesión*), el acceso será con **Iniciar sesión**.
4. Para empezar a trabajar: registra un **cliente**, luego su **vehículo**, y después un **servicio** para cada vehículo.

Los datos quedan en el teléfono. Desinstalar la app o borrar sus datos elimina la base de datos.

---

## Base de datos

- **Archivo:** `motorcore.db3`, en el almacenamiento privado de la app (`FileSystem.AppDataDirectory`).
- **Creación:** las tablas se crean automáticamente en `MotorCore_Garage/Services/Db.cs` la primera vez que se usa la base de datos.
- **Esquema documentado:** [`schema/MotorCodeGarageDB.sql`](schema/MotorCodeGarageDB.sql). Es el mismo esquema que crea la app; puede ejecutarse con `sqlite3 motorcore.db3 < schema/MotorCodeGarageDB.sql` para una base nueva.
- **Acceso desde código:** la clase `Taller` (`Services/Taller.cs`) carga las tablas en memoria al arrancar y escribe cada cambio en la base de datos. Las páginas no hablan con SQLite directamente.

| Tabla | Contenido |
|---|---|
| `Usuarios` | Cuentas de acceso (usuario, hash de contraseña, rol) |
| `Configuracion` | Nombre del taller y número de bahías |
| `Clientes` | Nombre y teléfono de cada cliente |
| `Vehiculos` | Datos del vehículo, propietario y kilometraje actual |
| `ComponenteUltimo` | Kilometraje del último servicio por componente y vehículo |
| `ComponentesCatalogo` | Componentes y su intervalo de servicio en km |
| `Citas` | Agenda de servicios |
| `HistorialServicios` | Servicios realizados |
| `Bitacora` | Registro de actividad |

Para reiniciar la base de datos durante el desarrollo, desinstala la app del emulador o borra sus datos.

---

## Estructura del proyecto

```
MotorCore-Garage/
├── .gitignore                       Archivos generados e IDE excluidos de git
├── MotorCore_Garage.slnx            Solución
├── README.md
├── schema/
│   └── MotorCodeGarageDB.sql        Esquema SQLite de la base de datos local
├── .vscode/settings.json            Configuración de VS Code
└── MotorCore_Garage/
    ├── App.xaml / App.xaml.cs       Punto de entrada (arranca con Splash)
    ├── AppShell.xaml / .cs          Shell: pestañas y registro de rutas
    ├── MauiProgram.cs               Configuración de la app y fuentes
    ├── MotorCore_Garage.csproj      Proyecto MAUI (targets, versiones, paquetes)
    ├── Models/
    │   └── Modelos.cs               Entidades de la base de datos y modelos de presentación
    ├── Services/
    │   ├── Db.cs                    Conexión SQLite y creación de tablas
    │   ├── Seguridad.cs             Hash y verificación de contraseñas
    │   ├── Taller.cs                Lógica de negocio y acceso a datos
    │   └── Ui.cs                    Chips, colores de desgaste y avisos
    ├── Pages/
    │   ├── SplashPage               Arranque; decide entre registro e inicio de sesión
    │   ├── RegistroPage             Registro inicial del taller
    │   ├── LoginPage                Inicio de sesión
    │   ├── InicioPage               Resumen del día y acciones rápidas
    │   ├── VehiculosPage            Listado de vehículos
    │   ├── ExpedientePage           Detalle y estado de un vehículo
    │   ├── ClientesPage             Listado y búsqueda de clientes
    │   ├── ClienteDetallePage       Detalle de un cliente
    │   ├── NuevoClientePage         Alta de cliente
    │   ├── NuevoVehiculoPage        Alta de vehículo
    │   ├── RegistrarServicioPage    Registro de servicio realizado
    │   ├── RegistrarKmPage          Registro de kilometraje
    │   ├── AgendaPage               Agenda semanal
    │   ├── NuevaCitaPage            Programar cita
    │   ├── RecepcionPage            Recepción de vehículo y cambio de estado
    │   ├── AlertasPage              Alertas de mantenimiento
    │   ├── ReportesPage             Reportes (solo administrador)
    │   ├── MasPage                  Configuración, bitácora y cierre de sesión
    │   └── Vistas.cs                Constructores de tarjetas reutilizables
    ├── Resources/
    │   ├── Styles/Colors.xaml       Paleta de colores
    │   ├── Styles/Styles.xaml       Estilos implícitos por tipo de control (modo claro/oscuro)
    │   ├── Styles/Componentes.xaml  Estilos con nombre (tipografía, tarjetas, botones, páginas)
    │   ├── Fonts/                   Open Sans
    │   └── AppIcon/, Splash/, Images/, Raw/
    └── Platforms/                   Código específico: Android, iOS, MacCatalyst, Windows
```

### Convenciones

- **Entidades** en `Models/Modelos.cs` con atributos de sqlite-net; **páginas** en `Pages/` con su par `.xaml` / `.xaml.cs`.
- Toda escritura pasa por `Taller` (que guarda en SQLite y actualiza la memoria); las páginas no usan `Db` directamente.
- Las tarjetas que se repiten se construyen con métodos estáticos en `Vistas.cs`.
- Los estilos se referencian por nombre (`Style="{StaticResource Titulo}"`); no usar colores ni tamaños fijos en las páginas.
- Las pantallas se navegan con `Shell.Current.GoToAsync`; los parámetros se pasan con `[QueryProperty]`.

---

## Cálculo de desgaste y alertas

Cada componente del catálogo tiene un intervalo en km (por ejemplo, aceite cada 5 000 km). Para cada vehículo se guarda el kilometraje del último servicio de cada componente.

- **Usado** = kilometraje actual − kilometraje del último servicio del componente.
- **Restante** = intervalo − usado.
- **Estado:** *Buen estado* si el restante es ≥ 1 000 km; *Próximo* si queda menos de 1 000 km; *Vencido* si ya se pasó el intervalo.
- Al **registrar un vehículo** se asume que sus componentes están revisados en su kilometraje actual. Al **registrar un servicio**, los componentes atendidos reinician su contador en el kilometraje de ese servicio.
- **Alertas:** se generan por cada componente *Próximo* o *Vencido*. La fecha estimada usa un ritmo de 50 km diarios.

---

## Sistema de diseño

### Paleta

| Token | Valor | Uso |
|---|---|---|
| `Primary` | `#E8701A` | Color principal, botones y pestañas |
| `Graphite` | gris oscuro | Cabeceras de página |
| `WearOk` / `WearSoon` / `WearDue` | verde / ámbar / rojo | Estado de desgaste: buen estado, próximo, vencido |
| `Ink` / `InkMuted` | texto principal / secundario | Tipografía |

Los estilos respetan el modo claro y oscuro del sistema mediante `AppThemeBinding`.

### Estilos principales (`Componentes.xaml`)

| Clave | Uso |
|---|---|
| `Hero`, `HeroTitulo`, `HeroSubtitulo` | Cabecera de cada pestaña (monitoreo y cliente) |
| `BarraVolver`, `CabeceraTitulo` | Cabecera de pantallas secundarias, con botón ‹ |
| `Display`, `Titulo`, `Seccion` | Encabezados dentro del contenido |
| `CuerpoStrong`, `Cuerpo`, `Pie`, `Etiqueta` | Texto de cuerpo, secundario y etiquetas |
| `Tarjeta`, `Chip`, `Campo` | Contenedores y campos de formulario |
| `EstadoVacio` | Mensaje cuando una lista no tiene elementos |
| `BotonPrimario`, `BotonSecundario` | Botones de acción |
| `Pagina` | Fondo y barra de navegación de cada página |

---

## Estado actual y hoja de ruta

### Implementado
- [x] Registro inicial del taller y cuenta de administrador.
- [x] Inicio de sesión con contraseñas protegidas (PBKDF2).
- [x] Base de datos SQLite local con las tablas descritas arriba.
- [x] Alta de clientes, vehículos y servicios; registro de kilometraje.
- [x] Cálculo de desgaste por componente y alertas.
- [x] Agenda, nueva cita y recepción de vehículos.
- [x] Configuración de bahías e intervalos, bitácora y cierre de sesión.
- [x] Sistema de estilos con soporte de modo claro y oscuro.
- [x] Compilación para Android verificada.
- [x] Alta y eliminación de cuentas de mecánico desde *Más → Equipo* (solo administrador).
- [x] Cabecera unificada en todas las pantallas y estados vacíos en las listas.

### Pendiente
- [ ] Probar el flujo completo en dispositivo (registro, login, cliente, vehículo, servicio).
- [ ] Respaldo y restauración de la base de datos (hoy el respaldo es un JSON de lectura).
- [ ] Mantener la cabecera visible cuando se abre el teclado en formularios.
- [ ] Pestañas de estado y órdenes de trabajo.
- [ ] Reportes con datos reales.
- [ ] Sincronización con un servidor (opcional, para varios equipos).
- [ ] Proyecto de pruebas unitarias para `Taller` y el cálculo de desgaste.
- [ ] Capturas de pantalla en `docs/img/`.

---

## Contribuir

1. Crea una rama desde `main`: `git checkout -b feature/nombre-del-cambio`.
2. Haz tus cambios y verifica que el proyecto compila (`dotnet build -f net10.0-android`).
3. Si cambias una entidad, actualiza también `schema/MotorCodeGarageDB.sql`.
4. Confirma con mensajes de commit claros y abre un Pull Request hacia `main`.

No subas archivos generados (`bin/`, `obj/`), claves de firma (`*.keystore`, `*.jks`), bases de datos locales (`*.db3`) ni credenciales; el `.gitignore` ya excluye los generados.

---

## Licencia

Pendiente de definir por el autor del proyecto.
