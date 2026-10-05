# Estado de migración TDV2

Inicio: 2026-09-30. Última adaptación: **2026-10-05**. Proyecto convencional ASP.NET Core 10/EF Core/PostgreSQL/React implementado y verificado en el alcance descrito. **La aceptación institucional y el despliegue Ubuntu continúan pendientes; no se declara terminada la sustitución operativa de Laravel.**

## Captura colaborativa, supervisión y envío — entrega vigente 2026-10-05

### Implementado

- `responsable_ur_supervisor` concentra responsabilidad y consulta institucional sin administración. Nivel 2 edita su rama autorizada; nivel 3 su formato. `responsable_ur_institucional` continúa como alias transitorio. El catálogo compartido de Nexo no se renombró; se conservan roles y asignaciones de consulta. La representación mantiene separados actor real y efectivo.
- La delegación valida encargado, acceso vigente, rama y rol en TDV2 y Nexo. Nivel 3 sólo agrega colaboradores locales de su UR/subordinadas a su mismo formato; nivel 2 conserva ambos tipos. Se ampliaron los filtros TDV2 de las dos fuentes Nexo ya corregidas, preservando cambios locales y las reglas de otras aplicaciones. [Configuración y transición exactas](docs/migracion/CONFIGURACION_NEXO_ENTRA.md).
- PostgreSQL arbitra reservas de 45 segundos por fila/grupo de campos, sesión/pestaña/contexto y versión. Autoguardado parcial, UUID y recibos idempotentes, renovación por cambios, liberación tras confirmar, recuperación explícita de propuestas y auditoría atómica. No hay transacciones abiertas durante la captura. El guardado completo anterior se rechaza con 428 y se retiró su implementación.
- SignalR sólo entrega contenido autorizado, revalida Nexo/ticket/contexto y recupera estado al reconectar. Origin se compara con la configuración pública; se admite WebSocket HTTP/2 CONNECT únicamente en este canal de consulta. Las mutaciones y negociación HTTP siguen exigiendo CSRF. No se renuevan reservas mediante latidos de pantalla.
- Enviar guarda pendientes, confirma, exige responsable/avance/requisitos/ausencia de reservas ajenas y se serializa con reservas/guardados. Conserva ejercicio, fecha UTC, versión, identidad real/efectiva, auditoría e instantánea de unidad/respuestas/ILDA. Trigger de inmutabilidad para todos, incluidos administradores y sincronizadores. Sin reapertura.
- React conserva MUI/Roboto e identidad visual: Contexto incorpora los campos de Encabezado, siete secciones montadas, última sección confirmada, navegación superior/móvil, prioridad 1–5 accesible sin defecto y estado compacto sin barra fija. No hay conversión automática de prioridades históricas. Se conservan jerarquías/históricos tipo 0 e identificadores; las claves se formatean sólo al mostrar/buscar.
- Microsoft valida el ID token antes de usar el claim opcional login_hint. Inicio habitual sin selección forzada, hints de cuenta real y Usar otra cuenta con revocación/CSRF. logout_hint nunca se deriva del correo ni de la representación.
- EF: `20261005135417_CollaborativeFormsAndSubmission`, seis columnas y tres tablas adicionales; 17 entidades y seis relaciones. Backfill de ejercicio sin alterar respuestas. Down rechaza retirar la protección si existen envíos. Migración explícita, nunca F5. [Diseño, instalación y comandos](docs/migracion/COLABORACION_ENVIO.md).

### Probado

| Comando desde la raíz | Evidencia y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false` | 53/53 dominio y transporte. Las pruebas del guardado completo retirado se sustituyeron por pruebas de reservas/parches con PostgreSQL real |
| `npm.cmd --prefix ClientApp run types:check` y `npm.cmd --prefix ClientApp run test:forms` | TypeScript correcto; 33/33 frontend: respuestas atrasadas, reintentos, propuesta pendiente, recuperación sin autoguardado, renovación atrasada y selección de sección durante carga |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly` | [29/29 PostgreSQL real](docs/migracion/evidencia-colaboracion-migraciones.json): base vacía y actualización con contenido, repetición, script idempotente, restricciones, rollback y rechazo de Down tras envío. [Huella de esquema nuevo](docs/migracion/evidencia-colaboracion-esquema.json) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser` | [112/112 backend/PostgreSQL](docs/migracion/evidencia-colaboracion-regresion.json), con fuentes externas sintéticas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser` | [17/17 casos de captura](docs/migracion/evidencia-colaboracion-edicion.json): sesiones independientes, dos pestañas, mismo/distinto bloque, vencimiento/reasignación, caducidad antes del commit, revocación, concurrencia con envío, colaborador sin envío, inmutabilidad, prioridad y OIDC firmado |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly` | 40 recorridos Edge: [13 formatos](docs/migracion/evidencia-colaboracion-react-formatos.json), [12 acceso](docs/migracion/evidencia-colaboracion-react-acceso.json), [4 alcance](docs/migracion/evidencia-colaboracion-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-colaboracion-react-sincronizaciones.json). Cero errores JS; escritorio, móvil, teclado y movimiento reducido |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-supervision` | [7/7 con SQL generado por las fuentes reales](docs/migracion/evidencia-supervision-nexo.json), exclusivamente datos y PostgreSQL sintéticos. [Parche adicional](docs/migracion/nexo-supervision.patch) y [hashes coincidentes con las fuentes aplicadas](docs/migracion/evidencia-supervision-nexo-fuentes.json) |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Modelo y snapshot coinciden, sin conexión ni secretos. Script incremental generado en `.artifacts/colaboracion-envio.sql` |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` y `node ClientApp/tests/browser/publish-flow.mjs` | [Paquete final Production/HTTPS](docs/migracion/evidencia-colaboracion-publicacion.json), React compilado sin Vite, sin credenciales, rechazos seguros y cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [tdv2.slnx / TDV2 HTTPS + React](docs/migracion/evidencia-colaboracion-visual-studio.json): Vite 200, lanzamiento 302, React/props 200, privado 401, HTTPS confiable y HMR wss 7136. Sólo páginas anónimas |

Se revisaron capturas de [prioridad móvil](docs/migracion/colaboracion-prioridad-movil.png), [formato enviado](docs/migracion/colaboracion-formato-enviado.png) y [Sincronizaciones móvil](docs/migracion/colaboracion-sincronizaciones-movil.png). Las fuentes externas se bloquean en navegador sintético; la configuración tipográfica original se conserva.

La prueba final de Imprimir generó un [PDF sintético del formato enviado](docs/migracion/colaboracion-formato-enviado.pdf), verificando las siete secciones. Corridas finales: backend `.artifacts/native-postgres-20261005-154331-a579677d`; migraciones `154018-02f01596`; edición `154049-d16e5e8a`; formatos/impresión `154847-ac17674c`; acceso/alcance `152920-19ce7115`; sincronizaciones `153805-e2794a9b`. Los clústeres desechables terminaron detenidos. [Compilación y depuradores de Visual Studio](docs/migracion/evidencia-colaboracion-visual-studio.txt) confirmados; no se autenticaron personas institucionales.

Los intentos fallidos no se cuentan como aprobación. Permitieron corregir CONNECT/WebSocket, el clic Enviar durante blur/autoguardado y una respuesta inicial que reemplazaba la sección seleccionada. Se actualizaron fixtures que aún usaban la clave anterior. El Vite antiguo bloqueó Rolldown durante el primer publish y se detuvo sólo ese proceso de TDV2. El arranque de navegador afectado por `npm ci` se repitió después de terminar la publicación. La migración inicial y las actualizaciones se operan con un único ejecutor: el ensayo concurrente sólo acredita una migración pendiente con historial establecido, no autoriza varios operadores sobre una cadena de migraciones.

Para repetir un recorrido aislado, establecer `$env:TDV2_TEST_BROWSER_FLOW='formats'` o `'sync'` antes de `-BrowserOnly`, y después `Remove-Item Env:TDV2_TEST_BROWSER_FLOW`; los valores admitidos son all/formats/access/scope/sync. Las repeticiones dirigidas figuran junto con los archivos de evidencia, sin sumar varias veces un mismo caso.

### Pendiente institucional

Aplicar la tercera migración con un operador al destino previamente verificado; se conserva la base existente y las dos migraciones iniciales ya aplicadas. Registrar/asociar el rol canónico, revisar asignaciones/catálogos compartidos y publicar la delegación en la conexión Nexo existente. Agregar el claim opcional login_hint al ID token en Entra y probar SSO/salida con las políticas reales. Validar proxy WebSocket, usuarios institucionales, fuentes SII/ILDA y despliegue Ubuntu. No se enviaron formatos institucionales, no se concedieron accesos reales ni se modificaron User Secrets; sincronización automática sigue desactivada. Instrucciones en [Nexo/Entra](docs/migracion/CONFIGURACION_NEXO_ENTRA.md) y [captura/migración](docs/migracion/COLABORACION_ENVIO.md).

## Responsables y consulta institucional — entrega anterior 2026-10-05

### Implementado

- Nivel 3: botón Colaboradores, búsqueda y alta local en su misma UR o descendientes, siempre para el formato del responsable nivel 3. Backend y funciones Nexo rechazan rama ajena y roles de alcance global/dependencias; nivel 2 conserva sus facultades. Vínculo local, concesión central, empleado textual, revocación y auditoría siguen siendo obligatorios.
- Rol `responsable_ur_institucional`: funciona solo como responsable y añade lectura institucional. La responsabilidad sigue siendo institucional; no concede Configuración, Sincronizaciones ni Pruebas de acceso. Representación y vista de prueba conservan sus restricciones.
- EF lee el campo ya existente `tipo_ur`; tipo 0 no es área elegible ni formato, aunque permanece como nodo auxiliar y con su historia. El directorio público conserva ID/código/padre originales y añade `id_ur_principal` para agrupar nivel 2/3 a través de auxiliares. No se añadieron migraciones ni se modificaron las existentes.
- React: claves numéricas sin ceros sólo para presentación/búsqueda, Mis áreas/Todas las áreas, etiquetas Solo consulta, encabezado compacto/adaptable de Sincronizaciones, transiciones breves y movimiento reducido. Actualizar estado no sustituye el borrador ante un error temporal; un rechazo de acceso sigue retirando la vista privada.
- Fuente de Nexo corregida en cuatro archivos, sólo para clave `tdv2`; las demás aplicaciones conservan su comportamiento. Cambios locales previos preservados y hashes comprobados antes de escribir fuera del workspace. [Parche](docs/migracion/nexo-tdv2-delegacion.patch) y [configuración exacta](docs/migracion/RESPONSABLES_UR.md).

### Probado

| Comando desde la raíz | Resultado y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification --no-restore -p:UseAppHost=false` | 58/58 dominio y transporte; dobles en memoria donde corresponde |
| `npm --prefix ClientApp run types:check` y `npm --prefix ClientApp run test:forms` | TypeScript correcto y 24/24 frontend: árbol, tipo 0, búsquedas 06000/6000, autosave, conservación del borrador y revocación |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1` | [95/95 HTTP/PostgreSQL + grupo navegador, 96/96 grupos](docs/migracion/evidencia-ur-postgresql.json). Incluye conservación del padre original, historia tipo 0, ILDA sin mezclar claves, alta/retiro sintéticos, revocaciones, concurrencia y atomicidad |
| Edge de la suite anterior | [9 formatos](docs/migracion/evidencia-ur-react-formatos.json), [12 acceso](docs/migracion/evidencia-ur-react-acceso.json), [4 alcance/UR](docs/migracion/evidencia-ur-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-ur-react-sincronizaciones.json): 36 recorridos, sin errores JS. Escritorio 1440×1000, móvil 390×844 y movimiento reducido; fuentes externas bloqueadas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-tdv2-scope` | [7/7 con funciones y vistas generadas por el código real de Nexo](docs/migracion/evidencia-ur-nexo.json), datos/base sintéticos. Las cuatro fuentes aplicadas coinciden byte a byte con las probadas: [hashes](docs/migracion/evidencia-ur-nexo-fuentes.json). PHP `-n -l` correcto; no se ejecutó la suite Laravel ni su bootstrap |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Sin cambios pendientes de modelo; sin conexión a base ni secretos |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Correcto, incluye npm ci/build; auditoría npm: 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production, HTTPS confiable, React compilado, sin Vite ni secretos](docs/migracion/evidencia-ur-publicacion.json); rechazos 503/401/419 esperados, cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, lanzamiento 302 a HTTPS, React/props 200, privado 401 y HMR wss 7136](docs/migracion/evidencia-ur-visual-studio.json), cero errores JS. No autentica personas institucionales |
| Parche y whitespace | `git -c core.autocrlf=false apply --check --directory=.artifacts/nexo-tdv2-scope/before docs/migracion/nexo-tdv2-delegacion.patch` y `git -c core.autocrlf=false -c core.whitespace=cr-at-eol diff --check`: correctos |

