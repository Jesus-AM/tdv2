# Cabecera compacta de Competencias digitales · 2026-10-30

## Cambio implementado

La cabecera anterior separaba logotipo/cuenta y módulos en dos filas. Ahora presenta una sola barra: logotipo blanco oficial de Transformación Digital a la izquierda, módulos autorizados en el centro y cuenta a la derecha. Mantiene el contenedor existente de hasta 1400 px, sus márgenes adaptables, Noto Sans, fondo institucional `#213455`, indicador activo amarillo y foco visible. La barra tiene 72 px útiles y un borde inferior de 1 px.

`DigitalHeader` mide el espacio real del logotipo, los módulos y la cuenta mediante `ResizeObserver`, agrupando las mediciones por cuadro y esperando también la carga de fuentes. No envía peticiones para calcular la distribución:

1. Con espacio suficiente muestra fotografía, nombre e indicador de cuenta. El nombre puede abreviarse visualmente hasta 200 px; su texto completo conserva el nombre accesible, el título y el contenido del menú.
2. Si falta espacio, oculta solamente el nombre de la cuenta; conserva los módulos completos, fotografía e indicador.
3. Si los módulos ya no caben, o el contenedor mide menos de 960 px, presenta un botón de navegación y los módulos en un menú desplegable. Las etiquetas pueden ocupar varias líneas dentro del menú. No se comprimen ni saltan a otra fila de la barra.

Se mantienen el orden y los elementos de `digital.navigation` publicados por el backend. No hay un orden fijo nuevo ni cambios en permisos o claves. El menú utiliza el componente MUI existente, `aria-expanded`, nombre accesible, señalización del módulo actual, navegación por teclado, cierre con Escape/clic fuera y retorno del foco. Al cambiar entre menú y navegación visible se lleva el foco a un control disponible.

La cuenta sigue siendo el `AccountMenu` compartido, sin cambios en su código, fuente de fotografía, identidad efectiva, avatar de iniciales, privacidad o cierre de sesión. La banda de representación permanece fuera de la fila compacta. El observador existente del encabezado sigue calculando su altura completa, incluida esa banda, para posicionar el progreso y el margen de desplazamiento de la encuesta.

No cambian título, pasos, captura, informe, pie, autoguardado, auditoría ni política de sesión de 120 minutos. Los tres enlaces explícitos `/connect?returnUrl=…` de la corrección de [accesos públicos tras vencer](ACCESOS_PUBLICOS_TRAS_VENCIMIENTO_20261030.md) permanecen intactos.

## Archivos

