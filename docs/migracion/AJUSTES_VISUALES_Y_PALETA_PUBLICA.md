# Presencia, catálogo, Acciones y colores públicos — 2026-10-16

## Revisión vigente del portal — 2026-10-17

**Ajuste posterior: enlaces sin subrayado.** En `Index.cshtml` se retiran los subrayados de navegación, accesos rápidos, correo y documentos; la regla común cubre normal, visitado, hover y foco. Se conservan iconos/flechas, destinos y geometría. El resaltado `#CFD6E8` y texto azul distinguen hover/foco, incluido ahora el correo; el contorno de teclado permanece azul sobre claro y blanco sobre Contacto. No cambian estilos internos ni recursos del quiz.

Verificación nueva en `.artifacts/public-links-20261017/`: build Release de aplicación/verificador correcto (dos advertencias previas NU1902/NU1903); suite pública existente antes/después correcta en 1366/768/390/320 px, 316 elementos sin cambios de contenido/geometría/tipografía, y 16 recursos HTTP/TestServer correctos. Inspección adicional en Edge de 19 enlaces por ventana (1366/390/320), diez controles visibles en normal/hover/foco: sin subrayados, foco visible, resaltado y tamaño/posición estables; clic y Enter en anclas/documentos, comprobación posterior a la visita. La privacidad del navegador impide usar CSS calculado para demostrar el historial `:visited`; ese estado comparte expresamente la regla sin decoración. Activación real de `mailto:` interceptada sólo en la comprobación para no abrir un cliente de correo. Documentos conservan sus destinos `#` existentes. Capturas `before/after-asideside-*.png`, `contact-focus-*.png` y `link-observations.json`. Móvil emulado, sin bases ni OAuth; fotografía externa inaccesible como antes. No se repitió el recorrido del juego ni TypeScript: no cambian sus fuentes. Sin push, despliegue o migraciones.

Se mejora únicamente el color, el acabado y la llegada a las anclas del portal público sobre los cambios locales existentes. **Contenido, orden, geometría y tipografía conservados**: todo el Razor después de `</style>` es idéntico al inicio de esta revisión, incluidos textos, preguntas/respuestas, nombres de botones, enlaces, SVG, imágenes y ramas de sesión. Inter, tamaños/pesos, jerarquía UACJ–Dirección–proyecto y compensación horizontal del logo permanecen. No cambian vistas internas, autenticación, colaboradores, captura ni el interior del quiz. Las decisiones anteriores sobre conservar exactamente el velo/sombras/hover quedan sustituidas sólo por el alcance de esta revisión.

### Manual y colores

Se abrió la versión actual de `C:\Users\Jesus Arenas\Documents\colores.pdf` mediante Windows PDF y se renderizaron sus seis páginas. Revisadas la paleta institucional (141), combinaciones (143) y complementarios (144–145). SHA-256: `1D99B0E50EDD3EAA3707121CF084BCF546A187017E90EB3E4C29A9BF94876E04`. Se usan los HEX escritos, con **#213455** como azul; no se reproduce la errata de la muestra 2026 de la página 144. No se utiliza amarillo.

| Color | Uso vigente |
|---|---|
| `#213455` | Velo y respaldo del hero, centro del modelo, etiquetas de dimensiones, recuadros de los seis iconos, línea de las ocho fases, textos, bordes/foco y botones. Contacto, diagnóstico y pie conservan este azul. |
| `#FFFFFF` | Navegación, El proyecto, cuerpos de tarjetas/respuestas; textos e iconos sobre azul. |
| `#DADADA` | Separadores y superficies neutras existentes; degradado del modelo reducido a una capa al 18 % sobre blanco. |
| `#CFD6E8` | Único complementario: encabezados de preguntas abiertas y resaltado de enlaces/botones al interactuar. Distingue estados sin convertirse en fondo general de secciones. |

Se retira `#174187` del hover público: las interacciones utilizan azul institucional, blanco y el resaltado claro. No se añaden otros tonos. Las transparencias conservan el color primario como base, sin modificar su saturación.

### Ajustes aplicados

