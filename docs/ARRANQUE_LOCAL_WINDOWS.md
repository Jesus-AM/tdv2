# TDV2 local en Visual Studio y Windows

Configuración vigente: **User Secrets del proyecto tdv2**. Microsoft, Nexo, SII, ILDA y la conexión a **tdv2_local_validation** ya se trasladaron desde DPAPI. No importar Herd de nuevo. No hay usuario ni autenticación simulada en este arranque.

Al terminar esta preparación se dejó **PostgreSQL aislado activo para tu próximo F5**, con automática apagada; ASP.NET quedó detenido y sus puertos libres. Tras Stop o reiniciar Windows, volver a ejecutar StartDatabase como se indica abajo.

## Editar desde Visual Studio

1. Abrir `C:\Users\Jesus Arenas\source\repos\tdv2\tdv2.slnx`.
2. Clic derecho en el **proyecto tdv2** → **Administrar secretos de usuario**.
3. Editar `secrets.json`, guardar y reiniciar la depuración para aplicar todos los cambios.

Archivo asociado a UserSecretsId del proyecto:

```text
C:\Users\Jesus Arenas\AppData\Roaming\Microsoft\UserSecrets\0397954e-63d2-48b6-a581-a586951257cc\secrets.json
```

Está fuera del repositorio, con permisos privados para la cuenta Windows. **User Secrets guarda JSON sin cifrado DPAPI**: es el mecanismo de desarrollo editable solicitado. No pegar su contenido en React, código, appsettings, launchSettings, documentación ni conversación.

Claves exactas (el archivo ya contiene sus valores):

| Clave User Secrets | Procedencia / uso |
|---|---|
| `Microsoft:TenantId` | Tenant importado |
| `Microsoft:ClientId` | Registro de aplicación importado |
| `Microsoft:ClientSecret` | Valor del secreto, originalmente MSGRAPH_SECRET_ID |
| `Microsoft:PublicOrigin` | https://localhost:7136; retorno derivado /connect |
| `ConnectionStrings:Tdv2` | tdv2_local_validation, cuenta tdv2_local_app |
| `ConnectionStrings:Nexo` | Publicación PostgreSQL Nexo, aplicación por clave tdv2 |
| `ConnectionStrings:Sii` | SQL Server, importado desde MSSQL_* |
| `ConnectionStrings:Ilda` | MySQL, importado desde ILDA_DB_* |
| `Synchronization:IldaEnabled` | false |

En JSON se usan **dos puntos**, no los dobles guiones bajos de las variables de entorno. La programación se almacena en PostgreSQL (`sincronizacion_configuracion`), no en una clave inventada: actualmente **activa=false, incluir_ilda=false, proxima_en=null**. No se inicia procesador con F5.

ASP.NET carga User Secrets automáticamente mediante `WebApplication.CreateBuilder` en **Development**, como establece el perfil `https`. El lanzador **ya no inyecta valores Microsoft/ConnectionStrings/Synchronization desde DPAPI**. `Import` y `Configure` anteriores sólo indican dónde editar: no leen Herd ni escriben configuración. `MigrateSecrets` es explícito y de una sola vez: si el archivo existe, no lo modifica ni repone claves eliminadas.

Las variables de entorno explícitas conservan la precedencia normal de ASP.NET. El lanzador detecta las nueve claves si están definidas y se detiene sin imprimir sus valores ni cambiarlas. No duplicar User Secrets en el perfil de depuración. Reiniciar Visual Studio tras quitar una variable persistente.

## Iniciar con F5

La base es PostgreSQL aislado, **no un servicio Windows**. Antes de la primera depuración tras detenerla o reiniciar Windows, ejecutar en PowerShell, también desde la terminal de Visual Studio:

```powershell
Set-Location 'C:\Users\Jesus Arenas\source\repos\tdv2'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
```

Inicia sólo el clúster registrado, comprueba directorio/base y deja apagada la automática. No migra esquemas ni ejecuta sincronizaciones. El comando termina y **puedes cerrar esa terminal**.

