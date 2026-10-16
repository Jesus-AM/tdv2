# Aplicaciones web y móviles desde listas — 2026-10-15

## Implementado

El origen vigente es exclusivamente `Sheet1` / columna **Aplicación** de los Excel proporcionados el 15 de octubre. Se extrajeron **207 web y 26 móviles**, sin nombres duplicados por categoría, sin filas inválidas y sin filas completamente vacías. Se conservan acentos y contenido; sólo se recortan espacios exteriores. No se consulta ni se deduce `ACTIVA`, UR, responsable o tecnología.

`tdv2/Contracts/aplicaciones-2026-10-14.json` contiene los nombres, claves locales estables y metadatos de extracción (archivo, SHA-256, conteos/incidencias). Se versiona y se incluye en compilación/publicación mediante `tdv2.csproj`. El nombre de archivo sólo identifica la procedencia: ni el sitio ni el importador acceden a Downloads, Excel o SQL Server. **No regenerar el archivo desde cero**: sus UUID son identidades persistentes. La extracción repetida conserva las claves por categoría/nombre y produce exactamente los mismos bytes.

`scripts/Extract-ApplicationCatalogs.py` usa sólo la biblioteca estándar de Python. Lee strings compartidos o inline, conserva texto enriquecido visible, omite filas totalmente vacías y reporta filas inválidas/duplicadas. Si falla cualquiera de los conteos o hay incidencias, no sustituye el archivo versionado. Python sólo se requiere para una extracción deliberada, no para la importación en servidor.

### Modelo y compatibilidad

**Nueva migración: `20261015165555_LocalApplicationLists`.** No se cambió ninguna migración anterior.

- `public.aplicaciones_catalogo`: PK compuesta `categoria`/`id`, `nombre` varchar(4000), `clave_lista` nullable y única dentro de su categoría, `disponible` boolean. No inventa IDs institucionales ni columnas obligatorias inexistentes en Excel.
- `public.aplicaciones_importaciones`: categoría, edición, SHA-256 del archivo, fecha UTC y resultado JSON de la última importación confirmada.
- `sii_aplicaciones_moviles/web` permanecen como archivo histórico local. La migración copia **categoría, ID textual original y nombre**, dejando las entradas indisponibles hasta confirmar su pertenencia a las listas. No reinterpreta `ACTIVA`.
- Se conservan tablas anteriores, `sincronizacion_catalogos`, ejecuciones y auditorías históricas. No se escriben formatos ni instantáneas, no se regeneran códigos ni asociaciones de procedimientos.

La migración es **aditiva y compatible a nivel de esquema con la versión anterior**: no retira sus columnas/tablas. No debe convivir el binario anterior con el nuevo después del cambio de origen: el anterior seguiría consultando SQL Server y no reconoce los UUID locales en selecciones nuevas. Detener web y procesadores anteriores durante la actualización; actualizar todos antes de reabrir. El `Down` rechaza retirar las tablas si ya hubo importación o claves locales. Un retroceso de aplicación posterior a guardar UUID necesita una revisión expresa; no basta con restaurar el binario anterior.

### Correspondencias e importación

`ApplicationListImport` compara categoría y **nombre exacto después de Trim**, con comparación ordinal: distingue mayúsculas, acentos y espacios interiores. No usa similitud, número de fila, orden, UR o responsable. La adscripción no interviene.

1. Una `clave_lista` ya vinculada conserva su ID, incluso al repetir la importación.
2. Para una entrada sin vínculo: una única coincidencia anterior no vinculada conserva el ID anterior.
3. Sin coincidencia, usa el UUID `local:…` guardado en el archivo. El prefijo evita colisiones con todos los IDs int32 anteriores, positivos o negativos.
4. Si hay varias coincidencias anteriores, **no se elige ninguna**. Se informa el nombre y los IDs candidatos en `ambiguas`, y se crea una selección local independiente. Las asociaciones antiguas continúan intactas, fuera de nuevas selecciones. Repetir conserva esa selección local y sigue informando la ambigüedad.
5. Las 233 entradas confirmadas quedan disponibles. Las demás se conservan indisponibles; nunca se eliminan.

