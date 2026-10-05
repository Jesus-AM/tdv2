# Autenticación, Nexo y PostgreSQL

Implementación y pruebas del segundo bloque, ampliadas por [delegación, representación y auditoría](DELEGACION_REPRESENTACION.md). Estado y resultados vigentes en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). **No es autorización para conectar o migrar las bases existentes.**

## Contratos tomados de Herd

| Referencia Laravel | Adaptación ASP.NET |
|---|---|
| app/Http/Controllers/Auth/AuthController.php y listener Microsoft | Controllers/AuthenticationController.cs, Integrations/Microsoft/MicrosoftClient.cs, Security/PostgresIdentity.cs |
| Sesión Microsoft versionada y validación tenant/object ID | Security/PostgresTicketStore.cs, PostgresIdentity.Matches y Web/RequestAccess.cs |
| GraphToken y foto Graph | Security/GraphTokens.cs y MicrosoftClient.Photo |
| Services/NexoAccessService.php | Integrations/Nexo/PostgresNexoProfiles.cs y Domain/ModuleAccess |
| Services/TDV2/FormAccess.php y directorio UR | Domain/FormAccess, UnitDirectory y Web/AccessBoundary.cs |
| Formatos y validación canónica | Domain/FormSchema.cs e Infrastructure/PostgresFormStore.cs |

El inicio usa código OAuth con state y PKCE, conforme al contrato actual de Laravel. La prueba de identidad es la respuesta HTTPS autenticada de Graph /me después de canjear el código en el tenant configurado. No se consume un ID token para autenticar ni se aceptan sus claims sin verificar. No se implementó un segundo protocolo OIDC basado en ID token/nonce.

El nombre local procede de Nexo. Los claims del ticket contienen ID local, correo, nombre, tenant, object ID y versión de sesión; no almacenan roles, módulos ni alcance autorizante. Nexo se consulta nuevamente en cada solicitud protegida. La pertenencia a la aplicación se obtiene de sus vistas publicadas para la cuenta PostgreSQL, nunca de React.

Las UR siguen procediendo del catálogo local replicado desde SII: adscripción/origen publicados por Nexo y num_empleado textual se resuelven contra ese catálogo. Nexo publica la jerarquía de módulos, que se valida por clave, padre y ruta exacta. No se convierte una adscripción en responsabilidad institucional.

## Configuración del destino

La aplicación no contiene valores reales. Configuración mediante el proveedor seguro del entorno o variables inyectadas al proceso; no copiar .env de Herd ni introducir secretos en comandos guardados, documentos o capturas.

| Clave ASP.NET | Variable de entorno | Requisito |
|---|---|---|
| Microsoft:TenantId | Microsoft__TenantId | GUID del tenant institucional; no common/organizations |
| Microsoft:ClientId | Microsoft__ClientId | GUID del registro de aplicación |
| Microsoft:ClientSecret | Microsoft__ClientSecret | Secreto del registro, suministrado fuera del repositorio |
| Microsoft:PublicOrigin | Microsoft__PublicOrigin | Origen HTTPS fijo, sin ruta, query ni credenciales; por ejemplo https://localhost:7136 |
| ConnectionStrings:Tdv2 | ConnectionStrings__Tdv2 | Base de TDV2 nueva/aislada preparada explícitamente; usuario de ejecución sin DDL |
| ConnectionStrings:Nexo | ConnectionStrings__Nexo | Cuenta publicada por Nexo para tdv2: SELECT de vistas y EXECUTE de funciones delegadas/representación; sin escritura directa en tablas internas |

El callback se calcula como PublicOrigin + /connect. Registrar exactamente ese destino en Microsoft antes de validar con una cuenta real. La vuelta del logout usa PublicOrigin + /. Los scopes solicitados son openid, profile, offline_access y User.Read. No existe parámetro de navegador para cambiar tenant, aplicación Nexo o destino de redirección.

La clave de aplicación Nexo es **tdv2**, no un ID configurable desde React. El adaptador exige exactamente una fila en nexo_aplicacion, con ID positivo y nivel_control=2. El ID numérico se deriva de esa fila; las pruebas usan 47 para evitar depender de un ID histórico. Consulta public.nexo_* y exige columnas de identidad institucional num_empleado, ID_UR y origen_ur. Roles se filtran por correo y usuario_aplicacion_id; módulos por roles y aplicacion_id.

