# Portal público y perfil — 2026-10-27

**Revisión visual posterior vigente:** [Identidad común, firmas y fotografía](IDENTIDAD_VISUAL_20261027.md). Noto Sans ahora cubre los tres espacios y el juego; la fotografía externa fue sustituida por IMG_9885 local optimizada. Las áreas se presentan como firmas Noto uniformes; orden público UACJ/DGTI/SDS/STE/SOR/SID, y Competencias traslada DGTI de su cabecera al pie. Se conserva la organización del perfil, documentos y accesos separados. Las secciones siguientes documentan las etapas anteriores y no describen ya la tipografía ni composición vigente.

## Ampliación de UACJ/DGTI en el pie

Ajuste posterior: `.footer-identity` pasa de 400 a 480 px (+20 %), manteniendo la firma conjunta completa, proporciones y posición izquierda. `.footer-signatures` vuelve a ser un grupo flexible que pasa a otra fila cuando falta espacio para todas; distribuye las cuatro subdirecciones sin reducirlas ni dejar una aislada junto al bloque principal. Sus anchos individuales y escala del 28 % no cambian. Únicos cambios productivos: esas dos reglas CSS de `tdv2/Views/Home/Index.cshtml`.

La imagen principal conserva `max-width:100%`: a 320/390 px ya ocupaba el ancho disponible, por lo que no crece fuera del contenedor. El +20 % es efectivo donde caben los 480 px; no se recorta ni se altera la imagen para separar UACJ y DGTI. El fondo azul, imágenes blancas, flujo del pie y Competencias permanecen intactos.

Compilación Release y revisión Edge en 1440, 1366, 1280, 1100, 1024, 768, 390 y 320 px correctas. Se comprobaron tamaño principal, escala constante de subdirecciones, orden, proporciones, alineación izquierda, ausencia de desbordamiento del pie, estados breves/largos y geometría/estilos idénticos del resto del portal. Móvil emulado; persisten el desbordamiento previo ajeno al pie a 1024 px y advertencias NuGet anteriores. Sin bases, migraciones, push ni despliegue.

[Escritorio](evidencia/pie-principal-ampliado-20261027/footer-1366.png) · [Móvil](evidencia/pie-principal-ampliado-20261027/footer-390.png) · [320 px](evidencia/pie-principal-ampliado-20261027/footer-320.png) · [Mediciones](evidencia/pie-principal-ampliado-20261027/report.json). Artefactos: `.artifacts/public-footer-plus20-20261027/`; comprobación puntual: `.artifacts/verify-footer-plus20.mjs`, sobre Razor exportado por TestServer sin bases.

## Escala uniforme de las cuatro subdirecciones

La corrección más reciente conserva todas las firmas como imágenes. El ancho común anterior hacía crecer las letras de Operación y Redes respecto de Tecnología Educativa. Se toma Inteligencia de Datos como referencia y se aplica una escala constante del 28 % a cada recurso completo:

| Firma | Dimensiones originales | Ancho mostrado |
|---|---|---:|
| Inteligencia de Datos | 548 × 117 px | 153.44 px |
| Desarrollo de Sistemas | 603 × 111 px | 168.84 px |
| Operación y Redes | 479 × 123 px | 134.12 px |
| Tecnología Educativa | 550 × 121 px | 154 px |

Las letras de la primera línea original tienen 39–40 px de altura visible; mostradas quedan en 10.92–11.20 px de tinta, distinto del tamaño CSS de una fuente HTML. La diferencia residual procede del raster original. Las cajas comparten alineación superior, altura mínima de 48 px y el mismo espaciado/divisor. Se conservan los márgenes, peso y espaciado propios de cada imagen, sin recortar, deformar ni redibujar. No se amplía una firma corta para llenar su espacio. Cuando falta ancho, se pasa a otra fila; hasta 600 px las cuatro subdirecciones se apilan manteniendo su escala de escritorio. La firma conjunta UACJ/DGTI conserva tamaño, posición y recurso. La última aclaración del usuario priorizó uniformar las cuatro imágenes; no se realizó la conversión a texto HTML consultada previamente.

