# Columnas fijas, encabezados de grupo y alineación UACJ — 2026-10-16

## Reversión parcial vigente

Por solicitud posterior se revierte **únicamente la delimitación visual de Edición y Acciones**. Se compararon los archivos vigentes con las copias locales anteriores de `.artifacts/fixed-columns-logo-20261016/baseline/`, comprobando que sus diferencias correspondían sólo a ese ajuste. No se restauró desde HEAD ni se descartó el trabajo pendiente de Git.

- `ClientApp/resources/css/app.css`, `ClientApp/resources/js/Components/CaptureTableScroll.tsx` y `ClientApp/resources/js/pages/FormatoUR.tsx` vuelven exactamente a esas copias anteriores. Recuperan fondos, sombras suaves previas, espaciados y anchos de Edición/Acciones de 124/112 px. Se retiran los separadores interiores de 1 px y los controles/columnas de 68 px introducidos en la última entrega.
- Edición conserva la fijación izquierda y Acciones la derecha, con el comportamiento responsive anterior: en contenedores de hasta 600 px Edición se desplaza horizontalmente para dejar espacio a los campos; Acciones permanece fija. No se modifica el contenedor de scroll ni los mecanismos de captura.
- Los grupos recuperan una sola celda con `colSpan` completo, franja de ancho completo, nombre/código y **Agregar sistema o herramienta**. Se retiran las celdas laterales vacías. El alta sigue siendo del procedimiento; eliminar sigue siendo una acción de cada registro. No cambian asociaciones, permisos, reservas, claves ni la función de alta/eliminación.
- Se conserva sin modificaciones `tdv2/Views/Home/Index.cshtml`, incluida la jerarquía institucional y `translateX(-3.272%)` del logotipo. Tampoco cambian archivos de imágenes, catálogo, datos, contratos ni migraciones. `RemoveUsedMedia` no se ejecuta ni se presume aplicada.
- `visual-tables-ui.mjs` recupera su posicionamiento anterior de campos en móvil. La suite nueva `fixed-columns-ui.mjs`, que exigía la apariencia ahora descartada, se retira; su fuente se conserva en `.artifacts/columns-revert-20261016/baseline/` y sus informes anteriores siguen siendo evidencia histórica.

**Verificado nuevamente:** TypeScript y compilación Vite correctos; **94/94 pruebas frontend**, **24/24 comprobaciones de tablas** y **9/9 de selectores**. Navegador Edge real con React/MUI y transporte HTTP/SignalR, identidades, reservas y persistencia simulados. Escritorio 1366 px, móvil emulado 390/320 px y zoom CSS 200 %: desplazamiento horizontal/vertical, Acciones fija y alineada, fondos opacos, menús por encima de Acciones, dos participantes, pocas/muchas filas, permisos, confirmar/cancelar eliminación, respuestas conservadas al eliminar el procedimiento, familias/recarga y autoguardado sin perder nodo/foco/scroll. Incluye Identificación, Sistemas, Datos y Acuerdos. Capturas de escritorio y móvil revisadas.

Evidencia nueva **`.artifacts/columns-revert-20261016/`**: `types.txt`, `vite.txt`, `frontend.txt`, `internal.txt`, `record-select.txt`, `regression/internal-report.json`, `regression/presence-*.png`, `regression/catalog-*.png`, `regression/identification-*.png`, `regression/many-rows-*.png` y `record-select/record-select/report.json`. La comparación de fuentes y hashes queda en `baseline/hashes.json` y `scope-audit.json`. Las capturas de la entrega anterior se conservan en `.artifacts/fixed-columns-logo-20261016/`; no se sobrescriben ni se presentan como verificaciones nuevas.

**Límites:** no se ejecutó backend, PostgreSQL ni migraciones en esta reversión; las comprobaciones no prueban persistencia real ni dispositivos físicos. Los recursos externos se bloquean en las fixtures internas. La portada no se volvió a capturar porque sus fuentes e imágenes no cambiaron; conserva los límites de verificación pública documentados abajo. No se hizo push ni despliegue. No queda trabajo pendiente dentro de esta reversión.

