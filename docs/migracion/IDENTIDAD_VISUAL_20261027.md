# Identidad visual compartida — 2026-10-27

Esta revisión sustituye las indicaciones visuales anteriores sobre Inter/Roboto, orden de firmas y pie de Competencias limitado a cuatro subdirecciones. Conserva el trabajo local previo, los contratos y la lógica de negocio. No crea migraciones ni ejecuta migraciones, reinicios, conexiones institucionales, cambios de secretos/Nexo, push o despliegue.

## Implementado

### Colores y fuentes

`ClientApp/resources/js/design-tokens.json` es la fuente común de colores. MUI la importa; `scripts/build-design-tokens.mjs` genera `wwwroot/css/design-tokens.css` antes de `npm run dev/build`. El CSS generado se incluye también en Git y en el paquete para servir Razor sin arrancar Vite. `institutional.css` importa los tokens y las fuentes locales.

| Uso | Valor |
|---|---|
| Marca, acciones principales, navegación, enlaces y pies institucionales | `#213455` |
| Advertencias, indicador activo de Competencias, brechas y prioridad Media | `#F0BD10` |
| Bordes y separadores claros | `#DADADA` |
| Superficies de lectura | Blanco y neutros claros |

Las advertencias conservan texto oscuro e iconos/etiquetas; MUI utiliza fondo amarillo suavizado y borde institucional. Las brechas mantienen la etiqueta y el icono, sin convertirse en errores rojos. Se conservan verdes/rojos semánticos y la paleta independiente de presencia de editores; no se iguala su color al encabezado de tablas. Los niveles de competencia siguen diferenciados.

Noto Sans se aplica al portal, Procedimientos, Competencias y al documento independiente del juego Microsoft 365. `wwwroot/fonts/noto-sans/noto-sans.css` declara los cuatro TTF existentes. La inspección binaria de `name` y `OS/2` confirmó:

| Archivo | Familia interna / familia tipográfica | Peso real |
|---|---|---:|
| NotoSans-Regular.ttf | Noto Sans / Regular | 400 |
| NotoSans-Medium.ttf | Noto Sans Medium / Noto Sans, Medium | 500 |
| NotoSans-SemiBold.ttf | Noto Sans SemiBold / Noto Sans, SemiBold | 600 |
| NotoSans-Bold.ttf | Noto Sans / Bold | 700 |

Se conservan licencia OFL y procedencia documentadas en la carpeta de fuentes. No se usan Google Fonts ni fuentes remotas. Los archivos Inter/Roboto antiguos se mantienen como recursos históricos, sin cargarlos en estas pantallas. El CSS original del juego se conserva íntegro; `quiz-brand.css` aplica la identidad y pesos disponibles, sin modificar preguntas, puntuación ni JavaScript del juego. El logotipo Microsoft conserva sus colores originales.

El manual local `colores.pdf` disponible tiene seis páginas de paleta y confirma los tres HEX principales. Su SHA-256 es `1D99B0E50EDD3EAA3707121CF084BCF546A187017E90EB3E4C29A9BF94876E04`. No incluye la sección tipográfica. No se encontraron archivos Avenir/Avenirs en el proyecto ni al buscar recursos por nombre en Downloads, Documents y Windows Fonts. **Avenirs LS STD no se declara ni se presenta como instalada**: para incorporarla hace falta el TTF/OTF autorizado, con licencia y pesos correspondientes; se verificará su nombre interno. Los titulares usan Noto Sans mientras tanto.

### Firmas y flujo de página

