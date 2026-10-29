# Auditoría del costo de autoguardado — 2026-10-29

## Alcance y método

Esta auditoría amplía [la medición anterior](AUTOGUARDADO_POSTGRES_Y_RECUPERACION_20261029.md). No cambia la política de captura ni elimina controles para perseguir un número de consultas. Todo se ejecuta en clústeres PostgreSQL desechables, validados por host loopback, puerto distinto del institucional, nombre y directorio dentro de `.artifacts`.

Se probaron 1, 10 y 25 personas sintéticas distintas, con sus propios identificadores Microsoft, empleados, membresías publicadas y cookies. Cada persona mantiene una sola solicitud en vuelo. Diez rondas por escenario producen **10, 100 y 250 observaciones**, respectivamente; P50 y P95 usan rango más cercano. Con 10 muestras el P95 es el máximo observado: no representa una estimación precisa del percentil de producción.

Componentes reales: ASP.NET Core/Kestrel, middleware, cookies cifradas y CSRF, comprobación de identidad, consultas de roles/módulos, EF/Npgsql, PostgreSQL, migraciones y triggers existentes, auditoría local, cola y SQL de `registrar_actividad` extraído del código local de Nexo. Componentes sintéticos: Microsoft OAuth/Graph, catálogo publicado de Nexo, datos de empleados/UR y todas las cuentas. El catálogo de prueba tiene seis UR; el borrador de carga tiene perfil completo y Word respondido. No se consultaron fuentes institucionales ni se midió su tamaño o latencia.

La carga usa HTTP real de Kestrel en loopback. Un transporte exclusivo del proyecto de pruebas conserva cookies bajo su origen HTTPS y marca el esquema de transporte con el mecanismo sintético existente. **No mide cifrado ni handshake TLS de producción.** Dos preparaciones iniciales con TLS efímero desde .NET fallaron en Schannel, antes del primer guardado; se conservaron sus resultados, se corrigió el transporte del arnés y no se cuentan como errores de captura.

Las rondas API modelan una pausa de 1500 ms para automáticos; la ráfaga modela cinco selecciones cercanas antes de un solo envío. No son 25 navegadores React. El debounce real y la captura fluida se comprueban separadamente en Edge con el formulario y sus bundles reales, transporte controlado y medición de Resource Timing. El escenario idéntico fuerza un PUT para auditar el backend: el cliente normal omite ese envío.

## Frontera de cada medición

1. Crear fixtures, autenticar personas, obtener CSRF, aceptar aviso, guardar perfil y calentar el código son fases separadas. El login respeta el limitador real de 20 solicitudes/minuto/IP: las pausas de preparación no entran en el tiempo HTTP del guardado.
2. Inmediatamente antes de cada lote se reinicia `pg_stat_statements`. Durante el lote sólo se envían las operaciones medidas y un observador de bloqueos cada 25 ms, con cuenta administradora de pruebas.
3. Cada solicitud lleva un `traceparent` sintético propio. Un `ActivityListener` de pruebas recoge únicamente sus comandos Npgsql: categorías, huellas de SQL y duraciones, sin parámetros ni respuestas. No existe este observador en producción.
4. PostgreSQL aporta contadores y tiempo de ejecución por base, sentencia y nivel. Se excluyen del total principal todas las sentencias **de nivel superior** del administrador de fixtures/observación. Se conservan las internas `SECURITY DEFINER`: por ejemplo, el trigger de `cd_control`. Sus tiempos se muestran aparte y **no se vuelven a sumar** al comando que los contiene.
5. La consulta de resultados, verificaciones posteriores por usuario y limpieza se realizan fuera del lote. El JSON conserva las fases y la instrumentación excluida, no sólo el total conveniente para el informe.

Los tiempos se distinguen así:

- **Antes del envío:** pausa/agrupación; no es ejecución SQL ni tiempo HTTP.
- **HTTP completo:** desde el envío hasta consumir todo el cuerpo de respuesta; incluye aplicación, conexiones, autorización, serialización y transporte loopback.
- **SQL cliente:** suma de duraciones de los comandos Npgsql correlacionados. Incluye comunicación/lectura del resultado; no es tiempo puro de PostgreSQL ni apertura de todas las conexiones.
- **SQL servidor:** `total_exec_time` de sentencias de nivel superior; incluye espera durante su ejecución. En concurrencia los tiempos se suman entre conexiones. Se informa su media por operación; `pg_stat_statements` no entrega P95 por solicitud y no se inventa uno. El P95 SQL presentado corresponde expresamente al **cliente Npgsql**.
- **Bloqueos:** muestreo `pg_stat_activity` cada 25 ms; es evidencia de esperas observadas, no una prueba de ausencia de esperas menores que ese intervalo.

## Qué son las 35 sentencias

Se confirmó el total en un solo PUT del arnés sin pool, con cola de actividad vacía. En la traza Npgsql aparecen 27 comandos; PostgreSQL añade los ocho BEGIN/COMMIT/ROLLBACK no emitidos como spans de comando. SAVEPOINT/RELEASE sí aparecen en ambos. **No se suman entre sí ambas fuentes.**

| Base/contexto | Función | Cantidad por PUT | Finalidad |
| --- | --- | ---: | --- |
| TDV2, sesión | Leer ticket vigente | 1 | Autenticar esta solicitud; no es el login inicial |
| TDV2, identidad | Comprobar vínculo Microsoft/usuario | 1 | Evitar usar identidad local o claims que ya no corresponden |
| Nexo de Competencias | Aplicación, empleado, roles, módulos | 4 | Autoridad vigente de esta aplicación, sin permisos en caché entre solicitudes |
| TDV2, representación | Leer contexto | 3 | Contexto inicial y dos comprobaciones dentro de la transacción |
| TDV2, catálogo | UR y fecha de actualización | 1 | Adscripción, antecesoras y alcance reales del catálogo local |
| TDV2, concurrencia | Sesión vigente `FOR SHARE` | 2 | Antes de escribir y antes del commit; impide cambio de actor y detecta caducidad |
| Diagnóstico, concurrencia | Bloqueo compartido de generación | 2 | Guardado y lectura posterior protegidos frente a reinicio |
| Diagnóstico, concurrencia | Leer generación | 2 | Rechazar una generación antigua, también al construir la respuesta |
| Diagnóstico, concurrencia | Advisory lock del empleado | 1 | Serializar pestañas/dispositivos de la misma persona |
| Diagnóstico, encuesta | Leer encuesta | 2 | Versión/estado antes de guardar y estado efectivamente persistido después |
| Diagnóstico, privacidad | Consentimiento vigente | 2 | Comprobar escritura y devolver estado de aceptación correcto |
| Diagnóstico, respuestas | UPDATE de encuesta | 1 | Perfil/respuestas/contexto, fecha y versión; sin actualización si son idénticos |
| TDV2, auditoría local | Bloqueo del evento + INSERT condicional | 2 | Un registro local por revisión, sin respuestas ni secretos |
| Diagnóstico, entrega Nexo | Buscar pendiente `SKIP LOCKED` | 1 | Mantener la entrega diferida existente; no crea eventos automáticos |
| TDV2 + diagnóstico | Control transaccional | 10 | 4 BEGIN, 2 COMMIT, 2 ROLLBACK de lectura/cola vacía, SAVEPOINT y RELEASE |
| **Total de nivel superior** | | **35** | **TDV2 12 + diagnóstico 19 + Nexo 4** |

