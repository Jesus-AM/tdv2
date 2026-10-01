# TDV2 · ASP.NET Core 10 + React

Abrir **tdv2.slnx** en Visual Studio con soporte .NET 10 y JavaScript/TypeScript. La solución contiene el backend `tdv2`, el frontend existente `ClientApp` y una carpeta de pruebas. **Migración institucional en curso: no sustituir Laravel ni desplegar todavía.** Evidencia y límites: [ESTADO_MIGRACION.md](ESTADO_MIGRACION.md).

## Desarrollo con Visual Studio

Requisitos: .NET SDK 10, Node 22.23 o compatible con Vite 8, npm y las cargas de Visual Studio para ASP.NET y JavaScript. PostgreSQL nativo ya está instalado en este equipo; no se instala ni se administra el servicio usado por Laravel.

Una vez, o después de cambiar `package-lock.json`, desde `ClientApp`:

```powershell
npm.cmd ci --ignore-scripts
```

Desde la raíz, para iniciar la base detenida **sin recrearla ni migrarla**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
```

En Visual Studio, seleccionar el perfil de solución **TDV2 HTTPS + React** y el perfil **https** del backend. F5 inicia `ClientApp` con Vite y `tdv2` con Kestrel. Si no aparece el perfil compartido, usar **Configurar proyectos de inicio → Varios proyectos**: ClientApp y tdv2 en Iniciar; los verificadores en Ninguno. Las instrucciones completas están en [ARRANQUE_LOCAL_WINDOWS.md](docs/ARRANQUE_LOCAL_WINDOWS.md).

Abrir **https://localhost:7136**. El retorno Microsoft se conserva en **https://localhost:7136/connect**. Vite escucha sólo en `127.0.0.1:5173`; ASP.NET sirve su HTML, módulos y WebSocket HMR a través de `/__vite/`. El navegador mantiene el mismo origen para React, cookies, CSRF y OAuth. Las rutas privadas se autorizan antes de entregar HTML. Editar pantallas en `ClientApp/resources/js/pages`; Vite refleja los cambios sin publicar.

Alternativa por terminal, con PostgreSQL ya activo:

```powershell
# Terminal 1, en ClientApp
npm.cmd run dev
# Terminal 2, en la raíz
dotnet run --project tdv2 --launch-profile https
```

Para usar React compilado: `npm.cmd run build` en ClientApp, iniciar sólo tdv2 con **https-compiled**. `Local-Tdv2.ps1 Start` también usa este perfil. Mayús+F5 detiene la depuración; `Local-Tdv2.ps1 Stop` detiene únicamente PostgreSQL aislado y conserva sus datos. No ejecutar F5 y otro backend en los mismos puertos.

## Dónde mantener cada responsabilidad

| Ubicación | Responsabilidad |
|---|---|
| `tdv2/Program.cs` | Crear host, registrar servicios, elegir web o comando explícito de sincronización |
| `tdv2/Hosting` | Inyección de dependencias, cookies, CSRF, límites y orden del middleware |
| `tdv2/Controllers` | Rutas, entradas HTTP y respuestas; no contiene SQL |
| `tdv2/Services` | Casos de uso de formatos, colaboradores, autenticación y contextos; límites de transacción |
| `tdv2/Domain` | Alcance por UR, módulos, plantilla, validación y cálculo de avance |
| `tdv2/Infrastructure` | Npgsql, consultas institucionales Nexo y persistencia local |
| `tdv2/Security` | Identidad Microsoft, tokens, sesiones/contextos cifrados y auditoría |
| `tdv2/Synchronization` | Lectores remotos, réplica local, publicación, reservas y recuperación |
| `tdv2/Web` | Autorización por solicitud, errores públicos, shell React y contrato JSON/CSRF |
| `ClientApp/resources` | React, TypeScript, MUI, tema, tipografías, componentes y estilos conservados |
| `database` | SQL explícito; bootstrap y ensayo de transición separados |
| `tests`, `ClientApp/tests` | Dobles y datos sintéticos; nunca se incorporan al ejecutable web |
| `docs/migracion` | Contratos, inventario de Laravel y evidencia diferenciada |

Los servicios de aplicación usan `RequestAccess` scoped para resolver la solicitud vigente. Las reglas de alcance y avance permanecen en Domain, sin HTTP. No convertir esa caché por solicitud en caché de permisos entre solicitudes: Nexo debe poder revocar inmediatamente. Las transacciones pertenecen al caso de uso; los componentes SQL reciben la conexión/transacción cuando el cambio debe confirmarse junto con su auditoría.

## Qué configuración editar

| Archivo o proveedor | Uso |
|---|---|
| `tdv2/appsettings.json` | Opciones generales, logging y límites de sincronización; sin credenciales |
| `tdv2/appsettings.Development.json` | Valores de desarrollo; Vite se habilita en el perfil https |
| **tdv2 → Administrar secretos de usuario** | Microsoft y cadenas Tdv2/Nexo/Sii/Ilda locales ya importadas |
| `tdv2/Properties/launchSettings.json` | Perfiles, ambiente y puertos; no guardar secretos aquí |
| `tdv2.slnLaunch` | Inicio conjunto de backend y frontend desde Visual Studio |
| `ClientApp/vite.config.ts` | Base de módulos y HMR; ningún secreto ni conexión institucional |
| Variables externas del servicio | Credenciales de producción; nombres jerárquicos con `__` |
| `DataProtection:KeyDirectory` | Directorio persistente de llaves del servidor, fuera del paquete publicado |

ASP.NET aplica la precedencia estándar: appsettings → archivo del ambiente → User Secrets sólo en Development → variables de entorno → argumentos. User Secrets es JSON de desarrollo **sin cifrado**. No se imprime ni se entrega al navegador. El lanzador no repone DPAPI ni sobrescribe ediciones. La correspondencia completa con Laravel está en [la guía local](docs/ARRANQUE_LOCAL_WINDOWS.md#equivalencias-de-la-configuración-importada).

DPAPI sólo permanece en la administración del clúster aislado Windows y en la protección de llaves ASP.NET de Windows. El backend no depende del antiguo almacén institucional. `Import`, `Configure` y `MigrateSecrets` son avisos de compatibilidad sin lecturas ni escrituras. `Prepare` mantiene su función explícita de preparar/verificar el esquema local; no usarlo como sustituto de StartDatabase.

## Bases y migraciones

- **tdv2_db:** permanece para Laravel; no se leyó, respaldó, migró ni modificó en esta preparación.
- **tdv2_local_validation:** se conserva su conexión, esquema y datos. Automática apagada; ningún procesador se inicia con F5.
- **Base vacía aislada:** SQL 001–004, explícitamente mediante el mecanismo existente de hashes. Nunca ejecutar el bootstrap sobre una base Laravel.
- **Copia de transición:** [procedimiento y SQL separados](docs/migracion/TRANSICION_DATOS.md). Se probó respaldo/restauración/conversión con datos sintéticos. Falta una copia real autorizada.
- Nexo, SII e ILDA conservan sus bases; no hay DDL remoto. Las pantallas sólo consultan catálogos locales. [Operación explícita del procesador](docs/migracion/SINCRONIZACIONES.md).

## Verificar

```powershell
# Raíz
dotnet build tdv2.slnx
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -TransitionOnly

