# Desarrollo de TDV2 en Visual Studio

Abrir `tdv2.slnx`. `tdv2` es ASP.NET Core 10; `ClientApp/ClientApp.esproj` muestra la aplicación React existente mediante el sistema de proyectos JavaScript de Visual Studio. Los verificadores están agrupados en Pruebas y no se inician con F5.

## F5 con actualización de React

1. Restaurar paquetes .NET al abrir/compilar. Ejecutar `npm.cmd ci --ignore-scripts` desde ClientApp si faltan dependencias o cambió el lockfile.
2. Desde la raíz, ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase`. Inicia sólo el clúster aislado registrado, sin recrear la base ni aplicar migraciones. La terminal puede cerrarse.
3. Seleccionar el perfil compartido **TDV2 HTTPS + React** de la solución y **https** del backend; pulsar F5. Abrir `https://localhost:7136`.
4. Editar `ClientApp/resources/js` o `resources/css`. Vite actualiza la interfaz por el WebSocket HTTPS del mismo origen.
5. Mayús+F5 detiene la depuración. `Local-Tdv2.ps1 Stop` detiene PostgreSQL cuando ya no se necesite; los datos permanecen.

Si el perfil compartido no aparece, habilitar perfiles de inicio de varios proyectos en las opciones de Visual Studio o configurar manualmente **ClientApp → Iniciar** y **tdv2 → Iniciar**; los proyectos de pruebas en **Ninguno**. Es el [mecanismo estándar de perfiles .slnLaunch](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects). La configuración de `StartupCommand` procede del [sistema JavaScript .esproj](https://learn.microsoft.com/en-us/visualstudio/javascript/javascript-project-system-msbuild-reference).

El perfil `https` usa `ASPNETCORE_ENVIRONMENT=Development` y `ReactDevelopment__UseVite=true`. El perfil **https-compiled** sirve el último `npm run build` sin Vite. No iniciar ambos a la vez. `Local-Tdv2.ps1 Start` usa el perfil compilado; `Prepare` sigue siendo una operación explícita que compila y verifica/aplica SQL pendiente únicamente en la base de validación.

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
| DB_* de Laravel | **No trasladado**: ConnectionStrings:Tdv2 conserva tdv2_local_validation |
| Activación remota o automática anterior | **No trasladada**; Synchronization:IldaEnabled=false y programación local pausada |
| APP_KEY / NEXO_APP_ID | No se importan. Nexo se resuelve por la clave fija tdv2; sus roles no se configuran localmente |

No cambiar TLS para conseguir acceso. Las cadenas ya fueron analizadas por los proveedores sin abrirlas; eso no acredita certificados, VPN, permisos ni conectividad. Las fuentes fijas siguen siendo `poa.UNIDADES_RESPONSABLES_POA` y `ilda_db.informacion_area`.

## Comprobaciones y límites

```powershell
# Raíz, sin depuración activa
dotnet build tdv2.slnx
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-UserSecrets.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Compiled
```

Test-UserSecrets edita temporalmente un ClientId sintético, comprueba la precedencia y que los comandos retirados no lo reponen, y restaura el archivo byte por byte si no hubo otra edición concurrente. No inicia sesión. Test-Development usa Edge con confianza TLS real, prueba `/connect` **sin seguir** la redirección y verifica React/CSRF/HMR. Conserva la pantalla modificada ante una edición concurrente en vez de sobrescribirla; en operación normal restaura sus bytes exactos.

Si falta el certificado de desarrollo en otra cuenta/equipo, ejecutar personalmente `dotnet dev-certs https --trust`. En esta sesión se utilizó el certificado ya existente. No se instaló otro certificado ni un servicio PostgreSQL.

**Verificado por CLI y Edge; pendiente operar F5 dentro de la interfaz de Visual Studio.** Un 302 a Microsoft sólo confirma el armado del inicio OAuth y su callback. Faltan registro Entra, consentimiento, MFA, token/Graph y acceso institucional real. Para Nexo y sus funciones publicadas consultar [autenticación](migracion/AUTENTICACION_POSTGRESQL.md) y [delegación/representación](migracion/DELEGACION_REPRESENTACION.md). No se introducen usuarios o roles de demostración en la aplicación.