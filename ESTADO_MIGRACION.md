# Estado de migración TDV2

Inicio: 2026-09-30. Última adaptación: **2026-10-09**. Proyecto convencional ASP.NET Core 10/EF Core/PostgreSQL/React implementado y verificado en el alcance descrito. **La aceptación institucional y el despliegue Ubuntu continúan pendientes; no se declara terminada la sustitución operativa de Laravel.**

## 2026-10-09 — Revisar y enviar por etapa

**Implementado:** pestaña **Revisar y enviar** después de Sistemas y herramientas, accesible con pendientes y sin sumar avance. Reutiliza la revisión, el diálogo y los servicios existentes. Presenta Primera etapa, área/ejercicio, estado de las tres secciones, resumen de procedimientos/herramientas/medios, autoguardado y enlaces por sección, registro y campo. Conserva Contexto inicial, recuperación de ubicación, tablas compactas, ayudas, presencia, reservas y eliminación directa. Se corrigió una carrera reproducida al seguir un pendiente: ahora termina el guardado/liberación anterior antes de enfocar y reservar el destino.

**Enviar primera etapa** conserva permisos y añade comprobación explícita de etapa. Se habilita sólo con respuestas completas y confirmadas, sin errores ni reservas ajenas. D/N históricos continúan pendientes. El diálogo explica el bloqueo y queda invalidado ante cambios remotos de versión/etapa o edición ajena. El servidor revalida requisitos, permisos, contexto, CSRF, ejercicio, versión y reservas dentro de la transacción existente. Petición única en curso y recibo idempotente evitan duplicados; envío, recibo y auditoría son atómicos. Tras confirmar se muestra **Primera etapa enviada**, fecha y nombre del autor efectivo.

La migración nueva **`20261009171656_StageSubmissions`** añade etapa activa y envíos independientes con instantánea parcial, fecha, actores, nombre, versión y operación. Triggers y servicio protegen cada etapa enviada. No atribuye envíos globales anteriores a una etapa: sus datos, autores, fechas, auditorías y bloqueo permanecen intactos. No borra respuestas posteriores. La segunda etapa permanece deshabilitada y sin definición de campos/requisitos; su futuro envío no heredará el de la primera. [Contrato, persistencia y operación](docs/migracion/REVISAR_ENVIO_ETAPAS.md).

**Probado:** TypeScript, Vite y compilación .NET correctos; **80/80** pruebas frontend (incluyen doble clic, reintento y SignalR anterior al ACK) y **76/76** dominio/HTTP con dobles. **57/57** comprobaciones de migración en PostgreSQL 18 desechable: esquema aditivo, modelo/snapshot, aplicación inicial/repetida, scripts idempotentes, Up/Down sin contenido, rechazo de reversión con enviados, protección y conservación de históricos. Las expectativas de las suites antiguas se ajustaron a la columna aditiva `etapa_activa`; las migraciones anteriores no cambiaron. [Resultados EF](docs/migracion/revisar-enviar-20261009/migraciones.json).

**56 casos de regresión nativa y 10 casos de etapas comprobados:** responsables/administradores/colaboradores/consulta, rechazo de UR ajena, D/N, revisión incompleta, ubicación sin avance, primera etapa sin requisitos posteriores, solicitudes concurrentes y reintentos, reservas/versiones obsoletas, fallo de auditoría con rollback, históricos y separación de etapas. La corrida completa inicial dio 65/66: la prueba nueva esperaba incorrectamente que consulta escribiera su ubicación; el servidor conservó su HTTP 403. Se corrigió esa expectativa y se repitieron las diez pruebas de etapas: **10/10**, sin ampliar permisos. [Corrida inicial](docs/migracion/revisar-enviar-20261009/regresion-inicial.json), [repetición de etapas](docs/migracion/revisar-enviar-20261009/etapas-postgresql.json).

**7/7 recorridos de revisión y 11/11 de colaboración en Edge → ASP.NET → PostgreSQL/SignalR reales**, con dos identidades sintéticas distintas y contextos separados: revisión incompleta, enlaces/foco, recuperación de pestaña, colaboración sin envío, guardado retenido, cambio remoto durante confirmación, rechazo directo del backend, doble clic/reintento, recarga con fecha/autor/bloqueo, tablas, reservas, primera escritura/pegado/Tab, eliminación, Medios independiente y reconexión/vencimiento. Escritorio 1440 px, móvil 390/320 px y teclado; sin errores JavaScript. Capturas revisadas: [escritorio](docs/migracion/revisar-enviar-20261009/stage-review-1440.png), [móvil](docs/migracion/revisar-enviar-20261009/stage-review-390.png), [envío confirmado](docs/migracion/revisar-enviar-20261009/stage-submitted-mobile.png). [Revisión](docs/migracion/revisar-enviar-20261009/revision-navegador.json), [colaboración](docs/migracion/revisar-enviar-20261009/colaboracion-navegador.json).

**Pendiente/límites:** migración sin aplicar a bases institucionales; segunda etapa sin habilitar; dispositivos físicos y aceptación institucional pendientes. Microsoft/Nexo son pares sintéticos; las pruebas unitarias sí simulan transporte. En navegador las carreras retienen solicitudes reales y la desconexión se emula, sin reemplazar la API de edición. La independencia de una segunda etapa se comprueba sólo con persistencia y datos sintéticos: no demuestra una API de captura de segunda etapa habilitada. Sin acceso institucional ni cambios en Nexo, Herd o credenciales; sin push ni despliegue.

Aplicación **posterior y explícita por el operador**, desde la raíz y sobre el destino previamente verificado/configurado:

```powershell
dotnet ef database update 20261009171656_StageSubmissions --project tdv2 -- --environment Development
```

Development utiliza User Secrets vigentes sin modificarlos. No hay migraciones al arrancar. [Comandos reproducibles de pruebas, revisión SQL y operación](docs/migracion/REVISAR_ENVIO_ETAPAS.md).

## 2026-10-09 — Icono de ayuda en la instrucción de Identificación

**Implementado:** `IdentificationInstructions.tsx` sustituye únicamente el carácter ⓘ por `HelpOutlineOutlined` de MUI, el mismo componente utilizado en `FieldHelp.tsx`: 19 px, `text.primary` y alineación vertical al centro del texto. Referencia visual sin interacción, con nombre accesible «Ayuda». Conserva el párrafo y «procedimientos institucionales» en negritas.

**Probado:** TypeScript (`npm.cmd --prefix ClientApp run types:check`) y Vite (`npm.cmd --prefix ClientApp run build`) correctos. Edge a 1440/390/320 px con transporte sintético, sin bases: SVG idéntico al botón real, tamaño y color iguales, negritas de peso 700, icono dentro del párrafo y sin desbordamiento ni errores JavaScript. Capturas revisadas y métricas en `.artifacts/instruccion-ayuda-20261009/`.

**Pendiente/límite:** móvil emulado, sin validación en dispositivo físico. Sin cambios de lógica, permisos, datos o credenciales; sin push ni despliegue.

## 2026-10-09 — Acceso público y validación Vigente/Ajustar

**Implementado:** el botón principal del portal muestra «Registrar un procedimiento institucional», con texto adaptable al ancho y flecha alineada. Conserva exactamente el destino condicional `/connect` o `/inicio` y la autenticación. Identificación general ofrece sólo `V · Vigente` y `A · Ajustar`; «Selecciona una opción» es indicativo, sin entrada seleccionable. Se mantienen las instrucciones de eliminar registros ajenos al área.

D/N existentes conservan su etiqueta y, en formatos editables, muestran «Validación pendiente de actualizar». El servidor sólo los admite sin modificación cuando coinciden con el valor persistido del mismo registro, dentro de la operación de escritura existente. Permite guardar otros campos de esa fila y otros bloques; rechaza nuevas asignaciones, cambios D↔N y copias a nuevos IDs. La regla compartida de elegibilidad exige V/A en avance, revisión, relaciones y envío; corregir a V/A o eliminar utiliza reservas/autoguardado/retiro existentes. No hay conversión, limpieza ni migración nueva; enviados e históricos bloqueados conservan contenido e instantáneas. [Contrato de captura](docs/migracion/IDENTIFICACION_TABLA.md).

**Probado:** TypeScript y compilaciones Vite/ASP.NET correctas; **78/78** pruebas de frontend y **76/76** de dominio/HTTP con dobles. **5/5** casos nuevos ASP.NET → PostgreSQL 18 nativo desechable: rechazo HTTP 422 de D/N en filas actuales y nuevas, atomicidad de varios bloques, conservación al editar otros campos, pendientes/avance y rechazo de envío, corrección V/A persistida y envío permitido, eliminación directa y lecturas/escrituras bloqueadas de enviados e históricos con comparación íntegra de la fila almacenada. [Resultados PostgreSQL](docs/migracion/validacion-procedimientos-20261009/postgresql.json); el contador del runner incluye además un grupo de navegador.

**6/6** recorridos Edge → ASP.NET → PostgreSQL: acceso desde el portal antes/después de autenticar, botón completo a 1440/390/320 px, menú exactamente V/A, indicativo no seleccionable, reserva, autoguardado, guardado/recarga, D/N legibles y pendientes, edición de otros campos sin conversiones, corrección por mouse/teclado, eliminación directa y enviado de consulta. **11/11** recorridos de regresión con dos identidades sintéticas distintas: exclusión por fila, edición simultánea de filas distintas, fotografías/presencia, primera escritura/pegado/Tab, actualización SignalR, eliminación libre/ajena, renovación tardía, Medios independiente, menús/ayudas, otra pestaña, desconexión/reconexión/caducidad y envío de primera etapa. Cero errores JavaScript. [Navegador](docs/migracion/validacion-procedimientos-20261009/navegador.json), [colaboración](docs/migracion/validacion-procedimientos-20261009/colaboracion.json). Capturas finales revisadas en `.artifacts/native-postgres-20261009-163112-0531b958/`.

**Pendiente/límites:** móvil/tacto emulados, sin comprobación en dispositivos físicos ni con identidades institucionales. Microsoft es un par HTTP sintético y Nexo vistas/funciones locales de prueba; la captura, las reservas y la persistencia no usan transporte simulado. La regresión de carrera retiene solicitudes reales y la de desconexión emula pérdida de red. Recursos externos del portal bloqueados durante la verificación, referencias de fuentes conservadas. Sin consultas ni cambios a bases/fuentes institucionales, Nexo, Herd o credenciales. Sin push ni despliegue; sin migraciones que aplicar por este ajuste.

Comandos reproducibles desde la raíz (Node instalado con Visual Studio; cada ejecución nativa crea y detiene su clúster aislado):

```powershell
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:PATH=(Split-Path $env:TDV2_TEST_NODE)+';'+$env:PATH
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_CASE_PREFIX='Edición/validación:'
$env:TDV2_TEST_BROWSER_FLOW='validation'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -DirectStart
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_BROWSER_FLOW='delivery'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

## 2026-10-09 — Contacto público compacto

**Implementado:** únicamente el bloque Contacto y sus estilos en `tdv2/Views/Home/Index.cshtml`: Subdirección de Inteligencia de Datos, enlace `mailto:sid@uacj.mx`, «Extensiones telefónicas: 4374, 4274, 4174» y «Lunes a viernes, 8:00 a 15:00 h». Se retiran de esa tarjeta Universidad, Dirección y nombres/títulos personales. Encabezado, colores y tipografía conservados; separación de 8 px entre datos, sin márgenes adicionales ni estilos de elementos retirados.

**Probado:** Razor/ASP.NET compilado y servido en aislamiento; Edge a 1440×1000, 390×844 y 320×740, contenido exacto, mailto, tarjeta compacta, ausencia de desbordamientos y capturas revisadas. Comprobación SHA-256 antes/después confirma que el resto de la portada, el layout autenticado y el PNG no cambiaron: logo de 440 px, Dirección debajo y footers centrados conservados. Evidencia local en `.artifacts/contacto-20261009/`.

**Pendiente/límite:** móvil emulado y recursos externos bloqueados durante la comprobación, sin alterar sus referencias. Sin bases, credenciales ni servicios institucionales; sin cambios funcionales, push o despliegue.

## 2026-10-09 — Logo público de 440 px y footers centrados

**Implementado:** `.uacj-logo` pasa de 380 a 440 px conservando `max-width: 100%` y altura automática. El PNG original permanece intacto; Dirección debajo, alineada a la izquierda y sin raya decorativa externa. La vista pública centra el texto del footer y `AuthenticatedLayout.tsx` centra el footer compartido con flex y alineación textual. Ambos mantienen únicamente «Universidad Autónoma de Ciudad Juárez», fuente y espaciado compacto. Sin cambios funcionales.

**Probado:** compilación Razor/ASP.NET, TypeScript y Vite correctos. Edge a 1440, 390 y 320 px: logo de 440 px en escritorio, proporción original y ajuste móvil sin desbordamiento, Dirección alineada y líneas del footer público centradas. Inicio, Configuración y Formato con transporte sintético verifican el footer compartido en los mismos tres anchos: centrado con desviación menor a 1 px, fuente de 11 px y padding de 16 × 24 px conservados. Texto exacto, imágenes locales sin errores y SHA-256 del logo servido igual al original. Capturas revisadas; evidencia local en `.artifacts/logo-footer-20261009/`.

**Pendiente/límite:** móvil emulado; fuentes y fondo externos bloqueados durante la comprobación y sus referencias conservadas. Portada servida por ASP.NET aislado sin configuración institucional ni servicio de sincronización; área autenticada con transporte sintético, sin bases. No se modificaron funcionalidades, credenciales ni bases; sin push ni despliegue.

## 2026-10-09 — Sincronizaciones manuales desde el proceso web

**Implementado:** `ManualSyncWorker` integra la cola manual en el ciclo de vida de ASP.NET mediante `BackgroundService`, consulta persistente cada dos segundos y señal tras commit. Reutiliza `SyncCoordinator`, reservas de 30 minutos, recuperación, publicación y auditoría; no hay tareas sueltas en el endpoint. El POST devuelve 202 sin esperar descargas y el servidor excluye solicitudes incompatibles. `manualOnly` no crea horarios, no cambia su configuración ni suplanta el latido del CLI. Web y procesadores externos comparten la misma reserva. Sin nueva migración, variables, credenciales ni servicio de despliegue adicional.

React distingue «Iniciando…» de «Sincronizando…» según confirmación real, actualiza estado/contadores por catálogo sin recarga y protege dobles clics y ACK perdido. Error local del procesador y demoras de inicio aparecen de forma contextual; el trabajo aceptado se conserva y se reintenta consultar la misma cola, sin duplicarlo. El administrador puede solicitar otro trabajo cuando el anterior haya terminado. Permisos, CSRF, contexto, representación, SII UR/módulos e ILDA independiente se conservan. No se modifican formularios ni respuestas.

**Probado:** **44/44** casos sobre PostgreSQL nativo desechable, incluidos nueve nuevos con el hosted service real: horarios pausados/vencidos, HTTP breve durante lectura retenida, ILDA deshabilitada, doble solicitud y dos administradores distintos, proceso CLI hijo concurrente, fallos separados de los tres catálogos, error al reclamar con recuperación de la misma solicitud, pendiente sin señal, terminación real de proceso web hijo/reinicio/caducidad y rechazos directos de autorización/CSRF/representación/revocación. La caducidad se adelanta únicamente en la base sintética; no se espera media hora real. **76/76** dominio/HTTP con dobles, TypeScript y compilación Vite/ASP.NET correctos.

**6/6** recorridos nuevos Edge → ASP.NET → PostgreSQL con servicio hospedado real: inicio manual sin CLI, doble clic, ACK demorado, recarga/navegación/cierre de página, fallo parcial, diagnóstico de cola, pérdida de ACK/consulta, recuperación y escritorio/móvil emulado. **13/13** recorridos de regresión: configuración versionada, horarios con/sin ILDA, permisos, catálogos y conservación de respuestas. En esta regresión histórica el host automático se retira sólo del verificador y los ciclos se invocan explícitamente para observar cada estado; no es evidencia de autoarranque. Ninguna ejecución de navegador tuvo errores JavaScript. [Cola y reservas](docs/migracion/sincronizacion-manual-20261009/postgresql.json), [servicio web y navegador](docs/migracion/sincronizacion-manual-20261009/navegador-manual.json), [regresión](docs/migracion/sincronizacion-manual-20261009/navegador-regresion.json).

**Pendiente/límites:** SQL Server/MySQL son lectores ADO.NET sintéticos, Microsoft un par HTTP simulado y Nexo vistas/funciones locales sintéticas. Sólo los casos de ACK/red demoran o pierden respuestas HTTP reales; no simulan la cola, el servicio, publicaciones o reservas. Faltan SII/ILDA institucionales, dispositivos móviles físicos y operación Ubuntu/supervisor/SIGTERM. No se leyeron credenciales ni se accedió a fuentes/bases institucionales, Nexo o Herd. No hubo push, despliegue ni migración aplicada fuera del aislamiento.

Comandos de esta comprobación (Node de Visual Studio ya instalado; cada script crea y detiene su propio clúster):

```powershell
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SyncOnly -DirectStart
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:TDV2_TEST_BROWSER_FLOW='sync-manual'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
$env:TDV2_TEST_BROWSER_FLOW='sync'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
```

Operación vigente y cambios de despliegue documentados en [Sincronizaciones](docs/migracion/SINCRONIZACIONES.md), [Windows](docs/ARRANQUE_LOCAL_WINDOWS.md) y [Ubuntu](docs/PUBLICACION.md). Basta el botón de Configuración → Sincronizaciones con ASP.NET en ejecución; los horarios habilitados continúan usando su procesador externo. Sin nuevos comandos de migración para este ajuste.

## 2026-10-09 — Footer compartido del área autenticada

**Implementado:** `ClientApp/resources/js/Layouts/AuthenticatedLayout.tsx` muestra únicamente «Universidad Autónoma de Ciudad Juárez». Conserva posición, tipografía de 11 px, espaciado y estilos originales. El mismo layout sirve a todos los módulos y submódulos. La portada pública mantiene exactamente su contenido anterior (SHA-256 verificado antes/después); sin cambios en permisos, formatos, colaboración ni persistencia.

**Probado:** TypeScript y build Vite correctos. Edge con transporte sintético local: Inicio, Configuración y Formato en 1440×900, 390×844 y 320×740 (9 comprobaciones). Un solo footer, texto exacto, después del contenido, altura de 49.5 px, sin desbordamiento horizontal ni errores JavaScript. Capturas de escritorio y móvil revisadas; evidencia local en `.artifacts/authenticated-footer-20261009/`.

**Pendiente/límite:** móvil emulado, sin comprobación en dispositivos físicos ni autenticación institucional. Sin acceso a bases, Nexo, Herd o credenciales; sin push ni despliegue.

## 2026-10-09 — Ajuste exclusivo de la portada pública

**Implementado:** únicamente `tdv2/Views/Home/Index.cshtml` cambia comportamiento público. Se retiran Proyectos estratégicos, Indicadores de avance, sus enlaces/estilos exclusivos y la FAQ que remitía al tablero retirado. Se elimina el nombre de la Subdirección y la etiqueta «Proyecto institucional · UACJ». El logo existente `/images/logos/firmaLogotipo-blancosinfondo-3.png` aparece antes de la Dirección, ambos a la izquierda, con 18 px de separación. Logo de 380 px y máximo 100%, altura automática, proporción/transparencia/colores intactos; desaparecen borde decorativo y sangría de la Dirección. Footer compacto con únicamente «Universidad Autónoma de Ciudad Juárez». Se conserva Transformación Digital, la configuración tipográfica y el acceso `/connect`. No cambia React autenticado ni backend de permisos/formatos.

**Probado:** compilación Razor/ASP.NET Release; portada servida por ASP.NET con configuración vacía y protección efímera, sin bases ni servicios institucionales. Edge en 1440×1000, 390×844 y 320×740: sin desbordamiento horizontal, texto retirado ausente, destinos de sección existentes, imágenes locales cargadas, proporción/alineación y footer verificados. HTTP 200 `image/png` desde la ruta exacta; bytes idénticos al original SHA-256 `0bac7d97c01a83dcb4c3c7233947ea4bd0b6151d6c4eb223b4a9baf3254481bf`. `StaticWebAssetsPrepareForPublish` incluye el PNG como `All / PreserveNewest`, sin modificar el proyecto ni publicar. [Resultado](docs/migracion/portal-publico-20261009/verification.json), [publicación](docs/migracion/portal-publico-20261009/logo-publish.json), [escritorio](docs/migracion/portal-publico-20261009/desktop.png), [móvil](docs/migracion/portal-publico-20261009/mobile.png), [footer](docs/migracion/portal-publico-20261009/mobile-footer.png).

**Pendiente/límite:** las capturas no cargaron Google Fonts ni el fondo remoto de Unsplash por restricciones de red del entorno; sus referencias originales permanecen sin cambios y se muestran las alternativas del navegador. No se probó autenticación institucional ni se accedió a bases, Nexo, Herd o credenciales. Sin migraciones, push ni despliegue. Los documentos con enlaces `#` preexistentes conservan su estado; no se inventaron URLs de documentos.

## 2026-10-09 — Desplazamiento sin liberar, columna Edición y alturas compactas

### Implementado

