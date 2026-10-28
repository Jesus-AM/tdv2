# Instrumento y actividad de Competencias — 2026-10-28

## Alcance y estado

Se continúa el trabajo local existente, incluido el informe organizado, aviso `.2`, menú publicado por Nexo, paquetes corregidos y validación de identidad que resolvió el 401. No se cambiaron roles, rutas existentes, fuentes, colores, conexiones ni sincronizadores. No se accedió a bases institucionales ni se ejecutaron migraciones/reinicios reales, push o despliegues.

La edición vigente es **`uacj-digital-2026-10-28.1`**, recurso `tdv2/Contracts/competencias-digitales-v2.json`. La edición `uacj-digital-2026-10-27.1` y su archivo permanecen intactos. El aviso conserva exactamente su texto, URL y versión **`uacj-digital-aviso-2026-10-27.2`**; cambiar de instrumento no concede ni exige por sí solo otra aceptación del aviso.

## Perfil y metodología

El único catálogo de captura está en el recurso del instrumento. `puesto` sigue siendo el identificador persistido y su etiqueta visible es «Perfil laboral». No representa un rol Nexo. Las funciones admiten selección múltiple obligatoria de **Académica, Administrativa, Directiva y Otra**, sin pregunta adicional para Otra (comportamiento anterior). Técnica y Atención o servicio quedan fuera de la edición nueva.

Correspondencia explícita en `positionRules`, en el orden solicitado:

| Perfil laboral nuevo | Regla metodológica anterior |
| --- | --- |
| Dirección | Dirección |
| Subdirección | Subdirección |
| Coordinación | Jefatura de función |
| Jefatura de departamento | Jefatura de función |
| Jefatura de función | Jefatura de función |
| Responsable | Jefatura de función |
| Personal Académico | Personal docente |
| Asistente Profesional | Personal operativo |
| Apoyo Técnico | Personal operativo |
| Auxiliar administrativo | Personal operativo |
| Personal Manual | Personal operativo |

Es una correspondencia de implementación para reutilizar las reglas existentes, no una nueva rúbrica ni una clasificación institucional de personas. Los nombres conservados mantienen sus reglas. Los nuevos perfiles de coordinación/responsabilidad usan Jefatura y los de apoyo/operación usan Personal operativo. Queda para revisión metodológica institucional esta correspondencia explícita; no se presentó como una decisión emitida por Nexo. Un perfil desconocido se rechaza, en lugar de caer silenciosamente en una regla genérica.

`Requirements` sigue combinando los requisitos propios del perfil con la base operativa transversal (excepto SII), con los mismos límites y máximos anteriores. Las funciones seleccionadas son contexto descriptivo: **no cambiaban los niveles esperados en la implementación anterior**; no se les añadió una ponderación. Datos personales conserva su efecto existente en las recomendaciones. Personal Académico activa explícitamente las herramientas exclusivas académicas, la selección de cursos y los textos de recomendación que antes correspondían a Personal docente.

Se retiran las entradas y sus referencias en `funcs.req`:

| Herramienta | ID vigente confirmado en el instrumento anterior |
| --- | --- |
| Bookings | `m-bookings` |
| Viva Connections y Org Explorer | `m-viva-connections-y-org-explo` |
| Viva Learning | `m-viva-learning` |
| Seguridad en Microsoft 365 | `m-seguridad-en-microsoft-365` |

La nueva edición tiene **80 herramientas**, cinco bloques y seis campos de perfil. Ciberseguridad no se modifica. El total de respuestas exigibles se calcula por aplicabilidad, complementarias elegidas y perfil; **80 no es un denominador fijo de llenado**. Por ejemplo, Dirección sin complementarias seleccionadas tiene 53 respuestas obligatorias contando Perfil.

Se conservan escala 0–3, umbrales consecutivos de rúbricas, medias por bloque/global, No aplica, No utiliza, pendientes y fórmulas de brechas/ruta. Retirar las cuatro definiciones elimina sus preguntas, requisitos, pendientes, contadores, cursos y contribuciones de la edición nueva. Las categorías se obtienen de las herramientas restantes, sin cabeceras vacías. Un cero explícito continúa siendo válido; ausencia de respuesta no genera competencia ni brecha preliminar. El informe conserva resumen, recomendaciones, tres fases, selector/tabla de bloque y fichas móviles, sin descarga HTML.

