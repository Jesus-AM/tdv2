# Colaboradores centrales y delegados — 2026-10-06

## Implementado

- `tdv2/Domain/Access.cs`: concesiones de origen `central` explícito habilitan el rol efectivo coincidente según la adscripción individual vigente. Local resuelve un formato elegible de nivel 2/3; dependencias resuelve el nivel 2 y su rama de nivel 3. Sólo relaciones por IDs, sin prefijos ni equivalencia ID/clave. Las concesiones de origen `aplicacion` conservan todas las comprobaciones de vínculo local. Un retiro local pendiente sigue bloqueado.
- `tdv2/Web/RequestAccess.cs`: consulta concesiones vigentes aun sin filas locales, siempre para la identidad efectiva. La caché sólo dura la solicitud. Listado, apertura, lectura/SignalR, reservas, guardado y eliminación comparten el cálculo de alcance. No modifica permisos de envío, administración, representación ni consulta de prueba.
- `tdv2/Integrations/Nexo/PostgresNexoProfiles.cs`: conserva el origen publicado; ignora concesiones históricas de entrada sin rol. No deduce procedencia desde `Has` ni convierte valores desconocidos.
- `FormService.cs` e `Inicio.tsx`: el estado vacío explica falta de empleado, adscripción, jerarquía o autorización confirmada, conservando la interfaz y tipografía existentes.

Las concesiones centrales y delegadas aportan alcances independientes. Retirar una no invalida otras legítimas. Administrador sigue limitado a su rama para edición; supervisor conserva lectura institucional y sus responsabilidades verificadas. Los enviados continúan inmutables. No hay migración, dependencia nueva ni cambio de configuración de autenticación.

## Verificación sintética

Las pruebas de dominio cubren niveles 2/3/inferiores, falta de identidad/módulo/jerarquía, IDs distintos de claves, exclusión N y admisión 0, rol sin procedencia, combinación/revocación de ambas vías y composición con administrador/supervisor.

`CentralCollaborationTests.cs` prueba el cliente Nexo PostgreSQL real contra un publicador sintético y los endpoints ASP.NET: alta sin vínculos, ramas, reservas/guardado/eliminación, traslado, retiro, falta de módulo, suspensión publicada, fallo de consulta, retiro local pendiente, asignaciones simultáneas y representación. Las pruebas previas continúan verificando delegación, auditoría, concurrencia y enviados.

`central-collaboration-flow.mjs` comprueba en Edge el formato disponible sin colaboración local, autoguardado, rechazo ajeno, falta de envío y revocación durante una escritura pendiente: el servidor rechaza, la propuesta permanece visible, la copia compartida no cambia y SignalR termina por revalidación. También comprueba el mensaje por empleado ausente.

`Export-NexoDelegation.php` carga únicamente clases de fuente (PHP sin bootstrap, sin `.env`, Artisan ni conexiones) y genera la consulta de concesiones real de Nexo. `NexoDelegationTests.cs` la ejecuta en PostgreSQL desechable para verificar separación `central`/`aplicacion`, suspensión y revocación. Esa prueba del SQL fuente no certifica qué versión está publicada institucionalmente.

Resultados finales: **61/61 dominio/transporte**, **57/57 frontend**, **31/31 backend HTTP/PostgreSQL**, **8/8 SQL de Nexo** y **30/30 comprobaciones Edge**, sin errores JavaScript. TypeScript, Vite y solución Release correctos, cero advertencias/errores .NET. Evidencia: [backend](evidencia-colaboradores-centrales-postgresql.json), [SQL fuente de Nexo](evidencia-colaboradores-centrales-nexo.json), [navegador](evidencia-colaboradores-centrales-react.json). [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md) distingue las corridas, los fallos de fixture corregidos y los pendientes del operador. No se modificó código de producción para compensar esos fallos de preparación.

## Comandos desde la raíz

Con .NET 10, Node/npm y PostgreSQL 18 instalados:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly
```

Los scripts crean bases desechables en loopback y detienen sus clústeres; no leen ConnectionStrings institucionales. Los datos, identidades Microsoft/Nexo y cambios de permisos son sintéticos. Desde Visual Studio se pueden ejecutar en Terminal; para arranque habitual abrir `tdv2.slnx` y seleccionar **TDV2 HTTPS + React**. F5 no aplica migraciones. No efectuar pruebas de concesiones/escritura contra la base institucional.

## Pendiente institucional

Verificar la publicación existente y registrar asignaciones centrales explícitas con los roles/módulos correspondientes según [CONFIGURACION_NEXO_ENTRA.md](CONFIGURACION_NEXO_ENTRA.md). No se recrean aplicación, conexión, roles ni base. Si ya está publicado el contrato vigente, no requiere actualizar Nexo; únicamente distribuir TDV2 mediante el proceso autorizado del operador. Esta entrega no publica, despliega, modifica credenciales ni conecta con bases institucionales.