Se conservaron los cambios locales y el contrato anterior de entrega, permisos y persistencia. Se reprodujo que arrastrar la barra horizontal nativa movía el foco del textarea al contenedor DIV; `onBlurCapture` lo interpretaba como salir de la fila y llamaba a guardar/liberar. `RecordEditingContext` utiliza destinos de foco y clics reales, distingue barras/gestos y conserva el contexto ante blur sin destino. No fuerza foco ni renueva por desplazamiento. Otro registro/sección conserva el guardado y liberación confirmados; ayudas y menús pertenecen al contexto original.

`RowEditingPresence` ocupa la primera columna **Edición**, fija y de 124 px, con fotografía/iniciales, color y estado textual; sustituye expresamente la posición anterior en Acciones. Eliminar queda separado y arriba, con área de 44 × 44 px, icono rojo, nombre «Eliminar registro», foco y explicación accesible al deshabilitarse. Se conserva la eliminación directa atómica y el tratamiento de renovaciones tardías. Multilínea: dos a cuatro líneas visibles y scroll interno, íntegro también en consulta; ¿Qué entrega? amplía su mínimo a 280 px sin cambiar la fuente. Las dos tablas dejan el scroll vertical a la página y conservan horizontal propio.

### Verificado

- Reproducción anterior con barra nativa, ASP.NET y PostgreSQL desechable: reserva propia → libre al desplazar. Traza conservada.
- **9/9** recorridos nuevos de barras, selección/cursor, texto largo, lectura enviada, presencia, eliminación, teclas/menús/ayudas, SII/Otra/Otro y móvil/tacto emulado, con **dos identidades distintas** y transporte real de escritura/reservas/SignalR.
- **11/11** regresiones E2E: carrera de usuarios, otra pestaña, filas independientes, actualización entre usuarios, Medios, conexión/vencimiento, eliminación/renovación concurrentes, envío de primera etapa e inmutabilidad.
- **51/51** comprobaciones de edición/autorización en PostgreSQL nativo aislado: atomicidad, concurrencia, permisos, testigos, relaciones, primera etapa e inmutabilidad.
- **78/78** frontend con transporte simulado donde corresponde y **76/76** dominio/HTTP con dobles; TypeScript y build Vite correctos. No se confunden con los recorridos reales anteriores.

[Causa, resultados, archivos y comandos de repetición](docs/migracion/DESPLAZAMIENTO_TABLAS.md). No hay nueva migración ni cambios al modelo de persistencia en este ajuste.

### Pendiente

Mouse/panel táctil/pantallas táctiles físicos, navegadores móviles reales y fuentes institucionales. Las pruebas sólo acreditan aislamiento sintético y emulación. Sin accesos ni cambios a Nexo, Herd, credenciales o bases institucionales; sin push ni despliegue. Las migraciones anteriores siguen requiriendo aplicación explícita del operador, nunca arranque web.

## 2026-10-09 — Entrega hasta Sistemas, elegibilidad, foco y diagnóstico SII

### Implementado

Se conservó la versión local y todos sus cambios. Identificación y Sistemas mantienen tablas, 13 px, encabezados en negritas, contenido multilínea, fotografías y reservas por registro. El doble foco procedía de los outlines de `input/textarea` dentro de tablas más el fieldset enfocado de MUI: se retira únicamente el outline interior de `MuiOutlinedInput`, conservando foco de teclado y contorno de presencia. `RowEditingActions` reserva el mismo espacio en Acciones de ambas tablas; el avatar muestra el nombre también al enfocar, sin cambiar el foco lógico de edición.

La entrega y el avance vigentes comprenden Contexto, Identificación y Sistemas con Medios. El backend reutiliza esa revisión para enviar, conserva permisos intrínsecos del responsable, control de versión/reservas, snapshot e inmutabilidad. Mostrar secciones posteriores como administrador no amplía requisitos. Se conservan sus respuestas, sin convertirlas en completadas ni normalizarlas en guardados ajenos. Medios es una sección independiente dentro de Sistemas y usa su propia reserva/autoguardado. El campo vigente `Otro medio` no tenía placeholder de CAST; continúa sin ejemplo y la opción Portal del CAST permanece en herramientas.

`ProcedureEligibility` centraliza campos obligatorios actuales, validez y V/A. Lectura/ACK entregan opciones confirmadas; SignalR actualiza las otras sesiones. La escritura comprueba disponibilidad dentro de la transacción. Los borradores incompletos se guardan, pero no se ofrecen para nuevas relaciones. Un vínculo que después quede incompleto se conserva y genera pendiente contextual de entrega.

Se comprobó que EF/modelo/migración `20261009080533_SiiModulesCatalog` corresponden a `public.sii_modulos`. Las consultas especializadas ahora explicitan el esquema. El diagnóstico local distingue migración pendiente, historial aplicado sin tabla, otro esquema e historial sin permiso de lectura. La pantalla administrativa informa error/contador no disponible; el procesador marca la etapa fallida sin abrir la fuente de módulos. No se crea el catálogo manualmente, no se oculta el fallo como lista vacía correcta y no se añade DDL al arranque. **No hay nueva migración en este ajuste ni cambios a migraciones anteriores.**

### Verificado

**76/76 .NET** de dominio/HTTP en memoria y lector ADO.NET sintético; **78/78 frontend** con transporte simulado; **10/10 recorridos Edge/React con transporte simulado**, cero errores JavaScript. Compilación Release y TypeScript/Vite correctos. **35/35 sincronizaciones en PostgreSQL nativo aislado**, incluida ausencia de esquema, fallo por catálogo sin consulta de su fuente, 1501 módulos, integridad, rollback, manual/automática e ILDA independiente (`native-postgres-20261009-115252-a9b6af89`).

La verificación nativa de este ajuste pasó **51/51 casos de edición** (`native-postgres-20261009-115720-67ee7190`): alcance/permiso, revocaciones, responsables y colaboradores, reservas, versiones, atomicidad, errores, eliminación/renovación tardía, respuestas históricas y enviados. Pasó **57/57 comprobaciones EF/PostgreSQL** (`native-postgres-20261009-120236-eb1453e0`), incluidos cinco diagnósticos nuevos de módulos y aplicación/repetición mediante EF. Las pruebas de migraciones históricas conservan sus porcentajes históricos esperados; no se editaron migraciones para adaptar esos porcentajes al cálculo actual. [Edición](docs/migracion/entrega-sistemas-20261009/edicion-postgresql.json), [EF](docs/migracion/entrega-sistemas-20261009/migraciones-postgresql.json), [sincronizaciones](docs/migracion/entrega-sistemas-20261009/sincronizaciones-postgresql.json), [UI con transporte simulado](docs/migracion/entrega-sistemas-20261009/navegador-transporte-simulado.json).

**11/11 recorridos Edge → ASP.NET → PostgreSQL con dos identidades distintas**, más otra pestaña de la misma cuenta, cero errores JavaScript (`native-postgres-20261009-120744-82efeb72`). Reservas simultáneas, filas independientes, identidad/presencia, guardado/salida, primeros caracteres/pegado/Tab, elegibilidad remota, eliminación y renovaciones tardías, Medios independiente, ayudas, desconexión/caducidad/reconexión, móvil y entrega sin posteriores. Se prueban rechazos directos del backend y enviados. [Reporte](docs/migracion/entrega-sistemas-20261009/dos-usuarios-postgresql.json), [foco de escritorio](docs/migracion/entrega-sistemas-20261009/escritorio-foco.png), [presencia móvil](docs/migracion/entrega-sistemas-20261009/movil-presencia.png). Las pruebas de conexión bloquean peticiones externas; no son validación institucional.

**7/7 recorridos nativos de módulos** (`native-postgres-20261009-120504-4fb94e16`) y **13/13 de Sincronizaciones** (`native-postgres-20261009-120549-793841be`), cero errores JavaScript. Incluyen buscador/ID/instantánea, sin catálogo, Otra/Otro, varias herramientas, sesiones, ayudas, móvil, enviados, cola/programación/ILDA, fallos parciales y diagnóstico visible al faltar el esquema local. [Módulos](docs/migracion/entrega-sistemas-20261009/modulos-navegador-postgresql.json), [Sincronizaciones](docs/migracion/entrega-sistemas-20261009/sincronizaciones-navegador.json), [error administrativo](docs/migracion/entrega-sistemas-20261009/sincronizaciones-esquema-ausente.png). Motores aislados detenidos tras cada corrida.

### Pendiente y límites

No se consultó la base usada por la instalación que produjo la captura: `42P01` prueba ausencia de una relación resoluble en PostgreSQL; no determina por sí solo si la configuración apunta a otra base. La reproducción aislada con migración pendiente no acredita esa causa en una base institucional. El operador debe verificar el destino previsto y aplicar las migraciones pendientes explícitamente. Siguen pendientes SII real (SELECT de las dos columnas, DesarrolloSII, TLS/permisos/tipos/volumen), dispositivos físicos y aceptación institucional. Sin cambios en Nexo, Herd, credenciales, bases institucionales, push ni despliegue.

[Contrato y comandos EF/operación](docs/migracion/SISTEMAS_HERRAMIENTAS.md) · [Edición automática](docs/migracion/EDICION_AUTOMATICA.md).

## 2026-10-09 — Sistemas y herramientas y módulos de SIIv2 (entrega anterior)

### Implementado

Se preservaron cambios locales, tablas, tipografía y densidad compacta. Identificación muestra Código/origen sin renumerar; Prioridad pierde sólo el consejo solicitado. Ayudas compartidas con X accesible, Cerrar, Escape/foco y secciones opcionales. Sistemas utiliza una herramienta por fila, varias filas por procedimiento, selección única de herramienta/uso/funcionamiento, detalles separados y búsqueda de módulos locales. Se preservan históricos, enviados, primera etapa, participación, reservas/autoguardado y eliminación directa. El servidor valida opciones nuevas/referencias, calcula pendientes y conserva la descripción del módulo como instantánea.

SII incorpora UR y módulos a la cola/procesador existente, manual/automático e independiente de ILDA. Consulta fija de dos columnas de `sii.MODULOS_SII` por la misma conexión Sii; réplica EF con IDs estables y bajas locales, publicaciones/resultados/auditoría transaccionales por catálogo y fallos sanitizados. Nueva migración **`20261009080533_SiiModulesCatalog`**, sin alterar migraciones anteriores ni respuestas. No se activa programación, se aplica DDL al arranque ni se modifica Nexo.

### Verificado

**74/74 .NET** (dominio/HTTP en memoria y lector ADO.NET sintético), **78/78 frontend**, **10/10 Edge con React y transporte simulado**, cero errores JavaScript. Incluyen 1501 módulos sin truncamiento, IDs/duplicados/lectura parcial, conexión compartida sin abrir, borradores, detalles, múltiples herramientas, históricos, códigos/orígenes, endpoint autorizado, ayudas, teclado, dos sesiones y anchos 1920/1366/768/360. TypeScript/Vite y solución Release correctos; .NET sin advertencias. EF confirma snapshot/modelo y genera el SQL sin conectarse. Capturas inspeccionadas. [Reporte y capturas](docs/migracion/sistemas-herramientas-20261009/navegador.json).

**34/34 sincronizaciones sobre PostgreSQL 18.6 desechable**, incluidos cuatro casos nuevos de módulos: 1501/repetición/IDs, fallos preservando copia, rollback de catálogo/metadatos/resultado/auditoría y manual/automática con/sin ILDA. Corrida `native-postgres-20261009-084200-2373f124`. **7/7 recorridos Edge → ASP.NET → PostgreSQL**: borrador sin catálogo, búsqueda/ID/persistencia, renombre/baja histórica, Otra/Otro, dos sesiones/SignalR/reserva/autoguardado, filas del mismo procedimiento, móvil/teclado/enviado. Corrida `native-postgres-20261009-084018-c939ad44`, cero errores JavaScript. [Evidencia navegador PostgreSQL](docs/migracion/sistemas-herramientas-20261009/navegador-postgresql.json). Motores detenidos al finalizar.

**52/52 comprobaciones EF/PostgreSQL**: aplicación desde cero/con esquema anterior, repetición, rollback, rechazos de contenido inesperado y conservación de enviados/respuestas. Se amplió el verificador de sólo lectura para reconocer `SiiModulesCatalog` pendiente. Corrida `native-postgres-20261009-084557-d4d2a18e`. [Evidencia EF](docs/migracion/sistemas-herramientas-20261009/migraciones-postgresql.json).

**12/12 recorridos Edge de Sincronizaciones con ASP.NET/PostgreSQL**, cero errores JavaScript: cola/exclusión, resultado separado UR/módulos/ILDA, parcial por módulos, programación con/sin ILDA, revocación, recuperación y escritorio/móvil. Se actualizaron selectores de pruebas antiguas a «Trámite o servicio»; no se cambió la etiqueta vigente para satisfacerlas. Corrida `native-postgres-20261009-084856-06da90d2`; [reporte](docs/migracion/sistemas-herramientas-20261009/sincronizaciones-navegador.json).

**49 escenarios de edición comprobados en PostgreSQL**: `native-postgres-20261009-085429-706ad183` pasó 48/49; el caso restante esperaba rechazar una referencia histórica intacta. Se ajustó a la regla vigente de conservarla y permitir su corrección explícita. La repetición `native-postgres-20261009-085927-33d92b32` pasó 2/2, incluidos ese caso y el rechazo de borrar/renombrar un código confirmado (también con null). [Evidencia agregada con ambas corridas](docs/migracion/sistemas-herramientas-20261009/edicion-postgresql.json). Se actualizaron fixtures antiguos de PUT retirado, Otro destinatario, eliminación sin intención y envío fuera de primera etapa; no se relajó producción para satisfacerlos. Las restricciones de primera etapa, revocaciones, concurrencia, reservas, auditoría, enviados y ambos casos nuevos de persistencia de módulos pasaron.

### Pendiente y límites

Los primeros intentos con `pg_ctl` fallaron por relanzamiento con token restringido (87/3). Se resolvió mediante `-DirectStart`, que inicia sólo el nuevo clúster verificado en `.artifacts`, con el mismo usuario/restricciones y sin elevación. Para Edge, `TDV2_TEST_EPHEMERAL_TLS=true` usa HTTPS efímero en loopback sin instalar certificados. Son adaptaciones del entorno sintético; no modifican el sitio productivo ni recurren a bases existentes.

Falta SII real: SELECT sobre `sii.MODULOS_SII`, DesarrolloSII, autenticación/TLS, tipos, volumen/rendimiento y aceptación institucional/dispositivos físicos. La migración institucional queda para el operador. Sin acceso a servicios/bases institucionales, cambios en Herd/Nexo/credenciales, push ni despliegue Ubuntu. [Contrato del formulario, migración/comandos exactos y operación](docs/migracion/SISTEMAS_HERRAMIENTAS.md).


## 2026-10-08 — Recuperación de densidad compacta en Identificación

### Implementado

Se retiraron exclusivamente las ampliaciones de tamaño/espaciado de la última entrega. Captura vuelve a 13 px de `capture-scroll`, encabezados a 12 px del tema con peso **700**, instrucciones a `body2`, ayudas/opciones de destinatarios a 14 px. Se recuperaron los tamaños pequeños, padding de celdas y anchos mínimos anteriores de los componentes; se quitaron las columnas y el ancho fijo de 2045 px. El tema global se conserva; no se restauraron archivos completos.

Se mantienen contraste/foco visible, tabla, texto multilínea, instrucciones, siete categorías, ayudas, colores de prioridad y todos los cambios de eliminación/reservas/autoguardado. Ningún cambio de backend, autorización, datos o migraciones. La referencia a 16 px de la entrega anterior describe su estado histórico, sustituido por este ajuste visual.

### Verificado

TypeScript y build Vite correctos; diff sin errores de espacios. **16/16 recorridos Edge/React/ASP.NET/PostgreSQL sintético**, cero errores JavaScript, sobre el código local final. Incluyen teclado, selectores/ayudas, dos sesiones/pestañas, reserva automática, autoguardado, eliminación directa/renovación en vuelo y conservación de históricos. Las expectativas visuales existentes se ajustaron a los valores originales, sin añadir una suite nueva.

[Reporte](docs/migracion/identificacion-compacta-20261008/navegador.json) y capturas: [1366 px](docs/migracion/identificacion-compacta-20261008/tabla-1366.png), [1920 px](docs/migracion/identificacion-compacta-20261008/tabla-1920.png), [768 px](docs/migracion/identificacion-compacta-20261008/tabla-768.png), [móvil 360 px](docs/migracion/identificacion-compacta-20261008/tabla-360.png). Escritorio y móvil inspeccionados; sin desbordamiento de página y con scroll dentro de la tabla. Corrida `native-postgres-20261009-055750-b66f6187`; clúster aislado detenido.

### Pendiente y límites

No requiere nueva migración ni reversión. Continúan pendientes las instalaciones/validaciones institucionales anteriores y la aceptación en dispositivos físicos. No se modificaron bases institucionales, Nexo, Herd ni credenciales; no hubo push ni despliegue.

## 2026-10-08 — Tabla de Identificación, categorías vigentes y renovación tras eliminar

### Implementado

Identificación conserva tabla de ocho columnas, código junto a Fuente, tipografía original y edición directa. Texto/controles de 16 px, encabezados 700, valores normales, columnas estables, textos multilínea y acciones de 44 px; scroll contenido en móvil. Instrucciones y ayudas actualizadas; Prioridad conserva números/colores y no muestra Ejemplo en su ayuda.

Destinatarios ofrece las siete categorías definitivas, sin Otro/Externo ni captura de detalle. Nueva migración **`20261009052502_CurrentRecipients`**: retira sólo esos valores JSON exactos y `usuarioOtro` en borradores del ejercicio vigente, conserva otras selecciones/respuestas, versiona filas/formato, invalida sus reservas y audita transaccionalmente. Rechazo localizado de peticiones antiguas; vacío queda pendiente. Enviados e históricos bloqueados conservan originales, mostrados como consulta. Las referencias anteriores a Otro/Externo como opciones vigentes quedan sustituidas por esta entrega.

**Causa reproducida:** `commitChange` renovaba también al retirar; una renovación con intención de eliminación podía llegar después del PATCH y `FormRemoval.Changes` buscaba la fila ya borrada. Ahora espera actividad anterior, no renueva retiros y limpia estado después del ACK. El backend acredita un retiro mediante su recibo y responde 409 `registro_eliminado` sin excepción esperada; ID desconocido es 404, nunca éxito. Revalida autorización primero. Se mantienen cascada/versiones/auditoría/exclusión ILDA, reintentos idempotentes y protección de enviados. También se evita reaplicar una lectura inicial consumida tras un 422, conservando estados remotos pendientes necesarios para recibos antiguos.

### Verificado

- TypeScript, build Vite y solución Release finales correctos; .NET con cero errores y advertencias. La primera compilación había emitido NU1900 por auditoría de NuGet no disponible, sin errores de código.
- **78/78 frontend**, **62/62 dominio/HTTP en memoria**, **20/20 PostgreSQL más 2/2 ampliaciones** (21 escenarios diferentes; uno repetido/reforzado), **50/50 migraciones EF**. Destinatarios/avance, permisos, revocación, bloqueos de etapa, históricos/enviados, rollback, versiones, reservas simultáneas, respuesta perdida, eliminación/relaciones y sincronización ILDA sintética.
- **16/16 Edge/React/ASP.NET/PostgreSQL**, cero errores JS. Dos sesiones/pestañas, escritura/Tab/Select, nuevas categorías y recarga, ayudas, eliminación directa/Cancelar, renovación en vuelo sin nueva actividad del retiro, liberación automática y consulta histórica. Escritorio 1920/1366, tableta 768, móvil táctil 360 y viewport 683 equivalente al contenido al 200 %. Capturas inspeccionadas; todos los clústeres desechables detenidos.

[Causa, evidencia, capturas, comandos y alcance exacto](docs/migracion/IDENTIFICACION_TABLA.md). Nueva migración preparada/probada únicamente en bases sintéticas. No cambian participación, roles, autenticación ni Nexo; fotos y edición automática se conservan.

### Pendiente

Aplicación explícita posterior de la migración por el operador. Zoom nativo al 200 %, lector de pantalla, dispositivos físicos y aceptación institucional: Edge headless no aplicó el atajo de zoom (`nativeZoom200=1`), por lo que no se acredita esa comprobación con la captura equivalente. No se modificaron Nexo, Herd, credenciales ni bases institucionales; no hubo push ni despliegue.

## 2026-10-08 — Procedimientos Institucionales, primera etapa

### Implementado

