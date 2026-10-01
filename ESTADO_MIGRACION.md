# Estado de migración TDV2

Inicio: 2026-09-30. Última suite completa: **2026-10-01 06:59 UTC**; ajuste de conector verificado **07:01 UTC**. **Migración en curso; no apta aún para sustituir Laravel.**

Configuración → Sincronizaciones está implementado: SII, réplica íntegra ILDA, consumo local en formatos, cola/programación, reservas, recuperación y auditoría. Se verificó en PostgreSQL nativo aislado y React con fuentes sintéticas. **No se validó conectividad institucional ni operación Ubuntu; este bloque no se declara cerrado frente a servicios reales.**

El bloque de colaboradores mediante administración delegada, Actuar como usuario, vista de consulta por rol/área y auditoría está implementado y verificado con React, ASP.NET y PostgreSQL nativo aislado. **No está cerrado frente a servicios institucionales:** Microsoft es un doble HTTP y Nexo un publicador SQL sintético. No se ejecutó Nexo Laravel real ni se consultó el esquema desplegado.

## Inspección y límites

### User Secrets y Visual Studio — 2026-10-01

**Implementado:** se inicializó `UserSecretsId=0397954e-63d2-48b6-a581-a586951257cc` en tdv2.csproj y se trasladaron nueve claves desde la configuración DPAPI existente: `Microsoft:TenantId/ClientId/ClientSecret/PublicOrigin`, `ConnectionStrings:Tdv2/Nexo/Sii/Ilda`, `Synchronization:IldaEnabled=false`. El destino es `%APPDATA%\Microsoft\UserSecrets\0397954e-63d2-48b6-a581-a586951257cc\secrets.json`, fuera del repositorio y con permisos privados. **User Secrets es JSON editable sin cifrado DPAPI**, por petición del usuario. No se imprimieron valores ni se pusieron secretos en React, launchSettings o archivos versionados. No se volvió a leer Herd.

`WebApplication.CreateBuilder` carga el archivo mediante la configuración estándar de Development; no se añadió una fuente especial ni se modificó autenticación/autorización. El perfil https de tipo Project queda primero y abre el navegador en https://localhost:7136; también se fijó ActiveDebugProfile=https en el archivo personal existente tdv2.csproj.user (ignorado por el repositorio). Se preservó el retorno /connect y la misma conexión a tdv2_local_validation @ 127.0.0.1:51476, con tdv2_local_app. `StartDatabase` prepara exclusivamente el PostgreSQL aislado para F5 y deja automática apagada; no aplica migraciones ni inicia procesador. ILDA permanece deshabilitada.

El lanzador anterior ya no inyecta Microsoft/ConnectionStrings/Synchronization desde DPAPI. `Import` y `Configure` quedan retirados y sólo indican dónde editar. `MigrateSecrets` sólo escribe si el archivo todavía no existe: repetirlo respeta todo el archivo y las claves eliminadas. `Start` usa el mismo User Secrets; detecta variables de entorno explícitas con precedencia y no las modifica silenciosamente. `database.clixml` continúa para administrar el clúster; `institutional.clixml` queda como copia histórica cifrada sin lectura en los arranques. Reiniciar depuración después de editar, pues las opciones Microsoft usan IOptions.

**Probado:** compilación de backend/verificador sin errores ni advertencias. Nueve valores trasladados y comparados con sus originales; proveedor efectivo `secrets.json` comprobado para las nueve claves en el entorno del perfil https. Las cuatro cadenas se analizaron con proveedores .NET sin abrir conexiones institucionales. Prueba de edición temporal de ClientId a GUID sintético: conservada tras MigrateSecrets/Import/Configure, observada por el proveedor y archivo original restaurado byte por byte. No se ejecutó login con ese GUID ni se creó usuario simulado.

Arranque directo `dotnet run --project tdv2/tdv2.csproj --no-build --no-restore --launch-profile https`, sin el lanzador DPAPI: **4/4 comprobaciones Edge** de HTTPS, React/JSON, 401/CSRF y /connect 302 con retorno correcto, sin seguir la redirección a Microsoft. PostgreSQL confirmó cuatro migraciones, cero usuarios/formatos/UR/ejecuciones y automática=false. La restauración inicial de dependencias del verificador fue bloqueada por la red del sandbox; se repitió autorizadamente y la compilación final pasó.

Evidencia: [proveedores y conservación de ediciones](docs/migracion/evidencia-user-secrets.json), [navegador con perfil https](docs/migracion/evidencia-user-secrets-navegador.json). El lanzador `Start` actualizado también pasó las mismas cuatro comprobaciones sin inyección DPAPI. Las pruebas usan la aplicación real anónima; no sustituyen Microsoft/Nexo con simuladores. **Se verificó el perfil usado por F5 mediante CLI; no se operó la interfaz ni el depurador Visual Studio.** No se probaron conexiones institucionales ni permisos reales, y no se declara autenticación validada.

Estado al terminar: ASP.NET detenido, puertos 7136/5064 libres; **PostgreSQL aislado queda activo en 51476 para el próximo F5**, automática=false y cero ejecuciones. No hace falta una terminal abierta ni se instalaron servicios. 154/154 archivos de código de Herd conservan el hash original; el .env no se leyó en este bloque.

