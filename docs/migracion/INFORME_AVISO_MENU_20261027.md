# Mi informe y ruta, aviso vigente y orden publicado por Nexo — 2026-10-27

Esta revisión sustituye las instrucciones históricas de orden del menú y texto del aviso. Conserva los cambios anteriores de 1400 px, cabecera, cuenta, fotografía, pie, Noto Sans y colores institucionales. No modifica portal público, Procedimientos, captura, fórmulas definitivas, permisos ni migraciones.

## Implementado

**Informe personal.** Resumen con identidad/contexto, estado, índice global y cinco bloques; recomendaciones agrupadas en prioridades, siguientes pasos y fortalezas sin cambiar sus textos; ruta con fases, periodos, propósito, nivel objetivo, cursos, licencias e indicadores por desarrollar. Se conserva la expansión de propuestas restantes. El detalle usa pestañas accesibles y una sola tabla de ancho completo; hasta 700 px usa selector y fichas con Herramienta o tema, Tu nivel, Esperado y Estado. Nombres completos, saltos de línea naturales y sin desplazamiento vertical interno. Seleccionar un bloque no filtra el resumen, las recomendaciones ni la ruta.

**Estados reales.** `DigitalSurveyView.HasResponses` procede del backend: exige una respuesta explícita, válida y aplicable a una herramienta; perfil institucional, perfil laboral y estados `pending` no cuentan. Sin respuestas se muestra «Todavía no tienes un informe» y «Ir a la encuesta» si ese módulo está concedido. Una captura parcial muestra «Informe preliminar» y «Continuar encuesta». Cero real y la rúbrica explícita «No realizo ninguna» son válidos. No aplica conserva su valor sin producir puntuaciones ficticias.

En la **proyección preliminar** una complementaria sin selección se presenta como «Sin seleccionar», sin brecha ni curso derivado de un valor inicial. Una complementaria seleccionada pero pendiente continúa pendiente; «No utiliza» explícito conserva su significado. Las preguntas exclusivas del perfil siguen las reglas existentes. Se reutilizan las mismas funciones de recomendaciones y capacitación sobre respuestas reales, sin cambiar requisitos, umbrales, medias ni avance/validación de captura. El tratamiento definitivo original de las complementarias no seleccionadas se conserva. `Read` mantiene su transacción y control de generación; `Project` separa sólo la construcción de la respuesta para poder verificarla sin base.

**Historia.** Si existe `ReportJson`, se entrega intacto, con instrumento y contexto guardados. No se reevalúa su contenido para sustituir el informe; una prueba coloca un resultado histórico deliberadamente distinto y comprueba igualdad exacta del JSON recuperado. La extracción de `BuildReport` conserva el cálculo definitivo existente. No se incorporan descargas, edición o reapertura.

## Aviso de privacidad

Versión vigente: **`uacj-digital-aviso-2026-10-27.2`**. Encabezado: **Aviso de privacidad**. El cuerpo es el texto proporcionado por el usuario, incorporado literalmente:

> Los datos personales proporcionados serán utilizados únicamente para los fines de registro y estadística dentro la aplicación de Diagnóstico de Madurez Digital de las Competencias Digitales del Personal de la Universidad Autónoma de Ciudad Juárez, con el propósito de identificar las competencias digitales del personal y orientar las acciones de capacitación en el marco del proyecto de Transformación Digital Institucional. Para conocer el tratamiento de sus datos personales, consulte el Aviso de Privacidad de la UACJ en:

