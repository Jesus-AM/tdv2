# Sistemas y herramientas y módulos SIIv2 — 2026-10-09

**Presentación vigente 2026-10-16:** revertida sólo la última delimitación. Los grupos recuperan una celda de ancho completo para código/nombre y **Agregar sistema o herramienta**. Se retiran los laterales vacíos y separadores nuevos; vuelven fondos, espaciados y anchos previos. Las acciones de cada registro permanecen en Acciones fija a la derecha; Edición sigue fija salvo la excepción móvil anterior hasta 600 px. El botón de grupo conserva asociación, permisos y alta explícita; no crea filas automáticamente. [Alcance, pruebas, capturas y límites de la reversión](COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente).

**Actualización vigente 2026-10-16 (catálogo):** herramientas opcionales por procedimiento, nuevas familias Microsoft 365/Adobe, opciones históricas conservadas y duplicados nuevos rechazados. Medios utilizados y Otro medio están retirados, incluida su persistencia específica. Requiere `20261016190000_RemoveUsedMedia`; el resto de los envíos permanece intacto. [Catálogo vigente, limpieza exacta, respaldo y actualización Windows/Ubuntu](CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md). Las verificaciones y decisiones anteriores de este documento son evidencia fechada, no instrucciones para reinstalar el catálogo ni Medios.

**Regla vigente 2026-10-16:** capturar herramientas por procedimiento es opcional (cero, una o varias). Las filas iniciadas conservan requisitos y condicionales; no tener herramientas no resta avance ni impide enviar. Los formatos nuevos comienzan sin filas automáticas. Revisar y enviar mantiene los pendientes reales con Ir al campo; se retira la cobertura obligatoria y su acción de alta. Se conserva la tabla compacta, multiselección, asociaciones, reservas y autoguardado. [Fórmula, textos y compatibilidad](COBERTURA_Y_GUARDADO.md). Las referencias a cobertura obligatoria en las entregas anteriores son históricas.

**Actualización vigente 2026-10-15:** Aplicaciones móviles / Aplicaciones Web usan exclusivamente las listas versionadas de los Excel: 26 móviles y 207 web. PostgreSQL conserva IDs anteriores cuando la correspondencia exacta es inequívoca, UUID locales nuevos e instantáneas históricas. El importador no utiliza ACTIVA ni responsables. Los multiselectores mantienen cero/una/cantidad, búsqueda, casillas, nombres completos y reservas; una o más aplicaciones válidas cuentan como un requisito. SQL Server sólo sincroniza UR y módulos. Nueva migración **20261015165555_LocalApplicationLists** e importación explícita, sin alterar formatos enviados. [Contrato, correspondencias y comandos](APLICACIONES_LISTAS.md). Las referencias anteriores a aplicaciones sincronizadas desde SQL Server son históricas.

## Agrupación dentro de la tabla y redacción — 2026-10-16

**Implementado:** la tabla presenta un encabezado por procedimiento, con código y nombre completo y el botón **Agregar sistema o herramienta**. Sigue el orden de Identificación general; cada grupo conserva el orden relativo original de sus herramientas. El alta es explícita, usa el código confirmado por el servidor y conserva la comprobación de elegibilidad/reserva. Los procedimientos incompletos se identifican y no admiten asociaciones nuevas. Un procedimiento sin herramientas sólo muestra su encabezado y acción, no crea registros ni pendientes. **Agregar fila** continúa fuera del contenedor para capturar y elegir el procedimiento. Medios utilizados se retiró en la actualización vigente.

La relación sigue siendo `sistemas[].proceso` → `identificacion[].codigo`, confirmada y estable en el servidor. `system-groups.ts` sólo proyecta los datos; no ordena ni modifica el JSON. Renombrar actualiza el encabezado; reasignar mueve la fila; vínculos vacíos o desconocidos quedan en **Sin procedimiento asignado**. Todos los registros se muestran inicialmente. Las filas React conservan claves por ID y son hermanas en un único cuerpo de tabla: mover un grupo no desmonta controles. `StableCaptureBody` conserva el punto de lectura, desplazamiento horizontal y, si el movimiento DOM lo pierde, el foco del mismo control con su selección; no fuerza foco durante scroll ni modifica reservas. El encabezado de grupo se adapta al ancho visible y acompaña el desplazamiento horizontal. Encabezados de columna, presencia fija, acciones, menús y navegación desde pendientes se conservan.

**Textos:** revisadas Contexto, Identificación, Sistemas, Medios, Datos, Evaluación, Preguntas, Acuerdos y Revisión. Ayuda «Área responsable», explicación directa de prioridad, concordancia de cantidades ILDA/registros, confirmación de eliminación sin anunciar asociaciones cuando son cero y explicación de reasignación más precisa. Preguntas históricas se presentan con «procedimiento», trato de tú y redacción uniforme; opciones como «Ninguno o muy pocos» y «El conocimiento se comparte» mantienen sus valores originales. Los mapas son explícitos y sólo afectan etiquetas conocidas, nunca respuestas libres, identificadores, nombres de catálogos ni instantáneas. Mensajes del catálogo conservan la escritura oficial de SIIv2.

**Compatibilidad:** ajuste frontend, sin migración ni actualización de datos. Conserva captura opcional, evaluador/avance/envío, multiselección, referencias históricas, reservas, autoguardado y bloqueo de enviados. No habilita secciones posteriores.

**Verificado:** TypeScript/Vite/.NET; **91/91** pruebas frontend y **115/115** dominio/HTTP con dobles. **9 casos nativos** con PostgreSQL 18 desechable: opcionalidad y rechazo de filas incompletas, ausencia de altas automáticas, retiro transaccional/conflicto, conexiones independientes, reintentos, rollback, JSON/snapshots intactos, multiselección y referencias históricas. **9/9 recorridos nuevos Edge → ASP.NET → PostgreSQL**: orden intercalado, alta por grupo, Ir al campo, reasignación sin remount (identidad DOM comprobada), guardado/recarga, autoguardado conservando scroll y selección, reserva observada desde otra pestaña, renombrado, eliminación/reasignación, encabezados/acciones, teclado/tacto emulado y textos de las ocho secciones. Capturas a 1366/390/320 px; sin desbordamiento de página y botón de grupo dentro de su ancho. **11/11 recorridos nativos adicionales de catálogos**: SIIv2 y aplicaciones móviles/web, cero/una/varias selecciones, búsquedas, PostgreSQL, recarga, respuestas históricas, Otra/Otro, dos pestañas/SignalR, teclado/móvil de 360 px y enviado inmutable. Total: **20 recorridos de navegador con transporte real**. **9/9 casos adicionales de selectores con transporte simulado**, incluyendo reserva lenta/denegada/fallida, búsqueda, cambios rápidos e intención tardía; se distinguen de la prueba PostgreSQL.

Evidencia: `.artifacts/grouped-tools-{types,frontend,vite,build,unit}.txt`, `.artifacts/native-postgres-20261016-161608-6fa565a3/{verification.json,system-groups-native-browser.json}` y capturas `system-groups-table-*.png` / `form-texts-*.png`; `.artifacts/native-postgres-20261016-161846-ea0c4d59/systems-native-browser.json` y `.artifacts/grouped-tools-catalogs.txt` para los 11 recorridos de catálogos; `.artifacts/record-select-ui/` para transporte simulado. El reporte nativo cuenta nueve casos de API y un ejecutor de navegador que contiene nueve recorridos (no son 18 pruebas de API). Durante la preparación del recorrido se corrigieron esperas de carga inicial, adquisición mediante foco antes de `fill`, y apertura con teclado después de Ir al campo; no se relajaron las reservas de producción para superar esos errores de prueba. Compilación conserva las dos advertencias NuGet preexistentes de `System.Security.Cryptography.Pkcs` 9.0.18.

**Pendiente:** dispositivos táctiles físicos, lector de pantalla y aceptación institucional. Identidad y fuentes remotas sintéticas; las dos pestañas son sesiones de la misma cuenta de prueba. No equivalen a dos identidades institucionales ni a sincronizaciones reales. Los clústeres nuevos se detuvieron al finalizar; no se inspeccionaron ni modificaron bases existentes.

**Archivos de esta entrega:**