Único archivo productivo modificado: `tdv2/Views/Home/Index.cshtml`, selectores de `.footer-signatures`. Compilación Release y comparación Edge en 1440, 1366, 1280, 1100, 1024, 768, 390 y 320 px correctas. Alineación óptica de la primera línea dentro de 0.31 px; recursos completos y proporcionales, escala estable, orden, pie al fondo en estados breves y después de contenido largo. Los elementos ajenos al pie son idénticos antes/después. Se mantiene el desbordamiento previo exterior al pie a 1024 px; no se cambia por estar fuera del alcance. Móvil emulado, sin bases ni migraciones; advertencias NuGet anteriores.

Evidencia: [escritorio](evidencia/subdirecciones-uniformes-20261027/footer-1366.png), [móvil](evidencia/subdirecciones-uniformes-20261027/footer-390.png), [320 px](evidencia/subdirecciones-uniformes-20261027/footer-320.png) e [informe de medidas](evidencia/subdirecciones-uniformes-20261027/report.json). Artefactos y comparación puntual en `.artifacts/public-footer-hierarchy-20261027/` y `.artifacts/verify-footer-hierarchy.mjs`. No se modificó el pie de Competencias.

## Ajuste posterior: pie público en una franja

Solo se ajustaron los selectores del pie en `tdv2/Views/Home/Index.cshtml`: contenedor flex con salto de línea, firma conjunta UACJ/DGTI de 400 px máximos al inicio del contenido y cuatro subdirecciones a continuación. El contenedor de subdirecciones usa `display:contents` para distribuir sus firmas en la misma franja, respetando el orden del documento. No se separa, recorta, deforma ni redibuja la imagen original conjunta. Se retiran el centrado, fila superior y separador horizontal anteriores. Las imágenes, fondo azul y funcionamiento del pie al fondo no cambian; tampoco el pie de Competencias ni otros elementos del portal.

Compilación Release correcta. Revisión visual Edge antes/después en 1440, 1366, 1280, 1100, 1024, 768, 390 y 320 px: una franja desde 1100 px; varias filas ordenadas con menos espacio, sin desbordamiento del pie. Medidas de alineación, proporciones y flujo con contenido largo/vacío/carga/error correctas. Geometría/estilos de los elementos externos al pie idénticos; archivos del pie de Competencias idénticos por hash. A 1024 px existe desbordamiento previo fuera del pie, conservado por el alcance solicitado. Móvil emulado; no se accede a bases ni se aplican migraciones. Advertencias NuGet anteriores sin cambios.

Evidencia: [escritorio](evidencia/pie-publico-20261027/footer-1366.png), [móvil 390 px](evidencia/pie-publico-20261027/footer-390.png), [móvil 320 px](evidencia/pie-publico-20261027/footer-320.png) e [informe de medidas](evidencia/pie-publico-20261027/report.json). Razor exportado mediante `dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/public-footer-strip-20261027/public-after.html`; comprobación puntual con Node/Edge en `.artifacts/verify-footer-strip.mjs`. Las capturas de la sección anterior documentan la distribución sustituida.

## Implementado

Revisión sobre el trabajo local existente, sin cambios de instrumentos, fórmulas, contratos, permisos, secretos, conexiones ni migraciones. **No se ejecutan reinicios de encuestas en esta revisión.**

Perfil se organiza en Datos institucionales (nombre, dependencia N2, área N3), Perfil laboral (puesto, antigüedad, funciones, personal a cargo) y Uso de herramientas e información (jornada digital, información confidencial). El orden visual y de teclado coincide; dos columnas en escritorio y una hasta 700 px. Funciones y jornada digital disponen del ancho completo. Las unidades admiten varias líneas. Se mantienen IDs `perfil:*`, opciones, asteriscos obligatorios, resolución institucional y «No aplica» legítimo. El foco busca el primer pendiente en orden del documento; no se cambia el orden histórico del instrumento.