El UPDATE interno de `cd_control` es **uno adicional dentro del trigger**, con tiempo ya contenido en el UPDATE de encuesta. No se debe ocultar ni contarlo dos veces. El documento idéntico utiliza 32 sentencias: no UPDATE de encuesta/control ni SAVEPOINT/RELEASE; el INSERT condicional de auditoría afecta cero filas. Guardar avance sin cambios utiliza 44 sentencias en este fixture con entrega confirmada: TDV2 14, diagnóstico 24 y Nexo 6, además del SQL interno de la función central. No modifica respuestas; agrega una operación manual y su recibo.

Las lecturas repetidas tienen fronteras diferentes. `AccessState.Guard` no se sustituye por el contexto leído antes de obtener el bloqueo. `Read` después del commit no se sustituye por el objeto enviado por el navegador: podría haberse intercalado otra revisión o un reinicio. Tampoco se omiten las comprobaciones del receptor de actividad por haber autorizado previamente el módulo: validan otra operación. Se conserva el intento de entregar actividad pendiente desde `Write`, por lo que una cola no vacía puede elevar el costo de una solicitud sin generar un nuevo evento automático.

### Duración de las sentencias de una solicitud

Sonda sin pool `native-postgres-20261029-073935-f6798c0f`: un PUT con cambio, sin tráfico de preparación. La duración indicada es la suma de ejecución **PostgreSQL** de las llamadas de cada fila (ms), no la duración de abrir conexiones.

| Base/contexto | Finalidad | Sentencias | Filas | SQL ms |
| --- | --- | --- | --- | --- |
| Diagnóstico | buscar entrega pendiente con SKIP LOCKED | 1 | 0 | 0.0192 |
| Diagnóstico | bloqueo compartido de generación | 2 | 2 | 0.0214 |
| Diagnóstico | leer generación vigente | 2 | 2 | 0.0347 |
| Diagnóstico | serialización por empleado | 1 | 1 | 0.0066 |
| Diagnóstico | leer captura/versión antes de escribir y después del commit | 2 | 2 | 1.3880 |
| Diagnóstico | consentimiento vigente en escritura y respuesta | 2 | 2 | 0.0940 |
| Diagnóstico | actualizar captura y revisión | 1 | 1 | 3.4401 |
| Diagnóstico | BEGIN | 3 | 0 | 0.0136 |
| Diagnóstico | COMMIT | 1 | 0 | 0.0003 |
| Diagnóstico | RELEASE | 1 | 0 | 0.0005 |
| Diagnóstico | ROLLBACK | 2 | 0 | 0.0016 |
| Diagnóstico | SAVEPOINT | 1 | 0 | 0.0010 |
| Nexo sintético | identidad y empleado publicados | 1 | 1 | 0.0342 |
| Nexo sintético | módulos permitidos y orden | 1 | 5 | 0.0409 |
| Nexo sintético | roles vigentes | 1 | 1 | 0.0159 |
| Nexo sintético | validar aplicación publicada | 1 | 1 | 0.0499 |
| TDV2 | bloqueo de deduplicación del evento | 1 | 1 | 0.0072 |
| TDV2 | insertar revisión/evento si no existe | 1 | 1 | 0.1499 |
| TDV2 | jerarquía y fecha del catálogo local SII | 1 | 6 | 0.0417 |
| TDV2 | sesión vigente bloqueada antes de escribir/commit | 2 | 2 | 0.0861 |
| TDV2 | ticket de sesión vigente | 1 | 1 | 0.0554 |
| TDV2 | vínculo usuario Microsoft y versión de sesión | 1 | 1 | 0.0164 |
| TDV2 | contexto efectivo inicial y validaciones transaccionales | 3 | 0 | 0.0520 |
| TDV2 | BEGIN | 1 | 0 | 0.0043 |
| TDV2 | COMMIT | 1 | 0 | 0.0007 |

Total: **35 sentencias / 5.5756 ms de ejecución SQL**; 47.69 ms de comandos Npgsql y 411.20 ms HTTP. Trigger interno de control: 0.0207 ms, ya incluido en el UPDATE. Las filas del SELECT indican filas leídas; no son modificaciones.