- `ClientApp/resources/js/pages/FormatoUR.tsx`, `lib/system-groups.ts`, `Components/StableCaptureBody.tsx`, `Components/CaptureTableScroll.tsx` y `ClientApp/resources/css/app.css`: grupos, altas, claves y posición/foco.
- `lib/form-labels.ts`, `Components/ProcedureFieldHelp.tsx`, `Components/RemoveRowDialog.tsx`, `Components/FormSubmissionReview.tsx` y `Components/CatalogMultiSelect.tsx`: presentación de textos; los nombres de campos internos permanecen.
- `ClientApp/tests/frontend/form-review.test.mjs`, `ClientApp/tests/browser/system-groups-native.mjs`, `tests/TDV2.NativeVerification/{NativeApplication,NativeTests}.cs`: proyección pura, fixture sintético y recorrido navegador real.
- `README.md`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md` y este documento. Los demás archivos ya modificados en Git se conservaron.

Comandos de comprobación, con Node disponible:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
$env:TDV2_TEST_CASE_PREFIX='Edición/integridad:|Edición/módulos'
$env:TDV2_TEST_BROWSER_FLOW='system-groups'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly
# Regresión de catálogos/controles con transporte real y fuentes sintéticas:
$env:TDV2_TEST_BROWSER_FLOW='systems'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
# Reservas lentas/fallidas con transporte simulado:
Push-Location ClientApp
node tests/browser/record-select-ui.mjs
Pop-Location
```

## Resumen compacto y pendientes — 2026-10-14

### Implementado

- Sistemas ya no muestra el bloque «Procedimientos y captura de herramientas», sus contadores/lista/acciones. Conserva la tabla con su scroll, Agregar fila y Medios debajo. Cobertura, requisitos, porcentaje y bloqueo del envío siguen siendo los del backend, sin modificaciones.
- Revisar y enviar conserva **Ir al campo** para enfocar el control incompleto y **Agregar registro** para abrir Sistemas y preparar la fila con procedimiento confirmado. Si ya existe captura, la localiza; no duplica registros para mostrar pendientes. Espera el guardado y conserva reservas/permisos.
- Módulos muestra **Selecciona módulos** cuando está vacío, **nombre completo** si hay uno, o **N módulos seleccionados** cuando hay varios. Sin IDs añadidos, chips ni lista de nombres dentro del campo. Un nombre largo se distribuye en varias líneas. Mantiene ancho, fuente de 13 px, sangría/línea y espacio de etiqueta flotante. Sólo errores o problemas reales del catálogo tienen texto debajo.
- El menú conserva búsqueda y nombres completos con casillas, selección múltiple y cierre con Escape/clic exterior. El resumen permanece durante la búsqueda para conservar el control/foco durante presencia y ACK. Las referencias históricas seleccionadas retiradas del catálogo también pueden desmarcarse; una vez retiradas dejan de ofrecerse. Los IDs, instantáneas, validaciones y persistencia no cambian.

### Verificado en este ajuste

- TypeScript, Vite y **88/88 pruebas frontend** existentes.
- **46 grupos Edge con transporte simulado**: módulos (12), apertura/reservas (9), Sistemas (13) y scroll (12). Cero/uno/varios con guardado/recarga; búsqueda, descripciones duplicadas, desmarcar hasta cero, historial, errores junto al campo, consulta bloqueada, primer clic, reserva lenta/denegada, cambio rápido entre filas, foco/teclado/tacto emulado y scroll. Tablas cortas/largas, menús, nombres completos y pantallas 1366/390/360/320 px según la suite. Se actualizaron las expectativas de accesibilidad y etiqueta flotante para la nueva presentación.
- **12 recorridos Edge → ASP.NET → PostgreSQL 18 aislado**: revisión/cobertura (5) en `.artifacts/native-postgres-20261014-160500-b8e82bf5`; Sistemas (7) en `.artifacts/native-postgres-20261014-160634-f0bf4a95`. Identidad/fuentes sintéticas; guardado, recarga, SignalR y reservas reales. Tres estados del selector persistidos, revisión/foco/alta, pendiente de última herramienta eliminada, recuperación sin duplicados, reasignación, ayuda, dos sesiones, envío y bloqueo. No equivalen a dos identidades institucionales.
- Capturas revisadas: `.artifacts/sii-multiple-ui/{empty,single,multiple}-*.png` y capturas nativas. Dispositivos físicos, lector de pantalla y aceptación institucional pendientes.

Comandos desde la raíz, con Node/npm en PATH:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
Push-Location ClientApp
node tests/browser/sii-multiple-ui.mjs
node tests/browser/record-select-ui.mjs
node tests/browser/system-tools-ui.mjs
node tests/browser/capture-scroll-ui.mjs
Pop-Location
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
$env:TDV2_TEST_BROWSER_FLOW='coverage'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='systems'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

**Archivos:** `Components/SystemField.tsx`, `FormSubmissionReview.tsx`, `pages/FormatoUR.tsx`, `resources/css/app.css`; retirado `Components/ProcedureCoverageList.tsx`. Pruebas: `sii-multiple-ui.mjs`, `record-select-ui.mjs`, `system-tools-ui.mjs`, `system-tools-native.mjs`, `procedure-coverage-native.mjs`. Documentación: README, ESTADO_MIGRACION, INVENTARIO, este documento y COBERTURA_Y_GUARDADO.

**Actualización:** sólo frontend; ninguna migración, configuración ni modificación de backend. Clústeres sintéticos nuevos detenidos al terminar. Sin acceso a bases existentes/institucionales, Nexo/Herd, credenciales reales, push o despliegue.

## Cobertura de procedimientos — 2026-10-14

Cada procedimiento aplicable requiere al menos una herramienta completa. Sus pendientes se localizan en Revisar y enviar, con código/nombre completo y acciones de captura/revisión. Pendientes/avance/envío comparten la evaluación del servidor. Retirar un procedimiento informa cuántas asociaciones se pierden; conserva respuestas y permite reasignarlas. Modelo híbrido y módulos múltiples conservados; versión de estructura compatible sin migración. [Contrato, fórmula, pruebas nativas, archivos y actualización](COBERTURA_Y_GUARDADO.md).

## Selección múltiple de módulos — 2026-10-14

### Implementado

- **Módulos de SIIv2** usa Autocomplete múltiple MUI: búsqueda por nombre, casillas, menú que continúa abierto al marcar/desmarcar y cierre con Escape/clic exterior. La primera implementación usaba etiquetas removibles y ayuda bajo el campo; el ajuste compacto descrito arriba sustituye esa presentación. Conserva selección individual por ratón/teclado, nombres completos en opciones, fuente de 13 px, sangría/línea de agrupación, etiquetas flotantes y espacio propio para botones.
- Abrir sigue iniciando la reserva desde la primera interacción. Durante su confirmación no se habilitan opciones, borrado ni selección. La presencia y el ACK de autoguardado mantienen el control, menú, búsqueda y foco; las respuestas tardías no reabren otro registro. Los errores quedan asociados a `modulosSii` y conservan lo capturado. Procedimiento mantiene su nombre completo y comportamiento anterior.
- Cada fila sigue representando una herramienta con procedimiento, uso, funcionamiento y áreas de oportunidad compartidos. Varias filas SIIv2 del mismo procedimiento continúan permitidas para respuestas diferentes; no se consolidan registros.

### Contrato, persistencia y compatibilidad

`SystemRow.modulosSii` es una colección de `{ id: string, descripcion: string }`. Los IDs institucionales son la asociación; nunca se concatenan nombres. El cliente remite la colección en el mismo parche de bloque con reserva/versión/operación existente. El backend rechaza IDs inválidos, elementos repetidos, estructuras incorrectas y selecciones nuevas que no estén disponibles en la réplica local. Valida **todos** los elementos antes de confirmar la escritura; un elemento inválido impide publicar parcialmente el registro. Las descripciones nuevas se obtienen del catálogo local bajo bloqueo compartido dentro de la transacción de guardado. No se consulta SII al capturar.

`SystemModules` centraliza la lectura compatible, validez estructural y confirmación de instantáneas:

- Si no existe la colección, lee `moduloSiiId`/`moduloSiiDescripcion` como una selección de un elemento. GET no migra ni reescribe contenido. Guardar otros campos de un registro escalar no fuerza su conversión.
- Al cambiar explícitamente la selección desde la interfaz, guarda la colección y limpia los campos escalares de esa fila para evitar asociaciones contradictorias. Una colección vacía es autoritativa: no resucita el ID anterior.
- Un cliente antiguo que omita la colección al guardar otro campo conserva la colección previamente confirmada. No borra selecciones múltiples por omisión.
- Cada ID conservado mantiene su descripción guardada, aunque cambie o desaparezca de las opciones disponibles. El cliente no puede sustituir esa instantánea por otro texto. Una referencia retirada puede mantenerse o quitarse; incorporarla nuevamente exige disponibilidad. Dos IDs con igual descripción siguen siendo dos selecciones distintas, sin mostrar sus IDs en la interfaz.
- Los detalles de una herramienta no activa no cuentan como respuestas ni bloquean la captura de campos ajenos. Al volver a SIIv2 se verifica nuevamente la disponibilidad. Los enviados e históricos bloqueados siguen inmutables.

**Sin migración nueva:** `UnitForm.Content` ya se almacena en `formatos_ur.contenido` de tipo `json`; recibos e instantáneas también admiten el nuevo arreglo. No cambian entidades/columnas, códigos, relaciones de procedimientos ni migraciones aplicadas. No hay un comando EF adicional por este ajuste. El catálogo continúa requiriendo sus migraciones existentes, aplicadas sólo de manera explícita por el operador como documenta la sección de operación.

### Avance y pendientes

Se mantiene `floor(100 × requisitos válidos / requisitos aplicables)`, sin ponderación adicional. Para una fila SIIv2, **Módulos de SIIv2 cuenta una vez**, independientemente de cuántos se elijan: requiere al menos uno, todos los IDs válidos/no repetidos y sus descripciones confirmadas. Las referencias históricas autorizadas conservan validez aunque se retiren del catálogo. Vacío sigue siendo un borrador guardable y genera un pendiente `modulosSii`; al cambiar a otra herramienta ese requisito deja de aplicar. Inicio, captura y revisión reutilizan `FormRequirements`/`SystemAnswers`; los enlaces a pendientes enfocan el mismo campo plural.

### Verificado

- TypeScript, Vite y compilación .NET Release sin errores. **106/106 casos dominio/HTTP**: colección, compatibilidad, instantáneas, IDs inválidos/duplicados, estructuras y avance/revisión, además de regresiones de autorización. **88/88 pruebas frontend** de bloques, reservas, transporte y navegación.
- **55 grupos Edge con transporte simulado**: `sii-multiple-ui.mjs` (12), `system-tools-ui.mjs` (13), `record-select-ui.mjs` (9), `capture-scroll-ui.mjs` (12), `procedure-select-ui.mjs` (9). Búsqueda/casillas, descripciones duplicadas, quitar con icono/Delete, vaciar/guardar/recargar, errores 422, valores históricos, nombres largos, tipografía, foco y ACK sin cerrar menú, reserva lenta/denegada, cambios rápidos, ratón/teclado/tacto emulado, tablas/menús y escritorio/móvil hasta 320 px. Capturas y reportes bajo `.artifacts/sii-multiple-ui` y las carpetas homónimas de cada suite; revisadas capturas de escritorio y móvil.
- **3/3 casos HTTP con ASP.NET/EF Core y PostgreSQL 18 desechable** (`native-postgres-20261014-140747-4d26df2a`): persistencia/recarga de varios IDs, duplicados de descripción, asociación escalar previa, rechazo de elemento inválido sin cambios parciales, avance persistido, vaciado, retirada histórica, otras filas y permisos de catálogo/enviados. Los fallos iniciales detectaron un detalle histórico inactivo que se intentaba validar; se conservó el criterio anterior de validar módulos activos. También se ajustaron datos/SQL del escenario sintético; la repetición terminó correcta.
- **7/7 recorridos Edge → ASP.NET → PostgreSQL**, con SignalR y guardado reales, proveedores institucionales sustituidos por dobles. Borrador sin catálogo, pendiente plural e Ir al campo con foco, selección de dos IDs con descripción igual, menú abierto durante guardado, recarga, desaparición del pendiente, descripciones conservadas al renombrar/retirar, Otros, sesiones separadas, varias filas por procedimiento y consulta de enviado. Corrida final correcta `native-postgres-20261014-141032-b76042b3`.
- Los clústeres se crearon bajo `.artifacts/native-postgres-*`, en loopback/puerto aleatorio, con identidad comprobada por el arnés y cierre al terminar. El mensaje inicial de token restringido de `initdb` no impidió completar las corridas. Sólo se aplicaron migraciones existentes a esas bases desechables; no se accedió a bases reales/institucionales.

Comandos desde la raíz (Node/npm disponibles en PATH):

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
Push-Location ClientApp
node tests/browser/sii-multiple-ui.mjs
node tests/browser/system-tools-ui.mjs
node tests/browser/record-select-ui.mjs
node tests/browser/capture-scroll-ui.mjs
node tests/browser/procedure-select-ui.mjs
Pop-Location
$env:TDV2_TEST_CASE_PREFIX = 'Edición/módulos'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser -DirectStart
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_NODE = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:TDV2_TEST_BROWSER_FLOW = 'systems'
$env:TDV2_TEST_EPHEMERAL_TLS = 'true'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
```

### Archivos y límites

- Interfaz: `ClientApp/resources/js/Components/SystemField.tsx`, `pages/FormatoUR.tsx`, `types/tdv2.ts`, `ClientApp/resources/css/app.css`.
- Backend: `tdv2/Domain/SystemModules.cs` (nuevo), `SystemAnswers.cs`, `FormSchema.cs`, `FormRequirements.cs`, `FormReview.cs` y `tdv2/Services/FormEditingService.cs`.
- Pruebas: `tests/TDV2.Verification/SystemCatalogTests.cs`, `CaptureProgressTests.cs`, `tests/TDV2.NativeVerification/SystemModuleTests.cs`; navegador `sii-multiple-ui.mjs` (nuevo), `system-tools-ui.mjs`, `record-select-ui.mjs`, `capture-scroll-ui.mjs`, `system-tools-native.mjs` y etiqueta plural de `table-scroll-native.mjs` (sintaxis comprobada; suite completa no ejecutada en este ajuste).
- Documentación: este archivo, README, ESTADO_MIGRACION e INVENTARIO.

**Pendiente:** aceptación institucional, dispositivos físicos y lector de pantalla. El móvil/tacto se emula y las dos sesiones del navegador nativo usan la misma identidad sintética; no se presenta como prueba entre dos personas diferentes. Fuentes SII/ILDA/Microsoft y autoridad Nexo son sintéticas, no acreditan servicios institucionales. No cambios de Nexo/Herd, credenciales reales, push ni despliegue.

## Catálogo histórico, Procedimiento y desplazamiento propio — 2026-10-13

**Implementado:**

- Portal del CAST deja de ofrecerse en Sistema o herramienta. `portal_cast` conserva su etiqueta completa al leer respuestas anteriores, incluidas las bloqueadas; no se sustituye ni borra al guardar otro campo. `SystemAnswers.SelectableTools` excluye esa clave en nuevas asignaciones, mientras `Tools` conserva su validez histórica para avance/revisión. La validación sólo admite conservarla en el mismo registro que ya la tenía; no permite introducirla en uno nuevo ni regresar a ella después de cambiar. No cambia la obligatoriedad ni los campos condicionales de las demás opciones.
- Identificación muestra **Procedimiento**, también en el nombre accesible, las instrucciones de Contexto, el botón **Agregar procedimiento** y los pendientes del servidor mediante `ProcedureEligibility.Required`. La ayuda explica: «Identifica el procedimiento que realiza tu área para atender una necesidad o cumplir una función». La clave `tramite`, sus reglas, asociaciones y contenido permanecen intactos.
- `CaptureTableScroll` proporciona una sola región de desplazamiento por cada tabla de captura, con ambos ejes, acceso por teclado y nombre accesible. La altura es natural para tablas cortas; el máximo es `max(160px, 100dvh - 230px)` para reservar espacio de navegación/acciones. Sustituye la instrucción anterior de desplazar todas las filas únicamente mediante la página. No hay manejadores que cancelen rueda/tacto ni `overscroll-behavior: contain`. Los campos de texto mantienen su desplazamiento propio y límites visuales existentes.
- Los encabezados permanecen fijos dentro del contenedor, sobre las presencias de las filas; Edición tiene la capa superior en su intersección. Un `ResizeObserver` mide exclusivamente el encabezado para reservar su altura al navegar. El marco exterior del formato usa `overflow: clip` para conservar las esquinas sin crear otro contenedor programáticamente desplazable. Menús MUI/Autocomplete siguen en portales fuera de la región, sin recortarse. Agregar queda fuera del contenedor y Medios mantiene su bloque/reserva independiente debajo.
- `focusCaptureField` enfoca sin desplazamiento automático y, tras el render provocado por el foco, revela el control completo respetando cabecera/presencia/barra institucional. Sólo actúa si el destino continúa conectado y enfocado. Ir al campo y la alta confirmada de una fila usan esta misma presentación; al agregar también se revela la fila. No se reinicia el scroll por renders, presencia, reserva, escritura o guardado. Se conservan reservas, eventos de apertura inmediata, nombres completos, versiones y autoguardado.

**Probado:** tipos y Vite correctos; compilación Release y **104/104 casos dominio/HTTP con dobles**, **88/88 frontend**. Los dos casos nuevos de dominio verifican rechazo de nuevas asignaciones CAST, conservación al guardar comentarios, corrección explícita a Excel, etiqueta histórica, avance sin penalización adicional y terminología del pendiente `tramite`.

Edge/React/MUI reales: `capture-scroll-ui.mjs` **12/12 grupos** con 1, 30 y 31 filas en 1366/390 px; ausencia de CAST en menús, histórico guardado/recargado en fixture, tabla corta sin espacio vacío, ambas barras, cabeceras/capas, primera/última fila, scroll estable al escribir/guardar/presencia, PageDown, rueda, gesto táctil emulado, continuidad a la página tras llegar al extremo, menús de Procedimiento/Módulo/herramienta/uso, dependientes, Ir al campo y fila agregada visible con foco. Para comprobar continuidad se usan gestos de rueda separados: uno alcanza el extremo y el siguiente continúa en la página. Los gestos táctiles se envían como eventos touchStart/move/end. Se conservan **9/9** de interfaz, **9/9** de nombres completos de Procedimiento, **9/9** de apertura/reservas y **13/13** de Sistemas. Reportes/capturas en `.artifacts/capture-scroll-ui/`, `.artifacts/procedure-select-ui/`, `.artifacts/interface-layout-20261010/`, `.artifacts/record-select-ui/` y `.artifacts/system-tools-ui/`.

**Pendiente/límites:** ningún acceso a bases existentes, fuentes institucionales o credenciales. Dominio/HTTP usa TestServer y almacenes sustituidos; navegador simula HTTP, identidad, reservas, SignalR y persistencia en memoria. No es integración PostgreSQL/Nexo/SII. Móvil/tacto emulados; dispositivos físicos, panel táctil físico y lector de pantalla pendientes. Las suites nativas sólo actualizan selectores de texto y comprueban sintaxis; no se ejecutaron porque requieren base. Sin migraciones, dependencias nuevas, push o despliegue.

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
Push-Location ClientApp
try {
    node tests/browser/capture-scroll-ui.mjs
    node tests/browser/system-tools-ui.mjs
    node tests/browser/procedure-select-ui.mjs
    node tests/browser/record-select-ui.mjs
    node tests/browser/interface-layout.mjs
} finally { Pop-Location }
```

