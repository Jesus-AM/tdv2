# Historial de preparación local, 2026-10-01

Evidencia histórica. Para operar el proyecto usar [la guía vigente](../ARRANQUE_LOCAL_WINDOWS.md). Los comandos Import/Configure/MigrateSecrets de este historial están retirados.

### User Secrets y Visual Studio — 2026-10-01

**Implementado:** se inicializó `UserSecretsId=0397954e-63d2-48b6-a581-a586951257cc` en tdv2.csproj y se trasladaron nueve claves desde la configuración DPAPI existente: `Microsoft:TenantId/ClientId/ClientSecret/PublicOrigin`, `ConnectionStrings:Tdv2/Nexo/Sii/Ilda`, `Synchronization:IldaEnabled=false`. El destino es `%APPDATA%\Microsoft\UserSecrets\0397954e-63d2-48b6-a581-a586951257cc\secrets.json`, fuera del repositorio y con permisos privados. **User Secrets es JSON editable sin cifrado DPAPI**, por petición del usuario. No se imprimieron valores ni se pusieron secretos en React, launchSettings o archivos versionados. No se volvió a leer Herd.

`WebApplication.CreateBuilder` carga el archivo mediante la configuración estándar de Development; no se añadió una fuente especial ni se modificó autenticación/autorización. El perfil https de tipo Project queda primero y abre el navegador en https://localhost:7136; también se fijó ActiveDebugProfile=https en el archivo personal existente tdv2.csproj.user (ignorado por el repositorio). Se preservó el retorno /connect y la misma conexión a tdv2_local_validation @ 127.0.0.1:51476, con tdv2_local_app. `StartDatabase` prepara exclusivamente el PostgreSQL aislado para F5 y deja automática apagada; no aplica migraciones ni inicia procesador. ILDA permanece deshabilitada.

El lanzador anterior ya no inyecta Microsoft/ConnectionStrings/Synchronization desde DPAPI. `Import` y `Configure` quedan retirados y sólo indican dónde editar. `MigrateSecrets` sólo escribe si el archivo todavía no existe: repetirlo respeta todo el archivo y las claves eliminadas. `Start` usa el mismo User Secrets; detecta variables de entorno explícitas con precedencia y no las modifica silenciosamente. `database.clixml` continúa para administrar el clúster; `institutional.clixml` queda como copia histórica cifrada sin lectura en los arranques. Reiniciar depuración después de editar, pues las opciones Microsoft usan IOptions.

**Probado:** compilación de backend/verificador sin errores ni advertencias. Nueve valores trasladados y comparados con sus originales; proveedor efectivo `secrets.json` comprobado para las nueve claves en el entorno del perfil https. Las cuatro cadenas se analizaron con proveedores .NET sin abrir conexiones institucionales. Prueba de edición temporal de ClientId a GUID sintético: conservada tras MigrateSecrets/Import/Configure, observada por el proveedor y archivo original restaurado byte por byte. No se ejecutó login con ese GUID ni se creó usuario simulado.

Arranque directo `dotnet run --project tdv2/tdv2.csproj --no-build --no-restore --launch-profile https`, sin el lanzador DPAPI: **4/4 comprobaciones Edge** de HTTPS, React/JSON, 401/CSRF y /connect 302 con retorno correcto, sin seguir la redirección a Microsoft. PostgreSQL confirmó cuatro migraciones, cero usuarios/formatos/UR/ejecuciones y automática=false. La restauración inicial de dependencias del verificador fue bloqueada por la red del sandbox; se repitió autorizadamente y la compilación final pasó.

Evidencia: [proveedores y conservación de ediciones](evidencia-user-secrets.json), [navegador con perfil https](evidencia-user-secrets-navegador.json). El lanzador `Start` actualizado también pasó las mismas cuatro comprobaciones sin inyección DPAPI. Las pruebas usan la aplicación real anónima; no sustituyen Microsoft/Nexo con simuladores. **Se verificó el perfil usado por F5 mediante CLI; no se operó la interfaz ni el depurador Visual Studio.** No se probaron conexiones institucionales ni permisos reales, y no se declara autenticación validada.

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