## Borradores e históricos

Leer un borrador antiguo no lo escribe ni lo transforma. La respuesta incluye `editionUpdate`: versiones, herramientas retiradas y valores del perfil que necesitan revisión. Captura/autoguardado se bloquean hasta confirmar «Confirmar actualización del borrador». La acción llama a `POST /competencias-digitales/api/encuesta/actualizar-instrumento`, con CSRF, versión optimista, generación vigente, aviso aceptado y prohibición de escritura representada.

La transacción conserva **todas las respuestas con ID vigente** (también las académicas que dejen temporalmente de aplicar), retiene funciones válidas y limpia de la captura activa sólo opciones retiradas. Un puesto retirado queda sin selección: la persona lo elige, sin reclasificación automática. Se conserva la copia exacta de instrumento, perfil, respuestas y versión anteriores en `InstrumentJson.previousDraft`, con fecha de actualización. No se expone ese archivo de captura al navegador. Un borrador con perfil incompleto vuelve a Perfil; los pasos posteriores y sus respuestas no se borran.

El reintento de la actualización ya confirmada no vuelve a cambiar la versión ni genera otra actividad. Una pestaña con el documento anterior recibe conflicto y debe recargar. La actualización sólo reconoce la edición anterior declarada en `supersedes`; no transforma versiones desconocidas. No hay actualización masiva, segundo intento ni reapertura.

Las definitivas se leen con su `InstrumentJson` y **`ReportJson` congelado**. No se sustituye su perfil, respuestas o resultado. Los filtros de reportes agregados incluyen perfiles históricos presentes dentro del alcance autorizado; herramientas retiradas sólo reaparecen en agregados cuando existen resultados históricos de ellas. Se calculan con sus observaciones disponibles, sin convertir la ausencia en cero. No se excluye un histórico por llevar Personal docente/Personal operativo.

## Hallazgo de auditoría y contrato real de Nexo

`Security/OperationAudit.cs` y `AuthenticationAudit.cs` escribían en **`activity_logs` de TDV2**, sin entregar actividad genérica de Competencias a Nexo. La autorización Nexo y sus operaciones de representación no acreditaban esa entrega. Se conserva esa auditoría.

Se inspeccionó exclusivamente código local de Nexo (sin bootstrap, `.env`, bases ni cambios):

- `app/Services/ActividadAplicacion.php`.
- `app/Services/PostgresPublication.php` y `app/Services/Postgres/registrar_actividad.sql`.
- `app/Services/PostgresProvisioner.php` y la migración que define actividad.

El contrato publicado es **`public.nexo_a{ApplicationId}_registrar_actividad(jsonb)`**, por la conexión ya existente `CompetenciasDigitalesNexo`. Devuelve **`evento_uuid, duplicado`**. Nexo valida `session_user`, cuenta técnica/aplicación activa y publicación vigente; deduplica por aplicación/UUID y exige igualdad del payload en reintentos. El nombre de función se construye exclusivamente con el entero configurado y validado contra `nexo_aplicacion`, nunca con datos del navegador.

El payload usa únicamente campos permitidos: UUID, fecha UTC, categoría, acción, resultado, correo/nombre del actor validado, correo del afectado cuando corresponde, tipo e ID del recurso. **No contiene respuestas, perfiles laborales, tokens de representación, contraseñas, cadenas, IP ni user-agent.** Tampoco se infiere una concesión UR de la adscripción. El actor es siempre la identidad real; una lectura representada incluye el afectado y acción `consultar_representado`. Los roles/usuarios no se reciben del navegador para auditar.

### Entrega y deduplicación

No existía una cola de entrega para este contrato. Se añade `public.cd_activity_events` en la base del diagnóstico: payload permitido, UUID, aplicación, clave de deduplicación y sesión resumidas con SHA-256, fechas, intentos, próximo reintento, estado y recibo. La clave de sesión es un resumen derivado de la sesión interna; no es una cookie ni un token utilizable.

