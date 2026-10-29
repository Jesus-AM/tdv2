# Encuesta sin bloqueo durante el autoguardado — 2026-10-28

**Política posterior vigente, 2026-10-29:** se conserva esta corrección de fluidez, pero se retira el reenvío del documento fallido. Un fallo detiene el autoguardado; la recuperación es exclusivamente manual, con datos actuales y control de versiones. [Mediciones SQL, nueva política y pruebas](AUTOGUARDADO_POSTGRES_Y_RECUPERACION_20261029.md). Las descripciones y evidencias de reintento automático siguientes son históricas.

Revisión posterior del mismo día: se retiró exclusivamente la explicación larga tras Guardar avance; permanece el estado breve en su espacio estable. Se repiten las pruebas de captura fluida y guardado manual junto con los nuevos accesos independientes. Las mediciones antes/después siguientes corresponden al ajuste original de fluidez, no a la latencia de autenticación. [Resultados de la revisión posterior](../migracion/ACCESO_POR_APLICACION_Y_MENSAJES_20261028.md).

## Diagnóstico medido antes de modificar la captura

Se revisaron `Survey.tsx`, su CSS, `DigitalSurveyService.Save/Write`, la respuesta `Saved` del controlador y la confirmación manual/cola de actividad. Git estaba limpio al iniciar; el trabajo de las entregas anteriores ya estaba incorporado en `6200241db3813618ab4fb20901405f78a86cb8c8`. No se restauraron archivos ni se alteraron paquetes, 401, instrumento, aviso, permisos o migraciones.

La reproducción usa Edge, los bundles React/MUI reales y el instrumento vigente, con identidad sintética y transporte HTTP local. Se añadió una demora controlada de **2000 ms** al guardado para separar el tiempo de red del trabajo del navegador. Cinco selecciones cercanas producen un solo autoguardado; se observa el formulario mientras llega la respuesta. Se miden tiempos HTTP con Resource Timing, JavaScript/layout con CDP Performance, commits React y continuidad de los nodos del formulario. La medida de selección termina tras dos `requestAnimationFrame`; incluye la espera de cuadros, no equivale solo al tiempo de ejecución de React.

La causa fue el uso de **`busy` para cada autoguardado**: deshabilitaba todos los campos y pasos hasta que terminaba la solicitud. Deshabilitar el control activo perdía el foco. Además, el mensaje «Guardando…» y el mensaje posterior aparecían/desaparecían encima del contenido, cambiando su posición y el aspecto de los controles. La respuesta reemplazaba todo el perfil y respuestas locales; permitir escribir sin corregir esto habría perdido selecciones posteriores.

No se observaron recargas completas, sustitución de los nodos ni claves que remontaran preguntas. JavaScript por selección estaba entre **2,09 y 5,69 ms**, con un commit React: no se encontró un cálculo costoso que justificara memoización general o cambios de metodología.

| Medida inicial | Escritorio 1440 px | Móvil 390 px |
| --- | ---: | ---: |
| Solicitud HTTP con demora de 2000 ms | 2018,2 ms | 2021,5 ms |
| Formulario deshabilitado por autoguardado | 2016,6 ms | 2018,2 ms |
| Desplazamiento del inicio de la sección | 34,5 px | 52,78 px |
| Selección hasta dos cuadros | 16,5–25,3 ms | 22,5–25,1 ms |
| Nodos de pregunta reemplazados | 0 | 0 |
| Foco al terminar | Perdido | Perdido |

Los tiempos HTTP anteriores **son de una red controlada de prueba**, no mediciones del servidor institucional. El servicio actual también puede intentar entregar actividad pendiente antes de responder; se conserva ese mecanismo y no se atribuye a Nexo una lentitud que no se midió contra él.

## Corrección

- El estado `savingNow` indica persistencia en segundo plano y no deshabilita campos. `busy` queda para operaciones explícitas que requieren una barrera: confirmar aviso/edición, guardar manualmente, cambiar de sección o finalizar.
- Se conserva la espera existente de **1500 ms desde la última edición**: agrupa selecciones cercanas, sin añadir una demora nueva. Solo hay un PUT en curso. Si se edita mientras responde, el siguiente documento usa la revisión confirmada y espera únicamente el tiempo restante de esa ventana. No se envía una petición por tecla o indicador.
- Una revisión local distingue la instantánea enviada de las ediciones posteriores. La respuesta actualiza progreso, navegación y versión confirmados; conserva el perfil/respuestas más recientes cuando siguen pendientes. El autoguardado no cambia el paso ni llama a `scrollIntoView`.
- Red/5xx conservan la solicitud exacta para recuperarla idempotentemente antes de enviar un documento posterior. Rechazos de validación permiten corregir el borrador. Un conflicto no se resuelve sobrescribiendo al otro navegador: se conservan las selecciones locales y se informa el problema.
- Si otro cliente escribe entre el commit y la lectura del servidor, la comparación semántica de la captura impide usar esa versión ajena para sobrescribirla con ediciones locales pendientes. No se agregan versiones artificiales ni reglas de autorización en el navegador.
- «Guardar avance», continuar/finalizar y salir hacia otro módulo esperan todos los cambios pendientes. La confirmación manual usa el endpoint, UUID y recibos existentes. La validación de secciones y la finalización siguen siendo del backend. Al desmontar el formulario se cancela su temporizador.
- Un indicador breve ocupa un espacio estable. Los errores conservan las respuestas y permiten reintentar; los avisos de validación mantienen el foco en el primer pendiente. No se alteran tarjetas, fuentes, colores, cabecera, pie o rúbricas.

