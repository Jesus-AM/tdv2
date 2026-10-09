# Sistemas y herramientas y módulos SIIv2 — 2026-10-09

## Implementado

Se conservan las tablas, tema y densidad compacta de captura (13 px, encabezados 12 px/negritas). Identificación muestra **Código** y debajo el origen guardado: ILDA, norma o Nuevo. React no genera códigos; el servidor rechaza cambiar códigos confirmados y orígenes. Prioridad conserva cinco niveles y colores, sin Ejemplo ni Consejo. La ayuda compartida tiene X accesible, Cerrar, Escape, retorno de foco, scroll y secciones opcionales.

Una herramienta por fila; varios registros pueden compartir procedimiento hasta el límite existente de 200 filas del formulario. Procedimiento ofrece únicamente registros guardados del mismo formato, existentes, completos y válidos en sus campos obligatorios actuales y marcados V/A. `ProcedureEligibility` es la regla común de opciones, escritura transaccional, avance y revisión. No basta con nombre/código/validación. Una relación anterior que se invalide se conserva como pendiente; puede corregirse en Identificación y bloquea la entrega. Herramienta, uso y funcionamiento son selección única. SIIv2 abre un buscador de módulos; Otra y Otro uso abren detalles en su misma celda. Fallas o comentarios sigue siendo multilínea y opcional, también para fallas/no funciona. Los estados muestran icono, texto y color.

Las instrucciones son texto normal con título, pregunta destacada y descripción. Después de la tabla aparece Medios utilizados, como sección de la misma pestaña, con reserva/autoguardado independientes. El campo vigente `Otro medio` carece de placeholder; se conserva «Portal del CAST de uso de técnicos» en herramientas. Ambas tablas comparten la primera columna fija **Edición** y un único foco de campo MUI, distinto del contorno del registro ocupado. Acciones contiene Eliminar, alineado arriba y con área de 44 × 44 px. Comentarios y detalles Otra/Otro muestran dos a cuatro líneas, con desplazamiento interno sin recortar datos. Desplazar la tabla o la página conserva la reserva y el contexto de edición; [reglas y evidencia](DESPLAZAMIENTO_TABLAS.md).

La revisión y el envío están aquí: comprenden Contexto (informativo), Identificación general y Sistemas y herramientas, incluidos Medios (con sus condiciones existentes, sin hacerlo obligatorio). Avance y validación del servidor usan exactamente esa etapa. Mostrar pestañas posteriores como administrador no amplía requisitos. Se conservan permisos intrínsecos de envío, reservas, versión, snapshot e inmutabilidad; los colaboradores siguen sin poder enviar.

No cambian permisos, participación, primera etapa, enviados, fotografías, reservas automáticas, autoguardado independiente ni eliminación directa. Abrir menús/ayudas y cambiar controles mantiene la unidad de edición. Buscar no escribe un ID ni se trata como respuesta. La primera modificación requiere reserva confirmada.

Las claves JSON nuevas se guardan estructuradas sin migrar las respuestas anteriores:

| Campo | Claves/detalle |
|---|---|
| `sistema` | `sii_v2`, `portal_cast`, `excel`, `correo`, `formulario`, `papel`, `otra` |
| `sistemaOtro` | Nombre libre separado de la opción |
| `moduloSiiId` | ID institucional estable |
| `moduloSiiDescripcion` | Instantánea autorizada por el servidor al seleccionar el módulo |
| `uso` | `registrar`, `agendar`, `consultar`, `seguimiento`, `avisos`, `reportes`, `autorizar`, `otro` |
| `usoOtro` | Detalle libre separado del uso |
| `estado` | `bien`, `fallas`, `no_funciona`, `sin_uso` |

Alternar opciones conserva detalles para recuperar una selección accidental. Sólo el detalle de la opción activa cuenta como respuesta. Vacíos, SIIv2 sin módulo y Otra/Otro sin detalle admiten autoguardado y cuentan pendientes en el avance del servidor. «Selecciona…» es indicativo deshabilitado.

Los valores históricos se muestran como tales sin convertirlos. Guardar comentarios u otros campos permite conservar respuestas anteriores; modificar una opción exige una clave vigente. Enviados/históricos bloqueados mantienen su lectura y permanecen inmutables.

## Réplica local y sincronización

Entidad/configuración EF `SiiModule`, tabla `public.sii_modulos`: PK textual `id_modulo`, `desc_modulo`, `presente`, `sincronizado_en`. `presente` expresa disponibilidad local observada, **no ACTIVO institucional**. Los registros retirados se conservan para referencias históricas.

