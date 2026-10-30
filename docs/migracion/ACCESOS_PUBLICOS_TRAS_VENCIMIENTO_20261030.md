# Accesos públicos después del vencimiento · 2026-10-30

## Causa comprobada y corrección

Los dos botones eran enlaces HTML nativos: encuesta → `/competencias-digitales`; registro de procedimientos → `/inicio`. El JavaScript público sólo ajusta la altura del encabezado y controla el juego; no ejecuta el cliente privado de sesión ni un interceptor Axios al pulsar estos enlaces.

La primera decisión que bloqueaba el recorrido era `SessionExpiryMiddleware`: ante un ticket antiguo/vencido o la cookie HttpOnly `tdv2.session-ended`, devolvía una redirección a `/` para esas rutas privadas. El controlador de autenticación ni siquiera recibía el clic. Repetirlo volvía a la portada, sin OAuth ni excepción; esto es coherente con que el registro del servicio no mostrara un error del clic. La política distinguía el acceso explícito a `/connect`, pero los botones no lo utilizaban.

La regresión se reprodujo con el paquete del commit `7cc9bc4`, ASP.NET y PostgreSQL aislados y ambos botones reales. Sin sesión y con sesión válida entraban; ticket antiguo, cookie vencida desde una portada abierta y nuevo clic después del vencimiento volvían a `/`, sin iniciar OAuth. **Diez comprobaciones previas, seis bloqueos esperados y cero errores JavaScript.** [Resultado previo](evidencia/accesos-publicos-20261030/antes.json).

Se cambian únicamente tres atributos `href` de producción, en `tdv2/Views/Home/Index.cshtml`:

| Acceso voluntario | Enlace explícito | Destino final |
|---|---|---|
| Registrar un procedimiento institucional | `/connect?returnUrl=/inicio` | `/inicio` |
| Responder encuesta de competencias digitales | `/connect?returnUrl=/competencias-digitales` | `/competencias-digitales` |
| Ir a mi espacio (cabecera autenticada) | `/connect?returnUrl=/accesos` | `/accesos`, con la selección/resolución vigente |

`Connect` ya reutiliza una sesión válida tras comprobar su identidad local; no reinicia su plazo. Sin sesión válida inicia el flujo existente Microsoft, conserva el retorno local permitido, consume estado/cookie/PKCE y crea una sesión con su propio inicio fiable. La autorización del sitio sigue realizándose en su destino mediante los contratos vigentes, sin combinar permisos ni ampliar acceso.

No se retira la marca de vencimiento desde JavaScript ni se habilita OAuth automático al vencer. Las URLs privadas del historial siguen volviendo a `/`; un clic explícito público pasa por `/connect`, que ya estaba permitido. El callback correcto retira la marca. La fotografía, cabecera, diseño, juego, encuesta, respuestas, históricos, autoguardado, representación y auditoría no cambian.

## Generation: incidencia separada

`CompetenciasController.Generation()` consulta `DigitalDbContext.Control` sólo en el endpoint de Competencias. Su cliente se monta dentro de Competencias y comprueba la generación al recuperar foco y periódicamente; esa consulta no forma parte de los enlaces Razor públicos ni del acceso a Procedimientos.

Se añadió un fallo **sintético** de EF (`InvalidOperationException` con `NpgsqlException`/`TimeoutException` interiores) exclusivamente al proyecto de pruebas. El endpoint responde **503**, no 401; conserva la sesión. Una prueba de navegador provoca esa respuesta mediante el cliente real de Competencias y después entra a Procedimientos desde el botón público sin otro OAuth. El interceptor global sólo termina la sesión ante 401 o un cambio explícito de sesión, no ante este 503.