### Cuatro operaciones individuales

| Operación | SQL superior | SQL servidor ms | Npgsql ms | HTTP ms | Envío / respuesta B |
| --- | --- | --- | --- | --- | --- |
| Con cambios | 35 | 5.58 | 47.69 | 411.20 | 286 / 126121 |
| Idéntico (API forzada) | 32 | 1.95 | 43.32 | 520.64 | 286 / 126121 |
| Manual sin cambios | 44 | 25.90 | 76.97 | 661.39 | 116 / 126121 |
| Ráfaga de 5 selecciones | 35 | 5.60 | 54.26 | 415.43 | 286 / 126109 |

En manual, el trabajo de entrega/recibos es:

| Base | Finalidad | Sentencias | SQL ms |
| --- | --- | --- | --- |
| Diagnóstico | buscar entrega pendiente con SKIP LOCKED | 2 | 0.1072 |
| Nexo sintético | entregar y confirmar recibo central | 1 | 23.1860 |
| Diagnóstico | comprobar recibo manual existente | 1 | 0.0033 |
| Diagnóstico | encolar evento manual idempotente | 1 | 0.3626 |
| Diagnóstico | persistir intento y recibo | 1 | 0.0618 |

La entrega vuelve a comprobar la aplicación publicada (Nexo pasa de cuatro a seis sentencias), además del SQL interno del contrato central. La revisión ya fue auditada en el PUT anterior; este POST registra la operación manual mediante `Mirror`, con una transacción TDV2 propia, y el recibo evita repetirla. El detalle de los cuatro escenarios, incluido SQL interno, se conserva en [probe.json](evidencia/costo-autoguardado-20261029/probe.json).

### Trabajo que NO forma parte de esas 35 sentencias

| Fase de la sonda | SQL superior de aplicación | SQL superior de preparación/observación | SQL interno | Duración fase ms |
| --- | --- | --- | --- | --- |
| preparar catálogo sintético | 0 | 6 | 0 | 25.93 |
| autenticación inicial y CSRF (fuera del guardado) | 15 | 1 | 0 | 500.69 |
| consentimiento, perfil y calentamiento (fuera del guardado) | 111 | 1 | 3 | 1596.10 |
| verificar probe/1/cambios (fuera del guardado) | 14 | 1 | 0 | 201.93 |
| verificar probe/1/identico_forzado_api (fuera del guardado) | 14 | 1 | 0 | 300.52 |
| verificar probe/1/manual_sin_cambios (fuera del guardado) | 14 | 1 | 0 | 317.49 |
| verificar probe/1/selecciones_rapidas (fuera del guardado) | 14 | 1 | 0 | 201.71 |

Durante el PUT medido se excluyeron 18 sentencias administrativas de instrumentación (2.53 ms SQL). Incluyen reset/observación, no captura. El registro de fases no pretende contabilizar toda la creación del clúster: initdb, migraciones/fixtures generales anteriores al caso y su apagado/limpieza están fuera de la ventana. No se atribuyen sus sentencias al guardado. La correlación confirma **comandos de solicitud + control transaccional no trazado = total superior PostgreSQL**. Con pool se añade también su mantenimiento no trazado.


## Carga y comparación

La configuración del arnés anterior fija `Pooling=false`. Se repite la misma prueba antes/después **sin cambios de código de producción**: las diferencias entre esas dos rondas son variabilidad y calentamiento, no una optimización atribuible al código. Una prueba adicional cambia únicamente el pooling en las conexiones desechables del host de pruebas. No se leen ni editan User Secrets, variables reales ni permisos institucionales.

### Antes/después, mismo código y conexiones sin pool

P50 = tiempo habitual de la muestra. Todos los valores de duración están en ms. A = primera pasada, B = repetición; **no hubo optimización de producción entre ellas**.

