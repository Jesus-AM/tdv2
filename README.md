# TDV2 · ASP.NET Core 10 + React

Backend ASP.NET con controladores, EF Core 10/Npgsql y PostgreSQL; frontend React, TypeScript y MUI. Abre **tdv2.slnx** y selecciona **TDV2 HTTPS + React** para F5. El retorno Microsoft permanece en **https://localhost:7136/connect**.

La adaptación técnica no acredita todavía el acceso institucional real ni el despliegue Ubuntu. Implementado, probado y pendiente se registran por separado en [ESTADO_MIGRACION.md](ESTADO_MIGRACION.md).

Código: [Jesus-AM/tdv2](https://github.com/Jesus-AM/tdv2), rama `main`. [Archivos compartidos, exclusiones y revisión previa de publicación](docs/REPOSITORIO.md).

Actualización 2026-10-05: `responsable_ur_supervisor`, colaboradores locales para nivel 3, reservas por bloque, autoguardado parcial, SignalR y envío definitivo. Aplicar explícitamente **CollaborativeFormsAndSubmission** antes de usar esta versión. Véanse [captura y migración](docs/migracion/COLABORACION_ENVIO.md) y [configuración de Nexo/Entra](docs/migracion/CONFIGURACION_NEXO_ENTRA.md). Los cambios de fuente de Nexo no publican funciones automáticamente.

## Dónde modificar cada cosa

| Responsabilidad | Ubicación | Equivalente Laravel |
|---|---|---|
| Rutas, entrada HTTP, SignalR y respuestas | `tdv2/Controllers`; atributos `HttpGet/HttpPost/HttpPatch/HttpDelete` | routes + controllers + broadcasting |
| Entidades persistidas | `tdv2/Domain/Entities` | app/Models |
| Reglas de acceso, plantilla, validación y cálculo de avance | `tdv2/Domain` | reglas/modelos de dominio |
| Casos de uso | `tdv2/Services` | services/actions |
| DbContext y mapeos de tablas | `tdv2/Infrastructure/Tdv2DbContext.cs`, `Infrastructure/Configurations` | Eloquent + configuración de relaciones |
| Migraciones y snapshot | `tdv2/Migrations` | database/migrations |
| Microsoft, Nexo y fuentes SII/ILDA | `tdv2/Integrations` | clientes/conectores externos |
| Publicación de catálogos, cola y reservas | `tdv2/Synchronization` | jobs + servicios de sincronización |
| Sesiones, cifrado, autorización y auditoría | `tdv2/Security` | guards/policies/middleware |
| Composición del host y límites HTTP | `tdv2/Hosting`, `tdv2/Web` | providers + middleware |
| Pantallas y componentes | `ClientApp/resources/js/pages`, `Components` | resources/js |
| Opciones / credenciales locales | appsettings / User Secrets | config / .env |
| Pruebas separadas | `tests`, `ClientApp/tests` | tests |

Se reutiliza Infrastructure como carpeta de persistencia; no existe una segunda capa Data. EF se utiliza en formatos, áreas, colaboraciones, identidad local e inventario. El SQL especializado conserva bloqueos de filas, reservas y publicación/auditoría en una misma transacción. Nexo sigue siendo la autoridad de permisos y sus tablas no pertenecen al DbContext. La portada pública existente se conserva; las nueve pantallas siguen en React.

## Configurar y ejecutar

Requisitos: Visual Studio con ASP.NET y JavaScript, .NET SDK 10 y Node/npm compatible con el lockfile. PostgreSQL 18 y Edge son necesarios para la suite nativa, no para compilar.

Todos los comandos de esta guía se ejecutan **desde la raíz del repositorio**:

```powershell
# Cachés y herramientas de esta sesión dentro del destino.
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.artifacts/nuget'
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.artifacts/dotnet-home'
$env:npm_config_cache = Join-Path (Get-Location) '.artifacts/npm-cache'
dotnet restore tdv2.slnx
dotnet tool restore
npm.cmd --prefix ClientApp ci --ignore-scripts
dotnet build tdv2.slnx
```

En Visual Studio: **tdv2 → Administrar secretos de usuario**. Se conserva UserSecretsId `0397954e-63d2-48b6-a581-a586951257cc`. Claves: `Microsoft:TenantId`, `ClientId`, `ClientSecret`, `PublicOrigin`; `ConnectionStrings:Tdv2`, `Nexo`, `Sii`, `Ilda`; `Synchronization:IldaEnabled`. Mantén `Microsoft:PublicOrigin=https://localhost:7136`. User Secrets es JSON local para desarrollo; no se copia al frontend ni al repositorio.

`WebApplication.CreateBuilder` carga appsettings, appsettings del ambiente y User Secrets en Development; entorno y argumentos tienen precedencia. La aplicación no importa DPAPI/Herd ni sobrescribe las ediciones. Las credenciales vigentes de TDV2 apuntan a la base recreada del servidor por decisión del usuario.

F5 inicia Vite y ASP.NET mediante el perfil compartido. Para trabajar con dos terminales:

```powershell
# Terminal 1: React con HMR
npm.cmd --prefix ClientApp run dev

# Terminal 2: ASP.NET en 7136, proxy /__vite/ y callback /connect
dotnet run --project tdv2 --launch-profile https
```

Abre https://localhost:7136. Cambia componentes en ClientApp y controladores/servicios en tdv2. Para ejecutar sin Vite:

```powershell
npm.cmd --prefix ClientApp run build
dotnet run --project tdv2 --launch-profile https-compiled
```

F5 y el arranque web **no migran, sincronizan ni administran PostgreSQL**. El procesador continúa siendo explícito. Guía detallada: [arranque local](docs/ARRANQUE_LOCAL_WINDOWS.md).

## Migraciones EF Core

EF es el único mecanismo vigente; historial `public."__EFMigrationsHistory"`. No usar el antiguo `--migrate` ni aplicar SQL 001–004. Esos archivos se conservan exclusivamente como fixtures en `tests/TDV2.NativeVerification/LegacySchema`.

```powershell
# Inspeccionar y comprobar que el modelo tenga su migración (sin conectar).
dotnet ef migrations list --no-connect --project tdv2
dotnet ef migrations has-pending-model-changes --project tdv2

# Después de editar entidades y mapeos; revisar Up, Down y snapshot.
dotnet ef migrations add NombreDelCambio --project tdv2 --output-dir Migrations
dotnet ef migrations script --idempotent --project tdv2 --output .artifacts/tdv2-migrations.sql

# Aplicar explícitamente ConnectionStrings:Tdv2 de User Secrets.
dotnet ef database update --project tdv2 -- --environment Development
```

Equivalencias: `make:migration` → `dotnet ef migrations add`; `migrate` → `dotnet ef database update`; `migrate:status` → `dotnet ef migrations list`. Confirmar migración y snapshot junto con el cambio de modelo. Ejecutar siempre con **un solo operador**, sin actualizaciones concurrentes. La inicial exige una base vacía; las siguientes utilizan el historial EF existente. Revisar SQL y recuperación antes de aplicar sobre datos.

Antes y después de aplicar al destino autorizado, este verificador consulta sólo metadatos y contadores, en una transacción de lectura. Sustituir `SERVIDOR_TDV2` y `USUARIO_TDV2` por los valores del destino autorizado en User Secrets; la documentación pública omite esos identificadores:

```powershell
dotnet run --project tests/TDV2.NativeVerification -- --database-check --environment Development --target-host=SERVIDOR_TDV2 --target-port=5432 --target-database=tdv2_db --target-user=USUARIO_TDV2
```

Comprueba conexión/destino, TLS, permisos, tablas, historial, contadores y pausa. Si encuentra objetos ajenos se conservan y el resultado no es válido. La primera migración también rechaza una base ocupada sin historial EF. No usar `EnsureCreated`, `Migrate` ni DDL en Program.cs.

Migraciones iniciales: `InitialTdv2` crea las 14 tablas y `InitializePausedSynchronization` inserta únicamente la configuración pausada. Las pruebas verifican su equivalencia con el esquema anterior. Las dos unicidades nullable se expresan como índices UNIQUE; la relación de ejecución activa tiene un índice adicional.

Ambas migraciones están aplicadas en el servidor autorizado; repetir el comando no cambió esquema, historial ni contadores. [Evidencia de la aplicación](docs/migracion/evidencia-ef-servidor-verificacion.json) y [repetición](docs/migracion/evidencia-ef-servidor-repeticion.json).

La nueva `20261005135417_CollaborativeFormsAndSubmission` está pendiente de aplicación por el operador. Se verificó sobre datos sintéticos, sin convertir prioridades históricas ni enviar formatos institucionales. [Comandos de actualización y cambios de esquema](docs/migracion/COLABORACION_ENVIO.md#actualizar-desde-la-raíz).

Referencia del framework: [migraciones EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) y [proveedor Npgsql](https://www.npgsql.org/efcore/).

El preparador `scripts/Local-Tdv2.ps1 Prepare` conserva marcador, DPAPI y validación de clúster, y usa ahora `dotnet ef database update` sólo en su base aislada. Si esa base conserva el esquema SQL anterior, se detiene para revisión: no lo elimina ni lo adopta automáticamente. `Prepare-Transition.ps1` conserva restauración/diagnóstico de una copia autorizada; su antigua conversión SQL `-Apply` está retirada. La conversión de datos Laravel requiere una adopción EF revisada sobre esa copia.

## Probar

```powershell
dotnet build tdv2.slnx
dotnet run --project tests/TDV2.Verification --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms

# Con F5 ya iniciado: páginas anónimas, HTTPS y WebSocket HMR.
node ClientApp/tests/browser/visual-studio-startup.mjs
```

Ejecutar las suites .NET secuencialmente y con la depuración detenida para no bloquear DLL en Windows. Los verificadores .NET son ejecutables con salida no cero al fallar; no son suites descubiertas por `dotnet test`. La suite nativa crea un clúster desechable bajo .artifacts, verifica su identidad y lo detiene al terminar; las fuentes externas son sintéticas. Admite `-PostgresBin` y `-SkipBrowser`.

Los antiguos Test-UserSecrets/Test-Development corresponden al entorno aislado anterior y no validan el destino remoto vigente. La evidencia distingue memoria, PostgreSQL real, navegador y servicios institucionales.

## Publicar

Detén la depuración y el servidor Vite antes de publicar: en Windows, `npm ci` necesita reemplazar los archivos de node_modules.

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-maintenance
node ClientApp/tests/browser/publish-flow.mjs
```

El publish restaura/compila React e incluye wwwroot; producción ejecuta `dotnet tdv2.dll` sin Node/Vite. No incluye credenciales de desarrollo. Preparar configuración externa, llaves persistentes, HTTPS y aplicar las migraciones en una ventana explícita. [Publicación y operación Ubuntu](docs/PUBLICACION.md).

Pendientes institucionales: Microsoft/Graph reales, contratos y revocaciones de Nexo, conectividad SII/ILDA, eventual conversión autorizada de datos históricos, aceptación visual y operación Ubuntu. La sincronización automática permanece desactivada.