Archivos: `ClientApp/resources/js/Components/{CaptureTableScroll,SystemField,ProcedureFieldHelp}.tsx`, `ClientApp/resources/js/lib/capture-focus.ts`, `ClientApp/resources/js/pages/FormatoUR.tsx`, `ClientApp/resources/css/app.css`, `tdv2/Domain/{SystemAnswers,ProcedureEligibility}.cs`, `tests/TDV2.Verification/SystemCatalogTests.cs`, nueva `ClientApp/tests/browser/capture-scroll-ui.mjs`; etiquetas de pruebas en `delivery-collaboration-native.mjs`, `first-stage-flow.mjs`, `interface-layout.mjs`, `native-flow.mjs`, `sync-flow.mjs`, `table-scroll-native.mjs`. Documentación: README, ESTADO_MIGRACION, INVENTARIO, EDICION_AUTOMATICA y este archivo. Los cambios locales previos se conservan.

## Procedimiento completo y sin ayuda por registro — 2026-10-13

**Implementado:** se retiran el botón de ayuda por fila y el tooltip del procedimiento. El selector ocupa todo el espacio interior de la celda; permanecen las ayudas de los encabezados y demás campos. `ProcedureSelect` usa el código y el nombre completos recibidos en `procedimientosDisponibles` y, para una relación histórica fuera de opciones, los de `contenido.identificacion`. No hay textos institucionales codificados en el componente ni cambios en asociaciones/identificadores. La columna conserva 280–360 px; el valor usa `renderValue` de MUI, varias líneas sin elipsis/límite, altura natural y descripción accesible asociada al input. Las opciones también respetan nombres completos y saltos de línea. El input nativo mantiene búsqueda y navegación por teclado.

Se comparte con Módulo SII el CSS local de valor multilínea y columna independiente para botones, conservando los 13 px y la descripción sin ID de ese módulo. Procedimiento mantiene su nombre al abrir y coloca la búsqueda inmediatamente debajo dentro del control. La prueba inicial con un nombre muy largo reprodujo que retirar el nombre al abrir contraía la fila entre `mousedown` y `pointerup`: este último alcanzaba Otro medio y cerraba el menú. Mantener el valor evita esa contracción sin retrasos ni cambios en los eventos de reserva. Se conservan apertura inmediata, estados pendientes y selección autorizada, IDs estables, guardado al salir y autoguardado.

**Probado:** TypeScript y Vite correctos; **88/88 frontend**. Edge con React/MUI real: `procedure-select-ui.mjs` **9/9** grupos en 1366/390/320 px; `interface-layout.mjs` **9/9**; `record-select-ui.mjs` **9/9**; `system-tools-ui.mjs` **13/13**. Se verifican PO-08/PO-09 con nombres sintéticos extensos, saltos de línea y otro nombre de 14 repeticiones; texto íntegro cerrado/abierto, ancho fijo, tamaño, no solapamiento con botones, ausencia de ayuda por fila, descripción accesible, búsqueda por código/nombre, ratón/tacto emulado, flechas/Enter/Tab/Escape, flecha tras scroll horizontal, guardado/recarga de la asociación y consulta histórica. Las regresiones conservan reserva lenta/denegada, cambios rápidos de fila, selección protegida, SII, errores, ayudas y enviados. Reportes y capturas en `.artifacts/procedure-select-ui/`, `.artifacts/interface-layout-20261010/`, `.artifacts/record-select-ui/` y `.artifacts/system-tools-ui/`.

**Pendiente/límites:** los nombres reales de PO-08 y PO-09 no están en las fuentes/fixtures revisados; no se accedió a bases para obtenerlos. La aplicación presenta los nombres existentes que recibe del formato sin transformarlos; la aceptación de esos dos registros reales queda pendiente. Identidad, HTTP, SignalR, reservas y persistencia están simulados en memoria; estas pruebas no acreditan ASP.NET/PostgreSQL ni acceso institucional. Móvil/tacto emulados, dispositivos físicos y lector de pantalla pendientes. Sin bases modificadas, migraciones, dependencias, push o despliegue.

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
Push-Location ClientApp
try {
    node tests/browser/procedure-select-ui.mjs
    node tests/browser/interface-layout.mjs
    node tests/browser/record-select-ui.mjs
    node tests/browser/system-tools-ui.mjs
} finally { Pop-Location }
```

Archivos de este ajuste: `ClientApp/resources/js/Components/{ProcedureSelect,SystemField}.tsx`, `ClientApp/resources/css/app.css`, `ClientApp/tests/browser/{procedure-select-ui,interface-layout}.mjs`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md` y este documento. Se conservan los cambios locales anteriores.

## Apertura y reserva de selectores — 2026-10-13

**Reproducido:** el primer clic de Módulo seguía `pointerdown → mousedown → focusin → mouseup → click`. `selectEvents.onOpen` esperaba `engine.focus`, pero el `focusin` del registro generaba otra intención y la anterior devolvía falso. La reserva quedaba propia y el menú cerrado (`aria-expanded=false`). Evidencia sintética: `.artifacts/record-select-ui/baseline.json`. No se cambiaron las claves de fila por ID: las pruebas comprueban que el input sigue siendo el mismo nodo al llegar la presencia/confirmación. A → B → A, con A esperando respuesta, también reprodujo una adquisición obsoleta de B; la nueva regresión frontend falló antes de corregirla.