Comandos desde la raíz, con la misma cuenta Windows:

```powershell
# Sólo fue necesario una vez; repetir no sobrescribe el archivo existente:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 MigrateSecrets
dotnet build tests/TDV2.LocalConfigurationVerification/TDV2.LocalConfigurationVerification.csproj -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-UserSecrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
# En Visual Studio: tdv2 como inicio, perfil https, F5. Alternativa verificable:
dotnet run --project tdv2/tdv2.csproj --no-build --no-restore --launch-profile https
# Segunda terminal en ClientApp: npm.cmd run test:local
# Al terminar: Mayús+F5 o Ctrl+C, según cómo se inició; luego, desde la raíz:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Stop
```

**Pendiente:** registro del retorno en Entra si falta, vigencia del secreto, consentimiento, Microsoft/Graph, red/TLS y publicación/permisos Nexo, conectores SII/ILDA reales y catálogo local UR. La programación sigue apagada y no hubo sincronizaciones. Operación personal detallada: [Visual Studio, secretos y arranque](docs/ARRANQUE_LOCAL_WINDOWS.md).

### Configuración personal importada de Herd — 2026-10-01

Registro histórico anterior a User Secrets. Las acciones Import/Configure y la inyección DPAPI aquí descritas quedaron sustituidas por el apartado vigente anterior.

**Implementado:** con autorización expresa se leyó `C:\Users\Jesus Arenas\Herd\tdv2\.env` sin modificarlo. `scripts/Local-Tdv2.ps1 Import` guarda Microsoft, Nexo, SII e ILDA mediante DPAPI y reemplazo atómico dentro del directorio privado existente. Valida claves requeridas, GUID, puertos, TLS y contratos antes de publicar; rechaza duplicados/interpolaciones no resueltas sin mostrar valores. `Start` carga las conexiones cifradas exclusivamente en el backend; `Configure` conserva SII/ILDA al corregir Microsoft/Nexo. No cambia React, diseño ni fuentes.

Mapeo confirmado contra Laravel: `MSGRAPH_TENANT_ID/CLIENT_ID/SECRET_ID` → Microsoft (SECRET_ID contiene el valor del secreto); `NEXO_DB_*` → Npgsql; **`MSSQL_*`** → SQL Server SII; `ILDA_DB_*` → MySQL ILDA. Se conservan los parámetros TLS explícitos. `NEXO_DB_SSLROOTCERT` e `ILDA_DB_SSL_CA` estaban vacíos; no se aportaron CA personalizadas, cuya necesidad real no se verificó. ILDA sin CA usa Preferred; no acredita cifrado negociado. La clave sigue siendo tdv2; no se importa un ID de aplicación ni se amplían permisos. Se validan los destinos fijos poa.UNIDADES_RESPONSABLES_POA e ilda_db.

Se excluyeron `DB_*`, `APP_KEY`, `NEXO_APP_ID`, el retorno Laravel y opciones de activación. Se conservan **tdv2_local_validation @ 127.0.0.1:51476**, sus credenciales originales y **https://localhost:7136/connect**. Automática=false, ILDA deshabilitada, sin procesador ni sincronizaciones ejecutadas. No se conectó a bases institucionales ni se aplicó DDL en esta importación.

**Probado:** **8/8 verificaciones sintéticas** de parseo, preservación de contraseñas, exclusiones, TLS, DPAPI y rechazos. La prueba necesita el perfil Windows real: el sandbox no puede proteger/descifrar con DPAPI de esa cuenta. Importación del archivo autorizado con descifrado idéntico y hashes de `.env`, marcador y credenciales locales sin cambios. **4/4 comprobaciones en Edge real** después de importar: HTTPS confiable, React → ASP.NET, CSRF/401 anónimo y /connect **302** con redirect_uri local correcto. PostgreSQL confirmó 4 migraciones, 0 usuarios/formatos/UR/ejecuciones y automática=false. Esto prueba carga de configuración y persistencia local del intento OAuth, no inicio de sesión.

La primera prueba posterior a importar siguió la redirección hasta la página pública de Microsoft y falló por recursos bloqueados. Se corrigió el test: `fetch` con `redirect: manual`, leyendo las cabeceras locales mediante Chromium. Una repetición intermedia agotó la espera del evento de respuesta opaca; la versión final pasó sin seguir el redirect. No hubo credenciales introducidas, canje de tokens ni usuario autenticado; Nexo/SII/ILDA no se consultaron. Se mantienen los fallos como antecedente, no como evidencia de éxito.

Evidencia sin secretos: [importación a las 07:56 UTC](docs/migracion/evidencia-importacion-local.json), [reglas sintéticas](docs/migracion/evidencia-importador-sintetico.json) y [arranque configurado a las 07:59 UTC](docs/migracion/evidencia-arranque-configurado.json). La evidencia anterior con /connect 503 se conserva como histórica. Al terminar se detuvieron ASP.NET y PostgreSQL aislado, con los puertos 7136/5064/51476 libres; configuración y datos permanecen.

