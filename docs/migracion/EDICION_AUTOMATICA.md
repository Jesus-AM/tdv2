# Edición automática por registro — 2026-10-08

Esta entrega sustituye la recuperación manual descrita en las entregas anteriores. No existen Resolver, Resolver eliminación, comparaciones ni elección de versiones. No cambia participación de áreas, Nexo, autenticación, fotografías, permisos ni esquema.

## Actualización 2026-10-09: entrega y presencia

La entrega vigente termina en Sistemas y herramientas, incluidos Medios utilizados. `ProcedureEligibility` centraliza los campos obligatorios existentes de Identificación y la disponibilidad V/A; el estado y cada confirmación de guardado incluyen `procedimientosDisponibles`. SignalR provoca una nueva lectura autorizada y actualiza ambos usuarios al completar, invalidar o eliminar procedimientos. Una relación histórica que quede incompleta se conserva, permite guardar otros campos y genera un pendiente contextual que impide entregar.

Identificación y Sistemas comparten `RowEditingPresence`: la foto/iniciales y el estado ocupan la primera columna **Edición**, de 124 px y fija al desplazar horizontalmente. Esta ubicación sustituye la anterior en Acciones. El nombre completo está disponible al pasar el puntero, enfocar o tocar; «Estás editando», «Ocupado» y «Otra pestaña» acompañan al color. La presencia no se inserta entre valores ni mueve el cursor. El foco del campo es el fieldset de MUI; sólo se elimina el outline duplicado de su input interior dentro de estas dos tablas. Se conservan los indicadores de teclado en botones, fotos y ayudas y el contorno suave de la fila.

El contexto de edición se sigue mediante `RecordEditingContext`: una pérdida de foco sin destino no es abandono. El foco en el contenedor desplazable, arrastrar sus barras, la rueda y los gestos táctiles conservan el registro; no se restaura el foco a la fuerza ni se renueva por desplazamiento. Otro destino de foco o un clic completado fuera del contexto termina la edición mediante el guardado y liberación existentes. Menús, ayudas y avatares conservan el contexto original. [Causa reproducida y verificación](DESPLAZAMIENTO_TABLAS.md).

Eliminar ocupa una columna independiente de 76 px, alineada arriba, con icono rojo y área de 44 × 44 px. Los campos multilínea empiezan con dos líneas y crecen hasta cuatro; el resto se recorre dentro del campo también en consulta. Las tablas de captura ya no tienen el límite vertical de 65vh: la página desplaza verticalmente y la tabla horizontalmente.

Medios utilizados es una sección separada después de la tabla, con su propia reserva `medios`, guardado y liberación. No comparte la reserva de una herramienta. El campo vigente `Otro medio` no tiene ejemplo ni placeholder; «Portal del CAST de uso de técnicos» permanece en el catálogo de herramientas.

## Causa reproducida antes de modificar la aplicación

La prueba `Edición/reproducción: referencia a código local no confirmado rechaza incluso un bloque independiente` se ejecutó contra el código original con una sola sesión sintética y dos reservas válidas. Envió un sistema con `proceso=PO-01`, ausente de Identificación, junto con un acuerdo válido. El servidor devolvió **422 «El proceso seleccionado no pertenece al formato»** y no guardó el acuerdo. Corrigiendo sólo la referencia, la misma sesión, reservas y versiones pudieron guardar. [Resultado previo](edicion-automatica-20261008/reproduccion-previa.json).

El código revisado completaba esta cadena:

1. `processEdit` calculaba códigos en el navegador y los ofrecía a Sistemas/Datos antes de confirmarlos. Dos editores también podían calcular el mismo siguiente código.
2. El motor agrupaba todos los bloques pendientes en una petición; `FormSchema.Validate` validaba el documento completo, incluso referencias históricas ajenas al cambio.
3. Un rechazo marcaba todos los bloques enviados como incidencias y `canEdit` impedía corregirlos. El error de validación se trataba como conflicto de edición.

