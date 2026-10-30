# Participación vigente, instrucciones e Inicial por categoría — 2026-10-29

**Revisión posterior vigente:** [Indicadores, submódulos y reapertura](INDICADORES_SUBMODULOS_REAPERTURA_20261029.md). La población se define por acceso vigente a la aplicación; sustituye el requisito de módulo Encuesta de esta entrega anterior. Los filtros ahora ofrecen 2–5, predeterminado 2. Se conservan instrucciones y equivalencias.

Esta revisión sustituye las metas manuales del avance descritas en documentos anteriores. No modifica el instrumento, las respuestas existentes, las aceptaciones, los permisos, la identidad Microsoft/Nexo ni los informes congelados. No crea migraciones.

## Revisión de las equivalencias

Se compararon `Contracts/competencias-digitales-v2.json`, `DigitalInstrument.Requirements/RulePosition/Academic/BuildReport`, la edición v1 y `required()` del HTML documental. Las once correspondencias solicitadas **ya estaban implementadas**; se conservan y se agregan aserciones explícitas, independientes del mapa leído. No se justifica crear otra edición de evaluación por cambios de ayuda visual.

| Perfil seleccionado y conservado | Regla original de evaluación |
| --- | --- |
| Dirección | Dirección |
| Subdirección | Subdirección |
| Coordinación | Jefatura de función |
| Jefatura de departamento | Jefatura de función |
| Jefatura de función | Jefatura de función |
| Responsable | Jefatura de función |
| Personal Académico | Personal docente |
| Asistente Profesional | Personal operativo |
| Apoyo Técnico | Personal operativo |
| Auxiliar administrativo | Personal operativo |
| Personal Manual | Personal operativo, regla **preexistente conservada** |

Dirección y Subdirección consultan conjuntos distintos de `funcs`, con sus respectivos requisitos. Por ejemplo, Outlook exige 3 para Dirección y 1 para Subdirección; Word exige 1 y 2, respectivamente, conforme al recurso original. Se conserva el máximo de los requisitos de su perfil (tope 3) y la base operativa fuera de SII (tope 1). Las funciones seleccionadas en el perfil no sustituyen el puesto ni conceden permisos; el comportamiento vigente del instrumento no usa esas casillas para ponderar el resultado observado. Personal Académico mantiene herramientas docentes y recomendaciones académicas.

El nivel observado proviene de respuestas, no del puesto. Cada rúbrica tiene doce actividades: tres de cuatro por nivel y niveles anteriores cumplidos; no se suman actividades de otras rúbricas. Los promedios por bloque usan observaciones evaluables de 0 a 3; el global promedia bloques evaluables. Nivel esperado, brechas, prioridades y cursos conservan la misma evaluación. Las cuatro herramientas retiradas continúan fuera de v2. La etiqueta elegida permanece en captura, almacenamiento y filtros históricos.

La edición vigente sigue siendo `uacj-digital-2026-10-28.1`. Las definitivas leen su instrumento y `ReportJson` persistidos. Las actualizaciones explícitas de borradores v1 siguen archivando la captura anterior y pidiendo corregir opciones retiradas; no se ejecuta transformación masiva. La versión del aviso no cambia.

## Captura e instrucciones

`surveyInstructions.ts` concentra las cinco descripciones e instrucciones exactas suministradas para la captura. Son ayudas de presentación, separadas del contenido metodológico persistido. Una explicación por rubro distingue **Inicial (cero válido)**, **No aplica (exclusión explícita del puntaje)** y **Sin respuesta (pendiente)**.

Antes, la acción de categoría sobrescribía todas sus respuestas. Ahora **Marcar pendientes como Inicial** cuenta internamente y modifica sólo preguntas completamente vacías de la categoría obligatoria visible. No incluye herramientas docentes ocultas ni complementarias no seleccionadas; las complementarias conservan su captura individual. No sobrescribe niveles existentes, Inicial, No aplica ni una rúbrica parcialmente marcada. Un objeto `pending` sin contenido cuenta como vacío. Al quedar sin vacíos, el botón se deshabilita sin una leyenda adicional. Las respuestas se pueden editar después.

La [corrección posterior de presentación](PRESENTACION_ENCUESTA_20261029.md) concentra las explicaciones al inicio del apartado y retira las cantidades/leyendas por categoría; los controles individuales muestran sólo «Inicial» y «No aplica». Una sola actualización de estado entra al coordinador existente: debounce 1500 ms, una escritura en vuelo, protección de revisiones locales, suspensión al primer fallo y recuperación explícita mediante Guardar avance. No se añade una petición por pregunta, una consulta de población/Graph por guardado ni actividad automática hacia Nexo. El evento manual conserva UUID, recibo e idempotencia. Los estados de lectura, representación y finalización mantienen sus restricciones. Las capturas inferiores documentan la presentación anterior a esa simplificación.