- **Hero:** fotografía/URL, posición y tamaño originales; velo de un solo azul con opacidades 96/92/90 % en las paradas existentes y respaldo azul si la foto no carga. Textos blancos opacos. No se modifican píxeles de imágenes.
- **Modelo:** fondo casi blanco, sombras moderadas, cuerpos blancos con borde gris discreto, etiquetas azul/blanco y bloque central azul. Se conservan dimensiones y disposición. Se elimina la elevación de hover en tarjetas informativas.
- **Ejes y ruta:** seis recuadros azul/blanco con SVG originales; tarjetas blancas sin elevación ni apariencia de botón. La línea usa azul institucional con sus mismos 2 px; círculos y ocho fases conservan tratamiento uniforme, sin estados inventados.
- **Accesos y documentos:** fondo suave al interactuar y foco visible, sin subrayados conforme al ajuste posterior; se corrige el selector no coincidente `.panel .qa` sin activar las reglas de padding, display o tipografía que nunca se aplicaban. Contacto conserva blanco sobre azul en reposo y azul sobre resaltado al interactuar. Botones reconocibles y foco visible; se retiran desplazamientos de hover del botón y su flecha para evitar movimiento. No cambian dimensiones ni destinos.
- **Preguntas:** encabezado abierto `#CFD6E8`, texto/indicador azul, cuerpo blanco. Foco interior para que no lo corte el contenedor. Se conserva `<details>`, incluido poder abrir varias preguntas.
- **Anclas:** `public-navigation.js` mide la altura real de `.topbar` y la actualiza con `ResizeObserver`. CSS aplica `scroll-margin-top` con esa altura + 18 px a las secciones y paneles con ID. No intercepta clics, no cambia el historial, no ejecuta scroll programático ni mueve secciones. Se conserva el desplazamiento suave nativo y movimiento reducido. Fallback inicial de 67 px; la medición cubre encabezados móviles de varias filas y la variante autenticada.

### Verificado ahora

- TypeScript, sintaxis del nuevo JS y compilación .NET Release correctos. Persisten cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18. No se necesita recompilar Vite para CSS de Razor y el nuevo archivo estático; no cambió el código productivo React/MUI.
- `public-colors-ui.mjs before/after --surface-finish`: **316 elementos por ventana** en 1366×900, 768×1024, 390×844 y 320×800. Geometría, familia/tamaño/peso, espaciado, radios, contenido y enlaces idénticos; sólo se exceptúan las transiciones de hover expresamente retiradas. Sin desbordamiento horizontal. Inter web local confirmada por CDP.
- **172 registros de comprobación**, incluidos **83 llegadas por anclas** (menú visible, accesos, enlaces directos, atrás/adelante), cuatro cabeceras con sesión sintética, estados hover/activo/foco y fuente real. Ejes queda visible; encabezado anónimo medido de 67 px en escritorio a 110.61 px en 320 px. Se comprueba también el desplazamiento suave normal, además de movimiento reducido.
- Contraste conservador de textos a partir de colores calculados y transparencias: mínimo **8.55:1**, hero **9.22:1** incluso suponiendo fotografía blanca detrás. Etiquetas azul/blanco **12.44:1**; los trazos principales de los seis iconos usan la misma combinación. Se conservan los detalles decorativos de opacidad de los SVG originales.
- Preguntas abiertas/cerradas, varias abiertas, teclado/foco, estados sin cambios de geometría, menú, accesos y correo; modelo, ejes, ruta, diagnóstico, participación, beneficios, preguntas y lateral capturados en las mismas ventanas/zonas antes y después.
- **9/9 grupos del quiz** existentes: seis partidas completas de diez preguntas, cuatro resultados, ayuda, reinicio, cierre/Escape dentro/fuera del iframe y restitución de foco/desplazamiento. JS/CSS originales del juego verificados íntegros; Inter del quiz y Roboto interno realmente cargadas. El estilo público no entra en el iframe.
- **16/16 recursos públicos** servidos anónimamente por ASP.NET/TestServer real, incluido el nuevo JS, con `DENY`/`nosniff` conservados. También se exportó la rama autenticada de Razor con identidad de prueba: se mantienen Ir a mi espacio, POST de cierre y navegación. No se ejecuta OAuth institucional.

Incidencias del verificador corregidas: el fixture antiguo sólo servía assets/images/fonts y devolvía HTML por el JS público; ahora sirve también css/js/quiz locales. La comprobación de foco compara distribución, permitiendo el scroll nativo para llegar al control. Al cambiar de HTML anónimo a autenticado se fuerza documento nuevo, pues navegar al mismo hash no vuelve a solicitar HTML. No se modificó la aplicación para sortear estas expectativas.

### Evidencia y límites

Nueva carpeta **`.artifacts/public-finish-20261017/`**, sin sobrescribir evidencia anterior:

- `public-{before,after}-{1366,768,390,320}.png` y `*-layout.json`: vista completa y geometría.
- `{before,after}-{hero,que-es,modelo,ejes,ruta,diagnostico,participa,beneficios,asideside,preguntas}-{ancho}.png`: mismas zonas y ventanas. `*-faq-multiple-*` documenta varias preguntas abiertas.
- `after-anchor-ejes-*`, `after-authenticated-ejes-*`: llegada de anclas con/sin sesión sintética.
- `public-{before,after}-report.json`, `public-contrast-*.json`, `quiz-inter-after-report.json`, capturas del quiz, `http-assets.json`, `build.txt`, `types.txt`, `scope-audit.json` y `manual-1.png`…`manual-6.png`.

**Limitación real:** la fotografía de `images.unsplash.com` no pudo descargarse, tanto en Edge como en la solicitud HTTP directa. Los dos pases comparten esa indisponibilidad; no se sustituyó ni se recoloreó el recurso. Logos y fuentes locales sí cargaron. Queda pendiente confirmar el acabado con la foto externa disponible; el contraste del velo se verificó contra extremos blanco/negro. No se afirma una comparación visual con la fotografía cargada.

Edge real, tamaños/tacto emulados y pruebas en loopback. Razor se exporta desde ASP.NET/TestServer; las pruebas internas del quiz usan transporte simulado. Sin PostgreSQL/Nexo reales, dispositivos físicos, otros motores, lector de pantalla ni nueva prueba OAuth. Los enlaces de documentos conservan sus destinos `#` preexistentes, sin inventar documentos. No hay cambios de contratos, datos, migraciones, seguridad o permisos. Sin push, publicación ni despliegue. `RemoveUsedMedia` intacta; no se presume aplicada.

### Archivos y reproducción

Producción: `tdv2/Views/Home/Index.cshtml` y nuevo `tdv2/wwwroot/js/public-navigation.js`. Pruebas: `ClientApp/tests/browser/public-colors-ui.mjs`, `system-ui-fixture.mjs` y `tests/TDV2.Verification/PublicAssetsTests.cs`. Documentación: README, ESTADO_MIGRACION, INVENTARIO y este documento. Los restantes cambios pendientes de Git son previos y se conservan.

Desde la raíz, con .NET 10, Node y Edge disponibles:

```powershell
npm.cmd --prefix ClientApp run types:check
node --check tdv2/wwwroot/js/public-navigation.js
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/public-finish-20261017/public-after.html
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets .artifacts/public-finish-20261017/http-assets.json
Push-Location ClientApp
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/public-finish-20261017'
node tests/browser/public-colors-ui.mjs after --surface-finish
node tests/browser/quiz-inter-ui.mjs after
Pop-Location
```

El pase comparativo exige la referencia `before` exportada/capturada antes de editar; no restaura archivos. Para repetir sin sobrescribir esta evidencia, usar otra carpeta en `TDV2_VISUAL_ARTIFACTS`. En Ubuntu usar `npm` y `export TDV2_VISUAL_ARTIFACTS=...`; los demás comandos son equivalentes, pero esta ejecución fue Windows/Edge. El JS nuevo se incluye mediante el contenido estático `wwwroot` del paquete habitual; no requiere migración ni ejecución contra una base. Las evidencias y decisiones que siguen corresponden a entregas anteriores.

## Actualización vigente: vistas internas y Roboto

