# Reservas de edición y SignalR — 2026-10-06

## Implementado

`FormHub.Watch` usa `PeriodicTimer` para revalidar cada 15 segundos. Espera una notificación, el intervalo o la terminación del stream. El intervalo **no cancela** una lectura ni genera `OperationCanceledException`. Desconexión y cancelación de suscripción comparten un token de vida. Si una consulta en curso se cancela, el filtro comprueba el token concreto y su estado; otras cancelaciones siguen propagándose. El `catch` está separado de `yield return`, como exige C#. `finally` retira la suscripción; se liberan ámbito, registro y temporizador. No se alteró el depurador.

Cada entrega mantiene la comprobación de ticket, Nexo, contexto, revisión y alcance. Cancelar el stream no guarda, elimina ni libera propuestas: SignalR informa; PostgreSQL conserva la autoridad de escritura.

La reserva se solicita automáticamente al entrar en un campo del registro, también con teclado. No hay botones para iniciar o terminar la edición ni confirmación para reservar. `BlockEditor` exige una reserva confirmada antes de aceptar cambios, incluidas altas y eliminaciones con relaciones. Adquirir entrega versión **y contenido vigente**. Durante la espera los campos son de consulta y aparece «Preparando edición…». Casillas, radios y Select solicitan la reserva desde el propio control; el menú sólo abre después de confirmarla. Una intención pendiente no se aplica si se abandona el registro. Una disputa normal por la reserva se muestra en el registro; no genera un conflicto global. SignalR por sí solo no concede la primera escritura.

**Eliminar no exige entrar antes a un campo.** El icono rojo permite abrir la confirmación al cargar un registro eliminable; abrir, enfocar el icono o cancelar no adquiere reserva. `engine.prepare(keys)` reserva el registro y sus relaciones sólo al confirmar. Se revalidan contenido y versiones por ID; una confirmación antigua se bloquea y pide cancelar/revisar. Titular ajeno u otra pestaña deshabilitan la acción con su nombre/motivo, y liberar la rehabilita por SignalR. El diálogo espera el guardado y después libera; un fallo conserva una recuperación contextual identificada por registro. [Detalle y evidencia](ELIMINACION_DIRECTA.md).

Las filas son independientes; preguntas y criterios conservan sus bloques individuales. `BlockEditingStatus` muestra una sola indicación por registro:

- Otra persona: candado, nombre y fondo ámbar suave.
- Reserva propia: «Estás editando» en azul.
- Otra pestaña de la misma sesión/contexto: «Estás editando este registro en otra pestaña».

No se compara el nombre para inferir identidad. Otra sesión del mismo usuario es independiente y muestra el nombre del titular. No se exponen hashes de sesión ni identificadores de reservas ajenas. Se puede consultar/copiar; modificación y eliminación quedan bloqueadas. Transiciones de 180 ms, anuladas con movimiento reducido.

Las reservas duran 45 segundos y se renuevan sólo por cambios reales, como máximo una petición cada 15 segundos durante actividad. Foco, lectura SignalR y pantalla abierta no renuevan. Al salir se confirma el guardado antes de liberar automáticamente. Select/Dialog conservan el foco lógico del bloque aunque usen portales. SignalR retira el estado ocupado en las demás sesiones; basta entrar en el campo para editar directamente, sin recargar ni pulsar una acción de reserva. Si el campo conservaba foco al liberarse o vencer, una nueva interacción vuelve a confirmar reserva y contenido. No se readquiere por tener la página abierta. Volver durante una liberación espera antes de adquirir otra reserva. Los cierres abruptos se resuelven por vencimiento.

La desconexión congela modificaciones. Un mensaje en vuelo no rehabilita la captura: una nueva consulta debe revalidar acceso, reserva y versión. Perder la reserva conserva la propuesta y exige una decisión explícita, sin readquirir ni sobrescribir automáticamente. Los reintentos inciertos conservan UUID y cuerpo originales.

Se retiró el panel habitual de recuperación. Un fallo real muestra un mensaje junto al registro y **Resolver**. El diálogo compara esa propuesta y sus relaciones; permite confirmarla o conservar explícitamente la respuesta guardada y mantiene la descarga de emergencia. Cancelar no cambia respuestas. Una eliminación fallida conserva su aviso al pie de la tabla aunque la fila ya no esté en la propuesta. Se preservan propuestas de otros bloques.

