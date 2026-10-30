# Sesión compartida de dos horas y textos de reportes · 2026-10-30

**Revisión posterior de los fallos adicionales:** los diez pendientes descritos abajo y otro caso relacionado se reprodujeron en una copia aislada del commit anterior `06e307c` y se corrigieron en los fixtures/expectativas de pruebas. Verificación final **37/37 casos afectados y relacionados**, sin cambiar producción ni la política de sesión. [Causa individual, comparación, archivos y resultados](REVISION_FALLOS_SUITE_20261030.md). La descripción de la suite interrumpida se conserva como evidencia histórica, no como estado pendiente de esos casos.

## Comportamiento implementado

Procedimientos institucionales y Competencias digitales comparten la autenticación local `tdv2.aspnet.session` y su ticket protegido en `tdv2_sessions`. El límite efectivo es **7200 segundos desde el inicio original**. La inactividad tiene también un límite de 7200 segundos: puesto que la última interacción nunca precede al inicio, el límite absoluto vence antes o al mismo tiempo que el de inactividad. Por ello no hacen falta latidos, escrituras por interacción ni una nueva tabla de actividad. Esta simplificación depende de que ambos límites sean iguales; no implementa una política futura de inactividad más corta.

Antes existían una cookie de dos horas con renovación deslizante y un temporizador del layout que volvía a empezar al montarse. El cambio elimina esa renovación y utiliza una referencia protegida al inicio original, conservada aunque se renueve técnicamente el ticket, se cambie de sitio o se entre/salga de representación.

- `SessionLifetime` crea el inicio, el plazo y un identificador aleatorio de coordinación. Inicio y principal deben coincidir; el reloj del servidor valida el límite exacto. El identificador público de coordinación no es el secreto ni el hash de la sesión.
- Las opciones efectivas de cookie se fijan también mediante `PostConfigure`: dos horas y `SlidingExpiration=false`. El login establece `AllowRefresh=false` y cookie no persistente. La serialización de cookie tiene precisión de segundos; sólo su envoltura redondea hacia arriba. La política y `expires_at` SQL conservan el límite exacto en milisegundos, sin conceder tiempo adicional.
- `PostgresTicketStore` rechaza tickets vencidos, futuros o sin referencia original válida. Una renovación no puede aumentar el vencimiento SQL ni recrear una sesión revocada. `AccessState` verifica además el plazo en guardas de escritura/lectura tardía; presencia revisa su vigencia y la conexión SignalR se cierra al vencer la autenticación.
- Autoguardados, consultas, presencia, Graph y renovación de tokens no modifican el plazo. No se introducen consultas de población ni Microsoft por respuesta.

## Vencimiento y recuperación voluntaria

`SessionExpiryMiddleware` invalida únicamente la sesión recibida, termina su contexto local y solicita finalizar su representación mediante el contrato existente. El fallo del cierre remoto no concede continuidad local. Se conserva la auditoría con actor real y representado; el cierre de Competencias reutiliza su cola/recibos existentes. No se ejecuta un cierre global Microsoft o Nexo.

Las API responden **401 JSON**, incluso si reciben `Accept: text/html`; no entregan la portada como JSON exitoso. Las páginas protegidas solicitadas como documento vuelven a `/`. Un marcador HttpOnly de sesión terminada impide que Atrás o una URL privada inicien automáticamente OAuth. La portada y el acceso explícito `/connect` permanecen públicos; un inicio correcto nuevo retira el marcador. Un **403** o **503** anterior al vencimiento conserva la sesión y no se interpreta como expiración.

El cliente utiliza el tiempo restante suministrado por el servidor y un reloj monotónico. Un temporizador común a ambos sitios, comprobaciones al volver a la pestaña/historial y coordinación entre pestañas realizan la salida oportuna. La comprobación al recuperar visibilidad está limitada a una por 30 segundos; no renueva la sesión. BroadcastChannel y su alternativa `storage` intercambian únicamente el identificador público de coordinación, nunca respuestas o credenciales. La aparición de un nuevo identificador retira vistas de la sesión anterior; las solicitudes ordinarias incluyen ese identificador y el servidor devuelve **409 `sessionChanged`** si otra pestaña ya inició una sesión diferente. Esta respuesta no revoca la nueva sesión.