**Pendiente:** registro del retorno en Entra si falta, vigencia del secreto, consentimiento, Microsoft/Graph, red/TLS y publicación/permisos Nexo, conectores SII/ILDA reales y catálogo local UR. La programación sigue apagada y no hubo sincronizaciones. Operación personal detallada: [Visual Studio, secretos y arranque](../ARRANQUE_LOCAL_WINDOWS.md).

### Configuración personal importada de Herd — 2026-10-01

Registro histórico anterior a User Secrets. Las acciones Import/Configure y la inyección DPAPI aquí descritas quedaron sustituidas por el apartado vigente anterior.

**Implementado:** con autorización expresa se leyó `C:\Users\Jesus Arenas\Herd\tdv2\.env` sin modificarlo. `scripts/Local-Tdv2.ps1 Import` guarda Microsoft, Nexo, SII e ILDA mediante DPAPI y reemplazo atómico dentro del directorio privado existente. Valida claves requeridas, GUID, puertos, TLS y contratos antes de publicar; rechaza duplicados/interpolaciones no resueltas sin mostrar valores. `Start` carga las conexiones cifradas exclusivamente en el backend; `Configure` conserva SII/ILDA al corregir Microsoft/Nexo. No cambia React, diseño ni fuentes.

Mapeo confirmado contra Laravel: `MSGRAPH_TENANT_ID/CLIENT_ID/SECRET_ID` → Microsoft (SECRET_ID contiene el valor del secreto); `NEXO_DB_*` → Npgsql; **`MSSQL_*`** → SQL Server SII; `ILDA_DB_*` → MySQL ILDA. Se conservan los parámetros TLS explícitos. `NEXO_DB_SSLROOTCERT` e `ILDA_DB_SSL_CA` estaban vacíos; no se aportaron CA personalizadas, cuya necesidad real no se verificó. ILDA sin CA usa Preferred; no acredita cifrado negociado. La clave sigue siendo tdv2; no se importa un ID de aplicación ni se amplían permisos. Se validan los destinos fijos poa.UNIDADES_RESPONSABLES_POA e ilda_db.

Se excluyeron `DB_*`, `APP_KEY`, `NEXO_APP_ID`, el retorno Laravel y opciones de activación. Se conservan **tdv2_local_validation @ 127.0.0.1:51476**, sus credenciales originales y **https://localhost:7136/connect**. Automática=false, ILDA deshabilitada, sin procesador ni sincronizaciones ejecutadas. No se conectó a bases institucionales ni se aplicó DDL en esta importación.

**Probado:** **8/8 verificaciones sintéticas** de parseo, preservación de contraseñas, exclusiones, TLS, DPAPI y rechazos. La prueba necesita el perfil Windows real: el sandbox no puede proteger/descifrar con DPAPI de esa cuenta. Importación del archivo autorizado con descifrado idéntico y hashes de `.env`, marcador y credenciales locales sin cambios. **4/4 comprobaciones en Edge real** después de importar: HTTPS confiable, React → ASP.NET, CSRF/401 anónimo y /connect **302** con redirect_uri local correcto. PostgreSQL confirmó 4 migraciones, 0 usuarios/formatos/UR/ejecuciones y automática=false. Esto prueba carga de configuración y persistencia local del intento OAuth, no inicio de sesión.

La primera prueba posterior a importar siguió la redirección hasta la página pública de Microsoft y falló por recursos bloqueados. Se corrigió el test: `fetch` con `redirect: manual`, leyendo las cabeceras locales mediante Chromium. Una repetición intermedia agotó la espera del evento de respuesta opaca; la versión final pasó sin seguir el redirect. No hubo credenciales introducidas, canje de tokens ni usuario autenticado; Nexo/SII/ILDA no se consultaron. Se mantienen los fallos como antecedente, no como evidencia de éxito.

Evidencia sin secretos: [importación a las 07:56 UTC](evidencia-importacion-local.json), [reglas sintéticas](evidencia-importador-sintetico.json) y [arranque configurado a las 07:59 UTC](evidencia-arranque-configurado.json). La evidencia anterior con /connect 503 se conserva como histórica. Al terminar se detuvieron ASP.NET y PostgreSQL aislado, con los puertos 7136/5064/51476 libres; configuración y datos permanecen.

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