## Población actual: fuente y contrato

Se reutilizan la conexión **`ConnectionStrings:CompetenciasDigitalesNexo`**, el identificador **`CompetenciasDigitales:Nexo:ApplicationId`** y las vistas ya consultadas por el acceso real:

- `public.nexo_aplicacion`: comprueba la aplicación configurada.
- `public.nexo_usuarios`: `usuario_id` canónico, `usuario_aplicacion_id`, cuenta individual, `num_empleado`, `ID_UR`, `origen_ur`, correo canónico.
- `public.nexo_usuario_rol`: roles efectivos por pertenencia y correo canónico.
- `public.nexo_modulo_rol` y `public.nexo_modulos`: acceso efectivo al destino existente `/competencias-digitales/encuesta` en esa aplicación.

La procedencia se inspeccionó en **código local de Nexo**, `PostgresPublication.php` y `PostgresUserUnits.php`, sin modificarlo ni conectar a Nexo institucional. `effective()` de la publicación contempla concesiones centrales/delegadas, revocaciones y el mecanismo anterior sin concesiones. No se implementa un segundo cálculo de vigencia en TDV2. La consulta exige uno de los mismos roles empleados admitidos por `DigitalAccess.ValidateEmployee` y acceso al módulo de encuesta mediante cualquiera de sus roles efectivos, igual que `DigitalScope.Module(2)`.

`PostgresNexoPopulation.cs` hace una consulta SQL agrupada sobre esas vistas. No usa usuarios locales, intentos, sesiones, cuentas de otros sitios ni todo el padrón SII como denominador. Incluye administradores y responsables si tienen acceso a la encuesta. No crea personas ni encuestas. Los permisos SELECT existentes son suficientes **si la publicación institucional coincide con el contrato inspeccionado**; no se crean funciones, tablas o vistas centrales ni se solicitan concesiones de escritura.

Se deduplica por `usuario_id`, conservando el número de empleado textual para relacionar la única encuesta existente. Varios roles/asignaciones no multiplican personas; el correo no es clave de conteo ni se buscan alias para obtener más permisos. Un mismo canónico con datos contradictorios o dos canónicos con el mismo empleado producen información no disponible, sin fusionar identidades. Las correspondencias Microsoft `(tid,oid)` y los históricos no se modifican.

La adscripción actual es la publicada por Nexo: manual validada, responsabilidad única o adscripción única, con `origen_ur` y cadena institucional completa. No se elige la primera adscripción ni se aproxima por nombre. Una adscripción ambigua/desconocida impide certificar el total: se muestra **información no disponible**, no cero ni una meta parcial. Esta comprobación conservadora de integridad devuelve sólo un booleano global; las personas cargadas están limitadas en SQL a los IDs de unidades autorizados y filtrados. Debe resolverse la inconsistencia en la fuente institucional antes de certificar totales.

Una caída o incompatibilidad del contrato también produce información no disponible. No se convierte en denegación definitiva ni se usa una caché de población para ocultarla. No hay consultas remotas adicionales durante la captura. Queda pendiente comprobar vigencia y permisos de lectura de estas vistas en el entorno real por el operador; las pruebas de esta entrega usan publicaciones sintéticas.

## Alcance, filtros y conteos

El backend resuelve primero el alcance de `DigitalScope`: administrador/supervisor institucional; responsable de nivel 2 o 3 sólo sus unidades asignadas y dependientes. El filtro de nivel sólo existe para los dos primeros. Nivel 1 y `Unit.Kind == "N"` normalizado se retiran de las opciones de filtros; no se identifican por nombre. Los parámetros que intentan seleccionar esos filtros se rechazan. La jerarquía completa se conserva para autorización, antecesores, descendientes, resúmenes y personas adscritas a esos nodos. Las opciones incluyen antecesores autorizados y nombre del padre para que el selector siga encontrando y describiendo descendientes a través de un nodo acumulativo oculto. Una responsabilidad sobre un nodo acumulativo conserva su contexto y resumen, pero no lo agrega como opción seleccionable.

El filtro de nivel agrupa usando todos los nodos de ese nivel, incluidos los acumulativos necesarios para reunir a sus dependientes. Incluso cuando los nodos de un nivel son todos tipo N, ese nivel (excepto 1) conserva su resumen; esos nodos no se ofrecen en el selector de unidades. Filtrar opciones no descarta población de la agrupación.