| Evento | Punto de confirmación / deduplicación |
| --- | --- |
| Acceso | Navegación autorizada; una vez por sesión del subsitio, sin registrar cada render/poll |
| Guardado | Sólo cambio realmente confirmado; encuesta + revisión, en la misma transacción que las respuestas |
| Finalización | UUID de encuesta; reintentos idempotentes no repiten el evento |
| Actualizar edición | Encuesta + edición; misma transacción que la actualización |
| Consultar informe | Página o API, sesión + contexto representado + generación + revisión; no duplica la lectura por ambos caminos |
| Metas | Una operación por escritura confirmada; versión rechaza reintentos obsoletos |
| Resumen/ejecución de reinicio | UUID de operación; misma transacción del diagnóstico, sin enviar respuestas/conteo individual de personas |
| Representación | ID de representación + inicio/fin, actor real y afectado; el cierre local no se presenta como confirmación del cierre central |
| Cierre de sesión | Sólo sesiones con actividad previa de Competencias; conserva el actor y registra fin local de representación cuando corresponde |

El evento se confirma primero en la base del diagnóstico. Después de confirmar respuestas, se intenta entrega con límite de tiempo; una caída de Nexo no revierte ni pierde la captura. La entrega copia primero metadata a `activity_logs` (con bloqueo/UUID para no duplicarla), luego exige un recibo central que corresponda al UUID. **`ReceivedAt` no nulo es la evidencia de recepción; `LocalRecordedAt` por sí solo no lo es.** `DuplicateReceipt` conserva la confirmación de un reintento ya recibido.

Los fallos se identifican como `nexo_rechazado` o `entrega_no_confirmada`, con advertencia genérica sin excepciones SQL completas. El reintento conserva exactamente UUID/payload; espera exponencial de 20 segundos hasta una hora. `FOR UPDATE SKIP LOCKED` permite entregas concurrentes sin duplicar. Los eventos y recibos sobreviven al reinicio administrativo: no están entre sus tablas de limpieza.

Las escrituras de encuesta/metas/reinicio y su cola son atómicas en DigitalDbContext. Las lecturas y el puente de representación/logout son best effort porque la sesión/bitácora existente reside en TDV2: un fallo **de persistencia local del diagnóstico** durante ese puente deja advertencia y la bitácora de sesión existente, pero puede impedir crear el evento para entrega. No hay transacción distribuida ni se afirma recuperación automática de ese caso. Una caída **central de Nexo después de encolar** sí queda pendiente de reintento durable. Las acciones centrales propias de representación siguen usando el mecanismo anterior de Nexo.

### Configuración y permisos

No hay secretos ni identificadores nuevos. Se reutilizan:

| Clave .NET / User Secrets | Entorno Windows/Ubuntu | Uso |
| --- | --- | --- |
| `CompetenciasDigitales:Nexo:ApplicationId` | `CompetenciasDigitales__Nexo__ApplicationId` | ID real de la aplicación de Competencias |
| `ConnectionStrings:CompetenciasDigitalesNexo` | `ConnectionStrings__CompetenciasDigitalesNexo` | Publicación/función Nexo bajo la cuenta técnica de esa aplicación |
| `ConnectionStrings:CompetenciasDigitales` | `ConnectionStrings__CompetenciasDigitales` | Diagnóstico, cola y recibos |
| `ConnectionStrings:Tdv2` | `ConnectionStrings__Tdv2` | Catálogos/sesiones y bitácora local existentes |

El operador debe comprobar que Nexo publique la función indicada para **esa cuenta/aplicación**, con EXECUTE concedido por su mecanismo real de aprovisionamiento. No se requieren escrituras directas sobre tablas centrales ni nuevos permisos Microsoft. No se cambió el proyecto de Nexo ni sus datos. No se inventa un ID definitivo ni un rol técnico.

