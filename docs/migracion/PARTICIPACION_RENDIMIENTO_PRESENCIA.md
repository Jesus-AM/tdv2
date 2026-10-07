# Participación, rendimiento y presencia

Actualización local del 6 de octubre de 2026. No aplica cambios a bases institucionales ni publica Nexo o TDV2.

## Implementación

`GET /configuracion/procesos`, `POST /configuracion/procesos/vista-previa` y `PUT /configuracion/procesos` exigen administrador, los módulos padre e hijo de Nexo, su relación y rutas exactas. `AccessBoundary` y el controlador rechazan Vista de prueba y Actuar como usuario; las mutaciones conservan CSRF y contexto de sesión.

`ProcessSettings`, su configuración EF y `ProcessConfigurationService` guardan un único registro versionado. Inicialmente participan niveles 2 y 3 y se excluye N. Los tipos se comparan con espacios exteriores eliminados y mayúsculas; 0, nulo y vacío no equivalen a N. «Sin tipo» permite excluir explícitamente nulos/vacíos. Las opciones guardadas se conservan aunque desaparezcan temporalmente del catálogo. También se permite seleccionar ningún nivel, previa revisión del impacto.

La confirmación protegida incluye versión, propuesta, catálogo completo presente y versiones/estado de los formatos afectados. Se vuelve a calcular bajo bloqueo antes de guardar. Una modificación concurrente exige otra vista previa; configuración y auditoría de valores anteriores/nuevos se confirman en la misma transacción. La vista previa describe alcances centrales potenciales **por adscripción**, sin enumerar personas ni presumir que tienen una concesión.

El bloqueo compartido de `configuracion_procesos` en captura y publicación de catálogos se coordina con el exclusivo de configuración. Ninguna transacción permanece abierta durante la interacción humana. Una captura autorizada con otra versión se rechaza conservando la propuesta. Las señales despiertan las suscripciones, que vuelven a autorizarse; se conserva además la comprobación de 15 segundos y su cancelación limpia.

`Participation` separa elegibilidad de autorización y catálogo. El directorio muestra los niveles configurados y conserva jerarquías por ID. Excluir no borra formatos, respuestas, vínculos, nodos ni instantáneas. Reactivar recupera la misma identidad y contenido; enviados siguen bloqueados. El administrador conserva su rama institucional de nivel 2, aunque su nodo no participe. El responsable sigue sujeto a empleado y responsabilidad institucional; habilitar niveles no concede autoridad sobre áreas ajenas.

La asignación central explícita (`origen=central`) recalcula su alcance por adscripción: local resuelve el primer formato participante de sus ascendientes, sin ascender por encima de su rama de nivel 2; dependencias incluye los participantes cuyo ascendiente nivel 2 sea su raíz. El rol efectivo solo nunca basta. La delegación conserva su contrato actual de niveles 2/3, otorgante, empleado, adscripción y concesión `aplicacion`. Un vínculo cuyo formato fue excluido no se traslada; una revocación local pendiente sigue bloqueada. La vista de prueba usa el catálogo como contexto de adscripción y siempre es consulta.

## Rendimiento e interacción

El motor y sus propuestas viven fuera de la pestaña visible. Sólo se monta esa pestaña. `changeBlock` copia el bloque afectado; `applyBlocks` conserva referencias de tablas y filas ajenas. Las operaciones con relaciones conservan su preparación conjunta. La comparación de cambios evita dividir y serializar todo el documento por tecla; volver al valor original elimina el pendiente. Las filas memoizadas conservan foco y reciben cambios de contenido, reserva, permisos y selección. Los estados remotos equivalentes reutilizan el contenido.

La escritura continúa inmediata después de confirmar reserva y versión, con autoguardado de un segundo. No hay botones para comenzar o terminar. Se guardan cambios antes de liberar, se distinguen sesiones/pestañas y se mantienen propuestas ante fallos; Select y Dialog no liberan por el foco de sus portales. La eliminación directa conserva confirmación MUI, reservas relacionadas y comprobación de contenido por ID.