| Usuarios / n | Operación | HTTP A P50 / P95 | HTTP B P50 / P95 | SQL cliente B P95 | SQL servidor B media |
| --- | --- | --- | --- | --- | --- |
| 1 / 10 | Con cambios | 320.8 / 510.7 | 393.6 / 414.8 | 48.08 | 5.54 |
| 1 / 10 | Idéntico (API forzada) | 300.4 / 393.7 | 399.0 / 486.8 | 42.68 | 3.12 |
| 1 / 10 | Manual sin cambios | 619.3 / 785.0 | 498.7 / 884.2 | 79.37 | 13.81 |
| 1 / 10 | Ráfaga de 5 selecciones | 415.8 / 524.1 | 304.3 / 419.6 | 47.49 | 4.94 |
| 10 / 100 | Con cambios | 913.5 / 1065.5 | 692.8 / 829.6 | 54.07 | 6.45 |
| 10 / 100 | Idéntico (API forzada) | 917.6 / 994.3 | 680.5 / 784.0 | 48.04 | 3.49 |
| 10 / 100 | Manual sin cambios | 1296.3 / 1436.9 | 981.9 / 1091.3 | 76.69 | 17.29 |
| 10 / 100 | Ráfaga de 5 selecciones | 911.1 / 1060.5 | 691.1 / 819.9 | 52.06 | 6.03 |
| 25 / 250 | Con cambios | 2252.2 / 2394.5 | 1723.4 / 1825.8 | 59.70 | 7.01 |
| 25 / 250 | Idéntico (API forzada) | 2236.5 / 2484.4 | 1747.1 / 1872.5 | 50.12 | 3.99 |
| 25 / 250 | Manual sin cambios | 3141.1 / 3979.6 | 2461.5 / 2593.5 | 77.58 | 17.56 |
| 25 / 250 | Ráfaga de 5 selecciones | 2236.9 / 2380.3 | 1776.7 / 1888.3 | 64.88 | 8.99 |

En ambas pasadas: 35 sentencias/cambio o ráfaga, 32/idéntico y 44/manual con entrega; **2.880 solicitudes, cero errores y cero conflictos entre personas distintas**. Cada sesión modelada hace diez solicitudes por escenario; una ráfaga de cinco selecciones produce un envío. En esta carga cerrada se espera a todas las respuestas de una ronda antes de la siguiente; no se afirma capacidad máxima ni tráfico de llegada abierto.

### Sensibilidad al pool, exclusivamente en pruebas

| Usuarios / n | Operación | HTTP P50 / P95 | Npgsql P95 | SQL servidor media | SQL superior / mantenimiento (media) |
| --- | --- | --- | --- | --- | --- |
| 1 / 10 | Con cambios | 30.3 / 68.9 | 26.02 | 2.93 | 45.00 / 10.00 |
| 1 / 10 | Idéntico (API forzada) | 16.7 / 31.4 | 16.38 | 1.80 | 42.00 / 10.00 |
| 1 / 10 | Manual sin cambios | 23.9 / 194.8 | 79.57 | 5.27 | 57.90 / 13.90 |
| 1 / 10 | Ráfaga de 5 selecciones | 17.5 / 33.4 | 22.30 | 2.72 | 45.00 / 10.00 |
| 10 / 100 | Con cambios | 49.3 / 122.4 | 31.95 | 9.72 | 44.74 / 9.74 |
| 10 / 100 | Idéntico (API forzada) | 32.1 / 74.6 | 50.66 | 1.71 | 42.00 / 10.00 |
| 10 / 100 | Manual sin cambios | 35.5 / 191.9 | 89.91 | 5.16 | 57.74 / 13.86 |
| 10 / 100 | Ráfaga de 5 selecciones | 53.5 / 86.5 | 77.74 | 14.58 | 45.00 / 10.00 |
| 25 / 250 | Con cambios | 138.6 / 189.6 | 141.24 | 39.99 | 44.88 / 9.88 |
| 25 / 250 | Idéntico (API forzada) | 85.5 / 211.9 | 161.53 | 2.08 | 41.98 / 9.98 |
| 25 / 250 | Manual sin cambios | 96.4 / 377.0 | 113.31 | 5.18 | 57.14 / 13.60 |
| 25 / 250 | Ráfaga de 5 selecciones | 146.0 / 199.2 | 163.08 | 54.35 | 45.26 / 10.06 |