En la base propia, el propietario de migraciones crea la cola y sustituye la función de trigger. La cuenta web necesita SELECT/INSERT en `cd_activity_events` y UPDATE de `LocalRecordedAt`, `ReceivedAt`, `DuplicateReceipt`, `Attempts`, `NextAttemptAt`, `Failure`; conserva sus permisos anteriores de encuesta/sesión/bitácora. No necesita DELETE/TRUNCATE de la cola, administración, secuencias ni acceso adicional a catálogos. Los nombres de objetos son calificados. La nueva función de trigger es invoker, `search_path=pg_catalog`; no introduce una función privilegiada de escritura. La función central es la publicada y administrada por Nexo.

Ejemplo de concesión mínima para el operador en **la base del diagnóstico**, después de definir en psql la variable `rol_web_diagnostico` con el rol real; no es el nombre de un rol nuevo:

```sql
GRANT SELECT, INSERT ON TABLE public.cd_activity_events TO :"rol_web_diagnostico";
GRANT UPDATE ("LocalRecordedAt", "ReceivedAt", "DuplicateReceipt", "Attempts", "NextAttemptAt", "Failure")
ON TABLE public.cd_activity_events TO :"rol_web_diagnostico";
```

## Migración, respaldo y operación posterior

Nueva migración **exclusiva de DigitalDbContext**: `20261028175912_DigitalActivityDelivery`. Las dos migraciones anteriores y sus Designers permanecen intactos.

Además de crear cola/índices (clave UUID, sin secuencia), sustituye `cd_guard_survey` para permitir solamente el salto v1→v2 en borradores, con incremento de versión y copia exacta de instrumento/perfil/respuestas anteriores. No permite cambiar participante, editar una definitiva, borrar un intento ni actualizar y finalizar en la misma operación. El trigger de revisión del reinicio sigue activo. La migración no transforma ninguna fila de encuestas ni acepta avisos.

**Antes de habilitar el código en el entorno real:** respaldo completo de la base del diagnóstico, incluidos `InstrumentJson`, `ReportJson`, consentimientos, control, recibos de reinicio, historia y cola cuando exista. Mantener la bitácora TDV2 y actividad central según su operación institucional. Usar el mecanismo de respaldo/restauración autorizado del operador; no copiar secretos a comandos o documentación. Probar recuperación en otra base aislada. No mezclar historias EF ni restaurar parcialmente respuestas sin su contexto/generación.

Comandos para fase operativa posterior, desde la raíz del proyecto y con la configuración del destino validada por el operador (no ejecutados sobre bases reales en esta entrega):

```powershell
# Windows: sólo migraciones de diagnóstico; no inicia la web ni ejecuta reinicios.
dotnet ef migrations script 20261027131404_AdministrativeDigitalReset 20261028175912_DigitalActivityDelivery --context DigitalDbContext --project tdv2 --idempotent --output .artifacts/digital-activity.sql
dotnet ef database update 20261028175912_DigitalActivityDelivery --context DigitalDbContext --project tdv2

# Desarrollo con configuración administrada existente:
dotnet run --project tdv2 --no-launch-profile -- --deliver-digital-activity
```

En Ubuntu, revisar/aplicar el mismo script EF generado o un bundle de migración **DigitalDbContext** con su conexión separada. Tras desplegar por el procedimiento autorizado, desde el directorio publicado y con sus variables existentes:

```sh
dotnet tdv2.dll --deliver-digital-activity
```

La entrega explícita procesa hasta 100 eventos vencidos y conserva los que esperan reintento. Salida 0: no quedan pendientes; 2: quedan pendientes (incluidos próximos reintentos); 1: error operativo, sin imprimir detalles sensibles. Puede invocarse posteriormente por el programador del operador; **no se instala ninguna tarea ni se aplica DDL al arrancar**. Las solicitudes normales también intentan lotes pequeños tras commit. Comprobar recibos en `cd_activity_events` y en Nexo; no interpretar el mensaje de guardado de encuesta como confirmación central.

El Down rechaza retirar el esquema si existen eventos/recibos o encuestas v2. No es un procedimiento de borrado de auditoría. Si hay contenido, recuperar con respaldo y versión de código compatible, en una ventana controlada por el operador; no degradar automáticamente a un binario que sólo reconoce 84 herramientas. El archivo anterior de borrador se conserva como contexto, no como función de reapertura.