Se revisaron las capturas de [Sincronizaciones escritorio](docs/migracion/ur-sincronizaciones-escritorio.png), [móvil](docs/migracion/ur-sincronizaciones-movil.png) y [consulta institucional móvil](docs/migracion/ur-consulta-institucional-movil.png). Los estilos/tipografías no se sustituyeron; la prueba aislada bloquea descargas externas de fuentes.

Regresión final: `.artifacts/native-postgres-20261005-121554-1445277a`; Nexo: `.artifacts/native-postgres-20261005-115608-3fa5659f`. Clústeres de prueba detenidos. El IDE conserva los documentos y el perfil HTTPS + React quedó en ejecución tras comprobar F5.

Intentos corregidos, no contados como evidencia: dos expectativas iniciales usaban una redirección como JSON y 403 en lugar del 409 existente para representación de consulta. Se corrigieron y repitió toda la regresión. La revisión final añadió padre original e historia tipo 0 y se repitió la suite con ese código. El primer publish encontró un Vite previo bloqueando Rolldown; se detuvo sólo el proceso cuya ruta correspondía a TDV2 y se publicó correctamente. El sandbox no accedía al certificado HTTPS del usuario; la verificación se repitió fuera del sandbox manteniendo validación de certificado. El parche de Nexo se regeneró conservando los finales de línea originales y pasó la comprobación sin modificar la copia previa.

### Pendiente institucional

Registrar/asociar `responsable_ur_institucional` a `procesos_operativos` (`/inicio`), configurar los roles delegantes/asignables y publicar las funciones actualizadas de la conexión existente de Nexo. Instrucciones en [RESPONSABLES_UR.md](docs/migracion/RESPONSABLES_UR.md). No se ejecutó Artisan/Composer en Herd, no se leyó .env, no hubo altas/retiros reales ni conexiones a bases institucionales, migraciones remotas o sincronizaciones. Las pruebas de concesiones/retiros y programación fueron exclusivamente sintéticas. La sincronización automática institucional permanece sin cambios.

## Proyecto convencional y migraciones EF Core — vigente

### Implementado

