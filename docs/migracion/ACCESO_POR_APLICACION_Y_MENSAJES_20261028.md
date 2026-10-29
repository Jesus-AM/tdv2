# Mensajes de guardado y acceso independiente — 2026-10-28

**Revisión posterior del mismo día:** Mi espacio y las entradas genéricas resuelven un único sitio directamente, muestran el selector con dos o más y distinguen una falla de comprobación del estado sin sitios. Registro extensible y terminología «sitios»; las evidencias siguientes documentan la entrega anterior. [Comportamiento vigente, archivos, pruebas y capturas](MI_ESPACIO_SITIOS_20261028.md).

## Implementado

Se conserva el trabajo local de [autoguardado fluido](../competencias-digitales/AUTOGUARDADO_FLUIDO_20261028.md). Esta revisión retira únicamente el mensaje largo que aparecía tras pulsar Guardar avance. El indicador breve `Avance guardado`, los cambios pendientes y los errores permanecen en el espacio estable existente. No se alteran debounce, foco, selecciones, serialización de solicitudes, recuperación, versiones ni recibos manuales. Los autoguardados siguen sin generar eventos de guardado para Nexo; Guardar avance confirma una operación manual idempotente después de persistir.

La revisión del acceso encontró dos dependencias de Procedimientos: el destino genérico de OAuth era `/inicio` y el canje de identidad exigía pertenencia a una aplicación antes de emitir la sesión. Además, los mensajes públicos exponían el proveedor y sus roles. Ahora se distinguen identidad Microsoft, acceso a Procedimientos y acceso a Competencias.

### Identidad y autorización

- Microsoft conserva el tenant configurado, Graph `/me`, state de un uso, PKCE, validación del ID token/nonce y vínculo local de object ID/tenant. No se confía en una identidad enviada por React.
- El nombre institucional se obtiene de la aplicación solicitada cuando está disponible, o de la otra publicación de permisos. Si ninguna proporciona un perfil, el nombre alterno es el correo ya validado por Microsoft; no se inventa nombre, número de empleado ni adscripción. El displayName no validado de Graph no sustituye el nombre institucional.
- Una cuenta institucional válida puede mantener sesión aunque ninguna aplicación privada esté autorizada. El ticket no incorpora roles, módulos ni alcance. Esto **no concede acceso privado**: `RequestAccess` y `DigitalAccess` siguen verificando las publicaciones correspondientes en cada solicitud. Ante fallas no se usan permisos locales ni claims antiguos.
- Competencias conserva además la comprobación de empleado, sus módulos y su alcance institucional. No deriva acceso de los permisos de Procedimientos, ni viceversa. La representación mantiene actor real, identidad efectiva y salida; el selector no permite saltarse un contexto de representación activo.
- La consulta de accesos no genera eventos de encuesta, no modifica respuestas y no sustituye los eventos de actividad existentes. Se conserva la auditoría local de errores, login y cierre de sesión.

### Entradas, retorno y cambio de cuenta

| Entrada | Comportamiento |
| --- | --- |
| `/` | Portal público, también para visitantes y cuentas sin acceso privado. No comprueba permisos de una aplicación para mostrarlo. |
| Registrar un procedimiento | Enlace a `/inicio`; si no hay sesión, el backend inicia OAuth conservando ese destino. |
| Responder encuesta | Conserva `/competencias-digitales` y su retorno institucional. |
| `/connect` sin destino | Después de validar identidad conduce a `/accesos`. No selecciona Procedimientos automáticamente. |
| `/accesos` | Requiere identidad, no permiso de Procedimientos. Ofrece únicamente aplicaciones cuya autorización acaba de verificarse. Sin aplicaciones muestra un aviso general, portal y cambio de cuenta. Una falla parcial se identifica sin ocultar el acceso comprobado a la otra aplicación. |
| Ir a mi espacio en el portal | Abre `/accesos`. |
| Cambiar de cuenta | Reutiliza POST con CSRF a `/session/use-another-account`, revocación/cookies existentes y `prompt=select_account`. Conserva el destino mediante `returnUrl` validado. |

`ApplicationEntry.SafeReturn` permite rutas locales conocidas de interfaz: portal, selector, páginas de Competencias, `/inicio`, colaboradores, configuración, representación/vista de prueba y `/formatos/{id}`. Los parámetros se conservan únicamente sobre una ruta permitida. Rechaza destinos externos, `//`, barras invertidas, controles, rutas codificadas, segmentos `..`, API y rutas desconocidas. El retorno se cifra en la cookie temporal existente y se vincula al state; un parámetro añadido al callback no lo sustituye. Una ruta rechazada vuelve al selector, nunca a una URL externa.

401 HTML de ambas aplicaciones inicia OAuth con el destino; 401 JSON permanece 401 y el navegador inicia el flujo institucional. Una sesión inválida se retira antes de volver a autenticar para evitar ciclos. **Una denegación de permisos no cierra la sesión**. 403 y 503 se conservan en las API y en el transporte de páginas; no se convierten en login fallido ni éxito.

