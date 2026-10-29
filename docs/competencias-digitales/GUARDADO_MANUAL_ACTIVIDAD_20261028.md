# Guardado de encuesta y actividad Nexo — 2026-10-28

**Recuperación vigente, 2026-10-29:** se detienen los envíos de captura tras un fallo. Guardar avance consulta el estado real y guarda los datos actuales sin replay automático; conserva el UUID/recibo de confirmación cuando corresponda. [Política y verificación SQL/navegador](AUTOGUARDADO_POSTGRES_Y_RECUPERACION_20261029.md). Esta restricción no afecta a los reintentos de la cola de actividad descritos aquí.

## Comportamiento vigente

Actualización de mensajes: Guardar avance ya no añade una explicación larga sobre continuar desde otro dispositivo. Permanece `Avance guardado` en el indicador discreto estable; errores y cambios pendientes siguen visibles. No cambia la confirmación manual ni la cola. [Revisión de mensajes y acceso independiente](../migracion/ACCESO_POR_APLICACION_Y_MENSAJES_20261028.md).

La persistencia de la encuesta y la actividad de guardado manual quedan separadas. El autoguardado sigue almacenando perfil y respuestas, validando secciones, consentimiento, generación y versión. **No crea eventos de guardado para Nexo.** Esto incluye cambios de campos/preguntas, guardados al cambiar de sección o módulo, «Guardar y continuar», preparación de la finalización y reintentos de esas operaciones.

Solo **«Guardar avance»**, el botón de guardado explícito existente, solicita confirmar un guardado manual. Si hay respuestas pendientes, primero espera su persistencia; si el autoguardado ya las almacenó, no genera otra revisión ni modifica las respuestas. La confirmación valida la revisión guardada en el backend y crea una única entrada de actividad `categoria=encuesta`, `accion=guardar`, `resultado=exitoso` mediante la cola existente.

| Acción | Persistencia / bitácora local | Actividad Nexo nueva |
| --- | --- | --- |
| Autoguardado de perfil o respuestas | Sí; UUID local estable por encuesta/revisión | Ningún evento de guardado |
| Cambio de sección o módulo / Guardar y continuar | Conserva el guardado existente | Ningún evento de guardado; navegación puede conservar su evento de acceso según la deduplicación existente |
| Guardar avance | Espera lo pendiente y confirma la revisión | Un evento manual por operación confirmada |
| Reintento del mismo guardado manual | No reescribe ni incrementa revisión | Reutiliza la entrada/UUID y recibos existentes |
| Guardado fallido, conflicto o rechazo | Conserva borrador del navegador; no confirma éxito | Ningún evento exitoso nuevo |
| Finalizar encuesta | Cierre transaccional e inmutable existente | Evento de finalización existente |
| Acceso, consulta de informe, actualización de instrumento, metas, reinicio, representación y cierre de sesión | Sin cambios de reglas | Conservan los eventos y deduplicaciones anteriores |

Una segunda pulsación independiente y confirmada de «Guardar avance» es otra operación manual, aunque el documento sea idéntico. El doble clic se trata como una sola acción. La representación continúa siendo exclusivamente de consulta y no puede confirmar guardados, aun si la cuenta original es administradora.

## Contrato interno y concurrencia

`PUT /competencias-digitales/api/encuesta` conserva `DigitalSave` y sus respuestas. Retira exclusivamente la llamada que encolaba `save:{encuesta}:{revision}`. `Saved` solo relee la encuesta; no registra actividad. `DigitalAccess`, `OperationAudit`, `AuthenticationAudit` y los manejadores de errores no tienen otra vía que convierta esos autoguardados en actividad central de guardado.

Se agrega **`POST /competencias-digitales/api/encuesta/guardar`**, contrato `DigitalManualSave`:

```json
{
  "version": 4,
  "generation": "UUID de la generación vigente recibida del servidor",
  "operation": "UUID de la pulsación manual, reutilizado en sus reintentos"
}
```

El ejemplo describe los campos; los dos UUID deben ser válidos, no esas cadenas ilustrativas. No acepta respuestas, identidad, porcentajes, SQL ni estado de finalización. Reutiliza autorización del módulo Encuesta, CSRF, límite de escrituras, sesión/identidad vigente y prohibición de representación. Exige consentimiento vigente, edición compatible y encuesta abierta. La generación y el bloqueo por empleado son los mismos de los guardados/finalizaciones. Para una operación nueva exige versión exacta; una confirmación antigua ya registrada no vuelve a confirmar una revisión posterior ni escribe datos. Tras un reinicio, la generación antigua se rechaza.

