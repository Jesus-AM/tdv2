# Avance, SQL Server y Pruebas de acceso — 2026-10-10

La fórmula siguiente incluye la actualización de cobertura del 2026-10-14. [Implementación, ejemplo completo, pruebas nativas y compatibilidad JSON](COBERTURA_Y_GUARDADO.md). Los resultados de verificación al final de este documento corresponden a la entrega original del 2026-10-10.

Complementa la [distribución operativa](INTERFAZ_OPERATIVA.md) sobre los cambios locales existentes. Conserva MUI, barra blanca, tipografía, densidad, navegación, ámbitos de acceso, versiones, reservas, autoguardado e inmutabilidad. Sin dependencias, migraciones, cambios de configuración de despliegue o escrituras de bases durante este trabajo.

## Implementado y fórmula

Antes, avance y revisión recorrían los campos por separado. El detalle condicional se agrupaba con su opción y cualquier respuesta histórica no vacía de herramientas podía contar como válida. La fila inicial vacía también se trataba como registro. `FormRequirements.Inspect` produce ahora una sola evaluación, utilizada por `FormCapture.Progress`, la revisión de la etapa y su autorización de entrega. No cambia la etapa habilitada ni añade campos de captura.

`porcentaje = floor(100 × V / T)`, donde `V` es la cantidad de requisitos aplicables completos y válidos, y `T` el total de requisitos aplicables. Todos cuentan una vez, sin pesos por sección. Con `T = 0`, porcentaje 0 y revisión no lista; nunca se divide entre cero. El truncamiento impide presentar 100 mientras falte algún requisito. Completar la captura no equivale a enviar.

| Registro o condición | Qué cuenta |
|---|---|
| Procedimiento real | Los siete requisitos existentes de `ProcedureEligibility`: trámite, destinatarios válidos, resultado, responsable, validación V/A, prioridad 1–5 y código confirmado con formato válido |
| Herramienta real | Cuatro: procedimiento completo/relacionable en el mismo formato, herramienta del catálogo de opciones, uso del catálogo y funcionamiento válido |
| SIIv2 | Un requisito adicional por fila: uno o varios módulos, todos con ID válido/único e instantánea de descripción confirmada |
| Procedimiento aplicable | Una comprobación de cobertura: al menos una herramienta asociada con todos sus requisitos válidos, reutilizando la evaluación de sus campos |
| Otra herramienta | Un requisito adicional: nombre de la herramienta, no sólo espacios y dentro del límite existente |
| Otro uso | Un requisito adicional: descripción del uso con las mismas condiciones de texto |
| Sección sin registros reales | Un requisito incumplido de agregar/completar al menos un registro; en Sistemas lo sustituye la cobertura cuando hay procedimientos aplicables, sin duplicar ese mínimo |
| Contexto, Medios, comentarios y campos posteriores | Opcionales/informativos o fuera de la captura activa; no penalizan ni añaden avance por visitarlos |

Los detalles condicionales dejan de contar cuando su opción no está activa, sin borrarlos. Los nuevos módulos siguen validándose contra la réplica local dentro de la operación de escritura; comprobar su sintaxis en la evaluación no sustituye esa autorización. Referencias históricas conservan ID e instantánea aunque el módulo se retire de nuevas selecciones. Publicación del catálogo y evaluación comparten la misma comprobación de sintaxis de ID, sin cambiar la regla previa de publicación ni consultar SQL Server.

Sólo la fila `inicial` intacta, sin respuestas (y con origen vacío/Nuevo para Identificación), es ayuda de captura. No cuenta en cantidades ni como una fila real incompleta. Las filas agregadas con ID propio, importadas o con código/respuestas son registros reales, aunque estén incompletas. No se borra nada para calcular. Al agregar o eliminar registros cambia el conjunto de requisitos aplicables y se vuelve a evaluar; no se asigna porcentaje fijo por crear filas.