Repetir desde la raíz con Node y Edge disponibles, usando otro directorio para conservar esta evidencia:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/columns-revert-repeat'
Push-Location ClientApp
node tests/browser/visual-tables-ui.mjs
node tests/browser/record-select-ui.mjs
Pop-Location
```

Ubuntu: `npm` en lugar de `npm.cmd`, `export TDV2_VISUAL_ARTIFACTS="$PWD/.artifacts/columns-revert-repeat"` y los dos comandos `node` desde `ClientApp`, con Edge instalado. Este equivalente no se ejecutó. No hay actualización de backend ni paso de base de datos asociado a esta reversión visual.

## Entrega anterior: estado recuperado y alcance

**Registro histórico:** la delimitación interna descrita a continuación fue revertida. La alineación UACJ y su evidencia pública siguen vigentes. Estas pruebas corresponden a la entrega anterior, no a la revisión actual.

Se conservaron todos los cambios locales, registrados y nuevos, sin restauraciones desde Git. Ya estaban implementados catálogo, multiselectores, agrupación, duplicados, retirada de Medios, presencia diferenciada, Acciones fija y jerarquía UACJ–Dirección–proyecto. Faltaban los límites continuos de las columnas y separar el encabezado de grupo de sus laterales. La regla móvil anterior liberaba Edición; la solicitud actual la sustituye por **ambos lados fijos también en móvil**.

Sólo presentación y sus pruebas/documentación. No cambian contratos, permisos, protocolo de reservas, respuestas, identificadores, evaluadores, asociaciones, catálogo, orden JSON ni migraciones. No se ejecuta PostgreSQL, EF, push o despliegue. `20261016190000_RemoveUsedMedia` permanece intacta; su existencia no demuestra aplicación.

## Implementación anterior de columnas — revertida

- Separadores interiores de **1 px**, discretos y continuos: derecho de Edición e izquierdo de Acciones, incluidos encabezados, grupos y registros. Se dibujan mediante sombra interior para conservarlos al desplazar celdas de una tabla con bordes colapsados. Fondos opacos; las reservas conservan el fondo tenue y el indicador de participante de 4 px, avatar, nombre y estado. Acciones en Datos/Acuerdos recibe el mismo separador; no se agregan columnas nuevas.
- Cada grupo de Sistemas ahora contiene **tres celdas**: Edición vacía/fija, centro con `colSpan=5` y Acciones vacía/fija. Sólo el centro lleva el fondo de grupo. Conserva el h3 con código/nombre completos y el mismo botón de alta, permisos y asociación; los laterales vacíos no reciben foco ni acciones. No hay altas automáticas ni cambios en las claves de filas.
- El contenido central usa `position:sticky` con inicio después de Edición y ancho igual al contenedor menos ambas columnas. Nombre y botón se mantienen dentro de ese intervalo durante todo el recorrido horizontal. El título se alinea con la celda Procedimiento; el botón va a su derecha o debajo cuando falta espacio, con etiqueta completa y sin elipsis.
- En contenedores de hasta **600 px**, los laterales miden **68 px cada uno**, con botón de eliminar de 44 px completo. Avatar/estado se apilan; fuentes conservadas. Los campos centrales se ajustan al espacio restante para que el primero y el último puedan verse completos. Las flechas/limpieza de los Autocomplete pasan a una línea propia; búsqueda y selección conservan su comportamiento. Se adaptan también los mínimos de Prioridad y usuarios atendidos en Identificación para que no desborden la última celda ni desplacen Acciones.
- `CaptureTableScroll` mide anchos en unidades CSS sin reiniciar scroll; la altura del encabezado usa `offsetHeight`, evitando duplicar la escala al emular zoom. Se conservan la región de scroll existente, rueda/tacto, portales MUI, foco y reservas. En escritorio permanecen los anchos 124/112 px.

## Logotipo público

El PNG original mide **2934 × 597 px**. Su primer píxel con alfa mayor que cero está en **x=96** (x=97 para alfa ≥128); la diferencia no proviene del padding del contenedor. Se aplica exclusivamente `translateX(-3.272%)`, equivalente a `-96/2934` del ancho renderizado: unos **18.32 px** a 560 px, **11.19 px** a 342 px y **8.90 px** a 272 px.

El archivo, sus proporciones y composición permanecen iguales, sin recortar la marca. No se mueven los textos inferiores ni cambian tamaños, pesos, espaciados verticales, altura, colores, fotografía o tratamiento del fondo. La familia y el único h1 se conservan. El navegador obtiene el límite visible mediante el canal alfa, no sólo la caja del img; coincide con el inicio de los cuatro textos con error inferior a 1 px en los seis tamaños probados.

## Verificación de la entrega anterior

- TypeScript, Vite y build .NET Release correctos; **94/94 pruebas frontend**. Build mantiene cuatro advertencias previas NU1902/NU1903 de `System.Security.Cryptography.Pkcs` 9.0.18.
- `fixed-columns-ui.mjs`: **25/25 comprobaciones** en 1366, 768, 390, 320 px y 1366 px con zoom CSS 200 %. Capturas previas/posteriores, tres posiciones horizontales, límites continuos, fondos, celdas laterales vacías, nombres largos completos, botón dentro del centro, campos extremos, pocas/29 filas, autoguardado con mismo nodo/foco/scroll, asociación de alta en grupo vacío, cancelación/confirmación de eliminación, históricos sin asociación, permisos y selectores de Identificación.
- Regresión `visual-tables-ui.mjs`: **24/24**, informe final en `regression/internal-report.json`, con dos participantes, presencia, menús, multiselección, familias, guardar/recargar, eliminación/asociaciones, muchas filas, Datos y Acuerdos. `record-select-ui.mjs`: **9/9**, incluida primera interacción con ratón/teclado/toque emulado, reserva lenta/denegada/error y cancelación de intenciones tardías.
- Público `before/after --logo-alignment`: **seis tamaños**, 1920/1366/1024/768/390/320 px. Compara 306 elementos: sólo cambia la coordenada horizontal del img UACJ; el resto de geometría, tipografía, espacios, contenido, referencias, formas y animaciones es idéntico. Comprueba alfa/proporciones/marca completa, blanco/opacidad, paleta sin amarillo, contraste mínimo 8.05:1, teclado, preguntas y 84 entradas de estados.

Las capturas internas usan React/MUI/Edge reales con **HTTP/SignalR, identidad, reservas y persistencia simulados**. El público usa HTML exportado por **Razor real en ASP.NET de pruebas con dobles en memoria**, servido después al navegador. No se ejecutan pruebas de persistencia real ni suites nativas PostgreSQL. No se presentan las pruebas anteriores de backend como ejecuciones nuevas.

## Evidencia y límites

Directorio nuevo **`.artifacts/fixed-columns-logo-20261016/`**:

- `table-{before,after}-{ancho}-{zoom}x-{0,0.5,1}.png`: tabla con la misma fixture y distintas posiciones horizontales; `many-after-*` e `identification-after-*` amplían la evidencia.
- `hero-{before,after}-{ancho}.png`, `public-{before,after}-{ancho}-viewport.png`, páginas completas, layouts, reportes y contrastes JSON. Cada par público conserva las mismas dimensiones.
- `fixed-before-report.json`, `fixed-after-report.json`, `regression/`, `record-select/`, salidas de tipos/frontend/Vite/Release y `baseline/` con fuentes anteriores y hashes del árbol local. No es respaldo de datos.

Para la inspección de zoom se utilizaron además las capturas de página completa `regression/presence-1366-2x.png` y `regression/many-rows-1366-2x.png`: los recortes automáticos del contenedor alto al 200 % pueden incluir la barra global superpuesta por el encuadre de Playwright. La comprobación geométrica de columnas se conserva por separado en los informes.

Google Fonts y Unsplash resultaron inaccesibles en ambos pases públicos. Sí cargaron los logotipos locales. La comparación usa los mismos recursos disponibles, conserva las URLs y no sustituye imágenes. Pendiente revisión con externos cargados, dispositivos físicos, zoom nativo, otros motores y lector de pantalla. Continúa el desbordamiento previo de 13 px de la columna lateral pública a 1024 px, ajeno al bloque principal y conservado por alcance; el logotipo no genera desbordamientos.

La inspección móvil detectó inicialmente compresión del texto junto a los iconos y mínimos de 240 px en Identificación que alteraban el límite derecho. Se corrigieron en presentación; las ejecuciones intermedias fallidas no se cuentan como pruebas aprobadas. Las nuevas capturas documentan la corrección.

## Archivos de la entrega anterior y reproducción de sus regresiones conservadas

Producción: `ClientApp/resources/css/app.css`, `ClientApp/resources/js/Components/CaptureTableScroll.tsx`, `ClientApp/resources/js/pages/FormatoUR.tsx`, `tdv2/Views/Home/Index.cshtml`.

Pruebas de aquella entrega: `ClientApp/tests/browser/fixed-columns-ui.mjs` (retirada en la reversión); modos de comparación del alfa en `public-colors-ui.mjs` (conservados) y posicionamiento del campo según el ancho real de Edición en `visual-tables-ui.mjs` (restaurado a la versión anterior). Documentación: README, ESTADO, INVENTARIO, AJUSTES_VISUALES_Y_PALETA_PUBLICA, DESPLAZAMIENTO_TABLAS, SISTEMAS_HERRAMIENTAS y este documento.

Desde la raíz, con dependencias restauradas, Node/.NET y Edge disponibles:

```powershell
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/fixed-columns-logo-20261016'
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/fixed-columns-logo-20261016/public-after.html
Push-Location ClientApp
node tests/browser/record-select-ui.mjs
node tests/browser/public-colors-ui.mjs after --logo-alignment
$env:TDV2_VISUAL_ARTIFACTS = '../.artifacts/fixed-columns-logo-20261016/regression'
node tests/browser/visual-tables-ui.mjs
Pop-Location
```

Ubuntu (equivalente no ejecutado): usar `npm` en lugar de `npm.cmd`, `export TDV2_VISUAL_ARTIFACTS="$PWD/.artifacts/fixed-columns-logo-20261016"` y ejecutar los mismos comandos `dotnet`; los `node` desde `ClientApp`. La exportación pública posterior requiere las referencias `before` de esta sesión; no sobrescribirlas con la vista ya corregida. Para otra comparación, capturarlas antes de editar. En Windows se mantienen los almacenes de herramientas `.artifacts/dotnet-home`/`.artifacts/nuget` y Node de Visual Studio ya documentados.

Orden de actualización futuro: construir frontend y después paquete ASP.NET con sus assets/Razor. **No hay paso de base de datos para esta entrega.** No se publicó nada; la instalación pendiente del catálogo/retirada de Medios sigue siendo una operación distinta, explícita y con respaldo.