**Pendiente:** comprobar o registrar personalmente en Entra el retorno Web `https://localhost:7136/connect`; no se consultó su registro. Faltan validar vigencia del secreto, consentimiento, MFA, Graph y login/logout completos. Las conexiones Nexo/SII/ILDA están **configuradas, no comprobadas**: red/VPN, TLS, credenciales, vistas/permisos/publicador y fuentes institucionales siguen pendientes. La base local continúa sin catálogo UR; importar configuración no publica catálogos ni concede acceso. No se declara completado ningún bloque institucional.

Comandos reproducibles desde la raíz, con la misma cuenta Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-LocalImport.ps1
# Import ya ejecutado. Repetir sólo al cambiar la referencia, con la aplicación detenida:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Import
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Start
# Segunda terminal opcional durante la verificación:
cd ClientApp
npm.cmd run test:local
# Ctrl+C en Start. Desde la raíz:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Stop
```

Instrucciones de uso y detalle de equivalencias: [arranque local](docs/ARRANQUE_LOCAL_WINDOWS.md). La configuración cifrada no se copia a la documentación ni al frontend.

### Arranque local personal en Windows — 2026-10-01

Esta sección describe la preparación **anterior** a la importación autorizada de arriba; sus ausencias de configuración y el 503 ya no describen el estado actual.

**Implementado:** `scripts/Local-Tdv2.ps1` ofrece Prepare/Configure/Start/Stop/Status. Usa el perfil https real: **https://localhost:7136** y http://localhost:5064; React compilado lo sirve ASP.NET, sin terminal Vite. La base persistente **tdv2_local_validation @ 127.0.0.1:51476** está dentro de `.artifacts/local-validation/data`, separada de servicios/bases existentes. Se aplicaron únicamente allí 001–004 con registro de hashes y cuenta de aplicación sin DDL. Credenciales locales aleatorias protegidas por DPAPI y directorio privado; las credenciales Microsoft/Nexo personales aún no se introdujeron. Inicio pausa automática, deshabilita ILDA, excluye conexiones SII/ILDA heredadas y no arranca trabajadores. No se instalaron servicios ni tareas; no se cargaron fixtures, usuarios ni permisos sintéticos.

**Probado:** Prepare compiló ASP.NET sin errores/advertencias y React con Vite. Segunda preparación conservó el esquema y no reaplicó migraciones. Arranque HTTPS con certificado confiable del usuario ya existente, sin instalar ni exportar otro. **4/4 comprobaciones en Edge real**, sin ignorar errores TLS: portada, props React → ASP.NET, reintento anónimo 401/CSRF y /connect 503 por falta de configuración. Cero errores JavaScript, ninguna sesión ficticia. PostgreSQL confirmó 4 migraciones, 0 usuarios/formatos/UR/ejecuciones y automática=false. Un segundo Start se rechazó sin afectar al primero; se comprobaron Ctrl+C y Stop. Al finalizar quedaron detenidos ASP.NET y este PostgreSQL, conservando el entorno para uso personal.

**Bloqueos pendientes:** TenantId y ClientId institucionales, valor de ClientSecret, registro del retorno Web **https://localhost:7136/connect**, consentimiento de scopes y conexión PostgreSQL Nexo con sus vistas/permisos/publicación tdv2. El origen HTTPS y la conexión TDV2 aislada ya los proporciona el script. No se autenticó Microsoft ni se conectó Nexo real; tampoco se validaron fuentes institucionales. La base no contiene catálogo UR: incluso tras configurar identidad, probar formatos/alcance exige cargarlo expresamente en esta base, con claves/empleados correspondientes a Nexo. El modo de arranque no procesa sincronizaciones.

Se corrigieron una espera del lanzador pg_ctl por captura de streams en Windows y el tratamiento de stderr para un servidor detenido. La prueba nueva admite el BOM de launchSettings y realiza llamadas HTTP en Edge para usar su confianza TLS real. No cambió código de autenticación, autorización, pantallas, tema ni tipografías. Las pruebas de esta sección son de **arranque real anónimo**, distintas de las suites autenticadas con dobles anteriores.

Instrucciones personales, permisos Nexo, comandos y parada: [ARRANQUE_LOCAL_WINDOWS.md](docs/ARRANQUE_LOCAL_WINDOWS.md). Informe: [evidencia-arranque-local.json](docs/migracion/evidencia-arranque-local.json); capturas `.artifacts/local-validation/browser/`. Repetir `npm.cmd run test:local` desde ClientApp, con Start abierto. Las peticiones externas del navegador se bloquearon, incluidas fuentes; no acredita aceptación visual ni inicio institucional.

### Inspección de la migración

- Destino original inspeccionado: solución tdv2.slnx, proyecto Web API net10.0, plantilla WeatherForecast y OpenAPI 10.0.12. SDK 10.0.401/runtime 10.0.12. No existían Git ni AGENTS.md; no se inicializó Git.
- Herd es exclusivamente referencia de lectura. Durante la migración inicial no se leyeron .env ni credenciales; la excepción posterior autorizada para configuración está documentada arriba. No se ejecutó Laravel ni se leyeron respaldos/logs; no se conectó ninguna base existente. Todos los cambios están en el destino ASP.NET.
- Inventario: 27 rutas propias, 27 tablas declaradas, 119 métodos PHP y 12 pruebas frontend originales. **154/154 hashes de Herd siguen iguales** al manifiesto inicial. Las pruebas PHP son criterios de aceptación; no se ejecutaron.
- Para este bloque se revisaron antes de implementar CollaboratorController, RepresentationController, PreviewController, ConfigurationController, NexoDelegation, NexoRepresentation, RepresentationAudit, PreviewSession/ViewContext, FormAccess, middleware, rutas/configuración y pruebas asociadas.
- Para sincronizaciones se revisaron InstitutionalSource/InstitutionalSync, IldaSource/IldaSync/IldaInventory, SyncManager/SyncController, comandos, configuración y migraciones 2026_09_25_000001/2026_09_30_000001, además de InstitutionalSourceTest, InstitutionalSyncTest, IldaInventoryTest y ConfigurationSyncTest. Contratos y diferencias: [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md).
- Contratos, diferencias y recuperación: [DELEGACION_REPRESENTACION.md](docs/migracion/DELEGACION_REPRESENTACION.md). Configuración de identidad/SQL: [AUTENTICACION_POSTGRESQL.md](docs/migracion/AUTENTICACION_POSTGRESQL.md). Inventario original: [INVENTARIO.md](docs/migracion/INVENTARIO.md).

## Implementado

### Base, autenticación y formatos conservados

- React, TypeScript, nueve pantallas, MUI, tema, CSS, declaración de tipografías, portada y navegación originales. Comunicación JSON del mismo origen sustituye Inertia. El historial sólo guarda URLs, nunca props ni identidades.
- Microsoft conserva /connect inicio/callback, tenant GUID, state de un uso con 600 segundos de vigencia, vínculo al navegador y PKCE S256. Canje e identidad Graph /me desde servidor, dominio institucional y object ID GUID; Nexo se valida antes de persistir. Logout revoca la sesión y redirige al destino Microsoft fijo.
- Sesión cifrada PostgreSQL de dos horas, cookie Secure/HttpOnly/SameSite=Lax y CSRF. Graph cifra/refresca tokens con bloqueo; foto fallida no concede permisos ni termina sesión.
- Nexo sigue siendo autoridad. Aplicación única resuelta por **clave literal tdv2**, ID positivo publicado y nivel 2; identidad individual, empleado textual, roles, módulos y parentesco/ruta vigentes. No se aceptan identidad, roles o UR de React como autorización.
- Formatos sólo para UR activas nivel 2/3. Áreas subordinadas a nivel 3 comparten su formato cuando están autorizadas; no se crean formatos por áreas inferiores. Administrador conserva consulta institucional y edición limitada a su rama nivel 2, incluso combinando roles.
- Respuestas canónicas, JSON, versión, avance calculado en servidor, autor y timestamps. GET no crea filas; bloqueo de UR serializa primera creación y actualización. Versión obsoleta devuelve 409; error SQL revierte el cambio.
- Autorización en HTML y endpoints, revalidada por solicitud. Nexo caído no concede permisos de respaldo.

### Administración delegada

- Búsqueda, alta y retiro mediante vistas y funciones publicadas por Nexo. Nexo permanece Laravel; no se escriben sus tablas internas.
- Intersección de responsabilidad/adscripción válida con nexo_delegacion; niveles_delegacion=[2], como la configuración Laravel. Ser administrador no basta para delegar.
- Roles asignables sólo desde nexo_delegacion_roles; rol_id enviado por React se ignora. Nexo comprueba persona/origen/alcance y, tras el alta central, TDV2 vuelve a leer identidad publicada antes de crear el vínculo.
- Colaborador local: formato 2/3 correspondiente a su adscripción. Colaborador de áreas dependientes/global: formatos de la rama nivel 2 otorgante. Siempre se requiere vínculo local y concesión central vigente coincidente.
- Altas concurrentes serializadas por persona/origen/rol. Retiro local y auditoría se confirman antes de llamar Nexo; fallo central devuelve 202, conserva acceso revocado y muestra retiro pendiente con reintento. Se conserva la concesión si otro vínculo local activo la utiliza.
- Búsquedas distinguen vacío, falta de autorización y fallo de conexión, sin convertir errores en listas vacías.

### Actuar como usuario y vista por rol/área

- Ambas herramientas permanecen en Configuración → Pruebas de acceso. Configuración exige administrador y módulos vigentes; representación además exige capacidad independiente de Nexo.
- Buscar/iniciar/validar/finalizar usan nexo_a{ID}_representacion(jsonb), actor real en servidor y token cifrado. Delegación durante representación usa esa misma función con actor real + token; nunca suplanta directamente al actor ante las funciones de delegación.
- Estado efectivo verificado en servidor por solicitud: actor/persona/ID, caducidad local y central, autorización vigente, módulos, duración y escritura. No se amplían permisos si Nexo falla, revoca o devuelve datos ambiguos.
- Identidad Microsoft real separada del perfil representado. Franja original muestra persona, actor, vigencia, consulta/escritura y salida. Caducidad/revocación bloquean el acceso; volver a permisos propios requiere salida explícita. La pantalla de acceso restringido también conserva un botón para terminar el contexto.
- Vista de prueba sólo escenario por rol/área, 30 minutos y consulta. No acepta modo usuario/email; bloquea escrituras y búsqueda delegada. Configuración, contextos anidados y mutaciones desde pestañas con contexto obsoleto se rechazan en servidor.
- Contexto cifrado/revisión por sesión en tdv2_access_contexts. Bloqueos y comparación de revisión evitan sustituciones concurrentes durante escrituras. Dos inicios simultáneos admiten uno y rechazan el otro.
- DELETE de salida y logout siguen disponibles ante falla de Nexo, con CSRF. Se descarta el contexto local; se registra si el cierre central pudo confirmarse.

### Auditoría y persistencia local

- Cambios propios y representados registran actor real, identidad representada cuando aplica, acción, recurso, contexto y resultado. Se registran también búsquedas y rechazos del bloque, incluido CSRF. No se serializan tokens, cabeceras, credenciales, errores SQL ni el motivo libre.
- Guardado de formato, alta/revocación/retiro local, inicio/salida de contexto y logout comparten transacción con su auditoría. Si la bitácora falla, el cambio local se revierte.
- No hay transacción distribuida entre TDV2 y Nexo. Un alta central puede permanecer sin vínculo local tras un error; no habilita edición y se recupera por reintento. Un inicio central sin commit local intenta finalizarse. Retiros y cierres centrales fallidos conservan estados explícitos; ver límites en el documento de contratos.
- database/001_core.sql es bootstrap sólo de base vacía; 002_aspnet_sessions.sql añade sesiones/OAuth y **003_access_contexts.sql** el estado cifrado. No convertir ni aplicar sobre bases existentes. La aplicación nunca ejecuta DDL al arrancar.
- Llaves Data Protection bajo tdv2/.runtime/keys, DPAPI Windows; las pruebas usan llaves efímeras. Nuevo login necesario para cookies Laravel y tickets ASP.NET anteriores a este bloque.

### Configuración → Sincronizaciones

- Conectores de producción SQL Server/MySQL con consultas fijas: SII sólo `poa.UNIDADES_RESPONSABLES_POA`, diez columnas y MAX(EJERCICIO) por defecto; ILDA toda `ilda_db.informacion_area`, todas las filas/columnas en JSON original y auxiliares locales. No hay escrituras remotas ni consultas remotas al abrir formatos.
- Descarga completa antes de publicar, validación de columnas/IDs/ejercicio/ciclos, límites 100000 filas y 128 MiB ILDA, reducción superior al 20% rechazada. Bajas lógicas mantienen relaciones/respuestas/versiones. Empleado SII conserva ceros y se compara con Nexo textual.
- Inventario exclusivamente PostgreSQL con igualdad exacta ur2 ↔ clave; formulario hasta 200 filas, fuente ILDA y Trámite / servicio desde informacion_generada. Incorporar nuevos IDs al GET no persiste ni sustituye respuestas guardadas, aun si el origen cambia o desaparece.
- Cola persistente y manual sii/ilda/ambas (HTTP 202); automática siempre incluye SII, ILDA optativa, intervalos/horario originales y zona America/Ciudad_Juarez mediante base IANA NodaTime. Configuración optimista y autorizada sólo al administrador real con módulos Nexo; rechaza CSRF ausente, vista de prueba y representación en páginas/endpoints.
- Reserva PostgreSQL con propietario UUID, 30 minutos, exclusión entre procesos, comprobación antes y después de cada transacción y recuperación de interrupciones sin publicar desde trabajadores vencidos. Cada catálogo/resultados/auditoría son atómicos; éxito parcial se muestra como parcial. Un cambio concurrente de catálogo antes de guardar formato exige revalidación (409).
- `--sync-worker` opera por separado del sitio, publica latido cada minuto sin renovar por ello la reserva; `--sync-once` hace un ciclo y `--sync-check=sii|ilda|ambas` sólo comprueba. Automática e ILDA deshabilitadas por defecto. No se instalaron tareas, cron ni servicios.
- Pantalla Sincronizaciones idéntica a Laravel salvo import de transporte; historial, progreso, estados, alertas y tipografías conservados. Dependencias, diferencias de tipos MySQL, zona horaria, esquema y operación Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). DDL explícito `database/004_synchronizations.sql` únicamente para la base ASP.NET aislada preparada.

## Probado y evidencia

| Ejecución | Resultado | Alcance |
|---|---|---|
| dotnet build tdv2.slnx -p:NuGetAudit=false | 0 errores, 0 advertencias | Compilación; auditoría NuGet desactivada en esa ejecución |
| dotnet run --project tests/TDV2.Verification --no-restore | **56/56** | Dominio/HTTP con identidad, Nexo y almacenamiento en memoria sintéticos |
| npm.cmd run types:check | Código 0 | TypeScript |
| npm.cmd run test:forms | **21/21** | 12 originales + 9 de transporte; Axios/navegador simulados |
| npm.cmd run build | Código 0 | Bundle Vite |
| scripts/Test-NativePostgres.ps1 | **91/91 grupos** | 90 casos HTTP/SQL (30 del bloque) + un grupo de 31 recorridos en Edge |
| scripts/Test-NativePostgres.ps1 -SyncOnly (ajuste final SII) | **30/30** | Tras conservar booleanos textuales SII como 1/0; fuentes ADO.NET sintéticas |
| dotnet publish tdv2/tdv2.csproj -c Release --no-restore -o .artifacts/publish | Código 0 | Paquete construido; CLI inválida devuelve 2 sin conectar a fuentes |

PostgreSQL **18.6 nativo para Windows**, Edge **154.0.4258.37** sin interfaz. SQL, transacciones, bloqueos, cookies, CSRF y pantallas reales; Microsoft simulado por HTTP y Nexo simulado mediante vistas/funciones publicadas en PostgreSQL. La cuenta Nexo de pruebas sólo lee vistas y ejecuta funciones autorizadas, sin acceso directo a tablas internas. No se probó el publicador Laravel institucional. SII/ILDA usan lectores ADO.NET sintéticos con el SQL/mapeo/publicación de producción: no hubo conexión TDS/MySQL ni validación TLS o tipos nativos institucionales.

Se validaron loopback, puerto no convencional, base, usuario y SHOW data_directory antes de escribir. La suite creó un clúster nuevo dentro del destino y **lo dejó detenido**. No inició/detuvo servicios PostgreSQL existentes ni tocó sus bases.

Evidencia actual sin credenciales:

- [90 casos PostgreSQL y grupo de navegador](docs/migracion/evidencia-sincronizaciones-postgresql.json).
- [10 recorridos del módulo Sincronizaciones](docs/migracion/evidencia-sincronizaciones-navegador.json).
- [Regresión de 12 recorridos de acceso](docs/migracion/evidencia-sincronizaciones-regresion-acceso.json) y [9 recorridos de autenticación/formatos](docs/migracion/evidencia-sincronizaciones-regresion-formatos.json).
- [30 casos tras el ajuste final de conversión SII](docs/migracion/evidencia-sincronizaciones-ajuste-conector.json).

Suite completa: `.artifacts/native-postgres-20261001-065537-4cce3a7f/`. Ajuste posterior de conector: `.artifacts/native-postgres-20261001-070037-0c2a3c8e/`. Capturas: sincronizaciones-completada.png, sincronizaciones-historial.png y formato-ilda-local.png. La comprobación posterior -SyncOnly verifica el ajuste puntual de booleanos SII (1/0, igual que Laravel); no repite ni se suma a los 31 flujos del navegador.

Evidencia histórica de delegación:

- [60 casos PostgreSQL y grupo navegador](docs/migracion/evidencia-delegacion-postgresql.json).
- [12 recorridos de colaboradores/representación/vista/auditoría](docs/migracion/evidencia-delegacion-navegador.json).
- [9 recorridos de regresión de autenticación/formatos](docs/migracion/evidencia-regresion-navegador.json).

Artefactos históricos de delegación: `.artifacts/native-postgres-20261001-060357-784fc391/`, incluidos JSON, logs sintéticos y capturas representacion-activa.png, vista-rol-area.png, formato-postgresql.png y ur-denegada.png. Los informes anteriores [PostgreSQL](docs/migracion/evidencia-postgresql.json) y [navegador](docs/migracion/evidencia-navegador.json) se conservan como evidencia histórica del bloque anterior.

Casos verificados de sincronizaciones:

- SELECT fijo SII, ejercicio único, diez columnas, empleado textual y espacios; vacío, duplicados, ciclos, descarga interrumpida/incompleta, tamaño y caída inesperada conservan el catálogo anterior.
- ILDA completa (más de 200 registros), columnas adicionales, nulos/vacíos/binarios reversibles, coincidencia exacta de claves. GET sólo local incluso con fuente caída/deshabilitada; agrega IDs sin persistir y conserva respuestas/versiones tras cambios y desapariciones.
- Exclusión entre solicitudes y trabajadores, 202 sin descarga HTTP, recuperación de proceso hijo terminado, reserva vencida antes/durante publicación y trabajador viejo incapaz de liberar/publicar sobre reserva nueva. Latido no prolonga reserva.
- Publicación por fuente transaccional; ambas direcciones de fallo parcial, errores SQL/auditoría revierten catálogo/metadatos/resultado, solicitud y configuración. Comprobación CLI sin cola/publicación; reintentos explícitos convergen.
- Programación diaria y cambio estacional Juárez, intervalo y zona inválidos, dos versiones concurrentes, automática siempre SII/ILDA opcional, intervalos perdidos agrupados y activación ILDA validada.
- Administrador real, módulos padre/hijo, Nexo, CSRF y contexto exigidos en páginas y endpoints. Vista de prueba y representación rechazan configuración; cambio SII concurrente exige revalidar antes de guardar.
- React muestra pendiente, ejecutando, completada, parcial y fallida; conserva borrador de programación ante 409, historial y opciones automáticas, recupera tras error SQL/interrupción y guarda información ILDA local sin tocar MySQL. **Cero errores JavaScript de página** en los tres scripts.

Casos conservados de delegación y representación:
- Búsqueda permitida/vacía/denegada/fallida; administrador sin delegación, empleado no coincidente, rol no asignable, nivel 3 no delegable, persona/origen/rama forjados, administrador combinado sin ampliación.
- Persona de nivel 4 autorizada en formato nivel 3; colaborador local/global; GET/PUT de UR ajenas o inferiores denegados. Identidad no publicada tras alta impide vínculo; reintento y altas simultáneas convergen.
- Revocación local persiste si Nexo falla; reintento confirma retiro. Falla inducida en auditoría revierte alta y revocación locales.
- Administrador sin capacidad no representa; capacidad sin escritura no la concede. Búsqueda fuera de alcance, token cifrado/no publicado, actor real intacto, módulos revocados, escritura/consulta y alcance.
- Vencimiento local y central, revocación, Nexo no disponible, pestaña obsoleta, contextos anidados y dos inicios simultáneos. Regreso explícito al actor, incluido Nexo caído.
- Formato y auditoría atómicos durante representación; fallo de auditoría al iniciar intenta compensación central. Logout representado revoca sesión/contexto y audita atómicamente; CSRF rechazado identifica ambas personas sin cambios.
- Vista por rol/área sólo consulta; modo usuario, área inválida, búsqueda delegada, configuración y escrituras directas rechazados. Caducidad no restaura permisos reales automáticamente.
- Navegador: altas/retiros y reintento, mensajes diferenciados, ramas local/global, herramientas separadas, capacidad ausente, representación de consulta/escritura, autoguardado, revocación/vencimiento, salida con Nexo caído, vista de prueba y elusión de endpoints desde el navegador. PostgreSQL confirma actor/representado y ausencia de tokens en auditoría. **Cero errores JavaScript de página** en ambos scripts.

La regresión conserva OAuth/state/PKCE, identidad Microsoft, aplicación/roles/módulos Nexo, logout/CSRF, UR, guardados concurrentes/versionado, rollback/reintento y Graph cifrado/refresco. Son 31 recorridos en tres scripts (9 + 12 + 10), contenidos en un solo grupo; no se cuentan otra vez como grupos HTTP.

Tema, CSS, layout y lógica de directorio conservados (layout con sustitución de import de transporte). Ajustes puntuales en páginas y texto “áreas dependientes” documentados; no hay rediseño. Capturas inspeccionadas como evidencia funcional, **no comparación visual exacta con Herd**. Todas las solicitudes externas del navegador se bloquean, incluidas fuentes web; no acredita descarga de tipografías, MFA ni SSO real.

Ejecuciones fallidas previas se conservaron en .artifacts y no se cuentan como éxitos. Se corrigió una ambigüedad de variable en el publicador SQL sintético y un error productivo de lectura de confirmaciones delegadas de una columna; el retiro/reintento fue repetido exitosamente. En este bloque fallaron inicialmente cuatro casos por ausencia de la zona IANA en Windows (resuelto con NodaTime) y una espera prematura de carga en la prueba de dos pestañas (corregida). Una ejecución diagnóstica simultánea no pudo compilar porque Windows mantenía la DLL abierta; se repitió secuencialmente. Los fallos no cuentan como evidencia de éxito. No se ampliaron permisos para hacer pasar las pruebas.

## Comandos reproducibles

Desde ClientApp:

```powershell
npm.cmd ci --ignore-scripts
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
```

Desde la raíz del destino:

```powershell
dotnet build tdv2.slnx -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

