# Configuración de Nexo y Entra

## Aplicación y módulos

### Renombrado del módulo existente — primera etapa 2026-10-08

1. **Primero** instalar TDV2 compatible con `procedimientos_institucionales` y `procesos_operativos`. Ambos se autorizan desde el acceso efectivo publicado por Nexo para TDV2, ruta `/inicio`; no hay permisos inferidos del nombre. Si se reciben ambas claves, se presenta una sola entrada.
2. **Después** editar el módulo actual: nombre **Procedimientos Institucionales**, clave **`procedimientos_institucionales`**, ruta **`/inicio`**. Conservar **ID**, jerarquía y todas sus relaciones/asignaciones por rol. No crear otro módulo, aplicación, conexión o base. La aplicación permanece `tdv2`, nivel 2. `configuracion_procesos` no cambia.
3. Publicar mediante el flujo actual de Nexo. Comprobar con las herramientas existentes la fila de `nexo_modulos` (mismo ID, ruta y jerarquía), las relaciones en `nexo_modulo_rol` y el acceso efectivo de responsables, supervisor, colaboradores y administrador. Verificar inicio/listado/formato y ausencia de módulos ajenos; retirar un permiso debe seguir retirando su acceso. El alias se mantiene durante esta transición.

No se ejecutaron estos pasos sobre Nexo real. La matriz siguiente usa la clave nueva; antes del paso 2 es válida la clave anterior en las mismas asignaciones. La primera etapa restringe captura, no modifica roles, delegación, representación ni participación de áreas. [Detalles y migración local de TDV2](PRIMERA_ETAPA.md).

Mantener **tdv2**, su ID y conexión/base existentes, en **nivel 2: módulos por rol**. No recrear recursos ni duplicar claves. La ampliación autorizada es `configuracion_procesos`; no cambia las claves de colaboradores.

| Rol | Módulos |
|---|---|
| `administrador` | `procedimientos_institucionales`, `configuracion`, `configuracion_procesos`, `sincronizaciones`, `pruebas_acceso` |
| `responsable_ur` | `procedimientos_institucionales` |
| `responsable_ur_supervisor` — Responsable de UR con supervisión | `procedimientos_institucionales` |
| `colaborador_local` | `procedimientos_institucionales` |
| `colaborador_dependencias` | `procedimientos_institucionales` |

Rutas existentes: `/inicio`, `/configuracion`, `/configuracion/sincronizaciones`, `/configuracion/pruebas-acceso`. Sincronizaciones y Pruebas de acceso son hijos de Configuración. Conservar los roles de consulta que tengan usuarios.

**Nuevo submódulo:** clave `configuracion_procesos`, nombre **Configuración procesos**, ruta `/configuracion/procesos`, icono `mdi-tune`, padre **ID real de `configuracion`**, rol **administrador**. Publicar la relación y ambos permisos mediante las herramientas existentes de Nexo. TDV2 rechaza padre/ruta incorrectos, falta de cualquiera de los módulos y contextos de representación o vista de prueba. No hay cambios en Entra. [Migración, participación y conservación de alcances](PARTICIPACION_RENDIMIENTO_PRESENCIA.md).

Supervisor funciona solo: responsabilidad por empleado textual/datos institucionales, captura en su UR y dependientes cuando es responsable de nivel 2; nivel 3 captura su formato. Añade consulta institucional, sin edición/envío/delegación ajenos ni Configuración, Sincronizaciones, Pruebas de acceso o representación. Administrador conserva su límite adicional de rama de nivel 2 aunque combine roles.

## Transición sin afectar otras aplicaciones

La fuente confirma que `roles.aplicacion_id` es local a la aplicación y `roles.catalogo_rol_id → catalogo_roles` puede ser compartido. `ControladorCatalogos.saveRole` propaga nombre/descripción a todas las aplicaciones vinculadas. **No renombrar ese catálogo para cambiar únicamente TDV2.** No se consultaron asignaciones personales de la base institucional.

