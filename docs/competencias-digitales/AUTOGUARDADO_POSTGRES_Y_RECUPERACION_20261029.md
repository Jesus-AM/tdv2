# Autoguardado: PostgreSQL aislado y recuperación explícita — 2026-10-29

**Auditoría ampliada posterior:** [Costo por solicitud, desglose de las 35 sentencias, carga con 1/10/25 usuarios y sensibilidad al pool](COSTO_AUTOGUARDADO_20261029.md). Separa login/fixtures/verificación, SQL interno y latencia HTTP. Las muestras originales siguientes permanecen como evidencia histórica; la ampliación no cambia la política de captura ni las conexiones de producción.

## Comportamiento implementado

Esta revisión sustituye la recuperación automática de solicitudes fallidas descrita en [Captura fluida](AUTOGUARDADO_FLUIDO_20261028.md). Conserva el debounce de **1500 ms**, una sola escritura en vuelo, la selección inmediata y las revisiones locales que protegen ediciones realizadas mientras llega una respuesta. No modifica campos, diseño, metodología ni API de persistencia.

- Un fallo de conexión/HTTP 5xx detiene los envíos automáticos de esa captura. También se detienen preventivamente ante conflictos o rechazos del servidor. Se cancela el temporizador y no se crea una cola para enviar posteriormente. Editar campos, volver a tener conexión, recuperar foco o cambiar de sección no reactiva los envíos.
- El indicador estable dice **«Sin confirmar. Usa Guardar avance.»**. Los datos visibles permanecen en memoria de la página: los cambios no confirmados no se anuncian como guardados ni recuperables después de cerrarla. El aviso nativo de salida con cambios pendientes se conserva. Los errores de validación/conflicto mantienen su detalle.
- Solo **Guardar avance** permite recuperar la captura sin recargar. Consulta el estado actual del servidor; admite la revisión conocida o el documento exacto de un commit cuya confirmación se perdió. Conserva las ediciones locales y envía una instantánea nueva de los datos actuales, si son distintos. Nunca reenvía el documento fallido antes de enviar esos cambios. Si otra sesión dejó una revisión incompatible, mantiene el conflicto; no adopta su versión para sobrescribirla.
- La instantánea incierta se conserva exclusivamente para comparar la lectura de recuperación. No es una operación en cola, no se ejecuta, no se guarda en localStorage y no sobrevive a la recarga.
- Si falla el guardado manual o su confirmación, tampoco se reintenta automáticamente. Un manual confirmado reactiva el autoguardado para **cambios posteriores**, sin reproducir ediciones anteriores. El UUID de confirmación se conserva si se pierde su respuesta: un nuevo intento explícito recupera el mismo recibo.
- Navegación/finalización siguen exigiendo persistencia y validación del backend. Si el autoguardado está detenido, primero debe confirmarse Guardar avance. Una recarga muestra lo efectivamente persistido, incluso si su respuesta se perdió, y descarta los cambios que sólo estaban en pantalla.
- Una comparación semántica evita el PUT cuando el usuario vuelve al valor ya guardado dentro del debounce. La versión/fecha de la encuesta no se alteran para generar actividad. Las validaciones explícitas de transición conservan su comprobación en servidor.

No se modifica `DigitalSurveyService`, los contratos, versiones, consentimientos, transacciones, bloqueos, permisos, finalización ni informe histórico. El servidor ya deduplica documentos idénticos; no tiene un ciclo de reintentos para guardar respuestas. Se conserva la distinción entre una lectura posterior de otra sesión y la confirmación de nuestra propia captura.

## Auditoría

Los PUT de autoguardado, cambios de bloque y navegación siguen generando **cero eventos de guardado hacia Nexo**, tanto en cola como en el receptor sintético. Se conserva `activity_logs` y su deduplicación por encuesta/revisión. Guardar avance confirma una sola operación mediante el endpoint/UUID/recibos existentes, también cuando las respuestas ya se guardaron automáticamente.

**La cola Nexo y su política de entrega/reintentos no cambian.** Un reintento de entrega de actividad no guarda respuestas. La disponibilidad del receptor de actividad y la autorización vigente continúan siendo comprobaciones diferentes. Los ensayos de receptor caído, recibo perdido y representación usan el contrato extraído del código local, exclusivamente con bases y cuentas sintéticas; no acreditan recepción institucional.