Ejemplos controlados: un procedimiento válido y una herramienta ordinaria válida suman 12/12 = 100 %, incluida su cobertura. Esa misma herramienta con Otra y Otro, sin detalles, suma 11/14 = 78 %: falta también una herramienta completa para cubrir el procedimiento. Con detalles válidos vuelve a 100 %. SIIv2 sin módulos suma 11/13 = 84 %. Dos procedimientos completos y una sola herramienta válida para uno suman 19/20 = 95 %, con envío rechazado. Un campo pendiente entre cientos sigue mostrando como máximo 99 %.

D/N históricos y opciones de herramienta/uso/funcionamiento que ya no pertenecen al catálogo vigente permanecen legibles y sin conversión. Pueden conservarse al guardar campos ajenos conforme a las excepciones existentes, pero no cuentan como válidos para completar la captura editable. Los pendientes reutilizan los requisitos incumplidos; el mensaje identifica sección, registro, código/nombre y campo. Las comprobaciones de código y validación que llevan al mismo control producen un solo enlace, conservando ambas comprobaciones en el cálculo.

Inicio y el formato proyectan el avance de borradores desde el contenido confirmado del servidor, incluida la integración vigente del inventario. Una lectura no reescribe la caché guardada ni crea registros; la siguiente escritura normal persiste el cálculo vigente. No se hizo backfill. El formato muestra **Avance de captura confirmado**, junto al estado de autoguardado: no anticipa avance ni Guardado por texto todavía local. La revisión se actualiza después de confirmar o recibir estado remoto, sin reemplazar el foco ni el borrador. Enviados/históricos conservan contenido, porcentaje persistido, fechas, autores y bloqueo; no se reinterpreta un envío antiguo.

El promedio de Inicio y el avance del conjunto son `floor(sum(porcentajes individuales) / N)`, sobre formatos únicos por `id_ur` dentro del alcance/ramas correspondientes. Sin formatos, 0. No se cuentan nodos auxiliares sin formato, tipos excluidos o áreas duplicadas; jerarquía y ámbito siguen las reglas existentes. No se pondera por cantidad de dependencias o campos. Se conserva el porcentaje individual separado del conjunto. La fecha de envío recibida distingue **Enviado** de **Captura completa**; tener 100 % no concede permiso ni envía el formato. Sólo se retira la explicación textual del promedio del conjunto.

## Revisión, Sistemas y herramientas

La pestaña sigue siendo Revisar y enviar; su contenido usa **Resumen del formato**, **Pendientes** y **Enviar formato**, sin referencias visibles a primera etapa. La definición técnica mantiene únicamente Contexto, Identificación y Sistemas con Medios. Administradores no incorporan requisitos posteriores al mostrarlos. El diálogo conserva su comprobación concurrente y explica el bloqueo con la nueva redacción; sólo tras el ACK muestra **Formato enviado**, fecha y autor.

**Ir al campo** espera el guardado anterior, abre la sección, localiza el registro y enfoca el control, respetando la columna fija Edición. Si faltan todos los registros de la sección, enfoca Agregar trámite o servicio / Agregar fila. En consulta, la alternativa es el panel enfocable; no habilita capturas. Se mantienen el selector buscable de Procedimiento de 280–360 px, nombres completos accesibles, reservas, varias herramientas por procedimiento, condicionales dentro de su celda y Medios independiente.

## SQL Server e ILDA

**Sincronizar SQL Server** envía una sola solicitud `fuentes=sii` al servicio existente. `SyncCoordinator.Catalogs` ya expande secuencialmente UR y módulos, independientemente de ILDA; no requiere otro procesador. Conserva ejercicio sólo para UR, transacciones por catálogo, exclusión, recuperación, auditoría y permisos. ILDA usa su propia acción `fuentes=ilda`. No se modifica el API `ambas` ni la inclusión independiente de ILDA en programación automática.

Ambos bloques tienen estado con texto/icono y fechas confirmadas. SQL Server muestra los dos resultados individuales y sólo completa cuando ambos lo confirman en el mismo intento. Un fallo parcial identifica el catálogo fallido. La fecha correcta individual viene de metadatos del catálogo y sobrevive a intentos fallidos. La fecha conjunta requiere un intento con ambos éxitos: no une éxitos de intentos diferentes. El historial disponible contiene hasta 50 ejecuciones; si no alcanza para acreditar esa fecha, se informa la ausencia. Tampoco se inventan contadores cuando falta un resultado.

