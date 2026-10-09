# Revisar y enviar por etapa — 2026-10-09

## Implementado

La pestaña **Revisar y enviar** aparece después de Sistemas y herramientas. Se puede consultar con pendientes; no es un bloque de captura ni suma avance. Los formatos nuevos empiezan en Contexto. Durante la captura, la ubicación de los existentes se conserva por actor e identidad efectiva, incluyendo la revisión, sin cambiar respuestas, versión o porcentaje. La consulta recupera la ubicación existente, sin concederle nuevas escrituras. Navegar hacia un pendiente espera la finalización del guardado anterior antes de enfocar y reservar el campo de destino.

La revisión muestra **Primera etapa**, área, ejercicio, estado de sus tres secciones, cantidades de procedimientos y herramientas, medios seleccionados, autoguardado y pendientes por sección, registro y campo. Los enlaces llevan al control correspondiente. Contexto sigue siendo informativo; Medios y comentarios conservan sus condiciones opcionales. Las validaciones históricas D/N permanecen pendientes, sin borrarlas ni convertirlas. Mostrar las secciones posteriores como administrador no cambia los requisitos de envío.

El componente existente `FormSubmissionReview` utiliza la proyección `entrega` del servidor. `FormStages` define las respuestas, secciones, revisión, resumen y avance de cada etapa habilitada. Hoy sólo existe la definición de la primera: Contexto, Identificación general y Sistemas y herramientas, incluidos Medios. Reutiliza `FormCapture`, `FormReview`, `ProcedureEligibility` y las reglas de herramientas; no añade campos obligatorios.

**Enviar primera etapa** requiere permiso vigente sobre el formato, respuestas completas, autoguardado confirmado y ausencia de reservas ajenas. Los colaboradores y perfiles de consulta ven la revisión sin adquirir permiso de envío. El diálogo explica: «Al enviar la primera etapa, sus respuestas quedarán disponibles para consulta y ya no podrán editarse.» Se invalida si cambia la versión o etapa, llegan pendientes o aparece otra reserva. SignalR mantiene la revisión actualizada sin recargar.

POST `/formatos/{ur}/enviar` reutiliza la transacción y los bloqueos existentes: valida identidad, contexto/representación, CSRF, alcance, participación, ejercicio, etapa habilitada, versión, requisitos y reservas. `stage` identifica la etapa revisada; la omisión conserva compatibilidad con los clientes de la primera etapa. No existe una ruta para habilitar otra etapa. El UUID y la huella del recibo evitan duplicaciones por doble clic o reintento. Envío, recibo y auditoría confirman o revierten juntos. El cliente comparte una sola petición en curso. Al confirmar muestra **Primera etapa enviada**, fecha y nombre conservado del autor efectivo.

## Persistencia y conservación

La migración **`20261009171656_StageSubmissions`** añade `formatos_ur.etapa_activa` (inicialmente 1) y `public.formato_envios_etapas`. Esta última tiene clave formato/etapa y conserva ejercicio, versión, operación, fecha, actor Microsoft, identidad efectiva, nombre y una instantánea sólo de las respuestas de esa etapa y su área. No altera las respuestas, fechas ni auditorías anteriores.

Los envíos globales existentes conservan sus columnas y su trigger de bloqueo. **No se atribuyen automáticamente a la primera etapa**: carecer de un identificador explícito de etapa es insuficiente para inferirlo. Se muestran como envío anterior de etapa no identificada y permanecen íntegramente bloqueados. No hay reapertura ni backfill de contenido.

Los nuevos triggers impiden modificar/eliminar un envío de etapa o sus respuestas confirmadas, incluso desde otra vía SQL. La inserción comprueba formato/ejercicio/etapa e igualdad con las respuestas guardadas. El servicio también rechaza reservar o cambiar bloques pertenecientes a instantáneas enviadas. Retirar esta migración se rechaza si existen envíos de etapa o envíos globales; no se elimina protección para facilitar un Down.

Una etapa futura requiere una definición explícita y su habilitación autorizada. Su revisión, avance y envío tienen identidad propia; la fecha de la primera no la marca como enviada. Las referencias de la primera se conservan para consulta y sus respuestas permanecen bloqueadas. **La segunda etapa no está implementada ni habilitada**, y no se inventan sus campos o requisitos. La prueba de independencia utiliza datos exclusivamente sintéticos en PostgreSQL; no publica una API de captura de segunda etapa.

## Aplicación posterior por el operador

Desde la raíz del repositorio, con el destino autorizado ya configurado y tras revisar el SQL y la recuperación:

```powershell
dotnet ef migrations script 20261009080533_SiiModulesCatalog 20261009171656_StageSubmissions --project tdv2 --output .artifacts/StageSubmissions.sql -- --environment Development
dotnet ef database update 20261009171656_StageSubmissions --project tdv2 -- --environment Development
```

El comando Development utiliza la configuración local vigente de User Secrets sin cambiarla. En Ubuntu, el operador usa la configuración externa ya instalada con `--environment Production`; el mecanismo sigue siendo una ejecución explícita de EF, nunca el arranque web. Actualizar backend y assets juntos y recargar clientes antiguos. No se aplicó esta migración a ninguna base institucional en este trabajo.

## Verificación reproducible

Los resultados finales y límites están en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). Las pruebas nativas crean y detienen PostgreSQL 18 desechable bajo `.artifacts`; Microsoft y Nexo son pares sintéticos del proyecto de pruebas. No se consultan fuentes institucionales.

```powershell
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:PATH=(Split-Path $env:TDV2_TEST_NODE)+';'+$env:PATH
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly -DirectStart
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser -DirectStart
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_BROWSER_FLOW='stage-review'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
$env:TDV2_TEST_BROWSER_FLOW='delivery'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

La prueba de navegador usa HTTP/SignalR y persistencia reales. Retiene solicitudes reales para comprobar autoguardado y respuestas en vuelo; no sustituye el transporte de edición. Las pruebas unitarias de `BlockEditor` sí usan transporte simulado. Escritorio y móvil se verifican en Edge automatizado; los dispositivos físicos y la aceptación institucional quedan pendientes.