El filtro de perfil laboral permanece en **Resultados generales**, con perfiles actuales y etiquetas históricas presentes dentro del alcance. Se retira de la participación actual: no se conoce el perfil de quienes aún no han respondido y excluirlos produciría un denominador incompleto.

Cada fila resume su unidad y dependientes; el total superior se calcula desde personas únicas del conjunto, nunca sumando filas superpuestas. Columnas: Unidad responsable, Personas que deben responder, Finalizadas, En curso, Sin iniciar, Participación, Madurez. Se presentan fichas con las mismas etiquetas hasta 1000 px (tableta/móvil), evitando comprimir las siete columnas o partir sus encabezados, sin barras verticales internas.

Para la misma población vigente y generación actual:

- **Finalizadas:** existe una encuesta finalizada, cualquiera que sea su edición válida. No se reabre ni deja de contar por una nueva edición.
- **En curso:** no finalizada, con al menos un campo laboral capturado o respuesta explícita (`level`, `na`, `unused`). Un cero es válido. Aceptación, login, fila local, contexto autocompletado y objetos `pending` vacíos no inician captura.
- **Sin iniciar:** resto de esa población, incluidas personas que nunca entraron y filas vacías creadas al aceptar el aviso.
- **Participación:** `100 × Finalizadas / Población`, hasta un decimal en la interfaz. Con población cero se indica Sin población, sin dividir; con fuente no disponible los conteos son nulos, no cero.
- **Madurez:** promedio de índices congelados evaluables de las finalizadas de esa misma población. No añade ceros por quienes no respondieron ni divide entre la población. Sin informes evaluables se indica Sin datos evaluables.

Se cumple `Finalizadas + En curso + Sin iniciar = Población`. La lectura mantiene el bloqueo compartido de generación del diagnóstico para no mezclar datos anteriores/posteriores a un reinicio. No existe una transacción distribuida entre Nexo y el diagnóstico: el acceso/adscripción es la instantánea de la consulta publicada, no una garantía de que no cambie después de responder.

**Históricos:** Resultados generales sigue consultando informes definitivos y adscripción congelada dentro del alcance autorizado, incluso después de un traslado o pérdida de acceso. Participación usa acceso y adscripción actuales. Por ello ambos módulos pueden tener numeradores diferentes, explicado en la vista; no se mezcla población actual con todos los informes históricos.

`cd_targets` y sus registros se conservan. Configuración muestra los valores existentes como históricos de sólo lectura. El endpoint anterior permanece protegido y responde 410 sin escribir, también para pestañas antiguas; no genera actividad de meta exitosa. La opción existente del reinicio para retirar metas históricas mantiene su autorización y transacción. No se ejecutó ningún reinicio institucional.

## Azul y presentación

El pie y la cabecera ya usaban `--uacj-blue` (`#213455`). `.cd-nav` tenía fondo transparente sobre la cabecera azul. Se establece explícitamente `background: var(--cd-blue)` en esa fila, conservando el token común, los dos renglones, estados, logo SVG blanco, cuenta, orden Nexo, Noto Sans y pie. La corrección está limitada a `.cd-site`. El nuevo detalle de participación elimina el ancho mínimo heredado de las tablas sólo en esa tabla; no altera tablas de Procedimientos.

## Archivos y verificación

- Backend: `Competencias/DigitalAccess.cs`, `DigitalAggregates.cs`, `DigitalSurveyService.cs`; `Integrations/Nexo/PostgresNexoProfiles.cs` y nuevo `PostgresNexoPopulation.cs`.
- Frontend: `competencias/Survey.tsx`, nuevo `surveyInstructions.ts`, `Aggregates.tsx`, `Configuration.tsx`, `types.ts`, `competencias-digitales.css`.
- Pruebas: `DigitalEditionTests.cs`, `DigitalPopulationTests.cs`, ajustes de `DigitalTests.cs`, `DigitalResetTests.cs`, `NativeApplication.cs`; `digital-category-initial.test.mjs`, `digital-population-ui.mjs` y recorrido `competencias-native.mjs`.
- Documentación: esta guía, README, ESTADO_MIGRACION y GUIA.

### Antes y después