Se conserva Iniciando… hasta confirmar ejecución, Sincronizando… durante el trabajo y bloqueo de solicitudes incompatibles, incluso entre SQL Server e ILDA. El servicio manual de ASP.NET funciona con horarios desactivados; no se activan por estas mejoras. [Operación Windows/Ubuntu y recuperación](SINCRONIZACIONES.md).

## Pruebas de acceso

Pestañas compartidas distinguen **Vista por rol y área** y **Actuar como usuario**. La primera busca área por nombre/clave, agrupa rol y área y muestra el resumen antes de Aplicar vista, explicando que es consulta sin escritura. La segunda usa exclusivamente la búsqueda autorizada existente, separa carga/vacío/error y muestra nombre, correo, área y roles recibidos antes de Activar representación. Motivo, límites y escritura desmarcada por defecto se conservan.

La franja del layout identifica contexto efectivo y cuenta autenticada al navegar, con Salir de la prueba / Volver a mi usuario. Un contexto activo impide activar otro sin salir; los rechazos del servidor permanecen como autoridad. No se modificaron controladores de identidad, permisos, concesiones, asignaciones, tokens ni auditoría. [Contrato de delegación y representación](DELEGACION_REPRESENTACION.md).

## Verificado en este complemento

| Comprobación | Resultado y alcance |
|---|---|
| TypeScript, Vite | Correctos con dependencias existentes |
| ASP.NET Release | Compila sin avisos ni errores |
| `test:forms` | 87/87; motor, permisos recibidos, transporte, directorio y resultados de sincronización |
| `TDV2.Verification` | 87/87; dominio y ASP.NET TestServer con dobles en memoria, sin PostgreSQL |
| `interface-layout.mjs` | 9/9; bundle real, identidad/transporte/reservas/SignalR simulados |
| `access-layout.mjs` | 4/4; modos, búsquedas, carga/vacío/error, resumen, activación rechazada/confirmada, navegación y salida simuladas |
| `system-tools-ui.mjs` | 10/10; regresión de Sistemas, condiciones, ayudas, dos sesiones simuladas y consulta |
| `operational-layout.mjs` | 3/3 grupos; indicadores, filtros/permisos recibidos, adaptación en cinco anchos |
| Suites nativas adaptadas | Sólo sintaxis de JavaScript verificada; no ejecutadas |

Los once casos nuevos de dominio/HTTP cubren vacío/parcial/completo, espacios, ID inválido, condiciones Otra/Otro/SIIv2, eliminación, fila de ayuda frente a registro real, D/N y opciones históricas, truncamiento, validación/serialización/recarga, coincidencia Inicio/formato/revisión desde contenido controlado y conservación de un enviado antiguo. Los tests HTTP usan almacenamiento en memoria preparado explícitamente; no acreditan una transacción PostgreSQL. Los casos existentes de catálogo usan ADO sintético con 1501 registros, lectura interrumpida, IDs y expansión manual/automática con ILDA activada/desactivada, sin conectarse a SQL Server.

Navegador: SQL Server con éxito total, parcial y fallo total conservando último éxito; ILDA aparte, doble clic y estado confirmado. Pendientes con foco (incluidas secciones sin registros), cambios conservados, ACK demorado y fallo de guardado. Acceso de consulta, nombre largo, búsqueda/teclado/tacto emulado, escritorio 1440/1050 y móvil 390/320; Inicio también 1920/1366/768. Sin errores JavaScript ni desbordamiento horizontal de página. Dos sesiones en Sistemas usan el doble de transporte y no sustituyen dos identidades reales contra ASP.NET/PostgreSQL.

Evidencia local, ignorada por Git: `.artifacts/interface-layout-20261010/verification.json`, `.artifacts/access-layout-20261010/verification.json`, `.artifacts/system-tools-ui/report.json` y `.artifacts/operational-layout/after/verification.json`, con sus capturas por módulo. Archivos de fallo de corridas anteriores, si existen, no son el resultado final.

