# Catálogo de herramientas y retirada de Medios utilizados — 2026-10-16

## Estado vigente

**Complemento visual 2026-10-16:** encabezados oscuros a peso 700, separación del resumen/búsqueda, colores de presencia diferenciados y Acciones fija. Se conservan íntegramente las reglas de este documento. La migración aquí descrita no se modificó ni ejecutó durante ese ajuste. [Implementación, capturas y pruebas visuales](AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

Se implementa sobre el árbol local con cambios pendientes de agrupación, herramientas opcionales, textos, scroll y reservas. No se restauraron archivos desde Git. El catálogo local de aplicaciones (207 web / 26 móviles), sus UUID, importador y referencias históricas permanecen. SQL Server sólo sincroniza UR y módulos; ILDA continúa independiente.

Esta entrega sustituye las reglas anteriores de catálogo, repetición de herramientas y conservación de Medios. Las evidencias fechadas anteriores no prueban esta versión. La existencia de archivos EF tampoco acredita su aplicación institucional. **No hubo push, despliegue ni acceso a bases institucionales.**

## Captura, compatibilidad y requisitos

El selector principal permite buscar y contiene exactamente estos grupos, cuyos encabezados no son opciones:

| Grupo | Opciones y claves persistidas |
|---|---|
| Sistemas institucionales | Sistema Integral de Información (`sii_v2`), Aplicaciones web (`aplicaciones_web`), Aplicaciones móviles (`aplicaciones_moviles`) |
| Productividad y colaboración | Microsoft 365 (`microsoft_365`) |
| Formularios y encuestas | LimeSurvey (`limesurvey`) |
| Análisis y visualización de datos | Power BI (`power_bi`), Tableau (`tableau`) |
| Diseño y producción multimedia | Adobe Creative Cloud (`adobe_creative_cloud`) |
| Al final, sin grupo adicional | Otra (escríbela) (`otra`) |

`sii_v2` no cambia de identidad. El dependiente se titula **Módulos del Sistema Integral de Información**. Se conservan `modulosSii` y las referencias singulares antiguas; `aplicacionesWeb` y `aplicacionesMoviles` continúan guardando objetos `{id, descripcion, catalogo}` confirmados desde el catálogo PostgreSQL local.

Las nuevas colecciones guardan claves de producto, no etiquetas ni identificadores compartidos con catálogos institucionales:

| Campo JSON en `sistemas[]` | Claves admitidas |
|---|---|
| `aplicacionesMicrosoft365` | `excel`, `word`, `powerpoint`, `outlook`, `teams`, `forms`, `onedrive`, `sharepoint` |
| `aplicacionesAdobe` | `acrobat_pro`, `acrobat_reader`, `adobe_express`, `adobe_scan`, `after_effects`, `animate`, `audition`, `bridge`, `character_animator`, `dreamweaver`, `fresco`, `illustrator`, `incopy`, `indesign`, `lightroom`, `lightroom_classic`, `media_encoder`, `photoshop`, `photoshop_express`, `premiere` |

Forms y Outlook son aplicaciones de Microsoft 365. Adobe se presenta alfabéticamente; registrar uso no acredita disponibilidad de licencias. LimeSurvey, Power BI y Tableau no añaden un campo con el mismo nombre. No hay opciones adicionales de IA o SPSS.

Todos los dependientes comparten búsqueda, casillas y menú abierto entre selecciones. Cerrados muestran indicación para seleccionar, nombre completo único o cantidad; sin chips ni IDs. Las etiquetas de dependientes están encima del control, con tamaño habitual y separación discreta, para que el nombre largo no se superponga. Se conserva la apertura mientras se obtiene la reserva; las opciones sólo pueden modificarse tras autorizarla. Las respuestas dormidas de cada familia se conservan al alternar y al guardar/recargar. El backend valida también las colecciones dormidas recibidas y no acepta claves de otra familia, duplicadas, desconocidas o con tipo incorrecto.

`excel`, `correo`, `formulario`, `papel` y `portal_cast` dejan de admitirse como selecciones nuevas. Las respuestas ya guardadas mantienen su etiqueta y clave y permiten cambiar otros campos. No se deduce que un formulario antiguo sea Forms o LimeSurvey ni se reclasifican respuestas automáticamente. Las claves históricas reconocidas siguen contando como respuesta de herramienta válida; los demás campos mantienen sus requisitos existentes.

Un procedimiento puede tener **cero herramientas**, sin pendientes ni penalización. Una fila real iniciada cuenta procedimiento, herramienta, uso y funcionamiento, más sus condicionales. Cualquier selector múltiple activo exige al menos una selección válida y suma **un** requisito. `FormRequirements` sigue siendo la evaluación compartida por avance, revisión y envío. Los borradores se proyectan con la regla vigente y guardan su avance durante una escritura normal; la migración no recalcula ningún porcentaje.

## Duplicados y concurrencia

La relación persistida sigue siendo `sistemas[].proceso` → código confirmado de Identificación; se resuelve al ID estable del procedimiento. Se compara ese ID y la clave principal, nunca nombres ni posición de filas. La propia fila queda excluida. Sin asociación válida no se forma un grupo común de duplicados.

La interfaz usa también las filas locales pendientes. Deshabilita opciones ya utilizadas y muestra **Ya agregado a este procedimiento**. La misma herramienta puede usarse en otro procedimiento. Reasignaciones conflictivas se deshabilitan con explicación; se conservan asociación y respuestas. La disponibilidad cambia al retirar, reasignar o cambiar una herramienta.

`otra` permanece disponible para varios nombres: el servidor compara nombres sin distinguir mayúsculas, tras recortar y colapsar espacios; conserva el texto escrito. No usa aproximación ni incorpora nombres al catálogo. Un conflicto de nombre devuelve 422 junto a `sistemaOtro`; el editor mantiene la propuesta para corregirla.

`SystemUniqueness.Validate` se ejecuta desde `FormSchema.Validate` después de combinar el parche con el documento vigente, dentro del bloqueo de UR de `FormEditingService.Save`. Dos conexiones competidoras se serializan antes de leer/validar. No se agrega un índice que impida leer duplicados históricos. Se comparan las parejas de IDs: una pareja histórica del mismo procedimiento/herramienta puede permanecer; ninguna pareja nueva puede aparecer. Esto permite editar campos ajenos, reducir duplicados o retirarlos, sin fusionar ni borrar respuestas y sin reemplazar un duplicado por otra fila para eludir la regla.

## Limpieza exacta y transaccional

Migración necesaria: **`20261016190000_RemoveUsedMedia`**, posterior a `20261015165555_LocalApplicationLists`. Es una migración EF de datos; no cambia el modelo, columnas, relaciones ni tablas. El historial EF controla su ejecución única. Su `Up` y un script idempotente generado por EF se han probado en aislamiento.

| Ubicación | Retirada exclusiva |
|---|---|
| `public.formatos_ur.contenido` (JSON) | Propiedades raíz `medios` y `medioOtro`, en borradores y registros históricos/enviados |
| `public.formatos_ur.instantanea_envio` (JSONB) | `contenido.medios` / `contenido.medioOtro`; también propiedades raíz `medios` / `medioOtro` para documentos antiguos guardados directamente |
| `public.formato_envios_etapas.instantanea` (JSONB) | Las mismas rutas explícitas, sin reclasificar herramientas ni alterar el resto de la instantánea |
| `public.formato_bloques` | Sólo filas con `bloque = 'medios'`, incluida su reserva/versionado exclusivo |
| `public.formato_operaciones.respuesta` (JSONB) | `entrega.resumen.medios` y entradas de `bloques[]` cuya `key` es exactamente `medios` |

No se buscan/eliminan recursivamente palabras o propiedades homónimas en respuestas ajenas. **`sistemaOtro` permanece.** No se convierten medios en herramientas. No cambian IDs, orden de filas, códigos, asociaciones, autores, fechas, estado de envío, porcentajes, versiones del formato ni metadatos de operaciones. El resto de las reservas/bloques se conserva. La conversión JSON→JSONB→JSON puede cambiar espacios u orden textual de propiedades, sin cambiar sus valores ni el orden de arrays.

Se bloquean las cuatro tablas en modo exclusivo durante la transacción. Únicamente se suspenden los tres triggers conocidos de inmutabilidad para esta limpieza autorizada, y se reactivan antes de finalizar. Un fallo revierte tanto los datos como el estado de los triggers. Nunca se habilita esta excepción desde el sitio ni al arrancar. No se usan `session_replication_role`, superusuario ni disparadores de usuario deshabilitados globalmente. La cuenta operadora necesita ser propietaria de esas tablas (o su rol propietario autorizado).

El contrato, plantillas, `FormBlocks`, normalización frontend y revisión ya no incluyen Medios. Peticiones al bloque retirado se rechazan y un guardado normal no reintroduce propiedades raíz antiguas. Los recibos conservan identidad, huella y resultado de las demás filas; un reintento antiguo exclusivo del bloque retirado se rechaza por clave inválida. Cerrar las pestañas de la versión anterior durante la actualización.

Los registros de auditoría que sólo identifican el nombre del bloque de una operación pasada permanecen como metadatos históricos; no contienen las respuestas del apartado. Las migraciones anteriores y fixtures SQL se conservan como historia, sin convertirlos en un segundo mecanismo de actualización.

## Respaldo y recuperación

Antes de aplicar en cualquier destino real, el operador debe detener escritores/procesadores y guardar un **respaldo completo PostgreSQL consistente**, con protección de acceso y cifrado según su operación. Debe incluir las cuatro tablas anteriores, sus metadatos y `__EFMigrationsHistory`. Conservar también el paquete anterior. Verificar el respaldo restaurándolo en una base de recuperación separada; no basta con listar columnas. No guardar respaldos ni credenciales en Git.

La limpieza retira respuestas incluso de envíos históricos por instrucción expresa. **Down no las recupera**: rechaza retroceder si hay formatos. Sólo permite desmontar un esquema vacío en pruebas. No crea columnas vacías ni promete recrear texto eliminado.

Para recuperación completa, mantener el servicio detenido, conservar cualquier escritura posterior y restaurar el respaldo íntegro en un destino de recuperación validado; comprobar respuestas, envíos, historial y triggers antes de planear la sustitución. Si hubo capturas posteriores, no sobrescribirlas con un respaldo antiguo: restaurar aparte y preparar una recuperación selectiva revisada de las rutas retiradas, con transacción y protección de enviados. Esa recuperación necesita los valores reales del respaldo y una decisión operativa; no se automatiza con datos vacíos.

## Orden y comandos de actualización (operador, no ejecutados aquí)

1. Construir backend y frontend de esta misma revisión y comprobar pruebas. El paquete incluye el catálogo versionado de aplicaciones.
2. Verificar destino/rol/configuración; detener web, procesos y pestañas de la versión anterior; obtener y probar el respaldo.
3. Aplicar EF explícitamente hasta `RemoveUsedMedia`, con un solo operador. F5/arranque web nunca aplica migraciones.
4. Sólo si las listas aún no se importaron, comprobar e importar con el paquete nuevo conforme a [APLICACIONES_LISTAS.md](APLICACIONES_LISTAS.md). No volver a SQL Server para aplicaciones.
5. Instalar conjuntamente backend y assets frontend del paquete; iniciar con la configuración vigente. Comprobar captura, recarga y revisión. No activar sincronizaciones/horarios por esta actualización.

Windows, desde la raíz, .NET 10/EF y Node/npm disponibles:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-tool-catalog
# Servicio libpq y ruta privados, previamente verificados por el operador:
pg_dump --dbname="service=$env:TDV2_BACKUP_SERVICE" --format=custom --file="$env:TDV2_BACKUP_FILE"
pg_restore --list "$env:TDV2_BACKUP_FILE"
# Desarrollo local usa los User Secrets existentes. No imprimir ni copiar sus valores.
dotnet ef database update 20261016190000_RemoveUsedMedia --project tdv2 --startup-project tdv2 -- --environment Development
```

Ubuntu, desde un checkout de esta versión con SDK/EF y configuración externa privada del destino:

```bash
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-tool-catalog
pg_dump --dbname="service=$TDV2_BACKUP_SERVICE" --format=custom --file="$TDV2_BACKUP_FILE"
pg_restore --list "$TDV2_BACKUP_FILE"
dotnet ef database update 20261016190000_RemoveUsedMedia --project tdv2 --startup-project tdv2 -- --environment Production
```

`TDV2_BACKUP_SERVICE` y `TDV2_BACKUP_FILE` son parámetros del operador; libpq debe apuntar al mismo destino verificado que EF. No contienen valores predeterminados ni autorizan otros destinos. La restauración de prueba se hace **sólo en la base separada y vacía de recuperación**: `pg_restore --exit-on-error --no-owner --dbname="service=SERVICIO_RECUPERACION_VERIFICADO" ARCHIVO_RESPALDO`. El operador suministra nombres reales y permisos; estos comandos no se ejecutaron aquí. Si Ubuntu sólo tiene runtime, EF se ejecuta desde el equipo de operación con SDK/código/configuración del mismo destino.

## Archivos y comprobación reproducible

Backend: `SystemAnswers`, `SystemSuites` (nuevo), `SystemUniqueness` (nuevo), `FormSchema`, `FormBlocks`, `FormCapture`, `FormRequirements`, `FormStages`, etiquetas de `SystemModules/SystemApplications` y `Contracts/procesos_operativos.json`. Nueva migración de datos `RemoveUsedMedia`.

Frontend: `system-catalog.ts` y `ToolSelect.tsx` (nuevos), `SystemField`, `CatalogMultiSelect`, `ProcedureSelect`, `FormatoUR`, `FormSubmissionReview`, tipos y utilidades de bloques/normalización. Se conservan `system-groups` y `StableCaptureBody` del trabajo previo. Se actualizan ejemplos y etiquetas de sincronización sin cambiar sus fuentes.

Pruebas nuevas: `ToolCatalogTests.cs`, `RemoveUsedMediaMigrationTests.cs`, `tool-catalog.test.mjs`, `tool-catalog-native.mjs`. Las pruebas antiguas que usaban Medios como bloque auxiliar ahora usan bloques vigentes; no se retiraron sus comprobaciones de conflictos, foco o guardado.

```powershell
$env:TDV2_TEST_CASE_PREFIX='Edición/catálogo vigente:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -MigrationsOnly
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
$env:TDV2_TEST_BROWSER_FLOW='tool-catalog'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

Los ejecutores anteriores crean clústeres nuevos dentro de `.artifacts` y los detienen al terminar; no reciben conexiones institucionales. Ejecutarlos secuencialmente si cambian fuentes compiladas, para evitar que Windows bloquee el reemplazo del ensamblado de una prueba todavía activa. Los scripts nativos están preparados para Windows; los comandos de construcción/EF son multiplataforma. No se ejecutó Ubuntu, no se probaron dispositivos físicos ni lector de pantalla y no se acredita aceptación institucional.

## Resultados de esta entrega

| Verificación | Resultado y alcance |
|---|---|
| TypeScript, Vite y compilación Release de la solución | Correctos; dependencias existentes |
| Dominio/HTTP con dobles | **116/116**, sin confundir este transporte con PostgreSQL |
| Frontend sin navegador | **94/94**, incluido catálogo exacto, disponibilidad local, Otra y regresión del motor de autoguardado |
| ASP.NET → PostgreSQL 18 desechable | **36 casos distintos correctos**: 5 nuevos y 31 de regresión; familias, valores inválidos, recarga, envío, conflictos, históricos, permisos, módulos/listas y atomicidad |
| Migraciones EF sobre PostgreSQL desechable | **67 comprobaciones**, incluidas 8 de limpieza: borrador/enviado/global/etapa/histórico, rutas exactas, rollback, repetición, metadatos y triggers |
| Edge → ASP.NET → PostgreSQL | **17 recorridos**: 8 de catálogo y 9 de agrupación/textos; identidad y fuentes sintéticas, transporte y persistencia reales |
| Edge con transporte simulado | **9 recorridos**, de apertura inmediata y reservas lentas, denegadas/fallidas, teclado, toque emulado y cancelación de intenciones tardías |

La concurrencia de duplicados observa dos conexiones esperando un bloqueo PostgreSQL antes de liberar la transacción: una escritura confirma y la otra devuelve 422, sin segunda fila ni recibo exitoso. Las sesiones son independientes con identidad sintética; no se presentan como dos identidades institucionales reales. Navegador comprueba conservación del mismo nodo DOM, foco, selección de texto y scroll durante autoguardado. Se inspeccionaron capturas de 1366/390/320 px, con el scroll propio de la tabla y sin desbordamiento de página.

Evidencia local ignorada por Git:

- `.artifacts/tools-{frontend,vite}.txt`, `tools-build-final.txt`, `tools-unit-final.txt`.
- `.artifacts/native-postgres-20261016-192002-91c1510d/verification.json`: 5/5 nuevos.
- `.artifacts/native-postgres-20261016-192115-f15446bd/ef-migrations.json`: 67 comprobaciones EF.
- `.artifacts/native-postgres-20261016-192322-4619feec/verification.json`: regresión inicial 28/31; los tres casos ajustados pasaron en `native-postgres-20261016-192757-4ed73330/verification.json` (allí el ejecutor de navegador todavía falló al no esperar el alta).
- `.artifacts/native-postgres-20261016-192938-6081b239/tool-catalog-browser.json` y `tool-catalog-*.png`: 8/8 de catálogo.
- `.artifacts/native-postgres-20261016-193115-259221a8/system-groups-native-browser.json`: 9/9 de agrupación.
- `.artifacts/record-select-ui/report.json` y `tools-browser-mock.txt`: 9/9 con transporte simulado.

Se corrigió una frase residual de Medios detectada por el navegador. Se adaptaron fixtures que aún esperaban seleccionar Excel como opción nueva, repetir SII en el mismo procedimiento, capturar el bloque retirado o terminar en la migración anterior. La prueba de destinatarios usaba una herramienta histórica incompleta ajena a su objetivo; ahora prueba destinatarios con herramientas opcionales vacías. También se precisaron esperas del alta asíncrona y el localizador del botón de limpiar selecciones, ahora que el selector principal también es buscable. Ninguna expectativa de autorización ni de conservación se relajó.

Un intento de navegador no pudo recompilar por MSB3021/MSB3027 mientras otra suite usaba el ensamblado; se repitió secuencialmente. Los ejecutores PostgreSQL mostraron mensajes 87/3 de relanzamiento restringido de `initdb`, pero completaron la creación/ejecución y detuvieron sus clústeres. Las primeras compilaciones mostraron avisos NuGet preexistentes sobre Pkcs 9.0.18; no se cambiaron dependencias. Los resultados fallidos intermedios no se cuentan como aprobados.

**Pendiente real:** aplicación institucional de la nueva migración y las anteriores que allí falten, comprobación del respaldo/restauración de ese destino y despliegue por el operador, aceptación institucional, Ubuntu, dispositivos físicos y lector de pantalla. La implementación y las verificaciones locales descritas están terminadas. No se verificó el historial de ninguna base existente ni se modificaron Nexo, Herd, secretos o catálogos institucionales.