La prueba previa del motor también reprodujo ese bloqueo general. Ahora es una regresión positiva: el campo inválido conserva su reserva, admite corrección y el registro independiente sí se guarda. No se consultó una base institucional; esta evidencia acredita el fallo del flujo local y no atribuye el origen de registros institucionales concretos.

## Cambios

- El servidor asigna el código dentro del bloqueo transaccional de UR y crea los nueve criterios vacíos en el mismo commit. Considera códigos históricos para no reutilizar la identidad de una evaluación eliminada. No cambia códigos confirmados ni inventa respuestas. El navegador ofrece los códigos después de recibir la confirmación.
- El borrador valida los registros escritos; retirar un proceso también comprueba sus dependencias. El envío valida la primera etapa mediante la misma revisión del servidor. Los valores de Datos, Evaluación, Preguntas y Acuerdos se conservan y no generan requisitos de esta entrega, aunque un administrador los muestre. No se eliminan vínculos históricos para conseguir que pase una validación ajena.
- Los errores 422 identifican bloque y campo. La reserva válida permite corregir directamente el dato; la validación no se transforma en conflicto de todos los registros.
- Cada fila independiente se guarda por separado. Una eliminación de proceso y sus relaciones sigue siendo una operación atómica: adquiere todas las reservas, conserva ID/versiones, exige que coincida lo confirmado, guarda y libera. Abrir/Cancelar no escribe ni adquiere reserva.
- Entrar en el campo solicita automáticamente la reserva. Las primeras teclas y el pegado se retienen mientras llega: sólo se aplican si la reserva está confirmada y el campo conserva su base. Tab entre campos no invalida esa escritura. Si no puede autorizarse, el texto queda visible como no guardado, nunca se aplica sobre una respuesta diferente.
- El ACK incorpora el código confirmado sin reemplazar texto escrito mientras viajaba el guardado. Cambiar de pestaña conserva datos fuera del componente desmontado. Continúan las actualizaciones por bloque y las referencias estables para filas no modificadas.
- Guardar no libera dentro de la petición mientras todavía se permite escribir. Después del ACK se comprueba que no queden cambios posteriores; sólo entonces se libera. Seleccionar opciones, abrir Select/Dialog y consultar avatares conservan el foco lógico de la fila.
- Se eliminó `FormProposalComparison` y sus acciones/métodos sin uso. Se conservan mensajes breves junto al registro, texto pendiente, aviso de salida y descarga de emergencia. La ocupación normal sólo muestra nombre, foto/iniciales, color y candado/otra pestaña.

## Recuperación interna y límites

Cada operación conserva UUID y cuerpo inmutables. Una respuesta perdida se confirma con el mismo recibo, aunque posteriormente haya vencido la reserva; el backend consulta el recibo sólo después de autorizar la solicitud. Si la operación nunca se confirmó, el token vencido no autoriza escribir. Reanudar requiere consultar estado vigente, comprobar acceso/contenido y obtener una reserva nueva.

Los intentos HTTP tienen un timeout de 15 segundos. La recuperación programa esperas de 1, 3 y 8 segundos, con presupuesto finito y límite por operación. No reintenta un 422 hasta que se corrige el dato. Un 401/403/419 bloquea escritura; no se trata como error temporal. Una reconexión comprobada o Guardar borrador puede reiniciar el presupuesto conservando el recibo. Ningún latido de SignalR renueva reservas ni inicia un bucle ilimitado de guardado.

Si la base compartida ya cambió, no se fusiona ni se escoge una versión. El texto local queda accesible, sin guardar; los otros registros autorizados siguen disponibles. No hay sobrescritura automática ni «último guardado gana». El pendiente vive en la pestaña, no en almacenamiento persistente del navegador: un cierre forzado puede perder lo no confirmado. El aviso de salida y la descarga permiten conservarlo deliberadamente. No se promete recuperación después de cerrar forzosamente el navegador.