- Nombre visible nuevo y autorización centralizada compatible con `procedimientos_institucionales`/`procesos_operativos`, misma ruta `/inicio`, sin duplicar navegación ni ampliar permisos. Se documentó la edición posterior del módulo existente de Nexo conservando ID, jerarquía y asignaciones; no se ejecutó sobre Nexo. `configuracion_procesos` y participación de UR siguen iguales.
- Inicio oculta las dos descripciones solicitadas sólo para administrador efectivo. Contexto, Identificación general y Sistemas son las pestañas de primera etapa. Administrador efectivo con interruptor **Mostrar secciones posteriores**, apagado al entrar. El servidor controla reservas, renovación y guardado, incluidos medios; no basta con ocultar pestañas. Representación usa el objetivo y la vista de prueba conserva consulta.
- Avance/revisión parciales del servidor separados del avance total: no exigen secciones ocultas ni generan un envío parcial. La etapa permanece borrador; completar el porcentaje no envía el formato. Se conservan respuestas ocultas y enviados inmutables. El administrador mantiene sólo sus facultades previas por UR y responsabilidad para envío completo.
- Ayudas MUI reutilizables con los textos solicitados, ejemplo/consejo, Cerrar con X, Escape y retorno de foco; se consultan sin adquirir reservas, también en lectura. Etiquetas nuevas; destinatarios con **Externo**, colección única y **Otro** con `usuarioOtro` separado que permanece al desmarcar. Vacíos se guardan como borrador, pero cuentan pendientes. Sin nueva limpieza de respuestas.
- Prioridad con círculos, números, etiquetas cortas y paleta solicitada, sin opción seleccionable para el indicativo. Sistemas ofrece Procedimiento sólo con código confirmado y V/A; el backend valida asociaciones nuevas/cambiadas y permite corregir históricos sin comparaciones.
- Retiro ILDA permitido mediante exclusión local por UR/ejercicio/ID. `FormRemoval` calcula la cascada en servidor; verifica todas las reservas/versiones y rechaza cambios ajenos a esa operación, especialmente en secciones ocultas. Retiro, exclusión, auditoría y recibo idempotente son transaccionales. La réplica completa permanece intacta y las consultas individuales/por conjuntos omiten el registro excluido. Nueva migración **`20261009032858_FormIldaExclusions`**, exclusivamente preparada/probada en entorno sintético. Reversión protegida ante exclusiones o enviados.

[Contrato, comando exacto de instalación, pruebas y Visual Studio](docs/migracion/PRIMERA_ETAPA.md). Se conservaron los cambios locales previos, fotografías/cachés, reservas automáticas, autoguardado, eliminación directa y ausencia de Resolver.

### Verificado

- TypeScript y build Vite correctos. Build Release ASP.NET/solución: cero errores/advertencias. **76/76** pruebas frontend, incluyendo motor de captura, versiones, respuesta perdida, desconexión, reservas, navegación y fotografías. **62/62** dominio/transporte ASP.NET (dobles en memoria; no sustituyen PostgreSQL).
- **19/19** casos HTTP/PostgreSQL aislado, nueve nuevos de primera etapa más diez regresiones de concurrencia/seguridad. Alias y revocación, perfil efectivo/representación/prueba, secciones prohibidas y medios, progreso parcial, Otro/Externo, conservación de respuestas ocultas/enviados, V/A/códigos, cascada de eliminación, reserva ajena y obsoleta, rollback de auditoría, reintento idempotente y exclusiones en lectura individual/conjunta.
- **13/13** recorridos Edge/React/ASP.NET/PostgreSQL; cero errores JavaScript. Dos sesiones y dos pestañas, primera escritura retenida durante adquisición, Tab, Select múltiple y Prioridad, ayudas por clic/teclado/toque/Escape sin liberar, Otro al desmarcar/recargar, eliminación ILDA directamente sin enfocar y Cancelar sin cambios, no reaparición tras nueva publicación del inventario sintético y control administrativo. Escritorio 1366/1920 y móvil táctil emulado 360 px con movimiento reducido. Capturas inspeccionadas.
- **44/44** comprobaciones de migraciones EF en PostgreSQL aislado: snapshot/modelo, siete migraciones, actualización desde seis sin limpieza de respuestas, repetición sin cambios, datos/instantáneas/roles conservados, reversión protegida y recibos/versión del proveedor. Corrida `native-postgres-20261009-035816-30007467`.
- Prueba adicional de retiro: ejecutó el **sincronizador de producción con lector ILDA sintético**, conservó la réplica y la exclusión evitó reaparición. También rechazó restaurar la fila con un parche directo. Arranque equivalente a **TDV2 HTTPS + React**: Vite 5173, HTTPS 7136, React, protección anónima 401 y HMR por `wss://localhost:7136`, sin consultas de datos ni servicios institucionales. No se automatizó la GUI de Visual Studio. Corrida `native-postgres-20261009-040001-a4946d37`; 2/2 agregados, cero errores JavaScript. Todos los clústeres se detuvieron.
- Durante la validación se corrigieron expectativas de la prueba anterior (secciones posteriores ahora restringidas) y del helper de navegador (buscar una fila de una pestaña desmontada). Se corrigió el icono para el export real de MUI instalado. La prueba de reversión motivó proteger también la nueva tabla antes de retirar su historial. No se modificaron migraciones anteriores.

Evidencia sintética: [frontend](docs/migracion/primera-etapa-20261008/frontend.txt), [19 casos PostgreSQL](docs/migracion/primera-etapa-20261008/postgresql.json), [13 recorridos finales del navegador](docs/migracion/primera-etapa-20261008/navegador.json), [44 comprobaciones EF](docs/migracion/primera-etapa-20261008/migraciones.json), [sincronizador y arranque](docs/migracion/primera-etapa-20261008/sincronizacion-arranque.json), [HTTPS/React/HMR](docs/migracion/primera-etapa-20261008/https-react-hmr.json). Capturas inspeccionadas: [administrador 1920 px](docs/migracion/primera-etapa-20261008/etapa-admin.png), [prioridad 1366 px](docs/migracion/primera-etapa-20261008/etapa-prioridad.png), [selector móvil](docs/migracion/primera-etapa-20261008/etapa-mobile.png) y [ayuda móvil](docs/migracion/primera-etapa-20261008/etapa-ayuda-mobile.png). Última corrida de navegador `native-postgres-20261009-040334-46ba8511`: se esperó el estado autorizado antes de capturar el administrador, 13/13 y cero errores JavaScript. Sólo se copiaron reportes/capturas sintéticos, nunca configuración privada del clúster.

### Pendiente y límites

El operador debe aplicar explícitamente la migración a su destino revisado e instalar TDV2 compatible **antes** de renombrar/publicar el módulo existente en Nexo. No se modificaron Nexo, Herd, credenciales ni bases institucionales; no hubo push ni despliegue. La aceptación con Microsoft/Nexo reales y dispositivos físicos sigue pendiente. El recorrido `first-stage` verifica la experiencia vigente; las evidencias de recorridos completos de entregas anteriores corresponden a su fecha y no acreditan la nueva restricción. Las propuestas siguen en memoria de la pestaña y con descarga ante fallos reales; no se promete recuperación tras cierre forzado.

## 2026-10-08 — Edición automática sin resolución manual

### Implementado

Se mantiene entrar, editar, guardar al salir y liberar automáticamente. Se retiraron **Resolver**, **Resolver eliminación**, la comparación de propuestas y sus métodos/componentes sin uso. Se conservan reservas por fila/pregunta/criterio, permisos, enviados inmutables, fotos persistentes, avatares/colores, administración central/delegada y participación vigente. Las menciones a recuperación manual en entregas anteriores de este archivo quedan como evidencia histórica, sustituidas por esta entrega.

**Causa reproducida antes del cambio:** una referencia de Sistemas a un código no confirmado devolvió 422 con reservas válidas y rechazó también un acuerdo independiente enviado en la misma operación. El motor bloqueaba después todos los bloques enviados como si fuera un conflicto. [Reproducción original](docs/migracion/edicion-automatica-20261008/reproduccion-previa.json), `native-postgres-20261008-174322-d8a19265`. El código generaba y ofrecía códigos desde el navegador antes del commit; el guardado parcial validaba todo el documento. No se examinó contenido institucional.

Ahora el servidor confirma códigos y criterios vacíos bajo la transacción de UR. El borrador valida filas cambiadas y vínculos afectados, identifica bloque/campo y permite corregir con reserva vigente. El envío conserva validación completa. Los registros independientes tienen operaciones/recibos separados; las eliminaciones con relaciones siguen siendo atómicas. No se limpian vínculos históricos para evitar errores.

Se conserva texto durante peticiones y se confirman respuestas perdidas con el mismo UUID, con reintentos limitados/progresivos. Volver al valor original mientras viaja un guardado espera el recibo y el cambio posterior antes de liberar. Tras vencimiento se exige nueva reserva y base idéntica; el texto incompatible queda accesible sin sobrescribir ni comparar. Se protegen primeras teclas, pegado y Tab mientras se prepara la edición. Siguen autoguardado de un segundo, descarga ante fallo real y revalidaciones SignalR cada 15 segundos.

[Detalle, límites y comandos desde Visual Studio](docs/migracion/EDICION_AUTOMATICA.md). **Sin nueva migración** ni dependencias. No se modificaron bases institucionales, credenciales, Nexo, Herd, autenticación ni Ubuntu. Sin push/despliegue.

### Probado

- TypeScript, Vite y solución Release correctos; .NET sin errores ni advertencias. **75/75** pruebas frontend (41 del motor/captura) y **62/62** de dominio/aplicación. Incluyen cachés de fotografías, referencias estables, validación corregible, pendientes independientes, primeras teclas/pegado/Tab, recibos y edición durante guardado.
- **28/28** escenarios de edición en PostgreSQL 18 aislado: [evidencia](docs/migracion/edicion-automatica-20261008/postgresql.json), `native-postgres-20261008-180443-63d43575`. Altas simultáneas, pertenencia sin borrar históricos, usuarios/pestañas, exclusión mutua, registros distintos, reservas vencidas/reasignadas, revocación, auditoría/rollback, eliminación con dependencias, ILDA y envío concurrente/inmutable.
- **33/33** recorridos Edge/React/ASP.NET/PostgreSQL: [evidencia](docs/migracion/edicion-automatica-20261008/navegador.json), `native-postgres-20261008-181904-0008b450`. Primera escritura/pegado/Tab durante adquisición, Select, dos sesiones/pestañas, eliminación directa/Cancelar en cuatro tablas, carreras durante diálogo, respuesta perdida, reconexión sin comparar, criterios independientes, foto/tooltip, tres ciclos SignalR, envío y retiro central. Cero errores JavaScript. [Captura](docs/migracion/edicion-automatica-20261008/captura.png) y [eliminación ocupada](docs/migracion/edicion-automatica-20261008/eliminacion-ocupada.png), inspeccionadas. Fuentes externas bloqueadas sólo en el host sintético.
- Una corrida previa de Edge se detuvo porque una aserción esperaba el texto anterior al nuevo caso de respuesta perdida; se actualizó al texto que éste guarda. La ampliación del selector múltiple también detectó que el verificador debía esperar la liberación: recibir contenido confirmado todavía no autoriza la fila. Se conserva esa separación en las pruebas.
- Verificación específica final: **6/6** de selección múltiple/Prioridad en Edge, sin errores JS, con dos sesiones, recarga, teclado/Escape, foco y móvil táctil emulado de 360 px con movimiento reducido: [resultado](docs/migracion/edicion-automatica-20261008/seleccion-multiple.json), [captura móvil](docs/migracion/edicion-automatica-20261008/movil.png). **2/2** regresiones PostgreSQL repetidas con validación de bloques malformados (422 con campo, no error 500): [resultado](docs/migracion/edicion-automatica-20261008/regresion-final.json). Corrida `native-postgres-20261008-182826-7563c721`; estos dos casos amplían los ya incluidos en los 28, no son casos adicionales. Todos los clústeres aislados detenidos. Los últimos ajustes de liberación tras volver al valor original y limpieza de grupos confirmados están cubiertos también por las 41 pruebas del motor; no se modificaron luego las reglas de servidor.

### Pendiente y límites

Aceptación institucional de Nexo/Microsoft/proxy y móviles físicos/IME. El texto no confirmado vive en la pestaña, con aviso de salida y descarga; no se persisten respuestas privadas en almacenamiento del navegador ni se promete recuperarlas tras un cierre forzado. Las pruebas locales no acreditan despliegue ni conectividad real. Se conserva **TDV2 HTTPS + React**; las escrituras desde Visual Studio se prueban con el ejecutor aislado, sin usar User Secrets institucionales.

## Persistencia de fotografías — actualización 2026-10-07

### Implementado

La cabecera utiliza `AccountPhotoProvider` sobre las páginas: conserva su miniatura durante navegación SPA y renovación en segundo plano. Al recargar recupera la caché privada del servidor tras autorizar la petición. Caché de 32 MiB por proceso, renovación a los 15 minutos y conservación ante fallos temporales por **un máximo de una hora desde la última verificación correcta**, sin prolongarla por reintentos. Espera progresiva de 30 segundos a 15 minutos; ausencia confirmada y errores permanentes retiran la imagen y usan iniciales.

Contexto opaco ligado a sesión, cuenta Microsoft e identidad efectiva; cancelación de respuestas antiguas y limpieza inmediata al cambiar cuenta/representación o cerrar sesión, también en otras pestañas mediante una notificación efímera. El endpoint comprueba nuevamente sesión/revisión tras una descarga lenta. Un 401/403/409 de TDV2 no conserva la foto como si Microsoft estuviera temporalmente caído. Se mantienen fotos verificadas del objetivo, participantes autorizados con reserva vigente y renovación serializada de tokens. Sin imágenes/tokens en almacenamiento web ni caché de permisos.

Se corrigió además el transporte de «Usar otra cuenta»: `/connect?account=other` requiere navegación completa, no consulta JSON. Sin cambios de hints ni reglas OAuth. Avatares, colores, nombres, reservas, autoguardado, eliminación y configuración de participación conservados. **No requiere nueva migración**; no modifica las migraciones ni pendientes anteriores.

Se identificó en el código el conflicto posible entre Windows/Ubuntu con la misma base y anillos Data Protection incompatibles. Un fallo de descifrado registra `token_protection` sin tokens ni excepción original y retira la foto al renovar. Probado con cifrado sintético de otro proveedor, sin inspeccionar ni compartir llaves reales. [Política, diagnóstico y comandos](docs/migracion/PERSISTENCIA_FOTOGRAFIAS.md).

### Probado

Compilación Release de la solución: cero errores y advertencias. TypeScript y Vite correctos; **71/71** pruebas frontend y **62/62** de dominio/transporte. Regresión HTTP/PostgreSQL: **152/152**, corrida `native-postgres-20261007-230704-85dcb3ca`, [evidencia](docs/migracion/fotografias-persistencia-20261007/postgresql.json). Incluye permisos, revocación, token refresh, cifrado incompatible, solicitudes simultáneas, colaboración y enviados. Clúster desechable detenido al terminar.

Tras el refuerzo final de revisión de contexto y límite/cancelación de la descarga: **15/15** casos específicos HTTP/PostgreSQL de fotos/tokens, más **9/9** escenarios en Edge. Corrida final `native-postgres-20261007-231448-59f7ff6f`: [backend](docs/migracion/fotografias-persistencia-20261007/fotografias.json) y [navegador](docs/migracion/fotografias-persistencia-20261007/navegador.json). El runner cuenta el recorrido Edge como un caso adicional (16/16), no como nueve casos HTTP.

Edge verificó navegación sin nuevas peticiones, recarga sin nueva descarga Graph, renovación lenta, error temporal, ausencia confirmada, representación, identidad objetivo verificada, teclado/móvil de 360 px, otra pestaña, cambio de cuenta y logout; sin errores JavaScript. Los primeros intentos detectaron una espera incorrecta del reloj sintético y el problema real de navegación OAuth con parámetros; se corrigieron, no se cuentan como aprobados.

Regresión del formato en Edge sobre el código final: **32/32**, sin errores JavaScript, `native-postgres-20261007-231643-6b8a7779`. [Evidencia](docs/migracion/fotografias-persistencia-20261007/regresion-formato.json): dos sesiones/pestañas, fotos/colores/tooltip sin perder foco, reservas automáticas, autoguardado, Select, eliminación directa, propuestas ante desconexión, tres ciclos SignalR de 15 segundos, envío definitivo y revocación. Todos los clústeres aislados quedaron detenidos.

### Pendiente

Aceptación con Microsoft real y comprobación de la topología institucional de cifrado Windows/Ubuntu. No se conectó a bases institucionales ni se modificaron Nexo, Herd, credenciales o llaves. Sin push ni despliegue. Las pruebas usan miniaturas sintéticas; no acreditan disponibilidad de fotos reales ni accesibilidad con dispositivos físicos.

## Usuarios que atiende y Prioridad — actualización 2026-10-07

### Implementado

«Usuarios que atiende» sustituye el texto libre por selección múltiple MUI con cinco opciones, casillas y menú que permanece abierto al marcar/desmarcar. Colección JSON bajo la clave existente `usuario`, sin duplicados ni valores predeterminados. Nuevos registros manuales y de ILDA comienzan vacíos. Validación, avance y revisión de envío comparten la regla: una lista vacía es pendiente. El texto histórico de los enviados permanece en consulta.

Prioridad muestra «Selecciona una prioridad» sólo como indicativo del campo vacío, fuera de las opciones del menú; conserva colores, selección única y valores existentes. Ambos controles reutilizan las reservas automáticas y autoguardado del registro, sin cambios en el motor colaborativo, permisos, avatares ni eliminación directa.

Migración de datos **`20261007195019_StructuredUsersServed`**, sin cambio de esquema: limpieza explícita, única y transaccional del campo anterior en borradores del ejercicio editable, preservando otras respuestas, registros, prioridades y listas nuevas. Actualiza avance/versiones, invalida reservas anteriores sólo de los bloques modificados y audita. Los enviados, sus instantáneas y los borradores históricos bloqueados por cambio de ejercicio no se modifican. No existe limpieza al abrir ni guardar. [Detalle y comando de actualización](docs/migracion/USUARIOS_ATENDIDOS.md).

### Probado

Compilación de `tdv2.slnx` en Release: cero errores y advertencias; **62/62** pruebas de dominio/transporte. **146/146** casos HTTP/PostgreSQL: [evidencia](docs/migracion/usuarios-atendidos-20261007/postgresql.json), corrida `native-postgres-20261007-204401-b254e2a9`. Se verificaron listas inválidas/duplicadas, persistencia sin limpieza posterior, envío rechazado con lista vacía y aceptado con opciones guardadas, prioridades intactas, permisos, reservas, revocación, ILDA y enviados. Clúster desechable detenido al terminar.

TypeScript y Vite correctos; **64/64** pruebas frontend. **41/41** comprobaciones EF/PostgreSQL aislado, incluida limpieza limitada, conservación exacta de enviado/instantánea y borrador de otro ejercicio, prioridades 4/17, rollback si falla la auditoría y repetición del comando/script sin borrar nuevas selecciones. [Evidencia EF](docs/migracion/usuarios-atendidos-20261007/ef.json), corrida final `native-postgres-20261007-205841-e6131401`. El primer intento detectó que `Down` retiraba parcialmente el historial antes de rechazar un enviado; se corrigió. La corrida anterior de 40 casos se amplió con la protección del ejercicio histórico; no se suman ejecuciones repetidas. SQL final generado sin conexión en `.artifacts/usuarios-atendidos.sql`.

**6/6** escenarios específicos en Edge con ASP.NET/PostgreSQL reales y Microsoft/Nexo sintéticos: reserva confirmada antes de abrir, selección múltiple y deselección sin cerrar, autoguardado/recarga, dos sesiones independientes con bloqueo y liberación en vivo, teclado/Tab/Escape, foco, scroll, indicativo de Prioridad ausente de las cinco opciones y móvil táctil emulado de **360 px** con movimiento reducido. Sin errores JavaScript. [Evidencia](docs/migracion/usuarios-atendidos-20261007/navegador.json), corrida `native-postgres-20261007-202927-9c2c554e`; [captura escritorio](docs/migracion/usuarios-atendidos-20261007/usuarios-desktop.png) y [móvil](docs/migracion/usuarios-atendidos-20261007/usuarios-mobile.png), inspeccionadas. El verificador espera la revalidación inicial antes de actuar; los intentos que pulsaban durante la carga no se cuentan como aprobados.

Regresión completa del formato sobre el bundle final: **32/32** escenarios en Edge, sin errores JavaScript. Incluye enlaces de pendientes al nuevo selector, reservas de dos sesiones/pestañas, eliminación directa en cuatro tablas, Cancelar sin escrituras, conflictos de versión, recuperación, foco/Select, avatares/fotos, tres ciclos SignalR de 15 segundos, envío, revocación central y cierre de sesión. [Evidencia](docs/migracion/usuarios-atendidos-20261007/regresion-formato.json), `native-postgres-20261007-205248-59628bfd`.

### Pendiente

Aplicar la migración explícitamente por el operador al destino revisado, con la aplicación detenida; pruebas con dispositivos físicos y lector de pantalla. **No se aplicó a bases institucionales** ni se modificaron Nexo, Herd o credenciales. Sin push/despliegue. Se conservan los pendientes institucionales y las reglas de participación actuales.

## Presentación de módulos y Configuración — actualización 2026-10-07

### Implementado

Encabezado compartido con título único de 24–26 px/600, descripción de 14 px, acciones adaptables y rutas de navegación en submódulos. Barra blanca alineada con el contenido, logo y tipografía conservados, separación de 32 px en escritorio y altura reservada medida mediante `ResizeObserver`. Menús con selección accesible, jerarquía publicada y cuenta separada sin desplazar la página.

Accesos compactos y componente `SettingsSection` para Configuración. Sincronizaciones separa fuentes, programación y ejecuciones; carga y errores no desplazan los campos, y el historial vacío es legible en móvil. Pruebas de acceso diferencia consulta de representación con escritura; se conserva su advertencia real. Configuración procesos sólo cambia presentación, etiquetas y ubicación de acciones/errores. Sin cambios de opciones, reglas de participación, autorización, autenticación, sincronización ni edición colaborativa.

