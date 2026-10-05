# Configuración de Nexo y Entra

## Aplicación y módulos

Mantener **tdv2**, su ID y conexión/base existentes, en **nivel 2: módulos por rol**. No recrear recursos ni registrar nuevas claves de módulos/colaboradores.

| Rol | Módulos |
|---|---|
| `administrador` | `procesos_operativos`, `configuracion`, `sincronizaciones`, `pruebas_acceso` |
| `responsable_ur` | `procesos_operativos` |
| `responsable_ur_supervisor` — Responsable de UR con supervisión | `procesos_operativos` |
| `colaborador_local` | `procesos_operativos` |
| `colaborador_dependencias` | `procesos_operativos` |

Rutas existentes: `/inicio`, `/configuracion`, `/configuracion/sincronizaciones`, `/configuracion/pruebas-acceso`. Sincronizaciones y Pruebas de acceso son hijos de Configuración. Conservar los roles de consulta que tengan usuarios.

Supervisor funciona solo: responsabilidad por empleado textual/datos institucionales, captura en su UR y dependientes cuando es responsable de nivel 2; nivel 3 captura su formato. Añade consulta institucional, sin edición/envío/delegación ajenos ni Configuración, Sincronizaciones, Pruebas de acceso o representación. Administrador conserva su límite adicional de rama de nivel 2 aunque combine roles.

## Transición sin afectar otras aplicaciones

La fuente confirma que `roles.aplicacion_id` es local a la aplicación y `roles.catalogo_rol_id → catalogo_roles` puede ser compartido. `ControladorCatalogos.saveRole` propaga nombre/descripción a todas las aplicaciones vinculadas. **No renombrar ese catálogo para cambiar únicamente TDV2.** No se consultaron asignaciones personales de la base institucional.

1. En Catálogos → Roles, localizar o registrar `responsable_ur_supervisor`, nombre **Responsable de UR con supervisión**; adjuntarlo a TDV2 desde sus Roles. Comprobar existencia antes de crear.
2. Para `responsable_ur_institucional` o `supervisor`, revisar aplicación, catálogo vinculado, otras aplicaciones, asignaciones, concesiones, módulos, delegación y representación antes de cualquier transición.
3. TDV2 admite **`responsable_ur_institucional` como alias temporal**, preservando asignaciones, vínculos e ID. No exige ambos roles. Mantenerlo operativo durante la transición.
4. El genérico `supervisor` conserva sus asignaciones y significado anterior; no se eleva automáticamente por su nombre. Tras comprobar su función, el operador puede conceder el rol canónico a las mismas personas/UR mediante administración central, verificar el acceso y planear la retirada posterior del anterior con las herramientas de Nexo. Este cambio no elimina roles de consulta ni concesiones previas.
5. La UI actual de Nexo adjunta roles de catálogo y no ofrece un renombrado independiente seguro. Si es obligatorio conservar exactamente el ID al cambiar clave/catálogo, preparar una transición específica revisando relaciones; no ejecutar un UPDATE global ni retirar primero el rol antiguo.

## Administración delegada

En configuración de delegación de TDV2:

- **Delegantes:** `responsable_ur`, `responsable_ur_supervisor`, `administrador`; conservar temporalmente `responsable_ur_institucional` mientras tenga asignaciones.
- **Asignables:** `colaborador_local` y **`colaborador_dependencias`**, clave vigente comprobada en fuente. No crear variantes ni un colaborador global.
- Nivel 2: ambos tipos dentro de su rama. Nivel 3: sólo local, personas de su UR o descendientes, exclusivamente para su mismo formato. No se generan formatos de niveles inferiores.
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