Sin configuración, /connect devuelve 503; nunca se habilita un usuario de demostración. /health/live sólo informa que el proceso responde: no acredita acceso a Microsoft, Nexo o PostgreSQL.

Para ejecutar la aplicación después de preparar únicamente un entorno autorizado y aislado:

```powershell
dotnet run --project tdv2 --launch-profile https
```

Abrir https://localhost:7136. El perfil HTTP permite inspeccionar la portada, pero no es el entorno de autenticación: cookies de sesión, OAuth y CSRF requieren HTTPS. El despliegue detrás de un proxy y sus encabezados confiables todavía no se validaron.

## SQL, cifrado y operación

- `tdv2/Migrations/InitialTdv2` es la migración EF inicial de las 14 tablas operativas: identidad, tokens, auditoría, UR, formatos, colaboraciones, sesiones, intentos OAuth, contextos y sincronización. `InitializePausedSynchronization` inserta únicamente la configuración pausada. Los SQL 001–004 se conservan sólo como fixtures en tests; no son un mecanismo vigente de aplicación.
- EF mantiene `__EFMigrationsHistory` y requiere ejecución explícita del operador. El bootstrap rechaza una base ocupada; la compatibilidad/adopción de un esquema anterior requiere revisión sobre una copia aislada. La cuenta local necesita SELECT/INSERT/UPDATE/DELETE sobre contextos y colaboraciones, e INSERT en activity_logs con acceso a sus secuencias. No se aplica DDL automáticamente.
- La aplicación necesita SELECT/INSERT/UPDATE/DELETE en identidad/tokens/sesiones/intentos/formatos y auditoría según su operación, USAGE en las secuencias correspondientes, SELECT en catálogo/colaboraciones y permiso UPDATE sobre la fila UR para SELECT FOR UPDATE. El procesador requiere además INSERT/UPDATE en UR y lectura/escritura de réplica/cola/configuración/metadatos/historial/bitácora; nunca DDL ni permisos de escritura en fuentes remotas. El test configura esos permisos sin otorgarle superusuario. Debe afinarse la concesión final en el despliegue.
- La cuenta de Nexo de las pruebas lee sus vistas y ejecuta funciones SECURITY DEFINER publicadas; no accede directamente a las tablas que las alimentan. En el entorno institucional los filtros y permisos los administra Nexo; aún no se verificaron allí.
- Formatos conserva JSON de respuestas, versión inicial de lectura 0 y primera escritura 1, avance calculado por servidor, actualizado_por y timestamps. Se asumen timestamps sin zona en UTC, según las declaraciones de Laravel; falta contrastarlo con el esquema efectivo.
- El bloqueo de UR serializa también el primer INSERT. La transacción lee versión, compara, escribe y confirma. No reintenta automáticamente un conflicto ni un resultado de COMMIT ambiguo por red.
- Tickets, PKCE y tokens Graph usan propósitos de cifrado diferentes. Los tokens nuevos llevan prefijo aspnet:v1:. No son compatibles con el cifrado Laravel; se requiere nuevo login y no se deben compartir escrituras de tokens entre ambas aplicaciones durante un corte sin diseñarlo previamente.
- Las llaves ASP.NET se persisten bajo tdv2/.runtime/keys y se protegen con DPAPI en Windows. Dependen de la cuenta de ejecución. No se validaron aún reinicio del servicio con llaves persistidas, varias instancias ni rotación/restauración. Las pruebas usan llaves efímeras y no certifican estos aspectos.
- La caducidad se comprueba al leer sesiones/intentos. Su limpieza física programada queda pendiente; el nuevo procesador de sincronizaciones no limpia sesiones. Logout elimina el ticket y la renovación concurrente no puede recrearlo.

## Aislamiento y reproducción

scripts/Test-NativePostgres.ps1 usa binarios nativos instalados; no Docker ni un servicio existente. Cada ejecución crea un directorio nuevo bajo .artifacts, inicializa un clúster SCRAM/UTF-8, escucha únicamente en 127.0.0.1 y usa un puerto libre diferente del servicio convencional. El ejecutable valida conexión y SHOW data_directory antes de cualquier escritura.