- Portal: **UACJ → DGTI → Desarrollo de Sistemas → Tecnología Educativa → Operación y Redes → Inteligencia de Datos**.
- Competencias: cabecera con UACJ y cuenta/foto existentes; pie **DGTI → Desarrollo de Sistemas → Tecnología Educativa → Operación y Redes → Inteligencia de Datos**. Se conserva el aviso de lectura en la cuenta y la salida de representación.
- Áreas: texto HTML Noto Sans, prefijo 400 y nombre 700. DGTI 16 px (17 en móvil); todas las subdirecciones 14 px, mismo interlineado 1.4, alineación y ancho de columna. Los nombres cortos no se escalan. Separadores blancos finos y fondo institucional.
- La firma UACJ mantiene su dibujo y proporciones originales. El recurso disponible `firma-1.png` es una composición UACJ/DGTI de 1186×112. Una ventana CSS de proporción 560:112 muestra la región completa UACJ/Inspira el Futuro situada antes del separador; la región DGTI se presenta aparte mediante Noto Sans. **No se modifica, deforma ni redibuja el archivo ni se cortan letras del logotipo UACJ.** No se encontró una versión oficial independiente de UACJ/Inspira el Futuro; puede sustituirse la referencia por ese recurso cuando se facilite, conservando la composición. Las firmas gráficas anteriores siguen disponibles sin alteraciones.
- En escritorio las firmas caben en una franja; al faltar ancho se distribuyen en filas, conservando el orden del documento. Se retiraron las reglas antiguas del pie CD que forzaban una columna y el ancho móvil de la imagen compuesta.
- Los contenedores mantienen altura mínima de ventana, contenido flexible y pie en flujo normal. No hay pie fijo ni alturas artificiales para forzar desplazamiento.
- La columna lateral del portal pasa debajo del contenido a 1120 px: corrige el desbordamiento anterior observado a 1024 px con nombres y textos completos. El resto de puntos adaptables y el contenido se conservan.

### Fotografía y texto público

La fotografía entregada se conserva byte por byte en `/images/portal/img_9885.jpg`: **6240×4160**, orientación EXIF **1**, 7 123 952 bytes, SHA-256 `CFC5723062CC8F52C8FCE483F07F039D07DC24A692FB18EE4866C4F900B1ED93`.

Derivadas JPEG de calidad 86, escalado bicúbico, sin recorte ni cambio de proporción:

| Ruta | Tamaño | Bytes |
|---|---|---:|
| `/images/portal/img_9885-1920.jpg` | 1920×1280 | 349 352 |
| `/images/portal/img_9885-960.jpg` | 960×640 | 117 519 |

Escritorio usa 1920 px; móvil usa `image-set` de 960/1920 para densidad 1×/2×. `cover` y posiciones 50 %/55 % en escritorio, 40 %/55 % en tableta y 32 %/50 % en móvil mantienen el acceso al edificio y la persona en primer plano. La altura sigue al contenido. La capa CSS azul utiliza opacidades 0.80/0.74/0.68: no se tiñe el archivo. El contraste se comprueba incluso suponiendo blanco puro detrás de la capa más transparente.

El texto del diagnóstico es exactamente **«Una fotografía honesta de su área en estas 8 dimensiones:»**. Se conservan encuesta y juego separados, contacto, documentos públicos y el resto de textos. No se alteran instrumentos, resultados persistidos ni fórmulas.

## Archivos de esta revisión

| Grupo | Archivos |
|---|---|
| Tema compartido | `ClientApp/resources/js/design-tokens.json`, `theme.ts`, `ClientApp/resources/css/app.css`, `ClientApp/index.html`, `ClientApp/package.json`, `scripts/build-design-tokens.mjs` |
| Recursos comunes | `tdv2/wwwroot/css/design-tokens.css`, `institutional.css`, `fonts/noto-sans/noto-sans.css`, compatibilidad `public.css` |
| Portal y portada | `tdv2/Views/Home/Index.cshtml`, los tres JPEG de `wwwroot/images/portal/` |
| Competencias | `pages/CompetenciasDigitales.tsx`, `Components/InstitutionalFooter.tsx`, `resources/css/competencias-digitales.css` |
| Controles compartidos | `Components/AccountMenu.tsx`, `AreaDirectory.tsx`, `ModuleNavigation.tsx`, `PrioritySelect.tsx`, `pages/Colaboradores.tsx`: únicamente colores del tema en esta revisión |
| Documento independiente | `wwwroot/quiz/microsoft-365/index.html`, nuevo `quiz-brand.css` |
| Verificación | `tests/TDV2.Verification/PublicAssetsTests.cs`, `EntryPoint.cs`, `ClientApp/tests/browser/identity-checks.mjs`, `identity-digital-ui.mjs`, `quiz-inter-ui.mjs`; expectativas visuales actualizadas en `competencias-native.mjs`; raíz publicable opcional en `system-ui-fixture.mjs` |
| Documentación | README, ESTADO_MIGRACION, GUIA de Competencias, PORTAL_Y_PERFIL, INVENTARIO, esta guía y evidencias |