Al vencer se cancelan solicitudes del cliente, se impiden nuevas operaciones, se desmonta sincrónicamente la vista privada y se hace `location.replace('/')`. Se limpian la identidad/foto y el estado en memoria. Se evita que los avisos de salida de formularios impidan ese retorno. **No se guarda al vencer**, no se reproduce una cola de respuestas y no se promete recuperación de cambios sin confirmación. Las respuestas ya confirmadas siguen en PostgreSQL y se consultan tras un acceso explícito nuevo. La política de suspensión del autoguardado tras un fallo y la auditoría de un único guardado manual permanecen intactas.

## Sesiones abiertas y operación

Los tickets anteriores no contienen un inicio original fiable: una fecha emitida por renovación deslizante no permite reconstruirlo. Se rechazan en su siguiente uso y requieren iniciar sesión explícitamente. No se ejecutó una invalidación masiva en una base institucional. Las sesiones nuevas conservan su inicio hasta vencer y no cierran sesiones independientes de otra persona o dispositivo.

**Sin migración, nuevo secreto, variable de entorno, permiso o cambio de Nexo/Azure.** La política está centralizada en `SessionLifetime.Duration`; no depende de `Session.IdleTimeout`. La configuración normal de HTTPS, Data Protection y conexión TDV2 sigue siendo necesaria y no se modifica. No se accedió a configuración privada ni a bases institucionales en esta revisión. La aplicación institucional y el despliegue quedan fuera de esta ejecución.

## Textos de reportes

Sólo se retiran «Vista institucional», la leyenda dinámica de agrupación de ambos reportes y el párrafo histórico solicitado de Resultados generales. El contexto seleccionado y los títulos se conservan. Avance muestra **«Porcentaje de avance»** en indicadores y columna: sigue siendo `100 × usuarios con envío final vigente / usuarios canónicos de Nexo con acceso vigente en el mismo alcance`. No cambian población, filtros 2–5, históricos, consultas, madurez o autorización. Las denominaciones anteriores de documentos históricos describen la etiqueta de su fecha.

## Archivos de esta revisión

- Backend: `Security/SessionLifetime.cs`, `Security/PostgresTicketStore.cs`, `Security/AccessState.cs`, `Web/SessionExpiryMiddleware.cs`, `Web/DomainProblemResponse.cs`, `Web/AccessBoundary.cs`, `Web/PageResponse.cs`, `Hosting/ServiceRegistration.cs`, `Hosting/ApplicationPipeline.cs`, `Controllers/AuthenticationController.cs`, `Controllers/SessionController.cs`, `Controllers/CompetenciasController.cs`, `Controllers/FormHub.cs`, `Services/SessionTerminationService.cs` (dentro de `tdv2/`).
- Frontend: `lib/session-deadline.ts`, `lib/session-lifetime-client.ts`, `lib/navigation.tsx`, `types/page.ts`, `Layouts/AuthenticatedLayout.tsx`, `competencias/Aggregates.tsx`; guardas de salida de `competencias/Survey.tsx`, `pages/FormatoUR.tsx` y `pages/ConfiguracionProcesos.tsx` (dentro de `ClientApp/resources/js/`). No se modifica el diseño ni los estilos institucionales.
- Pruebas: `DigitalSessionLifetimeTests.cs`, `NativeApplication.cs` (reloj controlado), `DigitalTests.cs` (registro y selector de navegador), autenticación sintética del proyecto Verification, `NativeTests.cs` (caso externo ajustado a la resolución UPN/alias vigente), `session-expiry-native.mjs`, `digital-registered-counts-ui.mjs`, `navigation.test.mjs`, `session-deadline.test.mjs`. Se utiliza el runner existente `Test-NativePostgres.ps1`.
- Documentación: este documento, README, ESTADO_MIGRACION, GUIA e INVENTARIO. El resto del trabajo local, incluidas migraciones previas pendientes, se conserva.

## Verificación reproducible y límites

Todas las escrituras y controles de reloj utilizan bases PostgreSQL desechables creadas por `scripts/Test-NativePostgres.ps1`; Microsoft y Nexo son fixtures sintéticos. ASP.NET, cookies/tickets, SQL PostgreSQL, React compilado y Edge son reales. No se cambia el reloj del equipo ni se espera dos horas.

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build

$env:TDV2_TEST_CASE_PREFIX = 'Sesión dos horas'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX

$env:TDV2_TEST_SESSION_UI = '1'
# Si node no está en PATH, definir TDV2_TEST_NODE con el ejecutable local.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -BrowserOnly
Remove-Item Env:TDV2_TEST_SESSION_UI
```

Las pruebas controladas comprueban el milisegundo anterior y el límite exacto, incluso con inicio fraccionario; polling sin ampliación; cambio de sitio; representación/cierre propio; renovación/revocación; tickets antiguos; 401 JSON frente a 403/503; petición anterior frente a login nuevo. Navegador recorre ambos sitios a **1440, 900 y 390 px**, dos pestañas, vencimiento sin OAuth, retorno a la portada, Atrás, nuevo acceso explícito y ausencia de escrituras/reenvíos de la captura pendiente.

| Comprobación | Resultado |
|---|---|
| Solución Release, sin restaurar paquetes | Correcta, 0 advertencias y 0 errores |
| TypeScript, Vite | Correctos |
| Frontend | 105 pruebas correctas; incluye reloj, 401 sin OAuth y bloqueo de nuevas solicitudes |
| Reglas y HTTP `TDV2.Verification` | 116/116 correctas; dobles en memoria donde corresponde |
| Regresión Competencias con PostgreSQL | 84/84 correctas, incluido guardado manual/autoguardado, identidad, reapertura, históricos y permisos; esta ejecución precedió al refuerzo del identificador entre pestañas |
| Sesión, binario final y PostgreSQL | Cinco grupos específicos correctos; [resultado](evidencia/sesiones-20261030/sesion-verification.json) |
| Navegador, binario y React compilados | Seis combinaciones sitio/tamaño correctas, sin errores JavaScript; [resultado](evidencia/sesiones-20261030/session-expiry-ui.json) |
| Cuenta totalmente externa / object ID inválido | Correcta tras ajustar el fixture a las reglas UPN/alias vigentes; [resultado](evidencia/sesiones-20261030/identidad-negativa.json) |

La suite nativa ampliada de Procedimientos **no quedó verde** y se detuvo después de fallar casos de fixtures/edición/sincronización fuera de esta corrección. No se declara una regresión general aprobada ni se atribuyen todos los fallos a una causa confirmada. Se observaron, entre otros: `Central/concesión sin rol…` (envío del fixture), `Participación/enviado…` (guardado del fixture), varios `Edición/…` y `Sync/web: doble solicitud…` (PostgresException al preparar el caso). El caso `Rendimiento/ILDA…` espera dos lecturas, pero el código existente en HEAD ya tiene tres: metadatos, inventario y exclusiones. Esa expectativa no se cambió ni se eliminó una lectura funcional. El negativo Microsoft anterior cambiaba `mail` y conservaba UPN institucional verificado; se corrigió sólo ese dato sintético, conforme a la corrección de alias ya implementada, y se comprobó nuevamente. Estos límites quedan separados de los resultados de sesión y Competencias anteriores.

La compilación final de la solución fue estándar. Para evitar disputar DLL de la suite ampliada que estaba en ejecución, algunas comprobaciones finales se ejecutaron con una compilación equivalente en `.artifacts/session-verification-final/` y una copia temporal del runner que sólo cambia la ruta del binario; no altera SQL, configuración ni casos de prueba. La última prueba específica usa nuevamente el runner y la salida Release estándar. Los clústeres desechables se detuvieron al concluir o interrumpir su prueba.

## Evidencia visual

Capturas exclusivamente de identidades y publicaciones sintéticas, inspeccionadas en navegador compilado. No muestran cuentas institucionales. El estado previo conserva cabecera, cuenta, controles y pie; el estado posterior es la portada pública real, sin identidad privada ni redirección automática de autenticación.

- Procedimientos: [vista previa de escritorio](evidencia/sesiones-20261030/sesion-procedimientos-antes-1440.png), regreso a portada en [escritorio](evidencia/sesiones-20261030/sesion-procedimientos-portal-1440.png) y [móvil](evidencia/sesiones-20261030/sesion-procedimientos-portal-390.png).
- Competencias: [encuesta móvil antes de vencer](evidencia/sesiones-20261030/sesion-competencias-antes-390.png), regreso a portada en [escritorio](evidencia/sesiones-20261030/sesion-competencias-portal-1440.png) y [móvil](evidencia/sesiones-20261030/sesion-competencias-portal-390.png).
- Reporte sin leyendas retiradas y nueva etiqueta: [escritorio](evidencia/sesiones-20261030/sesion-avance-1440.png), [móvil](evidencia/sesiones-20261030/sesion-avance-390.png). Resultados generales comprueba ausencia de las dos leyendas con aserciones del mismo recorrido.

No se declara validado Microsoft/Nexo institucional ni el comportamiento de dispositivos físicos: móvil/tableta son ventanas de navegador automatizado. La expiración del servidor es obligatoria aunque la red o el navegador suspendan temporalmente el temporizador; una pestaña vuelve a comprobar al hacerse visible.