## Medición SQL y HTTP

Se agregó instrumentación **sólo al ejecutor de pruebas**. `TDV2_TEST_AUTOSAVE_MEASURE=true` habilita `pg_stat_statements` en el clúster nuevo del arnés, después de verificar que está dentro de `.artifacts`. No hay instrumentación, extensiones ni DDL en el arranque de la aplicación.

`DigitalAutosaveMeasurements.cs` utiliza ASP.NET real, las consultas de autorización reales con catálogo Nexo sintético y tres bases desechables separadas. Registra bytes de cuerpo JSON, duración HTTP y estadísticas de PostgreSQL por solicitud. `pg_stat_statements.track=all` incluye SQL interno de triggers/funciones; los tiempos de nivel superior se suman **sin volver a sumar el SQL interno**, para evitar doble conteo. Se exportan únicamente nombres de bases de prueba, verbos/etiquetas de tablas, cantidades y duraciones; no SQL ni parámetros, respuestas personales o secretos.

Medición inicial, antes de modificar el cliente (`native-postgres-20261029-064014-fd66937b`):

| Solicitud | Envío JSON | Respuesta JSON | Sentencias de nivel superior | UPDATE encuesta | SQL servidor | HTTP local |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Perfil completo | 236 B | 125456 B | 35 | 1 | 6,36 ms | 507,30 ms |
| Documento idéntico | 236 B | 125456 B | 32 | 0 | 1,84 ms | 434,00 ms |
| Respuesta cero válida | 286 B | 126014 B | 35 | 1 | 5,88 ms | 582,12 ms |
| Cambios agrupados | 338 B | 126270 B | 35 | 1 | 5,74 ms | 567,10 ms |
| Repetición idéntica con versión antigua | 338 B | 126270 B | 32 | 0 | 1,90 ms | 538,13 ms |
| Versión antigua y contenido distinto | 338 B | 149 B | 18 | 0 | 0,96 ms | 253,74 ms |
| Documento de todos los bloques | 3634 B | 148487 B | 35 | 1 | 6,15 ms | 456,94 ms |

En un guardado modificado hay **23 SELECT, 1 UPDATE de encuesta, 1 INSERT de bitácora local y 10 sentencias transaccionales**. Las 35 sentencias no equivalen a 35 actualizaciones; el ROLLBACK al disponer una transacción de lectura es su cierre normal, no un fallo de captura. El ensayo inicial inducía dos escritores bloqueando deliberadamente el advisory lock del empleado: ambos esperaron, uno confirmó y otro recibió HTTP 409; se liberó tras 298 ms y no quedaron bloqueos residuales. El último cero guardado se recuperó mediante una lectura HTTP nueva.

La medición inicial filtraba el usuario administrador de prueba y no desglosaba SQL interno ejecutado con `SECURITY DEFINER`. La medición final incorpora también esas sentencias internas, separadas por `TopLevel=false`; no se comparan esas cantidades internas con la muestra inicial como si hubiese cambiado producción.

Medición final (`native-postgres-20261029-064424-8f0d7be4`), mismo servidor de aplicación y nueva base desechable:

| Solicitud | SQL servidor | HTTP local | UPDATE encuesta | UPDATE control (trigger) |
| --- | ---: | ---: | ---: | ---: |
| Perfil completo | 5,63 ms | 322,81 ms | 1 | 1 |
| Documento idéntico | 1,98 ms | 530,18 ms | 0 | 0 |
| Respuesta cero válida | 5,74 ms | 433,57 ms | 1 | 1 |
| Cambios agrupados | 5,61 ms | 311,63 ms | 1 | 1 |
| Repetición idéntica con versión antigua | 1,97 ms | 411,83 ms | 0 | 0 |
| Versión antigua y contenido distinto | 1,09 ms | 337,11 ms | 0 | 0 |
| Documento de todos los bloques | 6,10 ms | 418,74 ms | 1 | 1 |

Se conservan exactamente los tamaños y cantidades de sentencias de nivel superior. El trigger actualiza `cd_control` para mantener la invalidación administrativa: es una escritura necesaria, no una segunda copia de las respuestas. Los documentos idénticos todavía ejecutan el INSERT condicional de bitácora, con **cero filas insertadas**; la comparación nueva del cliente evita llegar a esa ruta si se revierte una edición antes de enviar. En la contención final se observaron dos escritores esperando y respuestas 200/409 tras liberar el bloqueo inducido (323,61 ms); cero bloqueos residuales. [Estadísticas iniciales](evidencia/autoguardado-20261029/sql-before.json) / [finales](evidencia/autoguardado-20261029/sql-after.json).

