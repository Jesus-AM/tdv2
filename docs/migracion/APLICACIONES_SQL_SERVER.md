# Documento histórico: origen SQL Server retirado el 2026-10-15

**Referencia histórica:** no ejecutar instrucciones de sincronización de aplicaciones de esta entrega anterior. El origen vigente es [APLICACIONES_LISTAS.md](APLICACIONES_LISTAS.md); la actualización de captura y limpieza vigente se documenta en [Catálogo vigente, limpieza exacta, respaldo y actualización Windows/Ubuntu](CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md). SQL Server sólo incluye UR y módulos.

**No utilizar los procedimientos de sincronización/publicación descritos abajo.** La operación vigente usa exclusivamente los Excel versionados, la migración aditiva `20261015165555_LocalApplicationLists` y los comandos `--check-applications` / `--import-applications`. [Instrucciones vigentes](APLICACIONES_LISTAS.md). Se conserva este documento como evidencia del esquema anterior, su migración y pruebas; las tablas e historial anteriores no se eliminan.

# Aplicaciones móviles y web — 2026-10-14

## Implementado

Se completa la preparación con los metadatos proporcionados por el operador; no se consultó la fuente institucional. Se conserva el modelo híbrido: catálogos relacionales locales y selecciones/instantáneas en el JSON del formato.

**Migración: 20261014180408_SiiApplicationsCatalog.** Crea únicamente public.sii_aplicaciones_moviles y public.sii_aplicaciones_web. Los IDs conservan el identity SQL Server mediante PK integer/ValueGeneratedNever, sin secuencias ni identidades locales. No modifica tablas de formatos, datos existentes o snapshots.

| Columna | Móviles | Web |
| --- | --- | --- |
| ID_APLICACION_MOVIL / ID_APLICACION_WEB | integer NOT NULL | integer NOT NULL |
| NOMBRE | varchar(200) NOT NULL | varchar(200) NOT NULL |
| DEFINICION | varchar(200) NULL | varchar(8000) NULL |
| EMP_RESPONSABLE | integer NULL | integer NULL |
| PROG_RESPONSABLE | integer NULL | integer NULL |
| ID_UR | integer NOT NULL | No consultada |
| URL | No consultada | varchar(500) NULL |
| ACTIVA | boolean NOT NULL | boolean NOT NULL |
| NUM_EMPLEADO_DIR_GRAL | integer NULL | integer NULL |
| ID_APLICACION_WEB_PERTENECE | No consultada | integer NULL, sin FK |

Ambos agregan sólo presente boolean y sincronizado_en timestamp UTC sin zona, siguiendo el patrón existente. Se conservan nulos, ceros, espacios, vacíos y acentos. ID_UR y el padre web son datos de origen, sin FK contra un subconjunto de aplicaciones/unidades. El padre puede faltar entre las activas.

## Sincronización

CatalogSource usa **ConnectionStrings:Sii** existente, con SELECT de las ocho/nueve columnas explícitas y **WHERE [ACTIVA] = 1**, sin TOP, SELECT *, filtros por ejercicio, UR o responsable. El límite técnico existente de 100000 filas aborta si se excede; no publica datos truncados.

**Sincronizar SQL Server** ejecuta UR → módulos SIIv2 → móviles → web. ILDA permanece independiente; la opción ambas y la programación opcional la agregan al final. Se reutilizan consumidor manual ASP.NET, CLI, cola persistente, exclusión y recuperación. No se habilitan horarios ni se añaden variables.

La lectura debe llegar a EOF y validar todas las filas. Tipos incompatibles, NULL obligatorio, columnas faltantes, longitudes inválidas e IDs duplicados incompatibles rechazan ese catálogo. Duplicados idénticos se consolidan; nombres iguales con diferentes IDs siguen separados.

Cada publicación incluye upsert, disponibilidad, fecha, resultado y auditoría **en la misma transacción**, comprobando propietario y caducidad antes del commit. Fallos revierten ese catálogo sin sustituir su última fecha correcta; otros catálogos pueden completar y la ejecución queda Parcial.

Una lectura completa retira mediante presente=false los IDs ausentes, conservando sus columnas. Un conjunto completo de cero activas es válido; una lectura fallida/interrumpida jamás autoriza esa retirada. presente no falsifica ACTIVA: conserva el último valor leído. Las protecciones de reducción existentes de UR/ILDA/módulos permanecen; el catálogo filtrado de aplicaciones admite reducciones legítimas hasta cero.

La pantalla muestra cuatro resultados SQL Server y última fecha correcta por catálogo. El éxito conjunto requiere los cuatro resultados del mismo intento. Un historial anterior con sólo UR/módulos no acredita sincronización de aplicaciones. La ausencia de tablas se informa al administrador como error PostgreSQL con la migración esperada; no se consulta esa fuente ni se devuelve éxito vacío. No hay DDL al arranque.

## Selectores, JSON y avance