La última orden requiere PostgreSQL nativo, Edge y certificado ASP.NET HTTPS ya existente. Usa C:\Program Files\PostgreSQL\18\bin; admite -PostgresBin. En esta sesión fue necesaria aprobación para ejecutarlo fuera del sandbox. Crea siempre un clúster nuevo, con contraseña sintética aleatoria no impresa, vacía el archivo de contraseña inicial y detiene su clúster en finally. Conserva datos/logs sintéticos. **-SkipBrowser ejecuta 90 casos HTTP/SQL; -BrowserOnly ejecuta 31 recorridos; -SyncOnly ejecuta los 30 casos del bloque sin navegador**. Ninguna ejecución parcial equivale a la suite completa. El host HTTPS lee el certificado existente; no instala certificados.

No ejecutar dos suites mientras una recompila la misma salida en Windows: el ejecutable mantiene DLL abiertas. Operación manual/continua, cancelación, reserva de 30 minutos, recuperación y comandos Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). No se instalaron tareas ni servicios.

Los proyectos de verificación son ejecutables con salida no cero al fallar, no suites descubiertas por dotnet test. Construir ClientApp antes del navegador. Los controles de fallos/datos sólo existen en el ejecutable de pruebas; no hay autenticación de demostración ni endpoints de fixture en producción.