**Interpretación:** no se detectaron actualizaciones redundantes de la encuesta en el servidor. Había solicitudes evitables cuando una edición se revertía a su valor persistido; el cliente ahora las omite. El contrato todavía devuelve el instrumento/evaluación completos, lo cual explica los 125–148 KB sin compresión de respuesta; se documenta ese costo, sin alterar el contrato en este ajuste. La latencia HTTP no equivale al tiempo SQL: incluye conexiones, autorización, serialización y transporte. El arnés usa `Pooling=false` y conexiones locales sintéticas; estos números no representan rendimiento institucional ni una prueba de capacidad. No se cambió la configuración de conexiones para mejorar artificialmente el resultado.

## Comprobaciones y evidencia

Se distinguen tres capas:

1. Edge/React y transporte controlado: selección rápida, espera de 2000 ms, fallo con temporizador pendiente, selecciones después del fallo, online/foco, manual fallido, confirmación perdida, recarga, conflicto y cambios revertidos sin PUT. Mide peticiones por sesión, foco, scroll, nodos reemplazados y tiempo de deshabilitación.
2. ASP.NET/PostgreSQL aislado: SQL real, actualización única, no-op, conflicto y contención; auditoría manual, indisponibilidad/recibos, permisos, consentimiento, concurrencia e inmutabilidad.
3. Navegador con ASP.NET/PostgreSQL: captura mientras hay respuesta pendiente, fallos antes y después del commit, recuperación manual con datos actuales, nueva sesión/recarga y rechazo de otra revisión.

El navegador con transporte controlado pasó **22 escenarios**, en 1440 y 390 px, más **16 escenarios de confirmación manual**. En ambas sesiones medidas, cinco selecciones iniciales se agruparon en un PUT y otras dos durante su respuesta en un segundo PUT: **2 solicitudes por 7 selecciones, máximo 1 en vuelo**, con 2000 ms de demora controlada (HTTP observado 2008–2018 ms) y **252 B por envío, 504 B por sesión medida**. Hubo **0 ms de campos deshabilitados, 0 nodos reemplazados, 0 px de salto o cambio de scroll**, y foco conservado. Una edición revertida produjo cero PUT. Después del primer fallo, tres selecciones separadas por más del debounce, online/foco y una transición de bloque produjeron cero solicitudes nuevas; el manual envió una instantánea actual, y solo una edición posterior reactivó el automático. El manual fallido tampoco reintentó. [Reporte de navegador](evidencia/autoguardado-20261029/browser-controlled.json) / [recibos manuales](evidencia/autoguardado-20261029/manual.json).

Capturas inspeccionadas: solicitud en vuelo [escritorio](evidencia/autoguardado-20261029/saving-1440.png) y [móvil](evidencia/autoguardado-20261029/saving-390.png); error estable con captura conservada [escritorio](evidencia/autoguardado-20261029/stopped-1440.png) y [móvil](evidencia/autoguardado-20261029/stopped-390.png). La captura móvil del error está desplazada para mostrar su indicador; no corresponde al inicio de la página.

El ejecutor completo pasó **66 casos de backend**, incluidos los siete de guardado manual: cero actividad automática, duplicados concurrentes, conflicto/CSRF/consentimiento, Nexo no disponible y entrega posterior, rollback, representación y finalización. En la primera ejecución integrada, el caso nuevo de escritorio pasó; el de móvil esperaba erróneamente un PUT al volver al valor que la iteración anterior ya había guardado. Se corrigió **la precondición del arnés** para iniciar ambos tamaños en cero, manteniendo la omisión de escrituras idénticas. [Resultado original íntegro, con ese fallo de prueba explícito](evidencia/autoguardado-20261029/native-first-run.json). No se presenta ese ejecutor inicial como una suite totalmente correcta ni se repiten los 66 casos sin cambios de backend.