### Avisos visibles

Título: **Acceso no disponible**.

- Procedimientos: **Tu cuenta no tiene acceso a Procedimientos institucionales.**
- Competencias: **Tu cuenta no tiene acceso a Competencias digitales.**
- Falla de comprobación: **No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.**

Las vistas no mencionan Nexo, claves de roles, aplicaciones numéricas ni excepciones. Incluyen Volver al portal y Cambiar de cuenta. El enlace a la otra aplicación llega desde una comprobación del servidor; nunca se infiere de un rol local o una denegación. El reintento se ofrece para fallas temporales. Los mensajes de error de validación y conflicto de captura conservan sus controles y códigos existentes.

## Archivos

- Identidad, entrada y retorno: `tdv2/Services/MicrosoftLoginService.cs`, `Controllers/AuthenticationController.cs`, `Controllers/SessionController.cs`, nuevo `Web/ApplicationEntry.cs`, `Web/AccessBoundary.cs`, `Web/PagePaths.cs`.
- Errores de acceso: `Web/DomainProblemResponse.cs`, `Web/RequestAccess.cs`, `Integrations/Nexo/PostgresNexoProfiles.cs`, `Competencias/DigitalAccess.cs`, `Controllers/HomeController.cs`.
- Portal: dos destinos en `tdv2/Views/Home/Index.cshtml`; no se cambia su contenido ni diseño.
- UI: nuevos `ClientApp/resources/js/pages/Accesos.tsx` y `Components/AccessActions.tsx`; `pages/AccesoRestringido.tsx`, `lib/navigation.tsx`, `types/page.ts`. En `competencias/Survey.tsx` sólo se retira el mensaje manual largo sobre el trabajo fluido ya pendiente.
- Pruebas: nuevo `tests/TDV2.NativeVerification/ApplicationEntryTests.cs`, `NativeTests.cs`, registro/filtro en `DigitalTests.cs`; nuevo `ClientApp/tests/browser/application-entry-ui.mjs`, `competencias-native.mjs`, `tests/frontend/navigation.test.mjs`. En `digital-manual-save-ui.mjs` se comprueba la ausencia de la frase larga y se toma la captura desde el inicio para que `fullPage` no coloque la cabecera sticky a media imagen.
- Los recorridos nativos existentes que iniciaban Procedimientos mediante `/connect` ahora solicitan explícitamente `/connect?returnUrl=/inicio`: `capture-matrix`, `deletion-flow`, `delivery-collaboration-native`, `first-stage-flow`, `native-flow`, `participants-browser`, `performance-flow`, `photos-flow`, `presentation-flow`, `scope-flow`, `stage-review-native`, `sync-manual-native`, `system-groups-native`, `system-tools-native`, `table-scroll-native`, `tool-catalog-native` y `users-served-flow`. `procedure-validation-native` y `procedure-coverage-native` actualizan la expectativa del enlace público a `/inicio`. Estos cambios sólo adaptan la entrada de sus fixtures; no acreditan haber repetido todas esas suites.
- README, ESTADO_MIGRACION, GUIA, AUTENTICACION_POSTGRESQL y notas de guardado enlazan este comportamiento vigente. Los demás diffs previos de autoguardado y sus evidencias se conservan.

## Verificación y reproducción aislada

Los recorridos usan Edge/React reales. La matriz HTTP usa ASP.NET, cookies, OAuth de prueba y vistas PostgreSQL en clústeres nuevos, explícitamente desechables; Microsoft y los datos publicados por Nexo son sintéticos. La suite `application-entry-ui` usa transporte simulado para reproducir las cuatro combinaciones de permisos y las fallas. Ninguna de estas pruebas acredita acceso a servicios institucionales.

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
dotnet build tests/TDV2.NativeVerification -c Release --no-restore
dotnet run --project tests/TDV2.Verification -c Release --no-restore

# Crea y detiene su propio clúster de PostgreSQL de prueba; nunca usa User Secrets institucionales.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -NoBuild -DirectStart
$env:TDV2_TEST_CASE_PREFIX='OAuth:|Microsoft|Nexo:|Sesión|CSRF|Edición/OIDC|Logout:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser -NoBuild -DirectStart
Remove-Item Env:TDV2_TEST_CASE_PREFIX

