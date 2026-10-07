# Usuarios que atiende y Prioridad — 2026-10-07

## Implementado

Identificación general utiliza un Select múltiple MUI con casillas: Comunidad universitaria, Docentes, Estudiantes, Personal administrativo y Otro. Marcar o desmarcar mantiene abierto el menú; el campo presenta las selecciones como texto. Las opciones parten vacías, tanto en registros manuales como en filas nuevas de ILDA.

El contrato conserva la clave `identificacion[].usuario`, ahora con una colección JSON de cadenas, por ejemplo `["Docentes", "Estudiantes"]`. El servidor rechaza valores escalares, opciones desconocidas y duplicados. Una colección vacía se puede guardar como borrador, pero cuenta como pendiente en avance y Revisión y envío. El servidor exige completarla antes del envío definitivo.

Prioridad conserva las cinco opciones, colores y valores históricos. «Selecciona una prioridad» se renderiza únicamente como indicativo del campo vacío: no existe una opción del menú con ese texto ni se guarda como respuesta.

Ambos controles comparten la reserva del registro, esperan su confirmación y conservan el autoguardado de un segundo. Los portales MUI no terminan la edición; Escape devuelve el foco al control y pasar al siguiente campo conserva la reserva. La salida de la fila guarda y después libera. Se mantienen versiones, propuestas pendientes, permisos, reservas ajenas, eliminación directa y bloqueo de enviados.

## Migración de datos

`20261007195019_StructuredUsersServed` no agrega columnas ni cambia el modelo. Dentro de la transacción EF:

- Limpia exclusivamente valores anteriores no estructurados de `identificacion[].usuario` en borradores (`enviado_en IS NULL`) del ejercicio editable, convirtiéndolos en `[]`. Conserva los registros y los demás campos, incluidas todas las prioridades. También preserva íntegramente los borradores históricos cuyo ejercicio ya difiere del catálogo, que el backend no permite editar.
- Conserva las colecciones que ya usan el nuevo contrato. No transforma datos al abrir ni durante cada guardado.
- Recalcula el avance de los borradores afectados con los pesos vigentes, incrementa su versión y la de los bloques modificados, y retira sus reservas antiguas. Las reservas de otros bloques permanecen intactas. El bloqueo transaccional impide intercalar escrituras con la conversión.
- Registra auditoría con campo, IDs de filas y versiones. Un fallo revierte contenido, versiones, auditoría e historial EF.
- No modifica ningún registro de formato enviado, su contenido, instantánea, porcentaje ni reservas. Su texto histórico se presenta en consulta, sin reinterpretarlo como selecciones nuevas.

El historial estándar EF asegura una única ejecución; además, las colecciones se conservan incluso al repetir el SQL. La limpieza del texto anterior es irreversible: `Down` no inventa respuestas ni convierte listas a texto; rechaza revertir si existen enviados, preservando las protecciones de las migraciones anteriores.

## Actualización posterior por el operador

Desde la raíz, con la aplicación detenida y después de revisar el destino y el historial configurados:

```powershell
dotnet tool restore
npm.cmd --prefix ClientApp run build
dotnet ef migrations script 20261007161549_ParticipantPhotographs 20261007195019_StructuredUsersServed --idempotent --project tdv2 --output .artifacts/usuarios-atendidos.sql
dotnet ef database update 20261007195019_StructuredUsersServed --project tdv2 -- --environment Development
```

El último comando utiliza `ConnectionStrings:Tdv2` y también aplica migraciones anteriores que estén pendientes. Se entrega para ejecución posterior: **no se aplicó en bases institucionales**. No volver a iniciar una versión antigua del editor sobre el contrato nuevo. F5 no ejecuta migraciones ni sincronizaciones.

## Verificación reproducible

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
$env:TDV2_TEST_BROWSER_FLOW='users-served'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

Las pruebas nativas crean y detienen PostgreSQL desechable en `.artifacts`. El navegador usa ASP.NET/React reales con Microsoft/Nexo sintéticos; no permite solicitudes externas. Node debe estar disponible en PATH. Evidencia y resultados finales en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).

En Visual Studio, abrir `tdv2.slnx` con **TDV2 HTTPS + React**, después de actualizar una base de desarrollo aislada. Con dos sesiones autorizadas de prueba, abrir Identificación general y entrar directamente a Usuarios que atiende. Comprobar casillas, menú abierto, bloqueo de la otra sesión, Tab dentro de la fila y liberación al salir. Recargar y comprobar selecciones y prioridad. Dejar usuarios vacío y revisar el pendiente en Acuerdos. No enviar formatos institucionales para esta comprobación.

## Evidencia y límites

Compilación de la solución sin errores ni advertencias; TypeScript/Vite correctos, 64 pruebas frontend y 62 de dominio/transporte aprobadas. [Regresión HTTP/PostgreSQL](usuarios-atendidos-20261007/postgresql.json): 146 casos, incluidos validación estricta, envío rechazado por lista vacía y aceptado con opciones confirmadas. [Regresión del formato en Edge](usuarios-atendidos-20261007/regresion-formato.json): 32 casos, incluyendo eliminación directa, reservas simultáneas, foco, propuestas, revocación y enviados.

[Migraciones en PostgreSQL](usuarios-atendidos-20261007/ef.json): 41 comprobaciones, incluidas limpieza limitada, rollback por fallo de auditoría, instantánea enviada y borrador de otro ejercicio intactos, prioridades 4 y 17 conservadas, versiones y repetición sin pérdida de selecciones.

[Navegador](usuarios-atendidos-20261007/navegador.json): seis escenarios del nuevo control, con dos sesiones independientes, casillas, selección/deselección, autoguardado, recarga, bloqueo/liberación en vivo, teclado, foco, scroll del menú, prioridad sin opción indicativa, viewport de 360 px y movimiento reducido. Las pruebas esperan la revalidación inicial antes de interactuar; las corridas preliminares que actuaban durante esa carga no se contabilizan como aprobadas.

Capturas sintéticas revisadas: [escritorio de 1366 px](usuarios-atendidos-20261007/usuarios-desktop.png) y [móvil táctil emulado de 360 px](usuarios-atendidos-20261007/usuarios-mobile.png). La revisión en dispositivo físico y lector de pantalla permanece pendiente. No se modificaron Nexo, Herd, credenciales ni bases institucionales. Sin push ni despliegue.
