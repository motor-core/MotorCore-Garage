# MotorCore_Garage.Core

Librería de lógica de MotorCore Garage. No tiene pantallas ni acceso a la base de datos: la app MAUI la referencia y las pruebas de `MotorCore_Garage.Tests` la prueban.

Sigue la regla de no depender de MAUI ni de `DateTime.Today`: las reglas reciben la fecha como parámetro para que las pruebas sean repetibles.

## Contenido

| Carpeta / archivo | Qué contiene |
|---|---|
| `Models/Modelos.cs` | Entidades de la base (`Usuario`, `Cliente`, `Vehiculo`, `Cita`, `Mantenimiento`, etc.) y modelos de presentación (`EstadoComponente`, `EstadoMantenimiento`, `Alerta`). |
| `Services/Seguridad.cs` | Hash de contraseñas con PBKDF2-SHA256 y sal aleatoria (`Generar` y `Verificar`). |
| `Logica/ReglasMantenimiento.cs` | Estado de un mantenimiento (al día, próximo o vencido) por kilómetros, por meses o por ambos. Lo que llegue primero manda. |

## Reglas de mantenimiento

- **Vencido:** los kilómetros restantes son cero o menos, o la fecha de vencimiento ya pasó.
- **Próximo:** faltan 1,000 km o menos, o la fecha vence en 30 días o menos.
- **Al día:** cualquier otro caso.
- Un mantenimiento sin intervalo en km no usa los kilómetros para decidir.

Las constantes `UmbralProximo` (1,000 km) y `DiasProximo` (30 días) están en `ReglasMantenimiento`.

## Uso

La app llama la regla pasando la fecha de hoy:

```csharp
var estado = ReglasMantenimiento.EstadoDe(mantenimiento, vehiculo, DateTime.Today);
```

## Dependencias

- `sqlite-net-pcl`: solo por los atributos de tabla de `Modelos.cs`. La librería no abre conexiones.

## Pruebas

Las pruebas están en `MotorCore_Garage.Tests`. Para correrlas desde la raíz del repositorio:

```bash
dotnet test MotorCore_Garage.Tests/MotorCore_Garage.Tests.csproj
```