**La excepción interna del caso local real sigue sin conocerse.** No se atribuye a credenciales, esquema, red o ausencia de control sin evidencia y no se presenta como resuelta mediante el transporte simulado. Para obtenerla de forma saneada desde el PowerShell habitual del operador, dentro del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Diagnose-DigitalGeneration.ps1
```

Con el verificador Release ya compilado se puede añadir `-NoBuild`. La entrada `--digital-generation-readonly` sale antes de inicializar servidor, fixtures, trabajadores, auditoría o migradores. Carga la configuración local normal y consulta únicamente `cd_control` con el mismo `SingleAsync` de generación, forzando `default_transaction_read_only=on`, tiempo de conexión de 8 s y consulta de 20 s. No ejecuta escrituras ni cambia permisos/configuración.

La salida JSON identifica `configuration` o `generation.read`, validez de la fila y, ante error, envoltura, SQLSTATE o clasificación de transporte/timeout. Omite mensajes SQL, parámetros, valores de generación, personas, rutas de archivos privados y secretos. Un fallo de transporte no se clasifica como lectura de configuración ni como caducidad. La consulta se probó con configuración sintética, no con User Secrets institucionales. **Pendiente del operador:** ejecutar ese comando y compartir su salida saneada para precisar la excepción real.

## Paquetes y navegador

Se generaron paquetes Release locales `before` y `after` dentro de `.artifacts/public-entry-20261030/`, sin publicar en un servidor. El código del paquete anterior procede de Git; el posterior incorpora los tres enlaces corregidos. En una copia de compilación se reutilizó la salida Vite de producción ya construida, retirando únicamente allí los pasos de reinstalación/recompilación de Node; el `.csproj` real no cambió. Contratos, fuentes, recursos y bundles se incluyen mediante los ítems normales de publicación.

El verificador carga **la misma DLL publicada por SHA-256**, no una página estática simulada, y utiliza el directorio publicado como raíz de contenido. Los dobles de Microsoft/Nexo y los controles de reloj/tickets/fallo de generación viven exclusivamente en el ejecutable de pruebas. [Identidad del paquete](evidencia/accesos-publicos-20261030/paquete.json).

El navegador recorre cinco estados de cada sitio a 1440, 900 y 390 px: sin sesión; válida sin renovar inicio/vencimiento; ticket antiguo; cookie vencida mientras la portada permanece abierta; regreso automático a `/` seguido de Atrás y clic voluntario. Comprueba contenido privado real, identidad de sesión/plazo, destino, callback, ausencia de OAuth espontáneo y errores JavaScript. Se añaden permiso exclusivo de cada sitio, fallo de generación independiente y Mi espacio con ticket antiguo mientras su enlace ya estaba visible.

La simulación OAuth sigue los saltos locales mediante el controlador real y sustituye únicamente el proveedor externo por su callback sintético. Conserva el limitador real: la prueba espacia esos inicios y reserva tiempo para el callback. Esa espera del fixture no es una medición de rendimiento institucional.

## Archivos y comprobaciones

- Producción: `tdv2/Views/Home/Index.cshtml`, tres enlaces y ningún cambio de lógica de sesión.
- Navegador: `ClientApp/tests/browser/public-entry-native.mjs`.
- Fixture: `tests/TDV2.NativeVerification/NativeApplication.cs`, `DigitalTests.cs`, `DigitalControlFailure.cs`, `DigitalSessionLifetimeTests.cs`.
- Diagnóstico de sólo lectura: `DigitalPopulationReadOnly.cs`, entrada en `NativeTests.cs` y `scripts/Diagnose-DigitalGeneration.ps1`.
- Regresión de enlaces Razor: `tests/TDV2.Verification/PublicAssetsTests.cs`.
- README, ESTADO_MIGRACION y esta guía. Se conserva todo el trabajo previo; no se restaura ningún archivo completo.

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
$env:TDV2_TEST_CASE_PREFIX = 'Sesión dos horas'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_PUBLIC_ENTRY_UI = '1'
# Opcional: TDV2_TEST_PUBLISHED_ROOT apunta al paquete local dentro de .artifacts.
# Si node no está en PATH, definir TDV2_TEST_NODE con su ejecutable existente.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -BrowserOnly
Remove-Item Env:TDV2_TEST_PUBLIC_ENTRY_UI
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
```

Resultados finales:

| Comprobación | Resultado |
|---|---|
| Paquete anterior, clics reales de ambos sitios | 10 comprobaciones; los seis escenarios con ticket antiguo/vencimiento quedan bloqueados como se esperaba al reproducir la regresión |
| Paquete corregido, escritorio/tableta/móvil | **36/36**: 30 combinaciones de sitio/estado/tamaño, aislamiento de Generation, cuatro comprobaciones de permisos y Mi espacio; [resultado](evidencia/accesos-publicos-20261030/despues.json) |
| Errores JavaScript del recorrido corregido | **0** |
| Sesión, rechazo de operaciones vencidas y error de generación | **6/6** grupos ASP.NET/PostgreSQL aislados, incluido diagnóstico de lectura y saneado de excepciones; [resultado](evidencia/accesos-publicos-20261030/sesiones.json) |
| Reglas/HTTP de backend | **116/116**, con comprobación de los enlaces explícitos en Razor anónimo/autenticado |
| Frontend | **105/105**, TypeScript y Vite correctos |
| Compilación de solución Release | Correcta, **0 advertencias y 0 errores** |
| Publicación local antes/después | Correcta; DLL usada por el host idéntica a la del paquete por SHA-256 |

Capturas después del clic voluntario y del nuevo inicio: Procedimientos en [escritorio](evidencia/accesos-publicos-20261030/acceso-publico-procedimientos-1440.png), [tableta](evidencia/accesos-publicos-20261030/acceso-publico-procedimientos-900.png) y [móvil](evidencia/accesos-publicos-20261030/acceso-publico-procedimientos-390.png); Competencias en [escritorio](evidencia/accesos-publicos-20261030/acceso-publico-competencias-1440.png), [tableta](evidencia/accesos-publicos-20261030/acceso-publico-competencias-900.png) y [móvil](evidencia/accesos-publicos-20261030/acceso-publico-competencias-390.png). Sólo aparecen identidades y áreas sintéticas.

No se ejecutó de nuevo la suite ampliada completa. El host de aceptación usa configuración Testing sintética con DLL/recursos Release publicados y ASP.NET/PostgreSQL reales; no acredita Microsoft/Nexo institucionales ni dispositivos físicos. Los ajustes del fixture durante la preparación (redirecciones del proveedor simulado y esperas del limitador) no cambian el producto ni se presentan como incidencias de producción.

No se accedió al servidor institucional, no se modificaron bases existentes, secretos o permisos, ni se ejecutaron reinicios/reaperturas reales, migraciones institucionales, push o despliegue. No hay nueva migración de proyecto. El estado de `tdv2.service` y sus rutas/puerto procede del operador; queda pendiente aplicar y comprobar la corrección en producción mediante su proceso habitual, fuera de esta tarea.
