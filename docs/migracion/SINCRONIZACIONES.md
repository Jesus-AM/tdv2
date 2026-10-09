# Contratos y operación de sincronizaciones

## Actualización 2026-10-09: solicitudes manuales atendidas por ASP.NET

La solicitud expresa de procesamiento manual desde el sitio sustituye la restricción anterior que exigía iniciar un procesador separado para toda sincronización. **La programación automática permanece bajo control explícito y no se activa por este cambio.** No hay migración nueva.

`ManualSyncWorker` es un `BackgroundService` del host: consulta la cola cada dos segundos y se despierta tras el commit de una solicitud HTTP. Usa `SyncCoordinator.Tick(manualOnly: true)`, las mismas reservas, comprobaciones de propietario/caducidad, publicaciones y auditoría del CLI. No hay `Task.Run` en el endpoint ni tareas desligadas del host. La señal en memoria sólo reduce la espera; el trabajo vive en PostgreSQL. El ciclo manual no crea ejecuciones programadas, no cambia `activa/proxima_en` y no sustituye el latido del procesador de horarios.

El POST sigue respondiendo **202** al aceptar, sin esperar la descarga. La interfaz muestra **Iniciando…** hasta leer el estado confirmado, **Sincronizando…** al recibir `ejecutando` y el resultado/contadores reales por UR, módulos SII e ILDA al terminar. Consulta cada segundo durante un trabajo y al volver a una pestaña visible; conserva la programación que se esté editando. La referencia local de solicitud bloquea dobles clics y, si se pierde el ACK, exige consultar el estado antes de permitir otro envío. PostgreSQL serializa solicitudes de todos los administradores: el conflicto 409 identifica la ejecución activa para mostrarla sin crear otra.

Si el servicio no está disponible o falla el acceso a la cola, se muestra un diagnóstico sanitizado y el POST rechaza nuevos intentos con 503. Las solicitudes previamente aceptadas siguen guardadas y el servicio reintenta consultar **la misma cola** cada dos segundos. Una espera de inicio superior a 30 segundos muestra un aviso específico, sin anunciar ejecución. Corregido el fallo local, se procesa el trabajo pendiente sin duplicarlo. Si falla una fuente, se conserva su réplica anterior y el historial muestra fallo o parcial; se permite solicitar un nuevo trabajo una vez terminado el anterior. No se registran excepciones SQL completas ni secretos.

Cerrar o navegar fuera de la página no cancela el trabajo. Al reiniciar ASP.NET se recuperan pendientes aunque no exista una señal en memoria. Una ejecución interrumpida conserva su reserva de 30 minutos y sus publicaciones confirmadas; tras caducar se registra fallida/parcial y se habilita un nuevo intento explícito. No se repite automáticamente una descarga interrumpida ni se acorta la reserva para eludir un procesador externo. Dos procesos web o web + CLI compiten por la misma reserva PostgreSQL.

**Operación:** mantener la aplicación iniciada y pulsar el botón de la fuente. No hace falta `--sync-once` ni `--sync-worker` para la captura manual. Windows/F5 y Ubuntu con `dotnet tdv2.dll` incluyen el servicio; no hay nuevas variables, secretos ni servicio adicional. Las conexiones existentes deben estar disponibles al proceso web. Los horarios habilitados siguen necesitando el procesador externo habitual; su advertencia se muestra por separado. [Windows](../ARRANQUE_LOCAL_WINDOWS.md), [publicación y supervisor Ubuntu](../PUBLICACION.md).

**Verificación vigente:** 44/44 casos PostgreSQL, incluidos nueve `Sync/web:`, y 6/6 recorridos `sync-manual-native.mjs` con Edge → ASP.NET → PostgreSQL y el hosted service real; 13/13 recorridos de regresión con ciclos explícitos. Las fuentes SQL Server/MySQL se sustituyen por lectores ADO.NET sintéticos; las pruebas de ACK retrasan o interrumpen únicamente la respuesta HTTP real. Resultados, comandos y pendientes se registran en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). [Escritorio](sincronizacion-manual-20261009/escritorio.png), [móvil emulado](sincronizacion-manual-20261009/movil.png). No acreditan conectividad institucional ni operación física en Ubuntu.

