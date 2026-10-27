# Navegación, test y pies institucionales — 2026-10-27

**Corrección posterior:** se revierte únicamente la prioridad/agrupación del menú, se amplía la distribución del pie interno y se retira una oración del bloque público del test. Las demás mejoras se conservan. El orden y la evidencia siguientes documentan esta entrega anterior; consultar la [corrección vigente](CORRECCION_MENU_PIE_TDV2_20261027.md).

## Implementado

### Competencias digitales

Se conserva el contenedor común de hasta **1400 px**, sus márgenes adaptables, cabecera UACJ/cuenta, fotografía y nombre accesible en móvil. Noto Sans local, azul `#213455`, contraste, recursos y tamaños de las firmas permanecen vigentes. El pie sigue mostrando Dirección General, Desarrollo de Sistemas, Tecnología Educativa, Operación y Redes e Inteligencia de Datos; UACJ permanece en la cabecera.

La navegación presenta exclusivamente las opciones autorizadas recibidas del servidor, en este orden:

| Posición visual | Módulo | Ruta conservada |
|---|---|---|
| 1 | Encuesta | `/competencias-digitales/encuesta` |
| 2 | Mi informe y ruta | `/competencias-digitales/mi-informe-ruta` |
| 3 | Avance por dependencia | `/competencias-digitales/avance-dependencia` |
| 4 | Resultados generales | `/competencias-digitales/resultados-generales` |
| 5 | Configuración | `/competencias-digitales/configuracion` |

Tres grupos accesibles —Mi diagnóstico, Reportes y Administración— separan el recorrido personal de los reportes y la configuración mediante espacios/divisores. Se omiten grupos vacíos. La navegación ocupa su propia fila y permite envolver las opciones en móvil. El observador existente sigue ajustando el desplazamiento de captura a la altura real de la cabecera. No se modifican índices de página, rutas, IDs, permisos, roles ni lógica de navegación/guardado.

**Origen del orden y Nexo:** `PostgresNexoProfiles` consulta `public.nexo_modulos` con `ORDER BY m.orden,m.id`, unido a `nexo_modulo_rol` y limitado a la aplicación y roles. Sin embargo, `CompetenciasController` construye su navegación mediante `Enumerable.Range(0,5).Where(scope.Module)` y los arrays históricos de `DigitalRoutes`. Ahora el componente ordena esas opciones ya concedidas por sus rutas exactas. No cambia los arrays ni sus índices, que utiliza la autorización del backend. **No hace falta un ajuste operativo en Nexo para este menú**; las posiciones anteriores son de presentación y no son IDs nuevos. No se consultaron ni modificaron datos reales de Nexo.

El diálogo compartido muestra **Aviso de privacidad** tanto para lectura como para aceptación. Se cambia únicamente `DialogTitle`: el JSON del instrumento, el título histórico almacenado, el cuerpo, la confirmación y la versión `uacj-digital-aviso-2026-10-27.1` se conservan byte por byte. No se solicitan nuevas aceptaciones ni se modifican consentimientos por este ajuste.

Avance y Resultados utilizan **CVE_UR - DESC_UR** en contexto, opciones, filas y referencias a la unidad superior. La tabla deja de añadir el nivel técnico junto al nombre. El código revisado no contenía una etiqueta literal «Tipo UR N»; se retira la anotación equivalente de nivel en la fila, sin transformar descripciones del catálogo mediante coincidencias de texto. Se mantienen los filtros de nivel para la vista institucional y los datos internos de jerarquía/alcance. El título de la recomendación de acompañamiento agrega la clave de la misma unidad seleccionada por el cálculo existente. La exportación conserva sus columnas separadas de clave y nombre; cálculos y selección de recomendaciones permanecen intactos.

### Portal y test Microsoft 365

El acceso principal sigue siendo **Responder encuesta de competencias digitales**, con destino `/competencias-digitales`. Debajo aparece el encabezado de tercer nivel **¿Qué tan bien dominas Microsoft 365?**, el texto solicitado «Pon a prueba tus conocimientos de Microsoft 365. Este test no sustituye la encuesta de competencias digitales.» y el botón secundario **Iniciar test**. Las ocho dimensiones y el resto del contenido no cambian.

«¿Cómo funciona?» abre una sección dentro del test, sin alerta nativa ni segundo diálogo. Explica las diez preguntas, explicación después de responder, XP (100 por acierto más 20 por cada acierto anterior de la racha), rachas, porcentaje/nivel y medallas. Se retira el texto de funcionalidades futuras. «Entendido» y Escape cierran la sección y devuelven el foco; `aria-expanded` refleja su estado. Abrirla enfoca el título y lo desplaza a la vista, también dentro del iframe. El iframe conserva la altura disponible del diálogo y desplaza su documento naturalmente; no se agregan alturas mínimas ni ventanas superpuestas.

