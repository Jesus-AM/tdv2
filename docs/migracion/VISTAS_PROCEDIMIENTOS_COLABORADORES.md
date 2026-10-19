# Procedimientos, Colaboradores y fuente interna — 2026-10-16

## Ajuste posterior exclusivamente de textos

Se conserva la restauración visual descrita abajo. En Colaboradores se eliminan los dos pies sobre vigencia y asignaciones comprobables, y la ayuda se sustituye por «Busca una persona por nombre o correo para agregarla como colaboradora.». Los avisos de Administrador ya no mencionan niveles numéricos. Se conserva el aviso específico de vista de prueba, sin alterar su restricción. El servicio sólo cambia tres literales; reglas, contratos, disposición y estilos intactos. [Detalle y revisión de permisos](COLABORADORES_VISIBILIDAD_ACCIONES.md#ajuste-de-textos-vigente--2026-10-16).

## Restauración visual vigente

Solicitud posterior: recuperar ambas vistas anteriores al último ajuste, con **una sola excepción: Origen y sus textos no se muestran en Colaboradores**. Roboto permanece en el sistema; el portal y el quiz conservan Inter. La presentación descrita en las secciones históricas de abajo queda sustituida por esta restauración.

### Qué se restauró

Se revisaron instrucciones, estado local, archivos nuevos, historial y diff. `Inicio.tsx`, `AreaDirectory.tsx` y `Colaboradores.tsx` estaban sin cambios respecto a `a4be53f` antes del ajuste de vistas; sus modificaciones pendientes correspondían a esa entrega. Se guardó el diff específico en `.artifacts/views-revert-20261016/visual-only.patch`, se comprobó y se aplicó inversamente por hunks. No se usó reset ni se sustituyeron indiscriminadamente archivos. Después se retiró sólo la presentación de procedencia de Colaboradores y el CSS exclusivo del ajuste deshecho.

- **Procedimientos:** nombre del área arriba; clave debajo con cantidades o estado, tanto en Mis áreas como en Todas las áreas y la vista personal. Vuelven las etiquetas, pesos, tamaños, espaciados, bordes, progresos y botones anteriores: Expandir visibles, Contraer, Ver formato y Continuar llenado donde correspondía. Se retiran la nueva línea de alcance del resumen y los nuevos chips de estado. Cifras, formatos únicos, estados, URL, permisos, filtros y memoria de vista no cambian. La presentación vuelve a `displayUnitCode`, incluido su tratamiento previo de ceros iniciales; el valor almacenado no cambia.
- **Colaboradores:** área autorizante y buscador en la fila inicial; resultados bajo «1. Selecciona una persona» y alcance/alta bajo «2. Define el alcance». Vuelven ayudas, vacíos, radios y lista flexible anteriores, sin las nuevas columnas ni tarjetas separadas. Las etiquetas del alta son «Colaborador local» / «Colaborador de áreas dependientes»; las de la lista, «Local» / «Áreas dependientes». Todas las asignaciones y acciones mantienen sus claves y no se agrupan por persona.
- **Única excepción en esa vista:** eliminados Origen, delegación en TDV2, Agregado por Administrador y la explicación de procedencia del retiro. No se añade sustituto. Permanecen Solo consulta, estados de retiro, avisos funcionales y confirmación. `origen`, otorgante y demás datos internos siguen en contratos, servicios y auditoría.
- **Conservado:** Roboto local y sus respaldos/carga; portal, quiz, Inter, imágenes, jerarquía/alineación UACJ; tablas de captura y reversión de delimitadores; catálogos, agrupación, reservas, autoguardado, avance/envío y eliminación de Medios. Ningún cambio de backend, contratos, datos o migraciones.

`Inicio` y `AreaDirectory` quedan sin diff respecto a la referencia Git. En `Colaboradores` sólo quedan los dos hunks que retiran procedencia. `app.css` conserva las dos adiciones previas para Roboto y herencia en controles nativos. Los handlers de búsqueda/alta/retiro, claves de roles, matriz N2/N3 y validaciones del servidor no se tocaron.

La auditoría de hashes compara 753 archivos del estado inicial: sólo cambian los 14 de presentación, pruebas y documentación indicados; 739 permanecen iguales, incluidos 211 archivos protegidos de backend, contratos, captura, portal, quiz y fuentes. `git diff --check` no encuentra errores.

### Verificación nueva de la restauración

- TypeScript y Vite correctos; **94/94 pruebas frontend**.
- **116/116 pruebas de dominio/HTTP**, ejecutadas con el binario Release existente de los servicios sin cambios. ASP.NET/TestServer real y dobles en memoria; incluye visibilidad, alcance, ambos roles, restricciones N2/N3 y rechazo de operaciones no autorizadas. No se recompiló el backend en esta revisión porque no cambió.
- **9/9 grupos** de `directory-collaborators-ui.mjs`: ubicación original de clave/nombre, estados, cifras únicas, ambas pestañas, búsqueda/filtros/paginación, expansión/contracción y apertura; alta de ambos tipos con payload exacto, error conservando selección, N2/N3, consulta, cancelación/Escape, retiro exacto, pendiente y reintento. Una expectativa inicial confundía filas contraídas con filas desmontadas: se corrigió para comprobar visibilidad y `aria-expanded`, sin cambiar la aplicación.
- **3 grupos** de `operational-layout.mjs` y **4/4** de `collaboration-actions-ui.mjs`: distribución anterior, navegación, actualización, asignaciones separadas, ausencia de origen en textos/atributos/tooltips, acciones permitidas y regresiones de Guardar/Enviar.
- Edge/React/MUI reales, con HTTP/identidades/altas/retiros simulados. Ventanas 1920/1440/1366/768/390/320 px según suite, móvil/tacto emulados, teclado/foco y zoom CSS 200 %. Sin desbordamiento horizontal de página; nombres de filas completos. Se conserva también la elipsis original del selector cerrado de área autorizante en pantallas estrechas, como parte de la apariencia solicitada.
- Roboto web local realmente renderizada en títulos, nombres, etiquetas, menú, confirmación y español en 400/500/600/700 (`CSS.getPlatformFontsForNode`, `isCustomFont=true`). No se vuelven a presentar pruebas anteriores de Inter/quiz como nuevas ejecuciones.

Evidencia nueva en **`.artifacts/views-revert-20261016/`**: `before/` y `after/` con las mismas ventanas; `report.json` con fuentes y resultados; `operational/after/`, `collaboration-actions/`, `types.txt`, `build.txt`, `frontend.txt`, `domain-http.txt`, `baseline-hashes.json` y `scope-audit.json`. Se revisaron visualmente capturas de escritorio y móvil. Las capturas de `.artifacts/views-20261016/` permanecen como evidencia del ajuste anterior.

Límites: sin PostgreSQL ni Nexo reales, dispositivos físicos, otros motores o lector de pantalla. Recursos externos bloqueados en el navegador; las fuentes y recursos locales necesarios sí cargaron. No se repitieron los recorridos del portal/quiz ni se creó un paquete de publicación: sus fuentes y recursos quedan idénticos al comienzo de la restauración. No hay pendientes de implementación de esta solicitud. Sin push, despliegue o migraciones; no se presume aplicada `20261016190000_RemoveUsedMedia`.

### Archivos y reproducción de esta restauración

Presentación: `ClientApp/resources/js/pages/Inicio.tsx`, `Components/AreaDirectory.tsx`, `pages/Colaboradores.tsx`, `ClientApp/resources/css/app.css`. Pruebas: `directory-collaborators-ui.mjs`, `operational-layout.mjs`, `collaboration-actions-ui.mjs`. Documentación: README, ESTADO_MIGRACION, INVENTARIO, AJUSTES_VISUALES_Y_PALETA_PUBLICA, DISTRIBUCION_PROCESOS, COLABORADORES_VISIBILIDAD_ACCIONES y este documento. Los restantes archivos pendientes corresponden a trabajo previo conservado.

Desde la raíz, con Node y Edge disponibles:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
$env:TDV2_VISUAL_ARTIFACTS='.artifacts/views-revert-20261016/operational'
node ClientApp/tests/browser/operational-layout.mjs after
Push-Location ClientApp
node tests/browser/directory-collaborators-ui.mjs after
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/views-revert-20261016/collaboration-actions'
node tests/browser/collaboration-actions-ui.mjs
Pop-Location
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
```

`before` captura el bundle que se esté sirviendo; no restaura código ni reconstruye una versión anterior. Para conservar estas evidencias al repetir, indicar otra carpeta en `TDV2_VIEWS_ARTIFACTS` / `TDV2_VISUAL_ARTIFACTS`. Las instrucciones y resultados siguientes documentan la entrega anterior; sólo su configuración de fuentes y reglas funcionales siguen vigentes.

## Historial: ajuste anterior de vistas (presentación sustituida)

## Estado recuperado y alcance

Se revisaron AGENTS, README, ESTADO, INVENTARIO, los documentos visuales/de colaboradores, el diff y el código real. El punto de partida es `a4be53f` más la integración local del quiz e Inter sin confirmar en Git. La ejecución interrumpida no había modificado Colaboradores: no había una implementación parcial que deshacer. Se conservan esos cambios previos; no se restauraron archivos completos.

Esta entrega modifica presentación y pruebas. No cambia servicios, contratos, identidad, roles, alcance, datos ni migraciones. El portal Razor, su jerarquía, alineación/logo UACJ, colores y recursos del quiz se conservan byte por byte respecto al inicio de esta entrega. La reversión de delimitadores de Edición/Acciones sigue vigente; no cambian sus estilos ni la agrupación de la captura.

## Fuentes efectivas

El historial de `theme.ts` confirma **`Roboto, sans-serif`** antes de Inter. No había un cargador ni archivos Roboto en el proyecto: dependía del sistema. La comprobación de renderizado en Edge encontró Roboto para 400/700 pero respaldo Arial para 500/600. Se recupera la familia y su respaldo exactos, añadiendo archivos locales para asegurar también los pesos intermedios.

| Espacio | Configuración vigente | Carga |
|---|---|---|
| Sistema interno, MUI y controles nativos | Roboto, sans-serif | `wwwroot/fonts/roboto/roboto.css`, TTF variable normal/cursiva 100–900, swap y precarga de la normal |
| Portal público | Inter con sus respaldos existentes | WOFF2 locales de Inter, sin cambios |
| Quiz Microsoft 365 | Inter y pesos originales, incluidos 800/900 | WOFF2 locales de Inter, sin cambios |

Los Roboto provienen de la [distribución de Google Fonts](https://github.com/google/fonts/tree/main/ofl/roboto), conservando los binarios originales y OFL 1.1. [Procedencia y hashes Git](../../tdv2/wwwroot/fonts/roboto/README.md). Sólo el shell interno carga Roboto; no se comparte ese CSS con Razor/iframe. No se instala una fuente en Windows ni se usa CDN durante la ejecución. Los logos siguen siendo imágenes intactas.

Se comprobaron fuentes **realmente renderizadas** mediante `CSS.getPlatformFontsForNode`, con `isCustomFont=true`, para títulos, nombres, etiquetas, menú, confirmación, grupo del catálogo y caracteres españoles. Roboto muestra Regular/Medium/SemiBold/Bold en 400/500/600/700; Inter continúa en portada y quiz.

## Procedimientos institucionales

- `CVE_UR - DESC_UR` forma la línea principal de grupos y filas, incluidas Mis áreas, Todas las áreas y la vista personal. Se usa directamente `cve_ur`, conservando ceros iniciales; no se utiliza `id_ur` ni el auxiliar que elimina ceros en otras vistas. El auxiliar, la búsqueda tolerante a ceros y el catálogo no se modifican.
- Nombres completos a 17 px/600, detalles a 14 px/400, ajuste a varias líneas. Cantidad de dependientes y completos permanece debajo; se conservan Tu área, Formato del área principal y Áreas dependientes. No se convierten mayúsculas ni siglas.
- Estados existentes con etiquetas discretas: Enviado depende de `enviado_en`; Captura completa sigue correspondiendo a 100 % sin envío. Sin iniciar y En captura conservan sus condiciones. Las etiquetas de permiso siguen separadas.
- El resumen se obtiene de `uniqueForms(formatos)` y el promedio original, sin nuevas fórmulas ni filtros. `FormService.Index` entrega los ámbitos del perfil; `Profile.InstitutionalRead` y `FormAccess` acreditan cuándo son institucionales. Se indica **Resumen institucional** para consulta institucional/administrador, incluso estando en Mis áreas. En perfiles de alcance restringido se indica **Resumen de tus formatos disponibles**. Cambiar pestaña no cambia esas cifras.
- Buscador y Mostrar equilibrados; Expandir visibles conserva su alcance sobre la página visible y Contraer todos vacía el conjunto completo de expansión. Ambos tienen iconos secundarios. Borde normal uniforme, estado expandido discreto y foco de teclado de 2 px independiente.
- Acción repetida **Abrir formato** con borde, misma URL y permisos. Porcentajes/barras permanecen alineados. Se conservan filtros, paginación, memoria de vista, pestañas y actualización.

## Colaboradores: presentación y reglas conservadas

Se inspeccionaron `CollaboratorService`, `CollaboratorAccess`, `CollaboratorsController`, `CollaborationRules`, `CollaborationListing`, `Access`, integración Nexo y contratos TS. El contrato de la lista entrega `tipo`, no un nombre de rol arbitrario. La interfaz muestra los nombres indicados por el usuario sobre el mapeo existente:

| Clave Nexo | Tipo del contrato | Nombre en esta vista | Alta delegada existente |
|---|---|---|---|
| `colaborador_local` | `local` | Colaborador Local | N2 y N3 autorizados; formato resuelto por `LocalCollaborationUnit` |
| `colaborador_dependencias` | `dependencias` | Colaborador dependencias | Sólo N2 autorizado; raíz otorgante y su rama |

No se cambia la selección existente de roles habilitados ni se añaden opciones. La disponibilidad deriva de `tipos` y el nivel del área; el servidor vuelve a comprobar delegación, responsabilidad, rol y jerarquía. La búsqueda sigue limitada por la raíz autorizada, con debounce, paginación, cancelación de respuestas obsoletas y mensajes existentes. La explicación del alcance utiliza `selected.formato` o el área autorizante, según el tipo real.

N2 ve asignaciones de su rama. N3 ve las locales de su área; dependencias no se propaga a N3. El retiro requiere que el área otorgante pertenezca a las raíces administrables y permite escritura; ver una asignación no concede retiro. Las asignaciones centrales conservan sólo consulta local. Se mantienen coincidencias de identidad/concesión/adscripción y los controles de representación. [Matriz y contrato detallados](COLABORADORES_VISIBILIDAD_ACCIONES.md#visibilidad-por-asignación).

El alta sigue en la misma página: área que autoriza arriba (nombre completo también en móvil), buscador/resultados juntos a la izquierda y resumen/rol/alcance/botón a la derecha. Inicio compacto, sin paneles vacíos altos. En móvil se apila en orden de uso. La selección visible compara email **y** adscripción, como ya hacía el estado del control.

Asignaciones en cuatro columnas alineadas: Persona, Rol, Área asignada, Acciones. Nombre 16 px/600, correo/detalles 14 px, roles completos y nombres largos sin elipsis. En móvil son tarjetas con listas de descripción accesibles. Cada fila conserva `c.key`, ID local, alcance y acción propios; no se agrupa por email ni se descartan asignaciones repetidas de una persona. Se conservan confirmación, consulta, revocada, retiro pendiente y reintento.

Se retiran únicamente las presentaciones de procedencia: Origen, delegación en TDV2, Agregado por Administrador y el texto de procedencia del retiro. `origen`, `ur_otorgante` y demás datos siguen intactos en contratos/backend para autorización y auditoría. Los avisos funcionales de delegación no habilitada, datos insuficientes y vigencia permanecen.

## Verificación realizada

- TypeScript, Vite y solución .NET Release correctos. **94/94 frontend** y **116/116 dominio/HTTP** con ASP.NET/TestServer y dobles en memoria; incluyen diez casos de visibilidad y cuatro HTTP de Colaboradores, reglas centrales/delegadas y rechazos. Cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18, sin nuevas dependencias npm/NuGet.
- **9/9 grupos nuevos** de `directory-collaborators-ui.mjs`: ambas pestañas, grupos/filas, ceros iniciales, conteo único, estados, búsqueda, filtros, paginación, navegación, dos roles/N2/N3, alta local/dependencias, error sin perder selección, retiro exacto/cancelación/Escape/pendiente/reintento, consulta, nombres completos, menú, diálogo y fuentes. Pantallas 1440/768/390/320 px, teclado y zoom CSS 200 %. Transporte de listado/búsqueda/alta/retiro simulado, sin persistencia real.
- **3 grupos** de regresión del directorio existente a 1920/1366/768/390/320 px; **4/4** de colaboradores/Guardar/Enviar, **24/24** de tablas y **9/9** de selectores. Incluyen presencia, bloqueos, ambos bordes fijos según el comportamiento previo, menús, confirmaciones, asociaciones, autoguardado con foco/nodo/desplazamiento y cambio remoto. React/MUI/Edge reales; HTTP/SignalR/datos de prueba simulados.
- **9/9** grupos del quiz, seis partidas de diez preguntas y los cuatro resultados, apertura, ayuda, reinicio, Escape desde iframe/padre, retorno de foco/scroll. Inter web local en portal/quiz y Roboto web local en la captura. **15/15 recursos** servidos anónimamente por ASP.NET real/TestServer, incluidos Roboto, Inter, licencias y quiz; MIME, contenido, DENY/nosniff correctos.
- Capturas antes/después con las mismas ventanas; inspección visual, fuentes por CDP, ausencia de desbordamiento y recorte en área autorizante, contraste de resultados seleccionados. No se reconstruyó el paquete de publicación en esta revisión; el SDK incluye los nuevos archivos de `wwwroot/fonts/roboto` mediante sus reglas existentes.

Incidencias corregidas: al restaurar sólo la declaración tipográfica se detectó el respaldo Arial en 500/600; se resolvió con Roboto local. MUI forzaba nowrap en el área seleccionada móvil; se corrigió únicamente ese control. La prueba antigua de portada todavía exigía las URLs de Space Grotesk/IBM Plex anteriores al quiz: ahora verifica Inter local, quiz y separación respecto a Roboto. Los auxiliares nuevos ajustaron una expectativa de orden de catálogo y esperaron a que React montara las etiquetas antes de medir CDP. Sólo los pases finales se cuentan como aprobados.

## Evidencia y límites

Todo en `.artifacts/views-20261016/`, conservando los artefactos anteriores:

- `before/` y `after/`: capturas de ambas vistas, grupos abiertos/cerrados y selección/inicio; `report.json` contiene resultados y fuentes. `after/procedimientos-nombres-completos-*`, `*-zoom-200` y `colaboradores-consulta-390.png` complementan la revisión.
- `operational/after/`, `collaboration-actions/`, `tables/internal-report.json`, `selectors/record-select/report.json`: regresiones y capturas internas.
- `public-quiz/`: capturas públicas/quiz/captura, peticiones y fuentes; `http-assets.json`, `domain-http.txt`, `frontend.txt`, `types.txt`, `build.txt`, `dotnet-build.txt` y `scope-audit.json`.

Sin PostgreSQL, Nexo real, cuentas institucionales, dispositivos físicos, Safari/Firefox, lector de pantalla ni zoom nativo en esta revisión. Altas/retiros UI prueban payload y presentación; no acreditan transacciones PostgreSQL/Nexo. El portal del navegador usa Razor exportado desde el host de pruebas, servido en loopback. Unsplash se bloqueó deliberadamente; su fotografía no se compara visualmente, aunque archivo/URL/tratamiento de portada están intactos. Las fuentes se sirvieron localmente y sí cargaron.

No queda implementación pendiente de esta solicitud. La aceptación institucional, otros motores/dispositivos y publicación del operador siguen pendientes externos. Sin push, despliegue ni cambios de base. `20261016190000_RemoveUsedMedia` permanece intacta y no se presume aplicada.

## Reproducción

Desde la raíz, con Node/npm, .NET 10 y Edge en PATH. No iniciar perfiles conectados a bases existentes.

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets .artifacts/views-20261016/http-assets.json
Push-Location ClientApp
node tests/browser/directory-collaborators-ui.mjs after
Pop-Location
```

En Ubuntu, `npm` sustituye `npm.cmd`; los comandos dotnet son iguales. El script de navegador usa el canal `msedge`: requiere Edge instalado y no fue ejecutado en Ubuntu. Antes de una nueva captura de referencia, usar el bundle anterior y `before`; el modo no recupera Git automáticamente. Para el quiz, exportar `public-after.html` y usar los comandos de [su documentación](QUIZ_MICROSOFT_365_E_INTER.md#reproducción-y-publicación), con otro directorio de evidencia.

Actualizar posteriormente mediante el paquete completo del procedimiento de publicación existente, incluyendo **ambas carpetas de fuentes**. No se necesita SQL ni EF para estos ajustes. No sustituir ni ejecutar migraciones como parte de esta entrega.

## Archivos de esta entrega

Producción: `ClientApp/index.html`, `resources/css/app.css`, `resources/js/theme.ts`, `resources/js/Components/AreaDirectory.tsx`, `resources/js/pages/Inicio.tsx`, `resources/js/pages/Colaboradores.tsx` y `tdv2/wwwroot/fonts/roboto/`.

Pruebas: nuevo `ClientApp/tests/browser/directory-collaborators-ui.mjs`; adaptaciones en `system-ui-fixture.mjs`, `operational-layout.mjs`, `collaboration-actions-ui.mjs`, `quiz-inter-ui.mjs`, `tests/TDV2.Verification/EntryPoint.cs` y `PublicAssetsTests.cs`. Documentación: README, ESTADO, INVENTARIO, PUBLICACION, documentación visual, del quiz, de distribución y de colaboradores, más este documento. Los otros archivos pendientes de Git pertenecen al trabajo previo conservado.