## Actualización 2026-10-09: módulos de SIIv2

Diagnóstico posterior de esquema: `SiiCatalogSchema` comprueba `public.sii_modulos` y, si falta, el historial EF y la existencia en otro esquema. La pantalla administrativa muestra un error útil y «No disponible», sin confundirlo con cero módulos. El coordinador registra la etapa fallida antes de consultar su fuente cuando falta el catálogo local. Las consultas especializadas usan `public.sii_modulos`; EF y la migración `20261009080533_SiiModulesCatalog` ya coinciden en ese esquema. No requiere nueva migración ni modifica una anterior.

El error `42P01` se reprodujo en PostgreSQL sintético deteniendo EF antes de esa migración y se corrigió aplicándola con EF. También se comprueban nombre/esquema distintos e historial incoherente. Para una instalación existente debe verificarse primero que `ConnectionStrings:Tdv2` sea la base prevista: el código no puede inferir un destino diferente sólo por ese error. No se consultó SQL Server ni una base institucional para este diagnóstico. [Comandos exactos de instalación posterior](SISTEMAS_HERRAMIENTAS.md#migración-y-operación-posterior).

SII incluye ahora **Unidades responsables** y **Módulos de SIIv2**, tanto manual como automáticamente, mediante el mismo coordinador/cola/reserva y `ConnectionStrings:Sii` (DesarrolloSII). La etapa `sii_modulos` ejecuta sólo `SELECT [ID_MODULO],[DESC_MODULO] FROM [sii].[MODULOS_SII]`, sin TOP ni filtros. Ejercicio afecta sólo a UR. ILDA continúa siendo independiente y no se activa programación automáticamente.

Cada catálogo tiene publicación, metadatos, auditoría y resultado transaccionales. El historial distingue fallos/parciales. La réplica `sii_modulos` conserva IDs y bajas locales, no modifica respuestas y sólo se consulta desde un formulario autorizado. La cuenta institucional requiere **SELECT sobre `sii.MODULOS_SII`**; no se concedieron permisos ni se cambió la fuente. Nueva migración: **`20261009080533_SiiModulesCatalog`**. [Contrato completo, migración, ejecución, pruebas y pendientes](SISTEMAS_HERRAMIENTAS.md).

Las referencias posteriores a SII exclusivamente como UR y a la pantalla original describen el alcance previo, ampliado por esta revisión. La suite actual pasó **34/34 casos sobre PostgreSQL 18.6 desechable**, incluidos 1501 módulos, repetición, fallos/rollback, manual/automática e independencia de ILDA (`native-postgres-20261009-084200-2373f124`). Fuentes institucionales simuladas; ningún servicio real consultado.

La pantalla pasó **12/12 recorridos Edge con ASP.NET/PostgreSQL**, incluidos parcial por módulos, automática con/sin ILDA, revocación, recuperación y adaptación móvil. [Reporte](sistemas-herramientas-20261009/sincronizaciones-navegador.json); cero errores JavaScript. Nueva migración verificada dentro de las **52/52 comprobaciones EF** de esta entrega.

Revisión previa, 2026-10-01: InstitutionalSource, InstitutionalSync, IldaSource, IldaSync, IldaInventory, SyncManager, SyncController, comandos, rutas/console, configuración institucional/ilda y migraciones 2026_09_25_000001 / 2026_09_30_000001 de Herd, exclusivamente lectura. Pruebas revisadas: InstitutionalSourceTest, InstitutionalSyncTest, IldaInventoryTest y ConfigurationSyncTest. No se ejecutó Laravel ni se consultaron credenciales/bases existentes.

## Contrato conservado

- SII: sólo poa.UNIDADES_RESPONSABLES_POA, diez columnas explícitas, READ COMMITTED sin NOLOCK, último ejercicio mediante MAX(EJERCICIO). Ejercicio único, descarga no vacía, IDs únicos, jerarquía sin ciclos. Empleado textual, espacios y ceros conservados. Límite 100000; descenso superior al 20% rechazado. Bajas lógicas, nunca borrar formatos ni modificar sus respuestas/versiones.
- ILDA: SELECT completo de ilda_db.informacion_area ordenado por id; sin filtros por UR/estado. Todas las columnas/filas, nulos y vacíos en datos JSON; índices locales id_origen/ur2/informacion_generada. Normalizar sólo ur2 auxiliar con trim, conservar original en datos. ID numérico textual hasta 40 dígitos; límite 100000 filas/128 MiB y reducción máxima 20%.
- Formatos: lectura sólo local; igualdad exacta entre ur2 auxiliar y cve_ur normalizada. Hasta 200 filas incorporadas al formulario, sin limitar la réplica. Fuente ILDA y tramite=informacion_generada. No sustituir filas ya guardadas; incorporar sólo IDs nuevos. Bajas/cambios externos no borran respuestas. GET no persiste formularios.
- Manual: sii/ilda/ambas, HTTP 202, exclusión mediante configuración bloqueada en PostgreSQL. Automática: SII siempre, ILDA optativa, intervalos 15/30/60/180/360/1440, hora diaria y America/Ciudad_Juarez por defecto. Configuración versionada; Laravel acepta zonas IANA válidas. No acumula intervalos vencidos.
- Procesador externo cada minuto, también con automática pausada; servicio web cada dos segundos exclusivamente para la cola manual. Reserva persistente de 30 minutos, propietario independiente, renovación entre etapas. Caducada: resultado interrumpido fallido/parcial según catálogos ya publicados, liberar para nueva solicitud; no reintentar silenciosamente una lectura interrumpida. Trabajador obsoleto no publica, termina ni libera reserva ajena.
- Cada publicación, resultado por fuente y auditoría comparten transacción. Si una fuente falla, conserva copia anterior y permite procesar la otra; estados pendiente/ejecutando/completada/parcial/fallida. Historial 50; procesador reciente si actividad dentro de 3 minutos.
- Sólo administrador real con Configuración y módulo hijo Sincronizaciones vigente en Nexo. Vista/representación bloquean GET y mutaciones; CSRF, contexto y límites por endpoint.

## Implementación y diferencias explícitas

- `tdv2/Synchronization` separa descarga, validación, publicación y coordinación persistente. `ConfigurationController` mantiene los endpoints y props; el POST despierta el servicio hospedado después de confirmar la cola. `LocalCatalog` se usa en inicio/formato sin resolver ni llamar al conector remoto. `Sincronizaciones.tsx` conserva el diseño y muestra progreso confirmado, diagnóstico y resultados por catálogo.
- SQL Server usa Microsoft.Data.SqlClient 7.1.0; MySQL usa MySqlConnector 2.6.2. Npgsql sigue en 10.0.0. Los paquetes se restauran dentro de `.artifacts/nuget` mediante `Directory.Build.props`.
- Nexo continúa como autoridad, incluida la identificación textual por empleado. SII no consulta personas ni tablas adicionales. Laravel permitía cambiar la tabla mediante SII_TABLA_UR; ASP.NET la fija a `poa.UNIDADES_RESPONSABLES_POA`, conforme a este encargo. No se introduce selector arbitrario de ejercicio en React: se conserva MAX(EJERCICIO) por defecto. La opción de servidor para omitir MAX sólo admite un ejercicio en la descarga; ejercicios mezclados se rechazan.
- `tdv2/Migrations/InitialTdv2` representa las cinco tablas del bloque con sus nombres/columnas, UUID e índices. `InitializePausedSynchronization` crea sólo la fila pausada. EF es el único mecanismo vigente; `004_synchronizations.sql` se conserva como fixture histórico. La migración inicial exige una base vacía, no convierte Laravel. No se ejecuta DDL al arrancar.
- Marcas de tiempo locales `timestamp` se escriben explícitamente en UTC; props HTTP incluyen desplazamiento. NodaTime 3.3.3 incorpora la base IANA: `America/Ciudad_Juarez` no estaba disponible mediante TimeZoneInfo en este Windows. Frecuencia diaria usa calendario local; hora inexistente durante cambio estacional se desplaza por el salto, hora ambigua toma la primera ocurrencia. Intervalos menores a un día son tiempo transcurrido desde el ciclo actual. No se reutiliza la zona del sistema operativo.
- JSON `datos` guarda todos los nombres y valores, sin trim del original. Los auxiliares no sustituyen el JSON. Decimales MySQL se conservan como texto exacto para evitar pérdida de precisión; binarios como objeto reversible `{ "$binary_base64": "..." }`; fechas/horas del proveedor como texto y fechas CLR como `yyyy-MM-dd HH:mm:ss.ffffff`. No se replica DDL, índices ni tipos SQL de MySQL, igual que la copia JSON Laravel. Esta conversión explícita de tipos debe contrastarse con los tipos y valores reales de ILDA; los tipos no admitidos abortan toda la publicación, nunca se omiten silenciosamente columnas.
- Protección adicional: se vuelve a comprobar propietario y vencimiento con reloj PostgreSQL justo antes de confirmar la publicación, además de al entrar en la transacción. Si SII cambia entre calcular permisos y bloquear el guardado de un formato, éste devuelve 409 sin escribir y exige nueva lectura/autorización.
- El proceso web atiende solicitudes manuales ya persistidas. `--sync-worker` sustituye `schedule:work`/cron para los horarios y también puede reclamar la cola manual; `--sync-once` ejecuta un ciclo; `--sync-check=...` comprueba descarga/reglas/reducción sin publicaciones ni cola. No se añadieron comandos que omitan validaciones o publiquen fuera de una reserva vigente.
- Las auditorías de solicitud/configuración/publicación/fallo/interrupción usan actor real o `sistema`, ID de ejecución, fuente, resumen y resultado. Se excluyen excepciones completas, cadenas SQL de conexión, tokens y credenciales. Cada cambio local y su auditoría se confirman juntos; un fallo de auditoría impide publicar.

## Configuración del servidor

Los nombres siguientes son variables de entorno .NET, provistas por el operador mediante su mecanismo de secretos; no guardar valores reales en repositorio, argumentos de consola, pruebas ni documentación. No se tomó configuración de Herd.

| Variable | Finalidad / valor por defecto |
|---|---|
| `ConnectionStrings__Tdv2` | PostgreSQL ASP.NET; base preparada explícitamente, permisos locales de lectura/escritura del catálogo, cola y bitácora |
| `ConnectionStrings__Sii` | SQL Server; credencial con SELECT sobre `poa.UNIDADES_RESPONSABLES_POA` y `sii.MODULOS_SII` (sólo ID_MODULO/DESC_MODULO) |
| `ConnectionStrings__Ilda` | MySQL; credencial con SELECT únicamente sobre `ilda_db.informacion_area`; base forzada a ilda_db |
| `Synchronization__IldaEnabled` | `false`; habilitar explícitamente para descargas ILDA. No impide consultar la copia local |
| `Synchronization__SiiLatestExercise` | `true`; selecciona MAX(EJERCICIO) |
| `Synchronization__MaxRows` | `100000`; admite reducir el límite, no elevarlo |
| `Synchronization__IldaMaxBytes` | `134217728`; admite reducirlo, no elevarlo |
| `Synchronization__CommandTimeoutSeconds` | `120`; rango 1–1200, inferior a la reserva de 30 minutos |

La conexión Nexo y Microsoft del sitio siguen documentadas en [AUTENTICACION_POSTGRESQL.md](AUTENTICACION_POSTGRESQL.md). Los trabajadores consumen la solicitud autorizada ya persistida y no requieren una identidad Microsoft interactiva. Usar cuentas de fuente de sólo lectura y TLS validado; `ApplicationIntent=ReadOnly` no sustituye los permisos SELECT del servidor. No se activan bases de origen por instalar paquetes o abrir páginas.

## Operación en Windows y Ubuntu

Para solicitudes manuales basta con la aplicación web habitual. Los comandos CLI siguientes son alternativas externas y no registran tareas, cron ni servicios. Web y CLI comparten conexión TDV2 y opciones de origen, y pueden convivir gracias a la reserva persistente. Construir desde la raíz del destino con .NET 10 y Node/npm:

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -o .artifacts/publish
```

Windows, desde la raíz del destino; ejecutar bajo la cuenta que recibe las variables de entorno anteriores:

```powershell
Set-Location .artifacts/publish
# Sólo comprobar fuentes, sin actualizar catálogos ni crear ejecuciones:
dotnet tdv2.dll --sync-check=sii
dotnet tdv2.dll --sync-check=ilda
dotnet tdv2.dll --sync-check=ambas
# Un ciclo: actualiza estado, recupera reservas vencidas y procesa cola/automática:
dotnet tdv2.dll --sync-once
# Proceso continuo en primer plano; finalizar con Ctrl+C:
dotnet tdv2.dll --sync-worker
```

Ubuntu, copiar el resultado de publish al directorio de despliegue y entrar en él. Requiere runtime ASP.NET Core 10 instalado por el operador y variables de entorno seguras en esa cuenta:

```bash
cd /ruta/al/despliegue/tdv2
dotnet tdv2.dll --sync-check=ambas
dotnet tdv2.dll --sync-once
dotnet tdv2.dll --sync-worker
```

No ejecutar `--sync-once` y `--sync-worker` como pasos obligatorios consecutivos: son alternativas operativas. `--sync-check` es diagnóstico previo optativo. El directorio de trabajo debe ser el de publicación para resolver Contracts/configuración. Salida 0: ciclo sin pendientes o completado; 1: parcial/fallido/error de acceso local; 2: argumento inválido de comprobación. El daemon continúa ante errores seguros y espera un minuto tras cada ciclo. Publica un latido independiente cada minuto incluso durante lecturas largas; **el latido nunca renueva la reserva**. Ctrl+C cancela en Windows/Linux; SIGTERM se atiende en Linux. No se ejecutaron estos comandos contra servicios institucionales ni se probó Ubuntu en esta sesión.

Si se despliega el CLI bajo un supervisor, debe mantener directorio, variables y usuario explícitos y relanzar el mismo comando. La aplicación web ya incluye el consumidor manual; el servicio externo sigue siendo necesario para horarios habilitados. No se instaló ni habilitó ningún servicio o tarea. La página informa el último latido externo (reciente durante tres minutos) y advierte de su ausencia sólo si la programación automática está activa. El diagnóstico del servicio manual aparece por separado.

## Interrupciones, resultados y recuperación

1. La cola vive en PostgreSQL. Encolar devuelve 202 sin descargar SII/MySQL dentro de la petición; el servicio puede comenzar en paralelo antes de que llegue ese ACK al navegador. La fila de configuración serializa solicitudes, reclamaciones y publicaciones; otra ejecución recibe 409.
2. Cada catálogo se descarga/valida completo fuera de su transacción local. Publicar marca bajas lógicas, actualiza datos, metadatos, resultado de esa fuente y auditoría en un único commit. SII e ILDA son publicaciones independientes: un fallo produce **parcial** si la otra fuente se confirmó. Ninguna alerta o mensaje lo convierte en éxito completo.
3. Si termina el trabajador, la ejecución queda persistida. Después de los 30 minutos de reserva, el siguiente ciclo marca interrupción fallida/parcial según lo ya confirmado y libera la cola. Los catálogos confirmados permanecen; los no confirmados se revierten. No hay reintento oculto de la ejecución anterior. Solicitar un nuevo trabajo tras revisar el origen; una automática ya vencida puede crear su siguiente ejecución.
4. No borrar a mano propietario/reserva ni detener servicios ajenos para recuperar. Un trabajador antiguo, aunque retome la ejecución, no puede publicar ni liberar la reserva de otro. Dos trabajadores pueden estar vivos: sólo uno descarga para una ejecución reclamada.
5. Error local al registrar incluso el fallo puede dejar el trabajo activo hasta caducidad: conserva consistencia y el siguiente ciclo lo recupera cuando la bitácora/base vuelva a funcionar. Caída de red durante COMMIT puede dejar incierto el resultado del cliente; consultar la ejecución/metadatos al recuperar. Las pruebas no certifican pérdida física de motor ni de confirmación de COMMIT.

## Verificación reproducible y límites

Desde el destino, con Node/Edge y PostgreSQL nativo ya disponibles:

```powershell
dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SyncOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

Construir React antes del navegador (`npm.cmd run build` en ClientApp). `-SyncOnly` aísla los casos del bloque sin navegador; `-SkipBrowser` incluye todos los HTTP/SQL; `-BrowserOnly` conserva los tres recorridos de scripts de navegador. Cada llamada crea un clúster nuevo dentro de `.artifacts`, puerto loopback aleatorio, contraseñas sintéticas no impresas y validación de `data_directory`; lo detiene en finally. Nunca usa PostgreSQL de Herd ni las bases existentes. El caso de interrupción termina exclusivamente un proceso hijo de prueba identificado y luego adelanta la caducidad en esa base aislada; no espera 30 minutos reales.

Se distinguen tres niveles de evidencia:

- **Reglas y almacenamiento:** PostgreSQL nativo real, transacciones, bloqueos, expiración antes/durante publicación, rechazo de trabajador obsoleto, proceso hijo interrumpido, reducción/duplicados/lectura parcial, concurrencia con guardados, rollback de catálogo y auditoría, permisos/CSRF/representación y flujos React completos.
- **Conectores con lectores simulados:** `CatalogSource` productivo ejecuta sus consultas fijas y mapea valores, pero `ISourceConnections` se sustituye por conexiones/lectores ADO.NET sintéticos. Se valida el SQL emitido, límites y errores parciales; **no** se valida protocolo TDS/MySQL, negociación TLS, autenticación, compatibilidad de servidor ni todos los tipos nativos del proveedor. El navegador bloquea toda red externa, incluidas fuentes tipográficas.
- **Institucional:** no ejecutado. Faltan conexiones de prueba autorizadas y cuentas SELECT para SII/ILDA, datos/tipos representativos, certificado TLS y publicación Nexo/Microsoft aislada. Pendientes volumen/rendimiento real, selección efectiva de ejercicio, precisión/formato de tipos ILDA, equivalencia ur2/claves, empleados Nexo, permisos institucionales, revocaciones y aceptación por responsables. Windows sí tiene evidencia sintética; Ubuntu, SIGTERM, supervisor/operación continua y corte requieren su propio ensayo.

Resultados: suite completa **91/91 grupos** (90 HTTP/SQL, incluidos 30 del bloque, y un grupo con 31 recorridos React). Tras ajustar la conversión SII de booleanos a texto 1/0 como Laravel, **30/30** casos del bloque repetidos. Cero errores JavaScript. Publicación Release compilada y CLI de comprobación con argumento inválido devuelve 2 sin abrir fuentes. La operación continua y SIGTERM en Ubuntu no se probaron.

Informes: [PostgreSQL](evidencia-sincronizaciones-postgresql.json), [10 recorridos de sincronización](evidencia-sincronizaciones-navegador.json), [ajuste final del conector](evidencia-sincronizaciones-ajuste-conector.json). Regresión, comandos y artefactos concretos en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). No llamar validada a la conectividad institucional por compilar ni por pasar estas pruebas sintéticas.