El Escape consumido por las instrucciones ya no cierra además el diálogo exterior. Se conserva el cierre normal del test y su estado al volver a abrirlo. La decoración de la portada del test queda detrás del contenido para conservar el contraste de su título. El código original de preguntas, respuestas, evaluación, XP, rachas y resultados sigue siendo idéntico: de `quiz.js` se retira exclusivamente la función de alerta; la interacción de instrucciones vive en `quiz-controls.js`.

### Pie interno de TDV2

`InstitutionalFooter` añade la variante `internal` y reutiliza los recursos y estilos del pie público: **UACJ → Dirección General → Desarrollo de Sistemas → Tecnología Educativa → Operación y Redes → Inteligencia de Datos**. El layout autenticado lo incorpora en todas sus pantallas, debajo del contenido; también aparece en la vista independiente de acceso restringido de TDV2. La variante de Competencias conserva sus cinco firmas.

El pie interno comparte la regla de ancho máximo y márgenes de `.page` mediante `.app-footer-inner`, sin identificarse como una segunda página. El layout vigente tiene navegación superior y menú móvil; no existe una barra lateral global fija que requiera un desplazamiento inventado. La navegación y tablas del formulario quedan dentro del contenido, antes del pie. El contenedor vertical flexible conserva el pie al fondo con poco contenido y después de páginas largas, sin posición fija.

## Archivos modificados

- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx` y `resources/css/competencias-digitales.css`: orden y grupos del menú; se conserva la distribución de 1400 px.
- `ClientApp/resources/js/competencias/PrivacyNotice.tsx`, `Aggregates.tsx`: etiqueta del aviso y nombres institucionales de áreas.
- `tdv2/Competencias/DigitalAggregates.cs`: únicamente el título de la recomendación de acompañamiento, incluyendo la clave de su unidad.
- `ClientApp/resources/js/Components/InstitutionalFooter.tsx`, `Layouts/AuthenticatedLayout.tsx`, `pages/AccesoRestringido.tsx` y `resources/css/app.css`: pie compartido y geometría del contenedor en TDV2.
- `tdv2/Views/Home/Index.cshtml`: título/explicación y jerarquía del acceso al test.
- `tdv2/wwwroot/quiz/microsoft-365/index.html`, `quiz.js`, `quiz-controls.js`, `quiz-brand.css`: instrucciones integradas y foco.
- `tdv2/wwwroot/js/public-quiz.js`: Escape respetando el cierre de las instrucciones.
- `ClientApp/tests/browser/identity-digital-ui.mjs`, `quiz-inter-ui.mjs`: cobertura ampliada.
- README, ESTADO_MIGRACION, guía del diagnóstico, esta nota y evidencias.

No se modificaron temas, fuentes, colores compartidos, imágenes, contratos del instrumento, migraciones, secretos ni conexiones. Se conservan los cambios locales anteriores. Los hashes del instrumento, Survey, informe personal, tema, CSS institucional, autorización y servicio de captura coinciden con el estado anterior.

## Verificación y límites

- TypeScript y Vite correctos. ASP.NET/ejecutable de verificación Release compilados sin errores; persisten las advertencias anteriores NU1902/NU1903 de Pkcs.
- Competencias: siete anchos con Vite y siete con bundles (1920, 1440, 1366, 1024, 768, 390 y 320 px). Contenedor, alineación, cuenta/teclado, firmas, perfil, preguntas, informe, privacidad de lectura, representación y recursos locales verificados. Sin errores JavaScript ni desbordamiento horizontal del documento.
- Navegación/reportes: seis conjuntos sintéticos de concesiones en cuatro anchos —participante, responsable, supervisor, administrador, solo informe y solo reporte—. Orden, grupos vacíos omitidos, configuración ausente cuando no está concedida, filtros institucionales/por rama, nombres completos y unidad superior. Prueba adicional de foco pendiente con menú completo del administrador, incluida pantalla de 320 px.
- Portal/test/TDV2: **34 comprobaciones** en cinco tamaños (1366, 1024, 768, 390 y 320 px), sin errores JavaScript. Texto y accesos separados, encabezado semántico, panel de ayuda/teclado/Escape, progreso conservado al cerrar y abrir el test a mitad del juego, resultados originales de 10/7/5/0 aciertos, firmas públicas/internas iguales, inicio vacío, acceso restringido, tablas y pie en estados largos/cortos/carga/error.
- **24 comprobaciones de tablas**: grupos, filtros, teclado, columnas fijas, presencia de dos usuarios, reservas, autoguardado, recarga y acciones con muchas filas, también en móvil/zoom. **Cuatro comprobaciones de colaboración/Guardar/Enviar** correctas. El primer recorrido de colaboración detectó dos elementos `.page` al incluir el pie; se corrigió la clase del contenedor a `.app-footer-inner`, compartiendo la misma geometría CSS. La repetición conserva las acciones y el envío confirmado por servidor sintético.
- **37 recursos públicos** comprobados por HTTP mediante ASP.NET TestServer, sin sesión institucional. Se incluyen las instrucciones y sus estilos/scripts, fuentes, imágenes y PDF existentes.

La prueba abre React/MUI/Vite y Edge reales, con respuestas de negocio e identidades sintéticas. Los filtros recibidos simulan el conjunto autorizado; esta revisión no revalida autorización SQL, integración Microsoft/Nexo ni persistencia PostgreSQL. No se ejecutaron bases, migraciones ni reinicios de encuestas, tampoco push o despliegue. Móvil/tableta emulados; queda pendiente la comprobación en dispositivos físicos y sesión institucional durante una fase operativa autorizada. No hay ajuste de Nexo requerido por esta entrega.

El modo abreviado de navegación requirió registrar también sus respuestas HTTP en el informe del arnés; su primer intento comprobó la UI pero no completó esa aserción final de recursos. Se corrigió el arnés y la repetición correcta es la evidencia entregada. La advertencia HMR previa permanece en el arnés Vite.

## Evidencia y repetición

| Vista | Escritorio | Móvil |
|---|---|---|
| Menú completo de Competencias | [Cabecera y pie](evidencia/navegacion-test-pies-20261027/digital-navigation-admin-1366.png) | [Navegación](evidencia/navegacion-test-pies-20261027/digital-navigation-admin-390.png) |
| Reportes: clave y nombre | [Avance](evidencia/navegacion-test-pies-20261027/digital-avance-dependencia-1366.png) | [Resultados](evidencia/navegacion-test-pies-20261027/digital-resultados-generales-390.png) |
| Ayuda integrada al test | [Instrucciones](evidencia/navegacion-test-pies-20261027/quiz-help-1366.png) | [Instrucciones](evidencia/navegacion-test-pies-20261027/quiz-help-390.png) |
| Pie de TDV2 | [Firmas](evidencia/navegacion-test-pies-20261027/internal-footer-1366.png) | [Inicio vacío](evidencia/navegacion-test-pies-20261027/internal-inicio-390.png) |

[Portal actualizado](evidencia/navegacion-test-pies-20261027/public-after-1366.png) · [Pie de tableta](evidencia/navegacion-test-pies-20261027/internal-footer-768.png) · [Foco con menú completo a 320 px](evidencia/navegacion-test-pies-20261027/digital-survey-admin-320.png).

[Vite](evidencia/navegacion-test-pies-20261027/digital-vite.json) · [Bundles](evidencia/navegacion-test-pies-20261027/digital-build.json) · [Navegación/foco](evidencia/navegacion-test-pies-20261027/navigation.json) · [Portal/test/TDV2](evidencia/navegacion-test-pies-20261027/portal.json) · [Tablas](evidencia/navegacion-test-pies-20261027/tables.json) · [Colaboración](evidencia/navegacion-test-pies-20261027/collaboration.json) · [HTTP](evidencia/navegacion-test-pies-20261027/public-assets.json).

Desde la raíz, con Node/.NET disponibles:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj --no-restore -c Release -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/navigation-footer-20261027/portal/public-after.html
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-public-assets .artifacts/navigation-footer-20261027/portal/public-assets.json
```

Desde `ClientApp`, en una consola de pruebas temporal:

```powershell
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/navigation-footer-20261027/portal'
node tests/browser/quiz-inter-ui.mjs after
$env:TDV2_TEST_VITE='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/navigation-footer-20261027/digital-vite'
node tests/browser/identity-digital-ui.mjs
Remove-Item Env:TDV2_TEST_VITE
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/navigation-footer-20261027/digital-build'
node tests/browser/identity-digital-ui.mjs
```

Regresiones adicionales: `node tests/browser/visual-tables-ui.mjs` y `node tests/browser/collaboration-actions-ui.mjs`, usando una carpeta distinta en `TDV2_VISUAL_ARTIFACTS` para cada ejecución. Para repetir únicamente navegación/reportes/foco, usar `TDV2_NAVIGATION_ONLY=1` con `identity-digital-ui.mjs`; quitar esa variable al repetir la suite completa.

Los directorios deben existir para las exportaciones .NET. Los arneses de navegador los crean al iniciar; todos usan puertos loopback aleatorios y evitan `.env`, User Secrets y servicios institucionales.