Se conservan respuestas, autoguardado, validación secuencial, versión/generación, recuperación entre dispositivos e informes definitivos. Cuenta/foto compartidas, representación de consulta y aviso de lectura desde la cuenta permanecen.

Los dos contenedores de página usan altura **mínima** `100dvh`, distribución vertical y contenido flexible. Los pies conservan su altura natural y posición estática: al fondo con contenido breve y después del contenido largo. Encabezados y progreso mantienen sus mediciones existentes; no hay alturas fijas ni rellenos para forzar desplazamiento.

El portal carga **Noto Sans 400/500/600/700** desde `/fonts/noto-sans/public.css`, reutilizando los cuatro TTF oficiales y OFL existentes. No consulta Google Fonts. Competencias conserva su alias aislado `CD Noto Sans`, Procedimientos Roboto y el documento del juego Inter. Firmas originales sin modificación.

Contacto: DGTI, Subdirección de Inteligencia de Datos, `td.pi@uacj.mx` y su mailto; extensiones y horario intactos. Ruta usa «ocho etapas» y los tres textos solicitados; las fases de capacitación del informe no cambian. Diagnóstico conserva ocho dimensiones y el párrafo exacto solicitado.

Accesos separados:

- **Responder encuesta de competencias digitales**: enlace principal normal a `/competencias-digitales`, con autenticación y retorno existentes.
- **Juego de Microsoft 365 / Iniciar test**: botón secundario `open-microsoft-quiz`; adaptador existente `/js/public-quiz.js`, diálogo y recurso `/quiz/microsoft-365/index.html` mediante srcdoc, sin relajar cabeceras. Preguntas, puntuación, lógica y diseño del juego intactos. Escape/cierre restituyen foco y desplazamiento; el juego no envía respuestas al diagnóstico.

Pie público azul `#213455`: firma blanca completa UACJ Inspira el Futuro/DGTI (`firma-1.png`), separador y cuatro subdirecciones (`firma-3/5/7/9.png`). Competencias conserva **solo las cuatro subdirecciones**. Recursos compartidos con proporción natural y distribución móvil.

## PDF públicos

Copias binarias desde Downloads/Documentos-td, sin modificaciones; cabeceras y títulos internos confirman correspondencia. Los tres textos de enlace y su orden se conservan; solo se retira Política de Innovación. El listado no deja una cuarta celda vacía.

| Enlace existente | Documento y ruta pública exacta |
|---|---|
| Plan Institucional de Transformación Digital | Marco general: `/documentos/PITD_2026_2030_Marco_General_UACJ.pdf` |
| Decálogo | `/documentos/Decalogo_Cultura_de_Datos_UACJ.pdf` |
| Glosario | `/documentos/Glosario_Transformacion_Digital_UACJ.pdf` |

Viven en `tdv2/wwwroot/documentos/`, respetando mayúsculas/minúsculas. Enlaces directos con `target="_blank"`, `rel="noopener"` y aviso para lector de pantalla; sin atributo download ni Content-Disposition attachment. ASP.NET sirve application/pdf sin sesión. El enlace directo sigue disponible para el visor/aplicación configurado por el navegador. No hay dependencias de ejecución en Downloads, rutas Windows o file://.

| Archivo | Bytes | SHA-256 original/copia |
|---|---:|---|
| PITD_2026_2030_Marco_General_UACJ.pdf | 14398920 | `6F5EAD2FD5936ED4C64BE7C49CF1AB8D8AD5015DDB8B3B7239028D9FAFC9C1C0` |
| Decalogo_Cultura_de_Datos_UACJ.pdf | 2167730 | `53C5ADE776F802FC9043F7B52C1BDFD1509B036F3B17E6EB79B021F1C9A983FC` |
| Glosario_Transformacion_Digital_UACJ.pdf | 8080899 | `0E72A5ED01B2BD70C63BFE2D5423107B4DBD7EF755B3282D62A590D3E5D58031` |

## Archivos de esta revisión