Se mantienen auditoría, CSRF, identidad real/efectiva, versión, permisos, ILDA y bloqueo de enviados. No se modificaron roles, OAuth, Nexo ni sincronizadores. No se añadió migración ni dependencia.

## Verificación desde la raíz

```powershell
# Sólo si Node no está en PATH: usa el instalado con Visual Studio, sin instalar nada.
$env:PATH = 'C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs;' + $env:PATH
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
# Sólo el recorrido de formatos, incluidas dos sesiones y dos pestañas:
$env:TDV2_TEST_BROWSER_FLOW = 'formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
# Regresión completa, incluidos los grupos PostgreSQL/navegador anteriores:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

El script crea y comprueba `tdv2_native_test` en un clúster nuevo de loopback bajo `.artifacts`, usa identidades/fuentes sintéticas y lo detiene al terminar. Compila Release sin apphost para no disputar el ejecutable Debug abierto por F5. No utiliza conexiones institucionales de User Secrets. React, ASP.NET y PostgreSQL son reales; Microsoft y Nexo son dobles de prueba. Pruebas en memoria, del proveedor y del navegador son evidencias distintas. Resultados: [estado](../../ESTADO_MIGRACION.md).

## Visual Studio y dos sesiones

1. Abre `tdv2.slnx`. Se conservan **TDV2 HTTPS + React**, HMR y `https://localhost:7136/connect`. F5 no aplica migraciones.
2. Para reproducir sin tocar datos institucionales, ejecuta los comandos anteriores desde la terminal de Visual Studio. `-BrowserOnly` usa dos sesiones autenticadas independientes (cookies separadas) y dos pestañas de una sesión en Edge automatizado; `-EditingOnly` añade usuarios distintos compitiendo en PostgreSQL. Puedes adjuntar el depurador al proceso `dotnet` cuya línea de comando ejecuta `TDV2.NativeVerification.dll` para observar `FormHub.Watch`.
3. Para aceptación manual con F5, usa un entorno de pruebas previamente configurado con base aislada y accesos de prueba de Nexo. **No ensayes escrituras con la conexión institucional vigente.** Abre una ventana normal y otra InPrivate con dos usuarios autorizados; abre además una segunda pestaña de la primera cuenta.
4. Entra directamente en un campo, casilla, criterio o desplegable. La reserva es automática; mientras prepara no debe aceptar cambios. Sólo una sesión obtiene la fila; las demás ven titular/candado y no pueden eliminarla. Otra fila sí puede editarse simultáneamente. La segunda pestaña muestra el mensaje específico.
5. Cambia de campo y abre Prioridad: la reserva permanece. Edita y sal de la fila: se guarda antes de liberar. El estado se actualiza en la otra sesión, donde puedes entrar directamente al registro y recibir los valores vigentes. No hace falta recargar ni pulsar botones de edición. Eliminar → Cancelar no cambia el contenido.
6. Deja la fila sin actividad al menos 45 segundos: vuelve a consulta sin falso conflicto. Mantén el stream más de 45 segundos con el depurador: los ciclos de 15 segundos continúan sin excepciones del temporizador.
7. Desconecta con una propuesta pendiente y reconecta. Debe conservarse sin aplicarse automáticamente; **Resolver** permite revisarla. Comprueba recarga/navegación con respuestas guardadas y advertencia de salida con pendientes. Cerrar sesión revoca también los streams abiertos; los enviados permanecen de consulta.

## Límites y pendientes

La propuesta pendiente vive en la pestaña. El aviso de salida y la descarga permiten conservarla, pero no hay persistencia de respuestas sin confirmar después de un cierre forzado. Esto no altera datos compartidos ni aplica «el último guardado gana». Al terminar un proceso puede no llegar la liberación HTTP: el vencimiento es la protección final.

Falta aceptación interactiva con F5, identidades Nexo/Entra reales y proxy institucional por el operador. La prueba local no certifica esa infraestructura. No se hicieron push, despliegue, cambios en Herd/credenciales ni escrituras institucionales.