## Auditoría conservada

El frontend solo llama a `POST /api/encuesta/guardar` por «Guardar avance». Los PUT automáticos, navegación y preparación de cierre siguen sin crear eventos de guardado en la cola Nexo. Se conserva auditoría local, finalización y otras acciones autorizadas, distinción actor/representado, prohibición de escritura por representación, entrega aplazada y recibos. No se modifica ningún servicio/backend ni se introduce otro transporte. Véase el [contrato de actividad manual](GUARDADO_MANUAL_ACTIVIDAD_20261028.md).

## Mediciones después de la corrección

Mismo navegador, bundles de producción, instrumento, ventanas y demora controlada. [Datos antes](evidencia/autoguardado-20261028/before.json) / [datos después y 16 escenarios](evidencia/autoguardado-20261028/after.json).

| Medida después | Escritorio 1440 px | Móvil 390 px |
| --- | ---: | ---: |
| Solicitud HTTP, misma demora de 2000 ms | 2013,2–2017,9 ms | 2013,1–2014,8 ms |
| Formulario deshabilitado por autoguardado | **0 ms** | **0 ms** |
| Desplazamiento del inicio de la sección | **0 px** | **0 px** |
| Cambio del scroll al confirmar | **0 px** | **0 px** |
| Selección hasta dos cuadros | 21,8–25,6 ms | 20,9–24,7 ms |
| Trabajo JavaScript por selección | 2,18–4,32 ms | 2,90–5,29 ms |
| Trabajo de layout por selección | 0,18–0,56 ms | 0,26–0,69 ms |
| Commits React por selección / nodos reemplazados | 1 / 0 | 1 / 0 |
| Foco al terminar | Conservado | Conservado |

No se afirma acelerar la red ni una mejora estadística de CPU: se eliminó la dependencia de la interacción respecto a la latencia. Las cinco selecciones iniciales siguen agrupándose en una petición. Después de corregir el bloqueo se pueden hacer otras dos selecciones mientras esa petición está pendiente: se envían juntas en una segunda petición, conservando el último cero válido. **Máximo de solicitudes simultáneas: una**. La selección sigue visible durante ambas.

Los 16 escenarios cubren escritorio/móvil, selección rápida, último cero persistido tras recarga, guardado manual durante autoguardado, HTTP 503, HTTP 409, respuesta perdida después del commit con ediciones posteriores, escritura de otra sesión entre commit y lectura, cancelación del temporizador tras conflicto y marcado/desmarcado de varios indicadores de una rúbrica. No crean confirmaciones manuales por autoguardar. También pasan los [16 casos de confirmación manual](evidencia/autoguardado-20261028/manual.json), incluidos doble clic, UUID de reintento y confirmación sin cambios.

La prueba extendida detectó un temporizador pendiente después de un conflicto: podía emitir otra solicitud y reemplazar el mensaje por un segundo error. Se corrigió cancelándolo tanto ante rechazo HTTP como ante una lectura concurrente incompatible. El escenario usa una respuesta retenida explícitamente y espera además a que transcurra la ventana de debounce para verificar que no existe otro envío. Una primera ejecución del caso lento también necesitó ampliar el límite del arnés: dos PUT y una confirmación, cada uno con 1800 ms controlados, superaban sus 5 segundos; se usa un límite de 15 segundos, sin cambiar las demoras.

Capturas con la selección y el diseño conservados: [escritorio](evidencia/autoguardado-20261028/survey-1440.png), [móvil](evidencia/autoguardado-20261028/survey-390.png); durante la solicitud pendiente: [escritorio](evidencia/autoguardado-20261028/saving-1440.png), [móvil](evidencia/autoguardado-20261028/saving-390.png). Se inspeccionaron las capturas y el navegador: mismas tarjetas/rúbricas y cabecera, sin controles sustituidos o saltos al confirmar.