[Detalle, comandos y capturas antes/después](docs/migracion/UI_MODULOS_CONFIGURACION.md). Se conservaron los cambios locales; [119 archivos de backend/lógica protegida sin variación](docs/migracion/ui-modulos-20261007/codigo-conservado.json). No se crea ninguna migración en esta entrega.

### Probado

TypeScript y compilación Vite correctos; **64/64** pruebas frontend. Edge con ASP.NET y PostgreSQL desechable: **32/32** recorridos de captura, **13/13** de acceso, **4/4** de alcance y **11/11** de sincronización (`native-postgres-20261007-174835-2f9c9823`). Después del ajuste de los avisos, **11/11** de sincronización repetidos sobre esa versión final (`native-postgres-20261007-175933-719f3118`); no se suman como casos diferentes. Sin errores JavaScript. Se comprobaron reservas, eliminación directa, foco, avatares/fotografía, autoguardado, propuestas, revocación, enviados y representación con dobles institucionales sintéticos.

Siete pantallas completas a **360/768/1366/1920 px** y ampliación **CSS 200 %**, con capturas comparables, un H1, sin desbordamiento de página ni títulos ocultos por la barra. Pruebas de teclado, menú móvil, selección activa, movimiento reducido, textos largos, permisos distintos, carga/error sin salto, conservación del borrador, Select y cancelación de confirmación sin escrituras. [Evidencia visual y funcional](docs/migracion/ui-modulos-20261007/after.json), corrida final `native-postgres-20261007-181024-1b50e32d`; **73 imágenes** anteriores/posteriores conservadas. Los intentos que detectaron capturas prematuras o pérdida de propiedades del selector se descartaron: el verificador espera que React monte el título y el ajuste de etiqueta conserva todos los `slotProps` originales de Autocomplete.

### Pendiente

Revisión manual con zoom nativo del navegador, lector de pantalla y dispositivos físicos: la evidencia automática de 200 % es ampliación CSS, no una sesión manual del navegador. Las nuevas reglas de participación continúan aplazadas y los pendientes institucionales anteriores permanecen vigentes. No se modificaron Nexo, Herd, User Secrets ni bases institucionales; no hubo push ni despliegue. Las bases de prueba fueron exclusivamente desechables.

## Respuesta de captura, avatares y fotografía — actualización 2026-10-07

### Implementado

Seguimiento incremental de bloques modificados; una tecla no compara todo el documento ni reprograma todas las reservas. Las filas memoizadas dependen de su reserva y sus relaciones reales. Estados remotos equivalentes conservan referencias y evitan avisar otra vez a React; mantienen comprobaciones de autorización/caducidad. Se integra el montaje previo de pestaña activa, sin perder propuestas ni cambiar autoguardado, eliminación directa, envío o SignalR.

Avatar con fotografía Microsoft opcional, tooltip MUI con nombre, foco visible y manejo de ratón/teclado/tacto sin adquirir/liberar reservas. Colores coordinados por identidad efectiva conservados. Endpoint protegido por acceso al formato y participante con reserva vigente, identidad Microsoft verificada, caché renovable sin permisos cacheados ni directorio público. Representación usa foto del objetivo o iniciales, nunca la foto del administrador.

Nueva migración `20261007161549_ParticipantPhotographs`: FK opcional en reservas a usuario Microsoft verificado, sin cambiar contenido ni instantáneas. **Las nuevas reglas de participación permanecen pendientes**; configuración/alcance existentes conservados, con [huellas antes/después](docs/migracion/evidencia-captura-politicas-conservadas.json). Continúan separadas las vías central explícita y delegada. [Detalle, comandos y recorrido de prueba](docs/migracion/CAPTURA_FOTOGRAFIAS_RENDIMIENTO.md).

### Probado

TypeScript y Vite correctos; **64/64** casos frontend y **62/62** de dominio/transporte. **34/34** comprobaciones EF en PostgreSQL desechable, incluyendo upgrade con enviado intacto, FK, repetición y script idempotente: [evidencia](docs/migracion/evidencia-captura-fotografias-migraciones.json), corrida `native-postgres-20261007-163801-c1df0a41`. Los intentos anteriores que detectaron el rollback parcial y un dato sintético incompleto no se cuentan como aprobados.

Compilación final `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false`: cero errores y advertencias. `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build`, en Production y sin conexión, confirma snapshot vigente. SQL de la nueva migración generado en `.artifacts/participant-photographs.sql`. El bundle final está reconstruido sin contadores de instrumentación.

**144/144** casos HTTP/PostgreSQL de regresión completos: [evidencia](docs/migracion/evidencia-captura-fotografias-postgresql.json), `native-postgres-20261007-165619-f90bd19c`. Incluyen fotografía protegida/cacheada/renovable, rechazo de identidad Graph ajena, representación con objetivo verificado o iniciales, permisos, colaboradores centrales/delegados, concurrencia, revocación, auditoría, ILDA y bloqueo de enviados. La corrida anterior interrumpida por el reinicio de la sesión no se usa como evidencia final. Todos los clústeres de prueba terminaron detenidos.

**31/31** escenarios de captura en Edge, sin errores JS: [evidencia](docs/migracion/evidencia-captura-fotografias-react.json), `native-postgres-20261007-162753-76a64950`. Reservas simultáneas, distintas filas/pestañas/sesiones, cancelación y eliminación sin foco en cuatro tablas, versiones/dependencias, desconexión/propuestas, revocación central, envío, Select, avatar/tooltip y tres ciclos de SignalR de 15 segundos.

**2/2** escenarios adicionales sobre la versión final del avatar: primera escritura confirmada, selección/cursor, inserción múltiple, Tab, fotografía, tooltip con ratón/teclado/toque real emulado y liberación sin recarga. [Evidencia táctil](docs/migracion/evidencia-captura-avatar-tactil.json), `native-postgres-20261007-165504-84389d5b`. La prueba táctil detectó que impedir el foco del puntero suprimía los eventos compatibles que esperaba el tooltip; se corrigió con apertura controlada para touch/pen, sin adquirir/liberar reservas. No se cuenta como aprobada la corrida que detectó ese defecto.

Medición antes/después con 20/100/200 registros del código local: filas ajenas renderizadas **19/99/199 → 0/0/0**. Una sola renovación durante 58 entradas, sin PATCH por tecla. La fotografía agrega una solicitud protegida por participante. La latencia a frame no mejora uniformemente; medias/p95 y comandos EF se publican sin porcentajes inferidos en [comparación](docs/migracion/evidencia-captura-comparacion.json). El motor reduce trabajo por cambio; Inicio conserva **8 comandos EF para 102 áreas**, optimización previa. No se confunde el contador EF con todas las consultas Npgsql/Nexo.

Arranque equivalente al perfil **TDV2 HTTPS + React**: Vite 5173, Kestrel 5064/7136, certificado existente, portada/React, HMR por 7136 y rechazo anónimo; cero consultas EF durante esas páginas. [Evidencia](docs/migracion/evidencia-captura-arranque-https.json), `native-postgres-20261007-163851-a21dc7a0`. Host sintético con el pipeline real; no se operó la interfaz gráfica del depurador ni se usaron User Secrets. Capturas en 1920, 1366, 768 y 390 px, sin desbordamiento de página y con movimiento reducido.

### Pendiente

Aplicación institucional de la nueva migración por el operador, después de revisar destino e historial; fotografías/consentimiento Microsoft reales, lectores de pantalla y dispositivos físicos. No hacen falta nuevos roles, permisos Graph de directorio ni publicación Nexo para esta entrega. Se conservan los pendientes institucionales anteriores. No hubo cambios de Nexo/Herd/User Secrets/bases institucionales, push ni despliegue. F5 continúa sin DDL ni sincronización automática.

## Participación, rendimiento y presencia — actualización 2026-10-06

### Implementado

Submódulo `configuracion_procesos` bajo `configuracion`, exclusivo de administrador en contexto propio. Configuración EF versionada de niveles/tipos, revisión de impacto, confirmación protegida contra cambios de catálogo/formatos y auditoría transaccional. Participación separada de catálogo y permisos; conservación de históricos y distinción central/delegada. Reservas/escrituras se coordinan con cambios de participación mediante bloqueos breves. Migración nueva `20261006233010_ProcessParticipationAndPresence`, sin modificar las anteriores.

Sólo se monta la pestaña activa; propuestas y colaboración permanecen en el motor. Cambios parciales con referencias estables, filas memoizadas y consulta de Inicio por conjuntos. Presencia con color coordinado por identidad efectiva, avatar de iniciales, nombre e indicadores de reserva; sin modificar OAuth ni los intervalos de SignalR. [Detalle, instalación, Nexo y pruebas desde Visual Studio](docs/migracion/PARTICIPACION_RENDIMIENTO_PRESENCIA.md).

### Probado

TypeScript y Vite correctos; **60/60** pruebas frontend y **62/62** de dominio/transporte. Compilación Release con cero advertencias y errores; `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build` confirma el modelo sin cambios pendientes.

**135/135** casos de regresión HTTP/PostgreSQL ([evidencia](docs/migracion/evidencia-participacion-regresion.json), corrida `native-postgres-20261006-234641-10f1571c`). **13/13** comprobaciones finales específicas de participación, presencia y rendimiento ([evidencia](docs/migracion/evidencia-participacion-postgresql.json), `native-postgres-20261007-003025-959cb54a`): incluyen contexto propio, permisos, vista previa obsoleta, auditoría atómica, carreras con captura, reactivación sin pérdida, enviados inmutables, restricciones de delegación, colisión de colores y límite/asociaciones ILDA. Son grupos parcialmente superpuestos; no se suman como 148 casos distintos.

**31/31** comprobaciones EF en PostgreSQL desechable ([evidencia](docs/migracion/evidencia-participacion-migraciones.json), `native-postgres-20261007-001318-f3d1379c`): preservación del esquema previo, repetición sin cambios, recuperación transaccional, actualización y protección de enviados. Dos aplicadores EF simultáneos pueden competir por DDL: se verificó convergencia al repetir secuencialmente, no éxito simultáneo garantizado. Instalación con un solo operador.

**30/30** comprobaciones Edge de captura ([evidencia](docs/migracion/evidencia-participacion-captura-react.json), `native-postgres-20261007-002227-02515193`), sin errores JavaScript: eliminación directa y concurrente, Select, foco, pestañas/sesiones, reconexión, criterios individuales, enviados, revocación central y tres ciclos de 15 segundos de SignalR. Se corrigió la prioridad CSS del contorno frente a MUI tras detectarla en navegador. No se cuentan como aprobados los dos intentos anteriores que detectaron la expectativa fija de ámbar y el contorno oculto.

Medición sintética del motor con 800 filas: media **4,87 → 0,075 ms** por cambio. Inicio con 102 áreas: **311 → 8 consultas EF**; 21,4 s antes y 1,02–2,66 s después en estas corridas locales. [Mediciones y límites](docs/migracion/evidencia-participacion-rendimiento.json). El bundle anterior montaba siete pestañas y 7.445 controles; no completó su ensayo de escritura, por lo que no es una latencia válida ni una prueba aprobada.