Las demás modificaciones pendientes de Git pertenecen al trabajo local conservado. No se cambiaron en esta revisión servicios de captura, autorización, evaluación, reinicio, catálogos ni contextos/migraciones. Se compararon hashes de los cinco archivos de migraciones/snapshot de Competencias antes y después.

## Comprobaciones y evidencia

- TypeScript, Vite y compilación Release ASP.NET: correctos; **95/95 pruebas frontend** y **116/116 de dominio/HTTP** con ASP.NET TestServer y dobles sin bases.
- **37 recursos HTTP anónimos**: fuentes, estilos, fotografías, PDF y juego; MIME, contenido y cabeceras comprobados. Los PDF siguen siendo públicos y no fuerzan descarga.
- Portal/juego: **19 grupos**, escritorio 1366, tableta 1024/768, móvil 390/320; 18 comprobaciones de fuente real mediante Chrome DevTools Protocol, cuatro pesos, caracteres españoles, contraste, firmas, contacto/documentos/enlaces y pies cortos/largos. Contraste mínimo conservador de portada **4.64:1**, cero solicitudes externas bloqueadas y cero errores JS. Seis partidas completas verifican preguntas, puntos, rachas, niveles y controles de teclado originales.
- Competencias: **cuatro recorridos visuales** en 1366/768/390/320. Perfil, rúbricas, informe, cabecera/cuenta, pie, avatar de respaldo, representación y aviso de lectura. Se comprueba foco del pendiente, paso bloqueado, conservación del valor ante guardado fallido y ausencia de aceptación por leer el aviso. **Proyecciones y respuestas HTTP sintéticas**: no equivalen a revalidar persistencia, evaluación o autorización institucional. El informe de las capturas es deliberadamente sintético.
- Tablas internas: **24 grupos** en 1366/390/320 y zoom CSS 200 %; controles, teclado, reservas entre actores, autoguardado/recarga, presencia y columnas Acciones fijas. **Cuatro grupos** adicionales de colaboración, Guardar/Enviar, confirmación, versión reciente y doble clic. React/MUI reales con transporte simulado, sin PostgreSQL.
- Se ejecutaron dos recorridos históricos que **no pasan contra el catálogo actual**: `system-tools-ui.mjs` busca la antigua opción «SIIv2» y `capture-scroll-ui.mjs` espera «Portal del CAST de uso de técnicos» como texto de un combobox que ahora es búsqueda. No se cambió el catálogo ni se ocultó el fallo. La cobertura vigente de estas pantallas está en `visual-tables-ui.mjs` y `collaboration-actions-ui.mjs`. Queda actualizar/retirar aquellos recorridos históricos en su revisión correspondiente.
- No se ejecutó la suite nativa PostgreSQL en esta revisión visual, para respetar la instrucción de no ejecutar migraciones ni reinicios. Las evidencias nativas anteriores permanecen fechadas en la guía de Competencias; no se presentan como nuevas.

Artefactos de trabajo: `.artifacts/identity-20261027/`. Capturas seleccionadas y reportes portables: [evidencia/identidad-20261027](evidencia/identidad-20261027/).