**Implementado:**

- Apertura visual independiente de autoridad en los dos Autocomplete del registro: Módulo y Procedimiento. El mismo clic inicia la reserva y muestra «Preparando edición…», con `aria-busy` y el estado accesible de MUI. Buscar sólo modifica el filtro local. No hay opciones seleccionables ni acción de limpiar hasta que `canEdit` confirme reserva/contenido vigentes; `changeBlock` conserva su comprobación final. Los Select existentes mantienen su interacción.
- Cada apertura tiene una intención identificada por referencia, registro y campo. Los cierres sólo afectan a esa combinación; salir de fila/sección o desmontar invalida la intención. Un ACK tardío no vuelve a abrir ni enfoca un control. La presencia actualiza la fila sin desmontar el input. Los fallos y la ocupación se explican en el menú con mensajes públicos; no cambian ID/instantánea ni respuestas.
- El popup completo, incluso carga/error/sin opciones, pertenece al contexto del registro. Pulsar el estado de carga conserva el foco con el mismo tratamiento localizado que MUI usa para «sin opciones». Entrar en un campo de otra fila sí se reconoce como salida aunque todavía exista el menú anterior: termina el guardado y liberación existentes. No se bloquea el desplazamiento ni los clics exteriores de forma global.
- El motor espera adquisiciones/liberaciones en curso y comprueba otra vez la intención antes de pedir una reserva en cola. Así B no queda ocupado si el usuario ya volvió a A. No se modifican reservas concedidas por el servidor, caducidad, renovaciones, permisos, autoguardado, backend ni envío.
- La revisión visual detectó la regla MUI `width: 0; min-width: 30px` del input imponiéndose al ancho del buscador. Se precisa el selector CSS local para que ocupe su columna disponible al abrir, conservando la columna de botones y el valor multilínea.

**Probado:** TypeScript y Vite correctos; **88/88 frontend**, incluida la regresión A/B/A. `record-select-ui.mjs`: **9/9** recorridos con primer clic en campo/flecha, ratón, Tab/flechas/Enter, tacto a 390 px, reserva lenta controlada, nodo/foco estables, selección bloqueada antes del ACK, carrera 409 con otra sesión, fallo 503, reintento, Escape, salida de sección y cambios rápidos. Se comprueban guardado anterior e ID final con el fixture. `system-tools-ui.mjs`: **13/13** regresiones de tipografía, nombres completos, errores, ayudas, sesiones, selección/persistencia sintética, consulta y Acciones en 1920/1366/768/360 px. Evidencia en `.artifacts/record-select-ui/report.json`, capturas de carga/denegación/error/tacto y `.artifacts/system-tools-ui/report.json`.

**Pendiente/límites:** navegador Edge ejecuta React/MUI real con HTTP, identidad, SignalR y reservas simulados en memoria. No son pruebas ASP.NET/PostgreSQL ni de autorización institucional. Móvil/tacto emulados; dispositivos físicos y lector de pantalla pendientes. No se modificaron bases ni se ejecutaron migraciones/sincronizaciones. Sin dependencias nuevas, push o despliegue.

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
Push-Location ClientApp
try {
    node tests/browser/record-select-ui.mjs
    node tests/browser/system-tools-ui.mjs
} finally { Pop-Location }
```

Archivos de este ajuste: `ClientApp/resources/js/pages/FormatoUR.tsx`, `ClientApp/resources/css/app.css`, `ClientApp/resources/js/Components/{SystemField,ProcedureSelect}.tsx`, `ClientApp/resources/js/lib/{record-autocomplete,record-editing-context,block-editor}.ts`, `ClientApp/tests/frontend/block-autosave.test.mjs`, `ClientApp/tests/browser/record-select-ui.mjs`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md`, `docs/migracion/EDICION_AUTOMATICA.md` y este documento. Los cambios locales anteriores se conservan.

## Tipografía y módulo completo — 2026-10-13

**Implementado:** la medición en navegador encontró contenido de 13 px y etiquetas MUI de 16 px, mientras la abertura del borde se calculaba sobre 13 px. Los tres campos dependientes ahora comparten familia, tamaño base de 13 px y peso 400 con sus controles principales. Se conserva la reducción normal de la etiqueta flotante de MUI; coincide con la abertura del borde. El ajuste CSS se limita a los dependientes de Sistemas y no cambia la tipografía global. El grupo mantiene sangría/línea y reserva 12 px arriba, además de los 8 px entre controles, para separar la etiqueta del selector principal tanto al enfocar como al mostrar errores.

El módulo usa `renderValue` de Autocomplete MUI para mostrar su descripción completa en varias líneas. La altura se adapta al nombre sin elipsis ni tooltip; el input de búsqueda conserva su interacción nativa y el nombre seleccionado se asocia también como descripción accesible. Las opciones permiten saltos de línea y usan la escala compacta. Los botones de abrir/limpiar ocupan su propia columna dentro del control: se reserva su tamaño real incluso con las áreas táctiles de móvil. No cambia el ancho de la tabla, los IDs, la instantánea, los eventos de reserva, la validación o el guardado. Otra herramienta y Otro uso conservan texto multilínea y errores junto al campo.

**Probado:** TypeScript, Vite y **87/87 pruebas frontend**, correctos. `system-tools-ui.mjs`: **13/13 recorridos Edge** con React/MUI real. Nuevas mediciones de familia/tamaño/peso de input, etiqueta y placeholder en los tres campos, vacíos/completos/enfocados/con error; separación de etiqueta y escala de la abertura del borde. Selección larga en varias líneas, opciones completas, búsqueda y selección con flechas/Enter, Escape, limpiar, Tab, guardado/recarga por ID y lectura sin edición en 1366/360 px. Las medidas verifican que nombre y botones no se superpongan, que no se reserve dos veces su espacio y que la tabla no cambie de ancho. Se conservan pruebas de scroll, reservas entre sesiones, errores locales sin perder texto y enviados. Capturas `typography-*.png`, `dependent-*.png` y reporte en `.artifacts/system-tools-ui/`; regresión de Acciones en 1920/1366/768/360 px. La prueba de teclado espera la reserva y recorre también el avatar que aparece al confirmarse, sin modificar esa interacción en la aplicación.

**Pendiente/límites:** HTTP, identidad, reservas y SignalR simulados; persistencia comprobada en el fixture de transporte, no en PostgreSQL/ASP.NET. No consultas institucionales ni bases modificadas. Móvil/tacto emulados; dispositivos físicos y lector de pantalla pendientes. `system-tools-native.mjs` sólo adapta las expectativas del valor seleccionado y pasa `node --check`; no se ejecutó la suite nativa. Sin migraciones, dependencias nuevas, push ni despliegue. Comandos frontend y navegador: los mismos del apartado de ajustes de captura; no se ejecutaron comandos .NET en este ajuste de presentación.

Archivos de este ajuste: `ClientApp/resources/js/Components/SystemField.tsx`, `ClientApp/resources/css/app.css`, `ClientApp/tests/browser/system-tools-ui.mjs`, `ClientApp/tests/browser/system-tools-native.mjs` (expectativas del valor visible), `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md` y este documento. Se conservan los cambios locales anteriores.

## Campos condicionales — 2026-10-13

**Implementado:** presentación compartida `DependentField` dentro de `SystemField.tsx` para SIIv2 → Módulo de SIIv2, Otra → Nombre de la herramienta y Otro uso → Describe el uso. Cada detalle sigue inmediatamente debajo de su selector dentro de la celda. Usa agrupación accesible con su etiqueta, separación de 8 px, margen lateral de 4–6 px, línea de 2 px y padding de 8–10 px; la línea toma el azul primario del tema al 28 %. No introduce fondo, tarjetas, sombras ni textos repetidos. La sangría total es de 14 px en móvil y 18 px desde el breakpoint pequeño; el ancho restante se adapta sin ensanchar la tabla.

El grupo conserva los errores del control y, para SII, los avisos de carga/error/catálogo pendiente e histórico. No es enfocable y no mueve el foco por código: el selector conserva su comportamiento MUI y Tab alcanza el campo dependiente. No se alteran condiciones, respuestas, ID/snapshot de módulo, eventos, autorización ni reservas. Se revisó el resto de la sección: Otro medio es siempre visible, por lo que no recibe una dependencia nueva. Los avisos contextuales del procedimiento no son campos adicionales de captura.