- `ClientApp/resources/js/competencias/DigitalHeader.tsx`: barra, medición y menú adaptable.
- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`: utiliza la barra dentro del encabezado y conserva su observador/banda de representación.
- `ClientApp/resources/css/competencias-digitales.css`: estilos exclusivos de la cabecera de Competencias y su menú; altura inicial actualizada. No cambia estilos del portal o Procedimientos.
- `ClientApp/tests/browser/digital-header-ui.mjs`: recorridos y capturas sobre los bundles compilados.
- README, ESTADO_MIGRACION, GUIA, INVENTARIO y este documento con sus evidencias.

Se reutiliza `/images/logos/transformacion-digital-blanco.svg` sin modificarlo, redibujarlo o aplicar filtros. No hay recursos, paquetes, configuración, permisos ni migraciones nuevos.

## Comprobaciones

React, recursos compilados, fuentes locales y Edge reales; identidad, autorización y respuestas HTTP sintéticas mediante el fixture de interfaz existente. No se inició ASP.NET ni se conectó a PostgreSQL o servicios institucionales para esta corrección visual.

| Comprobación | Resultado |
|---|---|
| TypeScript | Correcto (`types:check`) |
| Frontend de producción | Correcto (`build`, Vite 8.3.2) |
| Pruebas frontend existentes | 105/105 |
| Matriz de cabecera | 38/38 combinaciones, cero errores JavaScript y cero escrituras HTTP |
| Cambio de tamaño con menú abierto | Cierre y foco correctos; formulario conserva el mismo nodo |
| Progreso de encuesta en 1440/390 px | Debajo de la altura real del encabezado, sin superposición |
| Orden alternativo recibido del servidor | Conservado, sin reordenarlo en el cliente |
| Regresión de autoguardado en escritorio/móvil | 34/34, cero errores JavaScript |

La matriz incluye dos/cinco módulos y nombres cortos/largos en 1920, 1440, 1280, 1100, 900, 768, 390 y 320 px. Comprueba alineación con el contenido, proporciones del SVG, color calculado `rgb(33, 52, 85)`, Noto Sans, ausencia de desbordamiento horizontal, módulos sin recortar, selección actual, menús por teclado, nombre completo y representación visible. A 1280 px con cinco módulos y nombre largo se verifica específicamente el estado intermedio sin nombre; a 1440 px se muestra la cuenta completa.

Se incluyen cuatro casos de **reflujo equivalente a zoom del 200 %**: ventana de 1440 px representada por 720 px CSS y factor de dispositivo 2. Es una emulación del espacio de diseño y densidad, no una operación del zoom nativo de la barra de herramientas de Edge. Se agregan dos casos de representación, en escritorio y móvil. Los permisos y las fotografías institucionales no se recertifican con este transporte simulado; se conserva su integración existente.

Comandos desde la raíz (Node/npm existentes en PATH):

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
Push-Location ClientApp
$env:TDV2_VISUAL_ARTIFACTS = '..\.artifacts\digital-header-20261030'
node tests/browser/digital-header-ui.mjs
$env:TDV2_VISUAL_ARTIFACTS = '..\.artifacts\digital-header-20261030\autosave'
node tests/browser/digital-autosave-ui.mjs
Remove-Item Env:TDV2_VISUAL_ARTIFACTS
Pop-Location
```

El último recorrido vuelve a comprobar el autoguardado con el transporte sintético existente: selecciones durante una solicitud lenta, ausencia de remontaje/saltos, respuesta cero conservada al recargar, guardado manual durante autoguardado, suspensión tras error sin reenvíos por foco/conexión, confirmación perdida, conflicto, rúbricas y acción masiva. Se conserva la ausencia de confirmaciones manuales durante autoguardados y una confirmación por Guardar avance. [Resultado completo](evidencia/cabecera-compacta-20261030/autoguardado.json). No equivale a una prueba de persistencia PostgreSQL ni de entrega institucional a Nexo.

## Evidencias

- [Escritorio, cinco módulos y cuenta](evidencia/cabecera-compacta-20261030/cabecera-5-1440.png).
- [Espacio intermedio, cinco módulos sin nombre en la barra](evidencia/cabecera-compacta-20261030/cabecera-5-1280.png).
- [Participante con dos módulos](evidencia/cabecera-compacta-20261030/cabecera-2-1440.png).
- [Móvil, menú de cinco módulos](evidencia/cabecera-compacta-20261030/menu-5-390.png).
- [Cuenta con nombre completo en móvil](evidencia/cabecera-compacta-20261030/cuenta-nombre-completo-390.png).
- [Reflujo equivalente al 200 %](evidencia/cabecera-compacta-20261030/menu-5-1440-zoom200.png).
- [Representación en móvil](evidencia/cabecera-compacta-20261030/cabecera-2-390-representacion.png).
- [Encuesta en escritorio](evidencia/cabecera-compacta-20261030/encuesta-5-1440.png) y [móvil](evidencia/cabecera-compacta-20261030/encuesta-5-390.png).
- [Mediciones y resultados de cabecera](evidencia/cabecera-compacta-20261030/header-verification.json).

Sin modificaciones institucionales, migraciones, reinicios, reaperturas, push o despliegue. No se requiere intervención operativa ni cambios en Nexo para habilitar esta distribución.