```powershell
# Desde la raíz, con dependencias ya instaladas
$env:PATH='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;'+$env:PATH
$env:DOTNET_CLI_HOME=Join-Path (Get-Location) '.artifacts/dotnet-home'
$env:NUGET_PACKAGES=Join-Path (Get-Location) '.artifacts/nuget'
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet build tdv2 -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
node ClientApp/tests/browser/operational-layout.mjs
Push-Location ClientApp
try {
    node tests/browser/interface-layout.mjs
    node tests/browser/access-layout.mjs
    node tests/browser/system-tools-ui.mjs
} finally { Pop-Location }
```

## Pendiente y límites

Debug no pudo reemplazar `bin/Debug/net10.0/tdv2.dll` porque otro proceso la mantenía abierta (MSB3021/MSB3027). Se dejó ese proceso intacto; Release y la suite de dominio/HTTP compilaron y pasaron.

No se ejecutaron suites PostgreSQL ni se conectaron o modificaron bases en esta entrega. No se consultaron fuentes institucionales, Nexo, Herd o credenciales; no se hizo push ni despliegue. No hay nueva migración ni comando adicional de instalación: las migraciones anteriores mantienen su estado. Pendientes la aceptación institucional, ejecución nativa tras estos cambios en un entorno autorizado y dispositivos físicos. Los recorridos de acceso simulan también la auditoría y no acreditan la autorización institucional.

## Archivos del complemento

Rutas relativas a la raíz; los archivos ya modificados se editaron sobre su contenido local, sin restaurarlos. La relación del ajuste anterior, incluido `ProcedureSelect.tsx` y estilos, permanece en [INTERFAZ_OPERATIVA.md](INTERFAZ_OPERATIVA.md).

| Módulo | Archivos |
|---|---|
| Requisitos y avance | `tdv2/Domain/FormRequirements.cs` (nuevo), `FormCapture.cs`, `FormReview.cs`, `FormStages.cs`, `SystemAnswers.cs`; `tdv2/Services/FormService.cs`; `tdv2/Synchronization/CatalogPublication.cs` |
| Inicio | `ClientApp/resources/js/pages/Inicio.tsx`, `Components/AreaDirectory.tsx`, `lib/area-directory.ts`, `types/tdv2.ts` |
| Formato/revisión | `ClientApp/resources/js/pages/FormatoUR.tsx`, `Components/FormSubmissionReview.tsx` |
| Sincronizaciones | `ClientApp/resources/js/pages/Sincronizaciones.tsx`, `lib/sync-presentation.ts` |
| Pruebas de acceso | `ClientApp/resources/js/Components/AccessModeTabs.tsx` (nuevo), `pages/PruebasAcceso.tsx`, `pages/VistaPrueba.tsx`, `pages/ActuarComoUsuario.tsx`, `Layouts/AuthenticatedLayout.tsx` |
| Pruebas de dominio/HTTP | `tests/TDV2.Verification/CaptureProgressTests.cs` (nuevo), `EntryPoint.cs`, `Fixtures.cs` |
| Pruebas frontend | `ClientApp/tests/frontend/area-directory.test.mjs`, `sync-presentation.test.mjs` |
| Navegador | `ClientApp/tests/browser/access-layout.mjs` (nuevo), `interface-layout.mjs`, `system-ui-fixture.mjs`, `operational-layout.mjs` |
| Selectores de suites existentes | `ClientApp/tests/browser/access-flow.mjs`, `photos-flow.mjs`, `presentation-flow.mjs`, `stage-review-native.mjs`, `native-flow.mjs`, `delivery-collaboration-native.mjs`, `sync-flow.mjs`, `sync-manual-native.mjs` |
| Documentación | `README.md`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md`, `INTERFAZ_OPERATIVA.md`, `AVANCE_Y_PRUEBAS_ACCESO.md` (nuevo), `SINCRONIZACIONES.md`, `SISTEMAS_HERRAMIENTAS.md`, `REVISAR_ENVIO_ETAPAS.md`, `DELEGACION_REPRESENTACION.md` |