Ambas listas, disponibilidad, metadatos y auditoría comparten **una transacción PostgreSQL**. Una reserva advisory transaccional serializa importadores; los bloqueos de filas coordinan con `FOR SHARE` en la validación de nuevas selecciones. Un error, cancelación, archivo incompleto, colisión o fallo de auditoría revierte ambas listas. La repetición puede registrar un nuevo intento/fecha, pero no duplica entradas ni cambia IDs.

Los comandos imprimen conteos `nuevas`, `correspondencias`, `conservadas`, `retiradas` y `ambiguas` por categoría. `--check-applications` calcula el plan sin publicar cambios ni auditoría; una comprobación no sustituye el reporte de la importación definitiva, que revalida bajo bloqueo. Las excepciones operativas muestran sólo diagnóstico sanitizado/tipo, sin cadenas de conexión. El arranque normal no importa ni migra.

### Formulario y sincronizaciones

Las opciones Aplicaciones Web / Aplicaciones móviles conservan multiselección, búsqueda, casillas, cero/una/cantidad, nombres completos, apertura al primer clic, reservas y autoguardado. Las rutas existentes sólo devuelven ID, nombre y categoría desde PostgreSQL, bajo autorización del formato. Sin importación muestran el estado pendiente; un esquema ausente es error, no catálogo vacío exitoso.

El JSON conserva arrays separados `aplicacionesWeb` / `aplicacionesMoviles` con `{id, descripcion, catalogo}`. El servidor confirma nuevos IDs contra el catálogo local correspondiente y toma de allí el nombre; no confía en etiquetas del navegador. Mantener una selección histórica permite editar otros campos sin perderla. Una selección retirada y desmarcada no puede volver a activarse como nueva. Las selecciones dormidas al cambiar de herramienta siguen el comportamiento anterior. Una o más aplicaciones válidas cuentan como **un requisito condicional**, sin variar el avance por cantidad.

**Sincronizar SQL Server** ejecuta solamente `sii` (UR) → `sii_modulos`. ILDA sigue independiente. Se retiraron SELECT, lectores de aplicaciones, publicación SQL y tarjetas/contadores/mensajes operativos de aplicaciones. El historial almacenado no se borra; la pantalla omite resultados operativos de las fuentes retiradas. Una ejecución histórica sólo acredita éxito de UR/módulos si ambos resultados lo confirman. Se conserva `ConnectionStrings:Sii` para esos dos catálogos.

## Actualizar e importar posteriormente

La actualización conjunta vigente incluye `20261016190000_RemoveUsedMedia`. Antes de operar, seguir el respaldo y orden de [Catálogo vigente, limpieza exacta, respaldo y actualización Windows/Ubuntu](CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md). La migración de listas sigue siendo aditiva; la posterior limpieza de Medios tiene su propio alcance autorizado. No es necesario reimportar listas ya confirmadas por esta mejora.

Estos comandos se documentan para el operador; **no se ejecutaron contra una base institucional**. Revisar respaldo, destino y configuración existente antes de operar. No copiar ni imprimir secretos. La cuenta de migración necesita DDL local; la cuenta que importa necesita SELECT/INSERT/UPDATE en las dos tablas nuevas y escritura de auditoría conforme al mecanismo de permisos existente. El usuario web necesita consultar el catálogo. No se requieren nuevos permisos sobre tablas SQL Server de aplicaciones.

Orden: preparar paquete → detener versiones/procesadores anteriores mediante el supervisor existente → aplicar EF → comprobar/importar desde el paquete nuevo con la configuración PostgreSQL del destino → revisar conteos/incidencias → iniciar todos los procesos con la nueva versión. No activar horarios automáticamente.

