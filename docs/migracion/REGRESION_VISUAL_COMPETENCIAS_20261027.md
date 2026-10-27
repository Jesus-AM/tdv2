# Corrección de carga visual de Competencias — 2026-10-27

## Causa comprobada

El fallo se reprodujo en navegador con **Vite real**, el mismo prefijo `/__vite/` que usa `ReactShell` y autenticación/datos sintéticos. No se accedió a sesiones ni bases institucionales.

Vite reescribía los enlaces de `ClientApp/index.html`:

| Recurso | URL incorrecta en desarrollo | Respuesta |
|---|---|---|
| CSS institucional | `/__vite/css/institutional.css` | HTTP 200 **text/html** |
| Precarga Noto Sans | `/__vite/fonts/noto-sans/NotoSans-Regular.ttf` | HTTP 200 **text/html** |

Son archivos de `tdv2/wwwroot`, no del servidor Vite. Su fallback devolvía el HTML de la aplicación. El navegador no aplicaba la hoja institucional ni sus importaciones de tokens/fuentes.

Las mediciones de la página, antes de corregirla, confirmaron:

- `--uacj-blue` y `--cd-blue` vacíos; fondo calculado de la cabecera `rgba(0, 0, 0, 0)` sobre el blanco del subsitio.
- Nombre con `rgb(255, 255, 255)`, `visibility: visible`, `opacity: 1` y dimensiones normales. **Texto blanco sobre blanco**, no un permiso que ocultara la cuenta.
- Logotipo HTTP 200 `image/png`, cargado con sus 1186 píxeles de ancho natural. Su dibujo blanco tampoco contrastaba.
- Las variables de los bordes y el fondo del pie faltaban por el mismo motivo. El avatar seguía distinguiéndose.
- No se identificó una regla global que sobrescribiera los estilos ni una desaparición de la navegación. Las pruebas anteriores usaban los bundles compilados: no cubrían esta transformación de URLs de Vite.

## Corrección mínima

Se añade **`vite-ignore`** a los dos enlaces estáticos de `ClientApp/index.html`. Vite conserva `/css/institutional.css` y `/fonts/noto-sans/NotoSans-Regular.ttf`, que sirve ASP.NET en el mismo origen. El atributo está soportado por la versión instalada y se elimina del HTML transformado.

No se restauraron archivos ni se añadieron sobrescrituras globales o duplicaciones de colores. El CSS de Competencias continúa bajo `.cd-site`; las firmas conservan sus clases institucionales. Por SHA-256 permanecen idénticos el CSS público/interno/Competencias, tokens, tema MUI, vista Razor pública y componente principal de Competencias.

Con la hoja cargada: cabecera y pie `rgb(33, 52, 85)`, nombre blanco, tarjetas blancas con borde sólido de 1 px `rgb(218, 218, 218)`, Noto Sans local en los cuatro pesos y recursos UACJ blancos sobre azul. Se mantienen la cuenta/foto, los módulos suministrados por el backend y DGTI más cuatro subdirecciones en el pie.

## Verificación

- TypeScript, Vite y ASP.NET Release: correctos. El build ya no advierte que no encuentra `institutional.css` para transformarlo.
- **Cuatro recorridos de Competencias con Vite y cuatro con bundles**: 1366, 768, 390 y 320 px. Inicio, encuesta/perfil/rúbricas, informe, cuenta, avatar de respaldo, representación, aviso, foco, pasos bloqueados y guardado fallido conservando lo capturado. Pie natural en páginas cortas/largas y sin desbordamiento horizontal.
- Recursos medidos en desarrollo: `/css/institutional.css`, `/css/design-tokens.css` y declaración Noto HTTP 200 `text/css`; cuatro TTF y PNG cargados. Familia Noto Sans realmente renderizada comprobada mediante CDP. El test rechaza las rutas incorrectas `/__vite/css/` y `/__vite/fonts/`.
- **19 grupos de portal/juego** correctos; Procedimientos revisado en escritorio/móvil con Vite, fuente local real y sus controles/tablas existentes. La vista y estilos públicos no cambiaron.
- **37 recursos anónimos** verificados mediante ASP.NET TestServer: CSS, fuentes, imágenes y documentos con tipos de contenido correctos.
- Pruebas UI con React/MUI/Vite reales, transporte de negocio sintético y puertos aleatorios loopback; no acreditan Microsoft/Nexo/SQL reales. Móvil emulado. Persisten las advertencias NuGet previas de Pkcs y una advertencia de obsolescencia de configuración HMR; no se modificaron dependencias ni el perfil de desarrollo del operador.

La única modificación productiva de esta corrección es `ClientApp/index.html`. Se amplían `ClientApp/tests/browser/system-ui-fixture.mjs` e `identity-digital-ui.mjs` para cubrir desarrollo además de build, recursos y estilos calculados. Se actualizan README, ESTADO_MIGRACION, guía de Competencias y esta evidencia. **Sin cambios de permisos, navegación, autoguardado, evaluación, bases, migraciones, reinicios, push o despliegue.**

## Capturas

| Vista | Escritorio | Móvil |
|---|---|---|
| Fallo reproducido antes del arreglo | [Antes](evidencia/cd-regresion-20261027/before-home-1366.png) | [Antes](evidencia/cd-regresion-20261027/before-home-390.png) |
| Inicio corregido | [Después](evidencia/cd-regresion-20261027/digital-home-1366.png) | [Después](evidencia/cd-regresion-20261027/digital-home-390.png) |
| Perfil y controles | [Encuesta](evidencia/cd-regresion-20261027/digital-profile-1366.png) | [Encuesta](evidencia/cd-regresion-20261027/digital-profile-390.png) |
| Informe | [Informe](evidencia/cd-regresion-20261027/digital-report-1366.png) | [Informe](evidencia/cd-regresion-20261027/digital-report-390.png) |
| Procedimientos, presentación conservada | [Interno](evidencia/cd-regresion-20261027/internal-after-1366.png) | [Interno](evidencia/cd-regresion-20261027/internal-after-390.png) |

[Estilos/recursos antes](evidencia/cd-regresion-20261027/before-report.json) · [Vite corregido](evidencia/cd-regresion-20261027/vite-report.json) · [Bundles](evidencia/cd-regresion-20261027/build-report.json) · [Portal/Procedimientos](evidencia/cd-regresion-20261027/portal-report.json) · [HTTP ASP.NET](evidencia/cd-regresion-20261027/public-assets.json).

Artefactos de trabajo: `.artifacts/cd-visual-regression/`. Para repetir desde `ClientApp`, después de `npm run types:check` y `npm run build`:

```powershell
$env:TDV2_TEST_VITE='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cd-visual-regression/after-vite'
node tests/browser/identity-digital-ui.mjs
Remove-Item Env:TDV2_TEST_VITE
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cd-visual-regression/after-build'
node tests/browser/identity-digital-ui.mjs
```

El modo Vite de la prueba no usa los puertos 5173/7136 ni lee archivos `.env`; no requiere iniciar el host institucional. Para revisar la sesión habitual, basta recargar la página con Vite/ASP.NET actuales. No es necesario cambiar secretos, permisos, datos ni migraciones.