La clave durable de la cola es el resumen SHA-256 existente de aplicación + `manual-save:{generacion}:{encuesta}:{operacion}`. Incluye la encuesta del usuario validado y la generación, no un ID de persona enviado por el navegador. La restricción única de `cd_activity_events.Key`, el bloqueo por empleado y la consulta de operación previa impiden duplicados concurrentes. La confirmación no toca `cd_surveys.Version`, respuestas, fechas de captura, informe ni `cd_control.Revision`.

El frontend conserva una sola operación manual en curso. Desde la [corrección de autoguardado](AUTOGUARDADO_FLUIDO_20261028.md), las escrituras automáticas permiten seguir respondiendo; las operaciones explícitas coordinan y esperan toda la captura pendiente antes de confirmar. Conserva el UUID cuando pierde la respuesta o falla la confirmación. Una revisión/generación distinta inicia otra operación. El guardado por navegación espera también la operación pendiente; salir del formulario no lanza otra confirmación manual. No se añade localStorage ni un contador artificial de persistencia.

## Bitácora local, entrega y fallos

Los autoguardados conservan su registro `encuesta:guardar` en **`activity_logs` de TDV2**, con aplicación, actor validado, operación, resultado, fecha e ID de encuesta; sin respuestas ni secretos. Su UUID se deriva de aplicación/encuesta/revisión y se deduplica con el bloqueo y formato local ya utilizados por la entrega. Se comparte esa escritura en `DigitalActivityLog` con el espejo local de los demás eventos; **leer/escribir activity_logs nunca alimenta la cola Nexo**.

La bitácora automática se escribe dentro de la transacción TDV2 ya abierta para proteger la sesión, después de validar/guardar el diagnóstico y antes de confirmar. Un error previo al commit revierte ambas transacciones. Se mantienen las dos bases independientes: **no existe una transacción distribuida entre ellas**. Si falla la confirmación TDV2 después del commit del diagnóstico, la petición puede fallar con respuestas ya persistidas; el reintento exacto conserva la revisión y deduplica/completa el registro local. No se declara atomicidad distribuida ni se guardan respuestas en TDV2.

Para los eventos manuales se conserva la cola durable, el espejo local y el transporte real publicado `nexo_a{ApplicationId}_registrar_actividad(jsonb)`. El evento se confirma en la base del diagnóstico antes de intentar entrega; una caída central deja `ReceivedAt` nulo, estado de fallo y próximo intento, sin perder respuestas. Los reintentos mantienen UUID/payload y exigen el recibo correspondiente. El UUID de la pulsación es solo una clave interna; el payload central continúa limitado al contrato existente. Actor real y afectado se mantienen separados en los eventos de representación; una sesión representada no puede crear eventos de guardado.

Esta tolerancia corresponde a la entrega de actividad. Una falla que impida comprobar la autorización vigente de Nexo continúa denegando acceso; no se introducen permisos locales ni se reutilizan autorizaciones antiguas.

No se eliminan ni reclasifican registros o pendientes de versiones anteriores. Un vaciado de la cola puede entregar actividad manual/administrativa previamente pendiente; eso no es un evento nuevo generado por el autoguardado. Los eventos de guardado ya encolados por versiones anteriores conservan su historial y entrega. No se ejecutó ninguna limpieza sobre colas reales.

## Archivos

- `ClientApp/resources/js/competencias/Survey.tsx`: confirmación manual, coordinación con autoguardado y UUID de reintento; presentación sin cambios.
- `tdv2/Competencias/DigitalSurveyService.cs`: contrato/confirmación manual y autoguardado con auditoría exclusivamente local.
- `tdv2/Competencias/DigitalActivity.cs`: consulta idempotente, bitácora local compartida; transporte/recibos conservados.
- `tdv2/Controllers/CompetenciasController.cs`: endpoint autorizado de confirmación manual.
- `tests/TDV2.NativeVerification/DigitalManualSaveTests.cs`: siete casos nuevos con ASP.NET y PostgreSQL aislado.
- `tests/TDV2.NativeVerification/DigitalTests.cs` y `DigitalEditionActivityTests.cs`: registro de casos y expectativas de bitácora local/actividad manual.
- `ClientApp/tests/browser/digital-manual-save-ui.mjs`: dieciséis casos de interacción con React real y transporte sintético.
- `ClientApp/tests/browser/competencias-native.mjs`: el recorrido integrado espera la nueva confirmación manual, también cuando el autoguardado ya persistió los cambios.
- `ClientApp/tests/browser/page-flow.mjs`: la prueba de pie distingue entre contenido que cabe en la ventana y altura natural mayor por menú/firmas apiladas; no cambia estilos de la aplicación.
- README, ESTADO_MIGRACION, GUIA y nota anterior de instrumento/actividad: documentación vigente.