## Archivos modificados

- `tdv2/Contracts/competencias-digitales-v2.json`, `tdv2.csproj`, `Hosting/ServiceRegistration.cs`: nueva edición y publicación del recurso.
- `Competencias/DigitalInstrument.cs`, `DigitalEditions.cs`, `DigitalSurveyService.cs`, `DigitalAggregates.cs`: reglas explícitas, actualización de borrador, persistencia/versiones e históricos.
- `Competencias/DigitalActivity.cs`, `DigitalDbContext.cs`, migración/Designer/snapshot, `Program.cs`: contrato, cola, recibos y comando de entrega.
- `Competencias/DigitalRepresentation.cs`, `DigitalResetService.cs`, `Controllers/CompetenciasController.cs`, `AuthenticationController.cs`: puntos de auditoría; se conservan identidad, autorizaciones y CSRF existentes.
- `ClientApp/resources/js/competencias/{Survey,PersonalReport,Aggregates,types}.tsx/ts`: catálogo servido por backend, aviso de actualización y etiquetas de perfil.
- `tests/TDV2.Verification/DigitalEditionTests.cs`, `EntryPoint.cs`; `tests/TDV2.NativeVerification/{DigitalTests,DigitalEditionActivityTests}.cs`, fixture SQL y `Export-NexoActivity.ps1`; `scripts/Test-NativePostgres.ps1`.
- `ClientApp/tests/browser/{digital-edition-ui,competencias-native}.mjs`; README, ESTADO y esta guía/evidencia.

`PostgresIdentity.cs`, `AccessState.cs`, cliente HTTP y referencias de paquetes conservan las correcciones previas; no se retiró la solución del 401. La representación no adquiere capacidad de actualizar instrumento ni de responder.

## Verificación y evidencia

Las identidades, aplicaciones y datos de las pruebas son sintéticos. Se ejecutó la función SQL de actividad **extraída del código local real de Nexo**, sobre un esquema PostgreSQL desechable de pruebas; no un endpoint inventado ni una conexión institucional. `Export-NexoActivity.ps1` reproduce las sustituciones de `PostgresPublication::procedure(87)` y rechaza una plantilla inesperada. `87` es exclusivamente un ID de fixture. No se ejecuta PHP/Laravel ni se leen sus secretos.