TypeScript, Vite y compilación ASP.NET/NativeVerification Release finalizaron correctamente; .NET con cero errores y advertencias. **99/99 pruebas frontend** correctas. Los archivos de captura no usan instrumentación permanente ni generan nuevos eventos de Nexo; las estadísticas SQL sólo se habilitan en el ejecutor desechable.

La repetición del navegador en un clúster nuevo, `native-postgres-20261029-065408-90ba9bc5`, terminó con **24 grupos correctos y cero errores JavaScript**: [resultado del ejecutor](evidencia/autoguardado-20261029/native-browser-final.json) / [recorrido y mediciones](evidencia/autoguardado-20261029/integrated-browser.json). Incluye también privacidad, secciones bloqueadas, cuenta/foto, informes definitivos, autorización independiente, representación y regresión del portal/Procedimientos. Los reinicios del arnés sólo operan sobre fixtures sintéticos.

En cada sesión integrada de fallo hubo **1 intento automático abortado, 0 envíos adicionales al editar o recuperar conexión/foco, 1 PUT manual explícito y 1 PUT automático de un cambio posterior**. Los dos PUT confirmados tardaron 324,46/434,22 ms en escritorio y 306,82/308,60 ms en móvil, con cuerpos de 252/253 B por envío. El escenario de confirmación perdida envió una sola vez antes de la intervención manual y leyó el commit real de PostgreSQL. El caso de otra sesión produjo conflicto, conservó sus datos y nunca reutilizó la versión ajena para sobrescribirlos. El caso lento integrado previo midió 433,92/325,02 ms de HTTP local antes de retener artificialmente la respuesta del primero.

Capturas integradas: [manual confirmado](evidencia/autoguardado-20261029/integrated-saved.png), [error en escritorio](evidencia/autoguardado-20261029/integrated-stopped-1366.png), [error en móvil](evidencia/autoguardado-20261029/integrated-stopped-390.png). Los tres clústeres utilizados fueron detenidos por el arnés. `git diff --check` sin errores. No quedan verificaciones de esta entrega en ejecución.

La muestra no acredita capacidad con un padrón grande ni latencia de servicios reales. Reducir el tamaño de la respuesta completa o medir apertura de conexiones con un entorno representativo sería una optimización separada; no se eliminaron controles de autorización ni se cambiaron conexiones para reducir artificialmente las consultas. Se conserva la limitación existente de transacciones separadas entre diagnóstico y bitácora TDV2; no se afirma atomicidad distribuida.

## Archivos de esta revisión

- `ClientApp/resources/js/competencias/Survey.tsx`: detención explícita, recuperación manual, comparación antes de enviar e indicador breve estable.
- `ClientApp/tests/browser/digital-autosave-ui.mjs`: pruebas de sesión y medidas de interacción con transporte controlado.
- `ClientApp/tests/browser/digital-manual-save-ui.mjs`: confirmación manual, error breve y recuperación explícita del mismo recibo.
- `ClientApp/tests/browser/competencias-native.mjs`: comportamiento en navegador con PostgreSQL aislado; actualización de expectativas de recuperación.
- `tests/TDV2.NativeVerification/DigitalAutosaveMeasurements.cs`, `DigitalTests.cs`: medidas SQL/HTTP y registro optativo del escenario.
- `scripts/Test-NativePostgres.ps1`: extensión de medición optativa, limitada al clúster desechable.
- README, ESTADO_MIGRACION, GUIA y guías previas de captura/actividad: política vigente, evidencia y límites.

## Reproducción local

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false
$env:TDV2_TEST_AUTOSAVE_MEASURE = 'true'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -NoBuild -DirectStart
Remove-Item Env:TDV2_TEST_AUTOSAVE_MEASURE
Set-Location ClientApp
node tests/browser/digital-autosave-ui.mjs
node tests/browser/digital-manual-save-ui.mjs
```

Para sólo SQL, añadir `-SkipBrowser` y `TDV2_TEST_CASE_PREFIX='Autoguardado SQL:'`; retirar después esa variable para no filtrar una verificación completa. La extensión debe estar instalada con el PostgreSQL **aislado**. No requiere paquetes nuevos, migraciones, secretos, permisos ni cambios de configuración institucional. El arnés Windows tiene el fallback conocido de `initdb` 87/3 y detiene su propio clúster al terminar. Ubuntu y servicios institucionales no forman parte de estas pruebas. No se ejecutaron push, despliegues ni operaciones sobre bases institucionales.