# ClientApp
npm.cmd run types:check
npm.cmd run test:forms
```

Los verificadores .NET son ejecutables con salida no cero al fallar; no son suites descubiertas por `dotnet test`. La suite nativa crea un clúster nuevo dentro de `.artifacts`, lo valida antes de escribir y lo detiene al terminar. Microsoft/Nexo/SII/ILDA son sintéticos allí. Detener instancias que bloqueen las DLL antes de recompilar.

Con la aplicación detenida y la misma cuenta Windows: `scripts/Test-UserSecrets.ps1` verifica proveedores y conservación de ediciones sin conexiones remotas. `scripts/Test-Development.ps1` inicia el perfil https y Vite, comprueba HTTPS/React/CSRF y modifica temporalmente una pantalla para verificar HMR; restaura sus bytes y detiene sólo sus procesos. Deja PostgreSQL disponible para F5. `-Compiled` comprueba el perfil compilado. Las pruebas por CLI/Edge **no certifican haber operado el depurador de Visual Studio ni un login institucional real**.

## Publicar, sin desplegar

Desde la raíz, con Node/npm disponibles durante la construcción:

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-maintenance
```

El target de publicación ejecuta `npm ci --ignore-scripts` y `npm run build`, renueva únicamente los assets generados e incluye React en `wwwroot`. El paquete se ejecuta con `dotnet tdv2.dll`; **producción no requiere Node ni Vite**. La publicación la controla el csproj del backend para evitar copias duplicadas desde el proyecto JavaScript.

[Configuración y preparación Ubuntu](docs/PUBLICACION.md) explica secretos externos, llaves persistentes, permisos, HTTPS y migraciones explícitas. No se desplegó. La aceptación institucional, la prueba dentro de Visual Studio, la operación Ubuntu y la comparación con una copia real de Laravel siguen pendientes. Consultar el estado para resultados y advertencias de dependencias de esta ejecución.