| Espacio | Escritorio | Móvil |
|---|---|---|
| Portada | [Fotografía](evidencia/identidad-20261027/hero-after-1366.png) | [Fotografía](evidencia/identidad-20261027/hero-after-390.png) |
| Pie público | [Franja](evidencia/identidad-20261027/footer-1366.png) | [Filas](evidencia/identidad-20261027/footer-390.png) |
| Competencias | [Perfil y firmas](evidencia/identidad-20261027/digital-profile-1366.png) | [Perfil y firmas](evidencia/identidad-20261027/digital-profile-390.png) |
| Procedimientos | [Tabla y presencia](evidencia/identidad-20261027/presence-1366-1x.png) | [Tabla y presencia](evidencia/identidad-20261027/presence-390-1x.png) |

### Repetición local

Desde la raíz, con Node/npm y .NET disponibles en PATH:

```powershell
Push-Location ClientApp
npm run types:check
npm run build
npm run test:forms
Pop-Location
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj --no-restore -c Release -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
New-Item -ItemType Directory -Force .artifacts/identity-20261027 | Out-Null
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets .artifacts/identity-20261027/public-assets.json
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/identity-20261027/public-after.html
Push-Location ClientApp
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/identity-20261027'
node tests/browser/quiz-inter-ui.mjs after
node tests/browser/identity-digital-ui.mjs
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/identity-20261027/tables'
node tests/browser/visual-tables-ui.mjs
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/identity-20261027/internal-regression'
node tests/browser/collaboration-actions-ui.mjs
Pop-Location
```

### Paquete local y límites

`dotnet publish` se ejecutó en una copia aislada de fuentes bajo `.artifacts/identity-20261027/publish-source`, conservando el target normal que ejecuta `npm ci` y `npm run build`. La copia incluye `scripts/build-design-tokens.mjs`, sin secretos, configuraciones locales ni archivos `.user`. Esto evita intervenir en procesos Vite/binarios bloqueados del espacio de trabajo. Los paquetes se restauran desde cachés locales; no se despliega ni inicia la aplicación institucional.

```powershell
# Desde la raíz, una vez preparada la copia de fuentes:
$env:npm_config_cache=Join-Path (Get-Location) '.artifacts/npm-cache'
$env:npm_config_offline='true'
$env:npm_config_audit='false'
dotnet publish .artifacts/identity-20261027/publish-source/tdv2/tdv2.csproj -c Release -p:UseAppHost=false "-p:RestorePackagesPath=$(Join-Path (Get-Location) '.artifacts/nuget')" -o .artifacts/identity-20261027/publish
```

Los recursos tienen rutas web locales y nombres con capitalización exacta, compatibles con Ubuntu. No hay dependencias de Downloads, rutas Windows ni `file://` durante la ejecución. Los avisos Vite sobre CSS estático fuera de su raíz se contrastan con las pruebas HTTP y la inclusión en el paquete.

**Paquete final comprobado:** 36 archivos estáticos seleccionados y 42 bundles actuales coinciden por SHA-256 con las fuentes/compilación local; la entrada React también coincide. Los 19 grupos de portal/juego, las pantallas internas de fuente y los cuatro recorridos de Competencias pasan con `TDV2_PUBLIC_ROOT` apuntando al `publish/wwwroot` aislado. Esto prueba los recursos del paquete con el servidor HTTP de pruebas; los MIME de ASP.NET se comprobaron separadamente con TestServer. [Integridad](evidencia/identidad-20261027/publish-integrity.json), [portal/juego del paquete](evidencia/identidad-20261027/publish-portal-report.json), [Competencias del paquete](evidencia/identidad-20261027/publish-digital-report.json).

Persisten las advertencias previas NU1902/NU1903 de `System.Security.Cryptography.Pkcs 9.0.18`; la restauración aislada puede emitir NU1900 al no acceder al índice de vulnerabilidades. No se actualizan dependencias dentro de este cambio visual. Pruebas con Edge y móvil emulado; no acreditan dispositivos físicos, todos los navegadores, conexiones reales ni despliegue Linux. Pendientes operativos: aprobación visual institucional, archivo autorizado Avenirs LS STD si se desea utilizarlo y comprobación de cachés/MIME/encuadre en el dominio tras una publicación expresamente autorizada.
