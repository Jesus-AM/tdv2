# Quiz Microsoft 365 e Inter local — 2026-10-16

**Revisión vigente 2026-10-27:** el portal usa Noto Sans local y presenta dos accesos: encuesta principal a `/competencias-digitales` y **Juego de Microsoft 365 / Iniciar test**, secundario, con el mismo diálogo/adaptador/recursos de esta guía. El documento del juego conserva Inter, preguntas, lógica y diseño originales. Procedimientos conserva Roboto. [Cambios, comprobaciones y evidencia vigentes](PORTAL_Y_PERFIL_20261027.md). Las referencias siguientes a Inter en el portal documentan entregas anteriores.

## Estado y alcance

**Etiqueta vigente 2026-10-19:** el acceso público se llama **Iniciar test**. Es también su nombre accesible, obtenido del contenido del botón; conserva clases, ID, apertura y funcionamiento. Build Release y suite existente del quiz correctos; evidencia `.artifacts/quiz-button-20261019/`. El nombre anterior que aparece en la descripción de la entrega original corresponde al historial.

**Actualización posterior:** el portal y el quiz conservan íntegra esta integración y sus fuentes Inter. Sólo el sistema interno recupera `Roboto, sans-serif` con archivos locales, por solicitud posterior. Las referencias a Inter en MUI y sus capturas siguientes son evidencia de la entrega original. La regresión nueva confirma Inter público/quiz y Roboto interno; [estado vigente y pruebas](VISTAS_PROCEDIMIENTOS_COLABORADORES.md).

Se partió del commit local `a4be53f`, con Git limpio, después de revisar AGENTS, README, ESTADO, INVENTARIO, documentación visual y publicación. Conserva catálogo, agrupación, multiselectores, duplicados, retirada de Medios, permisos, presencia, reservas y autoguardado. La reversión de delimitadores permanece vigente: no cambian los estilos de Edición/Acciones ni los encabezados de grupo. También se conserva su excepción responsive anterior: hasta 600 px Edición puede desplazarse y Acciones sigue fija.

Sólo presentación y recursos estáticos en producción. Las adiciones C# pertenecen al proyecto de pruebas. Sin endpoints de respuestas, almacenamiento, seguimiento, autenticación nueva, cambios de contratos, migraciones, conexiones institucionales, push ni despliegue.

## Integración del quiz

Se leyó directamente `C:\Users\Jesus Arenas\Downloads\Reto_Microsoft_365_Diagnostico.html`; el original permanece intacto, SHA-256 `5B8310A96BC11F084B38C780F43A18B3B72509238FDC62CE6D15CEDA7AE513E2`. Una copia de referencia vive en `ClientApp/tests/fixtures/microsoft-365-original.html`, exclusivamente para verificar la conservación de preguntas, lógica y estilos. La aplicación no consulta Downloads ni usa rutas Windows.

Los recursos publicados están en `tdv2/wwwroot/quiz/microsoft-365/`:

- `index.html`: marcado original, enlaces a CSS/JS locales y atributos de acción en lugar de manejadores inline. Los dos estilos inline de iconos pasan a clases con los mismos tamaños.
- `quiz.css`: CSS original íntegro, seguido únicamente de esas dos clases y el foco visible de botones.
- `quiz.js`: **contenido exacto del script original**, comprobado en cada recorrido. Incluye las diez preguntas, opciones, respuestas, explicaciones, orden, fórmulas, niveles y medallas. Conserva «¡Nivel experto!» y «Resultado por competencias.».
- `quiz-controls.js`: conecta los cuatro botones y coloca el foco en pregunta, Siguiente y resultado; la explicación se anuncia como estado. No modifica cálculos ni respuestas.

En `Views/Home/Index.cshtml` se añade **Iniciar reto Microsoft 365** dentro de Diagnóstico de madurez digital, usando el botón institucional existente. No se retira contenido ni otras acciones. El portal Razor no monta React ni tenía un diálogo público reutilizable; se utiliza `<dialog>` nativo, con nombre accesible y botón Cerrar siempre visible, sin cargar el editor interno. Mide hasta 1180 px en escritorio y ocupa la pantalla hasta 600 px. Sólo el documento del iframe desplaza el juego.

`wwwroot/js/public-quiz.js` carga una única URL local fija mediante GET sin credenciales y coloca el HTML revisado en `iframe.srcdoc`. El iframe conserva su propio CSS y su título accesible. El cierre nativo con Escape se complementa con un manejador dentro del documento del iframe; al cerrar se restituyen el desplazamiento y el foco del botón de apertura. Hay estado de carga y reintento. Cerrar conserva la partida únicamente en memoria de esa página; recargar la elimina y Jugar de nuevo reinicia los contadores originales. No hay localStorage, cookies de resultados, telemetría ni vínculo con los formatos institucionales.

### Seguridad