## Verificación final

| Comprobación | Resultado |
| --- | --- |
| TypeScript / Vite | Correctos |
| ASP.NET / NativeVerification Release | Cero errores y cero advertencias |
| Pruebas frontend existentes | 95/95 correctas |
| Navegador con transporte sintético | 16 escenarios nuevos + 16 de guardado manual, cero errores JS |
| ASP.NET / PostgreSQL desechable | 55 casos de backend correctos y recorrido integrado correcto: [56 resultados del ejecutor, cero fallos](evidencia/autoguardado-20261028/native.json) |
| Navegador integrado con ASP.NET / PostgreSQL | [22 grupos correctos, cero errores JS](evidencia/autoguardado-20261028/integrated-browser.json) |
| Revisión del diff | Sin cambios de servidor, paquetes, configuración, migraciones ni recursos de identidad |

El recorrido integrado nuevo retiene la respuesta de un PUT **ya confirmado en PostgreSQL**, permite seleccionar otros niveles, pulsa Guardar avance durante esa espera y comprueba que el segundo PUT contiene el último cero válido. La confirmación manual espera esa revisión; al recargar se conserva el valor. [Captura integrada](evidencia/autoguardado-20261028/competencias-autoguardado-confirmado.png).

En ese recorrido, los dos PUT reales al ASP.NET/PostgreSQL local tardaron **569,17 y 467,62 ms**, medidos en `route.fetch` antes de retener artificialmente la primera respuesta. Estas son mediciones posteriores del entorno aislado; no se comparan con los 2000 ms controlados ni representan latencia de producción. La verificación incluye perfil y todos los bloques, validación negativa/URL/API, conflictos, respuesta perdida, retroceso, salida a otros módulos, finalización, informe inmutable, cuenta/representación, móvil 390/320 y regresión del portal y `/inicio`.

Los siete casos previos de auditoría manual se ejecutaron nuevamente con las bases de prueba y el contrato SQL de actividad extraído del código local de Nexo: cero eventos automáticos en cola/central sintético, evento manual sin modificar la captura, doble envío, error sin éxito, indisponibilidad de Nexo/entrega posterior, recibos perdidos, rollback y prohibición de representación/finalizadas. No es evidencia de recepción institucional.

Artefactos: `.artifacts/digital-autosave-20261028/`, `.artifacts/manual-save-20261028/browser/` y `.artifacts/native-postgres-20261028-203905-e2355af8/`. El clúster de esta ejecución fue detenido por el arnés. Se conservan resultados y capturas seleccionados en esta guía. No hay pendientes de implementación para este ajuste; queda fuera de la prueba la latencia y disponibilidad de servicios institucionales y la ejecución en Ubuntu.

## Archivos

- `ClientApp/resources/js/competencias/Survey.tsx`: estados separados, instantáneas/reintentos, coordinación de guardados y navegación.
- `ClientApp/resources/css/competencias-digitales.css`: espacio estable del indicador, limitado al subsitio.
- `ClientApp/tests/browser/digital-autosave-ui.mjs`: reproducción/mediciones y escenarios de red lenta, ediciones concurrentes, error y pérdida de respuesta, foco/scroll, recarga y rúbricas.
- `ClientApp/tests/browser/digital-manual-save-ui.mjs`: comprobación del nuevo indicador y botón disponible durante autoguardado.
- `ClientApp/tests/browser/competencias-native.mjs`: guardado lento con ASP.NET/PostgreSQL, continuación de captura y confirmación manual de la última selección; tiempos reales del transporte local aislado.
- README, ESTADO_MIGRACION, GUIA y guía de actividad manual: comportamiento actual y evidencia.

## Reproducción

Desde la raíz, con Node/npm y .NET disponibles:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -NoBuild -DirectStart
Set-Location ClientApp
node tests/browser/digital-autosave-ui.mjs
node tests/browser/digital-manual-save-ui.mjs
```

`--baseline` del script de autoguardado se utilizó **antes** de editar Survey; espera reproducir el bloqueo antiguo y no es un criterio válido para el código corregido. Los artefactos de esa medición se conservan por separado. El arnés nativo prepara exclusivamente un clúster desechable nuevo, con bases identificadas y fuentes/autenticación sintéticas. No usa User Secrets ni conexiones institucionales. El aviso 87/3 de `initdb` usa el fallback ya documentado; el arnés verifica el destino y detiene el clúster al terminar.

No hay migraciones, permisos, secretos o pasos nuevos de base de datos. No se ejecutan operaciones institucionales, push o despliegue. La latencia y recepción de Nexo institucional no se verificaron; los resultados de transporte sintético no acreditan esa integración real.