# Desde ClientApp; sólo puertos loopback, fuentes y personas sintéticas.
node tests/browser/application-entry-ui.mjs
node tests/browser/digital-autosave-ui.mjs
node tests/browser/digital-manual-save-ui.mjs
```

### Resultados

| Comprobación | Resultado y alcance |
| --- | --- |
| TypeScript y Vite | Correctos; recursos construidos localmente. |
| ASP.NET/NativeVerification Release | Compilación correcta, 0 advertencias y 0 errores. |
| Frontend `test:forms` | 97/97: incluye retorno 401 de ambas aplicaciones, enlace alternativo comprobado y cambio de cuenta con query conservando invalidación de identidad/foto. |
| Verificador .NET existente | 116/116, observado en la salida de ejecución; no equivale a conexión institucional. |
| Regresión PostgreSQL de Competencias | 55/55 casos anteriores correctos, incluidos consentimiento, versiones, finalizadas, concurrencia, representación y cola manual. Varios autoguardados no encolan guardado central; confirmación manual sin cambios, doble envío, reintento, conflicto, fallo de cola y entrega posterior conservan sus reglas. |
| Entrada independiente ASP.NET/PostgreSQL | 8/8 casos nuevos correctos en la repetición: sólo Procedimientos, sólo Competencias, ambas, ninguna, revocación, fallo temporal y alternativas, 401/CSRF/cambio de cuenta y retorno seguro. Se repitió además el caso de retorno tras completar la lista de rutas permitidas. |
| Autenticación y seguridad PostgreSQL | 20/20: state, PKCE, tenant/object ID, firma/nonce, revocación, CSRF, logout, sesión activa y recuperación sin bucle de una identidad inválida. |
| React/Edge con transporte sintético | 48 grupos correctos: 16 de acceso (1440/390 px), 16 de autoguardado fluido y 16 de confirmación manual. Sin errores JS. |
| Edge + ASP.NET + PostgreSQL aislado | 23 grupos correctos. Portal anónimo, retorno Microsoft simulado, cuenta/foto, consentimiento, captura y recarga, red lenta/errores/reintento, pasos, informe definitivo, representación, roles y acceso exclusivo. Escritorio 1366 px y móvil 390/320 px. Sin errores JS. |
| Revisión final | `git diff --check` correcto; sin cambios de migraciones, contratos del instrumento, paquetes o secretos. |

Se conservaron las incidencias de pruebas en la evidencia, sin presentarlas como ejecuciones completas correctas: la primera pasada fue **62/63** porque el nuevo caso encadenaba 26 solicitudes OAuth y sobrepasaba el límite de 20/minuto. El lote corregido ejecuta nueve flujos completos y comprueba el resto de rutas directamente, sin elevar el límite productivo. La siguiente ejecución pasó los ocho casos HTTP y 22 recorridos de navegador, pero su último recorrido usaba un ayudante de captura que exigía `.cd-site h1` en la vista genérica de denegación. Corregido ese ayudante, la repetición final pasó **23/23** recorridos y el caso de retorno adicional. Una expectativa anterior de autenticación aún pedía `/inicio` para una entrada sin destino; se actualizó a `/accesos` conservando la prueba de que otro callback no sustituye la identidad. La repetición de autenticación pasó 20/20.

Informes reproducibles en [evidencia/acceso-guardado-20261028](evidencia/acceso-guardado-20261028/): primera ejecución de Competencias, repetición de accesos, recorrido final, autenticación, UI simulada, tipos/builds y 97 pruebas frontend. Las ejecuciones de PostgreSQL residen también en `.artifacts/native-postgres-20261028-211055-13a66231`, `...-212030-431fe1a9`, `...-212030-74e9e307` y `...-212506-bbbd5be5`; todos esos clústeres se detuvieron al terminar.

Capturas revisadas:

- [Elección de aplicaciones en escritorio](evidencia/acceso-guardado-20261028/choices-2-1440.png) y [móvil](evidencia/acceso-guardado-20261028/choices-2-390.png).
- [Cuenta sin aplicaciones](evidencia/acceso-guardado-20261028/choices-0-1440.png) y [denegación de Competencias](evidencia/acceso-guardado-20261028/denied-cd-390.png), con transporte sintético.
- [Denegación de Procedimientos con acceso exclusivo a Competencias](evidencia/acceso-guardado-20261028/acceso-exclusivo-1366.png) y [móvil](evidencia/acceso-guardado-20261028/acceso-exclusivo-390.png), con backend y PostgreSQL reales de prueba.
- [Guardado manual con mensaje breve](evidencia/acceso-guardado-20261028/guardado-manual-1440.png), [móvil](evidencia/acceso-guardado-20261028/guardado-manual-390.png) y [confirmación tras autoguardado lento con PostgreSQL](evidencia/acceso-guardado-20261028/competencias-autoguardado-confirmado.png).

## Límites y operación posterior

No hay migraciones nuevas, secretos, roles, conexiones ni permisos nuevos de Nexo. No se modifican el instrumento, consentimientos, informes, cola, paquetes ni contratos de escritura. No se accedió a bases institucionales, no se ejecutó un reinicio institucional, ni se hizo push o despliegue.

Pendiente de validación operativa por el operador: las cuatro combinaciones de cuentas y retorno usando Microsoft/Nexo reales en el entorno autorizado. Windows y Ubuntu utilizan las mismas rutas y bundles; tras el procedimiento normal de publicación deben comprobarse `/accesos`, `/inicio`, `/competencias-digitales` y el cambio de cuenta. Esta entrega sólo prueba compilación y ejecución local; no valida despliegue ni proxy institucional.