- Se reutilizan Controllers para HTTP, Domain para reglas/entidades, Services para casos de uso e Infrastructure para persistencia. Se añadió `Tdv2DbContext`, 14 entidades en Domain/Entities y configuraciones `IEntityTypeConfiguration`. No se añadieron capas ni proyectos de aplicación duplicados. Program.cs sólo compone el host y permite los procesadores explícitos ya existentes.
- Integrations/Microsoft y Integrations/Nexo contienen los clientes externos; Integrations/Catalogs conserva el lector SII/ILDA. La publicación, cola y reservas siguen en Synchronization. EF sólo administra public de TDV2: no modela ni migra esquemas externos, roles locales ni tablas de plataforma Laravel sin uso.
- EF Core/Relational/Design y dotnet-ef **10.0.12**, proveedor Npgsql EF **10.0.0**, Npgsql directo **10.0.0**. Manifiesto local de herramientas en .config; paquetes/artefactos dentro del destino. Npgsql directo se conserva por su uso comprobado en bloqueos, tokens, operaciones centrales, reservas y auditoría.
- EF participa en consultas de áreas, formatos, colaboraciones, identidad de sesión e inventario local, además del guardado de formatos. Se mantienen empleado textual, tipos/longitudes/defaults/nulabilidad, JSON completo, secuencias serial/bigserial y relaciones locales. No se inventó FK desde el user_id integer histórico de tokens hacia users.id bigint ni hacia identidades/jerarquías externas.
- El guardado usa una transacción EF compartida con el bloqueo SQL de UR, revisión del contexto y auditoría. Version es token de concurrencia; se conserva el 409 y el cálculo de avance del servidor. Los SQL especializados de publicación y reservas mantienen propietario/caducidad antes del commit y latido sin renovación.
- Autorización mediante política ASP.NET, AuthorizationHandler y `IAuthorizationMiddlewareResultHandler`: revalida Nexo antes de HTML/JSON y conserva denegaciones, mensajes y auditoría. Se usa el mismo formateador de DomainProblem para middleware y política. Continúan el binding/ModelState MVC, filtros, DI, Options, cookie de sesión, antiforgery y rate limiting.
- Migraciones estándar `20261002051005_InitialTdv2` y `20261002051340_InitializePausedSynchronization`, con designers y snapshot. Historial único vigente: `public."__EFMigrationsHistory"`. La primera exige base vacía; la segunda sólo inserta configuración pausada. El modelo conserva todas las restricciones semánticas anteriores: dos unicidades nullable pasan de constraints UNIQUE a índices UNIQUE y se añade el índice de la FK de ejecución activa.
- Se retiró el ejecutor `--migrate`, sus recursos SQL compilados y el historial SQL como mecanismo vigente. SQL 001–004 queda exclusivamente en tests/TDV2.NativeVerification/LegacySchema; el DDL de conversión anterior, en TransitionSchema como fixture. El único archivo database/transition vigente es el diagnóstico de lectura.
- Local-Tdv2 conserva DPAPI administrativo, marcadores y Verify-Cluster, e invoca dotnet ef sólo para su destino aislado. Si encuentra la base local anterior sin historial EF, la conserva y exige revisión. Prepare-Transition mantiene restauración en una copia nueva y diagnóstico READ ONLY; `-Apply` SQL está retirado. La futura adopción de una copia real requiere revisión EF, no bootstrap automático.
- Se conservan tdv2.slnx, TDV2 HTTPS + React, HMR, UserSecretsId, configuración estándar, https://localhost:7136/connect y todas las pantallas/componentes/tema/tipografías existentes. No se editaron credenciales ni se leyó Herd en esta adaptación. README contiene comandos desde la raíz y equivalencias con Laravel.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore` | 0 errores y 0 advertencias |
| `dotnet ef migrations has-pending-model-changes --no-build --project tdv2` | Sin cambios de modelo pendientes; migrations list descubre las dos migraciones sin conectar |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [25/25 en PostgreSQL desechable](docs/migracion/evidencia-ef-migraciones.json): equivalencia SQL, tipos/defaults/secuencias, relaciones/índices, cuenta sin superusuario, repetición, contenido previo conservado, rollback/reanudación/Down/Up, migraciones pendientes concurrentes, script idempotente y concurrencia EF |
| `dotnet run --project tests/TDV2.Verification --no-build --no-restore` | 56/56 dominio/transporte; incluye dobles en memoria, no equivale a PostgreSQL |
| `Test-NativePostgres.ps1` final tras ajustar el resultado de autorización | [90/90 HTTP/PostgreSQL + un grupo navegador](docs/migracion/evidencia-ef-postgresql.json), 91 grupos correctos; fuentes institucionales sintéticas |
| React/Edge en la suite anterior | [9 recorridos formatos](docs/migracion/evidencia-ef-react-formatos.json) + [12 acceso](docs/migracion/evidencia-ef-react-acceso.json) + [10 sincronizaciones](docs/migracion/evidencia-ef-react-sincronizaciones.json), cero errores JS |
| TypeScript, `test:forms` y `build` de ClientApp | Correctos; 21/21 pruebas frontend, sin cambios de pantallas ni lockfile |
| F5 real desde EnvDTE en Visual Studio | [Compilación 2 correctos, implementación 1 correcta y depuradores .NET/JavaScript](docs/migracion/evidencia-ef-visual-studio.txt); solución/perfil conservados |
| `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, redirección 302, HTTPS confiable, React/props, privado 401 y HMR wss en 7136](docs/migracion/evidencia-ef-visual-studio-react.json); sin OAuth ni consultas institucionales |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Paquete generado con npm ci/build; auditoría npm informó 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production/HTTPS Windows, React compilado sin Vite, 503 sin credenciales, 401 privado, 419 sin CSRF, cero errores JS](docs/migracion/evidencia-ef-publicacion.json) |
| Verificador de configuración `--production` | Production no carga User Secrets; no se imprimieron valores ni se abrieron fuentes |
| Scripts PowerShell y `git diff --check` | Sintaxis y whitespace correctos. No se ejecutó Prepare contra la base local histórica |

Los intentos fallidos no cuentan como resultados: el sandbox impidió iniciar PostgreSQL/restaurar algunos paquetes y acceder a User Secrets, por lo que se ejecutaron las operaciones necesarias con aprobación. Se ajustó el verificador a la excepción PostgreSQL envuelta por EF. Dos bootstrap simultáneos sin historial provocaron una carrera del proveedor: **la primera aplicación tiene un solo operador**; las pendientes con historial se serializan con el bloqueo estándar EF/Npgsql. Un ensayo concurrente intentó recompilar una DLL en uso y se repitió secuencialmente. F5 detectó una pausa de Just My Code al propagar una denegación por el framework; se corrigió usando el resultado estándar de autorización y se repitieron F5, 56 pruebas y toda la regresión PostgreSQL/React. El primer publish encontró Rolldown bloqueado por el Vite remanente del IDE; se identificó y detuvo sólo ese proceso TDV2 antes de publicar correctamente.

Clúster final de migraciones: `.artifacts/native-postgres-20261002-053757-b02b1e9f`. Regresión final: `.artifacts/native-postgres-20261002-055330-864db610`. Todos los clústeres de esta ejecución quedaron detenidos. Se detuvo la depuración iniciada para verificar F5; el IDE y los documentos del usuario se conservaron.

### Aplicado en la base definitiva autorizada

- [Preflight inmediatamente anterior](docs/migracion/evidencia-ef-servidor-preflight.json): `SERVIDOR_TDV2:5432/tdv2_db`, `USUARIO_TDV2`, TLS confirmado, Search Path public, CONNECT/USAGE/CREATE y propietario verificados. Base vacía, sin objetos ajenos ni historial. La inicialización SQL anterior ya había sido eliminada por la recreación del usuario; no se adoptó contenido ni se eliminó ninguna base/conexión.
- Comando aplicado: `dotnet ef database update --no-build --project tdv2 -- --environment Development`, con ConnectionStrings:Tdv2 de User Secrets. [Resultado](docs/migracion/evidencia-ef-servidor-aplicacion.txt) y [verificación](docs/migracion/evidencia-ef-servidor-verificacion.json): ambas migraciones registradas, 14 tablas operativas más historial EF, 0 pendientes.
- Se repitió **el mismo comando**: [salida](docs/migracion/evidencia-ef-servidor-repeticion.txt) y [metadatos posteriores](docs/migracion/evidencia-ef-servidor-repeticion.json). Sin cambios de esquema, historial ni contadores. La huella de columnas/defaults/restricciones/índices/secuencias coincide también con [el ensayo sintético](docs/migracion/evidencia-ef-esquema.json).
- Todas las tablas operativas tienen 0 filas salvo sincronizacion_configuracion, que tiene la fila 1 pausada; automática/ILDA desactivadas, próxima ejecución y reservas nulas. Sin usuarios, sesiones, intentos OAuth, catálogos, formatos, auditoría ni trabajos sintéticos remotos. No se abrió Nexo/SII/ILDA ni se inició procesador.

### Pendiente institucional y operativo

Validar Microsoft/Graph/Entra reales, contratos y revocaciones Nexo, conectividad/tipos/volumen SII e ILDA; revisar una eventual copia autorizada con datos históricos y preparar su adopción EF; aceptación visual con fuentes externas, teclado/móvil/impresión; despliegue Ubuntu con proxy, supervisor, llaves persistentes y recuperación. Las pruebas Windows y los proveedores sintéticos no acreditan esos puntos. No se habilitaron sincronizaciones automáticas ni se desplegó la aplicación.

**Los apartados siguientes son evidencia histórica. Los comandos y mecanismos vigentes son los del bloque EF y del README; no reutilizar los ejecutores SQL/DPAPI retirados.**

## Inicialización SQL anterior del servidor — 2026-10-01 (histórica)

### Implementado

- Comando `--migrate=check|apply` en `tdv2/Migrations`, separado antes de componer el host web. Carga los proveedores normales y User Secrets en Development; sólo utiliza `ConnectionStrings:Tdv2`. Exige host/puerto/base/usuario explícitos y los contrasta con la conexión y con PostgreSQL; comprueba TLS remoto, search_path, escritura, CONNECT/USAGE/CREATE y propietario de tablas administradas. No inicia procesos de sincronización ni servicios institucionales.
- Reutiliza los bytes originales de 001–004 como recursos compilados; sólo SQL de la raíz, sin transition. SHA-256 íntegro, secuencia sin huecos, historial como prefijo verificado antes de escribir, exclusión entre sesiones y transacción por archivo incluyendo `public.tdv2_schema_migrations` (nombre/hash/fecha/usuario). Rechaza objetos preexistentes sin historial, cambios de hash y sincronización activa/reservada. El sitio no ejecuta DDL al arrancar.
- `Local-Tdv2.ps1` queda intacto, con Verify-Cluster, DPAPI, marcadores y tdv2_local_migrations. User Secrets no se editó ni se reemplazó: el usuario lo actualizó para la base recreada antes de esta tarea. Interfaz, F5 y Herd se conservaron. README contiene el comando exacto y el procedimiento para nuevas migraciones.

### Probado y ejecutado

| Verificación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | Compilación sin errores ni advertencias tras corregir la prueba nueva |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [19/19 en PostgreSQL desechable](docs/migracion/evidencia-migrador-postgresql-sintetico.json): cuenta sin superusuario, check sin DDL, primera aplicación, repetición/fechas, destino erróneo, base ocupada, hash cambiado, permisos, dos conexiones, rollback inicial/posterior/al registrar hash, reanudación, automática, COMMIT interno y errores sin secretos |
| Preflight del servidor, sólo metadatos | [Destino exacto SERVIDOR_TDV2:5432 / tdv2_db / USUARIO_TDV2](docs/migracion/evidencia-postgresql-servidor-preflight.json), PostgreSQL 18.6 Ubuntu, TLS confirmado, proveedor secrets.json, public vacío y permisos suficientes |
| Aplicación real autorizada | [001, 002, 003 y 004 aplicadas](docs/migracion/evidencia-postgresql-servidor-aplicacion.json); 14 tablas TDV2 más historial, claves primarias/índices válidos/restricciones validadas y sincronización pausada |
| Segunda ejecución del mismo apply | [0 aplicadas, 4 omitidas, 0 pendientes](docs/migracion/evidencia-postgresql-servidor-repeticion.json); misma huella del esquema |
| Comparación de metadatos | [Columnas, tipos, nulabilidad y defaults coinciden con el ensayo desechable](docs/migracion/evidencia-postgresql-servidor-verificacion.json); automática/ILDA desactivadas, próxima ejecución y reservas nulas |

La primera ejecución sintética fue impedida por el sandbox al iniciar PostgreSQL; se repitió fuera del aislamiento sobre un clúster nuevo y sólo dentro de `.artifacts`. Otra ejecución detectó que `inet::text` incluye máscara; se corrigió a `host(inet_server_addr())` y se obtuvo 19/19 antes de conectar al servidor. Los clústeres desechables quedaron detenidos. No se ejecutaron SQL de transition, cargas sintéticas remotas, login ni consultas a Nexo/SII/ILDA. Las únicas inserciones de bootstrap remoto son la configuración pausada prevista en 004 y los cuatro registros del historial.

### Pendiente

Inicialización e idempotencia verificadas en el servidor autorizado. No se acredita con ello el login institucional, conectividad Nexo/SII/ILDA, conversión de datos anteriores, publicación Ubuntu del sitio ni cierre de la migración funcional. Las evidencias anteriores sobre bases locales conservan su alcance histórico; `ConnectionStrings:Tdv2` vigente apunta ahora al servidor por decisión del usuario.

## Corrección del arranque de Visual Studio — 2026-10-01

### Implementado

- Se obtuvo de la instancia abierta el [error completo de implementación](docs/migracion/evidencia-vs-implementacion-antes.txt): configuración vacía de `launch.json`, `Value cannot be null. Parameter name: source`. ClientApp no tenía `.vscode/launch.json`; JSPS fallaba antes de ejecutar npm. El rechazo TCP de ReactShell era consecuencia de no haber Vite escuchando.
- Nuevo `ClientApp/.vscode/launch.json` para Edge; perfil compartido con `DebugTarget` explícito de cada proyecto y directorio de trabajo explícito en `.esproj`. Se conserva `npm run dev` y su dirección estricta `127.0.0.1:5173`. `https` no abre un segundo navegador; `https-compiled` sí conserva su apertura independiente.
- JSPS comprueba el puerto del destino antes de iniciar ASP.NET. El destino de implementación usa Vite y su ruta de desarrollo `/__vite/__launch`, que espera la salud local de Kestrel y redirige al HTTPS fijo. Espera acotada, respuesta genérica si no arranca; no se publica esa ruta. Edge conserva el depurador JavaScript y el mapeo de fuentes a través de `/__vite/`.
- [Sobrescrituras verificadas](docs/migracion/evidencia-vs-configuracion.json): `appsettings.Development.json` conserva `UseVite=false`, sobrescrito por `ReactDevelopment__UseVite=true` del perfil `https`. Sin otra clave React en User Secrets ni variables ambientales del proceso de comprobación/usuario/equipo. UserSecretsId, las nueve claves, el retorno `https://localhost:7136/connect`, interfaz y datos permanecen; Herd no se leyó ni modificó.
- La modificación previa de `tdv2.slnx` (Build/Deploy de ClientApp y organización) ya estaba presente al iniciar este trabajo y se conservó.