Comandos de verificación (desde la raíz, Node/npm disponibles en PATH):

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-digital-edition .artifacts/instrument-activity-20261028/edition.json
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-digital-presentation .artifacts/report-notice-20261027/projections.json
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false
powershell -NoProfile -File scripts/Test-NativePostgres.ps1 -DigitalOnly -SkipBrowser -DirectStart -NoBuild
# Navegador + ASP.NET + PostgreSQL desechable:+$env:TDV2_DIGITAL_PRESENTATION_ONLY='1'
powershell -NoProfile -File scripts/Test-NativePostgres.ps1 -DigitalOnly -BrowserOnly -DirectStart -NoBuild
# Desde ClientApp: React compilado + transporte sintético y proyecciones del backend.
node tests/browser/digital-edition-ui.mjs
node tests/browser/digital-report-ui.mjs
```

Resultados finales:

| Comprobación | Resultado y alcance |
| --- | --- |
| TypeScript, build React, ASP.NET/Verification/NativeVerification Release | Correctos; builds .NET con cero errores y advertencias. Referencias NuGet previas conservadas |
| Regresión backend sin base | 116 PASS |
| Frontend `test:forms` | 95 PASS |
| Edición/metodología/metadata | [49 PASS](evidencia/20261028/edicion-backend.json): once perfiles, funciones, requisitos, herramientas académicas, cero, 80 herramientas, archivo previo y aviso intacto |
| Informe/aviso/menú sin base | 24 PASS del evaluador/proyección y [32 grupos de navegador](evidencia/20261028/informe-regresion.json), incluidos inicial, parcial, cero, No aplica, definitivo y roles |
| React de la edición nueva, transporte sintético | [3 anchos correctos](evidencia/20261028/edicion-ui.json): 1440/768/390, confirmación de borrador, selección múltiple/autoguardado, herramientas académicas, 80 filas del informe y cero válido |
| ASP.NET + PostgreSQL 18 desechable | [48 PASS, cero fallos](evidencia/20261028/postgres.json): separación, migraciones, permisos, consentimientos, guardado/finalización concurrentes, reinicio sintético, históricos, archivo de borrador, rechazo de downgrade y recibos de actividad |
| Navegador integrado con ASP.NET/PostgreSQL aislados | [20 grupos correctos, cero errores JS](evidencia/20261028/aspnet-navegador.json): encuesta por secciones, fallos/reintentos, perfil, cuenta/foto, finalización, informe, roles/representación, acceso exclusivo de Competencias, portal y `/inicio`. Reinicio UI excluido; su backend se probó exclusivamente en el clúster desechable |
| Publicación local aislada | `dotnet publish` correcto en copia de fuentes, npm offline y cachés existentes. [SHA-256 iguales](evidencia/20261028/publish-integridad.json) para ambos instrumentos y bundle de Competencias; no se publica ni inicia un servicio institucional |
| Identidad/401 | [Archivos protegidos sin cambios](evidencia/20261028/identidad-preservada.json); OAuth, retorno profundo y participante sin permisos TDV2 pasan en ASP.NET aislado |

La primera ronda reveló expectativas antiguas de conteo y el trigger que prohibía cambiar un borrador; se corrigieron y se repitió la suite completa (48/48). El clúster Windows emplea el fallback existente tras los mensajes de `initdb` sobre token restringido 87/3. La envoltura PowerShell registró `NativeCommandError` y salida 1 por ese stderr, aunque el ejecutor .NET terminó sin fallos; el script comprueba expresamente su código de salida y los JSON finales registran 48/48 y 20 grupos de navegador. Esos informes corresponden al proveedor PostgreSQL real y sus procesos fueron detenidos al terminar. Las capturas integradas se toman por ventana visible para evitar el límite de textura de Chromium en informes móviles largos; no se introdujo scroll interno ni se alteró la página para capturarla. Las fotos del proveedor son píxeles sintéticos.

Evidencia visual:

| Estado | Escritorio | Móvil |
| --- | --- | --- |
| Borrador previo: información y confirmación | [1440](evidencia/20261028/borrador-anterior-1440.png) | [390](evidencia/20261028/borrador-anterior-390.png) |
| Perfil de nueva edición pendiente de revisión | [1440](evidencia/20261028/perfil-nuevo-1440.png) | [390](evidencia/20261028/perfil-nuevo-390.png) |
| Informe preliminar académico, cero real | [1440](evidencia/20261028/informe-cero-1440.png) | [390](evidencia/20261028/informe-cero-390.png) |
| Definitivo sintético persistido en PostgreSQL | [1366](evidencia/20261028/definitivo-aspnet-1366.png) | [390](evidencia/20261028/definitivo-aspnet-390.png) |

Artefactos reproducibles locales: `.artifacts/instrument-activity-20261028`; suite PostgreSQL en `.artifacts/native-postgres-20261028-181406-9f3a164b` y navegador integrado en `.artifacts/native-postgres-20261028-182049-33d19637`. No se incorporan credenciales, logs de bases ni directorios de datos a la evidencia documental.

**Límites reales:** pruebas ejecutadas en Windows, sin servidor Ubuntu ni proveedor institucional. El SQL Nexo real se probó sobre datos/esquema sintéticos; no certifica la revisión de publicación, permisos ni recepción del Nexo institucional. Falta revisión metodológica institucional de las correspondencias de perfiles, aplicación explícita de la migración nueva, permisos mínimos de cola y comprobación del contrato publicado para la cuenta de Competencias. No hacen falta nuevos secretos/roles de usuario. Verificar recibos reales con una acción autorizada después de esa configuración; nunca inferirlos de `activity_logs` ni de un guardado exitoso.