Las contraseñas de prueba se generan aleatoriamente y no se imprimen. El navegador no recibe cadenas de conexión. Al terminar se detiene ese clúster y se vacía el archivo inicial de contraseña; se conservan datos sintéticos y logs para investigar. Las claves de los dobles no se instalan en la aplicación productiva.

Requisitos: .NET 10, frontend construido, PostgreSQL nativo (probado 18.6), Edge (probado 154.0.4258.37), Node y certificado HTTPS ASP.NET ya existente. En Windows, Schannel rechazó el certificado generado sólo en memoria; la suite abre el almacén personal en lectura y usa el certificado de desarrollo existente, sin instalar/exportar llaves ni cambiar confianza. El navegador acepta ese certificado únicamente dentro de su contexto temporal de prueba.

```powershell
# Desde la raíz; nunca ejecutar estos comandos en Herd.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
# Diagnóstico parcial, siempre crea otro clúster nuevo:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
```

El host nativo conserva los proveedores productivos de identidad, cookie, CSRF, Nexo SQL y formatos. Sólo Microsoft y Data Protection se sustituyen por dobles de pruebas. El publicador sintético Nexo crea vistas reales mediante nexo-fixture.sql; **no ejecuta Nexo Laravel**. El error de Nexo se induce retirando SELECT a esa cuenta sobre una vista sintética. La falla de guardado se induce con un trigger que lanza una excepción en el clúster de prueba.

El navegador utiliza las pantallas originales y el menú real de logout. Intercepta la respuesta local de redirección hacia Microsoft para simular la autorización y salida; el servidor canjea/valida con su doble HTTP. Bloquea todas las solicitudes externas, incluidas fuentes web. Sus controles de fixtures viven exclusivamente en el ejecutable NativeVerification, con clave aleatoria; no se registran en la aplicación productiva.

Resultados de referencia: [31 casos SQL/HTTP y grupo navegador](evidencia-postgresql.json), [nueve recorridos de Edge](evidencia-navegador.json). No sumar el grupo navegador y sus nueve recorridos como si fueran diez flujos diferentes.

## Validación institucional pendiente

| Evidencia requerida | Lo disponible ahora | Lo que falta |
|---|---|---|
| Microsoft inicia/cierra sesión real | Código productivo; state/PKCE/Graph/refresh/logout con doble HTTP | Registro institucional, configuración inyectada, callback aprobado y cuenta de pruebas; MFA, consentimiento, foto, rotación y SSO reales |
| Nexo publica acceso de tdv2 | Adaptador SQL contra vistas sintéticas y cuenta de lectura | Publicación de pruebas Laravel, su cuenta filtrada y cambios de rol/módulo/UR realizados realmente desde Nexo |
| Persistencia compatible con datos existentes | PostgreSQL nativo nuevo con esquema declarado y respuestas sintéticas | Exportación de esquema sin secretos y copia aislada para contrastar tipos/índices, datos históricos, UTC y corte/reversión |
| Recuperación operativa | Rollback SQL, reintento, conflictos y revocación | Caída de proceso/red/motor, COMMIT ambiguo, llaves persistidas, varias instancias y proxy |
| Interfaz intacta | Fuentes y componentes conservados, recorridos/capturas funcionales | Comparación visual con fuentes cargadas, móvil, teclado, impresión y borradores al caducar sesión |

Pendientes de paridad institucional: logout_hint opcional de Microsoft, metadatos históricos de autenticación y aceptación de las vistas/funciones realmente publicadas por Nexo. Delegación, escenarios, representación y auditoría de sus cambios ya tienen implementación; resultados actuales y límites en el documento de estado. Los dos informes enlazados arriba conservan la evidencia histórica del segundo bloque. Mantener los bloques sin cerrar frente a servicios reales hasta obtener esa evidencia.

Referencias técnicas: [cookies ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [HTTPS de Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-10.0). Las reglas de negocio proceden del código de Herd; las referencias no sustituyen las pruebas descritas.
