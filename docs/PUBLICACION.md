# Publicación reproducible de TDV2

Este procedimiento construye un paquete; **no autoriza ni realiza un despliegue institucional**. La operación Ubuntu, el proxy real y las fuentes institucionales siguen pendientes de aceptación.

## Construcción

Desde la raíz, con .NET SDK 10 y Node/npm en PATH:

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-maintenance
```

El target `BuildReactForPublish` restaura el lockfile con `npm ci --ignore-scripts`, limpia exclusivamente `tdv2/wwwroot/assets`, compila Vite y agrega los archivos generados al paquete. No hay que recordar un build manual previo de React. Las imágenes y la portada se conservan. No publicar sobre un directorio viejo en el servidor: preparar una carpeta de versión nueva para no conservar archivos de versiones anteriores.

`UseAppHost=false` produce un paquete dependiente de ASP.NET Runtime 10 invocable con `dotnet tdv2.dll`; no obliga a usar un ejecutable Windows. La construcción se verificó en Windows, y el paquete se inició en ambiente Production con Edge/HTTPS. **No se ejecutó en Ubuntu.** Node y Vite sólo se necesitan para construir; no se incluyen como servidores de producción.

Comprobación local del paquete recién construido, desde la raíz y con 7146 libre:

```powershell
node ClientApp/tests/browser/publish-flow.mjs
```

## Configuración externa en Ubuntu

Instalar ASP.NET Core Runtime 10 mediante el procedimiento aprobado de infraestructura. Preparar una cuenta de servicio sin privilegios, un directorio de versión para el paquete y un directorio persistente privado para Data Protection. No copiar `.runtime` de Windows ni sus llaves DPAPI; Windows y Linux no comparten ese cifrado.

El servicio debe definir `ASPNETCORE_ENVIRONMENT=Production` (y evitar un `DOTNET_ENVIRONMENT=Development` heredado). ASP.NET no carga User Secrets en Production. Aportar mediante el gestor de secretos o entorno privado del servicio:

- `Microsoft__TenantId`, `Microsoft__ClientId`, `Microsoft__ClientSecret`, `Microsoft__PublicOrigin`.
- `ConnectionStrings__Tdv2`, `ConnectionStrings__Nexo`, `ConnectionStrings__Sii`, `ConnectionStrings__Ilda` según las fuentes autorizadas.
- `DataProtection__KeyDirectory` con una ruta persistente accesible sólo al servicio.
- `Synchronization__IldaEnabled=false` hasta habilitación explícita; el estado automático se administra en PostgreSQL.

Los nombres con `__` son las mismas claves jerárquicas de IConfiguration. No poner valores secretos en comandos, Git, React, unidades versionadas de systemd ni salidas de diagnóstico. Un archivo privado del servicio o variables de entorno no son por sí mismos almacenamiento cifrado. En Linux, el directorio de llaves requiere permisos estrictos y protección/respaldo de infraestructura; el código no promete cifrado en reposo por DPAPI.

Ejemplo **sin instalar** de invocación, una vez suministrados los secretos externos:

```bash
cd /opt/tdv2/releases/VERSION
ASPNETCORE_ENVIRONMENT=Production dotnet tdv2.dll --urls http://127.0.0.1:5064
```

Terminar HTTPS en el proxy institucional, reenviando todas las rutas (incluidos `/connect`, `/session`, `/user`, `/formatos` y `/configuracion`) al backend del mismo origen. No servir un fallback HTML del proxy por delante de la autorización ASP.NET. Mantener HTTPS público, Host validado y límites de cuerpo acordes con 1.6 MB; configurar `AllowedHosts` para el host final. `Microsoft:PublicOrigin` debe ser ese origen HTTPS explícito: no se deduce de cabeceras reenviadas ni parámetros del navegador. El backend no habilita confianza indiscriminada en `X-Forwarded-*`; no depende de ellas para construir el retorno OAuth.

Las cookies son Secure y el CSRF exige cabecera y cookie del mismo origen. El retorno local permanece `https://localhost:7136/connect`; producción requiere registrar su propio retorno autorizado en Entra. Las sesiones/cookies Laravel y las llaves Windows requieren nuevo login, no conversión automática.

Para captura colaborativa, reenviar también `/form-events` y su negociación, habilitando WebSocket y un timeout superior al keepalive de SignalR. TDV2 valida Origin contra `Microsoft:PublicOrigin`, incluso cuando el proxy termina TLS. Cada entrega vuelve a comprobar ticket, Nexo y UR; PostgreSQL arbitra todas las escrituras. En varias instancias, el aviso inmediato es local y las otras recuperan cambios mediante comprobación autorizada cada 15 segundos. Probar WebSocket/reconexión con el proxy institucional antes de operar.

## Base y procesador

EF Core es el único mecanismo de migraciones. Desde la raíz del código, con la configuración externa del destino y una cuenta de migración, ejecutar explícitamente `dotnet ef database update --project tdv2 -- --environment Production`. Revisar antes `dotnet ef migrations script --idempotent --project tdv2 --output .artifacts/tdv2-migrations.sql` y el destino/permisos. El primer bootstrap exige una base vacía y un solo operador. La cuenta del sitio no necesita DDL; el arranque no invoca EF Migrate ni EnsureCreated.

En desarrollo se añade `-- --environment Development` para cargar User Secrets. `public."__EFMigrationsHistory"` es el historial vigente. Los SQL 001–004 y la conversión anterior son fixtures históricos de pruebas; no se despliegan ni se aplican mediante un segundo ejecutor. Una copia Laravel requiere [revisión y adopción EF explícita](migracion/TRANSICION_DATOS.md), no el bootstrap.

El proceso web no ejecuta migraciones, no procesa la cola y no habilita tareas remotas. `--sync-worker`, `--sync-once` y `--sync-check=` son comandos independientes y explícitos; antes de instalarlos consultar [reservas, recuperación y operación](migracion/SINCRONIZACIONES.md). No se creó servicio, cron, tarea programada ni despliegue en este trabajo.