Sin nuevas migraciones, paquetes, conexiones, secretos ni permisos. Se reutiliza la tabla/índice de actividad ya implementados y los permisos existentes sobre `activity_logs`; las migraciones previas permanecen intactas. No se modifica Nexo, ni la corrección del 401, instrumento, metodología, informe, aviso o identidad visual.

## Verificación y límites

| Comprobación | Resultado |
| --- | --- |
| TypeScript y frontend | `tsc --noEmit` y build Vite correctos |
| ASP.NET / NativeVerification Release | Cero errores y cero advertencias |
| Pruebas frontend existentes | 95/95 correctas |
| Navegador de guardado manual | [16/16, cero errores JS](evidencia/guardado-manual-20261028/browser.json), dos tamaños |
| ASP.NET/PostgreSQL aislado | [55 casos de backend correctos](evidencia/guardado-manual-20261028/backend-initial.json), incluidos siete nuevos de guardado manual; el archivo conserva también el primer fallo visual explicado abajo |
| Navegador integrado con ASP.NET/PostgreSQL | [21 grupos correctos, cero errores JS](evidencia/guardado-manual-20261028/browser-integrated.json); [ejecutor sin fallos](evidencia/guardado-manual-20261028/integrated-run.json) al repetir el recorrido |

Los casos nuevos cubren autoguardados de perfil y cinco bloques con **cero filas de guardado en la cola y en el registro central sintético**, conservación/deduplicación local, confirmación manual sin cambiar una sola columna de la encuesta, doble envío concurrente, reintento tras una revisión posterior, CSRF/consentimiento/generación/versión inválidos, bloqueo de secciones, caída de la entrega central, recibo perdido, rollback de cola y restricciones de representación/finalizadas. La finalización conserva su evento y las pruebas existentes recorren accesos independientes, 401/OAuth, alcances, aviso, secuencia, informes históricos y protecciones administrativas.

La primera ejecución integrada terminó con 55 casos de backend correctos y una comprobación visual fallida: el arnés suponía que un menú completo y seis firmas apiladas cabían siempre en 900 px de alto, incluso a 320 px de ancho. Se corrigió exclusivamente la prueba para medir la altura natural y exigir que el pie quede al fondo **o después del contenido si no cabe**, sin relleno/desplazamiento artificial. No se modificó CSS ni se ocultó contenido para pasarla. En la repetición pasan los 21 grupos; a 320 px el estado breve con mensaje necesita 937,56 px, por lo que su desplazamiento es legítimo. La primera ronda del test nuevo de navegador también corrigió un selector de clase MUI antiguo por el rol accesible `alert`.

La recuperación de una confirmación perdida se prueba además cuando otra sesión ya modificó el perfil: conserva esa revisión y vuelve al primer paso accesible si el anterior quedó bloqueado. Este último ajuste del cliente se volvió a comprobar con tipos, build y los 16 casos del navegador sintético; no cambia el backend verificado. Artefactos locales: `.artifacts/manual-save-20261028`, `.artifacts/native-postgres-20261028-195327-5cc94601` (backend y primer recorrido) y `.artifacts/native-postgres-20261028-200413-bf2bcc18` (recorrido integrado repetido). Ambos clústeres fueron detenidos por el arnés.

Capturas de la interacción: [escritorio](evidencia/guardado-manual-20261028/guardado-manual-1440.png) y [móvil](evidencia/guardado-manual-20261028/guardado-manual-390.png).

Las pruebas de navegador usan Edge, React/MUI reales y transporte sintético controlable; comprueban escritorio y móvil (1440/390 px), no acreditan persistencia real. La suite nativa usa ASP.NET, PostgreSQL desechable y la función de actividad extraída textualmente del código local de Nexo sobre tablas/identidades sintéticas. **No acredita recepción institucional en Nexo**. No se lee configuración institucional ni se accede a bases existentes.

El clúster Windows usa el fallback ya documentado de `initdb` tras los mensajes 87/3 de token restringido. Se comprueba su creación e identidad antes de usarlo y se detiene al terminar; esos mensajes no equivalen al resultado de las pruebas. No se altera la política de seguridad del equipo.

Comandos desde la raíz, con .NET y Node/npm disponibles:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -NoBuild -DirectStart
Set-Location ClientApp
node tests/browser/digital-manual-save-ui.mjs
```

En Ubuntu, después de preparar explícitamente un clúster desechable y las variables del arnés según la guía de pruebas nativas, puede ejecutarse el mismo proyecto .NET y el script de navegador con Chromium compatible. No se afirma ejecución Linux en esta entrega. No hay paso operativo nuevo de migración ni permisos: permanecen los pendientes anteriores para habilitar la cola y contrato real de Nexo. Actualizar frontend y backend juntos para que el botón utilice la confirmación nueva; clientes anteriores seguirán guardando respuestas por PUT sin emitir actividad central de guardado. No hubo push ni despliegue.