Revalidar el manifiesto de Herd, sólo lectura:

```powershell
$reference = 'C:\Users\Jesus Arenas\Herd\tdv2'
$manifest = Get-Content -Raw -Encoding UTF8 docs/migracion/fuente-manifiesto.json | ConvertFrom-Json
$changed = @($manifest | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $reference $_.path)).Hash.ToLowerInvariant() -ne $_.sha256
})
if ($changed.Count) { throw 'La referencia cambió; revisar el inventario.' }
$manifest.Count
```

## Pendiente de validación real y configuración faltante

- **Microsoft institucional:** TenantId, ClientId, ClientSecret y PublicOrigin local ya trasladados a User Secrets desde DPAPI. Pendientes callback registrado, cuentas autorizadas, vigencia del secreto, consentimiento/User.Read/offline_access, MFA/acceso condicional, Graph/rotación reales y logout SSO. El 302 local no valida esos aspectos. No publicar secretos en archivos o conversación.
- **Nexo Laravel institucional:** conexión importada, sin probar. Pendientes red/TLS, publicación tdv2, vistas filtradas y EXECUTE de las funciones públicas de delegación/representación (contrato Nexo 9.6.0 citado por Laravel). Sin validar firmas/tipos efectivos, publicador Laravel, idempotencia real, duración/formato de selección, responsables/roles asignables, revocaciones desde Nexo, concesiones compartidas ni auditoría central. Pruebas que alteren concesiones centrales requieren una publicación/personas autorizadas para pruebas.
- **SII/ILDA institucionales:** conexiones importadas, sin probar ni ejecutar. Pendientes acceso autorizado SELECT, certificados TLS cuando correspondan y muestra de tipos/volumen. Sin validar protocolo TDS/MySQL, autenticación real, configuración de servidor, ejercicio desplegado, tipos/precisión/texto de fechas ILDA, claves/empleados reales ni rendimiento institucional. No basta el lector ADO.NET sintético para acreditar el conector de red.
- **Procesador en despliegue:** comandos Windows/Ubuntu documentados; no se ejecutó Ubuntu, SIGTERM, supervisor, reinicio de máquina ni operación prolongada. No se habilitó ningún servicio/tarea. El fallo de fuente se probó por excepciones/lecturas incompletas, no por cortar una conexión remota real.
- **Datos existentes:** intactos y sin consultar. Falta contrastar esquema/tipos/índices en copia aislada, historial de colaboraciones, migración de actividad, UTC y ensayos de conversión/corte/reversión. No reutilizar cookies/cifrado Laravel ni aplicar el bootstrap sobre una base existente.
- **Operación y recuperación:** reinicio con llaves persistidas bajo cuenta del servicio, varias instancias, rotación/respaldo, limpieza de sesiones/contextos caducados, proxy/HTTPS, límites distribuidos y caída física de motor/red. Los triggers/fallos SQL no representan pérdida de confirmación de COMMIT. No hay reconciliador automático entre TDV2/Nexo.
- **Paridad documentada:** el identificador Microsoft de sesión no se regenera como en Laravel; se usa contexto cifrado con revisión atómica y rechazo de contexto obsoleto. Auditoría extendida a operaciones propias; motivo libre omitido. Login conserva bitácora básica separada de la persistencia de identidad; falta acordar metadatos históricos, retención y consulta institucional de auditoría.
- **Interfaz:** comparación visual con fuentes descargadas, móvil/teclado, impresión/exportación y borradores al caducar sesión. Capturas funcionales y código conservado no certifican estos aspectos.