Construcción local, desde la raíz con .NET 10 y Node/npm en PATH:

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-application-lists
```

Windows, desde la raíz, con User Secrets existentes y destino previamente verificado:

```powershell
dotnet ef database update 20261016190000_RemoveUsedMedia --project tdv2 --startup-project tdv2 -- --environment Development
dotnet .artifacts/publish-application-lists/tdv2.dll --environment Development --check-applications
dotnet .artifacts/publish-application-lists/tdv2.dll --environment Development --import-applications
```

EF aplica las migraciones pendientes en orden, incluida `20261014180408_SiiApplicationsCatalog` si aún no está aplicada. Si el entorno Windows usa configuración externa de producción, sustituir `Development` por `Production`; no copiar credenciales entre entornos.

Ubuntu: desde la raíz del código de esta versión con SDK/EF y la configuración privada PostgreSQL ya suministrada al proceso:

```bash
dotnet ef database update 20261016190000_RemoveUsedMedia --project tdv2 --startup-project tdv2 -- --environment Production
```

Después, **desde el directorio del paquete nuevo** (no desde un paquete anterior), con esa misma configuración externa:

```bash
dotnet tdv2.dll --environment Production --check-applications
dotnet tdv2.dll --environment Production --import-applications
```

El comando EF requiere un checkout de esta versión y SDK/EF Core. Si Ubuntu sólo tiene runtime, ejecutar ese comando desde el equipo de operación autorizado que tenga el código/SDK y la configuración del mismo destino PostgreSQL; después ejecutar los dos comandos del paquete en Ubuntu. No instalar un segundo migrador ni crear tablas manualmente. El importador sólo requiere runtime .NET 10 y `Contracts/aplicaciones-2026-10-14.json`, incluido en el paquete. No necesita Python, Node, Excel ni las rutas iniciales de Downloads.

Los comandos de importación terminan con código 0 sólo tras confirmar el commit (o completar el plan sin escrituras); devuelven 1 ante fallo. No inician el sitio ni los consumidores de la cola. No existe pantalla de importación.

## Pruebas y límites

La extracción real confirmó 207/26, cero duplicados/filas inválidas y repetición idéntica. Cuatro pruebas Python usan XLSX sintéticos para espacios, acentos, vacíos, filas inválidas, duplicados, texto enriquecido y estabilidad por categoría al reordenar.

Pruebas PostgreSQL nativo aislado: migración desde el esquema anterior con IDs negativos/históricos/enviados; importación inicial/repetida/simultánea; ambigüedad; coincidencia exacta y separación de categorías; rechazo de listas inválidas; textos de 4000 caracteres; rollback conjunto de catálogos/metadatos/auditoría; formulario autorizado; selección múltiple, retiro, cambio de herramienta, recarga, avance y enviado inmutable; retirada concurrente con conexiones independientes. **55 casos nativos distintos correctos**, **59 comprobaciones EF**, **11 recorridos** de sistemas/aplicaciones y **7** de sincronizaciones con Edge/ASP.NET/PostgreSQL. Se verificaron los comandos del paquete como procesos independientes en Production, con configuración sintética aislada. Paquete construido y manifiesto publicado idéntico al versionado. Tipos/build, 114 casos de dominio/HTTP con dobles y 88 frontend correctos. Resultados intermedios, repetición del caso ajustado y artefactos en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).

**Pendiente:** ejecutar el plan/importación con la réplica real para conocer sus correspondencias/ambigüedades concretas, aceptación operativa en Ubuntu y dispositivos táctiles físicos. No se inspeccionaron catálogos institucionales ni bases existentes: cero incidencias en los Excel no equivale a cero ambigüedades contra una base real. Las fuentes SII/ILDA y autenticación se simulan en las pruebas; la persistencia, bloqueos y transacciones usan PostgreSQL real desechable.

Archivos principales: `ApplicationListImport.cs`, `ApplicationCatalogSchema.cs`, `LocalCatalog.cs`, `SystemApplications.cs`, `FormEditingService.cs`, entidades/configuraciones y snapshot EF, `Program.cs`, `ServiceRegistration.cs`, `CatalogSource.cs`, `CatalogPublication.cs`, `SyncCoordinator.cs`, `SynchronizationQueries.cs`, `sync-presentation.ts`, `Sincronizaciones.tsx`, manifiesto/script de extracción y pruebas de aplicaciones/sincronización/migraciones/navegador. Las tablas y componentes de captura conservan su estructura actual.