En Visual Studio:

1. Establecer **tdv2** como proyecto de inicio.
2. Seleccionar **https** junto al botón de inicio (Project/Kestrel; no http ni IIS Express).
3. Pulsar **F5**. Abrirá **https://localhost:7136**; si no se abre el navegador, visitar esa URL.

React está compilado y lo sirve ASP.NET desde tdv2/wwwroot; no necesita terminal Vite. También se escucha http://localhost:5064, pero Microsoft y cookies seguras se prueban por HTTPS. No iniciar F5 y el lanzador Start simultáneamente: usan los mismos puertos.

**Mayús+F5** detiene ASP.NET. Al terminar de trabajar, desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Stop
```

Detiene sólo PostgreSQL aislado y conserva datos. No se instalaron servicios ni tareas.

## Alternativa por terminal y recompilación

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Start
```

Usa **el mismo User Secrets**, sin respaldo automático DPAPI. Dejar esa terminal abierta; para detener, Ctrl+C y después Stop. Status muestra la ruta del archivo, disponibilidad y contadores locales, nunca valores.

F5 compila el backend. Si modificas React o necesitas reconstruir ambos, detener la aplicación y ejecutar desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 Prepare
```

Compila ASP.NET/React, verifica las cuatro migraciones sin reaplicarlas y deja el clúster detenido. No modifica User Secrets. Después usar StartDatabase y F5. No instala programas ni servicios.

## Base y requisitos institucionales

| Elemento | Estado |
|---|---|
| PostgreSQL nativo | C:\Program Files\PostgreSQL\18\bin |
| Base / dirección | tdv2_local_validation @ 127.0.0.1:51476 |
| Datos / marcador | .artifacts/local-validation/data y environment.json |
| Cuenta del backend | tdv2_local_app, sin superusuario ni DDL |
| Administración local | database.clixml sigue en DPAPI; no se copia la cuenta administradora a User Secrets |
| Esquema | Migraciones 001–004 ya aplicadas sólo aquí |
| Sincronización | Automática apagada, ILDA deshabilitada, sin procesador ni ejecuciones |

El script valida marcador/ruta antes de escribir. institutional.clixml queda como copia histórica cifrada: el backend y sus arranques no la leen. No se sustituyó la conexión local por Laravel ni se modificó Herd.

**Comprobar en Microsoft Entra y registrar, si falta, la URI Web https://localhost:7136/connect.** No se verificó su inscripción. La salida usa https://localhost:7136/. Pendientes: vigencia del secreto, consentimiento de openid/profile/offline_access/User.Read, MFA, Microsoft/Graph, correo @uacj.mx y acceso Nexo. Un 302 local no prueba autenticación institucional.

Nexo/SII/ILDA están **configurados, sin comprobar sus conexiones reales**. Se necesitan red/VPN, TLS y permisos. Se preservaron las opciones TLS importadas; las CA personalizadas Nexo/ILDA estaban vacías. ILDA sin CA usa Preferred; no se acredita TLS negociado ni se cambian opciones para conseguir acceso.

### Contrato y permisos que debe publicar Nexo

La conexión debe tener `CONNECT` a la base y `USAGE` sobre `public`, y `SELECT` en las vistas filtradas para esta aplicación:

- `nexo_aplicacion`, `nexo_usuarios`, `nexo_usuario_rol`, `nexo_modulos`, `nexo_modulo_rol`, `nexo_concesiones`.
- Para colaboradores: `nexo_delegacion`, `nexo_delegacion_roles` y vistas de usuarios correspondientes.

`nexo_aplicacion` debe devolver exactamente una fila con clave **tdv2**, ID positivo y `nivel_control=2`. No se configura un ID de aplicación a mano. Tu usuario debe ser individual, con rol vigente; Nexo debe publicar `num_empleado` textual, `ID_UR` y `origen_ur`. Los módulos, sus rutas y padres son los del contrato:

| Clave | Ruta | Padre |
|---|---|---|
| `procesos_operativos` | `/inicio` | ninguno |
| `configuracion` | `/configuracion` | ninguno; requiere administrador |
| `sincronizaciones` | `/configuracion/sincronizaciones` | configuracion |
| `pruebas_acceso` | `/configuracion/pruebas-acceso` | configuracion |

Delegación y representación requieren además `EXECUTE` en las funciones **publicadas** `public.nexo_a{ID}_buscar_personas(text,text,text,integer)`, `conceder_acceso(text,text,text,bigint,text)`, `retirar_acceso(text,text,bigint)` y `representacion(jsonb)`, con el mismo prefijo dinámico derivado de la aplicación. Sus autorizaciones internas siguen siendo de Nexo; administrador TDV2 no implica capacidad de representar ni delegar. No otorgar escritura directa sobre sus tablas internas. Para pruebas de alta/retiro/representación se necesita una publicación y personas de prueba autorizadas: esas funciones pueden cambiar el estado central. En esta preparación no se llamó ninguna función ni se conectó a Nexo.

Detalles del contrato y diferencias pendientes: [autenticación](migracion/AUTENTICACION_POSTGRESQL.md) y [delegación/representación](migracion/DELEGACION_REPRESENTACION.md).

## Qué puedes probar

Ahora: portada HTTPS, React/JSON/CSRF, rechazos anónimos, arranque/parada y edición de User Secrets. El login exige los requisitos institucionales anteriores. No hay sesión de demostración presentada como real.

La base sigue sin UR/formatos: incluso con login autorizado hace falta preparar expresamente el catálogo SII en la base aislada. Los formatos leen ILDA sólo desde su copia local. No se ejecutaron sincronizaciones; los botones manuales encolan y este arranque no procesa la cola. Operación del trabajador: [sincronizaciones](migracion/SINCRONIZACIONES.md).

El certificado HTTPS confiable de esta cuenta ya existía. Si falta en otra cuenta/equipo: ejecutar personalmente `dotnet dev-certs https --trust`. Usar la misma cuenta Windows que preparó el clúster; no borrar la base por problemas de DPAPI.

## Verificación reproducible

Desde la raíz, con la aplicación detenida:

```powershell
dotnet build tests/TDV2.LocalConfigurationVerification/TDV2.LocalConfigurationVerification.csproj -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-UserSecrets.ps1
```

Comprueba el proveedor efectivo de las nueve claves, analiza cadenas con los proveedores .NET **sin abrir conexiones** y valida base/retorno/ILDA. Prueba una edición temporal de ClientId a un GUID sintético: MigrateSecrets/Import/Configure deben conservarla. Después restaura exactamente el archivo original. Requiere formato plano inicial; detecta cambios concurrentes y no los sobrescribe. No usar `dotnet user-secrets list` en salidas compartidas.

Para comprobar el perfil de F5 sin el lanzador anterior:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
dotnet run --project tdv2/tdv2.csproj --no-build --no-restore --launch-profile https
```

Con el proceso activo, desde otra terminal en ClientApp: `npm.cmd run test:local`. Comprueba React/ASP.NET, HTTPS, 401/CSRF y 302/retorno mediante fetch **sin seguir la redirección** a Microsoft. No inicia sesión ni consulta Nexo.

Evidencia: [User Secrets y ediciones](migracion/evidencia-user-secrets.json), [navegador con perfil de Visual Studio](migracion/evidencia-user-secrets-navegador.json). La prueba del perfil se ejecutó por CLI; **no se operó la interfaz ni el depurador de Visual Studio**. Conectividad institucional y aceptación visual siguen pendientes.

Evidencia histórica: [arranque sin credenciales](migracion/evidencia-arranque-local.json), [importación DPAPI](migracion/evidencia-importacion-local.json), [8 reglas sintéticas del importador original](migracion/evidencia-importador-sintetico.json) y [arranque DPAPI configurado](migracion/evidencia-arranque-configurado.json). Sus comandos Import/Configure ya fueron sustituidos por User Secrets.