## Bloques restantes

| Bloque | Estado |
|---|---|
| B0 — inventario y trazabilidad | Listo a nivel de código; esquema desplegado no consultado |
| B1 — dominio/módulos/esquema/avance | Implementado; falta paridad exhaustiva institucional |
| B2 — transporte/CSRF/formatos | Flujo principal y regresión verificados; visual y despliegue pendientes |
| B3 — Microsoft/identidad/Nexo/Graph | Implementado con proveedores simulados; **validación institucional pendiente** |
| B4 — colaboradores/delegación | Búsqueda/alta/retiro/recuperación implementados y probados con funciones sintéticas; **Nexo real pendiente** |
| B5 — Configuración/pruebas/representación/auditoría | Vista y representación separadas, permisos y auditoría local implementados y probados; **aceptación institucional pendiente** |
| B6 — ILDA/UR SII | Implementado y probado con copia PostgreSQL real/fuentes sintéticas; **conectividad, tipos y aceptación institucional pendientes** |
| B7 — programador/recuperación | Cola, reserva, caducidad, exclusión, parcial/rollback y recuperación implementados/verificados; **operación institucional/Ubuntu pendiente**, sin tareas instaladas |
| B8 — integración/corte | PostgreSQL aislado/navegador disponibles; servicios reales, visual, despliegue y corte pendientes |

Las rutas de sincronización ya tienen implementación, permisos y pruebas. Queda optimizar el listado PostgreSQL para evitar una consulta por formato. La migración exige evidencia de todos los bloques y aceptación institucional; compilar o pasar simuladores no cumple esos criterios.