**Probado:** `npm.cmd --prefix ClientApp run types:check`, `npm.cmd --prefix ClientApp run build` y `npm.cmd --prefix ClientApp run test:forms` correctos (**87/87**). Desde ClientApp, `node tests/browser/system-tools-ui.mjs`: **12/12**, incluido un recorrido de los tres dependientes a 1366/360 px que mide sangría, línea, separación y ancho constante; comprueba foco del selector, Tab al detalle, nombres largos, errores 422 junto al control y `aria-describedby`, conservación de la reserva, corrección y guardado. Se conservan las regresiones de catálogo sin ID añadido, duplicados por ID interno, Egresados, ayudas, autoguardado, sesiones separadas, Acciones y enviados. Capturas `dependent-*.png` y reporte en `.artifacts/system-tools-ui/`.

**Límites:** React/MUI real en Edge con transporte/identidad/SignalR y errores de validación simulados; no es prueba de backend o PostgreSQL. No bases, migraciones, sincronizaciones institucionales ni dispositivos físicos. Los errores inyectados son exclusivamente datos de prueba y no cambian las validaciones de la aplicación.

Archivos de este ajuste: `ClientApp/resources/js/Components/SystemField.tsx`, `ClientApp/tests/browser/system-tools-ui.mjs`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md` y este documento. Se preservan los cambios locales de la entrega anterior.

## Ajustes de captura — 2026-10-12

**Implementado.** Acciones tenía 76 px en el `colgroup` y en la regla de última celda; con el padding existente y `overflow-wrap: anywhere` el encabezado podía dividirse. Ahora tiene 112 px, padding lateral de 16 px y texto sin salto, con encabezado y botones centrados. El mínimo de tabla aumenta los mismos 36 px, conservando el espacio de las demás columnas. Eliminar mantiene 44 × 44 px, su foco, confirmación y autorización; Edición sigue fija a la izquierda y la tabla mantiene scroll horizontal.

Se elimina la concatenación visual del ID de módulo tanto en las opciones como en la selección guardada. Se usa `descripcion` del catálogo o la instantánea `moduloSiiDescripcion` del registro; no se recortan ni sustituyen partes del nombre real. Las opciones con nombre igual siguen siendo registros independientes mediante `getOptionKey` e igualdad por ID. Se conserva el orden español, desempate interno por ID, selección/búsqueda y persistencia de `moduloSiiId`; no se modifican sincronización ni respuestas históricas.

**Egresados** usa como clave y etiqueta el mismo texto, según la convención del catálogo actual. Se añade después de Estudiantes en el selector múltiple y en `FormSchema.UserChoices`. `ProcedureEligibility`, avance y revisión ya consumen esa validación común y aceptan el nuevo valor sin duplicar reglas. Opciones anteriores, reservas y guardado se conservan.

**Áreas de oportunidad** es el encabezado, nombre accesible y título de ayuda del campo interno `fallas`. Se mantiene su texto multilínea y opcional, sin cambiar datos, estados de funcionamiento ni esquema. La ayuda conserva su explicación, ejemplo y consejo sobre problemas/comentarios.

**Probado.** TypeScript, Vite y compilación ASP.NET/Verification Release correctos (0 avisos/errores). 102 casos dominio/HTTP y 87 frontend correctos. `system-tools-ui.mjs`: 11 recorridos, incluidos duplicados que guardan IDs diferentes con nombres iguales, recarga y opción seleccionada correcta, instantánea tras cambio de nombre, nombre legítimo que contiene «ID», Egresados combinado con Docentes y texto previo/multilínea conservado en consulta. Acciones se mide en 1920, 1366, 768 y 360 px: una línea, 112 px, centro alineado, superficie de 44 px, contorno de foco completo y scroll dentro de la tabla; diálogo de eliminación con teclado. Se revisaron capturas y reservas/ayudas/estados existentes. `collaboration-actions-ui.mjs`: 4 regresiones de Guardar/Enviar/Colaboradores correctas. Las comprobaciones esperan la reserva confirmada y el cierre de diálogos antes de probar el siguiente estado; no se cambió la lógica de edición para las pruebas.

Evidencia: `.artifacts/system-tools-ui/report.json` y capturas `actions-{1920,1366,768,360}.png`; `.artifacts/collaboration-actions-20261011/verification.json`. El navegador ejecuta React/MUI real con transporte/identidad/SignalR simulados; HTTP de backend usa TestServer y almacenamiento en memoria. No acredita persistencia PostgreSQL, SII real ni dispositivos físicos. Suites nativas actualizadas sólo en sus selectores/expectativas y verificadas con `node --check`, sin ejecutarlas ni preparar bases. No hay migraciones nuevas ni ejecutadas.

Comandos desde la raíz, con Node/.NET y las dependencias existentes:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false -p:NuGetAudit=false
Push-Location ClientApp
try {
    node tests/browser/system-tools-ui.mjs
    node tests/browser/collaboration-actions-ui.mjs
} finally { Pop-Location }
```

Archivos de implementación: `ClientApp/resources/css/app.css`, `ClientApp/resources/js/pages/FormatoUR.tsx`, `ClientApp/resources/js/Components/{SystemField,UsersServedSelect,ProcedureFieldHelp}.tsx` y `tdv2/Domain/FormSchema.cs`.