### Probado

| Comprobación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | 0 errores, 0 advertencias |
| F5 real mediante EnvDTE `Debug.Start` en VS Community 2026 18.10.3 | [Implementación 1 correcta, 0 con errores](docs/migracion/evidencia-vs-implementacion-despues.txt); depuradores .NET y JavaScript, portada Transformación Digital; arranque en frío con 5173/7136/5064 inicialmente libres |
| `node tests/browser/visual-studio-startup.mjs` desde ClientApp, contra los procesos del IDE | [Vite 200, arranque 302, HTTPS confiable, portada y React renderizados, props 200, HTML privado 401, WebSocket HMR en 7136 y cero errores JS](docs/migracion/evidencia-vs-react.json); fuentes externas bloqueadas |
| `dotnet run --project tests/TDV2.LocalConfigurationVerification --no-build --no-restore -- <raíz>` | 9 claves desde User Secrets; cadenas analizadas sin abrir conexiones, callback/base/ILDA conservados |
| `node node_modules/typescript/bin/tsc --noEmit` y `npm.cmd run build` desde ClientApp | Correctos; Vite 8.3.2 y dependencia/lockfile conservados |

Se usó el Node ya incluido en Visual Studio para los verificadores; el IDE ejecutó su propio `npm run dev`. No se ejecutó StartDatabase, SQL, migración, sincronización ni inicio de sesión. Los intentos intermedios no se cuentan como éxito: el IDE requirió recargar el nuevo perfil; un destino que apuntaba directamente a 7136 falló durante Implementar y un destino Node no inició Vite. La configuración final usa Edge con comprobación de 5173 y espera de Kestrel.

### Pendiente

Este fallo de F5 queda verificado en el IDE. No se validaron breakpoints en todas las pantallas, autenticación institucional ni aceptación visual completa con fuentes externas. Continúan los pendientes funcionales de la migración descritos debajo; no se declara la migración terminada.

## Preparación para mantenimiento y Visual Studio — 2026-10-01 (evidencia anterior)

### Implementado