`GET /formatos/{ur}/modulos-sii` revalida sesión, Nexo, contexto y alcance de lectura. Devuelve únicamente estado, ID y descripción de módulos disponibles en PostgreSQL, sin filtro de UR. Nunca abre SQL Server. El selector distingue duplicados por ID, ordena en español e informa carga/error/pendiente sin bloquear el resto del formulario.

El backend comprueba selecciones nuevas/cambiadas contra módulos disponibles y mantiene un bloqueo compartido del módulo hasta confirmar la transacción del formato. Obtiene la descripción del catálogo; no confía en la del navegador. Una referencia guardada conserva su descripción después de una baja o cambio de nombre. Cambiar comentarios no refresca esa instantánea. Volver desde otra herramienta a SIIv2 valida disponibilidad de nuevo.

El coordinador existente incorpora `sii_modulos` como etapa separada de `sii` (UR), seguida por ILDA cuando se incluye. Manual y automática incluyen ambos catálogos SII, con ILDA independiente. Programación pausada, cola, exclusión, recuperación y comprobación de propietario/caducidad antes del commit se conservan. Publicación, metadatos, resultado y auditoría son transaccionales por catálogo. Una etapa fallida impide anunciar éxito completo. Nunca se actualizan formatos, respuestas, versiones o instantáneas desde la sincronización.

Consulta fija sobre **DesarrolloSII**, reutilizando exclusivamente `ConnectionStrings:Sii`:

```sql
SELECT [ID_MODULO],[DESC_MODULO] FROM [sii].[MODULOS_SII];
```

No consulta otras columnas ni aplica TOP 1000, ejercicio, ACTIVO, UR o responsable. La selección de ejercicio de UR (`Synchronization:SiiLatestExercise`) permanece igual. La cuenta institucional necesita **SELECT sobre `sii.MODULOS_SII`**, además del permiso de UR existente. No se concedieron permisos ni se modificaron credenciales.

Las excepciones de conexión/lectura no devuelven listas parciales. Se validan ambas columnas, IDs numéricos textuales positivos de hasta 40 caracteres y descripciones no vacías de hasta 4000 caracteres. IDs iguales con descripciones incompatibles rechazan la etapa; IDs distintos con igual descripción se conservan. Duplicados idénticos se consolidan por ID.

Se mantienen protecciones conservadoras: máximo configurado de 100000 filas (rechazo, no truncamiento), descarga vacía y reducción superior al 20% rechazadas. Se conserva la última copia y se requiere revisar el origen; nunca se usan esos resultados para vaciarlo. Las desapariciones aceptadas se marcan no disponibles sin borrar referencias. Auditoría y fallos contienen mensajes sanitizados/contadores, no volcados del origen.

## Migración y operación posterior

El ajuste de entrega/foco/elegibilidad/diagnóstico no requiere otra migración y no modifica las existentes. `42P01` significa que PostgreSQL no resuelve la relación solicitada; no demuestra un fallo de SQL Server. EF crea `public.sii_modulos` y el modelo usa `public`; las consultas especializadas ahora indican explícitamente ese esquema. `SiiCatalogSchema` distingue migración ausente del historial, historial aplicado sin tabla, tabla en otro esquema e historial inaccesible para la cuenta de ejecución. La pantalla administrativa muestra el diagnóstico y un contador no disponible, nunca cero como lectura correcta. La etapa de módulos falla antes de abrir su fuente si falta el esquema local; los resultados conservan su estado parcial/fallido.

En PostgreSQL sintético se reproduce el error al detener EF antes de `SiiModulesCatalog` y desaparece al aplicar la migración normal. Esto no identifica la base utilizada por una instalación existente: una conexión a otra base aún sin migrar produce el mismo síntoma. El operador debe comprobar el destino previsto de `ConnectionStrings:Tdv2`, historial y esquema en esa misma base. No se consultó ninguna base institucional, no se creó el catálogo manualmente y no se añadió DDL al arranque.

Nueva migración **`20261009080533_SiiModulesCatalog`**. Up sólo añade la réplica; los detalles nuevos utilizan el JSON existente. No se editaron migraciones anteriores. Down rechaza retirar el catálogo con módulos, referencias o formatos enviados. No se ejecuta DDL al arrancar.

Tras comprobar el destino previsto de User Secrets, el operador puede aplicar desde la raíz:

```powershell
dotnet tool restore
dotnet ef migrations list --project tdv2 --startup-project tdv2 -- --environment Development
dotnet ef database update 20261009080533_SiiModulesCatalog --project tdv2 --startup-project tdv2 -- --environment Development
```

EF también aplica migraciones anteriores pendientes; revisar su alcance. Esta entrega no aplicó migraciones institucionales. `.artifacts/SiiModulesCatalog.sql` es evidencia de generación, no un segundo migrador.

En **Configuración → Sincronizaciones**, **Sincronizar SII** encola UR y módulos; **Sincronizar SII e ILDA ahora** añade ILDA. Ambos contadores SII y sus resultados separados aparecen en pantalla/historial. La programación conserva su activación previa.

Las solicitudes manuales se atienden desde el proceso web: usar los botones de Configuración → Sincronizaciones, sin otra terminal. No se activan horarios ni cambia ILDA al hacerlo. [Operación vigente y recuperación](SINCRONIZACIONES.md). Para programación automática o un ciclo externo explícito, se conservan estas alternativas desde la raíz y bajo la configuración autorizada:

```powershell
dotnet run --project tdv2 --no-launch-profile -- --environment Development --sync-once
dotnet run --project tdv2 --no-launch-profile -- --environment Development --sync-worker
```

`--sync-once` procesa un ciclo; `--sync-worker` continúa cada minuto. `--sync-check=sii` comprueba UR y módulos sin publicar. Estos comandos pueden consultar las fuentes: no se ejecutaron contra servicios institucionales.

## Verificación del ajuste de entrega, foco y diagnóstico

Pasaron 76/76 pruebas .NET de dominio/HTTP en memoria, 78/78 frontend, 51/51 de edición en PostgreSQL nativo, 35/35 de sincronización nativa y 57/57 comprobaciones EF. Las migraciones anteriores conservan sus cálculos históricos; se actualizan las expectativas de esas pruebas, sin editar el SQL aplicado. Pasaron 11/11 recorridos con dos identidades sintéticas distintas en contextos de Edge separados, ASP.NET, PostgreSQL y SignalR reales. Se incluyen carrera por la misma fila, filas diferentes, nombre/foto/color, salida y liberación sin recarga, primera tecla/pegado/Tab, elegibilidad remota, medios independiente, menús/ayudas, eliminación, renovación tardía, desconexión/reconexión/caducidad, móvil, envío y rechazos directos del backend. Una pestaña de la misma cuenta se verifica como caso adicional.

Los 10 recorridos de interfaz de sistemas con transporte simulado se identifican por separado y no acreditan persistencia. No hay validación institucional: Microsoft es un par HTTP sintético, Nexo se publica mediante vistas/funciones sintéticas y las fuentes SII/ILDA son lectores ADO.NET de prueba. [Resultados nativos de edición](entrega-sistemas-20261009/edicion-postgresql.json), [EF](entrega-sistemas-20261009/migraciones-postgresql.json), [sincronización](entrega-sistemas-20261009/sincronizaciones-postgresql.json), [dos usuarios en navegador](entrega-sistemas-20261009/dos-usuarios-postgresql.json) y [UI simulada](entrega-sistemas-20261009/navegador-transporte-simulado.json).

## Verificado en la entrega anterior de módulos

Repetición posterior al ajuste de entrega: **7/7 recorridos nativos de módulos** y **13/13 de Sincronizaciones**, ambos con ASP.NET/PostgreSQL y cero errores JavaScript. El caso adicional de Sincronizaciones abre la pantalla con la tabla EF temporalmente ausente, comprueba el error administrativo y «No disponible», restaura el nombre en el fixture y confirma la recuperación. [Módulos](entrega-sistemas-20261009/modulos-navegador-postgresql.json), [Sincronizaciones](entrega-sistemas-20261009/sincronizaciones-navegador.json).