Por solicitud posterior, Procedimientos y Colaboradores recuperan su apariencia anterior al último ajuste: clave secundaria bajo el nombre, distribución, etiquetas, controles y espaciados previos. La única excepción visual es retirar Origen y sus textos en Colaboradores. Se conserva **Roboto, sans-serif** local dentro del sistema. El portal y el quiz mantienen **Inter**, colores, jerarquía, logo y recursos intactos. No cambian Edición/Acciones ni la agrupación de captura. [Restauración selectiva, fuentes, reglas y evidencia nueva](VISTAS_PROCEDIMIENTOS_COLABORADORES.md#restauración-visual-vigente).

## Entrega anterior: Inter local y reto Microsoft 365

En esa entrega se incorporó a portal, MUI y quiz **Inter 4.1 local**, variable 100–900, con licencia OFL, precarga y `font-display: swap`. Sustituye las familias descritas en las evidencias anteriores; los tamaños, colores, jerarquía y archivos de imágenes permanecen. La portada añade el botón del reto dentro de Diagnóstico y su diálogo/iframe público, con juego original sin recolorear. La reversión de delimitadores y los controles internos se conservan. Fuentes realmente renderizadas, antes/después, recorrido de diez preguntas, políticas, publicación y regresiones comprobados en aislamiento. [Estado, archivos, pruebas, capturas y límites](QUIZ_MICROSOFT_365_E_INTER.md).

## Actualización vigente: reversión de la delimitación y alineación UACJ conservada

Por solicitud posterior se retiran sólo los últimos separadores interiores, celdas laterales vacías de los grupos y adaptación móvil de 68 px. Vuelven la apariencia y distribución anteriores, con las columnas fijas y la excepción previa de Edición hasta 600 px. Se conserva el botón de agregar dentro del grupo. La portada permanece intacta: jerarquía descrita abajo y desplazamiento horizontal -3.272 % del logotipo para compensar sus 96 px transparentes originales. Evidencia nueva interna en `.artifacts/columns-revert-20261016/`; la comparación pública de `.artifacts/fixed-columns-logo-20261016/` corresponde a la entrega anterior. [Alcance, pruebas y límites vigentes](COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente).

## Revisión vigente: jerarquía institucional del bloque principal

El nuevo alcance autoriza **tamaños, pesos y espacios sólo dentro de `.hero`**, sustituyendo allí la restricción anterior de cambiar únicamente colores. Se parte de la paleta primaria y las mejoras internas ya implementadas, sin restaurar archivos ni rehacerlas. No cambian los colores, el fondo/fotografía/velo, textos, familia tipográfica, HTML, navegación ni archivos de imágenes. Todo el marcado posterior a `</style>` es idéntico al inicio de este ajuste.

- **Institución:** el conjunto original de UACJ pasa de 440 a **560 px**; conserva `max-width:100%`, `height:auto`, proporciones y composición completa. A 390/320 px de pantalla mide 342/272 px, respectivamente.
- **Dependencia:** Dirección General inmediatamente debajo, separación de **14 px**, tamaño adaptable de **16–18 px** y peso **600**. Su menor tamaño/peso la vincula a la institución sin competir con ella.
- **Proyecto:** se conserva el único `h1`, con **38 px** en escritorio y mínimo **24 px** en móvil, peso 600. Hay **36 px** entre la Dirección y el proyecto. Se mantienen debajo el lema y párrafo con sus tamaños originales. Los cuatro textos continúan en blanco opaco.
- Padding vertical de 104 a **96 px** en escritorio y de 56 a **48 px** hasta 960 px. Compensa el logotipo mayor sin añadir elementos, decoraciones ni altura innecesaria.

### Verificación y evidencia de este ajuste

Build Release correcto, sin errores y con las cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18. HTML exportado desde el Razor real mediante ASP.NET de pruebas con dobles en memoria; después servido a Edge 154 con assets locales. No se inicia el host operativo ni se conecta una base.

`public-colors-ui.mjs before/after --hero-hierarchy` pasó en seis tamaños. Compara **306 elementos**, incluidos **297 exteriores**: éstos conservan tipografía, dimensiones, espacios, formas, contenido, enlaces y animaciones; sólo admite su desplazamiento vertical por la altura del bloque. Dentro verifica proporciones/carga del logotipo original, h1 único, jerarquía, alineación, separación, fondo idéntico y ausencia de recortes/desbordamientos. Conserva pruebas de paleta, blanco/opacidad, contraste mínimo **8.05:1**, navegación por teclado, preguntas y 84 entradas de estados. Se inspeccionaron las capturas renderizadas de escritorio y móvil emulado.

| Pantalla | Ancho UACJ | Tamaño h1 | Altura anterior → actual del bloque |
|---|---:|---:|---:|
| 1920 × 1080 | 560 px | 38 px | 636.13 → 626.30 px |
| 1366 × 900 | 560 px | 38 px | 634.73 → 626.30 px |
| 1024 × 768 | 560 px | 38 px | 615.48 → 620.11 px |
| 768 × 1024 | 560 px | 29.18 px | 497.63 → 503.94 px |
| 390 × 844 | 342 px | 24 px | 566.53 → 501.41 px |
| 320 × 800 | 272 px | 24 px | 602.94 → 537.81 px |

Evidencia nueva en **`.artifacts/hero-hierarchy-20261016/`**, sin sobrescribir las revisiones anteriores: `hero-{before,after}-{ancho}.png` (bloque), `public-{before,after}-{ancho}-viewport.png` (mismo encuadre por par), capturas de página completa, HTML exportados, layouts e informes JSON, contrastes, salida de build y `baseline/Index.cshtml`/`baseline/hashes.json`. Las capturas del bloque tienen alturas diferentes por el ajuste autorizado; las del viewport conservan dimensiones iguales. El manifiesto inicial permite comprobar que las modificaciones pendientes ajenas a esta tarea, incluidos controles internos y `RemoveUsedMedia`, se conservan.

**Límites:** Google Fonts y Unsplash volvieron a resultar inaccesibles; ambos pases usan las mismas fuentes de respaldo y la misma ausencia de fotografía, conservando URLs y tratamiento originales. Los logotipos locales sí cargaron. Falta revisar con esos recursos cargados y dispositivos físicos. A 1024 px se detectó un desbordamiento previo de **13 px fuera del bloque**, en la columna lateral; permanece igual, sin corregirlo por la instrucción de conservar el resto de la portada. No hay desbordamiento del bloque ni desbordamiento de página a 320/390 px. No se reejecutaron suites internas, TypeScript/Vite ni dominio/HTTP de la revisión anterior: sus archivos no cambiaron. No se prueba PostgreSQL ni se ejecutan migraciones; las cifras de pruebas anteriores que siguen abajo no son ejecuciones nuevas.

**Archivos modificados en esta revisión:** `tdv2/Views/Home/Index.cshtml` (único archivo productivo), `ClientApp/tests/browser/public-colors-ui.mjs` (modo de comparación del bloque), README, ESTADO, INVENTARIO y este documento. Sin contratos, datos, cambios internos de tablas/presencia/catálogo/Acciones, push ni despliegue. `20261016190000_RemoveUsedMedia` permanece intacta, sin asumir aplicación.

### Repetición del pase posterior

Desde la raíz, con las dependencias y Edge disponibles; utiliza la referencia `before` capturada **antes** de modificar la vista. Nunca regenerarla desde el estado posterior. El modo sin `--hero-hierarchy` conserva la comparación estricta de toda la geometría para ajustes futuros que sean sólo de color.

```powershell
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/hero-hierarchy-20261016'
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/hero-hierarchy-20261016/public-after.html
Push-Location ClientApp
node tests/browser/public-colors-ui.mjs after --hero-hierarchy
Pop-Location
```

Equivalente Ubuntu, **no ejecutado aquí**: `export TDV2_VISUAL_ARTIFACTS="$PWD/.artifacts/hero-hierarchy-20261016"`, mismos dos comandos `dotnet`, y `(cd ClientApp && node tests/browser/public-colors-ui.mjs after --hero-hierarchy)`. En este Windows se mantienen `DOTNET_CLI_HOME`, `NUGET_PACKAGES` y Node de Visual Studio descritos más abajo. No hay paso de frontend ni base de datos para este cambio exclusivo de CSS en Razor.

## Revisión anterior: paleta primaria y textos blancos

Se revisó nuevamente el árbol local, incluido lo no registrado en Git. Presencia, catálogo y Acciones ya cumplían el alcance y **se conservaron sin cambios en producción**; se repitieron sus recorridos. El pendiente era la portada: los complementarios se habían extendido a textos/fondos y el lema/párrafo principal usaban `#CFD6E8`.

La corrección utiliza **#213455, #DADADA y #FFFFFF** como base. Los cuatro textos del bloque principal son blancos y opacos; navegación y El proyecto permanecen blancos. Sólo se conserva **#174187 en hover**, mediante `--azul-interactivo`, para diferenciar enlaces sobre blanco/gris y botones oscuros sin cambiar sus dimensiones ni perder contraste: blanco o gris como color del texto de esos enlaces no cumpliría 4.5:1. No se utiliza en títulos, párrafos, fondos generales ni estado normal. Los demás complementarios se retiraron de los estilos de interfaz; los archivos de imágenes/logotipos y su contenido permanecen intactos.

En esta revisión sólo cambia **`tdv2/Views/Home/Index.cshtml`** en producción. Los scripts de pruebas `public-colors-ui.mjs`, `visual-tables-ui.mjs` y `record-select-ui.mjs` admiten un directorio de evidencia separado; el primero añade comprobaciones de blanco/opacidad, paleta y capturas del viewport. README, ESTADO, INVENTARIO y este documento describen el estado actualizado. No se reimplementaron los ajustes internos ni se tocaron contratos, datos o migraciones.

## Estado y alcance

Implementado sobre el árbol local pendiente, sin restaurar archivos desde Git. Se leyeron AGENTS, README, ESTADO, INVENTARIO y los documentos de catálogo, tablas, edición e interfaz. La base encontrada ya incluía herramientas opcionales, catálogo agrupado, familias Microsoft/Adobe, duplicados por procedimiento, agrupación, listas locales y retirada de Medios. Esas funcionalidades y sus pruebas previas se conservan.

**Esta entrega modifica sólo presentación en producción.** No cambia contratos, reglas de negocio, almacenamiento, sincronizadores, permisos ni protocolo de reservas. No se ejecutaron migraciones, PostgreSQL, conexiones institucionales, push ni despliegue. `20261016190000_RemoveUsedMedia` se conserva byte por byte; su aplicación institucional sigue sin confirmarse. Los requisitos operativos de esa entrega previa no se sustituyen por esta actualización visual.

## Presentación interna

- Ocho colores funcionales independientes de la portada: `#713C91`, `#006A73`, `#A03D75`, `#765448`, `#51575D`, `#574198`, `#007C91`, `#815472`. Se reutiliza el índice enviado por el servidor: éste ya conserva el color por participante y evita índices ocupados por otros mientras dispone de alguno de los ocho. No se incorporan identidades ni datos personales nuevos. La paleta no utiliza rojo, ámbar, verde de éxito ni el azul de encabezados.
- Indicador lateral de 4 px, avatar y texto; tinte opaco del 4 %, sin contorno intenso de toda la fila. Se conserva **Estás editando**; la reserva ajena muestra **Nombre está editando** también en la tabla, además del nombre accesible del avatar. Otra pestaña se identifica textualmente. Se aplica mediante los componentes de presencia compartidos.
- Encabezados de herramientas a peso **700**, `#20314d` sobre `#f1f3f6`, sin opacidad. En Edge se verificó la fuente efectivamente utilizada mediante CDP: **Roboto-Bold**, instalada localmente, y opciones a peso 400. Grupos `role=group` con nombre accesible; sus encabezados no son opciones ni reciben selección.
- El selector principal conserva su etiqueta en el encabezado y nombre accesible. El problema actual no era una etiqueta flotante: el resumen y el input de búsqueda podían ocupar la misma celda de la cuadrícula al abrir. Ahora la búsqueda ocupa una segunda línea, como en los multiselectores. Sin reducción tipográfica ni cambios de apertura/reservas.
- Acciones usa `position:sticky; right:0` en encabezado y celdas, dentro del contenedor existente. Fondo opaco idéntico al estado de la fila, separación izquierda, capas 2/4 y menús MUI en su portal superior. Ancho 112 px en Identificación/Sistemas; 176 px en las tablas genéricas con presencia dentro de Acciones. Los botones no se duplican y conservan área de 44 × 44 px, etiquetas, ayudas, confirmación y restricciones.
- Se recupera la presentación original: los grupos usan `colSpan` completo y Edición se libera horizontalmente en contenedores hasta 600 px; Acciones permanece fija. Fuera de esa excepción, Edición sigue fija a la izquierda. No se añade otra barra ni se captura la rueda; fuentes y encabezados verticales se conservan. [Reversión vigente](COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente).

## Portada: exclusivamente colores en la revisión anterior

Se volvió a abrir el archivo local actual `Documents/colores.pdf`: ahora contiene **seis páginas**, renderizadas y revisadas visualmente, incluida la paleta primaria de la página impresa **141**, aplicaciones, combinaciones y complementarios (142–145). SHA-256 del PDF revisado: `1D99B0E50EDD3EAA3707121CF084BCF546A187017E90EB3E4C29A9BF94876E04`. Se usaron sus HEX, con **#213455** para el azul conforme a las páginas 141/145 y a la corrección expresa de la rotulación de la página 144. No se utiliza amarillo, tampoco en el aviso público de error.

Los tokens de la vista Razor centralizan los tres colores principales y el único complementario para hover. Fondos oscuros, títulos, textos e iconos normales utilizan azul institucional/blanco; separadores y superficies neutras, gris/blanco. Los degradados conservan ángulos y paradas; sus colores usan la paleta principal. Las sombras conservan sus geometrías y transparencias. El velo de la fotografía se conserva tal como estaba al iniciar esta revisión. No se añaden efectos ni elementos.

El aspecto tenue del lema y el párrafo provenía del **color**, no de una opacidad heredada. El navegador confirma `rgb(255, 255, 255)`, opacidad efectiva 1 y ausencia de filtros/mezclas en Dirección General, título, lema y párrafo. La navegación ahora tiene fondo blanco opaco; El proyecto hereda el blanco del documento sin agregar una superficie nueva.

**Se mantienen distribución, orden, contenido, fuentes declaradas, tamaños, pesos, márgenes, dimensiones, imágenes, archivos de logotipos, navegación, animaciones y comportamiento responsive.** La navegación sigue clara y el pie conserva su superficie oscura original. Los estilos están en la vista pública; el tema MUI no cambia por esta paleta.

## Verificación de la revisión anterior de colores

| Comprobación | Resultado y alcance |
|---|---|
| TypeScript y Vite | Correctos; sin dependencias nuevas |
| Build .NET Release | Correcto, 0 errores; cuatro advertencias NuGet ya presentes, NU1902/NU1903 de `System.Security.Cryptography.Pkcs` 9.0.18 (dos proyectos) |
| Frontend | 94/94, incluyendo catálogo, duplicados, agrupación, reservas y autoguardado |
| Dominio/HTTP ASP.NET | 116/116 con dobles en memoria; sin PostgreSQL |
| Nuevos recorridos Edge internos | 24/24: 1366, 390, 320 px y zoom CSS 200 %. Dos actores sintéticos en contextos distintos, propia/ajena/libre, menú sobre Acciones, familias, guardado/recarga, bloqueo de consulta, confirmación/cancelación, eliminación conservando asociaciones, 4 y 35 herramientas, 3 y 33 procedimientos, desplazamiento y alturas distintas; Datos/Acuerdos con presencia y confirmación |
| Regresión de selectores Edge | 9/9: primera interacción con ratón/teclado/toque emulado, reserva lenta/denegada/error, cambio de fila e intenciones canceladas |
| Portada antes/después | 1366×900, 390×844, 320×800; **306 elementos por tamaño**, con geometría, tipografía calculada, márgenes, formas, texto y referencias de imágenes/enlaces idénticos |
| Contrastes calculados en navegador | Encabezados del catálogo 11.75:1. Portada: 153 textos visibles en escritorio y 147 en móvil, mínimo conservador **8.05:1**; todas las vistas cumplen 4.5:1 normal/3:1 grande. Se componen transparencias y extremos de gradientes, considerando blanco/negro tras la imagen. Presencia propia/ajena verificada sobre su fondo real |

Se comprobaron también hover, pulsación y foco por teclado en botones, navegación, contacto, documentos y preguntas; 42 entradas de estado, incluidas las anotaciones de que no hay controles públicos deshabilitados. La apertura de preguntas conserva su estado visible. El HTML sólo contiene los tres HEX principales y el complementario de hover; ningún texto normal usa ese complementario. El contenido después de `</style>` es idéntico al original de esta revisión: no cambió el marcado, los textos, las referencias a imágenes ni los enlaces.

La portada se exportó desde el **Razor real de ASP.NET**, usando `Application.Client(false)` del proyecto de verificación. Edge recibe ese HTML y los assets locales en un servidor de pruebas; no se levantó el host operativo ni su trabajador de sincronización. La captura interna usa React/MUI reales y transporte HTTP/SignalR, persistencia y asignación de identidades simulados. **Ninguna de estas pruebas nuevas acredita PostgreSQL real ni concurrencia transaccional institucional**; la evidencia nativa anterior sigue en el documento del catálogo.

Se inspeccionaron capturas de escritorio, móvil y zoom. La prueba de autoguardado conserva el mismo nodo enfocado y las coordenadas de desplazamiento; el último campo desplazable se comprueba frente al borde de Acciones. Los menús se prueban mediante las capas y el elemento realmente visible sobre la intersección con la columna fija.

### Evidencia local

Evidencia **nueva**, sin sobrescribir la entrega anterior: `.artifacts/visual-primary-20261016/`:

- `manual-{1,2,3,4,5,6}.png`: versión actual del manual, sólo evidencia local.
- `public-before-{1366,390,320}.png`, `public-after-{1366,390,320}.png` y respectivos `*-layout.json`: comparación con mismas dimensiones.
- `public-{before,after}-{1366,390,320}-viewport.png`: encuadre inicial para revisar los textos blancos, con idénticas dimensiones por par.
- `public-before-report.json`, `public-after-report.json`, `public-contrast-{1366,390,320}.json`.
- `catalog-*`, `presence-*`, `identification-*`, `many-rows-*.png`: captura interna; sufijos `1366-1x`, `390-1x`, `320-1x`, `1366-2x`.
- `internal-report.json`, `internal.txt`, `record-select.txt`, `types.txt`, `vite.txt`, `frontend.txt`, `build.txt`, `backend.txt`.
- `record-select/`: informe y capturas de las nueve regresiones de apertura/reserva.
- `baseline/Index.cshtml`: vista exacta anterior a esta revisión. No constituye un respaldo de bases.

La evidencia de la primera entrega se conserva en `.artifacts/visual-refresh/`, incluida su copia de la migración. Hash de `RemoveUsedMedia`, sin cambios: `FEB7B9E04500E6ED52BB3368D36303B025DB2A105C3B375BEE6BE2BDA546FB5C`. No se infiere aplicación en una base por ese hash ni por existir el archivo.

### Límites e incidencias resueltas

- La red del entorno denegó Google Fonts y la fotografía de Unsplash. Los dos pases de la portada comparten exactamente esa indisponibilidad; sí cargan los logotipos locales. Las familias CSS y URLs originales se mantienen. Queda por comprobar la portada con esos recursos externos cargados en un entorno autorizado. No se sustituye la fotografía ni se modifican archivos de logotipos.
- El zoom es emulación mediante CSS `zoom:2`, conservada también tras recargar; no acredita zoom nativo de todos los navegadores. Pendientes dispositivos físicos, otros motores y lector de pantalla.
- Incidencias históricas de la primera entrega: foco programático después del ratón corregido a entrada por teclado; contraste 4.30:1 del rótulo corregido entonces a `#174187`; regla móvil sobrescrita corregida en el orden CSS. La revisión actual sustituye ese color normal del rótulo por el azul primario y mantiene aprobada la matriz. Esas ejecuciones intermedias anteriores no se cuentan como aprobadas ni como pruebas nuevas.
- No se reejecutaron las suites nativas que crean/migran PostgreSQL, por el alcance visual expreso. No se declara que `RemoveUsedMedia` esté aplicada en una base existente.

## Archivos de la primera entrega conservados

Producción: `ClientApp/resources/css/app.css`; `lib/participant-colors.ts`; componentes `BlockEditingStatus.tsx`, `ToolSelect.tsx`, `CaptureTableScroll.tsx`; `pages/FormatoUR.tsx`; `tdv2/Views/Home/Index.cshtml`.

Pruebas: `tests/TDV2.Verification/EntryPoint.cs` añade únicamente el exportador anónimo de HTML de pruebas; `system-ui-fixture.mjs` permite dos nombres/índices sintéticos; nuevos `visual-tables-ui.mjs`, `public-colors-ui.mjs`, `rendered-contrast.mjs`. Ninguna ruta o identidad de pruebas se agrega a producción.

Documentación: este archivo, README, ESTADO, INVENTARIO, DESPLAZAMIENTO_TABLAS, EDICION_AUTOMATICA y CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS. Los demás cambios pendientes de Git pertenecen al trabajo previo conservado.

Los archivos modificados específicamente en la revisión de paleta primaria se enumeran al inicio; los componentes internos anteriores no se volvieron a editar.

## Comandos de la revisión anterior de colores

Estos comandos y su evidencia corresponden al estado **anterior al ajuste de jerarquía**. El pase público estricto requiere la vista anterior; para la vista vigente utilizar los comandos y la referencia `hero-hierarchy-20261016` indicados al inicio de este documento. No sobrescribir las referencias visuales anteriores con la nueva jerarquía.

Desde la raíz, con Node/.NET disponibles y dependencias restauradas:

```powershell
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/visual-primary-20261016'
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/visual-primary-20261016/public-after.html
Push-Location ClientApp
node tests/browser/visual-tables-ui.mjs
node tests/browser/record-select-ui.mjs
node tests/browser/public-colors-ui.mjs after
Pop-Location
```

En este Windows se usaron `DOTNET_CLI_HOME=.artifacts/dotnet-home`, `NUGET_PACKAGES=.artifacts/nuget` y Node de Visual Studio en PATH. Se conservaron las advertencias NuGet, sin modificar paquetes en un trabajo visual.

Equivalente Ubuntu (no ejecutado aquí; navegador Edge compatible instalado para estos scripts):

```bash
export TDV2_VISUAL_ARTIFACTS="$PWD/.artifacts/visual-primary-20261016"
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/visual-primary-20261016/public-after.html
(cd ClientApp && node tests/browser/visual-tables-ui.mjs && node tests/browser/record-select-ui.mjs && node tests/browser/public-colors-ui.mjs after)
```

El pase público `after` requiere los artefactos `before` conservados de esta sesión. Para otra comparación, exportar/capturar `before` **antes** de modificar la vista, usando el mismo comando con `public-before.html` y argumento `before`; no regenerar la referencia con la vista posterior.

Orden de construcción: frontend → paquete ASP.NET que contiene Razor y assets → verificaciones. Una futura publicación autorizada debe actualizar ambos juntos. **No hay paso de base de datos para estos ajustes y no se ejecuta EF.** La instalación pendiente del catálogo/retirada de Medios debe evaluarse como operación independiente con sus propios respaldos e historial. La próxima sesión puede partir de este estado sin repetir la implementación; restan únicamente las comprobaciones externas/físicas y operativas descritas.