1. En Catálogos → Roles, localizar o registrar `responsable_ur_supervisor`, nombre **Responsable de UR con supervisión**; adjuntarlo a TDV2 desde sus Roles. Comprobar existencia antes de crear.
2. Para `responsable_ur_institucional` o `supervisor`, revisar aplicación, catálogo vinculado, otras aplicaciones, asignaciones, concesiones, módulos, delegación y representación antes de cualquier transición.
3. TDV2 admite **`responsable_ur_institucional` como alias temporal**, preservando asignaciones, vínculos e ID. No exige ambos roles. Mantenerlo operativo durante la transición.
4. El genérico `supervisor` conserva sus asignaciones y significado anterior; no se eleva automáticamente por su nombre. Tras comprobar su función, el operador puede conceder el rol canónico a las mismas personas/UR mediante administración central, verificar el acceso y planear la retirada posterior del anterior con las herramientas de Nexo. Este cambio no elimina roles de consulta ni concesiones previas.
5. La UI actual de Nexo adjunta roles de catálogo y no ofrece un renombrado independiente seguro. Si es obligatorio conservar exactamente el ID al cambiar clave/catálogo, preparar una transición específica revisando relaciones; no ejecutar un UPDATE global ni retirar primero el rol antiguo.

## Colaboradores: dos vías independientes (2026-10-06)

La fuente vigente de Nexo distingue el origen en **`public.nexo_concesiones.origen`**: `central` se produce en `AccessAssignments::grant` al asignar desde la administración central; `aplicacion` procede de la administración delegada. `nexo_usuario_rol` sólo publica roles efectivos y **no prueba** que sean centrales. TDV2 exige una concesión central explícita cuyo `rol_id` coincida con el rol efectivo de la persona; no interpreta otros valores, ni la ausencia de concesiones, como asignación central.

| Vía | Configurar en Nexo | Alcance en TDV2 |
|---|---|---|
| Central, `colaborador_local` | En la administración central de la aplicación TDV2, asignar ese rol a la cuenta individual. Autorizar `procedimientos_institucionales` (o su alias durante la transición) para el rol y comprobar empleado/adscripción vigentes. | Primer formato participante desde su adscripción hacia sus ascendientes por `ID_UR`/padres, sin sobrepasar su rama de nivel 2. Inicialmente participan niveles 2 y 3. |
| Central, `colaborador_dependencias` | Igual, con el rol vigente de dependencias. | Formatos participantes de su rama institucional de nivel 2; inicialmente su raíz y dependientes de nivel 3. La raíz estructural no necesita participar para resolver la rama. |
| Delegada, ambos roles | Mantener roles delegantes/asignables y publicación de funciones descritos abajo; conceder desde Colaboradores en TDV2. | Vínculo local no revocado + concesión `aplicacion` coincidente. Conserva límites de encargado nivel 2/3 y validación de empleado, adscripción, alcance y otorgante. |

La concesión central no necesita otra alta en Colaboradores de TDV2. No se crean vínculos al entrar. `id_ur_acceso` conserva la UR del otorgamiento; para esta vía no sustituye la **adscripción personal vigente** de `nexo_usuarios.ID_UR`. Un traslado recalcula el alcance en la siguiente solicitud, si Nexo mantiene el acceso efectivo; una suspensión institucional lo bloquea. La clave visible `CVE_UR` (con o sin ceros) no interviene en la autorización. La participación vigente decide niveles/tipos, inicialmente 2/3 y exclusión N, permitiendo tipo 0. Los nodos excluidos se conservan para resolver jerarquías; habilitar otro nivel no concede roles ni facultades sobre superiores.

Retirar la concesión central retira únicamente su aportación. Una delegada revocada localmente sigue bloqueada aun si el retiro en Nexo está pendiente; no se transforma en central por conservar el rol efectivo. Si hay una concesión central independiente, conserva sólo su propio alcance. Las asignaciones históricas que aparezcan únicamente en `nexo_usuario_rol`, sin procedencia explícita, **no obtienen acceso directo**: el operador debe revisar y regularizar el otorgamiento desde la administración central, sin inventar origen ni crear enlaces locales ficticios.

Estos roles solos no habilitan envío definitivo, administración de colaboradores, Configuración ni representación. Se conserva la composición con responsabilidades autorizadas; administrador mantiene su límite de rama aunque combine roles. Actuar como usuario usa concesiones y adscripción del representado; la vista de prueba continúa en consulta.

### Publicación necesaria

La fuente local revisada ya expone `concesion_id`, `rol_id`, `id_ur_acceso`, `origen` y `email` en `PostgresPublication.php` → `nexo_concesiones`, filtrando cuenta/aplicación/rol vigentes, suspensión y revocación. **No se necesita un contrato nuevo ni cambios de código en Nexo.** TDV2 usa también `nexo_usuarios`, `nexo_usuario_rol`, `nexo_modulos` y `nexo_modulo_rol`; no consulta tablas privadas.