`FormHub.Watch` conserva su corrección: los intervalos de 15 segundos no cancelan lecturas; se revalidan sesión, contexto, Nexo y alcance. Los nombres/fotos/colores no autorizan escrituras. La caducidad sigue siendo finita (45 segundos) y las renovaciones corresponden sólo a cambios reales.

## Verificar desde la raíz y Visual Studio

Recorrido vigente con **dos identidades distintas** y una pestaña adicional de la misma cuenta: `TDV2_TEST_BROWSER_FLOW=delivery`. ASP.NET, PostgreSQL, sesiones, CSRF, reservas, SignalR y envío son reales en aislamiento; Microsoft/Nexo/SII son sintéticos. [Evidencia](entrega-sistemas-20261009/dos-usuarios-postgresql.json). Se bloquean solicitudes externas. Móvil y tacto son emulación de Edge, no pruebas de dispositivos físicos.

```powershell
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:TDV2_TEST_BROWSER_FLOW='delivery'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -SyncOnly
```

`-DirectStart` inicia sólo el clúster nuevo bajo `.artifacts` con el mismo usuario; no modifica servicios Windows. `TDV2_TEST_NODE` puede omitirse si Node está en PATH. Las selecciones de pruebas son independientes; ejecutar secuencialmente para no disputar DLL de pruebas cargadas en Windows. Los siguientes comandos históricos cubren otras partes del editor:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll

$env:TDV2_TEST_CASE_PREFIX='Edición/'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='users-served'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El script crea `tdv2_native_test` en un puerto aleatorio de loopback y verifica que esté bajo `.artifacts`. Usa ASP.NET, PostgreSQL y Edge reales con Microsoft/Nexo sintéticos; no utiliza User Secrets institucionales.

Abre `tdv2.slnx`; conserva **TDV2 HTTPS + React**, HMR y `/connect`. Para probar escrituras con dos sesiones desde Visual Studio ejecuta los comandos sintéticos en su terminal. Para aceptación manual con F5 hace falta un entorno de pruebas configurado por el operador: ventana normal, InPrivate y segunda pestaña; entrar a campos, escribir/pegar inmediatamente, Tab, selección múltiple, salir, eliminar/Cancelar y desconectar/reconectar. No ensayar esas escrituras con la conexión institucional vigente. F5 no migra.

No hay una migración nueva. Se actualizan conjuntamente código backend y frontend; las migraciones previas del repositorio siguen siendo requisitos y no se aplicaron a ninguna base institucional en esta entrega. No hubo push ni despliegue.

## Evidencia y pendientes

Resultados finales y rutas de las corridas: [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). [PostgreSQL: 28/28](edicion-automatica-20261008/postgresql.json), con códigos concurrentes, pertenencia, reservas/versiones, auditoría atómica, revocación, ILDA y envío inmutable. Las pruebas frontend incluyen respuesta perdida, escritura posterior a la petición, reincorporación del código, expiración/reasignación y ausencia de métodos de recuperación manual.

TypeScript, Vite y solución Release correctos; **75/75 frontend**, **62/62 dominio/aplicación**, [33/33 recorridos de formato](edicion-automatica-20261008/navegador.json) y [6/6 selección múltiple, teclado y móvil](edicion-automatica-20261008/seleccion-multiple.json). Sin errores JavaScript. La [repetición final de dos regresiones PostgreSQL](edicion-automatica-20261008/regresion-final.json) incluye una entrada sin código obligatorio: responde 422 y permite corregir con la misma reserva. No se suman las repeticiones como casos nuevos. Capturas: [guardado confirmado](edicion-automatica-20261008/captura.png), [eliminación ocupada](edicion-automatica-20261008/eliminacion-ocupada.png) y [móvil](edicion-automatica-20261008/movil.png).

Pendiente: aceptación con identidades reales, conectividad/proxy institucional y dispositivos móviles físicos/teclados IME. Las pruebas con proveedores sintéticos y emulación móvil no acreditan esos entornos. No se investigaron ni corrigieron datos institucionales.