**1.440 solicitudes adicionales, cero errores/conflictos.** El mantenimiento se incluye en el total superior, no se suma otra vez: son RESET/DISCARD y limpieza de sesión de conexiones reutilizadas. Las llamadas de negocio del PUT cambiado siguen siendo 35 cuando la cola está vacía. El primer uso de conexiones nuevas y la entrega concurrente de pendientes explican los promedios fraccionarios.

Con 25 personas se encolaron 250 operaciones manuales: 246 se recibieron dentro del lote manual y cuatro durante las siguientes capturas. Estas últimas **entregaron eventos manuales ya existentes; no crearon eventos automáticos**. Los 360 eventos manuales de la prueba con pool terminaron recibidos una sola vez. La entrega `SKIP LOCKED` puede atender eventos de otras solicitudes; por eso no se atribuye ciegamente una recepción al autoguardado que la transportó. Las dos pasadas sin pool encolaron/recibieron 720 manuales; cero nuevos eventos en todos los automáticos.

### Espera previa, volumen y bloqueos

| Escenario modelado (sin pool, A+B) | Espera previa P50 min–max entre lotes | Solicitud JSON B | Respuesta JSON B |
| --- | --- | --- | --- |
| Con cambios | 1499.0–1504.0 ms | 286–288 | 125971–126123 |
| Idéntico (API forzada) | 1499.8–1508.3 ms | 287–288 | 126110–126111 |
| Manual sin cambios | 0.0–0.0 ms | 117–118 | 126110–126111 |
| Ráfaga de 5 selecciones | 1922.2–1955.1 ms | 287–288 | 125972–126123 |

La ráfaga incluye cinco intervalos sintéticos de 80 ms más 1500 ms de pausa; no es CPU/SQL bloqueando el formulario. El documento de carga contiene perfil y Word; el ensayo separado con todos los bloques conserva su medición en [conflict-and-full-document.json](evidencia/costo-autoguardado-20261029/conflict-and-full-document.json). El manual confirmado sin cambios envía sólo versión/generación/UUID. Si hay cambios aún pendientes, la interfaz necesita primero el PUT y después la confirmación manual; no se oculta ese costo bajo la cifra de una sola confirmación.

| Carga con pool | Pico de conexiones esperando | Muestras-conexión esperando | Eventos observados |
| --- | --- | --- | --- |
| 10 personas, Con cambios | 7 | 18 | Lock:transactionid=8, Lock:tuple=10 |
| 10 personas, Ráfaga de 5 selecciones | 7 | 37 | Lock:transactionid=35, Lock:tuple=2 |
| 25 personas, Con cambios | 17 | 259 | Lock:transactionid=176, Lock:tuple=83 |
| 25 personas, Ráfaga de 5 selecciones | 19 | 336 | Lock:transactionid=237, Lock:tuple=99 |

Son observaciones cada 25 ms, no milisegundos exactos de espera. Con pool y 25 personas, el UPDATE interno de `cd_control` acumuló 36.69 ms por cambio; en ráfagas, 49.62 ms. El aumento junto con esperas tuple/transactionid es consistente con la fila compartida de revisión global del trigger. Esa serialización protege la confirmación del reinicio administrativo; no se elimina. No se identificaron bloqueos permanentes ni errores; sin pool el pico observado fue tres conexiones. La configuración sin pool espacia la llegada a PostgreSQL y no revela igual contención.

