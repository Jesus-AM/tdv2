# Identificación general: tabla, destinatarios y eliminación

Entrega local del 8 de octubre de 2026. Se conserva el trabajo anterior: primera etapa, fotos, permisos, reservas automáticas, captura incremental, exclusiones ILDA y ausencia de Resolver/comparaciones. No cambia participación de UR, Nexo ni autenticación.

## Presentación y captura

Actualización vigente 2026-10-09: se añade **Edición** como primera columna estrecha y fija durante el desplazamiento horizontal. La siguiente es **Código**, con el origen debajo; ya no se llama Fuente. Acciones conserva únicamente Eliminar, alineado arriba y con área de 44 × 44 px. Los campos muestran dos a cuatro líneas y el resto se recorre internamente, también en consulta. Trámite y ¿Qué entrega? tienen un mínimo de 280 px. La tabla usa desplazamiento horizontal y la página vertical, sin límite de 65vh. Desplazar conserva reserva, texto y selección. [Causa, pruebas y contrato actual](DESPLAZAMIENTO_TABLAS.md). Esta presentación sustituye las ubicaciones y alturas históricas descritas a continuación, conservando la tipografía compacta.

Identificación continúa como tabla de ocho columnas: Fuente, Trámite o servicio, ¿A quién atiende?, ¿Qué entrega?, Área responsable, Validación, Prioridad y Acciones. Código y fuente comparten la primera columna. Por instrucción posterior se recuperaron los tamaños anteriores: captura a 13 px (`capture-scroll`), encabezados a 12 px del tema con peso 700, cuerpo/instrucciones `body2`, ayudas y opciones de destinatarios a 14 px. Celdas y botones utilizan nuevamente el espaciado/tamaño pequeño del tema, con mínimos táctiles móviles existentes. Se retiraron el ancho fijo de 2045 px y las columnas ampliadas; se recuperaron los mínimos anteriores de los componentes, conservando texto multilínea, contraste, foco visible y scroll dentro de la tabla. No se modificó el tema global ni se restauraron archivos completos.

Las capturas y comprobaciones de 16 px más abajo corresponden a la entrega anterior; la revisión compacta posterior se registra en `ESTADO_MIGRACION.md`. No hay una migración nueva por este ajuste visual.

`IdentificationInstructions` contiene las instrucciones solicitadas como contenido normal. Las ayudas conservan apertura accesible, Escape y retorno de foco sin pedir o liberar reservas. Destinatarios usa el ejemplo **Estudiantes · Docentes** y explica las tres categorías externas. Prioridad conserva valores, colores y etiquetas cortas; su ayuda no incluye Ejemplo.

Las opciones vigentes de `usuario` son exactamente Comunidad universitaria, Docentes, Estudiantes, Personal administrativo, Público en general, Instituciones públicas externas, Empresas y organizaciones privadas. Se conservan los valores de las cuatro opciones anteriores que siguen vigentes. Es una colección sin duplicados; vacío se guarda como borrador y queda pendiente. No hay captura de `usuarioOtro`. Enviados e históricos muestran sus valores originales y el detalle anterior como texto de consulta, nunca como control editable.

## Causa comprobada y corrección del retiro

[La reproducción previa](identificacion-20261008/reproduccion-previa.json) guardó un retiro y entregó después la renovación con la misma intención de eliminación: el servidor devolvió el mensaje exacto «El registro ya no está disponible. Revisa el contenido vigente». En el código, `commitChange` renovaba también cuando su cambio era borrar una fila; enviaba `removal` y competía con PATCH. `Reserve(renew: true)` volvía a calcular `FormRemoval.Changes` antes de validar la reserva, buscando una fila que el PATCH ya había retirado.

Ahora `prepareRemoval` espera la actividad anterior en vuelo; el retiro no genera una nueva renovación. Tras el ACK se eliminan foco, autorización local, actividad y estado del bloque retirado. Los callbacks atrasados no modifican una versión/testigo posterior ni restauran contenido. El servidor libera también el testigo de todo bloque eliminado dentro del mismo commit. La cascada de relaciones, auditoría, versiones, recibos y exclusión ILDA siguen siendo atómicos.

Una renovación antigua o nueva adquisición de una fila retirada recibe **409 `registro_eliminado`** sólo cuando existe un recibo persistido que acredita ese retiro. Un ID desconocido recibe **404 `registro_desconocido`**; autorización, sección y estado editable se comprueban antes. Son respuestas HTTP controladas, sin lanzar `DomainProblem` en la carrera esperada ni convertir todos los 409 en éxito. El PATCH idéntico mantiene su recibo idempotente. No se cambió el depurador ni se ocultaron fallos de SQL o autorización.

También se corrigió la reaplicación de una lectura inicial ya consumida después de un 422, que podía retirar una reserva adquirida en el mismo milisegundo. Se sigue reteniendo el estado remoto más reciente cuando hay texto/recibos pendientes; la regresión de respuesta perdida confirma que un ACK antiguo no sobrescribe cambios posteriores.

## Migración de datos

**`20261009052502_CurrentRecipients`**, nueva migración EF Core sin DDL de aplicación. Probada sólo en PostgreSQL desechable. No se modificaron migraciones anteriores.