- `ClientApp/resources/js/competencias/Survey.tsx`: grupos del perfil y foco visible.
- `ClientApp/resources/css/competencias-digitales.css`: columnas, grupos y contenedor flexible.
- `tdv2/Views/Home/Index.cshtml`: fuente, contenedor, textos, contacto, accesos, documentos y pie.
- `tdv2/wwwroot/fonts/noto-sans/public.css` y `README.txt`: reutilización pública; TTF/OFL existentes intactos.
- `tdv2/wwwroot/documentos/`: tres PDF originales.
- `tests/TDV2.Verification/PublicAssetsTests.cs`: HTTP anónimo, MIME, fuentes y PDF sin descarga forzada.
- `ClientApp/tests/browser/quiz-inter-ui.mjs`: contenido, Noto, documentos, pies y regresión del juego Inter/Procedimientos Roboto.
- `ClientApp/tests/browser/competencias-native.mjs`: enlace principal, perfil/teclado/foco y selección sin reinicio.
- `ClientApp/tests/browser/page-flow.mjs`: flujo normal y estados breves sintéticos.
- README, ESTADO_MIGRACION, GUIA, INVENTARIO, documentación del juego y esta guía. Los demás cambios locales anteriores se conservan.

## Pruebas y evidencia

TypeScript y Vite correctos; **95 pruebas frontend** correctas. ASP.NET y verificadores compilan en Release. **30 recursos HTTP anónimos** correctos con ASP.NET TestServer: PDF, Noto, firmas, Inter, Roboto y juego; sin bases, con dobles de negocio. La rama autenticada de Razor conserva sus acciones.

**20 grupos de navegador de Competencias correctos**, con ASP.NET real y PostgreSQL 18 desechable en loopback. Perfil institucional ineditable, orden/foco/columnas, captura y recuperación, saltos bloqueados, Inicial cero, complementarias, fallos de guardado/respuesta perdida, regreso sin pérdida, definitiva/informe, cuenta/foto/avatar, representación, roles y participante exclusivo. **La prueba de reinicio se omite explícitamente** mediante `TDV2_DIGITAL_PRESENTATION_ONLY=1`; tampoco se ejecuta la suite backend de reinicios. Solo se finaliza el diagnóstico de la identidad sintética del recorrido.

Portal/juego en Edge a **1366, 390 y 320 px**: fuente real por CDP (español y cuatro pesos Noto), Inter del iframe y Roboto interno. Seis partidas cubren los cuatro resultados originales y teclado; sin envíos de respuestas ni errores JS. Pies medidos con contenido largo y cuatro estados breves (vacío, carga, sin resultados, error) sustituidos **solo en el DOM de pruebas**, restaurando el documento por recarga. No son nuevas respuestas de API.

Artefactos: `.artifacts/public-profile-20261027/` y `.artifacts/native-postgres-20261027-150420-f7e5effa/` (20 grupos; servidor aislado detenido). Un recorrido posterior limitado a los tres grupos de acceso/privacidad/perfil, con `TDV2_DIGITAL_PROFILE_ONLY=1`, produce capturas desde el inicio de la página para evitar el encabezado adhesivo a mitad de una captura completa: `.artifacts/native-postgres-20261027-151719-78ea8dd4/`. No cambió código productivo entre estos dos recorridos. Selección documental en [evidencia/portal-perfil-20261027](../competencias-digitales/evidencia/portal-perfil-20261027/):

- [Perfil escritorio](../competencias-digitales/evidencia/portal-perfil-20261027/perfil-organizado-1366.png) y [perfil móvil](../competencias-digitales/evidencia/portal-perfil-20261027/perfil-organizado-390.png).
- [Portal escritorio](../competencias-digitales/evidencia/portal-perfil-20261027/public-after-1366.png) y [portal móvil](../competencias-digitales/evidencia/portal-perfil-20261027/public-after-390.png).
- [Pie público](../competencias-digitales/evidencia/portal-perfil-20261027/footer-1366.png) y [pie móvil](../competencias-digitales/evidencia/portal-perfil-20261027/footer-390.png).
- Informes JSON de navegador, flujo, HTTP, hashes y paquete junto a las capturas. Identidades, fotos y adscripciones de Competencias son sintéticas.

