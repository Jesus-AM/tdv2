# Desarrollo de TDV2 en Visual Studio

Abrir `tdv2.slnx`. `tdv2` es ASP.NET Core 10; `ClientApp/ClientApp.esproj` muestra la aplicación React existente mediante el sistema de proyectos JavaScript de Visual Studio. Los verificadores están agrupados en Pruebas y no se inician con F5.

**Conexión vigente:** el usuario recreó `SERVIDOR_TDV2:5432 / tdv2_db` y actualizó User Secrets. Se aplicaron las dos migraciones EF y se repitió el comando sin cambios el 2026-10-02 UTC. Historial `__EFMigrationsHistory`; [comandos dotnet ef](../README.md#migraciones-ef-core) y evidencia final en ESTADO_MIGRACION. Las instrucciones de clúster/DPAPI/StartDatabase que siguen se refieren exclusivamente al entorno local aislado; no inicializan el servidor ni son necesarias para arrancar el sitio conectado al servidor.

## F5 con actualización de React

1. Abrir `tdv2.slnx` y seleccionar **TDV2 HTTPS + React**. Si la solución estaba abierta durante esta corrección, volver a abrirla una vez para recargar los destinos compartidos.
2. Pulsar F5 o el botón verde. Visual Studio inicia Vite y ASP.NET y abre Edge en `https://localhost:7136`. No ejecutar npm ni dotnet manualmente en cada arranque.
3. Editar `ClientApp/resources/js` o `resources/css`. Vite actualiza la interfaz por el WebSocket HTTPS del mismo origen.
4. Mayús+F5 detiene la depuración. El sistema JavaScript puede conservar Vite para reutilizarlo en el siguiente F5.

La restauración .NET y `npm.cmd ci --ignore-scripts` sólo son preparación cuando faltan dependencias o cambia el lockfile. Las operaciones con datos requieren la base configurada ya disponible; la administración del clúster aislado continúa explícita mediante `Local-Tdv2.ps1 StartDatabase` / `Stop`. F5 no administra bases ni aplica migraciones. Desde el ajuste del 2026-10-09, el proceso web atiende la cola manual persistente con el mismo coordinador; abrir la aplicación no crea sincronizaciones ni activa horarios.

Para sincronizar manualmente basta con mantener ASP.NET ejecutándose y pulsar **Sincronizar SII**, **Sincronizar ILDA** o **Sincronizar SII e ILDA ahora** en Configuración → Sincronizaciones. No se necesita otra terminal ni `--sync-once`. Se usan las claves de conexión existentes, sin nuevas variables ni credenciales. ILDA conserva su habilitación independiente y los horarios pueden permanecer pausados. Al detener F5, la cola queda en PostgreSQL: los trabajos aún pendientes se retoman al volver; una ejecución interrumpida se recupera tras vencer su reserva, sin repetir silenciosamente publicaciones. [Estados, recuperación y procesadores externos](migracion/SINCRONIZACIONES.md).

El perfil fija **tdv2 → Iniciar → https** y **ClientApp → Iniciar → TDV2 React (Edge)**, en ese orden; pruebas en **Ninguno**. No depende del último depurador seleccionado. Es el [mecanismo estándar de perfiles .slnLaunch](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects). `ClientApp/.vscode/launch.json` configura el [depurador JavaScript de Visual Studio](https://learn.microsoft.com/en-us/visualstudio/javascript/tutorial-asp-net-core-with-react?view=visualstudio); el nombre `.vscode` también es requerido por Visual Studio.

`ClientApp.esproj` ejecuta `npm run dev` en ClientApp. Ese script conserva `vite --host 127.0.0.1 --port 5173 --strictPort`. JSPS verifica el puerto de `launch.json` durante **Implementar**, antes de iniciar Kestrel: por eso el destino es `http://127.0.0.1:5173/__vite/__launch`. Esta ruta sólo existe en desarrollo, espera hasta 30 segundos a `/health/live` local y redirige al HTTPS fijo. El depurador sigue el origen 7136 y mapea `/__vite/` a los archivos locales. Sólo ClientApp abre navegador; `https` tiene `launchBrowser=false` y `https-compiled` conserva `true`.

El perfil `https` usa `ASPNETCORE_ENVIRONMENT=Development` y `ReactDevelopment__UseVite=true`. El perfil **https-compiled** sirve el último `npm run build` sin Vite. No iniciar ambos a la vez. `Local-Tdv2.ps1 Start` usa el perfil compilado; `Prepare` aplica EF explícitamente sólo en su clúster verificado. Una base local anterior sin historial EF se conserva y requiere revisión; no se adopta ni se elimina automáticamente.

`appsettings.Development.json` conserva `UseVite=false`: la variable del perfil `https` tiene prioridad. Se comprobó que User Secrets y las variables ambientales del proceso de comprobación/usuario/equipo no aportaban otra sobrescritura de esa clave. No se modificaron credenciales.

| Elemento | Destino |
|---|---|
| Navegador / callback Microsoft | https://localhost:7136 / https://localhost:7136/connect |
| HTTP auxiliar de Kestrel | http://localhost:5064; las cookies seguras y OAuth se usan por HTTPS |
| Vite | http://127.0.0.1:5173, sólo desarrollo, detrás de `/__vite/` |
| PostgreSQL aislado | tdv2_local_validation @ 127.0.0.1:51476 |
| Datos / marcador | `.artifacts/local-validation/data` y `environment.json` |
| Cuenta de aplicación | tdv2_local_app, sin administración ni DDL |
| Binarios existentes | C:\Program Files\PostgreSQL\18\bin |

La portada sigue siendo Razor y las pantallas React conservan su transporte JSON. ASP.NET valida acceso antes del HTML; el proxy sólo sirve recursos de Vite. CSRF usa `X-CSRF-TOKEN`, cookie Secure/Strict y contexto `X-TDV2-Context`; sesión Secure/HttpOnly/Lax. El HMR no cambia el retorno Microsoft. El arranque web no aplica DDL, ejecuta fuentes remotas ni procesa la cola. La programación queda apagada mediante StartDatabase.

## Secretos editables

Clic derecho sobre **tdv2 → Administrar secretos de usuario**. Guardar y reiniciar la depuración, porque Microsoft usa `IOptions`. El identificador permanece:

```text
0397954e-63d2-48b6-a581-a586951257cc
```

El archivo está en `%APPDATA%\Microsoft\UserSecrets\0397954e-63d2-48b6-a581-a586951257cc\secrets.json`. Ya contiene la configuración local trasladada. **Es JSON sin cifrado, exclusivamente para desarrollo.** No copiarlo al repositorio, a React o a documentación. No usar `dotnet user-secrets list` en salidas compartidas.

ASP.NET carga ese proveedor mediante `WebApplication.CreateBuilder` en Development. Variables de entorno y argumentos tienen mayor prioridad. El lanzador detecta las nueve claves si están duplicadas en variables de entorno y se detiene sin mostrar sus valores. No mantiene valores paralelos ni sobrescribe ediciones.

`Import`, `Configure` y `MigrateSecrets` sólo muestran dónde editar; ya no importan, migran ni leen DPAPI/Herd. Se retiraron el importador y sus pruebas obsoletas después de verificar el reemplazo. `database.clixml` permanece para administrar el clúster con la misma cuenta Windows. `institutional.clixml` permanece como archivo histórico cifrado, sin lectores en el lanzador actual. No eliminar archivos de credenciales o recrear la base para solucionar un problema de permisos DPAPI.

## Equivalencias de la configuración importada

| Laravel anterior | Clave vigente / conversión |
|---|---|
| MSGRAPH_TENANT_ID | Microsoft:TenantId |
| MSGRAPH_CLIENT_ID | Microsoft:ClientId |
| MSGRAPH_SECRET_ID | Microsoft:ClientSecret; es el **valor** del secreto, no su identificador administrativo |
| Retorno Laravel | No se trasladó: Microsoft:PublicOrigin mantiene https://localhost:7136 y deriva /connect |
| NEXO_DB_HOST/PORT/DATABASE/USERNAME/PASSWORD | ConnectionStrings:Nexo; Host/Port/Database/Username/Password de Npgsql |
| NEXO_DB_SSLMODE / NEXO_DB_SSLROOTCERT | SSL Mode / Root Certificate en la cadena Nexo |
| MSSQL_HOST/PORT/DATABASE/USERNAME/PASSWORD | ConnectionStrings:Sii; Data Source=host,puerto; Initial Catalog; User ID; Password |
| MSSQL_ENCRYPT / MSSQL_TRUST_SERVER_CERTIFICATE | Encrypt / TrustServerCertificate en SQL Server |
| ILDA_DB_HOST/PORT/DATABASE/USERNAME/PASSWORD | ConnectionStrings:Ilda; Server/Port/Database/User ID/Password |
| ILDA_DB_SSL_CA | SslCa y VerifyFull si había CA; se conservó Preferred cuando no había CA |
| DB_* de Laravel | No se importan; ConnectionStrings:Tdv2 es ahora el servidor recreado, configurado directamente por el usuario |
| Activación remota o automática anterior | **No trasladada**; Synchronization:IldaEnabled=false y programación local pausada |
| APP_KEY / NEXO_APP_ID | No se importan. Nexo se resuelve por la clave fija tdv2; sus roles no se configuran localmente |

No cambiar TLS para conseguir acceso. Las cadenas ya fueron analizadas por los proveedores sin abrirlas; eso no acredita certificados, VPN, permisos ni conectividad. Las fuentes fijas siguen siendo `poa.UNIDADES_RESPONSABLES_POA` y `ilda_db.informacion_area`.

## Comprobaciones y límites

```powershell
# Raíz, sin depuración activa
dotnet build tdv2.slnx
dotnet run --project tests/TDV2.Verification --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

Test-UserSecrets y Test-Development son verificadores históricos del clúster local anterior. No ejecutarlos para validar el servidor vigente. Para F5 ya iniciado, `node ClientApp/tests/browser/visual-studio-startup.mjs` desde la raíz comprueba portada, React y HMR sin OAuth ni consultas institucionales.

Si falta el certificado de desarrollo en otra cuenta/equipo, ejecutar personalmente `dotnet dev-certs https --trust`. En esta sesión se utilizó el certificado ya existente. No se instaló otro certificado ni un servicio PostgreSQL.

**F5 verificado el 2026-10-01 en la instancia abierta de Visual Studio Community 2026 18.10.3 mediante `EnvDTE.ExecuteCommand("Debug.Start")`, incluido arranque en frío.** Se leyó su panel de salida: implementación correcta y depuradores .NET/JavaScript activos. El [error original completo](migracion/evidencia-vs-implementacion-antes.txt) era `Value cannot be null. Parameter name: source` al leer una configuración `launch.json` inexistente. [Resultado corregido](migracion/evidencia-vs-implementacion-despues.txt), [precedencia](migracion/evidencia-vs-configuracion.json) y [React/HTTPS](migracion/evidencia-vs-react.json).

Con ese perfil ya iniciado, `node tests/browser/visual-studio-startup.mjs` desde ClientApp repite la comprobación anónima de Vite, redirección, React, props, 401 y WebSocket HMR. No inicia servicios ni OAuth. Los artefactos quedan en `.artifacts/visual-studio`.

La verificación histórica de un 302 a Microsoft sólo confirma el armado del inicio OAuth y su callback. Faltan registro Entra, consentimiento, MFA, token/Graph y acceso institucional real. Esta corrección no repitió login ni accedió a bases. Para Nexo y sus funciones publicadas consultar [autenticación](migracion/AUTENTICACION_POSTGRESQL.md) y [delegación/representación](migracion/DELEGACION_REPRESENTACION.md).