El operador debe verificar que la conexión existente publique esas vistas y tenga permiso de lectura. Si su publicación está desactualizada, aplicar la publicación existente desde Nexo, sin recrear recursos. El ajuste mínimo para una publicación antigua que omita el origen es actualizar la vista de concesiones al contrato vigente; no añadir un origen supuesto a todos los roles. TDV2 deniega si no puede leer/comprobar las concesiones. No se inspeccionó ni actualizó la publicación institucional durante este trabajo.

Pruebas, límites y comandos: [COLABORADORES_CENTRALES.md](COLABORADORES_CENTRALES.md).

## Administración delegada

En configuración de delegación de TDV2:

- **Delegantes:** `responsable_ur`, `responsable_ur_supervisor`, `administrador`; conservar temporalmente `responsable_ur_institucional` mientras tenga asignaciones.
- **Asignables:** `colaborador_local` y **`colaborador_dependencias`**, clave vigente comprobada en fuente. No crear variantes ni un colaborador global.
- Nivel 2: ambos tipos dentro de su rama. Nivel 3: sólo local, personas de su UR o descendientes, exclusivamente para su mismo formato. La delegación sigue limitada a formatos 2/3 que participen; habilitar otros niveles no amplía estas concesiones ni sus roles delegantes. Excluir su formato conserva el vínculo sin trasladarlo.
- Se exige encargado institucional coincidente, cuenta individual, acceso efectivo/publicación vigentes y rama válida. Los roles de responsable, supervisor y administrador no son asignables mediante este flujo.

La fuente de `AdministracionDelegada.php` y `DelegacionPostgres.php` restringe roles delegantes/asignables **sólo para TDV2**. Las funciones `conceder_acceso.sql`/`retirar_acceso.sql` conservan las comprobaciones por nivel; representación hereda estas validaciones. Otras aplicaciones mantienen sus reglas.

Cambios de esta entrega: [parche adicional](nexo-supervision.patch), [fuentes comprobadas](evidencia-supervision-nexo-fuentes.json) y [siete pruebas con SQL generado por Nexo](evidencia-supervision-nexo.json). Se aplicaron al código fuente conservando las modificaciones previas de nivel 3; todavía deben publicarse en la conexión existente mediante el operador de Nexo.

Publicar las vistas/funciones con el flujo existente de aplicación de la conexión PostgreSQL de Nexo, **sin eliminar ni recrear conexión, aplicación o base**. Confirmar que `nexo_delegacion` publique correo/empleado/UR/nivel coincidentes y que `nexo_delegacion_roles` publique los IDs reales de ambas claves. Guardar configuración sin aplicar su publicación no cambia el SQL consumido por TDV2.

## Representación

Mantener Actuar como usuario sólo para `administrador`, con `pruebas_acceso`, su padre Configuración y capacidad publicada por Nexo. No añadir supervisor a `representacion_roles`. Un administrador que no sea encargado debe usar un objetivo y escritura autorizados por Nexo para probar delegación. Se auditan cuenta real y efectiva; el token nunca llega a React. La vista por rol/área sólo consulta.

## Microsoft Entra

1. En la aplicación existente: Token configuration → Add optional claim → **ID token → `login_hint`**. No usar email/preferred_username como logout_hint.
2. Conservar tenant, client ID, secreto, callback Web **https://localhost:7136/connect** y scopes `openid profile offline_access User.Read`; mantener origen/callback HTTPS propios de producción.
3. Verificar el retorno de cierre de sesión del origen público y probar SSO/salida con la política institucional y dos cuentas Microsoft. MFA/políticas de Entra pueden seguir requiriendo interacción.

TDV2 valida firma RS256, emisor, audiencia, caducidad, nonce y correspondencia `oid/tid` con Graph antes de conservar el hint opaco en el ticket cifrado. El inicio habitual no fuerza selección. La salida revoca primero la sesión local; una cookie protegida, Secure/HttpOnly y breve transporta únicamente el hint validado a la redirección Microsoft. **Usar otra cuenta** es un POST con CSRF que revoca localmente e inicia selección explícita. Los hints siempre son de la cuenta Microsoft real.

Para `login_hint` se usa ese claim cuando existe, o el correo real validado por Graph si no existe. Para `logout_hint` se admite exclusivamente el claim opcional del ID token: su ausencia omite el parámetro, nunca lo sustituye por correo ni por la identidad representada.

Referencias oficiales: [login_hint opcional](https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims-reference), [logout sin selector innecesario](https://learn.microsoft.com/en-us/troubleshoot/entra/entra-id/app-integration/sign-out-of-openid-connect-oauth2-applications-without-user-selection-prompt), [cliente SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-10.0).