| Aspecto | Antes | Ahora |
| --- | --- | --- |
| Equivalencias | Las once reglas solicitadas ya estaban en v2 | Se conservan y verifican explícitamente; sin nueva edición ni recálculo histórico |
| Inicial por categoría | Sobrescribía todas las respuestas de la categoría | Sólo vacíos, con cantidad exacta y conservación de NA, niveles y rúbricas parciales |
| Participación | Intentos existentes frente a metas manuales | Población autorizada vigente, incluso sin login; estados excluyentes y porcentaje de finalización |
| Metas existentes | Editor y escritura administrativa | Consulta histórica; endpoint anterior responde 410 sin cambiar registros |
| Filtros | Podían ofrecer nivel 1 y nodos tipo N | Opciones filtradas, conservando nodos para relaciones, alcance y conteos |
| Navegación | Fondo transparente sobre cabecera azul | Fondo propio explícito, calculado como `rgb(33, 52, 85)`, igual al pie |

### Verificación ejecutada

| Comprobación | Resultado |
| --- | --- |
| TypeScript, `npm run types:check` | Correcto, también después del último ajuste de contexto/antecesores |
| Frontend, `npm run test:forms` | 102 pruebas correctas |
| Vite, `npm run build` | Correcto |
| Solución ASP.NET, Release | Compilación correcta |
| Reglas y transporte existentes de `TDV2.Verification` | 116 comprobaciones correctas |
| Instrumento y equivalencias | 55 comprobaciones correctas; [resultados sin respuestas capturadas](evidencia/20261029-poblacion/instrumento.json) |
| Regresión completa de Competencias con PostgreSQL aislado | 73/73 correctas; [resultado](evidencia/20261029-poblacion/postgres-regresion.json) |
| Repetición PostgreSQL dirigida a población, filtros y auditoría manual | 12/12 correctas; [resultado](evidencia/20261029-poblacion/postgres-dirigidas.json) |
| Regresión en navegador con ASP.NET/PostgreSQL aislados | 23 grupos correctos, cero errores JS; [resultado y exclusión explícita](evidencia/20261029-poblacion/navegador-regresion.json) |
| Nuevos recorridos con Vite | 5 grupos correctos; [medidas y colores calculados](evidencia/20261029-poblacion/digital-population-development.json) |
| Nuevos recorridos con paquete de `dotnet publish` | 5 grupos correctos; [medidas y colores calculados](evidencia/20261029-poblacion/digital-population-published.json) |

La regresión en navegador incluye red lenta, suspensión del autoguardado tras fallo, recuperación manual, confirmación perdida, conflicto entre sesiones, navegación secuencial, captura de cero/NA/complementarias, finalización, informe inmutable, representación, responsables 2/3, supervisor, acceso exclusivo y presentación de portal/Procedimientos. Se omitió la acción de reinicio **en ese recorrido de navegador**; la suite PostgreSQL mantiene sus pruebas transaccionales de reinicio exclusivamente sintético. Después de la regresión completa se amplió la prueba de agrupación por nivel para niveles compuestos sólo por nodos N y se repitieron los doce casos dirigidos y ambos recorridos nuevos de navegador. No se suman esas repeticiones como pruebas distintas.

Dos fallos iniciales del nuevo fixture se debían a combinar INSERT parametrizados en un único comando de preparación; se separaron y se repitieron las pruebas. En la prueba ampliada del navegador se corrigió el resultado esperado de históricos: siete definitivas (seis de la población más una histórica sin acceso), frente a seis en participación vigente. También se amplió sólo la espera inicial de montaje del recorrido a 30 s después de que un arranque frío de Vite, concurrente con otras pruebas SQL, superara los 5 s; no cambia esperas ni reintentos de captura. Estos incidentes se conservan como límites de la preparación/ejecución de pruebas, no como envíos fallidos de usuarios institucionales.

Las nuevas pruebas verifican 10 = 6 + 2 + 2 y participación 60 %, un cero real de madurez, usuarios sin fila local/intento, rol repetido sin duplicar, cuenta con sólo informe excluida, correo canónico cambiado, traslado/revocación sin modificar el informe, nivel 1/tipo N ocultos en ambos módulos, responsable sobre nodo acumulativo con descendientes seleccionables, integridad ambigua y falla de fuente. La captura sigue funcionando durante un fallo exclusivo de la consulta colectiva. La acción masiva conserva selección parcial y NA, persiste al recargar, sigue editable y no genera eventos automáticos; Guardar avance añade un único evento manual.

En **ambas ejecuciones de navegador**, administrador con cinco módulos y participante con dos, a **1440, 900 y 390 px**, cabecera, navegación y pie devuelven `rgb(33, 52, 85)`, opacidad 1 y sin imagen/degradado de fondo. Se comprueban logo blanco local, Noto Sans, foco visible, cuenta, orden publicado y ausencia de desbordamiento horizontal. Las capturas se revisaron visualmente en los tres tamaños. La revisión de tableta motivó usar fichas hasta 1000 px para evitar palabras partidas en encabezados. No se cambian ni se comprimen las firmas.

