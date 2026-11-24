# MotorCore_Garage.Tests

Pruebas automáticas de la lógica de MotorCore Garage. Usan [xUnit](https://xunit.net/) y prueban la librería `MotorCore_Garage.Core`, no las pantallas.

## Correr las pruebas

Desde la raíz del repositorio:

```bash
dotnet test MotorCore_Garage.Tests/MotorCore_Garage.Tests.csproj
```

Para ver el nombre de cada prueba:

```bash
dotnet test MotorCore_Garage.Tests/MotorCore_Garage.Tests.csproj --logger "console;verbosity=detailed"
```

## Archivos

| Archivo | Qué prueba |
|---|---|
| `SeguridadTests.cs` | Hash de contraseñas: no se guarda en texto plano, la clave correcta entra, una incorrecta no, la sal cambia en cada hash y los formatos inválidos se rechazan. |
| `ReglasMantenimientoTests.cs` | Estado de los mantenimientos: al día, próximo y vencido por km, por meses y por ambos. |

## Cómo se escriben

- Cada prueba usa una fecha fija (`Hoy = 2026-11-24`) en lugar de `DateTime.Today`, para que el resultado no cambie según el día.
- `[Fact]` es una prueba sola. `[Theory]` con `[InlineData]` corre la misma prueba con varios valores.
- Los nombres de las pruebas describen el caso en español: `Aceite_vencido_al_pasar_el_intervalo`.

## Agregar una prueba

1. Crea o abre el archivo de la regla que quieres probar.
2. Escribe un método con `[Fact]`, prepara el caso y compara con `Assert`.
3. Corre `dotnet test`.

Las pantallas y la base SQLite no tienen pruebas todavía. Probarlas requiere separar primero el acceso a datos.