Revisión final del bundle actual con 800 filas y **tres escenarios agrupados aprobados** ([evidencia](docs/migracion/evidencia-participacion-navegador.json), `native-postgres-20261007-003326-318d776e`): sólo una pestaña montada; apertura 2,23 s; escritura a siguiente frame media 28 ms/p95 45,4 ms en 31 eventos. Configuración y presencia revisadas en **1920×1080, 1366×768, 768×1024 y 390×844**, sin desbordamiento de página ni errores JavaScript; las tablas conservan su desplazamiento horizontal interior. Contorno de 2 px, avatar, lectura en otra pestaña, liberación automática, movimiento reducido, Cancelar sin cambios y confirmación comprobados. [Capturas y reproducción](docs/migracion/PARTICIPACION_RENDIMIENTO_PRESENCIA.md#capturas). Todos los clústeres de estas pruebas quedaron detenidos.

### Pendiente

Publicar el módulo en Nexo y aplicar la migración institucional corresponde al operador, después de su revisión. La aceptación con identidades y jerarquías institucionales reales continúa pendiente. El mensaje recibido termina en «Al pasar el mouse sobre el»; el avatar tiene tooltip con el nombre, sin asumir el texto faltante. No hubo conexiones institucionales, cambios de credenciales/Herd, push ni despliegue.

## Colaboradores asignados desde Nexo — entrega anterior 2026-10-06

### Implementado

Dos vías de autorización independientes en `FormAccess.Scopes`: concesión explícita `central` + rol efectivo coincidente, calculada por adscripción vigente, o vínculo local + concesión `aplicacion`, con las validaciones delegadas conservadas. No hay fallback por `Has`, altas locales artificiales ni conversión de una revocación local pendiente en acceso central. Local resuelve un solo formato nivel 2/3; dependencias, su nivel 2 y la rama nivel 3. Tipo N sigue excluido y los IDs de jerarquía no se sustituyen por claves visibles.

`RequestAccess` consulta concesiones vigentes aun sin enlaces locales y usa la identidad representada cuando corresponde. Es la misma autorización para listado, apertura, reservas, guardado/eliminación y SignalR. No concede envío, administración ni representación a los colaboradores. El estado vacío explica problemas de empleado, adscripción, jerarquía o asignación. Se conserva el trabajo previo de eliminación directa y captura colaborativa.

La fuente de Nexo ya publica los orígenes reales `central` y `aplicacion`; no necesita cambios de código para distinguirlos. No requiere migración EF. [Implementación, comandos y pruebas](docs/migracion/COLABORADORES_CENTRALES.md); [configuración de ambas vías](docs/migracion/CONFIGURACION_NEXO_ENTRA.md).

### Probado

TypeScript y Vite correctos; **57/57** pruebas frontend y **61/61** de dominio/transporte. Compilación Release de la solución con cero advertencias y errores. **31/31** pruebas HTTP/PostgreSQL de captura y colaboradores (23 de captura + 8 centrales), incluidos enviados inmutables y cambios/retiros de alcance. [Evidencia seleccionada de backend](docs/migracion/evidencia-colaboradores-centrales-postgresql.json), extraída de `.artifacts/native-postgres-20261006-184324-cf999ac5/verification.json`; su intento de navegador se registra aparte.

**30/30** comprobaciones finales Edge sobre React → ASP.NET → PostgreSQL, sin errores JavaScript: los 27 casos de captura conservados y tres de colaboración central. La revocación durante una escritura pendiente devuelve 403, conserva la propuesta visible, no altera la versión compartida y termina SignalR por revalidación. El estado vacío explica empleado ausente. [Evidencia de navegador](docs/migracion/evidencia-colaboradores-centrales-react.json), corrida `.artifacts/native-postgres-20261006-185205-3459ed50`, código 0 y clúster detenido. `-BrowserOnly` registra un grupo agregado, no una sola comprobación de interfaz.

**8/8** pruebas del SQL generado desde la fuente local de Nexo en otra base sintética, incluida su consulta de concesiones: confirma separación `central`/`aplicacion`, revocación y suspensión. [Evidencia SQL Nexo](docs/migracion/evidencia-colaboradores-centrales-nexo.json). Corrida `.artifacts/native-postgres-20261006-184131-6bced401`, clúster detenido. No fue una consulta a Nexo institucional.

Intentos previos conservados: `.artifacts/native-postgres-20261006-183043-648c2348` tuvo 120 casos aprobados y seis fallidos por dos sentencias parametrizadas juntas en el preparador central (cinco casos centrales y navegador). Se separaron. La corrida `184324-cf999ac5` pasó los 31 casos de backend, pero su navegador encontró que una prueba previa había retirado el módulo del colaborador; el servidor denegó correctamente. Se corrigió la publicación de módulos **del fixture** y se repitió sólo el navegador completo, con los 30 resultados aprobados indicados arriba. No se cuentan esos intentos como suites completas aprobadas. Todos sus clústeres quedaron detenidos.

### Pendiente y límites

Comprobar por el operador la publicación institucional de `nexo_concesiones` y las asignaciones explícitas. Si la vista existente ya coincide con el contrato de la fuente, no hace falta republicar Nexo; si está desactualizada, aplicar la publicación existente sin recrear recursos. No hubo conexiones a bases institucionales, cambios de credenciales, modificación de Herd, push ni despliegue. La validación sintética no certifica la configuración institucional real.

## Eliminación directa de registros — entrega anterior 2026-10-06

### Implementado

El icono rojo permite abrir la confirmación sin enfocar campos ni tener reserva propia; foco del icono y Cancelar no adquieren reservas. `removalReason` conserva permisos, carga, enviados, ILDA, propuestas pendientes y reservas ajenas con nombre/otra pestaña. Al confirmar, `engine.prepare(keys)` reserva el registro y sus relaciones. Se comparan contenido y versiones por ID antes y después de esperar; cambios requieren cancelar y revisar. El diálogo espera el guardado, bloquea doble clic y después libera; los fallos conservan recuperación contextual incluso si ya no aparece la fila en la propuesta.

Se corrigió también el orden de la confirmación de reserva: su fecha PostgreSQL se obtiene después del commit, con apertura/cierre explícitos de conexión EF, para no invalidarla con una lectura simultánea tardía. No prolonga el vencimiento ni concede escritura desde SignalR. Se conservan `canEdit`, versiones, auditoría, relaciones y enviados inmutables. Sin migraciones ni dependencias nuevas.

Archivos corregidos, comportamiento y comandos: [ELIMINACION_DIRECTA.md](docs/migracion/ELIMINACION_DIRECTA.md).

### Probado

TypeScript y Vite correctos; **57/57** pruebas frontend. Compilación Release de `tdv2.slnx` sin errores ni advertencias. **23/23** casos de captura en PostgreSQL desechable, incluida la lectura concurrente durante adquisición, relaciones, ILDA, revocación, sesiones/pestañas, auditoría, envío e inmutabilidad.

**27/27** comprobaciones Edge sobre React → ASP.NET → PostgreSQL, sin errores JavaScript. Incluyen cargar y eliminar sin enfocar campos en las cuatro tablas, Cancelar sin reservas ni cambios, otra pestaña y relaciones ocupadas, liberación que rehabilita automáticamente, contenido actualizado durante el diálogo/adquisición, competencia de dos sesiones (200/409, un PATCH y un incremento de versión) y recuperación explícita tras fallo SQL. La regresión también conserva autoguardado, prioridad, sesiones revocadas, tres intervalos SignalR y envío inmutable.

Evidencia: [PostgreSQL](docs/migracion/evidencia-eliminacion-directa-postgresql.json), [navegador](docs/migracion/evidencia-eliminacion-directa-react.json), [captura del conflicto entre sesiones](docs/migracion/eliminacion-reserva-ajena.png). Corrida final `.artifacts/native-postgres-20261006-174139-1410854d`, código 0 y clúster detenido (sin `postmaster.pid`). El total agregado 24 incluye 23 casos de backend y una entrada para el grupo de navegador; no representa 24 pruebas de backend.

Los intentos previos se conservan en `.artifacts`: `native-postgres-20261006-172736-869505a2` detectó la lectura que invalidaba la reserva; `native-postgres-20261006-173308-99b8ee8e` permitió corregir la expectativa del fallo SQL (503); `native-postgres-20261006-173943-b9769ef5` detectó que EF cerraba la conexión al confirmar. Se corrigieron antes de la corrida final y no se cuentan como ejecuciones aprobadas.

### Pendiente y límites

Publicación y aceptación institucional por el operador: el servidor publicado no se actualizó. No se modificaron bases institucionales, credenciales ni Herd; sin push ni despliegue. Las pruebas usan identidades y contenido sintéticos.

## Distribución compacta de Procesos operativos — entrega anterior 2026-10-05

### Implementado

Indicadores de 80 px en escritorio, números de 26 px y separación de 14 px; etiquetas completas en móvil. Alcance y pestañas más próximos, conteo único agrupado con título y acciones del listado, filtros sin contenedor de relleno adicional. Filas normales de 80 px separadas 8 px, nombre prioritario, clave/cantidades secundarias y avance a la derecha; crecimiento libre para nombres largos. Se conservan cabecera, tipografía, permisos, cálculos, tipo N, acceso a formatos sin dependientes y cambios locales. No se modificó captura, backend ni esquema.

### Probado

TypeScript, Vite y compilación Release correctos, cero advertencias/errores .NET; **55/55** frontend. Revisión visual y funcional del bundle real con catálogo sintético en 1920×1080, 1366×768, tableta 768×1024 y móviles 390×844/320×740, sin desbordamientos ni errores JavaScript. En 1366×768 la primera área sube de 600 a 459 px y caben tres filas completas en lugar de una; en 1920×1080 pasa de cuatro a siete filas. Comprobados búsqueda, filtros, pestañas, expansión, paginación, formato propio sin dependientes, carga de Actualizar, teclado y movimiento reducido.

Capturas, medidas antes/después, comandos y reproducción en Visual Studio: [DISTRIBUCION_PROCESOS.md](docs/migracion/DISTRIBUCION_PROCESOS.md).

### Pendiente y límites

Aceptación institucional por el operador. Las capturas usan props sintéticas y no certifican de nuevo la autorización del servidor. No hubo conexiones a bases, cambios de credenciales/Herd, migraciones, push ni despliegue.

## Cabecera y elegibilidad de UR tipo N — entrega anterior 2026-10-05

### Implementado

- Se integra la cabecera ya ajustada: logo original a 180/150 px, separación de 32 px hasta el primer módulo en escritorio y barra blanca compacta. Procesos operativos mantiene título 26/24 px semibold, descripción solicitada de 14 px, separación de 4 px y acciones separadas 10 px. Colaboradores sigue azul y autorizado; Actualizar es de texto, con icono azul, carga y foco visible. En móvil las acciones pasan debajo cuando falta espacio; no se cambia la tipografía ni la organización de módulos.
- `UnitDirectory.IsEligible` y `eligibleArea` excluyen **únicamente tipo N**, normalizando espacios exteriores y mayúsculas para comparar. Se retira la exclusión por tipo 0: esas UR pueden aparecer si cumplen actividad, alcance y demás condiciones. La regla compartida alcanza formatos, directorios, contadores y selectores, también del administrador; los formatos siguen siendo sólo de niveles **2 y 3**.
- Los nodos N permanecen internamente para resolver adscripciones y ramas. El directorio no toma un nodo excluido como agrupador aunque llegue como principal explícito. Se conservan catálogo, formatos históricos y valores originales de tipo, ID y clave; `06000` se muestra como `6000` y ambas representaciones se pueden buscar.
- No hay cambios de esquema, migraciones ni dependencias. Autenticación, permisos, representación, reservas automáticas, autoguardado, SignalR y envío conservan su implementación.

### Probado

TypeScript, Vite y compilación Release de la solución correctos, sin errores ni advertencias de .NET. **55/55** pruebas frontend y **54/54** de dominio/transporte. Incluyen variantes `N`, `n`, espacios, tipo `0` en niveles 2/3, exclusión del nivel 4, ramas con nodos auxiliares y conservación de claves originales.

**117/117** casos de servidor/PostgreSQL y **4/4** recorridos Edge correctos, cero errores JavaScript. La prueba HTTP del administrador excluye `N`, `n` y espacios de formatos/selectores; permite tipo 0 en niveles 2/3; conserva el registro histórico completo antes y después del cambio sintético de tipo. La regresión cubre permisos, reservas por sesión/pestaña, autoguardado, versiones atrasadas, revocación, auditoría, envío concurrente e inmutabilidad. El navegador comprueba además supervisor, colaboradores de nivel 3, claves, jerarquía y móvil con movimiento reducido. Evidencias: [PostgreSQL](docs/migracion/evidencia-ur-tipo-n-postgresql.json), [React](docs/migracion/evidencia-ur-tipo-n-react.json). Corrida `.artifacts/native-postgres-20261006-000019-c8c8e5e8`; el total agregado 118 incluye una entrada para el grupo de navegador, no son 118 pruebas de backend.

Vista del bundle real en Edge con datos sintéticos del administrador en siete anchos: 320, 390, 600, 768, 901, 1024 y 1440 px. Tipo N ausente, tipos 0 de niveles 2/3 visibles y agrupados, contadores y búsqueda `06000`/`6000` correctos. Sin desbordamientos; comprobados separación del logo, alineación de acciones, carga de Actualizar, foco, permisos de Colaboradores, submódulos, menú móvil y movimiento reducido. Capturas revisadas: [escritorio](docs/migracion/ur-tipo-n-escritorio.png), [móvil](docs/migracion/ur-tipo-n-movil.png). [Medidas y comprobaciones visuales](docs/migracion/evidencia-ur-tipo-n-visual.json).

Comandos desde la raíz, con Node/npm y .NET 10 en PATH:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false
$env:TDV2_TEST_BROWSER_FLOW='scope'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El último comando de pruebas crea y detiene un PostgreSQL desechable local, usa dobles de Microsoft/Nexo y no toma la conexión institucional de User Secrets. La corrida terminó con código 0 y el clúster detenido. No ejecutar `database update` para esta corrección.

### Pendiente y límites

La aceptación institucional con cuentas y catálogos reales queda pendiente del operador. No se accedió a bases institucionales, credenciales, Herd ni Ubuntu; no hubo push ni despliegue. La publicación externa de Nexo documentada en entregas anteriores no se modifica: se siguen exigiendo sus capacidades vigentes, y cualquier cambio de esa publicación necesita un trabajo separado autorizado. La prueba visual aislada usa props sintéticas y no sustituye las pruebas del servidor.

Para revisar desde Visual Studio: abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React** y ejecutar F5. En Procesos operativos comprobar la cabecera, Actualizar y la adaptación al estrechar la ventana. Para escribir o alterar tipos, usar exclusivamente el verificador sintético indicado en la evidencia; no modificar catálogos institucionales para probar. El arranque habitual no aplica migraciones.

## Cabecera y encabezado de Procesos operativos — entrega anterior 2026-10-05

### Implementado

Logo original ampliado 20 % (180 px escritorio, 150 px móvil), proporción conservada, barra blanca de 68 px y separación de 32 px hasta el primer módulo en escritorio. Se conserva la jerarquía de módulos/submódulos. Sólo Procesos operativos usa título 26/24 px semibold, descripción solicitada de 14 px en gris secundario y separación de 4 px. Acciones a la derecha y al nivel del título, con 10 px entre ellas; se acomodan debajo de la descripción cuando falta espacio. Colaboradores conserva icono, botón azul y condición de permiso. Actualizar usa texto/icono azul sin contorno, indicador MUI de carga, bloqueo durante la solicitud y foco visible con teclado. Tipografía y cambios locales conservados.

### Probado

TypeScript y Vite correctos; `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false` sin advertencias ni errores. Vista del bundle real en Edge con datos sintéticos, sin ASP.NET ni conexiones a bases/autenticación: siete anchos (320, 390, 600, 768, 901, 1024 y 1440 px), sin desbordamientos. Verificados carga/GET de Actualizar, foco por teclado, ocultación de Colaboradores según su prop, menú de submódulos y navegación móvil con movimiento reducido. Revisión visual de las capturas: [escritorio](docs/migracion/cabecera-inicio-escritorio.png), [móvil](docs/migracion/cabecera-inicio-movil.png). [Medidas y comprobaciones](docs/migracion/evidencia-cabecera-inicio.json).

### Alcance y pendientes

No se cambió lógica de formatos, permisos del servidor, autenticación ni bases. No hubo despliegue ni push. La comprobación visual utiliza props sintéticas; no representa una nueva verificación de autorización institucional. Los pendientes institucionales de las entregas anteriores se conservan.

## Reserva automática al entrar en el registro — entrega anterior 2026-10-05

### Implementado

- Retirado **Editar registro**: entrar directamente en un campo, casilla, criterio o Select solicita la reserva. No hay botones ni confirmaciones para iniciar/terminar edición. Consultar el texto estático de una fila no la reserva.
- Antes de aceptar respuestas se confirma reserva, versión y contenido vigente. Se muestra «Preparando edición…» durante la espera. El Select abre tras confirmar; una intención pendiente no se aplica después de abandonar el registro. Un menú cerrado al perder reserva no se reabre por una adquisición posterior.
- Al salir se guarda y después se libera automáticamente. Cambiar campos o abrir Select/Dialog conserva el bloque. Las otras sesiones reciben por SignalR el valor/estado y pueden entrar directamente sin recarga. También se admite una nueva interacción si el campo conservaba foco después de liberarse o vencer.
- La fila ocupada conserva fondo ámbar suave, candado y nombre una sola vez; otra pestaña de la misma sesión tiene su texto específico. Reserva propia y preparación son indicadores discretos, sin acciones de edición. Se conserva accesibilidad, tipografía y movimiento reducido.
- Se mantienen las correcciones de `FormHub.Watch`, permisos, versiones, auditoría, autoguardado, recuperación contextual explícita ante fallos y formatos enviados inmutables. No se añaden dependencias ni cambios de esquema.
- El verificador aislado usa Release sin apphost para evitar el bloqueo del ejecutable Debug abierto por Visual Studio; no se detuvo ese proceso.

### Probado

- TypeScript y Vite correctos. Compilación de `tdv2.slnx` Release sin advertencias ni errores. **54/54** pruebas frontend y **53/53** dominio/transporte.
- **22/22** casos de captura HTTP/PostgreSQL en base desechable: reservas de usuarios/sesiones/pestañas, bloques distintos, versión vigente, vencimiento/reasignación, solicitudes atrasadas, idempotencia, revocación y carreras de envío. [Evidencia PostgreSQL](docs/migracion/evidencia-reserva-automatica-postgresql.json), corrida `225426-54c0e628`. Su total 23 incluye además una entrada agregada para el navegador; no son 23 casos de backend.
- **21/21** recorridos Edge del frontend compilado, cero errores JavaScript: [evidencia React](docs/migracion/evidencia-reserva-automatica-react.json), corrida `225926-48c179e4`. Dos sesiones autenticadas con cookies independientes compiten al enfocar (una respuesta 200 y otra 409); salir guarda/libera y la otra entra sin recarga. También pasan dos pestañas, bloques/criterios distintos, preparación sin escritura, Select, casillas con teclado, Cancelar y eliminación por ID, desconexión/recuperación, permisos y enviados inmutables. [Captura móvil, movimiento reducido](docs/migracion/reserva-automatica-movil.png).
- SignalR abierto durante tres intervalos de 15 segundos, reserva vencida sin actividad, cancelación/reapertura limpia y revocación por logout. Ambos clústeres de evidencia finalizaron detenidos. El primer intento de iniciar PostgreSQL fue impedido por el token restringido de Windows; se ejecutó fuera del aislamiento y exclusivamente en el clúster desechable. Un intento posterior encontró Debug ocupado por Visual Studio; se corrigió el verificador para usar Release antes de las corridas aprobadas.

Comandos exactos y recorrido con dos sesiones: [RESERVAS_SIGNALR.md](docs/migracion/RESERVAS_SIGNALR.md). Las evidencias de la entrega anterior se conservan por separado.

### Pendiente y límites

La aceptación manual con F5, identidades institucionales y proxy real corresponde al operador en un entorno de pruebas. Las pruebas locales usan ASP.NET, React/MUI, Edge y PostgreSQL reales con Nexo/Microsoft sintéticos. No se modificaron bases institucionales, credenciales, Herd ni Ubuntu; sin push, despliegue ni migración. Una propuesta sin confirmar vive en la pestaña; se mantienen el aviso de salida, la recuperación explícita y la descarga de emergencia.

## Exclusión por registro y SignalR — entrega anterior 2026-10-05

### Implementado

- `Watch` usa un temporizador periódico de 15 segundos, sin cancelar la lectura por intervalo. Cancelar el stream/desconectar termina limpiamente; las cancelaciones de consultas se filtran por sus tokens. Se mantienen reautorización, revocación, contexto y limpieza del iterador. No se alteró el depurador.
- La adquisición PostgreSQL devuelve contenido y versión confirmados. React no acepta modificaciones antes de reservar, ni durante liberación, desconexión, vencimiento o pérdida de titular. Altas/cascadas reservan los bloques necesarios; preguntas y criterios mantienen su granularidad.
- Un indicador por registro distingue reserva propia, otra persona y otra pestaña de la misma sesión. Consulta/copia disponibles, eliminación bloqueada si no hay reserva propia. Fondo ámbar, candado y texto legible; transición de 180 ms con movimiento reducido.
- Autoguardado y Guardar borrador conservados. Guardar precede a liberar; Select/Dialog no liberan por cambiar foco dentro del registro. El foco durante la carga inicial se retoma después de verificar acceso. Reintentos conservan UUID/cuerpo; recibos antiguos no sustituyen versiones compartidas recientes.
- Se sustituye el panel global habitual por mensaje y **Resolver** junto al registro afectado. Comparación contextual, confirmación explícita, conservación de la respuesta compartida y descarga de emergencia. Una reconexión no recupera propuestas automáticamente.
- Sin nuevas dependencias ni migración; se preservaron cambios locales, estructura, permisos, tipografía, OAuth y bloqueo de enviados. [Diseño y recorrido en Visual Studio con dos sesiones](docs/migracion/RESERVAS_SIGNALR.md).

### Probado

- TypeScript, Vite y compilación Release correctos; backend sin advertencias. **52/52** frontend y **53/53** dominio/transporte. EF: sin cambios pendientes del modelo.
- **117/117** casos HTTP/PostgreSQL: [evidencia](docs/migracion/evidencia-reservas-postgresql.json). Incluye 22 de captura/envío, adquisición con contenido vigente, competencia simultánea entre dos usuarios, sesiones/pestañas, escrituras en bloques distintos, versiones, vencimiento/reasignación, auditoría e inmutabilidad. También valida permisos, representación y sincronizaciones. Los 22 casos dirigidos están incluidos en los 117, no se suman otra vez.
- **47/47** recorridos Edge: [19 formatos](docs/migracion/evidencia-reservas-react-formatos.json), [13 acceso](docs/migracion/evidencia-reservas-react-acceso.json), [4 alcance](docs/migracion/evidencia-reservas-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-reservas-react-sincronizaciones.json). Cero errores JS. Incluye reserva pendiente sin aceptar texto, foco mantenido, criterios independientes, Select, Cancelar, eliminación por ID, desconexión/reconexión con propuesta conservada, envío inmutable y revocación del stream por logout.
- SignalR permaneció abierto al menos 45 segundos, con cuatro entregas durante tres intervalos de 15 segundos. La reserva venció sin actividad; cancelar/reabrir el stream terminó sin error y sin falso conflicto. [Captura móvil con reserva ajena y movimiento reducido](docs/migracion/reserva-otra-pestana-movil.png).

Comandos exactos desde la raíz: [guía de verificación](docs/migracion/RESERVAS_SIGNALR.md#verificación-desde-la-raíz). Regresión: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1`, corrida `220508-35bbd093`. Sus 117 casos de backend pasaron; el grupo navegador detectó una importación faltante de `expect` en la prueba de sincronizaciones. La evidencia PostgreSQL conserva explícitamente ese resultado original. Se corrigió la prueba y se ejecutó nuevamente **todo** el grupo con `-BrowserOnly`: corrida `221547-584400be`, 47 recorridos aprobados. No se presenta la corrida inicial como aprobación del navegador. Se corrigieron además el foco durante carga y la visualización de recibos antiguos, con pruebas específicas. Todos los clústeres de prueba terminaron detenidos.

### Pendiente y límites

Aceptación interactiva con F5 y sistemas institucionales reales. Las pruebas usan identidades, concesiones, respuestas y envíos sintéticos; no se tocaron bases institucionales, Herd, credenciales ni Ubuntu. No se hizo push ni despliegue. No se creó/aplicó una migración. Los borradores sin confirmar permanecen en la pestaña y pueden descargarse; un cierre forzado no implica persistencia local automática. La guía anterior separa la reproducción sintética desde Visual Studio de la aceptación institucional.

## Ajustes de captura y navegación — entrega anterior 2026-10-05

### Implementado

- Se retiraron Datos de la sesión e Imprimir, conservando Contexto, identificación de UR, Guardar borrador, autoguardado y descarga/recuperación de propuestas. El JSON histórico de encabezado no se normaliza ni se exige para enviar.
- El servidor recalcula la proyección de avance de borradores con los 90 puntos restantes normalizados a 100, sin escribir durante GET ni inventar respuestas. Guardar/envío persisten el cálculo. Los enviados conservan avance, contenido e instantánea, incluso ante nuevas sincronizaciones.
- Prioridad usa Select MUI con placeholder y escala vigente 1–5. Los valores históricos se conservan hasta una reclasificación explícita. El menú en portal mantiene el bloque de edición; solicitudes de foco canceladas no adquieren reservas tardías y salir espera adquisiciones en curso antes de liberar. Guardar borrador sigue recibiendo el clic aunque el blur inicie autoguardado; el editor serializa las operaciones.
- Eliminación en las cuatro tablas mediante diálogo MUI, Cancelar enfocado, icono rojo disponible y tooltip para bloqueo real. La confirmación usa ID estable y comprueba la fila actual; eliminar proceso incluye evaluación y desvinculación de sistemas/datos. Se mantienen todas las reservas y restricciones de ILDA.
- Navegación superior agrupa sólo la jerarquía autorizada por Nexo: Procesos operativos y Configuración con submódulos. Selección activa, teclado, móvil y movimiento reducido; acciones de cuenta y reglas OAuth/representación sin cambios.
- Revisión y envío está únicamente al final de Acuerdos. `FormReview` comparte pendientes concretos entre interfaz y requisitos del servidor. Enviar exige respuestas guardadas y una revisión válida, además de permisos, versión, reservas y auditoría ya existentes. Confirmación MUI con UR y bloqueo definitivo; consulta muestra fecha e identidad efectiva. Sin reapertura.
- No se añadieron librerías ni migraciones. Los cambios reutilizan ASP.NET/EF/React y se separan en componentes pequeños.
- Guardar borrador indica actividad hasta terminar la liberación; una eliminación confirma y libera toda su cascada. Una elección explícita de Contexto durante la carga prevalece sobre la última sección recuperada del servidor.

### Probado

| Comando desde la raíz | Resultado y alcance |
|---|---|
| `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false` | Correcto, cero errores y advertencias |
| `dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false` | 53/53 dominio/transporte; encabezado histórico conservado y formato completo sin datos de sesión |
| `npm.cmd --prefix ClientApp run types:check`, `run test:forms` y `run build` | TypeScript y Vite correctos; 41/41 frontend: concurrencia, respuestas atrasadas, foco/reservas, recuperación, eliminación por ID y jerarquía |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser` | [115/115 regresión PostgreSQL](docs/migracion/evidencia-ajustes-regresion.json). Posteriormente se agregó el caso de enviado histórico de la fila siguiente |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser` | [21/21 captura y envío](docs/migracion/evidencia-ajustes-edicion.json): incluye proyección sin escribir, 100 % con prioridad histórica aún pendiente, enviados históricos inmutables, cascadas incompletas/reserva ajena/ILDA rechazadas, sesiones independientes y carreras |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Sin cambios de modelo; no requiere conexión institucional |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly` (recorridos dirigidos indicados debajo) | **43/43**: [15 formatos](docs/migracion/evidencia-ajustes-react-formatos.json), [13 acceso](docs/migracion/evidencia-ajustes-react-acceso.json), [4 alcance](docs/migracion/evidencia-ajustes-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-ajustes-react-sincronizaciones.json). Escritorio/móvil, movimiento reducido, teclado, Cancelar en cuatro tablas, borrado concurrente por ID, pendientes, confirmación/envío y bloqueo posterior. Permisos, representación, revocaciones e ILDA conservados; cero errores JS |

Las escrituras se hicieron sólo en clústeres desechables con datos sintéticos. No se cuentan intentos fallidos como aprobación. Los 21 casos de edición incluyen casos ya presentes en la regresión, no se suman como pruebas independientes adicionales.

Capturas sintéticas revisadas: [prioridad móvil](docs/migracion/ajustes-prioridad-movil.png), [enviado en consulta](docs/migracion/ajustes-formato-enviado.png), menú en [escritorio](docs/migracion/ajustes-modulos-escritorio.png) y [móvil](docs/migracion/ajustes-modulos-movil.png). Los intentos previos detectaron carreras de foco/guardado y liberación, y una elección de Contexto ignorada durante carga. Se corrigieron antes de las corridas aprobadas. Las pruebas esperan carga y finalización de solicitudes, no un `networkidle` que ya había ocurrido; la prueba de requisitos de envío vence reservas de casos anteriores para aislar esa validación. Se corrigieron también expectativas de menú para respetar los módulos realmente publicados por cada fixture de Nexo, sin ampliar sus permisos.

Corridas aprobadas bajo `.artifacts/native-postgres-20261005-*`: backend `195115-6ef85165`; edición `201428-c72e92e8`; formatos `205514-516f6638`; acceso `205842-7072f304`; alcance `210050-d091fed9`; sincronizaciones `210320-d7aab87d`. Todos los clústeres terminaron detenidos. En la corrida de formatos pasó ese recorrido y falló la expectativa inicial del menú de acceso; acceso se corrigió y verificó por separado. No se presenta esa corrida como aprobación del grupo completo. Para reproducir cada recorrido: `$env:TDV2_TEST_BROWSER_FLOW='formats'` (o `access`, `scope`, `sync`), ejecutar `-BrowserOnly` y después `Remove-Item Env:TDV2_TEST_BROWSER_FLOW`. Sin esa variable se ejecutan los cuatro.

### Pendiente y límites de esta entrega

La aceptación con Nexo/Microsoft y usuarios institucionales reales sigue pendiente. No se iniciaron sesiones institucionales, no se modificaron Herd, secretos ni bases institucionales, y no hubo push, despliegue ni acceso a Ubuntu. No se aplicaron migraciones remotas. La tercera migración de la entrega anterior y la configuración institucional documentada siguen siendo pasos del operador; estos ajustes no añaden requisitos de esquema.

Abrir `tdv2.slnx` → **TDV2 HTTPS + React** → F5. El perfil y `/connect` no cambiaron; F5 no migra. [Recorrido y comandos de prueba](docs/migracion/COLABORACION_ENVIO.md#comprobar-los-ajustes-en-visual-studio). La evidencia de F5 de la entrega anterior es histórica; no se presenta como una nueva validación interactiva de este cambio.

## Captura colaborativa, supervisión y envío — entrega anterior 2026-10-05

### Implementado

- `responsable_ur_supervisor` concentra responsabilidad y consulta institucional sin administración. Nivel 2 edita su rama autorizada; nivel 3 su formato. `responsable_ur_institucional` continúa como alias transitorio. El catálogo compartido de Nexo no se renombró; se conservan roles y asignaciones de consulta. La representación mantiene separados actor real y efectivo.
- La delegación valida encargado, acceso vigente, rama y rol en TDV2 y Nexo. Nivel 3 sólo agrega colaboradores locales de su UR/subordinadas a su mismo formato; nivel 2 conserva ambos tipos. Se ampliaron los filtros TDV2 de las dos fuentes Nexo ya corregidas, preservando cambios locales y las reglas de otras aplicaciones. [Configuración y transición exactas](docs/migracion/CONFIGURACION_NEXO_ENTRA.md).
- PostgreSQL arbitra reservas de 45 segundos por fila/grupo de campos, sesión/pestaña/contexto y versión. Autoguardado parcial, UUID y recibos idempotentes, renovación por cambios, liberación tras confirmar, recuperación explícita de propuestas y auditoría atómica. No hay transacciones abiertas durante la captura. El guardado completo anterior se rechaza con 428 y se retiró su implementación.
- SignalR sólo entrega contenido autorizado, revalida Nexo/ticket/contexto y recupera estado al reconectar. Origin se compara con la configuración pública; se admite WebSocket HTTP/2 CONNECT únicamente en este canal de consulta. Las mutaciones y negociación HTTP siguen exigiendo CSRF. No se renuevan reservas mediante latidos de pantalla.
- Enviar guarda pendientes, confirma, exige responsable/avance/requisitos/ausencia de reservas ajenas y se serializa con reservas/guardados. Conserva ejercicio, fecha UTC, versión, identidad real/efectiva, auditoría e instantánea de unidad/respuestas/ILDA. Trigger de inmutabilidad para todos, incluidos administradores y sincronizadores. Sin reapertura.
- React conserva MUI/Roboto e identidad visual: Contexto incorpora los campos de Encabezado, siete secciones montadas, última sección confirmada, navegación superior/móvil, prioridad 1–5 accesible sin defecto y estado compacto sin barra fija. No hay conversión automática de prioridades históricas. Se conservan jerarquías/históricos tipo 0 e identificadores; las claves se formatean sólo al mostrar/buscar.
- Microsoft valida el ID token antes de usar el claim opcional login_hint. Inicio habitual sin selección forzada, hints de cuenta real y Usar otra cuenta con revocación/CSRF. logout_hint nunca se deriva del correo ni de la representación.
- EF: `20261005135417_CollaborativeFormsAndSubmission`, seis columnas y tres tablas adicionales; 17 entidades y seis relaciones. Backfill de ejercicio sin alterar respuestas. Down rechaza retirar la protección si existen envíos. Migración explícita, nunca F5. [Diseño, instalación y comandos](docs/migracion/COLABORACION_ENVIO.md).

### Probado

| Comando desde la raíz | Evidencia y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false` | 53/53 dominio y transporte. Las pruebas del guardado completo retirado se sustituyeron por pruebas de reservas/parches con PostgreSQL real |
| `npm.cmd --prefix ClientApp run types:check` y `npm.cmd --prefix ClientApp run test:forms` | TypeScript correcto; 33/33 frontend: respuestas atrasadas, reintentos, propuesta pendiente, recuperación sin autoguardado, renovación atrasada y selección de sección durante carga |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly` | [29/29 PostgreSQL real](docs/migracion/evidencia-colaboracion-migraciones.json): base vacía y actualización con contenido, repetición, script idempotente, restricciones, rollback y rechazo de Down tras envío. [Huella de esquema nuevo](docs/migracion/evidencia-colaboracion-esquema.json) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser` | [112/112 backend/PostgreSQL](docs/migracion/evidencia-colaboracion-regresion.json), con fuentes externas sintéticas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser` | [17/17 casos de captura](docs/migracion/evidencia-colaboracion-edicion.json): sesiones independientes, dos pestañas, mismo/distinto bloque, vencimiento/reasignación, caducidad antes del commit, revocación, concurrencia con envío, colaborador sin envío, inmutabilidad, prioridad y OIDC firmado |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly` | 40 recorridos Edge: [13 formatos](docs/migracion/evidencia-colaboracion-react-formatos.json), [12 acceso](docs/migracion/evidencia-colaboracion-react-acceso.json), [4 alcance](docs/migracion/evidencia-colaboracion-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-colaboracion-react-sincronizaciones.json). Cero errores JS; escritorio, móvil, teclado y movimiento reducido |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-supervision` | [7/7 con SQL generado por las fuentes reales](docs/migracion/evidencia-supervision-nexo.json), exclusivamente datos y PostgreSQL sintéticos. [Parche adicional](docs/migracion/nexo-supervision.patch) y [hashes coincidentes con las fuentes aplicadas](docs/migracion/evidencia-supervision-nexo-fuentes.json) |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Modelo y snapshot coinciden, sin conexión ni secretos. Script incremental generado en `.artifacts/colaboracion-envio.sql` |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` y `node ClientApp/tests/browser/publish-flow.mjs` | [Paquete final Production/HTTPS](docs/migracion/evidencia-colaboracion-publicacion.json), React compilado sin Vite, sin credenciales, rechazos seguros y cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [tdv2.slnx / TDV2 HTTPS + React](docs/migracion/evidencia-colaboracion-visual-studio.json): Vite 200, lanzamiento 302, React/props 200, privado 401, HTTPS confiable y HMR wss 7136. Sólo páginas anónimas |

Se revisaron capturas de [prioridad móvil](docs/migracion/colaboracion-prioridad-movil.png), [formato enviado](docs/migracion/colaboracion-formato-enviado.png) y [Sincronizaciones móvil](docs/migracion/colaboracion-sincronizaciones-movil.png). Las fuentes externas se bloquean en navegador sintético; la configuración tipográfica original se conserva.

La prueba final de Imprimir generó un [PDF sintético del formato enviado](docs/migracion/colaboracion-formato-enviado.pdf), verificando las siete secciones. Corridas finales: backend `.artifacts/native-postgres-20261005-154331-a579677d`; migraciones `154018-02f01596`; edición `154049-d16e5e8a`; formatos/impresión `154847-ac17674c`; acceso/alcance `152920-19ce7115`; sincronizaciones `153805-e2794a9b`. Los clústeres desechables terminaron detenidos. [Compilación y depuradores de Visual Studio](docs/migracion/evidencia-colaboracion-visual-studio.txt) confirmados; no se autenticaron personas institucionales.

Los intentos fallidos no se cuentan como aprobación. Permitieron corregir CONNECT/WebSocket, el clic Enviar durante blur/autoguardado y una respuesta inicial que reemplazaba la sección seleccionada. Se actualizaron fixtures que aún usaban la clave anterior. El Vite antiguo bloqueó Rolldown durante el primer publish y se detuvo sólo ese proceso de TDV2. El arranque de navegador afectado por `npm ci` se repitió después de terminar la publicación. La migración inicial y las actualizaciones se operan con un único ejecutor: el ensayo concurrente sólo acredita una migración pendiente con historial establecido, no autoriza varios operadores sobre una cadena de migraciones.

Para repetir un recorrido aislado, establecer `$env:TDV2_TEST_BROWSER_FLOW='formats'` o `'sync'` antes de `-BrowserOnly`, y después `Remove-Item Env:TDV2_TEST_BROWSER_FLOW`; los valores admitidos son all/formats/access/scope/sync. Las repeticiones dirigidas figuran junto con los archivos de evidencia, sin sumar varias veces un mismo caso.

### Pendiente institucional

Aplicar la tercera migración con un operador al destino previamente verificado; se conserva la base existente y las dos migraciones iniciales ya aplicadas. Registrar/asociar el rol canónico, revisar asignaciones/catálogos compartidos y publicar la delegación en la conexión Nexo existente. Agregar el claim opcional login_hint al ID token en Entra y probar SSO/salida con las políticas reales. Validar proxy WebSocket, usuarios institucionales, fuentes SII/ILDA y despliegue Ubuntu. No se enviaron formatos institucionales, no se concedieron accesos reales ni se modificaron User Secrets; sincronización automática sigue desactivada. Instrucciones en [Nexo/Entra](docs/migracion/CONFIGURACION_NEXO_ENTRA.md) y [captura/migración](docs/migracion/COLABORACION_ENVIO.md).

## Responsables y consulta institucional — entrega anterior 2026-10-05

### Implementado

- Nivel 3: botón Colaboradores, búsqueda y alta local en su misma UR o descendientes, siempre para el formato del responsable nivel 3. Backend y funciones Nexo rechazan rama ajena y roles de alcance global/dependencias; nivel 2 conserva sus facultades. Vínculo local, concesión central, empleado textual, revocación y auditoría siguen siendo obligatorios.
- Rol `responsable_ur_institucional`: funciona solo como responsable y añade lectura institucional. La responsabilidad sigue siendo institucional; no concede Configuración, Sincronizaciones ni Pruebas de acceso. Representación y vista de prueba conservan sus restricciones.
- EF lee el campo ya existente `tipo_ur`; tipo 0 no es área elegible ni formato, aunque permanece como nodo auxiliar y con su historia. El directorio público conserva ID/código/padre originales y añade `id_ur_principal` para agrupar nivel 2/3 a través de auxiliares. No se añadieron migraciones ni se modificaron las existentes.
- React: claves numéricas sin ceros sólo para presentación/búsqueda, Mis áreas/Todas las áreas, etiquetas Solo consulta, encabezado compacto/adaptable de Sincronizaciones, transiciones breves y movimiento reducido. Actualizar estado no sustituye el borrador ante un error temporal; un rechazo de acceso sigue retirando la vista privada.
- Fuente de Nexo corregida en cuatro archivos, sólo para clave `tdv2`; las demás aplicaciones conservan su comportamiento. Cambios locales previos preservados y hashes comprobados antes de escribir fuera del workspace. [Parche](docs/migracion/nexo-tdv2-delegacion.patch) y [configuración exacta](docs/migracion/RESPONSABLES_UR.md).

### Probado

| Comando desde la raíz | Resultado y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification --no-restore -p:UseAppHost=false` | 58/58 dominio y transporte; dobles en memoria donde corresponde |
| `npm --prefix ClientApp run types:check` y `npm --prefix ClientApp run test:forms` | TypeScript correcto y 24/24 frontend: árbol, tipo 0, búsquedas 06000/6000, autosave, conservación del borrador y revocación |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1` | [95/95 HTTP/PostgreSQL + grupo navegador, 96/96 grupos](docs/migracion/evidencia-ur-postgresql.json). Incluye conservación del padre original, historia tipo 0, ILDA sin mezclar claves, alta/retiro sintéticos, revocaciones, concurrencia y atomicidad |
| Edge de la suite anterior | [9 formatos](docs/migracion/evidencia-ur-react-formatos.json), [12 acceso](docs/migracion/evidencia-ur-react-acceso.json), [4 alcance/UR](docs/migracion/evidencia-ur-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-ur-react-sincronizaciones.json): 36 recorridos, sin errores JS. Escritorio 1440×1000, móvil 390×844 y movimiento reducido; fuentes externas bloqueadas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-tdv2-scope` | [7/7 con funciones y vistas generadas por el código real de Nexo](docs/migracion/evidencia-ur-nexo.json), datos/base sintéticos. Las cuatro fuentes aplicadas coinciden byte a byte con las probadas: [hashes](docs/migracion/evidencia-ur-nexo-fuentes.json). PHP `-n -l` correcto; no se ejecutó la suite Laravel ni su bootstrap |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Sin cambios pendientes de modelo; sin conexión a base ni secretos |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Correcto, incluye npm ci/build; auditoría npm: 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production, HTTPS confiable, React compilado, sin Vite ni secretos](docs/migracion/evidencia-ur-publicacion.json); rechazos 503/401/419 esperados, cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, lanzamiento 302 a HTTPS, React/props 200, privado 401 y HMR wss 7136](docs/migracion/evidencia-ur-visual-studio.json), cero errores JS. No autentica personas institucionales |
| Parche y whitespace | `git -c core.autocrlf=false apply --check --directory=.artifacts/nexo-tdv2-scope/before docs/migracion/nexo-tdv2-delegacion.patch` y `git -c core.autocrlf=false -c core.whitespace=cr-at-eol diff --check`: correctos |

Se revisaron las capturas de [Sincronizaciones escritorio](docs/migracion/ur-sincronizaciones-escritorio.png), [móvil](docs/migracion/ur-sincronizaciones-movil.png) y [consulta institucional móvil](docs/migracion/ur-consulta-institucional-movil.png). Los estilos/tipografías no se sustituyeron; la prueba aislada bloquea descargas externas de fuentes.

Regresión final: `.artifacts/native-postgres-20261005-121554-1445277a`; Nexo: `.artifacts/native-postgres-20261005-115608-3fa5659f`. Clústeres de prueba detenidos. El IDE conserva los documentos y el perfil HTTPS + React quedó en ejecución tras comprobar F5.

Intentos corregidos, no contados como evidencia: dos expectativas iniciales usaban una redirección como JSON y 403 en lugar del 409 existente para representación de consulta. Se corrigieron y repitió toda la regresión. La revisión final añadió padre original e historia tipo 0 y se repitió la suite con ese código. El primer publish encontró un Vite previo bloqueando Rolldown; se detuvo sólo el proceso cuya ruta correspondía a TDV2 y se publicó correctamente. El sandbox no accedía al certificado HTTPS del usuario; la verificación se repitió fuera del sandbox manteniendo validación de certificado. El parche de Nexo se regeneró conservando los finales de línea originales y pasó la comprobación sin modificar la copia previa.

### Pendiente institucional

Registrar/asociar `responsable_ur_institucional` a `procesos_operativos` (`/inicio`), configurar los roles delegantes/asignables y publicar las funciones actualizadas de la conexión existente de Nexo. Instrucciones en [RESPONSABLES_UR.md](docs/migracion/RESPONSABLES_UR.md). No se ejecutó Artisan/Composer en Herd, no se leyó .env, no hubo altas/retiros reales ni conexiones a bases institucionales, migraciones remotas o sincronizaciones. Las pruebas de concesiones/retiros y programación fueron exclusivamente sintéticas. La sincronización automática institucional permanece sin cambios.

## Proyecto convencional y migraciones EF Core — vigente

### Implementado

- Se reutilizan Controllers para HTTP, Domain para reglas/entidades, Services para casos de uso e Infrastructure para persistencia. Se añadió `Tdv2DbContext`, 14 entidades en Domain/Entities y configuraciones `IEntityTypeConfiguration`. No se añadieron capas ni proyectos de aplicación duplicados. Program.cs sólo compone el host y permite los procesadores explícitos ya existentes.
- Integrations/Microsoft y Integrations/Nexo contienen los clientes externos; Integrations/Catalogs conserva el lector SII/ILDA. La publicación, cola y reservas siguen en Synchronization. EF sólo administra public de TDV2: no modela ni migra esquemas externos, roles locales ni tablas de plataforma Laravel sin uso.
- EF Core/Relational/Design y dotnet-ef **10.0.12**, proveedor Npgsql EF **10.0.0**, Npgsql directo **10.0.0**. Manifiesto local de herramientas en .config; paquetes/artefactos dentro del destino. Npgsql directo se conserva por su uso comprobado en bloqueos, tokens, operaciones centrales, reservas y auditoría.
- EF participa en consultas de áreas, formatos, colaboraciones, identidad de sesión e inventario local, además del guardado de formatos. Se mantienen empleado textual, tipos/longitudes/defaults/nulabilidad, JSON completo, secuencias serial/bigserial y relaciones locales. No se inventó FK desde el user_id integer histórico de tokens hacia users.id bigint ni hacia identidades/jerarquías externas.
- El guardado usa una transacción EF compartida con el bloqueo SQL de UR, revisión del contexto y auditoría. Version es token de concurrencia; se conserva el 409 y el cálculo de avance del servidor. Los SQL especializados de publicación y reservas mantienen propietario/caducidad antes del commit y latido sin renovación.
- Autorización mediante política ASP.NET, AuthorizationHandler y `IAuthorizationMiddlewareResultHandler`: revalida Nexo antes de HTML/JSON y conserva denegaciones, mensajes y auditoría. Se usa el mismo formateador de DomainProblem para middleware y política. Continúan el binding/ModelState MVC, filtros, DI, Options, cookie de sesión, antiforgery y rate limiting.
- Migraciones estándar `20261002051005_InitialTdv2` y `20261002051340_InitializePausedSynchronization`, con designers y snapshot. Historial único vigente: `public."__EFMigrationsHistory"`. La primera exige base vacía; la segunda sólo inserta configuración pausada. El modelo conserva todas las restricciones semánticas anteriores: dos unicidades nullable pasan de constraints UNIQUE a índices UNIQUE y se añade el índice de la FK de ejecución activa.
- Se retiró el ejecutor `--migrate`, sus recursos SQL compilados y el historial SQL como mecanismo vigente. SQL 001–004 queda exclusivamente en tests/TDV2.NativeVerification/LegacySchema; el DDL de conversión anterior, en TransitionSchema como fixture. El único archivo database/transition vigente es el diagnóstico de lectura.
- Local-Tdv2 conserva DPAPI administrativo, marcadores y Verify-Cluster, e invoca dotnet ef sólo para su destino aislado. Si encuentra la base local anterior sin historial EF, la conserva y exige revisión. Prepare-Transition mantiene restauración en una copia nueva y diagnóstico READ ONLY; `-Apply` SQL está retirado. La futura adopción de una copia real requiere revisión EF, no bootstrap automático.
- Se conservan tdv2.slnx, TDV2 HTTPS + React, HMR, UserSecretsId, configuración estándar, https://localhost:7136/connect y todas las pantallas/componentes/tema/tipografías existentes. No se editaron credenciales ni se leyó Herd en esta adaptación. README contiene comandos desde la raíz y equivalencias con Laravel.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore` | 0 errores y 0 advertencias |
| `dotnet ef migrations has-pending-model-changes --no-build --project tdv2` | Sin cambios de modelo pendientes; migrations list descubre las dos migraciones sin conectar |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [25/25 en PostgreSQL desechable](docs/migracion/evidencia-ef-migraciones.json): equivalencia SQL, tipos/defaults/secuencias, relaciones/índices, cuenta sin superusuario, repetición, contenido previo conservado, rollback/reanudación/Down/Up, migraciones pendientes concurrentes, script idempotente y concurrencia EF |
| `dotnet run --project tests/TDV2.Verification --no-build --no-restore` | 56/56 dominio/transporte; incluye dobles en memoria, no equivale a PostgreSQL |
| `Test-NativePostgres.ps1` final tras ajustar el resultado de autorización | [90/90 HTTP/PostgreSQL + un grupo navegador](docs/migracion/evidencia-ef-postgresql.json), 91 grupos correctos; fuentes institucionales sintéticas |
| React/Edge en la suite anterior | [9 recorridos formatos](docs/migracion/evidencia-ef-react-formatos.json) + [12 acceso](docs/migracion/evidencia-ef-react-acceso.json) + [10 sincronizaciones](docs/migracion/evidencia-ef-react-sincronizaciones.json), cero errores JS |
| TypeScript, `test:forms` y `build` de ClientApp | Correctos; 21/21 pruebas frontend, sin cambios de pantallas ni lockfile |
| F5 real desde EnvDTE en Visual Studio | [Compilación 2 correctos, implementación 1 correcta y depuradores .NET/JavaScript](docs/migracion/evidencia-ef-visual-studio.txt); solución/perfil conservados |
| `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, redirección 302, HTTPS confiable, React/props, privado 401 y HMR wss en 7136](docs/migracion/evidencia-ef-visual-studio-react.json); sin OAuth ni consultas institucionales |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Paquete generado con npm ci/build; auditoría npm informó 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production/HTTPS Windows, React compilado sin Vite, 503 sin credenciales, 401 privado, 419 sin CSRF, cero errores JS](docs/migracion/evidencia-ef-publicacion.json) |
| Verificador de configuración `--production` | Production no carga User Secrets; no se imprimieron valores ni se abrieron fuentes |
| Scripts PowerShell y `git diff --check` | Sintaxis y whitespace correctos. No se ejecutó Prepare contra la base local histórica |

Los intentos fallidos no cuentan como resultados: el sandbox impidió iniciar PostgreSQL/restaurar algunos paquetes y acceder a User Secrets, por lo que se ejecutaron las operaciones necesarias con aprobación. Se ajustó el verificador a la excepción PostgreSQL envuelta por EF. Dos bootstrap simultáneos sin historial provocaron una carrera del proveedor: **la primera aplicación tiene un solo operador**; las pendientes con historial se serializan con el bloqueo estándar EF/Npgsql. Un ensayo concurrente intentó recompilar una DLL en uso y se repitió secuencialmente. F5 detectó una pausa de Just My Code al propagar una denegación por el framework; se corrigió usando el resultado estándar de autorización y se repitieron F5, 56 pruebas y toda la regresión PostgreSQL/React. El primer publish encontró Rolldown bloqueado por el Vite remanente del IDE; se identificó y detuvo sólo ese proceso TDV2 antes de publicar correctamente.

Clúster final de migraciones: `.artifacts/native-postgres-20261002-053757-b02b1e9f`. Regresión final: `.artifacts/native-postgres-20261002-055330-864db610`. Todos los clústeres de esta ejecución quedaron detenidos. Se detuvo la depuración iniciada para verificar F5; el IDE y los documentos del usuario se conservaron.

### Aplicado en la base definitiva autorizada

- [Preflight inmediatamente anterior](docs/migracion/evidencia-ef-servidor-preflight.json): `SERVIDOR_TDV2:5432/tdv2_db`, `USUARIO_TDV2`, TLS confirmado, Search Path public, CONNECT/USAGE/CREATE y propietario verificados. Base vacía, sin objetos ajenos ni historial. La inicialización SQL anterior ya había sido eliminada por la recreación del usuario; no se adoptó contenido ni se eliminó ninguna base/conexión.
- Comando aplicado: `dotnet ef database update --no-build --project tdv2 -- --environment Development`, con ConnectionStrings:Tdv2 de User Secrets. [Resultado](docs/migracion/evidencia-ef-servidor-aplicacion.txt) y [verificación](docs/migracion/evidencia-ef-servidor-verificacion.json): ambas migraciones registradas, 14 tablas operativas más historial EF, 0 pendientes.
- Se repitió **el mismo comando**: [salida](docs/migracion/evidencia-ef-servidor-repeticion.txt) y [metadatos posteriores](docs/migracion/evidencia-ef-servidor-repeticion.json). Sin cambios de esquema, historial ni contadores. La huella de columnas/defaults/restricciones/índices/secuencias coincide también con [el ensayo sintético](docs/migracion/evidencia-ef-esquema.json).
- Todas las tablas operativas tienen 0 filas salvo sincronizacion_configuracion, que tiene la fila 1 pausada; automática/ILDA desactivadas, próxima ejecución y reservas nulas. Sin usuarios, sesiones, intentos OAuth, catálogos, formatos, auditoría ni trabajos sintéticos remotos. No se abrió Nexo/SII/ILDA ni se inició procesador.

### Pendiente institucional y operativo

Validar Microsoft/Graph/Entra reales, contratos y revocaciones Nexo, conectividad/tipos/volumen SII e ILDA; revisar una eventual copia autorizada con datos históricos y preparar su adopción EF; aceptación visual con fuentes externas, teclado/móvil/impresión; despliegue Ubuntu con proxy, supervisor, llaves persistentes y recuperación. Las pruebas Windows y los proveedores sintéticos no acreditan esos puntos. No se habilitaron sincronizaciones automáticas ni se desplegó la aplicación.

**Los apartados siguientes son evidencia histórica. Los comandos y mecanismos vigentes son los del bloque EF y del README; no reutilizar los ejecutores SQL/DPAPI retirados.**

## Inicialización SQL anterior del servidor — 2026-10-01 (histórica)

### Implementado

- Comando `--migrate=check|apply` en `tdv2/Migrations`, separado antes de componer el host web. Carga los proveedores normales y User Secrets en Development; sólo utiliza `ConnectionStrings:Tdv2`. Exige host/puerto/base/usuario explícitos y los contrasta con la conexión y con PostgreSQL; comprueba TLS remoto, search_path, escritura, CONNECT/USAGE/CREATE y propietario de tablas administradas. No inicia procesos de sincronización ni servicios institucionales.
- Reutiliza los bytes originales de 001–004 como recursos compilados; sólo SQL de la raíz, sin transition. SHA-256 íntegro, secuencia sin huecos, historial como prefijo verificado antes de escribir, exclusión entre sesiones y transacción por archivo incluyendo `public.tdv2_schema_migrations` (nombre/hash/fecha/usuario). Rechaza objetos preexistentes sin historial, cambios de hash y sincronización activa/reservada. El sitio no ejecuta DDL al arrancar.
- `Local-Tdv2.ps1` queda intacto, con Verify-Cluster, DPAPI, marcadores y tdv2_local_migrations. User Secrets no se editó ni se reemplazó: el usuario lo actualizó para la base recreada antes de esta tarea. Interfaz, F5 y Herd se conservaron. README contiene el comando exacto y el procedimiento para nuevas migraciones.

### Probado y ejecutado

| Verificación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | Compilación sin errores ni advertencias tras corregir la prueba nueva |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [19/19 en PostgreSQL desechable](docs/migracion/evidencia-migrador-postgresql-sintetico.json): cuenta sin superusuario, check sin DDL, primera aplicación, repetición/fechas, destino erróneo, base ocupada, hash cambiado, permisos, dos conexiones, rollback inicial/posterior/al registrar hash, reanudación, automática, COMMIT interno y errores sin secretos |
| Preflight del servidor, sólo metadatos | [Destino exacto SERVIDOR_TDV2:5432 / tdv2_db / USUARIO_TDV2](docs/migracion/evidencia-postgresql-servidor-preflight.json), PostgreSQL 18.6 Ubuntu, TLS confirmado, proveedor secrets.json, public vacío y permisos suficientes |
| Aplicación real autorizada | [001, 002, 003 y 004 aplicadas](docs/migracion/evidencia-postgresql-servidor-aplicacion.json); 14 tablas TDV2 más historial, claves primarias/índices válidos/restricciones validadas y sincronización pausada |
| Segunda ejecución del mismo apply | [0 aplicadas, 4 omitidas, 0 pendientes](docs/migracion/evidencia-postgresql-servidor-repeticion.json); misma huella del esquema |
| Comparación de metadatos | [Columnas, tipos, nulabilidad y defaults coinciden con el ensayo desechable](docs/migracion/evidencia-postgresql-servidor-verificacion.json); automática/ILDA desactivadas, próxima ejecución y reservas nulas |

La primera ejecución sintética fue impedida por el sandbox al iniciar PostgreSQL; se repitió fuera del aislamiento sobre un clúster nuevo y sólo dentro de `.artifacts`. Otra ejecución detectó que `inet::text` incluye máscara; se corrigió a `host(inet_server_addr())` y se obtuvo 19/19 antes de conectar al servidor. Los clústeres desechables quedaron detenidos. No se ejecutaron SQL de transition, cargas sintéticas remotas, login ni consultas a Nexo/SII/ILDA. Las únicas inserciones de bootstrap remoto son la configuración pausada prevista en 004 y los cuatro registros del historial.

### Pendiente

Inicialización e idempotencia verificadas en el servidor autorizado. No se acredita con ello el login institucional, conectividad Nexo/SII/ILDA, conversión de datos anteriores, publicación Ubuntu del sitio ni cierre de la migración funcional. Las evidencias anteriores sobre bases locales conservan su alcance histórico; `ConnectionStrings:Tdv2` vigente apunta ahora al servidor por decisión del usuario.

## Corrección del arranque de Visual Studio — 2026-10-01

### Implementado

- Se obtuvo de la instancia abierta el [error completo de implementación](docs/migracion/evidencia-vs-implementacion-antes.txt): configuración vacía de `launch.json`, `Value cannot be null. Parameter name: source`. ClientApp no tenía `.vscode/launch.json`; JSPS fallaba antes de ejecutar npm. El rechazo TCP de ReactShell era consecuencia de no haber Vite escuchando.
- Nuevo `ClientApp/.vscode/launch.json` para Edge; perfil compartido con `DebugTarget` explícito de cada proyecto y directorio de trabajo explícito en `.esproj`. Se conserva `npm run dev` y su dirección estricta `127.0.0.1:5173`. `https` no abre un segundo navegador; `https-compiled` sí conserva su apertura independiente.
- JSPS comprueba el puerto del destino antes de iniciar ASP.NET. El destino de implementación usa Vite y su ruta de desarrollo `/__vite/__launch`, que espera la salud local de Kestrel y redirige al HTTPS fijo. Espera acotada, respuesta genérica si no arranca; no se publica esa ruta. Edge conserva el depurador JavaScript y el mapeo de fuentes a través de `/__vite/`.
- [Sobrescrituras verificadas](docs/migracion/evidencia-vs-configuracion.json): `appsettings.Development.json` conserva `UseVite=false`, sobrescrito por `ReactDevelopment__UseVite=true` del perfil `https`. Sin otra clave React en User Secrets ni variables ambientales del proceso de comprobación/usuario/equipo. UserSecretsId, las nueve claves, el retorno `https://localhost:7136/connect`, interfaz y datos permanecen; Herd no se leyó ni modificó.
- La modificación previa de `tdv2.slnx` (Build/Deploy de ClientApp y organización) ya estaba presente al iniciar este trabajo y se conservó.

### Probado

| Comprobación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | 0 errores, 0 advertencias |
| F5 real mediante EnvDTE `Debug.Start` en VS Community 2026 18.10.3 | [Implementación 1 correcta, 0 con errores](docs/migracion/evidencia-vs-implementacion-despues.txt); depuradores .NET y JavaScript, portada Transformación Digital; arranque en frío con 5173/7136/5064 inicialmente libres |
| `node tests/browser/visual-studio-startup.mjs` desde ClientApp, contra los procesos del IDE | [Vite 200, arranque 302, HTTPS confiable, portada y React renderizados, props 200, HTML privado 401, WebSocket HMR en 7136 y cero errores JS](docs/migracion/evidencia-vs-react.json); fuentes externas bloqueadas |
| `dotnet run --project tests/TDV2.LocalConfigurationVerification --no-build --no-restore -- <raíz>` | 9 claves desde User Secrets; cadenas analizadas sin abrir conexiones, callback/base/ILDA conservados |
| `node node_modules/typescript/bin/tsc --noEmit` y `npm.cmd run build` desde ClientApp | Correctos; Vite 8.3.2 y dependencia/lockfile conservados |

Se usó el Node ya incluido en Visual Studio para los verificadores; el IDE ejecutó su propio `npm run dev`. No se ejecutó StartDatabase, SQL, migración, sincronización ni inicio de sesión. Los intentos intermedios no se cuentan como éxito: el IDE requirió recargar el nuevo perfil; un destino que apuntaba directamente a 7136 falló durante Implementar y un destino Node no inició Vite. La configuración final usa Edge con comprobación de 5173 y espera de Kestrel.

### Pendiente

Este fallo de F5 queda verificado en el IDE. No se validaron breakpoints en todas las pantallas, autenticación institucional ni aceptación visual completa con fuentes externas. Continúan los pendientes funcionales de la migración descritos debajo; no se declara la migración terminada.

## Preparación para mantenimiento y Visual Studio — 2026-10-01 (evidencia anterior)

### Implementado

- Program.cs queda centrado en el host y la selección explícita del procesador. Controllers atiende HTTP; Services coordina formatos, colaboradores, Microsoft y contextos; Infrastructure conserva Npgsql y SQL local. Se extrajeron consultas de colaboradores/sincronizaciones y middleware de errores/autorización. No se añadieron proyectos .NET, ORM ni autenticación de demostración. Se conservan rutas, JSON, 419/409 y límites por operación.
- Comentarios y contratos XML en español explican UR/empleado textual, administrador combinado, intersección local/Nexo, identidad real/efectiva, tokens sólo de servidor, avance, revisión de contexto, transacciones y reservas. El latido sigue sin renovar reservas; no se movieron commits fuera de sus comprobaciones ni de la auditoría.
- ClientApp.esproj reutiliza React mediante el sistema JavaScript de Visual Studio. tdv2.slnLaunch inicia ClientApp y tdv2; launchSettings conserva 7136/5064 y ofrece https con Vite y https-compiled. El proxy de desarrollo /__vite/ usa SpaServices.Extensions; OAuth, cookies, JSON, CSRF y HMR conservan el origen 7136. No rediseño.
- Configuración general y límites en appsettings, opciones de desarrollo en appsettings.Development, credenciales en User Secrets y puertos en launchSettings. UserSecretsId y las nueve claves importadas se conservan. Tras verificar el reemplazo, se retiró el código que leía institutional.clixml: Import/Configure/MigrateSecrets sólo orientan al editor. DPAPI permanece para administrar el clúster aislado y proteger llaves ASP.NET de Windows. User Secrets no es cifrado ni fuente de producción.
- Publicación del backend compila React mediante npm ci/build y empaqueta wwwroot, sin Vite de producción. DataProtection:KeyDirectory permite llaves persistentes externas. El paquete dependiente de ASP.NET Runtime se genera con UseAppHost=false. Preparación Ubuntu documentada, sin instalar ni desplegar.
- Ensayo separado database/transition + Prepare-Transition.ps1: sólo admite una copia local identificada, valida esquema/claves/relaciones y datos básicos, conserva catálogos/respuestas con huellas transaccionales, aísla tokens Laravel y añade 002/003. Rechaza reservas y reaplicación; no usa 001/004 sobre Laravel. Pruebas sintéticas en el proyecto nativo existente. **Falta acceso/copia real autorizada de tdv2_db; no se respaldó ni consultó esa base.**
- README y guía local reorganizados. Historial DPAPI/User Secrets anterior trasladado a HISTORIAL_LOCAL.md, conservando su evidencia. Guías específicas de transición y publicación. Se registró el estado inicial en Git (`0e12049`) antes de incorporar los cambios revisables; no se publicó a un remoto.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| Compilación de la solución | 0 errores/advertencias; ejecuciones indicadas con NuGetAudit=false |
| TDV2.Verification | 56/56, dominio/HTTP con dobles en memoria |
| Test-NativePostgres.ps1 | 91/91 grupos: 90 HTTP/SQL y un grupo con 31 recorridos Edge; PostgreSQL aislado real, proveedores institucionales sintéticos |
| Test-UserSecrets.ps1 | Nueve claves efectivas; ediciones conservadas ante comandos retirados y archivo original restaurado byte por byte; sin conexiones remotas |
| Verificador con --production | Production no carga User Secrets; credenciales externas requeridas |
| Ensayo de transición final | 16/16; respaldo/restauración sintéticos, contrato, rollback y validador real de formatos |
| Regresión con dependencias corregidas | 31 recorridos Edge repetidos; sin repetir ni sumar los 90 casos SQL |
| TypeScript / pruebas frontend | TypeScript sin errores; 21/21 casos conservados |
| Test-Development.ps1 | Cuatro comprobaciones HTTPS/React/401/CSRF/Microsoft 302, más HMR sin navegación, cookies seguras, rechazo de HTML privado y error 503 controlado con Vite detenido |
| dotnet publish + publish-flow.mjs | Paquete Production inicia con HTTPS y React compilado sin Vite; /connect 503 sin secretos, /inicio 401 y CSRF 419 |
| Conservación por hashes | 30 archivos de interfaz/estilos/portada/logos/SQL 001–004 sin cambios; 154/154 archivos de referencia Laravel sin cambios |

Evidencia: [regresión PostgreSQL](docs/migracion/evidencia-organizacion-postgresql.json), [regresión del navegador con el lockfile final](docs/migracion/evidencia-organizacion-regresion-navegador.json), [arranque y redirección local](docs/migracion/evidencia-organizacion-arranque.json), [configuración](docs/migracion/evidencia-user-secrets.json), [Vite y HMR](docs/migracion/evidencia-vite.json), [publicación](docs/migracion/evidencia-publicacion.json), [conservación](docs/migracion/evidencia-conservacion-organizacion.json) y [transición sintética](docs/migracion/evidencia-transicion-sintetica.json). La interfaz/depurador de Visual Studio **no se operó**; los perfiles se ejecutaron por CLI. El 302 Microsoft no siguió la redirección ni acreditó login real. Los navegadores bloquearon fuentes web externas; no son aceptación visual exacta.

La primera preparación del fixture de transición falló por agrupar SQL parametrizado con múltiples sentencias; se separó el INSERT parametrizado y se repitió. La primera carga fría de Vite agotó 30 segundos durante la optimización; se ajustó la espera y se verificó después de restaurar dependencias. No se cuentan los intentos fallidos como éxitos. Los clústeres de prueba quedaron detenidos; el clúster local de validación permanece disponible para F5, sin automática ni trabajadores. Se conservó su usuario ya existente: los contadores observados fueron 1 usuario, 0 formatos, 0 UR y 0 ejecuciones; no se consultaron identidades ni se cargaron fixtures allí.

### Retirado y motivo

- Cinco archivos Web/*Endpoints.cs: reemplazados por controladores, servicios y consultas; regresión HTTP/SQL/navegador pasada.
- scripts/Import-LaravelSettings.ps1, scripts/Test-LocalImport.ps1 y lector institucional DPAPI de Local-Tdv2.ps1: reemplazo en User Secrets comprobado. Se conservan evidencia histórica y credenciales cifradas originales sin lecturas del arranque.
- tdv2/tdv2.http: plantilla de WeatherForecast sin ruta correspondiente.
- Microsoft.AspNetCore.OpenApi: paquete sin uso, reemplazado por la dependencia concreta del proxy Vite.
- concurrently: ninguna referencia de ejecución; el inicio múltiple lo gestiona Visual Studio. La auditoría inicial npm encontró 8 vulnerabilidades; se aplicaron correcciones compatibles del lockfile. Vite incluía un [aviso de rutas Windows](https://github.com/advisories/GHSA-fx2h-pf6j-xcff). La [auditoría posterior](docs/migracion/evidencia-npm-audit.json) informó cero. Vite pasó de 8.0.8 a 8.3.2 y PostCSS de 8.5.10 a 8.5.28; React 19.3.0, MUI 9.4.0 y TypeScript 5.9.3 permanecieron iguales ([versiones](docs/migracion/evidencia-dependencias.json)). La publicación, HMR y los 31 recorridos de navegador se repitieron con el lockfile corregido.

### Pendiente concreto

Operar el perfil compartido/F5 en la interfaz de Visual Studio; validar Entra/Microsoft/Graph y permisos institucionales Nexo; obtener y contrastar una copia autorizada de tdv2_db (tipos/longitudes/secuencias/datos históricos/UTC); conectores SII/ILDA reales; aceptación visual con fuentes, teclado/móvil/impresión; ejecución Ubuntu, proxy, supervisor, llaves persistentes y recuperación operativa. **No se desplegó ni se declara terminada la migración institucional.**

Comandos actuales y ubicaciones: [README](README.md), [arranque Visual Studio](docs/ARRANQUE_LOCAL_WINDOWS.md), [transición](docs/migracion/TRANSICION_DATOS.md), [publicación](docs/PUBLICACION.md). No usar el historial como instrucciones para reimportar configuración.

## Inspección y límites

Preparaciones anteriores: [historial local](docs/migracion/HISTORIAL_LOCAL.md). Se conservan resultados, limitaciones y referencias; sus lanzadores/importadores anteriores no son instrucciones vigentes.

- Inventario: 27 rutas propias, 27 tablas declaradas, 119 métodos PHP y 12 pruebas frontend originales. **154/154 hashes de Herd siguen iguales** al manifiesto inicial. Las pruebas PHP son criterios de aceptación; no se ejecutaron.
- Para este bloque se revisaron antes de implementar CollaboratorController, RepresentationController, PreviewController, ConfigurationController, NexoDelegation, NexoRepresentation, RepresentationAudit, PreviewSession/ViewContext, FormAccess, middleware, rutas/configuración y pruebas asociadas.
- Para sincronizaciones se revisaron InstitutionalSource/InstitutionalSync, IldaSource/IldaSync/IldaInventory, SyncManager/SyncController, comandos, configuración y migraciones 2026_09_25_000001/2026_09_30_000001, además de InstitutionalSourceTest, InstitutionalSyncTest, IldaInventoryTest y ConfigurationSyncTest. Contratos y diferencias: [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md).
- Contratos, diferencias y recuperación: [DELEGACION_REPRESENTACION.md](docs/migracion/DELEGACION_REPRESENTACION.md). Configuración de identidad/SQL: [AUTENTICACION_POSTGRESQL.md](docs/migracion/AUTENTICACION_POSTGRESQL.md). Inventario original: [INVENTARIO.md](docs/migracion/INVENTARIO.md).

## Implementado

### Base, autenticación y formatos conservados

- React, TypeScript, nueve pantallas, MUI, tema, CSS, declaración de tipografías, portada y navegación originales. Comunicación JSON del mismo origen sustituye Inertia. El historial sólo guarda URLs, nunca props ni identidades.
- Microsoft conserva /connect inicio/callback, tenant GUID, state de un uso con 600 segundos de vigencia, vínculo al navegador y PKCE S256. Canje e identidad Graph /me desde servidor, dominio institucional y object ID GUID; Nexo se valida antes de persistir. Logout revoca la sesión y redirige al destino Microsoft fijo.
- Sesión cifrada PostgreSQL de dos horas, cookie Secure/HttpOnly/SameSite=Lax y CSRF. Graph cifra/refresca tokens con bloqueo; foto fallida no concede permisos ni termina sesión.
- Nexo sigue siendo autoridad. Aplicación única resuelta por **clave literal tdv2**, ID positivo publicado y nivel 2; identidad individual, empleado textual, roles, módulos y parentesco/ruta vigentes. No se aceptan identidad, roles o UR de React como autorización.
- Formatos sólo para UR activas nivel 2/3. Áreas subordinadas a nivel 3 comparten su formato cuando están autorizadas; no se crean formatos por áreas inferiores. Administrador conserva consulta institucional y edición limitada a su rama nivel 2, incluso combinando roles.
- Respuestas canónicas, JSON, versión, avance calculado en servidor, autor y timestamps. GET no crea filas; bloqueo de UR serializa primera creación y actualización. Versión obsoleta devuelve 409; error SQL revierte el cambio.
- Autorización en HTML y endpoints, revalidada por solicitud. Nexo caído no concede permisos de respaldo.

### Administración delegada

- Búsqueda, alta y retiro mediante vistas y funciones publicadas por Nexo. Nexo permanece Laravel; no se escriben sus tablas internas.
- Intersección de responsabilidad/adscripción válida con nexo_delegacion; niveles_delegacion=[2], como la configuración Laravel. Ser administrador no basta para delegar.
- Roles asignables sólo desde nexo_delegacion_roles; rol_id enviado por React se ignora. Nexo comprueba persona/origen/alcance y, tras el alta central, TDV2 vuelve a leer identidad publicada antes de crear el vínculo.
- Colaborador local: formato 2/3 correspondiente a su adscripción. Colaborador de áreas dependientes/global: formatos de la rama nivel 2 otorgante. Siempre se requiere vínculo local y concesión central vigente coincidente.
- Altas concurrentes serializadas por persona/origen/rol. Retiro local y auditoría se confirman antes de llamar Nexo; fallo central devuelve 202, conserva acceso revocado y muestra retiro pendiente con reintento. Se conserva la concesión si otro vínculo local activo la utiliza.
- Búsquedas distinguen vacío, falta de autorización y fallo de conexión, sin convertir errores en listas vacías.

### Actuar como usuario y vista por rol/área

- Ambas herramientas permanecen en Configuración → Pruebas de acceso. Configuración exige administrador y módulos vigentes; representación además exige capacidad independiente de Nexo.
- Buscar/iniciar/validar/finalizar usan nexo_a{ID}_representacion(jsonb), actor real en servidor y token cifrado. Delegación durante representación usa esa misma función con actor real + token; nunca suplanta directamente al actor ante las funciones de delegación.
- Estado efectivo verificado en servidor por solicitud: actor/persona/ID, caducidad local y central, autorización vigente, módulos, duración y escritura. No se amplían permisos si Nexo falla, revoca o devuelve datos ambiguos.
- Identidad Microsoft real separada del perfil representado. Franja original muestra persona, actor, vigencia, consulta/escritura y salida. Caducidad/revocación bloquean el acceso; volver a permisos propios requiere salida explícita. La pantalla de acceso restringido también conserva un botón para terminar el contexto.
- Vista de prueba sólo escenario por rol/área, 30 minutos y consulta. No acepta modo usuario/email; bloquea escrituras y búsqueda delegada. Configuración, contextos anidados y mutaciones desde pestañas con contexto obsoleto se rechazan en servidor.
- Contexto cifrado/revisión por sesión en tdv2_access_contexts. Bloqueos y comparación de revisión evitan sustituciones concurrentes durante escrituras. Dos inicios simultáneos admiten uno y rechazan el otro.
- DELETE de salida y logout siguen disponibles ante falla de Nexo, con CSRF. Se descarta el contexto local; se registra si el cierre central pudo confirmarse.

### Auditoría y persistencia local

- Cambios propios y representados registran actor real, identidad representada cuando aplica, acción, recurso, contexto y resultado. Se registran también búsquedas y rechazos del bloque, incluido CSRF. No se serializan tokens, cabeceras, credenciales, errores SQL ni el motivo libre.
- Guardado de formato, alta/revocación/retiro local, inicio/salida de contexto y logout comparten transacción con su auditoría. Si la bitácora falla, el cambio local se revierte.
- No hay transacción distribuida entre TDV2 y Nexo. Un alta central puede permanecer sin vínculo local tras un error; no habilita edición y se recupera por reintento. Un inicio central sin commit local intenta finalizarse. Retiros y cierres centrales fallidos conservan estados explícitos; ver límites en el documento de contratos.
- database/001_core.sql es bootstrap sólo de base vacía; 002_aspnet_sessions.sql añade sesiones/OAuth y **003_access_contexts.sql** el estado cifrado. No convertir ni aplicar sobre bases existentes. La aplicación nunca ejecuta DDL al arrancar.
- Llaves Data Protection bajo tdv2/.runtime/keys, DPAPI Windows; las pruebas usan llaves efímeras. Nuevo login necesario para cookies Laravel y tickets ASP.NET anteriores a este bloque.

### Configuración → Sincronizaciones

- Conectores de producción SQL Server/MySQL con consultas fijas: SII sólo `poa.UNIDADES_RESPONSABLES_POA`, diez columnas y MAX(EJERCICIO) por defecto; ILDA toda `ilda_db.informacion_area`, todas las filas/columnas en JSON original y auxiliares locales. No hay escrituras remotas ni consultas remotas al abrir formatos.
- Descarga completa antes de publicar, validación de columnas/IDs/ejercicio/ciclos, límites 100000 filas y 128 MiB ILDA, reducción superior al 20% rechazada. Bajas lógicas mantienen relaciones/respuestas/versiones. Empleado SII conserva ceros y se compara con Nexo textual.
- Inventario exclusivamente PostgreSQL con igualdad exacta ur2 ↔ clave; formulario hasta 200 filas, fuente ILDA y Trámite / servicio desde informacion_generada. Incorporar nuevos IDs al GET no persiste ni sustituye respuestas guardadas, aun si el origen cambia o desaparece.
- Cola persistente y manual sii/ilda/ambas (HTTP 202); automática siempre incluye SII, ILDA optativa, intervalos/horario originales y zona America/Ciudad_Juarez mediante base IANA NodaTime. Configuración optimista y autorizada sólo al administrador real con módulos Nexo; rechaza CSRF ausente, vista de prueba y representación en páginas/endpoints.
- Reserva PostgreSQL con propietario UUID, 30 minutos, exclusión entre procesos, comprobación antes y después de cada transacción y recuperación de interrupciones sin publicar desde trabajadores vencidos. Cada catálogo/resultados/auditoría son atómicos; éxito parcial se muestra como parcial. Un cambio concurrente de catálogo antes de guardar formato exige revalidación (409).
- `--sync-worker` opera por separado del sitio, publica latido cada minuto sin renovar por ello la reserva; `--sync-once` hace un ciclo y `--sync-check=sii|ilda|ambas` sólo comprueba. Automática e ILDA deshabilitadas por defecto. No se instalaron tareas, cron ni servicios.
- Pantalla Sincronizaciones idéntica a Laravel salvo import de transporte; historial, progreso, estados, alertas y tipografías conservados. Dependencias, diferencias de tipos MySQL, zona horaria, esquema y operación Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). DDL explícito `database/004_synchronizations.sql` únicamente para la base ASP.NET aislada preparada.

## Evidencia anterior de los bloques funcionales

| Ejecución | Resultado | Alcance |
|---|---|---|
| dotnet build tdv2.slnx -p:NuGetAudit=false | 0 errores, 0 advertencias | Compilación; auditoría NuGet desactivada en esa ejecución |
| dotnet run --project tests/TDV2.Verification --no-restore | **56/56** | Dominio/HTTP con identidad, Nexo y almacenamiento en memoria sintéticos |
| npm.cmd run types:check | Código 0 | TypeScript |
| npm.cmd run test:forms | **21/21** | 12 originales + 9 de transporte; Axios/navegador simulados |
| npm.cmd run build | Código 0 | Bundle Vite |
| scripts/Test-NativePostgres.ps1 | **91/91 grupos** | 90 casos HTTP/SQL (30 del bloque) + un grupo de 31 recorridos en Edge |
| scripts/Test-NativePostgres.ps1 -SyncOnly (ajuste final SII) | **30/30** | Tras conservar booleanos textuales SII como 1/0; fuentes ADO.NET sintéticas |
| dotnet publish tdv2/tdv2.csproj -c Release --no-restore -o .artifacts/publish | Código 0 | Paquete construido; CLI inválida devuelve 2 sin conectar a fuentes |

PostgreSQL **18.6 nativo para Windows**, Edge **154.0.4258.37** sin interfaz. SQL, transacciones, bloqueos, cookies, CSRF y pantallas reales; Microsoft simulado por HTTP y Nexo simulado mediante vistas/funciones publicadas en PostgreSQL. La cuenta Nexo de pruebas sólo lee vistas y ejecuta funciones autorizadas, sin acceso directo a tablas internas. No se probó el publicador Laravel institucional. SII/ILDA usan lectores ADO.NET sintéticos con el SQL/mapeo/publicación de producción: no hubo conexión TDS/MySQL ni validación TLS o tipos nativos institucionales.

Se validaron loopback, puerto no convencional, base, usuario y SHOW data_directory antes de escribir. La suite creó un clúster nuevo dentro del destino y **lo dejó detenido**. No inició/detuvo servicios PostgreSQL existentes ni tocó sus bases.

Evidencia actual sin credenciales:

- [90 casos PostgreSQL y grupo de navegador](docs/migracion/evidencia-sincronizaciones-postgresql.json).
- [10 recorridos del módulo Sincronizaciones](docs/migracion/evidencia-sincronizaciones-navegador.json).
- [Regresión de 12 recorridos de acceso](docs/migracion/evidencia-sincronizaciones-regresion-acceso.json) y [9 recorridos de autenticación/formatos](docs/migracion/evidencia-sincronizaciones-regresion-formatos.json).
- [30 casos tras el ajuste final de conversión SII](docs/migracion/evidencia-sincronizaciones-ajuste-conector.json).

Suite completa: `.artifacts/native-postgres-20261001-065537-4cce3a7f/`. Ajuste posterior de conector: `.artifacts/native-postgres-20261001-070037-0c2a3c8e/`. Capturas: sincronizaciones-completada.png, sincronizaciones-historial.png y formato-ilda-local.png. La comprobación posterior -SyncOnly verifica el ajuste puntual de booleanos SII (1/0, igual que Laravel); no repite ni se suma a los 31 flujos del navegador.

Evidencia histórica de delegación:

- [60 casos PostgreSQL y grupo navegador](docs/migracion/evidencia-delegacion-postgresql.json).
- [12 recorridos de colaboradores/representación/vista/auditoría](docs/migracion/evidencia-delegacion-navegador.json).
- [9 recorridos de regresión de autenticación/formatos](docs/migracion/evidencia-regresion-navegador.json).

Artefactos históricos de delegación: `.artifacts/native-postgres-20261001-060357-784fc391/`, incluidos JSON, logs sintéticos y capturas representacion-activa.png, vista-rol-area.png, formato-postgresql.png y ur-denegada.png. Los informes anteriores [PostgreSQL](docs/migracion/evidencia-postgresql.json) y [navegador](docs/migracion/evidencia-navegador.json) se conservan como evidencia histórica del bloque anterior.

Casos verificados de sincronizaciones:

- SELECT fijo SII, ejercicio único, diez columnas, empleado textual y espacios; vacío, duplicados, ciclos, descarga interrumpida/incompleta, tamaño y caída inesperada conservan el catálogo anterior.
- ILDA completa (más de 200 registros), columnas adicionales, nulos/vacíos/binarios reversibles, coincidencia exacta de claves. GET sólo local incluso con fuente caída/deshabilitada; agrega IDs sin persistir y conserva respuestas/versiones tras cambios y desapariciones.
- Exclusión entre solicitudes y trabajadores, 202 sin descarga HTTP, recuperación de proceso hijo terminado, reserva vencida antes/durante publicación y trabajador viejo incapaz de liberar/publicar sobre reserva nueva. Latido no prolonga reserva.
- Publicación por fuente transaccional; ambas direcciones de fallo parcial, errores SQL/auditoría revierten catálogo/metadatos/resultado, solicitud y configuración. Comprobación CLI sin cola/publicación; reintentos explícitos convergen.
- Programación diaria y cambio estacional Juárez, intervalo y zona inválidos, dos versiones concurrentes, automática siempre SII/ILDA opcional, intervalos perdidos agrupados y activación ILDA validada.
- Administrador real, módulos padre/hijo, Nexo, CSRF y contexto exigidos en páginas y endpoints. Vista de prueba y representación rechazan configuración; cambio SII concurrente exige revalidar antes de guardar.
- React muestra pendiente, ejecutando, completada, parcial y fallida; conserva borrador de programación ante 409, historial y opciones automáticas, recupera tras error SQL/interrupción y guarda información ILDA local sin tocar MySQL. **Cero errores JavaScript de página** en los tres scripts.

Casos conservados de delegación y representación:
- Búsqueda permitida/vacía/denegada/fallida; administrador sin delegación, empleado no coincidente, rol no asignable, nivel 3 no delegable (criterio histórico sustituido el 2026-10-05 por colaboración local en su rama), persona/origen/rama forjados, administrador combinado sin ampliación.
- Persona de nivel 4 autorizada en formato nivel 3; colaborador local/global; GET/PUT de UR ajenas o inferiores denegados. Identidad no publicada tras alta impide vínculo; reintento y altas simultáneas convergen.
- Revocación local persiste si Nexo falla; reintento confirma retiro. Falla inducida en auditoría revierte alta y revocación locales.
- Administrador sin capacidad no representa; capacidad sin escritura no la concede. Búsqueda fuera de alcance, token cifrado/no publicado, actor real intacto, módulos revocados, escritura/consulta y alcance.
- Vencimiento local y central, revocación, Nexo no disponible, pestaña obsoleta, contextos anidados y dos inicios simultáneos. Regreso explícito al actor, incluido Nexo caído.
- Formato y auditoría atómicos durante representación; fallo de auditoría al iniciar intenta compensación central. Logout representado revoca sesión/contexto y audita atómicamente; CSRF rechazado identifica ambas personas sin cambios.
- Vista por rol/área sólo consulta; modo usuario, área inválida, búsqueda delegada, configuración y escrituras directas rechazados. Caducidad no restaura permisos reales automáticamente.
- Navegador: altas/retiros y reintento, mensajes diferenciados, ramas local/global, herramientas separadas, capacidad ausente, representación de consulta/escritura, autoguardado, revocación/vencimiento, salida con Nexo caído, vista de prueba y elusión de endpoints desde el navegador. PostgreSQL confirma actor/representado y ausencia de tokens en auditoría. **Cero errores JavaScript de página** en ambos scripts.

La regresión conserva OAuth/state/PKCE, identidad Microsoft, aplicación/roles/módulos Nexo, logout/CSRF, UR, guardados concurrentes/versionado, rollback/reintento y Graph cifrado/refresco. Son 31 recorridos en tres scripts (9 + 12 + 10), contenidos en un solo grupo; no se cuentan otra vez como grupos HTTP.

Tema, CSS, layout y lógica de directorio conservados (layout con sustitución de import de transporte). Ajustes puntuales en páginas y texto “áreas dependientes” documentados; no hay rediseño. Capturas inspeccionadas como evidencia funcional, **no comparación visual exacta con Herd**. Todas las solicitudes externas del navegador se bloquean, incluidas fuentes web; no acredita descarga de tipografías, MFA ni SSO real.

Ejecuciones fallidas previas se conservaron en .artifacts y no se cuentan como éxitos. Se corrigió una ambigüedad de variable en el publicador SQL sintético y un error productivo de lectura de confirmaciones delegadas de una columna; el retiro/reintento fue repetido exitosamente. En este bloque fallaron inicialmente cuatro casos por ausencia de la zona IANA en Windows (resuelto con NodaTime) y una espera prematura de carga en la prueba de dos pestañas (corregida). Una ejecución diagnóstica simultánea no pudo compilar porque Windows mantenía la DLL abierta; se repitió secuencialmente. Los fallos no cuentan como evidencia de éxito. No se ampliaron permisos para hacer pasar las pruebas.

## Comandos reproducibles

Desde ClientApp:

```powershell
npm.cmd ci --ignore-scripts
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
```

Desde la raíz del destino:

```powershell
dotnet build tdv2.slnx -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

La última orden requiere PostgreSQL nativo, Edge y certificado ASP.NET HTTPS ya existente. Usa C:\Program Files\PostgreSQL\18\bin; admite -PostgresBin. En esta sesión fue necesaria aprobación para ejecutarlo fuera del sandbox. Crea siempre un clúster nuevo, con contraseña sintética aleatoria no impresa, vacía el archivo de contraseña inicial y detiene su clúster en finally. Conserva datos/logs sintéticos. **-SkipBrowser ejecuta 90 casos HTTP/SQL; -BrowserOnly ejecuta 31 recorridos; -SyncOnly ejecuta los 30 casos del bloque sin navegador**. Ninguna ejecución parcial equivale a la suite completa. El host HTTPS lee el certificado existente; no instala certificados.

No ejecutar dos suites mientras una recompila la misma salida en Windows: el ejecutable mantiene DLL abiertas. Operación manual/continua, cancelación, reserva de 30 minutos, recuperación y comandos Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). No se instalaron tareas ni servicios.

Los proyectos de verificación son ejecutables con salida no cero al fallar, no suites descubiertas por dotnet test. Construir ClientApp antes del navegador. Los controles de fallos/datos sólo existen en el ejecutable de pruebas; no hay autenticación de demostración ni endpoints de fixture en producción.

Revalidar el manifiesto de Herd, sólo lectura:

```powershell
$reference = 'C:\Users\Jesus Arenas\Herd\tdv2'
$manifest = Get-Content -Raw -Encoding UTF8 docs/migracion/fuente-manifiesto.json | ConvertFrom-Json
$changed = @($manifest | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $reference $_.path)).Hash.ToLowerInvariant() -ne $_.sha256
})
if ($changed.Count) { throw 'La referencia cambió; revisar el inventario.' }
$manifest.Count
```

## Pendiente de validación real y configuración faltante

- **Microsoft institucional:** TenantId, ClientId, ClientSecret y PublicOrigin local ya trasladados a User Secrets desde DPAPI. Pendientes callback registrado, cuentas autorizadas, vigencia del secreto, consentimiento/User.Read/offline_access, MFA/acceso condicional, Graph/rotación reales y logout SSO. El 302 local no valida esos aspectos. No publicar secretos en archivos o conversación.
- **Nexo Laravel institucional:** conexión importada, sin probar. Pendientes red/TLS, publicación tdv2, vistas filtradas y EXECUTE de las funciones públicas de delegación/representación (contrato Nexo 9.6.0 citado por Laravel). Sin validar firmas/tipos efectivos, publicador Laravel, idempotencia real, duración/formato de selección, responsables/roles asignables, revocaciones desde Nexo, concesiones compartidas ni auditoría central. Pruebas que alteren concesiones centrales requieren una publicación/personas autorizadas para pruebas.
- **SII/ILDA institucionales:** conexiones importadas, sin probar ni ejecutar. Pendientes acceso autorizado SELECT, certificados TLS cuando correspondan y muestra de tipos/volumen. Sin validar protocolo TDS/MySQL, autenticación real, configuración de servidor, ejercicio desplegado, tipos/precisión/texto de fechas ILDA, claves/empleados reales ni rendimiento institucional. No basta el lector ADO.NET sintético para acreditar el conector de red.
- **Procesador en despliegue:** comandos Windows/Ubuntu documentados; no se ejecutó Ubuntu, SIGTERM, supervisor, reinicio de máquina ni operación prolongada. No se habilitó ningún servicio/tarea. El fallo de fuente se probó por excepciones/lecturas incompletas, no por cortar una conexión remota real.
- **Datos existentes:** intactos y sin consultar. Ensayo sintético de conversión disponible; falta ejecutarlo sobre una copia real autorizada y contrastar esquema/tipos/índices, colaboraciones, actividad y UTC, además del corte/reversión institucional. No reutilizar cookies/cifrado Laravel ni aplicar el bootstrap sobre una base existente.
- **Operación y recuperación:** reinicio con llaves persistidas bajo cuenta del servicio, varias instancias, rotación/respaldo, limpieza de sesiones/contextos caducados, proxy/HTTPS, límites distribuidos y caída física de motor/red. Los triggers/fallos SQL no representan pérdida de confirmación de COMMIT. No hay reconciliador automático entre TDV2/Nexo.
- **Paridad documentada:** el identificador Microsoft de sesión no se regenera como en Laravel; se usa contexto cifrado con revisión atómica y rechazo de contexto obsoleto. Auditoría extendida a operaciones propias; motivo libre omitido. Login conserva bitácora básica separada de la persistencia de identidad; falta acordar metadatos históricos, retención y consulta institucional de auditoría.
- **Interfaz:** comparación visual con fuentes descargadas, móvil/teclado, impresión/exportación y borradores al caducar sesión. Capturas funcionales y código conservado no certifican estos aspectos.

## Bloques restantes

| Bloque | Estado |
|---|---|
| B0 — inventario y trazabilidad | Listo a nivel de código; esquema desplegado no consultado |
| B1 — dominio/módulos/esquema/avance | Implementado; falta paridad exhaustiva institucional |
| B2 — transporte/CSRF/formatos | Flujo principal y regresión verificados; visual y despliegue pendientes |
| B3 — Microsoft/identidad/Nexo/Graph | Implementado con proveedores simulados; **validación institucional pendiente** |
| B4 — colaboradores/delegación | Búsqueda/alta/retiro/recuperación implementados y probados con funciones sintéticas; **Nexo real pendiente** |
| B5 — Configuración/pruebas/representación/auditoría | Vista y representación separadas, permisos y auditoría local implementados y probados; **aceptación institucional pendiente** |
| B6 — ILDA/UR SII | Implementado y probado con copia PostgreSQL real/fuentes sintéticas; **conectividad, tipos y aceptación institucional pendientes** |
| B7 — programador/recuperación | Cola, reserva, caducidad, exclusión, parcial/rollback y recuperación implementados/verificados; **operación institucional/Ubuntu pendiente**, sin tareas instaladas |
| B8 — integración/corte | PostgreSQL aislado/navegador disponibles; servicios reales, visual, despliegue y corte pendientes |

Las rutas de sincronización ya tienen implementación, permisos y pruebas. Queda optimizar el listado PostgreSQL para evitar una consulta por formato. La migración exige evidencia de todos los bloques y aceptación institucional; compilar o pasar simuladores no cumple esos criterios.