Inicio obtiene formatos e inventarios por conjuntos. La consulta lateral de PostgreSQL trae hasta 201 filas por clave exacta de ILDA, para ofrecer 200 y detectar excedentes, conservando orden `C`, avisos y asociaciones `06000`/`6000` separadas. No carga la réplica completa, no paraleliza cientos de consultas y no comparte DbContext concurrentemente. No se cachean permisos entre solicitudes.

Los colores se asignan bajo el bloqueo PostgreSQL de la UR, a partir de la identidad efectiva, conservando los activos y evitando colisiones mientras haya colores libres. Todas las pestañas de una persona comparten color, pero sus reservas siguen independientes. El cliente sólo presenta el índice confirmado: contorno de 2 px, tinte, avatar de iniciales de 26 px, nombre, candado/edición y tooltip con nombre. La paleta no usa rojo. Al liberar o vencer desaparece el estado y se respeta movimiento reducido. No se consultan fotos Microsoft de personas representadas.

La última instrucción recibida quedó cortada en «Al pasar el mouse sobre el». Se solicitó completarla; el tooltip implementado muestra el nombre, sin incorporar requisitos no recibidos.

## Instalación explícita por el operador

En Nexo, conservar la aplicación `tdv2` y nivel 2. Registrar o actualizar **una sola vez**:

| Clave | Nombre | Padre | Ruta | Icono | Rol |
|---|---|---|---|---|---|
| `configuracion_procesos` | Configuración procesos | ID real de `configuracion` | `/configuracion/procesos` | `mdi-tune` | `administrador` |

Publicar el módulo y su relación mediante la publicación habitual de Nexo. El administrador necesita también `configuracion`. `procesos_operativos` mantiene `/inicio`. No cambian claves de colaboradores, orígenes centrales/delegados, capacidades de representación ni Entra. Responsables, supervisor y colaboradores no reciben el nuevo submódulo.

Nueva migración: `20261006233010_ProcessParticipationAndPresence`. Agrega `configuracion_procesos`, sus valores iniciales, y `participante`/`color` en `formato_bloques`. No altera migraciones anteriores. Si el usuario de ejecución difiere del propietario que migra, necesita SELECT/UPDATE sobre la nueva tabla además de sus permisos existentes. No hay DDL en F5.