El middleware existente envía `X-Frame-Options: DENY`, incluso para archivos estáticos. **No se altera ninguna cabecera ni se habilitan excepciones de enmarcado**. Se carga el recurso como documento local mediante `srcdoc`; no se navega un iframe hacia una página protegida, ni se acepta HTML/URL del visitante. La política del quiz añade `default-src 'none'; script-src 'self'; style-src 'self'; font-src 'self'; base-uri 'none'; form-action 'none'`. CSS y JavaScript son externos del mismo origen; no se incorpora `unsafe-inline`, `unsafe-eval` ni acceso de red desde el juego. Es contenido local de confianza con separación de estilos, no un sandbox para HTML arbitrario.

La ayuda conserva el alert y el texto originales. El navegador comprobó que funciona junto con la política, que no hay infracciones CSP y que el quiz no envía respuestas. El marco exterior sigue la paleta institucional sin amarillo; los colores originales del juego, incluido su logotipo Microsoft, permanecen dentro del iframe.

## Inter local

Se incluyen los archivos oficiales de [Inter 4.1](https://github.com/rsms/inter/releases/tag/v4.1) y su licencia **SIL OFL 1.1** en `tdv2/wwwroot/fonts/inter/`. `README.md` de ese directorio documenta procedencia, Git blobs y SHA-256. La descarga directa estaba restringida; los archivos se obtuvieron del tag oficial mediante el conector GitHub, sin transformarlos. El paquete final no depende del conector ni de ningún CDN.

`inter.css` define Inter normal y cursiva, pesos variables **100–900**, `font-display: swap` y respaldo Segoe UI/Arial/sans-serif. Portal y quiz cargan la misma URL local y precargan el WOFF2 normal. En la entrega original también se cambió MUI; la revisión posterior recupera Roboto sólo allí y conserva las etiquetas flotantes en 500. Se conservan valores 400, navegación 500, botones/subtítulos 600, encabezados 700 y los énfasis existentes. El quiz mantiene todos sus pesos originales, incluidos 800 y 900.

| Espacio | Fuente efectivamente observada antes | Después |
| --- | --- | --- |
| Título de portada | Arial, con externos bloqueados | Inter Variable, archivo web local |
| Grupo interno de Sistemas | Roboto instalada | Inter Variable, archivo web local |
| Título del quiz original | Segoe UI, sin carga de Inter | Inter Variable, archivo web local |

Se comprobaron fuentes renderizadas mediante CDP, no sólo `font-family`: `isCustomFont=true`. Se probaron pesos 400–900 y caracteres `ÁÉÍÓÚ áéíóú Ññ ¿¡ ü`. Los menús MUI conservan la familia del tema y los encabezados del catálogo el peso 700. Ya no hay enlaces a Google Fonts en la portada. Fotografías y logotipos conservan sus archivos/URLs; UACJ mantiene tamaño, proporciones, jerarquía y compensación horizontal. La distribución, colores y tamaños anteriores permanecen; el cambio de familia puede redistribuir las líneas de texto.

## Verificación nueva

- TypeScript, Vite y build .NET Release correctos; **94/94 pruebas frontend**. El build de solución mantiene las cuatro advertencias NU1902/NU1903 ya documentadas de Pkcs 9.0.18.
- **11/11 recursos anónimos** mediante el pipeline real ASP.NET/TestServer: portada, quiz, scripts, CSS, WOFF2 y licencia, con MIME, contenido, `DENY` y `nosniff`. Host de pruebas con dobles de negocio en memoria, sin PostgreSQL.
- **9 grupos de comprobaciones del quiz** en Edge a 1366×900, 390×844 y 320×800: seis partidas completas (10/7/5/0 aciertos en escritorio, 7 con rachas interrumpidas en móvil y 10 en pantalla de 320 px), explicaciones, XP, racha máxima, medallas, cuatro niveles, ayuda, reinicio, foco visible, Tab/Shift+Tab entre cierre e iframe, Escape en ambos documentos, botón Cerrar, posición/foco restaurados y ausencia de scroll horizontal. Las preguntas y lógica se comparan con la referencia íntegra. Se repitió el recorrido con los recursos estáticos del paquete publicado.
- **24/24 regresiones de tablas y 9/9 de selectores**, React/MUI/Edge reales con transporte HTTP/SignalR, identidades, reservas y persistencia simulados. Pocas/muchas filas, escritorio/móvil/zoom CSS 200 %, menús, encabezados, campos dependientes, columnas, dos actores, confirmaciones, asociaciones y autoguardado conservando nodo/foco/scroll.
- `dotnet publish` correcto en copia aislada con lockfile y cachés locales; comprobación de igualdad de once recursos esenciales en `publish/wwwroot`. El paquete incluye fuentes, licencia, quiz y controles, sin Downloads ni archivos externos. No se inicia el host operativo publicado ni se consulta una base para esta prueba.

El primer publish sobre la carpeta de trabajo encontró un binario Node bloqueado. Las dependencias se reconciliaron desde caché sin cambiar package.json/lockfile; no se detuvo el proceso ajeno. Se construyó en `.artifacts/quiz-inter-20261016/source/`, indicando explícitamente el caché NuGet raíz porque `Directory.Build.props` usa una ruta relativa. El intento inicial con caché vacío se canceló; sólo el publish final correcto se cuenta como evidencia. La primera consulta CDP de un texto de prueba no pintado devolvió cero glifos; se corrigió el auxiliar para esperar el pintado. Ninguna de esas incidencias requirió cambios en contratos, datos o políticas.

## Evidencia y límites

Directorio `.artifacts/quiz-inter-20261016/`:

- `public-{before,after}-{ancho}.png`, `hero-*`, `diagnosis-*` y `internal-*`: referencias antes/después con las mismas pantallas. `quiz-before-*` muestra el archivo original; `quiz-after-*`, `question-after-*` y `result-*-after-*` muestran la integración.
- `quiz-inter-{before,after}-report.json`: fuentes reales, peticiones, resultados y recursos externos bloqueados. `internal-regression/` y `record-select/`: regresiones y capturas internas, incluidas reservas y menús.
- `http-assets.json`, `publish-assets.json`, `published-browser/`, `publish-final.txt`, `types.txt`, `vite.txt`, `build.txt`, `frontend.txt` y `baseline/` con los hashes previos. `scope-audit.json` confirma el alcance final y la conservación del original, imágenes y migración.

Las capturas de escritorio y móvil se revisaron. Se bloquearon deliberadamente recursos externos para ambos pases: la fotografía Unsplash no está en la comparación; los enlaces originales permanecen. Antes se bloquearon además las fuentes Google, por lo que la comparación tipográfica anterior refleja el respaldo real de ese entorno. Después Inter carga desde el mismo sitio y no requiere red externa.

No se ejecutaron PostgreSQL, migraciones, dispositivos físicos, Safari/Firefox, lector de pantalla ni el proxy institucional. La publicación se construyó en Windows; Ubuntu y el arranque operativo del paquete quedan fuera de esta comprobación. Los recorridos públicos usan Razor exportado desde ASP.NET de pruebas y luego servido localmente a Edge; los HTTP de políticas/recursos sí pasan por ASP.NET. No se confunden con pruebas de autenticación o de persistencia. No queda implementación pendiente en este alcance.

## Reproducción y publicación

Desde la raíz, con Node/npm, .NET 10 y Edge disponibles. Conservar las referencias anteriores; usar otro directorio para una corrida nueva. El modo `before` se ejecuta antes de editar y requiere exportar `public-before.html`.

```powershell
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/quiz-inter-repeat'
New-Item -ItemType Directory -Path $env:TDV2_VISUAL_ARTIFACTS -Force | Out-Null
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public "$env:TDV2_VISUAL_ARTIFACTS/public-after.html"
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets "$env:TDV2_VISUAL_ARTIFACTS/http-assets.json"
Push-Location ClientApp
node tests/browser/quiz-inter-ui.mjs after
node tests/browser/visual-tables-ui.mjs
node tests/browser/record-select-ui.mjs
Pop-Location
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-quiz-inter
```

El publish vigente ejecuta `npm ci`, por lo que debe realizarse sin Vite bloqueando las dependencias o desde una copia aislada. En esta sesión se conservaron los cachés `.artifacts/dotnet-home`, `.artifacts/nuget`, `.artifacts/npm-cache`; para la copia aislada se pasó `-p:RestorePackagesPath=<ruta del caché raíz>` y `npm_config_offline=true`. Para comprobar los estáticos publicados, establecer `TDV2_PUBLIC_ROOT` al `wwwroot` del paquete y repetir `quiz-inter-ui.mjs after`.

Ubuntu: usar `npm`, `export TDV2_VISUAL_ARTIFACTS="$PWD/.artifacts/quiz-inter-repeat"`, `mkdir -p "$TDV2_VISUAL_ARTIFACTS"` y los mismos comandos .NET; ejecutar los `node` desde `ClientApp`. Este equivalente no se ejecutó. Construir frontend/backend con el target de publicación existente y distribuir el paquete completo. **Este ajuste no requiere migraciones ni un paso de datos**; las migraciones pendientes de entregas previas conservan sus requisitos independientes.

## Archivos

Producción: `ClientApp/index.html`, `ClientApp/resources/css/app.css`, `ClientApp/resources/js/theme.ts`, `tdv2/Views/Home/Index.cshtml`, los directorios nuevos `tdv2/wwwroot/fonts/inter/`, `tdv2/wwwroot/quiz/microsoft-365/`, `tdv2/wwwroot/css/public-quiz.css` y `tdv2/wwwroot/js/public-quiz.js`.

Pruebas: `ClientApp/tests/browser/quiz-inter-ui.mjs`, `system-ui-fixture.mjs` (MIME/ruta de fuentes locales), `ClientApp/tests/fixtures/microsoft-365-original.html`, `tests/TDV2.Verification/PublicAssetsTests.cs` y su registro explícito en `EntryPoint.cs`. Documentación: este archivo, README, ESTADO, INVENTARIO, AJUSTES_VISUALES_Y_PALETA_PUBLICA y PUBLICACION. `FormatoUR`, `CaptureTableScroll`, motores de captura, controles de presencia y seguridad productiva permanecen intactos.