El ensayo de **dos pestañas de una misma persona** está separado de las cargas anteriores: dos escritores alcanzaron un bloqueo inducido, liberado a los 502.66 ms desde lanzar las solicitudes; respuestas **200/409**, cero sobrescrituras y cero bloqueos residuales. Ese tiempo incluye el camino hasta el bloqueo; no se presenta como duración de espera individual. También se rechazó la revisión antigua diferente, y se aceptó una repetición idéntica sin UPDATE.


## Conclusión y conservación

No se encontraron actualizaciones redundantes de respuestas en estos escenarios. El cliente omite datos idénticos, y la API conserva su defensa de no-op, versiones y generación. No se eliminan consultas justificadas de seguridad/concurrencia ni se cambia el modelo de datos, el instrumento, los informes o los permisos. Tampoco se altera el diseño, el guardado detenido tras error, la recuperación manual ni la cola Nexo.

La evidencia no justifica quitar lecturas transaccionales ni modificar respuestas/índices/esquema. Sí demuestra que atribuir toda la latencia a «35 sentencias» era incorrecto: con las mismas comprobaciones, reutilizar conexiones en el arnés cambia sustancialmente el resultado, aunque añade limpieza SQL. **No se modificó la configuración institucional ni se comprobó su pooling real.** No se propone activarlo a ciegas como si se hubiera descubierto desactivado en producción.

La respuesta sigue incluyendo instrumento y evaluación (~126 KB en este borrador, ~148 KB con todos los bloques), bastante mayor que el envío. Reducir ese contrato o estudiar el punto de serialización global requiere otra medición representativa y preservar validación/recuperación; no se atribuye a esos tamaños un tiempo que no se aisló. El resultado local respalda conservar el autoguardado actual, sin una optimización adicional de consultas en esta entrega. No demuestra capacidad de un padrón institucional, ni equivalencia con TLS/red remota/catálogos grandes.

El INSERT local condicional sin filas no es una segunda actualización de captura. Mantiene deduplicación y permite registrar una revisión ya persistida si la transacción de auditoría anterior no confirmó. Diagnóstico y TDV2 tienen transacciones separadas; esta auditoría no afirma atomicidad distribuida. La cola de actividad conserva su política independiente de reintentos.


## Archivos y reproducción

- `tests/TDV2.NativeVerification/DigitalCostAudit.cs`: fases separadas, actores/cookies distintos, carga acotada, estadísticas SQL, latencias y observador aislado de bloqueos.
- `tests/TDV2.NativeVerification/DigitalCostTrace.cs`: correlación por solicitud y clasificación; no exporta SQL, parámetros ni datos de captura.
- `tests/TDV2.NativeVerification/DigitalTests.cs`: registro optativo del caso de auditoría.
- `ClientApp/tests/browser/digital-autosave-ui.mjs`: mide además el intervalo real desde selección hasta envío, sin instrumentación de producción.
- README, ESTADO_MIGRACION, GUIA, guía de medición previa y esta guía/evidencia.

```powershell
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false
$env:TDV2_TEST_AUTOSAVE_MEASURE = 'true'
$env:TDV2_TEST_COST_AUDIT = 'true'
$env:TDV2_TEST_CASE_PREFIX = 'Costo autoguardado:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -SkipBrowser -NoBuild -DirectStart
# Sensibilidad: cambia únicamente conexiones sintéticas; nunca configura la aplicación real.
$env:TDV2_TEST_COST_POOLING = 'true'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -SkipBrowser -NoBuild -DirectStart
Remove-Item Env:TDV2_TEST_COST_POOLING,Env:TDV2_TEST_COST_AUDIT,Env:TDV2_TEST_AUTOSAVE_MEASURE,Env:TDV2_TEST_CASE_PREFIX
```

`TDV2_TEST_COST_PROBE=true` limita el arnés a una observación por escenario para revisar su clasificación; esa ejecución no permite inferir percentiles de carga. La comprobación de conflicto entre dos pestañas utiliza el caso separado `Autoguardado SQL:` del ejecutor anterior. Todas las migraciones/preparaciones de estos comandos son exclusivas de las bases efímeras verificadas. No hay configuración nueva de producción ni necesidad de publicar diagnósticos en la interfaz.