Desde la raíz, para revisar y construir:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project tdv2
dotnet ef migrations script 20261005135417_CollaborativeFormsAndSubmission 20261006233010_ProcessParticipationAndPresence --project tdv2 --output .artifacts/participacion-presencia.sql
npm --prefix ClientApp ci
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release -p:UseAppHost=false
```

**Sólo el operador**, después de verificar destino, permisos y recuperación, aplica la migración; estos comandos no se ejecutaron contra la base institucional en esta entrega:

```powershell
dotnet ef database update --project tdv2 -- --environment Development
dotnet ef database update --project tdv2 -- --environment Development
```

La segunda ejecución debe informar que no quedan migraciones. La reversión se rechaza si hay formatos enviados; no se proporciona reapertura. Ejecutar con **un solo operador**: la prueba de dos aplicadores EF/Npgsql simultáneos detectó una colisión de DDL entre migraciones pendientes. Se verificó que no pierde columnas/índices y que un reintento secuencial converge al historial íntegro. No se promete que ambos aplicadores concurrentes terminen con éxito ni se añadió un migrador propio para ocultarlo.

## Verificación y límites

Las mediciones son sintéticas y locales, no una previsión de tiempos institucionales. El estado final y las evidencias se consignan en `ESTADO_MIGRACION.md`. El primer ensayo del bundle anterior montó siete pestañas y 7.445 controles con 800 filas; no completó la escritura y no se considera una prueba aprobada ni una medida de latencia válida.

| Medición | Antes | Después |
|---|---:|---:|
| Motor, 800 filas y 150 cambios: media/p95 | 4,87 / 6,68 ms | 0,075 / 0,130 ms |
| Inicio, 102 UR: comandos EF | 311 | 8 |
| Inicio en las corridas locales | 21,4 s | 1,02–2,66 s |
| Pestañas montadas al abrir Contexto | 7 | 1 |
| Apertura del formato de 800 filas | 10,7 s | 2,23 s |

La corrida final obtuvo 31 eventos de escritura a siguiente frame: media 28 ms, p95 45,4 ms. No hay comparación válida de esa latencia con la versión anterior, porque su intento no completó la escritura. Los tiempos dependen del calentamiento, equipo y carga local. [Datos de medición](evidencia-participacion-rendimiento.json), [navegador de rendimiento/configuración](evidencia-participacion-navegador.json).

Resultados: TypeScript/Vite y solución Release correctos; 60 pruebas frontend, 62 de dominio/transporte, 135 de regresión HTTP/PostgreSQL y 13 específicas finales aprobadas. Los dos grupos PostgreSQL se superponen y no se suman. Las 31 comprobaciones EF y los 30 casos de captura en Edge pasaron. [Regresión PostgreSQL](evidencia-participacion-regresion.json), [participación/presencia](evidencia-participacion-postgresql.json), [EF](evidencia-participacion-migraciones.json), [captura React](evidencia-participacion-captura-react.json).

Las pruebas cubren separación central/delegada, revocación, exclusión y reactivación, formatos enviados, confirmación obsoleta, auditoría fallida, carrera de configuración con captura, permisos por módulo/contexto, niveles nuevos sin delegación, igualdad exacta de claves ILDA y colisión deliberada de colores. El navegador comprueba foco, Select, eliminación directa por ID, cancelación, dos sesiones/pestañas, desconexión, autoguardado, liberación, reservas por criterio y SignalR durante tres intervalos sin actividad. No se sustituyeron esas verificaciones con pruebas en memoria.

### Capturas

Revisión visual del bundle final en Edge; datos e identidades sintéticos. Se bloquean solicitudes externas, incluidas fuentes remotas, por lo que las capturas usan las alternativas locales de la pila tipográfica. No se cambiaron las familias configuradas.

| Tamaño | Participación | Presencia y reserva |
|---|---|---|
| 1920×1080 | [Configuración](configuracion-procesos-1920.png) | [Registro](presencia-1920.png) |
| 1366×768 | [Configuración](configuracion-procesos-1366.png) | [Registro](presencia-1366.png) |
| 768×1024 | [Configuración](configuracion-procesos-768.png) | [Registro](presencia-768.png) |
| 390×844 | [Configuración](configuracion-procesos-390.png) | [Registro](presencia-390.png) |

Sin desbordamiento de página; las tablas conservan desplazamiento horizontal dentro de su contenedor. El contorno no desplaza las filas. Comprobados avatar de 26 px, candado/texto, transición desactivada con movimiento reducido y liberación visible sin recarga. La última corrida visual es `.artifacts/native-postgres-20261007-003326-318d776e`; la regresión de captura, `native-postgres-20261007-002227-02515193`. Ambos clústeres quedaron detenidos.

### Reproducción

Pruebas reproducibles sin tocar bases existentes:

```powershell
dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
$env:TDV2_TEST_BROWSER_FLOW='performance'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
```

Ejecutar estas suites **en secuencia**: Windows bloquea los binarios de prueba mientras están en uso. El preparador sólo crea clústeres nuevos bajo `.artifacts` en loopback y los detiene al terminar. El navegador usa identidades sintéticas y bloquea conexiones externas.

Para aceptación con Visual Studio: abrir `tdv2.slnx`, elegir **TDV2 HTTPS + React**, usar una base aislada con la migración aplicada explícitamente y una publicación de Nexo de pruebas. Abrir dos perfiles de navegador (y dos pestañas del mismo perfil), comprobar reserva automática, primera escritura, Tab, prioridad, guardado al salir y liberación en el observador. Con administrador propio, revisar/cancelar/confirmar la participación mientras la otra sesión conserva una propuesta. Comprobar reactivación e inmutabilidad de enviados exclusivamente sintéticos. F5 no migra ni activa sincronizadores.

Pendiente institucional: publicación del nuevo módulo, migración por el operador y aceptación con la jerarquía real. No se modificaron credenciales, Herd, bases institucionales ni Ubuntu; sin push o despliegue.