Inmediatamente después se muestra [https://www.uacj.mx/transparencia/aviso_privacidad.html](https://www.uacj.mx/transparencia/aviso_privacidad.html), con `target="_blank" rel="noopener noreferrer"`; puede dividirse en móvil sin desbordar. No se modifica el texto de confirmación existente.

Fuente única actual: `tdv2/Contracts/competencias-digitales-v1.json:notice`. El diálogo de captura y la consulta desde cuenta usan el mismo recurso. La versión técnica del instrumento, preguntas, rúbricas y cursos permanecen idénticos. La [versión anterior](../competencias-digitales/avisos/uacj-digital-aviso-2026-10-27.1.json) se conserva como copia documental exacta del recurso previo, sin publicarla como aviso vigente.

`cd_consents` ya identifica aceptaciones mediante empleado + versión. Los métodos de aceptación/guardado no cambiaron: agregan la aceptación vigente con fecha del servidor, preservan anteriores, exigen consentimiento antes de guardar y rechazan versiones antiguas. No se ejecuta ninguna actualización de registros existentes. En captura se solicita la versión nueva explícitamente; abrir/cerrar/consultar no acepta. Una finalizada continúa cerrada, puede leer el nuevo aviso y no registra una aceptación nueva por esa consulta. Representación sigue sin aceptar ni escribir. La incorporación del texto proporcionado no constituye una revisión jurídica independiente.

## Menú: autoridad y operación posterior

Se encontró la posición publicada en **`PostgresNexoProfiles`**, cuya consulta a `public.nexo_modulos` selecciona `m.orden` y ordena por **`m.orden, m.id`**. El DTO `Module` no expone un campo numérico de posición, pero la colección preserva el orden SQL. No fue necesario cambiar ese contrato compartido.

El controlador de Competencias estaba sustituyendo esa secuencia con `Enumerable.Range(0,5)`. Ahora `DigitalScope.MenuPages` recorre los módulos publicados, filtra rutas conocidas y permisos efectivos mediante el mismo `Module(i)` y conserva el orden recibido. El frontend únicamente representa `digital.navigation`, sin ordenar, agrupar ni destacar opciones. Las constantes de índices/rutas permanecen intactas.

El operador debe revisar **Posición en menú** de los módulos de esta aplicación en la administración de Nexo:

| Posición | Módulo existente | Ruta existente |
| --- | --- | --- |
| 1 | Encuesta | `/competencias-digitales/encuesta` |
| 2 | Mi informe y ruta | `/competencias-digitales/mi-informe-ruta` |
| 3 | Avance por dependencia | `/competencias-digitales/avance-dependencia` |
| 4 | Resultados generales | `/competencias-digitales/resultados-generales` |
| 5 | Configuración | `/competencias-digitales/configuracion` |

No se inventan IDs/claves ni se cambian concesiones. Configuración conserva exclusividad administrativa; reportes conservan alcance por rama; participante sólo ve sus módulos concedidos. Una concesión exclusiva de informe no recibe botón a una encuesta no autorizada. **No se consultaron ni modificaron posiciones institucionales reales**: si todavía difieren, la interfaz las respetará hasta que el operador coloque estas posiciones. Las capturas usan una publicación sintética con ese orden. Otra prueba publica el orden inverso y confirma que el backend lo conserva.

## Archivos

- `ClientApp/resources/js/competencias/PersonalReport.tsx`: jerarquía, estados, selector y fichas.
- `ClientApp/resources/css/competencias-digitales.css`: estilos exclusivos de informe; retirados los estilos de varias tablas estrechas.
- `ClientApp/resources/js/competencias/types.ts`: indicador de respuestas reales.
- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`: transmite la concesión de Encuesta al informe.
- `ClientApp/resources/js/competencias/PrivacyNotice.tsx`: URL institucional visible y segura.
- `tdv2/Competencias/DigitalInstrument.cs`: proyección preliminar y constructor común del informe.
- `tdv2/Competencias/DigitalSurveyService.cs`: proyección comprobable y selección del informe persistido.
- `tdv2/Competencias/DigitalAccess.cs`, `tdv2/Controllers/CompetenciasController.cs`: conservación del orden publicado, con filtros existentes.
- `tdv2/Contracts/competencias-digitales-v1.json`, `docs/competencias-digitales/avisos/uacj-digital-aviso-2026-10-27.1.json`: aviso vigente y archivo documental anterior.
- `tests/TDV2.Verification/DigitalPresentationTests.cs`, `EntryPoint.cs`: pruebas de proyección/orden y exportación sintética.
- `ClientApp/tests/browser/digital-report-ui.mjs`, `identity-digital-ui.mjs`: estados, accesibilidad, privacidad, menú y regresiones.
- README, ESTADO_MIGRACION, GUIA y este documento/evidencias.

Por SHA-256 se comprobó que no cambiaron portal Razor, `Survey.tsx`, `AccountMenu.tsx`, `InstitutionalFooter.tsx`, `app.css` e `institutional.css`. No se modificaron las migraciones ni los métodos de escritura de encuesta/aceptación/finalización.

## Verificado

- `npm run types:check`: correcto.
- `npm run build`: correcto; recursos locales compilados.
- ASP.NET Release y ejecutable de verificación: compilación correcta.
- Suite ASP.NET TestServer/domain sin base: **116/116**.
- Nueva suite backend: **24/24**, sin conexiones. Incluye perfil/autocompletado/pending, cero, No aplica, complementarias, aplicabilidad docente, media/avance sin cambios, final histórico exacto, aviso actual sobre instantánea anterior y menú por rol/orden publicado.
- Navegador Edge con bundles reales y proyecciones exportadas del backend: **32 grupos** (7 estados + flujo de aviso por cada ancho 1440, 768, 390 y 320). Cinco bloques/pestañas, tabla única/fichas, nombres completos, resumen/ruta invariables al seleccionar, avisos, cero, definitivas sin captura, footer natural y sin desbordamiento. Fuente efectiva inspeccionada mediante CDP: Noto Sans SemiBold local en etiquetas; brechas `rgb(240,189,16)`.
- Regresión de navegación: seis conjuntos de concesiones en cuatro anchos (1366,768,390,320), sin grupos, reportes por alcance, etiquetas institucionales y foco de primer pendiente con menú completo. Sin errores JS.
- Inspección visual de capturas de resumen, recomendaciones, fases, detalle, vacío, aviso, menú y pie. `git diff --check` correcto.

Los procesos de verificación terminaron; los procesos de Node anteriores del entorno se conservaron. Las advertencias NuGet NU1902/NU1903 de `System.Security.Cryptography.Pkcs 9.0.18` ya existentes quedan para otra tarea por indicación del usuario; no se actualizaron paquetes.

### Comandos reproducibles (desde la raíz salvo indicación)

```powershell
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj --no-restore -c Release -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-digital-presentation .artifacts/report-notice-20261027/projections.json
# Dentro de ClientApp, con Node disponible en PATH:
npm run types:check
npm run build
node tests/browser/digital-report-ui.mjs
$env:TDV2_NAVIGATION_ONLY='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/report-notice-20261027/regression'
node tests/browser/identity-digital-ui.mjs
```

### Evidencia visual

Todas las identidades y datos son sintéticos. Las capturas definitivas usan informes completos creados por el evaluador en memoria; no se finalizó ninguna encuesta real.

| Estado/vista | Escritorio | Tableta / móvil |
| --- | --- | --- |
| Sin respuestas | [1440 px](evidencia/informe-aviso-20261027/empty-1440.png) | [390 px](evidencia/informe-aviso-20261027/empty-390.png) |
| Preliminar | [1440 px](evidencia/informe-aviso-20261027/partial-1440.png) | [768 px](evidencia/informe-aviso-20261027/partial-768.png), [390 px](evidencia/informe-aviso-20261027/partial-390.png) |
| Cero válido | [1440 px](evidencia/informe-aviso-20261027/zero-1440.png) | Incluido en las 32 comprobaciones |
| Definitivo | [1440 px](evidencia/informe-aviso-20261027/final-1440.png) | [390 px](evidencia/informe-aviso-20261027/final-390.png) |
| Detalle por bloque | [1440 px](evidencia/informe-aviso-20261027/detalle-1440.png) | [768 px](evidencia/informe-aviso-20261027/detalle-768.png), [fichas 390 px](evidencia/informe-aviso-20261027/detalle-390.png) |
| Aviso nuevo | [1440 px](evidencia/informe-aviso-20261027/aviso-1440.png) | [390 px](evidencia/informe-aviso-20261027/aviso-390.png) |
| Menú administrativo | [1366 px](evidencia/informe-aviso-20261027/menu-administrador-1366.png) | [390 px](evidencia/informe-aviso-20261027/menu-administrador-390.png) |

[Informe completo con recomendaciones y fases](evidencia/informe-aviso-20261027/informe-completo-1440.png). [Registro de navegador y fuentes efectivas](evidencia/informe-aviso-20261027/report.json).

## Límites y pendientes reales

- No se ejecutaron PostgreSQL ni pruebas NativeVerification en esta revisión, pues su preparación aplica migraciones y contiene reinicios. No se atribuyen los resultados del transporte simulado a persistencia real. La conservación de aceptaciones se verifica en el flujo sintético y por inspección del mecanismo existente; su transacción PostgreSQL no se volvió a ejecutar.
- Pendiente, en una fase operativa autorizada: ajustar las posiciones de módulos en Nexo y comprobar publicación/visibilidad con cuentas reales; comprobar en un entorno aislado con esquema ya preparado la nueva aceptación por versión y permanencia de filas anteriores. No requiere migración nueva.
- Navegador real con tamaños emulados; pendiente aceptación en dispositivos físicos. No se comprobó la disponibilidad remota del sitio institucional del enlace, sólo URL exacta, texto y navegación segura.
- Sin bases institucionales, secretos, permisos Nexo, migraciones, reinicios, push, despliegues ni actualización NuGet.