Comandos adicionales ejecutados:

```powershell
$env:TDV2_TEST_AUTOSAVE_MEASURE = 'true'
$env:TDV2_TEST_COST_AUDIT = 'true'
$env:TDV2_TEST_COST_PROBE = 'true'
$env:TDV2_TEST_CASE_PREFIX = 'Autoguardado SQL:|Costo autoguardado:|Guardado manual:|Dos pestañas/sesiones:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -SkipBrowser -NoBuild -DirectStart
Remove-Item Env:TDV2_TEST_COST_PROBE,Env:TDV2_TEST_COST_AUDIT,Env:TDV2_TEST_AUTOSAVE_MEASURE,Env:TDV2_TEST_CASE_PREFIX
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
Push-Location ClientApp
node tests/browser/digital-autosave-ui.mjs
node tests/browser/digital-manual-save-ui.mjs
Pop-Location
git diff --check
```

- ASP.NET/NativeVerification Release: compilación correcta, sin advertencias ni errores. TypeScript, Vite y **99/99** pruebas frontend correctas.
- Dos pasadas sin pool y una con pool: **4.320 solicitudes de carga** con PostgreSQL/Kestrel reales, cero errores; fixtures sintéticos, no Nexo institucional. [Antes/después](evidencia/costo-autoguardado-20261029/baseline.json), [pool](evidencia/costo-autoguardado-20261029/pool.json). Las muestras compactas preservan tiempos/bytes/estado y cantidad SQL por solicitud; los comandos individuales completos siguen en los artefactos locales indicados. La clasificación del observador se afinó antes de la sonda final para reconocer parámetros normalizados; los totales previos no cambiaron.
- **10/10 casos PostgreSQL dirigidos**: sonda detallada, SQL/documento completo, conflicto entre sesiones y siete grupos manuales (autoguardados de todos los bloques, doble envío, recibos, permisos, generación/CSRF, receptor caído, rollback, representación y definitivas). [Resultado](evidencia/costo-autoguardado-20261029/probe-native.json).
- Edge/React: **22 escenarios de captura** y **16 manuales**, transporte controlado, sin errores JavaScript. [Captura y mediciones](evidencia/costo-autoguardado-20261029/browser.json), [manual](evidencia/costo-autoguardado-20261029/manual-browser.json).

| Ancho | Selecciones / PUT | Espera desde última selección hasta enviar ms | HTTP con demora controlada ms | Máximo en vuelo | Campos bloqueados / saltos / reinicios |
| --- | --- | --- | --- | --- | --- |
| 1440 | 7 / 2 | 1503.4 / 1498.4 | 2018.7 / 2019.3 | 1 | 0 ms / 0 px / 0 |
| 390 | 7 / 2 | 1500.7 / 1509.8 | 2016.4 / 2012.6 | 1 | 0 ms / 0 px / 0 |

La segunda captura conserva las selecciones realizadas durante la primera solicitud; su espera incluye la coordinación con esa solicitud, además del debounce. Foco y nodos conservados. El error detiene temporizadores/envíos y no reintenta al editar, recuperar foco o conexión; manual explícito y recarga conservan la política documentada. La instrumentación vive en las pruebas, no en el formulario de producción. No se repitió la suite completa de 66 casos ni todo el navegador PostgreSQL de la entrega anterior: no cambió código de producción y aquí se ejecutaron las regresiones dirigidas indicadas.

Los clústeres usados fueron detenidos al terminar. No se modificaron modelos, migraciones, conexiones, permisos, paquetes o la solución del 401. Sin bases institucionales, reinicios reales, push ni despliegues. Ubuntu no ejecutado; el código diagnóstico no se publica en la aplicación.