- Program.cs queda centrado en el host y la selección explícita del procesador. Controllers atiende HTTP; Services coordina formatos, colaboradores, Microsoft y contextos; Infrastructure conserva Npgsql y SQL local. Se extrajeron consultas de colaboradores/sincronizaciones y middleware de errores/autorización. No se añadieron proyectos .NET, ORM ni autenticación de demostración. Se conservan rutas, JSON, 419/409 y límites por operación.
- Comentarios y contratos XML en español explican UR/empleado textual, administrador combinado, intersección local/Nexo, identidad real/efectiva, tokens sólo de servidor, avance, revisión de contexto, transacciones y reservas. El latido sigue sin renovar reservas; no se movieron commits fuera de sus comprobaciones ni de la auditoría.
- ClientApp.esproj reutiliza React mediante el sistema JavaScript de Visual Studio. tdv2.slnLaunch inicia ClientApp y tdv2; launchSettings conserva 7136/5064 y ofrece https con Vite y https-compiled. El proxy de desarrollo /__vite/ usa SpaServices.Extensions; OAuth, cookies, JSON, CSRF y HMR conservan el origen 7136. No rediseño.
- Configuración general y límites en appsettings, opciones de desarrollo en appsettings.Development, credenciales en User Secrets y puertos en launchSettings. UserSecretsId y las nueve claves importadas se conservan. Tras verificar el reemplazo, se retiró el código que leía institutional.clixml: Import/Configure/MigrateSecrets sólo orientan al editor. DPAPI permanece para administrar el clúster aislado y proteger llaves ASP.NET de Windows. User Secrets no es cifrado ni fuente de producción.
- Publicación del backend compila React mediante npm ci/build y empaqueta wwwroot, sin Vite de producción. DataProtection:KeyDirectory permite llaves persistentes externas. El paquete dependiente de ASP.NET Runtime se genera con UseAppHost=false. Preparación Ubuntu documentada, sin instalar ni desplegar.
- Ensayo separado database/transition + Prepare-Transition.ps1: sólo admite una copia local identificada, valida esquema/claves/relaciones y datos básicos, conserva catálogos/respuestas con huellas transaccionales, aísla tokens Laravel y añade 002/003. Rechaza reservas y reaplicación; no usa 001/004 sobre Laravel. Pruebas sintéticas en el proyecto nativo existente. **Falta acceso/copia real autorizada de tdv2_db; no se respaldó ni consultó esa base.**
- README y guía local reorganizados. Historial DPAPI/User Secrets anterior trasladado a HISTORIAL_LOCAL.md, conservando su evidencia. Guías específicas de transición y publicación. Se registró el estado inicial en Git (`0e12049`) antes de incorporar los cambios revisables; no se publicó a un remoto.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| Compilación de la solución | 0 errores/advertencias; ejecuciones indicadas con NuGetAudit=false |
| TDV2.Verification | 56/56, dominio/HTTP con dobles en memoria |
| Test-NativePostgres.ps1 | 91/91 grupos: 90 HTTP/SQL y un grupo con 31 recorridos Edge; PostgreSQL aislado real, proveedores institucionales sintéticos |
| Test-UserSecrets.ps1 | Nueve claves efectivas; ediciones conservadas ante comandos retirados y archivo original restaurado byte por byte; sin conexiones remotas |
| Verificador con --production | Production no carga User Secrets; credenciales externas requeridas |
| Ensayo de transición final | 16/16; respaldo/restauración sintéticos, contrato, rollback y validador real de formatos |
| Regresión con dependencias corregidas | 31 recorridos Edge repetidos; sin repetir ni sumar los 90 casos SQL |
| TypeScript / pruebas frontend | TypeScript sin errores; 21/21 casos conservados |
| Test-Development.ps1 | Cuatro comprobaciones HTTPS/React/401/CSRF/Microsoft 302, más HMR sin navegación, cookies seguras, rechazo de HTML privado y error 503 controlado con Vite detenido |
| dotnet publish + publish-flow.mjs | Paquete Production inicia con HTTPS y React compilado sin Vite; /connect 503 sin secretos, /inicio 401 y CSRF 419 |
| Conservación por hashes | 30 archivos de interfaz/estilos/portada/logos/SQL 001–004 sin cambios; 154/154 archivos de referencia Laravel sin cambios |

Evidencia: [regresión PostgreSQL](docs/migracion/evidencia-organizacion-postgresql.json), [regresión del navegador con el lockfile final](docs/migracion/evidencia-organizacion-regresion-navegador.json), [arranque y redirección local](docs/migracion/evidencia-organizacion-arranque.json), [configuración](docs/migracion/evidencia-user-secrets.json), [Vite y HMR](docs/migracion/evidencia-vite.json), [publicación](docs/migracion/evidencia-publicacion.json), [conservación](docs/migracion/evidencia-conservacion-organizacion.json) y [transición sintética](docs/migracion/evidencia-transicion-sintetica.json). La interfaz/depurador de Visual Studio **no se operó**; los perfiles se ejecutaron por CLI. El 302 Microsoft no siguió la redirección ni acreditó login real. Los navegadores bloquearon fuentes web externas; no son aceptación visual exacta.

La primera preparación del fixture de transición falló por agrupar SQL parametrizado con múltiples sentencias; se separó el INSERT parametrizado y se repitió. La primera carga fría de Vite agotó 30 segundos durante la optimización; se ajustó la espera y se verificó después de restaurar dependencias. No se cuentan los intentos fallidos como éxitos. Los clústeres de prueba quedaron detenidos; el clúster local de validación permanece disponible para F5, sin automática ni trabajadores. Se conservó su usuario ya existente: los contadores observados fueron 1 usuario, 0 formatos, 0 UR y 0 ejecuciones; no se consultaron identidades ni se cargaron fixtures allí.

### Retirado y motivo