Las rutas /formatos/{ur}/aplicaciones/moviles y /formatos/{ur}/aplicaciones/web revalidan acceso al formato y consultan únicamente la réplica. Devuelven ID, nombre y discriminador; no definición, URL ni empleados. Estados: pendiente sin sincronización; error ante lectura/esquema fallido; disponible con lista vacía tras sincronización completa sin activas.

CatalogMultiSelect comparte el comportamiento de SIIv2: «Selecciona aplicaciones», un nombre completo multilínea o «N aplicaciones seleccionadas». Sin IDs visuales ni chips; búsqueda, casillas, nombres completos, selección múltiple sin cerrar, Escape y clic exterior. Conserva agrupación, tipografía, apertura inmediata, reserva antes de modificar, foco, autoguardado y scroll. Medios sigue independiente y no reaparece el bloque de cobertura retirado.

~~~json
{
  "sistema": "aplicaciones_moviles",
  "aplicacionesMoviles": [
    { "id": "34", "catalogo": "moviles", "descripcion": "Nombre confirmado por el servidor" }
  ],
  "aplicacionesWeb": []
}
~~~

Los IDs int32 se guardan como texto canónico, sin alias como 034. Cada colección exige su discriminador: el mismo número en móviles/web no es la misma asociación. El servidor rechaza duplicados y catálogo ajeno, e ignora nombres nuevos enviados por el navegador. La consulta FOR SHARE dentro de la escritura protege frente a una retirada concurrente; las referencias conservadas usan su descripción histórica.

Cambiar de herramienta conserva detalles dormidos, pero sólo evalúa/utiliza su colección activa; reactivar una colección verifica disponibilidad cuando no pertenecía al tipo previamente activo. Una colección vacía explícita permanece vacía al recargar. Omitir un detalle previamente almacenado no lo borra. Sin backfill ni transformación masiva del JSON; una sincronización nunca modifica respuestas o enviados.

Cada colección activa aporta **un requisito condicional**, completo si contiene al menos una referencia válida y todas las seleccionadas son válidas. Más aplicaciones no añaden puntos ni requisitos. Se conserva floor(100 × requisitos completos / requisitos aplicables), con cobertura por procedimiento y campos vigentes. Un borrador vacío se guarda, genera pendiente y bloquea envío. Inicio/revisión/envío comparten la evaluación del servidor y los enlaces llevan al dependiente.

## Verificación y límites

Resultados finales de esta entrega:

| Comprobación | Resultado | Evidencia |
| --- | --- | --- |
| TypeScript, Vite y Release .NET | Correctos; build final .NET sin avisos/errores | Comandos siguientes |
| Dominio/HTTP con dobles | 114/114 | TDV2.Verification |
| Frontend | 88/88 | test:forms |
| Sincronización ASP.NET/PostgreSQL 18 | 52/52 | .artifacts/native-postgres-20261014-181041-a4b2cb96/verification.json |
| Edición, histórico y concurrencia PostgreSQL | 12/12 | .artifacts/native-postgres-20261014-181511-75e4b3c0/verification.json |
| Migraciones EF sobre PostgreSQL | 59/59 | .artifacts/native-postgres-20261014-182051-6eb9629e/ef-migrations.json |
| Edge con ASP.NET/PostgreSQL/SignalR | 11/11 recorridos | .artifacts/native-postgres-20261014-181135-b959b0a1/systems-native-browser.json |
| Edge aplicaciones, transporte simulado | 12/12 grupos | .artifacts/applications-ui/report.json |

La primera corrida específica de aplicaciones completó 9/9 casos; están incluidos/ampliados en las suites posteriores, no se suman como casos distintos. Se corrigió el fixture de permisos después de recrear tablas para probar Down/Up. La regresión EF actualizó el verificador de sólo lectura para reconocer los esquemas anteriores sin los nuevos catálogos y la expectativa de Down (puede retirar los dos catálogos vacíos; la protección del envío bloquea retroceder más). Los intentos que disputaron DLL/manifiesto de pruebas se repitieron después de terminar el proceso anterior; no son fallos de la aplicación ni validaciones exitosas. Todos los clústeres sintéticos quedaron detenidos.

Pruebas reproducibles:

~~~powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:NuGetAudit=false -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:NuGetAudit=false -p:UseAppHost=false
$env:TDV2_TEST_CASE_PREFIX='Sync/aplicaciones|Edición/aplicaciones'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -SyncOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -MigrationsOnly
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
$env:TDV2_TEST_BROWSER_FLOW='systems'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
~~~

No compilar el mismo proyecto de pruebas simultáneamente: comparte el manifiesto MVC de obj. El preparador crea y valida un clúster PostgreSQL 18 nuevo, loopback/puerto aleatorio, dentro de .artifacts; lo detiene al terminar.

Casos nativos: migración anterior→nueva/repetida sobre enviado, tipos/nulos/longitudes, ausencia de identidades/FK, Down protegido, 1501 activas por catálogo, nombres duplicados, actualización, texto web de 8000 y URL de 500, padre ausente, retirada completa y fallos de lectura/columnas/duplicados/longitud. Un fallo provocado al auditar comprueba rollback de publicación/fecha/resultado. Guardado HTTP de cero/una/varias, referencias históricas, cambio de sistema, avance, enviados y concurrencia con conexiones independientes.