- TypeScript, Vite y solución Release correctos; .NET sin advertencias. EF confirma modelo/snapshot coherentes y genera la migración sin conectar.
- **74/74 .NET**: dominio/transporte en memoria y lector ADO.NET sintético. Incluye 1501 módulos, repetición de lectura, duplicados, interrupción, conexión fallida, IDs inválidos (incluido booleano), conexión Sii compartida sin abrirla, borradores, detalles, varias herramientas, estados, históricos, códigos/orígenes y autorización del endpoint.
- **78/78 frontend**: regresiones de reservas/autoguardado, versiones, pendientes, eliminaciones, navegación y fotografías.
- **34/34 sincronizaciones en PostgreSQL 18.6 desechable**, incluidos los cuatro casos nuevos de módulos, 1501 registros, IDs estables, fallos/rollback y ambos modos con/sin ILDA. Corrida `native-postgres-20261009-084200-2373f124`.
- **52/52 comprobaciones EF/PostgreSQL**: nueve migraciones, nueva réplica sobre ocho anteriores, repetición, conservación de datos, rollback y verificación de esquema previo/actual. Corrida `native-postgres-20261009-084557-d4d2a18e`; [evidencia](sistemas-herramientas-20261009/migraciones-postgresql.json).
- **49 escenarios de edición PostgreSQL comprobados**: 48/49 en `native-postgres-20261009-085429-706ad183`, más repetición dirigida 2/2 en `native-postgres-20261009-085927-33d92b32`. Incluye el caso histórico corregido y protección del código contra vacío/null/renombre. [Reporte agregado que conserva ambos resultados](sistemas-herramientas-20261009/edicion-postgresql.json). Los fixtures antiguos se adaptaron a reservas, destinatarios y primera etapa vigentes, sin cambiar sus restricciones de producción.
- **7/7 Edge → ASP.NET → PostgreSQL**, cero errores JavaScript: catálogo pendiente/borrador, búsqueda y persistencia, renombre/baja histórica, Otra/Otro, dos sesiones/SignalR/reservas/autoguardado, varias filas del mismo procedimiento y móvil/teclado/enviado. Corrida `native-postgres-20261009-084018-c939ad44`. [Reporte](sistemas-herramientas-20261009/navegador-postgresql.json). Fuentes remotas sintéticas, motor real detenido al terminar.
- **12/12 Edge de Sincronizaciones → ASP.NET/PostgreSQL**, cero errores JavaScript: resultados separados, fallos parciales, programación con/sin ILDA, revocación, recuperación y escritorio/móvil. Corrida `native-postgres-20261009-084856-06da90d2`; [reporte](sistemas-herramientas-20261009/sincronizaciones-navegador.json).
- **10/10 recorridos Edge con React real y transporte simulado**, cero errores JavaScript: búsqueda/persistencia simulada, carga/error/pendiente, detalles, iconos, X/Cerrar/Escape/foco, dos sesiones, filas independientes, Código/orígenes, históricos/enviados, teclado y anchos 1920/1366/768/360. Capturas inspeccionadas, scroll horizontal contenido. Este nivel **no acredita ASP.NET/PostgreSQL**; tipografías externas bloqueadas y móvil emulado.

## Pendiente

SII real: SELECT, base DesarrolloSII, autenticación/TLS/TDS, tipos efectivos de ID/descripción, volumen/rendimiento y protecciones de reducción. Aplicación institucional de EF y aceptación en dispositivos físicos. No hubo acceso institucional ni cambios en Nexo/Herd/credenciales, push o despliegue.

## Comandos sintéticos

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:NuGetAudit=false
Push-Location ClientApp
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
node tests/browser/system-tools-ui.mjs
Pop-Location
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -SyncOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
# Repetición dirigida de los dos escenarios de regresión de códigos/referencias:
$env:TDV2_TEST_CASE_PREFIX='Edición/regresión:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -MigrationsOnly
# Recorrido con React, ASP.NET y PostgreSQL real, todas las fuentes sintéticas:
$env:TDV2_TEST_BROWSER_FLOW='systems'
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
# Recorrido de Configuración → Sincronizaciones con el mismo entorno sintético:
$env:TDV2_TEST_BROWSER_FLOW='sync'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

Node se tomó de `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs`, añadido sólo al PATH del proceso. No se instaló ni modificó software de Herd.

Los intentos iniciales de `pg_ctl` fallaron por el relanzamiento con token restringido (87/3). El parámetro opcional `-DirectStart` inicia `postgres.exe` con el mismo usuario y restricciones, únicamente para el clúster recién creado y verificado dentro de `.artifacts`, en loopback/puerto aleatorio; conserva el apagado por `pg_ctl`. No utiliza bases existentes ni eleva permisos.

`TDV2_TEST_EPHEMERAL_TLS` habilita exclusivamente en el ejecutable de pruebas un puente HTTPS Node → Kestrel HTTP en loopback. El certificado y su clave son efímeros, se transmiten por stdin y nunca se instalan, confían ni escriben en el almacén de Windows. Esto permite probar cookies seguras y SignalR cuando no hay certificado de desarrollo disponible para Schannel. El sitio productivo no contiene el puente ni sus controles de fixture.