Pruebas: `tests/TDV2.Verification/{CaptureProgressTests,EntryPoint}.cs`; `ClientApp/tests/browser/{system-tools-ui,collaboration-actions-ui,system-tools-native,delivery-collaboration-native,table-scroll-native,first-stage-flow}.mjs`. Documentación: `README.md`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md` y este documento.

## Implementado

Avance vigente 2026-10-10: cada herramienta aporta cuatro requisitos (procedimiento completo/relacionable, herramienta, uso y funcionamiento válidos), más módulo SIIv2, nombre de Otra o descripción de Otro uso cuando corresponda. Opcionales vacíos y detalles ocultos no penalizan. Los valores históricos ajenos a las opciones actuales permanecen sin conversión, se pueden conservar al guardar otros campos y se señalan para corregir antes del envío. La revisión y el porcentaje usan la misma evaluación. No se invalida una referencia histórica SII por retiro del catálogo: conserva ID e instantánea. [Fórmula, casos y límites](AVANCE_Y_PRUEBAS_ACCESO.md).

Presentación vigente 2026-10-13: tabla con columnas de ancho controlado; Procedimiento ocupa 280–360 px y muestra código/nombre completos en varias líneas sin ensancharse por nombres largos. Sustituye la presentación abreviada con tooltip/ayuda por fila del 2026-10-10. Autocomplete MUI busca por código/nombre y despliega opciones completas dentro del ancho de pantalla; abre durante la adquisición de reserva y sólo permite seleccionar tras autorización. Vaciar la búsqueda no elimina la asociación; Quitar selección permite retirarla explícitamente. Medios conserva bloque/reserva propios con opciones distribuidas uniformemente. [Antecedentes de interfaz](INTERFAZ_OPERATIVA.md); comprobaciones vigentes en los apartados superiores.

Se conservan las tablas, tema y densidad compacta de captura (13 px, encabezados 12 px/negritas). Identificación muestra **Código** y debajo el origen guardado: ILDA, norma o Nuevo. React no genera códigos; el servidor rechaza cambiar códigos confirmados y orígenes. Prioridad conserva cinco niveles y colores, sin Ejemplo ni Consejo. La ayuda compartida tiene X accesible, Cerrar, Escape, retorno de foco, scroll y secciones opcionales.

Una herramienta por fila; varios registros pueden compartir procedimiento hasta el límite existente de 200 filas del formulario. Procedimiento ofrece únicamente registros guardados del mismo formato, existentes, completos y válidos en sus campos obligatorios actuales y marcados V/A. `ProcedureEligibility` es la regla común de opciones, escritura transaccional, avance y revisión. No basta con nombre/código/validación. Una relación anterior que se invalide se conserva como pendiente; puede corregirse en Identificación y bloquea la entrega. Herramienta, uso y funcionamiento son selección única. SIIv2 abre un buscador de módulos; Otra y Otro uso abren detalles en su misma celda. Áreas de oportunidad sigue siendo multilínea y opcional, también para fallas/no funciona. Los estados muestran icono, texto y color.

Las instrucciones son texto normal con título, pregunta destacada y descripción. Después de la tabla aparece Medios utilizados, como sección de la misma pestaña, con reserva/autoguardado independientes. El campo vigente `Otro medio` carece de placeholder; se conserva «Portal del CAST de uso de técnicos» en herramientas. Ambas tablas comparten la primera columna fija **Edición** y un único foco de campo MUI, distinto del contorno del registro ocupado. Acciones contiene Eliminar, alineado arriba y con área de 44 × 44 px. Comentarios y detalles Otra/Otro muestran dos a cuatro líneas, con desplazamiento interno sin recortar datos. Desplazar la tabla o la página conserva la reserva y el contexto de edición; [reglas y evidencia](DESPLAZAMIENTO_TABLAS.md).

La revisión y el envío están en la pestaña siguiente, **Revisar y enviar**: comprenden Contexto (informativo), Identificación general y Sistemas y herramientas (con sus condiciones existentes, sin hacerlo obligatorio). Avance y validación del servidor usan exactamente esa etapa. Mostrar pestañas posteriores como administrador no amplía requisitos. Se conservan permisos intrínsecos de envío, reservas, versión, snapshot e inmutabilidad; los colaboradores siguen sin poder enviar. [Envíos por etapa y migración necesaria](REVISAR_ENVIO_ETAPAS.md).

No cambian permisos, participación, primera etapa, enviados, fotografías, reservas automáticas, autoguardado independiente ni eliminación directa. Abrir menús/ayudas y cambiar controles mantiene la unidad de edición. Buscar no escribe un ID ni se trata como respuesta. La primera modificación requiere reserva confirmada.

Las claves JSON nuevas se guardan estructuradas sin migrar las respuestas anteriores:

| Campo | Claves/detalle |
|---|---|
| `sistema` | `sii_v2`, `portal_cast`, `excel`, `correo`, `formulario`, `papel`, `otra` |
| `sistemaOtro` | Nombre libre separado de la opción |
| `modulosSii` | Colección de `{ id, descripcion }`: ID institucional e instantánea autorizada por el servidor por módulo |
| `moduloSiiId` / `moduloSiiDescripcion` | Asociación escalar anterior, conservada para lectura compatible cuando no hay colección |
| `uso` | `registrar`, `agendar`, `consultar`, `seguimiento`, `avisos`, `reportes`, `autorizar`, `otro` |
| `usoOtro` | Detalle libre separado del uso |
| `estado` | `bien`, `fallas`, `no_funciona`, `sin_uso` |

Alternar opciones conserva detalles para recuperar una selección accidental. Sólo el detalle de la opción activa cuenta como respuesta. Vacíos, SIIv2 sin módulo y Otra/Otro sin detalle admiten autoguardado y cuentan pendientes en el avance del servidor. «Selecciona…» es indicativo deshabilitado.

Los valores históricos se muestran como tales sin convertirlos. Guardar comentarios u otros campos permite conservar respuestas anteriores; modificar una opción exige una clave vigente. Enviados/históricos bloqueados mantienen su lectura y permanecen inmutables.

## Réplica local y sincronización

Entidad/configuración EF `SiiModule`, tabla `public.sii_modulos`: PK textual `id_modulo`, `desc_modulo`, `presente`, `sincronizado_en`. `presente` expresa disponibilidad local observada, **no ACTIVO institucional**. Los registros retirados se conservan para referencias históricas.

`GET /formatos/{ur}/modulos-sii` revalida sesión, Nexo, contexto y alcance de lectura. Devuelve únicamente estado, ID y descripción de módulos disponibles en PostgreSQL, sin filtro de UR. Nunca abre SQL Server. El selector muestra sólo la descripción y distingue internamente los registros por ID; ordena en español e informa carga/error/pendiente sin bloquear el resto del formulario.

El backend comprueba selecciones nuevas/cambiadas contra módulos disponibles y mantiene un bloqueo compartido del módulo hasta confirmar la transacción del formato. Obtiene la descripción del catálogo; no confía en la del navegador. Una referencia guardada conserva su descripción después de una baja o cambio de nombre. Cambiar comentarios no refresca esa instantánea. Volver desde otra herramienta a SIIv2 valida disponibilidad de nuevo.

El coordinador existente incorpora `sii_modulos` como etapa separada de `sii` (UR), seguida por ILDA cuando se incluye. Manual y automática incluyen ambos catálogos SII, con ILDA independiente. Programación pausada, cola, exclusión, recuperación y comprobación de propietario/caducidad antes del commit se conservan. Publicación, metadatos, resultado y auditoría son transaccionales por catálogo. Una etapa fallida impide anunciar éxito completo. Nunca se actualizan formatos, respuestas, versiones o instantáneas desde la sincronización.

Consulta fija sobre **DesarrolloSII**, reutilizando exclusivamente `ConnectionStrings:Sii`:

```sql
SELECT [ID_MODULO],[DESC_MODULO] FROM [sii].[MODULOS_SII];
```

No consulta otras columnas ni aplica TOP 1000, ejercicio, ACTIVO, UR o responsable. La selección de ejercicio de UR (`Synchronization:SiiLatestExercise`) permanece igual. La cuenta institucional necesita **SELECT sobre `sii.MODULOS_SII`**, además del permiso de UR existente. No se concedieron permisos ni se modificaron credenciales.

Las excepciones de conexión/lectura no devuelven listas parciales. Se validan ambas columnas, IDs numéricos textuales positivos de hasta 40 caracteres y descripciones no vacías de hasta 4000 caracteres. IDs iguales con descripciones incompatibles rechazan la etapa; IDs distintos con igual descripción se conservan. Duplicados idénticos se consolidan por ID.

Se mantienen protecciones conservadoras: máximo configurado de 100000 filas (rechazo, no truncamiento), descarga vacía y reducción superior al 20% rechazadas. Se conserva la última copia y se requiere revisar el origen; nunca se usan esos resultados para vaciarlo. Las desapariciones aceptadas se marcan no disponibles sin borrar referencias. Auditoría y fallos contienen mensajes sanitizados/contadores, no volcados del origen.

## Migración y operación posterior

El ajuste de entrega/foco/elegibilidad/diagnóstico no requiere otra migración y no modifica las existentes. `42P01` significa que PostgreSQL no resuelve la relación solicitada; no demuestra un fallo de SQL Server. EF crea `public.sii_modulos` y el modelo usa `public`; las consultas especializadas ahora indican explícitamente ese esquema. `SiiCatalogSchema` distingue migración ausente del historial, historial aplicado sin tabla, tabla en otro esquema e historial inaccesible para la cuenta de ejecución. La pantalla administrativa muestra el diagnóstico y un contador no disponible, nunca cero como lectura correcta. La etapa de módulos falla antes de abrir su fuente si falta el esquema local; los resultados conservan su estado parcial/fallido.

En PostgreSQL sintético se reproduce el error al detener EF antes de `SiiModulesCatalog` y desaparece al aplicar la migración normal. Esto no identifica la base utilizada por una instalación existente: una conexión a otra base aún sin migrar produce el mismo síntoma. El operador debe comprobar el destino previsto de `ConnectionStrings:Tdv2`, historial y esquema en esa misma base. No se consultó ninguna base institucional, no se creó el catálogo manualmente y no se añadió DDL al arranque.

Nueva migración **`20261009080533_SiiModulesCatalog`**. Up sólo añade la réplica; los detalles nuevos utilizan el JSON existente. No se editaron migraciones anteriores. Down rechaza retirar el catálogo con módulos, referencias o formatos enviados. No se ejecuta DDL al arrancar.

Tras comprobar el destino previsto de User Secrets, el operador puede aplicar desde la raíz:

```powershell
dotnet tool restore
dotnet ef migrations list --project tdv2 --startup-project tdv2 -- --environment Development
dotnet ef database update 20261009080533_SiiModulesCatalog --project tdv2 --startup-project tdv2 -- --environment Development
```

EF también aplica migraciones anteriores pendientes; revisar su alcance. Esta entrega no aplicó migraciones institucionales. `.artifacts/SiiModulesCatalog.sql` es evidencia de generación, no un segundo migrador.

En **Configuración → Sincronizaciones**, **Sincronizar SII** encola UR y módulos; **Sincronizar SII e ILDA ahora** añade ILDA. Ambos contadores SII y sus resultados separados aparecen en pantalla/historial. La programación conserva su activación previa.

Las solicitudes manuales se atienden desde el proceso web: usar los botones de Configuración → Sincronizaciones, sin otra terminal. No se activan horarios ni cambia ILDA al hacerlo. [Operación vigente y recuperación](SINCRONIZACIONES.md). Para programación automática o un ciclo externo explícito, se conservan estas alternativas desde la raíz y bajo la configuración autorizada:

```powershell
dotnet run --project tdv2 --no-launch-profile -- --environment Development --sync-once
dotnet run --project tdv2 --no-launch-profile -- --environment Development --sync-worker
```

`--sync-once` procesa un ciclo; `--sync-worker` continúa cada minuto. `--sync-check=sii` comprueba UR y módulos sin publicar. Estos comandos pueden consultar las fuentes: no se ejecutaron contra servicios institucionales.

## Verificación del ajuste de entrega, foco y diagnóstico

Pasaron 76/76 pruebas .NET de dominio/HTTP en memoria, 78/78 frontend, 51/51 de edición en PostgreSQL nativo, 35/35 de sincronización nativa y 57/57 comprobaciones EF. Las migraciones anteriores conservan sus cálculos históricos; se actualizan las expectativas de esas pruebas, sin editar el SQL aplicado. Pasaron 11/11 recorridos con dos identidades sintéticas distintas en contextos de Edge separados, ASP.NET, PostgreSQL y SignalR reales. Se incluyen carrera por la misma fila, filas diferentes, nombre/foto/color, salida y liberación sin recarga, primera tecla/pegado/Tab, elegibilidad remota, medios independiente, menús/ayudas, eliminación, renovación tardía, desconexión/reconexión/caducidad, móvil, envío y rechazos directos del backend. Una pestaña de la misma cuenta se verifica como caso adicional.

Los 10 recorridos de interfaz de sistemas con transporte simulado se identifican por separado y no acreditan persistencia. No hay validación institucional: Microsoft es un par HTTP sintético, Nexo se publica mediante vistas/funciones sintéticas y las fuentes SII/ILDA son lectores ADO.NET de prueba. [Resultados nativos de edición](entrega-sistemas-20261009/edicion-postgresql.json), [EF](entrega-sistemas-20261009/migraciones-postgresql.json), [sincronización](entrega-sistemas-20261009/sincronizaciones-postgresql.json), [dos usuarios en navegador](entrega-sistemas-20261009/dos-usuarios-postgresql.json) y [UI simulada](entrega-sistemas-20261009/navegador-transporte-simulado.json).

## Verificado en la entrega anterior de módulos

Repetición posterior al ajuste de entrega: **7/7 recorridos nativos de módulos** y **13/13 de Sincronizaciones**, ambos con ASP.NET/PostgreSQL y cero errores JavaScript. El caso adicional de Sincronizaciones abre la pantalla con la tabla EF temporalmente ausente, comprueba el error administrativo y «No disponible», restaura el nombre en el fixture y confirma la recuperación. [Módulos](entrega-sistemas-20261009/modulos-navegador-postgresql.json), [Sincronizaciones](entrega-sistemas-20261009/sincronizaciones-navegador.json).

- TypeScript, Vite y solución Release correctos; .NET sin advertencias. EF confirma modelo/snapshot coherentes y genera la migración sin conectar.
- **74/74 .NET**: dominio/transporte en memoria y lector ADO.NET sintético. Incluye 1501 módulos, repetición de lectura, duplicados, interrupción, conexión fallida, IDs inválidos (incluido booleano), conexión Sii compartida sin abrirla, borradores, detalles, varias herramientas, estados, históricos, códigos/orígenes y autorización del endpoint.
- **78/78 frontend**: regresiones de reservas/autoguardado, versiones, pendientes, eliminaciones, navegación y fotografías.
- **34/34 sincronizaciones en PostgreSQL 18.6 desechable**, incluidos los cuatro casos nuevos de módulos, 1501 registros, IDs estables, fallos/rollback y ambos modos con/sin ILDA. Corrida `native-postgres-20261009-084200-2373f124`.
- **52/52 comprobaciones EF/PostgreSQL**: nueve migraciones, nueva réplica sobre ocho anteriores, repetición, conservación de datos, rollback y verificación de esquema previo/actual. Corrida `native-postgres-20261009-084557-d4d2a18e`; [evidencia](sistemas-herramientas-20261009/migraciones-postgresql.json).
- **49 escenarios de edición PostgreSQL comprobados**: 48/49 en `native-postgres-20261009-085429-706ad183`, más repetición dirigida 2/2 en `native-postgres-20261009-085927-33d92b32`. Incluye el caso histórico corregido y protección del código contra vacío/null/renombre. [Reporte agregado que conserva ambos resultados](sistemas-herramientas-20261009/edicion-postgresql.json). Los fixtures antiguos se adaptaron a reservas, destinatarios y primera etapa vigentes, sin cambiar sus restricciones de producción.
- **7/7 Edge → ASP.NET → PostgreSQL**, cero errores JavaScript: catálogo pendiente/borrador, búsqueda y persistencia, renombre/baja histórica, Otra/Otro, dos sesiones/SignalR/reservas/autoguardado, varias filas del mismo procedimiento y móvil/teclado/enviado. Corrida `native-postgres-20261009-084018-c939ad44`. [Reporte](sistemas-herramientas-20261009/navegador-postgresql.json). Fuentes remotas sintéticas, motor real detenido al terminar.
- **12/12 Edge de Sincronizaciones → ASP.NET/PostgreSQL**, cero errores JavaScript: resultados separados, fallos parciales, programación con/sin ILDA, revocación, recuperación y escritorio/móvil. Corrida `native-postgres-20261009-084856-06da90d2`; [reporte](sistemas-herramientas-20261009/sincronizaciones-navegador.json).
- **10/10 recorridos Edge con React real y transporte simulado**, cero errores JavaScript: búsqueda/persistencia simulada, carga/error/pendiente, detalles, iconos, X/Cerrar/Escape/foco, dos sesiones, filas independientes, Código/orígenes, históricos/enviados, teclado y anchos 1920/1366/768/360. Capturas inspeccionadas, scroll horizontal contenido. Este nivel **no acredita ASP.NET/PostgreSQL**; tipografías externas bloqueadas y móvil emulado.

## Pendiente

SII real: SELECT, base DesarrolloSII, autenticación/TLS/TDS, tipos efectivos de ID/descripción, volumen/rendimiento y protecciones de reducción. Aplicación institucional de EF y aceptación en dispositivos físicos. No hubo acceso institucional ni cambios en Nexo/Herd/credenciales, push o despliegue.

## Comandos sintéticos

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:NuGetAudit=false
Push-Location ClientApp
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
node tests/browser/system-tools-ui.mjs
Pop-Location
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -SyncOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
# Repetición dirigida de los dos escenarios de regresión de códigos/referencias:
$env:TDV2_TEST_CASE_PREFIX='Edición/regresión:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -MigrationsOnly
# Recorrido con React, ASP.NET y PostgreSQL real, todas las fuentes sintéticas:
$env:TDV2_TEST_BROWSER_FLOW='systems'
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
# Recorrido de Configuración → Sincronizaciones con el mismo entorno sintético:
$env:TDV2_TEST_BROWSER_FLOW='sync'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

Node se tomó de `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs`, añadido sólo al PATH del proceso. No se instaló ni modificó software de Herd.

Los intentos iniciales de `pg_ctl` fallaron por el relanzamiento con token restringido (87/3). El parámetro opcional `-DirectStart` inicia `postgres.exe` con el mismo usuario y restricciones, únicamente para el clúster recién creado y verificado dentro de `.artifacts`, en loopback/puerto aleatorio; conserva el apagado por `pg_ctl`. No utiliza bases existentes ni eleva permisos.

`TDV2_TEST_EPHEMERAL_TLS` habilita exclusivamente en el ejecutable de pruebas un puente HTTPS Node → Kestrel HTTP en loopback. El certificado y su clave son efímeros, se transmiten por stdin y nunca se instalan, confían ni escriben en el almacén de Windows. Esto permite probar cookies seguras y SignalR cuando no hay certificado de desarrollo disponible para Schannel. El sitio productivo no contiene el puente ni sus controles de fixture.