- Cinco archivos Web/*Endpoints.cs: reemplazados por controladores, servicios y consultas; regresión HTTP/SQL/navegador pasada.
- scripts/Import-LaravelSettings.ps1, scripts/Test-LocalImport.ps1 y lector institucional DPAPI de Local-Tdv2.ps1: reemplazo en User Secrets comprobado. Se conservan evidencia histórica y credenciales cifradas originales sin lecturas del arranque.
- tdv2/tdv2.http: plantilla de WeatherForecast sin ruta correspondiente.
- Microsoft.AspNetCore.OpenApi: paquete sin uso, reemplazado por la dependencia concreta del proxy Vite.
- concurrently: ninguna referencia de ejecución; el inicio múltiple lo gestiona Visual Studio. La auditoría inicial npm encontró 8 vulnerabilidades; se aplicaron correcciones compatibles del lockfile. Vite incluía un [aviso de rutas Windows](https://github.com/advisories/GHSA-fx2h-pf6j-xcff). La [auditoría posterior](docs/migracion/evidencia-npm-audit.json) informó cero. Vite pasó de 8.0.8 a 8.3.2 y PostCSS de 8.5.10 a 8.5.28; React 19.3.0, MUI 9.4.0 y TypeScript 5.9.3 permanecieron iguales ([versiones](docs/migracion/evidencia-dependencias.json)). La publicación, HMR y los 31 recorridos de navegador se repitieron con el lockfile corregido.

### Pendiente concreto

Operar el perfil compartido/F5 en la interfaz de Visual Studio; validar Entra/Microsoft/Graph y permisos institucionales Nexo; obtener y contrastar una copia autorizada de tdv2_db (tipos/longitudes/secuencias/datos históricos/UTC); conectores SII/ILDA reales; aceptación visual con fuentes, teclado/móvil/impresión; ejecución Ubuntu, proxy, supervisor, llaves persistentes y recuperación operativa. **No se desplegó ni se declara terminada la migración institucional.**

Comandos actuales y ubicaciones: [README](README.md), [arranque Visual Studio](docs/ARRANQUE_LOCAL_WINDOWS.md), [transición](docs/migracion/TRANSICION_DATOS.md), [publicación](docs/PUBLICACION.md). No usar el historial como instrucciones para reimportar configuración.

## Inspección y límites

Preparaciones anteriores: [historial local](docs/migracion/HISTORIAL_LOCAL.md). Se conservan resultados, limitaciones y referencias; sus lanzadores/importadores anteriores no son instrucciones vigentes.

- Inventario: 27 rutas propias, 27 tablas declaradas, 119 métodos PHP y 12 pruebas frontend originales. **154/154 hashes de Herd siguen iguales** al manifiesto inicial. Las pruebas PHP son criterios de aceptación; no se ejecutaron.
- Para este bloque se revisaron antes de implementar CollaboratorController, RepresentationController, PreviewController, ConfigurationController, NexoDelegation, NexoRepresentation, RepresentationAudit, PreviewSession/ViewContext, FormAccess, middleware, rutas/configuración y pruebas asociadas.
- Para sincronizaciones se revisaron InstitutionalSource/InstitutionalSync, IldaSource/IldaSync/IldaInventory, SyncManager/SyncController, comandos, configuración y migraciones 2026_09_25_000001/2026_09_30_000001, además de InstitutionalSourceTest, InstitutionalSyncTest, IldaInventoryTest y ConfigurationSyncTest. Contratos y diferencias: [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md).
- Contratos, diferencias y recuperación: [DELEGACION_REPRESENTACION.md](docs/migracion/DELEGACION_REPRESENTACION.md). Configuración de identidad/SQL: [AUTENTICACION_POSTGRESQL.md](docs/migracion/AUTENTICACION_POSTGRESQL.md). Inventario original: [INVENTARIO.md](docs/migracion/INVENTARIO.md).

## Implementado

### Base, autenticación y formatos conservados

- React, TypeScript, nueve pantallas, MUI, tema, CSS, declaración de tipografías, portada y navegación originales. Comunicación JSON del mismo origen sustituye Inertia. El historial sólo guarda URLs, nunca props ni identidades.
- Microsoft conserva /connect inicio/callback, tenant GUID, state de un uso con 600 segundos de vigencia, vínculo al navegador y PKCE S256. Canje e identidad Graph /me desde servidor, dominio institucional y object ID GUID; Nexo se valida antes de persistir. Logout revoca la sesión y redirige al destino Microsoft fijo.
- Sesión cifrada PostgreSQL de dos horas, cookie Secure/HttpOnly/SameSite=Lax y CSRF. Graph cifra/refresca tokens con bloqueo; foto fallida no concede permisos ni termina sesión.
- Nexo sigue siendo autoridad. Aplicación única resuelta por **clave literal tdv2**, ID positivo publicado y nivel 2; identidad individual, empleado textual, roles, módulos y parentesco/ruta vigentes. No se aceptan identidad, roles o UR de React como autorización.
- Formatos sólo para UR activas nivel 2/3. Áreas subordinadas a nivel 3 comparten su formato cuando están autorizadas; no se crean formatos por áreas inferiores. Administrador conserva consulta institucional y edición limitada a su rama nivel 2, incluso combinando roles.
- Respuestas canónicas, JSON, versión, avance calculado en servidor, autor y timestamps. GET no crea filas; bloqueo de UR serializa primera creación y actualización. Versión obsoleta devuelve 409; error SQL revierte el cambio.
- Autorización en HTML y endpoints, revalidada por solicitud. Nexo caído no concede permisos de respaldo.

### Administración delegada

- Búsqueda, alta y retiro mediante vistas y funciones publicadas por Nexo. Nexo permanece Laravel; no se escriben sus tablas internas.
- Intersección de responsabilidad/adscripción válida con nexo_delegacion; niveles_delegacion=[2], como la configuración Laravel. Ser administrador no basta para delegar.
- Roles asignables sólo desde nexo_delegacion_roles; rol_id enviado por React se ignora. Nexo comprueba persona/origen/alcance y, tras el alta central, TDV2 vuelve a leer identidad publicada antes de crear el vínculo.
- Colaborador local: formato 2/3 correspondiente a su adscripción. Colaborador de áreas dependientes/global: formatos de la rama nivel 2 otorgante. Siempre se requiere vínculo local y concesión central vigente coincidente.
- Altas concurrentes serializadas por persona/origen/rol. Retiro local y auditoría se confirman antes de llamar Nexo; fallo central devuelve 202, conserva acceso revocado y muestra retiro pendiente con reintento. Se conserva la concesión si otro vínculo local activo la utiliza.
- Búsquedas distinguen vacío, falta de autorización y fallo de conexión, sin convertir errores en listas vacías.

### Actuar como usuario y vista por rol/área

- Ambas herramientas permanecen en Configuración → Pruebas de acceso. Configuración exige administrador y módulos vigentes; representación además exige capacidad independiente de Nexo.
- Buscar/iniciar/validar/finalizar usan nexo_a{ID}_representacion(jsonb), actor real en servidor y token cifrado. Delegación durante representación usa esa misma función con actor real + token; nunca suplanta directamente al actor ante las funciones de delegación.
- Estado efectivo verificado en servidor por solicitud: actor/persona/ID, caducidad local y central, autorización vigente, módulos, duración y escritura. No se amplían permisos si Nexo falla, revoca o devuelve datos ambiguos.
- Identidad Microsoft real separada del perfil representado. Franja original muestra persona, actor, vigencia, consulta/escritura y salida. Caducidad/revocación bloquean el acceso; volver a permisos propios requiere salida explícita. La pantalla de acceso restringido también conserva un botón para terminar el contexto.
- Vista de prueba sólo escenario por rol/área, 30 minutos y consulta. No acepta modo usuario/email; bloquea escrituras y búsqueda delegada. Configuración, contextos anidados y mutaciones desde pestañas con contexto obsoleto se rechazan en servidor.
- Contexto cifrado/revisión por sesión en tdv2_access_contexts. Bloqueos y comparación de revisión evitan sustituciones concurrentes durante escrituras. Dos inicios simultáneos admiten uno y rechazan el otro.
- DELETE de salida y logout siguen disponibles ante falla de Nexo, con CSRF. Se descarta el contexto local; se registra si el cierre central pudo confirmarse.

### Auditoría y persistencia local

- Cambios propios y representados registran actor real, identidad representada cuando aplica, acción, recurso, contexto y resultado. Se registran también búsquedas y rechazos del bloque, incluido CSRF. No se serializan tokens, cabeceras, credenciales, errores SQL ni el motivo libre.
- Guardado de formato, alta/revocación/retiro local, inicio/salida de contexto y logout comparten transacción con su auditoría. Si la bitácora falla, el cambio local se revierte.
- No hay transacción distribuida entre TDV2 y Nexo. Un alta central puede permanecer sin vínculo local tras un error; no habilita edición y se recupera por reintento. Un inicio central sin commit local intenta finalizarse. Retiros y cierres centrales fallidos conservan estados explícitos; ver límites en el documento de contratos.
- database/001_core.sql es bootstrap sólo de base vacía; 002_aspnet_sessions.sql añade sesiones/OAuth y **003_access_contexts.sql** el estado cifrado. No convertir ni aplicar sobre bases existentes. La aplicación nunca ejecuta DDL al arrancar.
- Llaves Data Protection bajo tdv2/.runtime/keys, DPAPI Windows; las pruebas usan llaves efímeras. Nuevo login necesario para cookies Laravel y tickets ASP.NET anteriores a este bloque.

### Configuración → Sincronizaciones

- Conectores de producción SQL Server/MySQL con consultas fijas: SII sólo `poa.UNIDADES_RESPONSABLES_POA`, diez columnas y MAX(EJERCICIO) por defecto; ILDA toda `ilda_db.informacion_area`, todas las filas/columnas en JSON original y auxiliares locales. No hay escrituras remotas ni consultas remotas al abrir formatos.
- Descarga completa antes de publicar, validación de columnas/IDs/ejercicio/ciclos, límites 100000 filas y 128 MiB ILDA, reducción superior al 20% rechazada. Bajas lógicas mantienen relaciones/respuestas/versiones. Empleado SII conserva ceros y se compara con Nexo textual.
- Inventario exclusivamente PostgreSQL con igualdad exacta ur2 ↔ clave; formulario hasta 200 filas, fuente ILDA y Trámite / servicio desde informacion_generada. Incorporar nuevos IDs al GET no persiste ni sustituye respuestas guardadas, aun si el origen cambia o desaparece.
- Cola persistente y manual sii/ilda/ambas (HTTP 202); automática siempre incluye SII, ILDA optativa, intervalos/horario originales y zona America/Ciudad_Juarez mediante base IANA NodaTime. Configuración optimista y autorizada sólo al administrador real con módulos Nexo; rechaza CSRF ausente, vista de prueba y representación en páginas/endpoints.
- Reserva PostgreSQL con propietario UUID, 30 minutos, exclusión entre procesos, comprobación antes y después de cada transacción y recuperación de interrupciones sin publicar desde trabajadores vencidos. Cada catálogo/resultados/auditoría son atómicos; éxito parcial se muestra como parcial. Un cambio concurrente de catálogo antes de guardar formato exige revalidación (409).
- `--sync-worker` opera por separado del sitio, publica latido cada minuto sin renovar por ello la reserva; `--sync-once` hace un ciclo y `--sync-check=sii|ilda|ambas` sólo comprueba. Automática e ILDA deshabilitadas por defecto. No se instalaron tareas, cron ni servicios.
- Pantalla Sincronizaciones idéntica a Laravel salvo import de transporte; historial, progreso, estados, alertas y tipografías conservados. Dependencias, diferencias de tipos MySQL, zona horaria, esquema y operación Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). DDL explícito `database/004_synchronizations.sql` únicamente para la base ASP.NET aislada preparada.

## Evidencia anterior de los bloques funcionales

| Ejecución | Resultado | Alcance |
|---|---|---|
| dotnet build tdv2.slnx -p:NuGetAudit=false | 0 errores, 0 advertencias | Compilación; auditoría NuGet desactivada en esa ejecución |
| dotnet run --project tests/TDV2.Verification --no-restore | **56/56** | Dominio/HTTP con identidad, Nexo y almacenamiento en memoria sintéticos |
| npm.cmd run types:check | Código 0 | TypeScript |
| npm.cmd run test:forms | **21/21** | 12 originales + 9 de transporte; Axios/navegador simulados |
| npm.cmd run build | Código 0 | Bundle Vite |
| scripts/Test-NativePostgres.ps1 | **91/91 grupos** | 90 casos HTTP/SQL (30 del bloque) + un grupo de 31 recorridos en Edge |
| scripts/Test-NativePostgres.ps1 -SyncOnly (ajuste final SII) | **30/30** | Tras conservar booleanos textuales SII como 1/0; fuentes ADO.NET sintéticas |
| dotnet publish tdv2/tdv2.csproj -c Release --no-restore -o .artifacts/publish | Código 0 | Paquete construido; CLI inválida devuelve 2 sin conectar a fuentes |

PostgreSQL **18.6 nativo para Windows**, Edge **154.0.4258.37** sin interfaz. SQL, transacciones, bloqueos, cookies, CSRF y pantallas reales; Microsoft simulado por HTTP y Nexo simulado mediante vistas/funciones publicadas en PostgreSQL. La cuenta Nexo de pruebas sólo lee vistas y ejecuta funciones autorizadas, sin acceso directo a tablas internas. No se probó el publicador Laravel institucional. SII/ILDA usan lectores ADO.NET sintéticos con el SQL/mapeo/publicación de producción: no hubo conexión TDS/MySQL ni validación TLS o tipos nativos institucionales.

Se validaron loopback, puerto no convencional, base, usuario y SHOW data_directory antes de escribir. La suite creó un clúster nuevo dentro del destino y **lo dejó detenido**. No inició/detuvo servicios PostgreSQL existentes ni tocó sus bases.

Evidencia actual sin credenciales:

- [90 casos PostgreSQL y grupo de navegador](docs/migracion/evidencia-sincronizaciones-postgresql.json).
- [10 recorridos del módulo Sincronizaciones](docs/migracion/evidencia-sincronizaciones-navegador.json).
- [Regresión de 12 recorridos de acceso](docs/migracion/evidencia-sincronizaciones-regresion-acceso.json) y [9 recorridos de autenticación/formatos](docs/migracion/evidencia-sincronizaciones-regresion-formatos.json).
- [30 casos tras el ajuste final de conversión SII](docs/migracion/evidencia-sincronizaciones-ajuste-conector.json).

Suite completa: `.artifacts/native-postgres-20261001-065537-4cce3a7f/`. Ajuste posterior de conector: `.artifacts/native-postgres-20261001-070037-0c2a3c8e/`. Capturas: sincronizaciones-completada.png, sincronizaciones-historial.png y formato-ilda-local.png. La comprobación posterior -SyncOnly verifica el ajuste puntual de booleanos SII (1/0, igual que Laravel); no repite ni se suma a los 31 flujos del navegador.

Evidencia histórica de delegación:

- [60 casos PostgreSQL y grupo navegador](docs/migracion/evidencia-delegacion-postgresql.json).
- [12 recorridos de colaboradores/representación/vista/auditoría](docs/migracion/evidencia-delegacion-navegador.json).
- [9 recorridos de regresión de autenticación/formatos](docs/migracion/evidencia-regresion-navegador.json).

Artefactos históricos de delegación: `.artifacts/native-postgres-20261001-060357-784fc391/`, incluidos JSON, logs sintéticos y capturas representacion-activa.png, vista-rol-area.png, formato-postgresql.png y ur-denegada.png. Los informes anteriores [PostgreSQL](docs/migracion/evidencia-postgresql.json) y [navegador](docs/migracion/evidencia-navegador.json) se conservan como evidencia histórica del bloque anterior.

Casos verificados de sincronizaciones:

- SELECT fijo SII, ejercicio único, diez columnas, empleado textual y espacios; vacío, duplicados, ciclos, descarga interrumpida/incompleta, tamaño y caída inesperada conservan el catálogo anterior.
- ILDA completa (más de 200 registros), columnas adicionales, nulos/vacíos/binarios reversibles, coincidencia exacta de claves. GET sólo local incluso con fuente caída/deshabilitada; agrega IDs sin persistir y conserva respuestas/versiones tras cambios y desapariciones.
- Exclusión entre solicitudes y trabajadores, 202 sin descarga HTTP, recuperación de proceso hijo terminado, reserva vencida antes/durante publicación y trabajador viejo incapaz de liberar/publicar sobre reserva nueva. Latido no prolonga reserva.
- Publicación por fuente transaccional; ambas direcciones de fallo parcial, errores SQL/auditoría revierten catálogo/metadatos/resultado, solicitud y configuración. Comprobación CLI sin cola/publicación; reintentos explícitos convergen.
- Programación diaria y cambio estacional Juárez, intervalo y zona inválidos, dos versiones concurrentes, automática siempre SII/ILDA opcional, intervalos perdidos agrupados y activación ILDA validada.
- Administrador real, módulos padre/hijo, Nexo, CSRF y contexto exigidos en páginas y endpoints. Vista de prueba y representación rechazan configuración; cambio SII concurrente exige revalidar antes de guardar.
- React muestra pendiente, ejecutando, completada, parcial y fallida; conserva borrador de programación ante 409, historial y opciones automáticas, recupera tras error SQL/interrupción y guarda información ILDA local sin tocar MySQL. **Cero errores JavaScript de página** en los tres scripts.

Casos conservados de delegación y representación:
- Búsqueda permitida/vacía/denegada/fallida; administrador sin delegación, empleado no coincidente, rol no asignable, nivel 3 no delegable (criterio histórico sustituido el 2026-10-05 por colaboración local en su rama), persona/origen/rama forjados, administrador combinado sin ampliación.
- Persona de nivel 4 autorizada en formato nivel 3; colaborador local/global; GET/PUT de UR ajenas o inferiores denegados. Identidad no publicada tras alta impide vínculo; reintento y altas simultáneas convergen.
- Revocación local persiste si Nexo falla; reintento confirma retiro. Falla inducida en auditoría revierte alta y revocación locales.
- Administrador sin capacidad no representa; capacidad sin escritura no la concede. Búsqueda fuera de alcance, token cifrado/no publicado, actor real intacto, módulos revocados, escritura/consulta y alcance.
- Vencimiento local y central, revocación, Nexo no disponible, pestaña obsoleta, contextos anidados y dos inicios simultáneos. Regreso explícito al actor, incluido Nexo caído.
- Formato y auditoría atómicos durante representación; fallo de auditoría al iniciar intenta compensación central. Logout representado revoca sesión/contexto y audita atómicamente; CSRF rechazado identifica ambas personas sin cambios.
- Vista por rol/área sólo consulta; modo usuario, área inválida, búsqueda delegada, configuración y escrituras directas rechazados. Caducidad no restaura permisos reales automáticamente.
- Navegador: altas/retiros y reintento, mensajes diferenciados, ramas local/global, herramientas separadas, capacidad ausente, representación de consulta/escritura, autoguardado, revocación/vencimiento, salida con Nexo caído, vista de prueba y elusión de endpoints desde el navegador. PostgreSQL confirma actor/representado y ausencia de tokens en auditoría. **Cero errores JavaScript de página** en ambos scripts.

La regresión conserva OAuth/state/PKCE, identidad Microsoft, aplicación/roles/módulos Nexo, logout/CSRF, UR, guardados concurrentes/versionado, rollback/reintento y Graph cifrado/refresco. Son 31 recorridos en tres scripts (9 + 12 + 10), contenidos en un solo grupo; no se cuentan otra vez como grupos HTTP.

Tema, CSS, layout y lógica de directorio conservados (layout con sustitución de import de transporte). Ajustes puntuales en páginas y texto “áreas dependientes” documentados; no hay rediseño. Capturas inspeccionadas como evidencia funcional, **no comparación visual exacta con Herd**. Todas las solicitudes externas del navegador se bloquean, incluidas fuentes web; no acredita descarga de tipografías, MFA ni SSO real.

Ejecuciones fallidas previas se conservaron en .artifacts y no se cuentan como éxitos. Se corrigió una ambigüedad de variable en el publicador SQL sintético y un error productivo de lectura de confirmaciones delegadas de una columna; el retiro/reintento fue repetido exitosamente. En este bloque fallaron inicialmente cuatro casos por ausencia de la zona IANA en Windows (resuelto con NodaTime) y una espera prematura de carga en la prueba de dos pestañas (corregida). Una ejecución diagnóstica simultánea no pudo compilar porque Windows mantenía la DLL abierta; se repitió secuencialmente. Los fallos no cuentan como evidencia de éxito. No se ampliaron permisos para hacer pasar las pruebas.

## Comandos reproducibles

Desde ClientApp:

```powershell
npm.cmd ci --ignore-scripts
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
```

Desde la raíz del destino:

```powershell
dotnet build tdv2.slnx -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

La última orden requiere PostgreSQL nativo, Edge y certificado ASP.NET HTTPS ya existente. Usa C:\Program Files\PostgreSQL\18\bin; admite -PostgresBin. En esta sesión fue necesaria aprobación para ejecutarlo fuera del sandbox. Crea siempre un clúster nuevo, con contraseña sintética aleatoria no impresa, vacía el archivo de contraseña inicial y detiene su clúster en finally. Conserva datos/logs sintéticos. **-SkipBrowser ejecuta 90 casos HTTP/SQL; -BrowserOnly ejecuta 31 recorridos; -SyncOnly ejecuta los 30 casos del bloque sin navegador**. Ninguna ejecución parcial equivale a la suite completa. El host HTTPS lee el certificado existente; no instala certificados.

No ejecutar dos suites mientras una recompila la misma salida en Windows: el ejecutable mantiene DLL abiertas. Operación manual/continua, cancelación, reserva de 30 minutos, recuperación y comandos Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). No se instalaron tareas ni servicios.

Los proyectos de verificación son ejecutables con salida no cero al fallar, no suites descubiertas por dotnet test. Construir ClientApp antes del navegador. Los controles de fallos/datos sólo existen en el ejecutable de pruebas; no hay autenticación de demostración ni endpoints de fixture en producción.

Revalidar el manifiesto de Herd, sólo lectura:

```powershell
$reference = 'C:\Users\Jesus Arenas\Herd\tdv2'
$manifest = Get-Content -Raw -Encoding UTF8 docs/migracion/fuente-manifiesto.json | ConvertFrom-Json
$changed = @($manifest | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $reference $_.path)).Hash.ToLowerInvariant() -ne $_.sha256
})
if ($changed.Count) { throw 'La referencia cambió; revisar el inventario.' }
$manifest.Count
```

## Pendiente de validación real y configuración faltante

- **Microsoft institucional:** TenantId, ClientId, ClientSecret y PublicOrigin local ya trasladados a User Secrets desde DPAPI. Pendientes callback registrado, cuentas autorizadas, vigencia del secreto, consentimiento/User.Read/offline_access, MFA/acceso condicional, Graph/rotación reales y logout SSO. El 302 local no valida esos aspectos. No publicar secretos en archivos o conversación.
- **Nexo Laravel institucional:** conexión importada, sin probar. Pendientes red/TLS, publicación tdv2, vistas filtradas y EXECUTE de las funciones públicas de delegación/representación (contrato Nexo 9.6.0 citado por Laravel). Sin validar firmas/tipos efectivos, publicador Laravel, idempotencia real, duración/formato de selección, responsables/roles asignables, revocaciones desde Nexo, concesiones compartidas ni auditoría central. Pruebas que alteren concesiones centrales requieren una publicación/personas autorizadas para pruebas.
- **SII/ILDA institucionales:** conexiones importadas, sin probar ni ejecutar. Pendientes acceso autorizado SELECT, certificados TLS cuando correspondan y muestra de tipos/volumen. Sin validar protocolo TDS/MySQL, autenticación real, configuración de servidor, ejercicio desplegado, tipos/precisión/texto de fechas ILDA, claves/empleados reales ni rendimiento institucional. No basta el lector ADO.NET sintético para acreditar el conector de red.
- **Procesador en despliegue:** comandos Windows/Ubuntu documentados; no se ejecutó Ubuntu, SIGTERM, supervisor, reinicio de máquina ni operación prolongada. No se habilitó ningún servicio/tarea. El fallo de fuente se probó por excepciones/lecturas incompletas, no por cortar una conexión remota real.
- **Datos existentes:** intactos y sin consultar. Ensayo sintético de conversión disponible; falta ejecutarlo sobre una copia real autorizada y contrastar esquema/tipos/índices, colaboraciones, actividad y UTC, además del corte/reversión institucional. No reutilizar cookies/cifrado Laravel ni aplicar el bootstrap sobre una base existente.
- **Operación y recuperación:** reinicio con llaves persistidas bajo cuenta del servicio, varias instancias, rotación/respaldo, limpieza de sesiones/contextos caducados, proxy/HTTPS, límites distribuidos y caída física de motor/red. Los triggers/fallos SQL no representan pérdida de confirmación de COMMIT. No hay reconciliador automático entre TDV2/Nexo.
- **Paridad documentada:** el identificador Microsoft de sesión no se regenera como en Laravel; se usa contexto cifrado con revisión atómica y rechazo de contexto obsoleto. Auditoría extendida a operaciones propias; motivo libre omitido. Login conserva bitácora básica separada de la persistencia de identidad; falta acordar metadatos históricos, retención y consulta institucional de auditoría.
- **Interfaz:** comparación visual con fuentes descargadas, móvil/teclado, impresión/exportación y borradores al caducar sesión. Capturas funcionales y código conservado no certifican estos aspectos.

## Bloques restantes

| Bloque | Estado |
|---|---|
| B0 — inventario y trazabilidad | Listo a nivel de código; esquema desplegado no consultado |
| B1 — dominio/módulos/esquema/avance | Implementado; falta paridad exhaustiva institucional |
| B2 — transporte/CSRF/formatos | Flujo principal y regresión verificados; visual y despliegue pendientes |
| B3 — Microsoft/identidad/Nexo/Graph | Implementado con proveedores simulados; **validación institucional pendiente** |
| B4 — colaboradores/delegación | Búsqueda/alta/retiro/recuperación implementados y probados con funciones sintéticas; **Nexo real pendiente** |
| B5 — Configuración/pruebas/representación/auditoría | Vista y representación separadas, permisos y auditoría local implementados y probados; **aceptación institucional pendiente** |
| B6 — ILDA/UR SII | Implementado y probado con copia PostgreSQL real/fuentes sintéticas; **conectividad, tipos y aceptación institucional pendientes** |
| B7 — programador/recuperación | Cola, reserva, caducidad, exclusión, parcial/rollback y recuperación implementados/verificados; **operación institucional/Ubuntu pendiente**, sin tareas instaladas |
| B8 — integración/corte | PostgreSQL aislado/navegador disponibles; servicios reales, visual, despliegue y corte pendientes |

Las rutas de sincronización ya tienen implementación, permisos y pruebas. Queda optimizar el listado PostgreSQL para evitar una consulta por formato. La migración exige evidencia de todos los bloques y aceptación institucional; compilar o pasar simuladores no cumple esos criterios.