## Comandos y límites

Desde ClientApp: `npm run types:check`, `npm run build`, `npm run test:forms`.

Desde la raíz:

```powershell
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj -c Release --no-restore
dotnet build tests/TDV2.NativeVerification/TDV2.NativeVerification.csproj -c Release --no-restore
$env:TDV2_VISUAL_ARTIFACTS = Join-Path (Get-Location) '.artifacts/public-profile-20261027'
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public "$env:TDV2_VISUAL_ARTIFACTS/public-after.html"
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets "$env:TDV2_VISUAL_ARTIFACTS/public-assets.json"
# Desde ClientApp, conservando la ruta absoluta:
node tests/browser/quiz-inter-ui.mjs after
# Desde raíz; excluye expresamente reinicios:
$env:TDV2_DIGITAL_PRESENTATION_ONLY = '1'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -DigitalOnly -BrowserOnly -NoBuild
# Paquete local, sin iniciar aplicación ni desplegar:
dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/public-profile-20261027/publish
```

El publish normal encontró rolldown bloqueado por otro proceso Windows durante npm ci; no se cerró ese proceso. **Publicación local completada** desde una copia de fuentes en `.artifacts/public-profile-20261027/publish-source`, con cachés locales y el mismo target. Se restauraron los archivos faltantes de node_modules desde esa instalación de las mismas versiones, sin sobrescribir binarios bloqueados; TypeScript volvió a pasar y package-lock conserva su hash. No se cambió el target ni se reemplazó la compilación por assets antiguos.

Los **16 recursos seleccionados del paquete** (PDF, Noto/estilo/licencia, cinco firmas y documentos/lógica del juego) coinciden por hash con sus fuentes. La suite pública pasó usando `TDV2_PUBLIC_ROOT` apuntado a `publish/wwwroot`: **12 grupos**, incluidos tres tamaños, seis partidas y cierre/teclado. Los tres enlaces PDF abren una pestaña con la URL exacta y sin window.opener; el contenido se comprueba por HTTP, firma PDF, metadatos y hash. El visor PDF nativo no se acredita mediante capturas headless.

Comando usado tras preparar la copia de fuentes, sin copiar secretos ni configuraciones locales:

```powershell
$env:npm_config_cache = Join-Path (Get-Location) '.artifacts/npm-cache'
$env:npm_config_offline = 'true'
$env:npm_config_audit = 'false'
dotnet publish .artifacts/public-profile-20261027/publish-source/tdv2/tdv2.csproj -c Release -p:UseAppHost=false "-p:RestorePackagesPath=$(Join-Path (Get-Location) '.artifacts/nuget')" -o .artifacts/public-profile-20261027/publish
$env:TDV2_PUBLIC_ROOT = Join-Path (Get-Location) '.artifacts/public-profile-20261027/publish/wwwroot'
# Desde ClientApp, con TDV2_VISUAL_ARTIFACTS absoluto:
node tests/browser/quiz-inter-ui.mjs after
```

Persisten las advertencias anteriores de NuGet sobre System.Security.Cryptography.Pkcs 9.0.18; restaurar sin acceso al índice de vulnerabilidades puede emitir NU1900. No se actualizan dependencias. Las advertencias Vite sobre fuentes corresponden a archivos estáticos fuera de su raíz: se comprueba su entrega por HTTP y publicación.

Móvil emulado, no dispositivos físicos; fuentes institucionales/Microsoft/Nexo simuladas. La foto externa de portada está bloqueada en el navegador aislado; su comportamiento anterior se mantiene. Esta revisión no acredita integración institucional real ni despliegue Ubuntu. Quedan la publicación autorizada y comprobaciones del dominio real/MIME/proxy, junto con los pendientes administrativos previos. No se ejecutan cambios institucionales, secretos, permisos, reinicios, push o despliegue.