- Filtra valores JSON exactos `Otro`/`Externo`; no usa coincidencias parciales, traducciones ni categorías sustitutas. Retira la propiedad `usuarioOtro` y conserva las demás selecciones, campos y filas.
- Sólo formatos no enviados del ejercicio vigente (o ejercicio cero compatible), con catálogo de UR disponible. Los ejercicios anteriores y los enviados/instantáneas permanecen idénticos.
- Transacción con bloqueos breves de tablas durante la instalación; aumenta versión de formato y de las filas cambiadas, invalida sólo sus reservas, recalcula avance total y registra auditoría sin copiar el texto eliminado. La primera etapa se calcula al consultar con las nuevas reglas.
- El historial EF impide repetir la limpieza. Ningún GET, arranque o guardado ejecuta esta transformación. Las peticiones con versión antigua fallan; incluso con reserva/versiones nuevas, las opciones retiradas o `usuarioOtro` se rechazan con validación localizada 422.
- La lectura de un ejercicio histórico tampoco mezcla inventario actual ni ofrece captura; aplica el bloqueo de ejercicio que ya exigía el servidor al escribir.

Desde la raíz, con la aplicación detenida para la actualización y después de comprobar destino/historial:

```powershell
dotnet tool restore
dotnet ef migrations list --project tdv2 -- --environment Development
dotnet ef database update 20261009052502_CurrentRecipients --project tdv2 -- --environment Development
```

El último comando utiliza `ConnectionStrings:Tdv2` de la configuración habitual. **No se ejecutó contra bases institucionales.** No iniciar esta versión contra un esquema anterior. No hay restauración automática de categorías eliminadas al revertir; la migración no guarda copias de compatibilidad. No requiere configurar Nexo.

## Verificación y límites

- TypeScript, Vite y solución Release finales correctos; .NET con cero errores/advertencias. La primera compilación emitió NU1900 porque el entorno no pudo consultar la auditoría de NuGet; no fue un error de compilación.
- [78/78 frontend](identificacion-20261008/frontend.txt), incluidas reservas, respuesta perdida, pendientes independientes, fotos y dos regresiones nuevas de retiro/renovación. 62/62 dominio/transporte en memoria.
- [20/20 HTTP/PostgreSQL](identificacion-20261008/postgresql.json) y [2/2 ampliaciones](identificacion-20261008/historicos-revocacion.json): 21 escenarios distintos, uno repetido/reforzado. Incluyen permisos/revocación, alias, etapa, destinatarios, conservación de ocultos/enviados/históricos, dos sesiones, reserva/versiones, auditoría, cascada ILDA, sincronizador sintético y recibos.
- [50/50 comprobaciones EF](identificacion-20261008/migraciones.json): combinaciones Otro/Externo/ambos/Docentes+Otro/Estudiantes+Externo, nuevas categorías intactas, sólo campo autorizado, versiones/reservas, avance, rollback por fallo de auditoría, enviados/históricos y repetición de comando/script sin nueva limpieza.
- [16/16 recorridos Edge](identificacion-20261008/navegador.json), cero errores JavaScript: dos sesiones/pestañas, primera escritura, Tab, selectores, siete opciones, recarga, ayudas, ocupación/liberación, eliminación directa/Cancelar, renovación en vuelo sin nueva actividad de retiro, ILDA e históricos/enviados en consulta. React/ASP.NET/PostgreSQL reales; servicios institucionales sustituidos exclusivamente en el proyecto de pruebas.
- Capturas inspeccionadas: [1920](identificacion-20261008/tabla-1920.png), [1366](identificacion-20261008/tabla-1366.png), [768](identificacion-20261008/tabla-768.png), [360](identificacion-20261008/tabla-360.png), [viewport equivalente al 200 %](identificacion-20261008/tabla-viewport-200.png). Se verificaron letra/peso, altura de acción, textos largos completos, menús táctiles y ausencia de desbordamiento de página.

**Pendiente manual:** Edge headless no aplicó el atajo de zoom nativo (`nativeZoom200=1`); la captura de 683 px es un viewport equivalente, no evidencia de zoom real al 200 %. Comprobar ese zoom desde Edge/Visual Studio, lector de pantalla y dispositivos físicos. La aceptación con identidades/servicios institucionales y la instalación posterior corresponden al operador. No hubo push, despliegue ni cambios de credenciales, Nexo, Herd o bases institucionales.

Para reproducir en Windows:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
$env:TDV2_TEST_CASE_PREFIX='Edición/etapa:|Edición/retiro renovación:|Edición/dos sesiones|Edición/bloques distintos|Edición/vencimiento|Edición/lectura pasiva|Edición/adquisición|Edición/reintento|Edición/revocación|Edición/operación y auditoría|Edición/reserva que vence|Edición/SignalR'
$env:TDV2_TEST_BROWSER_FLOW='first-stage'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly
Remove-Item Env:TDV2_TEST_CASE_PREFIX
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El runner comprueba loopback, puerto distinto de 5432, base `tdv2_native_test` y clúster nuevo dentro de `.artifacts`; lo detiene al terminar. F5 conserva **tdv2.slnx → TDV2 HTTPS + React** y la configuración local. Probar escrituras sólo con el host sintético: no cambiar User Secrets ni usar formatos institucionales para la comprobación.