El paquete se produjo con el target normal, incluidos `npm ci --ignore-scripts` y Vite, en una copia de fuente bajo `.artifacts/digital-population-20261029/publish-source` para no interferir con el servidor Vite existente. Se alojaron su `tdv2.dll`, contenido y `wwwroot` en el host aislado de pruebas. SHA-256 del ensamblado publicado y ejecutado: `DBFC2824A26E2A328F406476EAB3C7F828E2739A122323E00C0FB1D9B68A7B04`. No se inició la aplicación con secretos institucionales ni se desplegó.

### Capturas

Todas corresponden a identidades, unidades y respuestas sintéticas del paquete compilado:

- Avance: [escritorio](evidencia/20261029-poblacion/published-avance-1440.png), [tableta](evidencia/20261029-poblacion/published-avance-900.png), [móvil](evidencia/20261029-poblacion/published-avance-390.png).
- Instrucciones: [escritorio](evidencia/20261029-poblacion/published-instrucciones-1440.png), [móvil](evidencia/20261029-poblacion/published-instrucciones-390.png).
- Categoría después de completar pendientes, conservando rúbrica parcial y NA: [escritorio](evidencia/20261029-poblacion/published-categoria-1440.png), [móvil](evidencia/20261029-poblacion/published-categoria-390.png).
- [Responsable N3 y contexto autorizado](evidencia/20261029-poblacion/published-responsable-n3.png), [fuente no disponible](evidencia/20261029-poblacion/published-poblacion-no-disponible.png), [conjunto sin población](evidencia/20261029-poblacion/published-sin-poblacion.png).

### Reproducción local

Con Node y .NET disponibles en PATH, desde la raíz:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --verify-digital-edition .artifacts/digital-population-20261029/edition.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -SkipBrowser
```

Nuevos recorridos (el preparador crea y cierra su clúster desechable; Edge/Playwright instalados):

```powershell
$env:TDV2_TEST_NODE = (Get-Command node).Source
$env:TDV2_DIGITAL_POPULATION_UI = '1'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -BrowserOnly
```

Para desarrollo, iniciar el Vite local habitual y añadir `TDV2_DIGITAL_VITE=1` al proceso de pruebas. Para paquete, sin esa variable, usar `TDV2_TEST_PUBLISHED_ROOT` con la ruta absoluta del paquete **dentro de `.artifacts` del repositorio** y copiar su `tdv2.dll` al directorio de salida de `TDV2.NativeVerification` antes del recorrido. Estas variables y endpoints de control pertenecen exclusivamente al proyecto de pruebas, no a la configuración productiva. El host valida el directorio y usa autenticación, fuentes y conexiones sintéticas. No ejecutar el paquete normal contra configuración institucional para repetir estas pruebas.

### Límites y fase operativa

PostgreSQL, ASP.NET, EF, transacciones, disparadores, consultas SQL de población y navegador son reales **en aislamiento**. La identidad OAuth/Graph y la publicación Nexo usan dobles/fixtures; la inspección del código local de publicación comprueba el contrato, no su instalación ni el contenido institucional. No se atribuye ninguna confirmación de permisos o entrega real a Nexo. Tampoco se diagnostica aquí `SII_LECTURA_O_INTEGRIDAD`.

El host Vite de pruebas usa un puerto HTTPS efímero y bloquea otros orígenes. La conexión HMR configurada para el puerto habitual no se valida; sus cierres específicos se separan de errores de aplicación en la evidencia de desarrollo. La navegación, carga de módulos, CSS y fuentes sí se verifican. No se midió capacidad ni latencia de Nexo institucional ni se ejecutó carga contra él.

No hay nuevas claves de configuración, permisos Azure/Nexo, dependencias o migraciones para estos cambios. Se conservan las migraciones y pendientes operativos de identidad/auditoría anteriores. Para Windows/Ubuntu se mantiene el mecanismo de publicación vigente y las mismas claves; no hay un paso DDL adicional por población/instrucciones. Antes de habilitar la vista con datos reales, el operador debe verificar que las vistas descritas estén publicadas con sus campos actuales, tengan SELECT para la conexión propia de Competencias y resuelvan una adscripción vigente y una identidad canónica inequívoca por persona. Si el contrato real está incompleto, la vista permanece no disponible hasta corregir la publicación institucional; no utiliza metas históricas como alternativa.

No se conectó a bases institucionales, modificó el repositorio Nexo, ejecutó un reinicio real, cambió secretos, hizo push ni desplegó.