Edge usa ASP.NET/PostgreSQL reales y fuentes/identidad sintéticas. Los recorridos complementarios de reserva lenta/denegada y cambios rápidos usan transporte simulado. Escritorio/móvil/tacto emulados no equivalen a dispositivos físicos ni a aceptación institucional. Quedan pendientes conectividad/permisos/datos reales SQL Server, dispositivos físicos y lectores de pantalla. No se modifican dependencias; NuGet mantiene sus advertencias anteriores de System.Security.Cryptography.Pkcs 9.0.18.

## Actualización posterior del servidor — no ejecutada

La migración es **aditiva y compatible con el esquema anterior del repositorio**: puede aplicarse antes de iniciar el binario nuevo. No se inspeccionó la versión efectivamente publicada; el operador debe comprobar su historial EF y destino. No adoptar tablas a mano ni aplicar sobre una base sin historial conocido.

Backend, frontend y procesadores externos deben actualizarse juntos antes de capturar aplicaciones. Un binario anterior puede desconocer las nuevas opciones/claves JSON; compatibilidad del esquema no garantiza volver a ese binario después de capturarlas. Down rechaza retirar catálogos con datos/referencias: no usarlo como recuperación rutinaria.

Orden de actualización:

1. Preparar artefacto completo y respaldo por el procedimiento del operador. Verificar destino/configuración segura existente. No introducir credenciales en comandos.
2. Detener temporalmente sólo TDV2 y sus procesadores durante el reemplazo, según la operación vigente; usar un solo operador de migración.
3. Aplicar explícitamente la migración con el código nuevo. Windows, desde la raíz, usando User Secrets de TDV2 ya configurados:

~~~powershell
dotnet tool restore
dotnet ef migrations list --project tdv2 --startup-project tdv2 -- --environment Development
dotnet ef database update 20261014180408_SiiApplicationsCatalog --project tdv2 --startup-project tdv2 -- --environment Development
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-tdv2
~~~

publish compila React mediante el target existente y requiere Node/npm en PATH. Puede prepararse el artefacto antes de la ventana ejecutando publish primero; publicar archivos no aplica migraciones.

Alternativa Ubuntu, desde la raíz del código autorizado con SDK, usando ConnectionStrings__Tdv2 suministrada por la configuración segura existente:

~~~bash
dotnet tool restore
dotnet ef migrations list --project tdv2 --startup-project tdv2 -- --environment Production
dotnet ef database update 20261014180408_SiiApplicationsCatalog --project tdv2 --startup-project tdv2 -- --environment Production
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-tdv2
~~~

Son alternativas para la misma actualización, no pasos que se ejecutan en ambos equipos. Un servidor con sólo runtime recibe el artefacto; la migración se ejecuta desde la estación autorizada con SDK/configuración. No se instalan herramientas o servicios aquí.

4. Si sitio y migrador usan roles PostgreSQL distintos, extender por el operador la política local existente a las tablas nuevas: SELECT/INSERT/UPDATE para quien sincroniza. La cuenta SQL Server necesita **SELECT sobre las columnas solicitadas** de sii.APLICACIONES_MOVILES_SII y sii.APLICACIONES_WEB_SII, además de sus permisos existentes. Esta tarea no concede permisos.
5. Reemplazar artefacto y reiniciar sitio/procesadores compatibles. Sin nuevas variables, credenciales, navegación o configuración Nexo; horarios permanecen como estaban.
6. Administrador: **Configuración → Sincronizaciones → Sincronizar SQL Server**. Revisar los cuatro resultados/fechas; ILDA conserva su botón. El host procesa la cola manual sin --sync-once adicional. Ante fallo parcial, revisar y reintentar por el mismo flujo, sin alterar tablas manualmente.

## Archivos principales

- Entidades/configuración: SiiMobileApplication.cs, SiiWebApplication.cs, SiiApplicationConfiguration.cs, Tdv2DbContext.cs, migración/Designer/snapshot.
- Publicación: CatalogSource.cs, CatalogPublication.cs, ApplicationCatalogPublication.cs, ApplicationCatalogSchema.cs, SyncCoordinator.cs, SynchronizationQueries.cs, LocalCatalog.cs.
- JSON/API: SystemApplications.cs, SystemAnswers.cs, FormRequirements.cs, FormSchema.cs, FormEditingService.cs, FormService.cs, FormsController.cs.
- Interfaz: CatalogMultiSelect.tsx, SystemField.tsx, FormatoUR.tsx, Sincronizaciones.tsx, sync-presentation.ts, types/tdv2.ts y CSS compartido.
- Pruebas: ApplicationCatalogTests.cs, ApplicationSelectionTests.cs, MigrationTests.cs, DatabaseInspection.cs, fuentes/host/runner nativos, casos de sincronización, applications-ui.mjs, system-tools-native.mjs y pruebas frontend.

Se conservan los demás cambios locales. Sin acceso institucional, push, despliegue ni migraciones al arranque.

