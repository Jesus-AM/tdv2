# Cabecera y pie de Competencias, acceso público al juego — 2026-10-28

**Revisión posterior:** la [cabecera azul y el SVG blanco oficial](CABECERA_AZUL_COMPETENCIAS_20261028.md) sustituyen únicamente el tratamiento blanco/oscuro de la cabecera documentado aquí. El pie compartido, el acceso público al juego y las demás mejoras se conservan. Las capturas siguientes son evidencia del estado anterior.

## Implementado

El bloque público del juego muestra únicamente **«¿Qué tanto sabes de Microsoft 365?»** y **«Jugar Ahora»**. Se retira su subtítulo y el margen asociado. Se conservan el ID del botón, diálogo, iframe, recursos, preguntas, puntuación, progreso y ayuda del juego. El acceso principal «Responder encuesta de competencias digitales» sigue apuntando a `/competencias-digitales` mediante navegación normal. No se modifican las ocho dimensiones ni otros textos del portal.

Competencias reutiliza el SVG oficial **`/images/logos/TRANSFORMACION_DIGITAL_logo.svg`**, el mismo recurso oscuro de la barra pública. Se conserva completo, proporcional, con altura de 48 px en escritorio y 40 px en móvil. La barra pública actual es blanca: Competencias adopta `--uacj-white`, texto `--uacj-blue`, borde `--uacj-gray` y `backdrop-filter: blur(10px)`. No se aproxima un azul ni se utiliza un logo blanco sobre blanco. La opción activa se distingue con fondo claro, peso y borde azul, además de `aria-current`; foco y hover visibles. Los estilos están bajo `.cd-site`.

Se mantienen las dos filas de identidad/cuenta y navegación, el contenedor de hasta 1400 px, nombre/foto y menú existentes. En móvil el nombre completo sigue accesible en el botón y al abrir el menú. El orden y la visibilidad se reciben del backend/Nexo sin ordenamiento adicional. La banda de representación, las dos identidades y la salida continúan visibles; su medición de altura mantiene libres los controles y el progreso de encuesta.

El componente `InstitutionalFooter` presenta ahora las seis firmas tanto en TDV2 como en Competencias, en el mismo orden del Razor público:

1. UACJ Inspira el Futuro.
2. Dirección General de Tecnologías de la Información.
3. Subdirección de Desarrollo de Sistemas.
4. Subdirección de Tecnología Educativa.
5. Subdirección de Operación y Redes.
6. Subdirección de Inteligencia de Datos.

Se reutilizan `firma-1.png`, su ventana UACJ existente, las firmas de áreas en Noto Sans y **la hoja compartida `institutional.css`, sin modificarla**. Se retiran las reglas exclusivas de cinco columnas de Competencias. Conserva los tamaños/jerarquía del portal: UACJ 220 px, Dirección 16 px (17 px móvil) y las cuatro subdirecciones 14 px, prefijo regular/nombre en negrita y línea 1.4. Fondo `#213455`, texto blanco y divisores existentes. Cuando las seis firmas caben, se distribuye el espacio libre del contenedor más ancho; en tamaños inferiores se aplica el mismo flujo flexible con varias filas del portal.

El pie continúa en el flujo normal: `min-height:100dvh`, contenedor vertical y contenido flexible. No se fija el pie ni se añade relleno para forzar desplazamiento. El acceso al aviso permanece en la cuenta; no cambia su cuerpo, versión o aceptación.

Esta revisión sustituye la distribución anterior de UACJ en la cabecera y sólo cinco firmas en el pie de Competencias. El pie público y el de TDV2 conservan su presentación anterior.

## Archivos de esta corrección

Producción, cuatro archivos:

- `tdv2/Views/Home/Index.cshtml`: título/botón del acceso al juego; retiro del subtítulo y su estilo exclusivo.
- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`: logo oscuro de Transformación Digital.
- `ClientApp/resources/css/competencias-digitales.css`: barra clara, estados de navegación y adopción de las firmas compartidas.
- `ClientApp/resources/js/Components/InstitutionalFooter.tsx`: UACJ también en la variante de Competencias; contenido de la variante interna sin cambios.

Pruebas actualizadas: `identity-digital-ui.mjs`, `quiz-inter-ui.mjs`, `digital-report-ui.mjs`, `competencias-native.mjs` y la comprobación del portal en `tests/TDV2.Verification/EntryPoint.cs`. En `digital-report-ui.mjs` y `competencias-native.mjs` sólo cambia la expectativa de que el pie incluye UACJ; no se ejecutaron esas dos suites por este ajuste. El informe y la captura se recorrieron con `identity-digital-ui.mjs`, sin PostgreSQL.

Documentación: README, ESTADO_MIGRACION, guía de Competencias y esta evidencia. Se conservan todos los cambios pendientes del instrumento/actividad. [Comprobación de 56 archivos protegidos sin cambios](evidencia/cabecera-pie-juego-20261028/conservacion.json): backend, contratos, migraciones, seguridad/401, referencias de paquetes, cliente HTTP, captura e informe, entre otros. No se añadieron migraciones ni se ejecutaron conexiones institucionales, reinicios, push o despliegue.

## Comprobaciones realizadas

| Comprobación | Resultado |
| --- | --- |
| TypeScript | `tsc --noEmit` correcto |
| Frontend | Vite build correcto |
| ASP.NET/Verification Release | Cero errores y cero advertencias |
| Backend existente, dobles sin bases | 116 pruebas correctas, incluida vista pública con ambos accesos |
| Portal/juego/TDV2 en navegador | [51 grupos correctos, cero errores JS](evidencia/cabecera-pie-juego-20261028/quiz-inter-after-report.json): textos exactos, botones separados, juego/ayuda/progreso, PDFs, fuente, pies e interfaz interna |
| Competencias, bundles | [11 grupos correctos, cero errores JS](evidencia/cabecera-pie-juego-20261028/digital-build-report.json) |
| Competencias, Vite real | [11 grupos correctos, cero errores JS](evidencia/cabecera-pie-juego-20261028/digital-vite-report.json); recursos CSS/fuentes servidos desde sus rutas correctas |

Competencias se revisó a **1920, 1440, 1366, 1024, 768, 390 y 320 px**, más cuatro recorridos de menús según autorización. Se comprueban alineación de cabecera/título/contenido/pie, carga y proporción del logo, Noto Sans realmente renderizada, foto/avatar de respaldo, nombre completo, teclado y retorno de foco, representación, contraste textual de la barra y banda de representación, navegación activa, foco del primer pendiente y fallo de guardado que conserva la respuesta. El pie se verifica con páginas largas y estados vacío/carga/error breves, sin superposición ni desplazamiento artificial/horizontal.

Las medidas de la [barra pública](evidencia/cabecera-pie-juego-20261028/portal-header.json) y de Competencias coinciden en fondo blanco y borde gris; el CSS utiliza los mismos tokens y desenfoque. Las cuatro firmas de subdirecciones comparten familia, tamaño, peso, interlineado y alineación; no se agrandan nombres cortos.

Pruebas con Razor real exportado por ASP.NET TestServer y React/MUI reales, transporte e identidades sintéticos, puertos loopback aleatorios. **No verifican Nexo/Microsoft institucional ni PostgreSQL en esta revisión.** Se conserva la advertencia de obsolescencia HMR del arnés Vite; no es un error de compilación ni un cambio de paquetes. Móvil/tableta emulados en Edge; queda la revisión opcional en dispositivos físicos. Sin configuración operativa nueva.

## Capturas

| Vista | Escritorio | Tableta / móvil |
| --- | --- | --- |
| Competencias: cabecera, cuenta y pie completo | [1440 px](evidencia/cabecera-pie-juego-20261028/digital-home-1440.png) | [768 px](evidencia/cabecera-pie-juego-20261028/digital-home-768.png) · [390 px](evidencia/cabecera-pie-juego-20261028/digital-home-390.png) |
| Encuesta principal y juego | [Accesos](evidencia/cabecera-pie-juego-20261028/accesos-1440.png) | [Accesos móvil](evidencia/cabecera-pie-juego-20261028/accesos-390.png) |
| Menú completo, cuenta y representación | — | [Administrador](evidencia/cabecera-pie-juego-20261028/digital-navigation-admin-390.png) · [Cuenta](evidencia/cabecera-pie-juego-20261028/digital-account-390.png) · [Representación](evidencia/cabecera-pie-juego-20261028/digital-representation-390.png) |

Artefactos locales: `.artifacts/brand-game-20261028`. Repetición con Node/npm disponibles en PATH:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/brand-game-20261028/public-after.html

# Desde ClientApp:
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/brand-game-20261028'
node tests/browser/quiz-inter-ui.mjs after
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/brand-game-20261028/digital-build'
node tests/browser/identity-digital-ui.mjs
$env:TDV2_TEST_VITE='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/brand-game-20261028/digital-vite'
node tests/browser/identity-digital-ui.mjs
Remove-Item Env:TDV2_TEST_VITE
```
