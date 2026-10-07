# Eliminación directa de registros — 2026-10-06

## Implementado

`removalReason` separa la posibilidad de iniciar una eliminación de la autorización para escribir. La ausencia de reserva propia ya no deshabilita el icono. Se conservan carga inicial, permisos, estado enviado, ILDA, propuestas con errores y reservas ajenas del registro o sus relaciones como restricciones. El motivo indica el titular o la otra pestaña; se actualiza al recibir el estado compartido.

El icono y la acción del diálogo mantienen MUI y color rojo. Enfocar el icono con ratón/teclado no activa el manejador de los campos. No hay un contenedor de fila deshabilitado por ausencia de reserva. Abrir muestra la confirmación y enfoca Cancelar; cancelar no toma reservas ni elimina datos.

Al confirmar, `engine.prepare(keys)` adquiere todas las reservas necesarias. La confirmación conserva ID, contenido y versiones del registro, su evaluación y sistemas/datos vinculados. Una modificación de esos bloques o una relación nueva exige cancelar, revisar la versión vigente y volver a confirmar. Reordenar o eliminar otra fila no cambia el ID elegido. `engine.edit` vuelve a validar después de esperar la reserva, y `canEdit` sigue siendo obligatorio para cambiar/guardar.

El diálogo permanece ocupado hasta que el servidor confirma el guardado; se evitan doble clic y cierre durante la operación. Confirmar un recibo anterior no se confunde con guardar la eliminación actual. Se liberan reservas limpias; una propuesta sin confirmar conserva su recuperación, identificada por nombre de registro, aunque la fila haya desaparecido de la propuesta local. Recuperar o conservar la respuesta guardada sigue requiriendo decisión explícita.

La verificación encontró además una carrera de orden: una lectura iniciada durante la adquisición podía llegar después y parecer posterior a la reserva. El servidor fecha la confirmación con el reloj PostgreSQL **después del commit**, balanceando la apertura/cierre de conexión de EF. El vencimiento original no se prolonga. No se cambian permisos, auditoría, testigos de reserva, versiones, bloqueo de enviados ni el temporizador de SignalR.

Archivos de aplicación corregidos:

- `ClientApp/resources/js/pages/FormatoUR.tsx`: disponibilidad, foco, confirmación y recuperación contextual.
- `ClientApp/resources/js/Components/RemoveRowDialog.tsx`: espera del guardado y bloqueo de confirmaciones antiguas.
- `ClientApp/resources/js/lib/form-deletion.ts`: instantánea del registro y relaciones por ID/versión.
- `ClientApp/resources/js/lib/block-editor.ts`: precondición revalidada y liberación selectiva de reservas limpias.
- `tdv2/Services/FormEditingService.cs`: fecha de confirmación de reserva posterior al commit.

No se necesitan migraciones ni nuevas dependencias.

## Verificación

Las pruebas usan Edge, React, ASP.NET y PostgreSQL reales con identidades Microsoft/Nexo sintéticas en un clúster desechable. No usan bases o credenciales institucionales.

El recorrido `ClientApp/tests/browser/deletion-flow.mjs` se integra en la suite de formatos existente y comprueba:

- Cargar Identificación general, Sistemas, Datos y Acuerdos; comprobar icono habilitado sin enfocar ningún campo. Abrir por teclado no envía reservas. Confirmar reserva automáticamente, espera el guardado y libera; verifica datos persistidos y cascada del proceso.
- Cancelar enfocado, sin alteración del contenido y sin petición de reserva.
- Reserva de otra pestaña, también de una relación; bloqueo con motivo y rehabilitación sin recargar al liberarla.
- Contenido o relaciones actualizados mientras el diálogo está abierto; confirmación bloqueada hasta revisar.
- Cambio recibido mientras `prepare` espera; no aplica la eliminación antigua y libera las reservas adquiridas.
- Dos sesiones con cookies independientes confirman a la vez: una adquisición 200, una 409, un solo PATCH y un solo incremento de versión.
- Fallo SQL 503 conserva la propuesta; recuperación explícita y reintento del guardado.

Las pruebas frontend cubren además el ID estable, versiones y relaciones nuevas. La prueba PostgreSQL pausa una adquisición mientras otra petición consulta; comprueba que esa lectura sea anterior a la confirmación.

Resultado final: **57/57 frontend**, **23/23 backend PostgreSQL** y **27/27 comprobaciones de navegador**, sin errores JavaScript. TypeScript, Vite y solución Release correctos; .NET sin advertencias ni errores. Evidencia: [PostgreSQL](evidencia-eliminacion-directa-postgresql.json), [React/Edge](evidencia-eliminacion-directa-react.json) y [captura del rechazo de una eliminación por reserva ajena](eliminacion-reserva-ajena.png), revisada visualmente. Las fuentes externas están bloqueadas en la prueba aislada; no se modificó la configuración tipográfica de producción. El detalle de la corrida final y los intentos previos corregidos está en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).

Comandos desde la raíz (Node/npm y .NET 10 en PATH):

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El script `Test-NativePostgres.ps1` crea y detiene el clúster sintético bajo `.artifacts`. También puede ejecutarse desde la terminal de Visual Studio. F5 conserva `tdv2.slnx` / **TDV2 HTTPS + React** y no aplica migraciones. Para aceptación manual, usar una base aislada y dos sesiones de prueba: sin enfocar campos, abrir/cancelar/eliminar; reservar desde la otra sesión, comprobar bloqueo y liberar; confirmar simultáneamente.

## Pendiente y límites

La versión publicada no se actualiza con estos cambios locales. La publicación y aceptación institucional corresponden al operador. No se modificaron bases institucionales, credenciales ni Herd; no hubo push ni despliegue. Las pruebas sintéticas no certifican la configuración institucional real.