Instrucciones de uso y detalle de equivalencias: [arranque local](../ARRANQUE_LOCAL_WINDOWS.md). La configuración cifrada no se copia a la documentación ni al frontend.

### Arranque local personal en Windows — 2026-10-01

Esta sección describe la preparación **anterior** a la importación autorizada de arriba; sus ausencias de configuración y el 503 ya no describen el estado actual.

**Implementado:** `scripts/Local-Tdv2.ps1` ofrece Prepare/Configure/Start/Stop/Status. Usa el perfil https real: **https://localhost:7136** y http://localhost:5064; React compilado lo sirve ASP.NET, sin terminal Vite. La base persistente **tdv2_local_validation @ 127.0.0.1:51476** está dentro de `.artifacts/local-validation/data`, separada de servicios/bases existentes. Se aplicaron únicamente allí 001–004 con registro de hashes y cuenta de aplicación sin DDL. Credenciales locales aleatorias protegidas por DPAPI y directorio privado; las credenciales Microsoft/Nexo personales aún no se introdujeron. Inicio pausa automática, deshabilita ILDA, excluye conexiones SII/ILDA heredadas y no arranca trabajadores. No se instalaron servicios ni tareas; no se cargaron fixtures, usuarios ni permisos sintéticos.

**Probado:** Prepare compiló ASP.NET sin errores/advertencias y React con Vite. Segunda preparación conservó el esquema y no reaplicó migraciones. Arranque HTTPS con certificado confiable del usuario ya existente, sin instalar ni exportar otro. **4/4 comprobaciones en Edge real**, sin ignorar errores TLS: portada, props React → ASP.NET, reintento anónimo 401/CSRF y /connect 503 por falta de configuración. Cero errores JavaScript, ninguna sesión ficticia. PostgreSQL confirmó 4 migraciones, 0 usuarios/formatos/UR/ejecuciones y automática=false. Un segundo Start se rechazó sin afectar al primero; se comprobaron Ctrl+C y Stop. Al finalizar quedaron detenidos ASP.NET y este PostgreSQL, conservando el entorno para uso personal.

**Bloqueos pendientes:** TenantId y ClientId institucionales, valor de ClientSecret, registro del retorno Web **https://localhost:7136/connect**, consentimiento de scopes y conexión PostgreSQL Nexo con sus vistas/permisos/publicación tdv2. El origen HTTPS y la conexión TDV2 aislada ya los proporciona el script. No se autenticó Microsoft ni se conectó Nexo real; tampoco se validaron fuentes institucionales. La base no contiene catálogo UR: incluso tras configurar identidad, probar formatos/alcance exige cargarlo expresamente en esta base, con claves/empleados correspondientes a Nexo. El modo de arranque no procesa sincronizaciones.

Se corrigieron una espera del lanzador pg_ctl por captura de streams en Windows y el tratamiento de stderr para un servidor detenido. La prueba nueva admite el BOM de launchSettings y realiza llamadas HTTP en Edge para usar su confianza TLS real. No cambió código de autenticación, autorización, pantallas, tema ni tipografías. Las pruebas de esta sección son de **arranque real anónimo**, distintas de las suites autenticadas con dobles anteriores.

Instrucciones personales, permisos Nexo, comandos y parada: [ARRANQUE_LOCAL_WINDOWS.md](../ARRANQUE_LOCAL_WINDOWS.md). Informe: [evidencia-arranque-local.json](evidencia-arranque-local.json); capturas `.artifacts/local-validation/browser/`. Repetir `npm.cmd run test:local` desde ClientApp, con Start abierto. Las peticiones externas del navegador se bloquearon, incluidas fuentes; no acredita aceptación visual ni inicio institucional.

### Inspección de la migración

- Destino original inspeccionado: solución tdv2.slnx, proyecto Web API net10.0, plantilla WeatherForecast y OpenAPI 10.0.12. SDK 10.0.401/runtime 10.0.12. No existían Git ni AGENTS.md; no se inicializó Git.
- Herd es exclusivamente referencia de lectura. Durante la migración inicial no se leyeron .env ni credenciales; la excepción posterior autorizada para configuración está documentada arriba. No se ejecutó Laravel ni se leyeron respaldos/logs; no se conectó ninguna base existente. Todos los cambios están en el destino ASP.NET.
