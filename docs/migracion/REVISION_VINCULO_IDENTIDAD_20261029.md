# Revisión de necesidad de la migración de identidad — 2026-10-29

## Conclusión y decisión local

**Reconocer UPN/SMTP no requiere una tabla nueva. Evitar duplicados de la cuenta Microsoft local tampoco:** ya existen `users.microsoft_tenant_id`, `users.microsoft_id` y su índice único. La corrección de búsqueda por `(tid,oid)` y la consulta de los alias verificados reutilizan esos campos.

**Falta persistir a qué `usuario_id` canónico de Nexo se acreditó ese objeto Microsoft.** El modelo previo sólo permite conservar el ID local, el objeto Microsoft y un correo mutable. Ninguna relación existente une de forma duradera esos identificadores con el usuario central.

Por ello se conserva **pendiente y sin aplicar** `20261029163206_VerifiedInstitutionalIdentityLinks`. No es redundante con el esquema existente para cumplir también los requisitos vigentes de conservar la identidad canónica al cambiar el correo y rechazar una correspondencia contradictoria, incluso tras cerrar sesión. No se vuelve a modificar la autenticación ni la corrección de alias en esta revisión.

La **tabla separada no es la única solución técnica**: agregar a `users` una referencia canónica Nexo nullable, única y con su procedencia sería otra alternativa. También exigiría una migración y adaptar el flujo. No se puede obtener esa garantía reutilizando únicamente los campos actuales. No se introduce una segunda propuesta de esquema ni se retira la persistencia ya preparada para reemplazarla por un vínculo basado sólo en correo.

Si el alcance se limitara a una coincidencia puntual UPN/SMTP en cada login, podría prescindirse de persistir el ID central; se perdería la posibilidad de distinguir una reasignación de correo de una continuidad legítima después de finalizar la sesión. Ese alcance reducido no conserva todos los criterios anteriores del usuario.

## Inventario comprobado en el código

Esta revisión inspecciona modelos, configuraciones, migraciones y contratos fuente. **No es una inspección del esquema o los datos de las bases institucionales.**

| Recurso existente | Qué identifica | Por qué no sustituye al vínculo canónico |
| --- | --- | --- |
| `users.id` | Cuenta local TDV2 | Su secuencia es local; no es `usuarios.id` de Nexo. No se pueden igualar numéricamente. |
| `users.microsoft_tenant_id` + `users.microsoft_id` | Tenant/objeto Microsoft de la cuenta local | Ya son únicos y se reutilizan. No contienen el ID Nexo. |
| `users.email` | Correo vigente de la cuenta local | Es mutable; la unicidad del correo no acredita continuidad del ID central si se reasigna. |
| `ms_graph_tokens.user_id` | Cuenta local propietaria del token | No es un ID Nexo. Ni los tokens ni el correo de esa fila son un directorio central. |
| `tdv2_sessions.ticket` | Sesión cifrada con caducidad | Puede contener el claim canónico durante esa sesión; logout la elimina y la expiración impide reutilizarla. No resuelve el siguiente login de forma duradera. |
| `tdv2_access_contexts` | Selección de contexto o representación de una sesión | Es temporal y describe al actor/representado; no acredita una identidad institucional permanente. |
| `activity_logs` | Hechos históricos de actividad | Auditoría no es un registro obligatorio ni único de vínculos. Login se registra por correo/nombre y su escritura es de mejor esfuerzo. No se debe convertir una bitácora en autoridad de identidad. |
| Catálogo UR / colaboraciones | Empleado, adscripción, responsabilidad y concesión | No contienen el par Azure ni el ID central de todas las personas. Una adscripción o concesión no autentica al usuario. |
| `cd_surveys` / `cd_consents` | Captura y aceptación por empleado, con contexto histórico | No contienen el vínculo canónico Azure–Nexo. Pueden no existir antes de responder, son exclusivos del diagnóstico y sus históricos no deben mutarse para iniciar sesión. |
| `public.nexo_usuarios` | `usuario_id`, `usuario_aplicacion_id`, correo, tipo, empleado y adscripción | Publica el ID canónico real, que se reutiliza; no publica `tid/oid` ni una relación institucional estable con Azure. |

Fuentes principales: `tdv2/Domain/Entities/User.cs`, `Infrastructure/Configurations/UserConfiguration.cs`, `GraphToken.cs`, `SessionTicket.cs`, `AccessContext.cs`, `ActivityLog.cs`, `Competencias/DigitalDbContext.cs`, `Security/PostgresTicketStore.cs` y `Security/AuthenticationAudit.cs`. El `User.cs` central y `PostgresPublication.php` de Nexo se leyeron como referencia; no se ejecutó Nexo ni se modificó su repositorio.

`email_verified_at`, `remember_token` y `password` no almacenan un ID Nexo. Reutilizarlos para ese fin cambiaría sus contratos y escondería un nuevo modelo de datos en columnas ajenas.

## Ejemplos concretos de lo que falta

Identificadores y direcciones siguientes son sintéticos.

1. El objeto Microsoft `(T,O)` se acredita con el usuario Nexo **20**, cuyo correo es `persona@ejemplo`.
2. Se cierra la sesión. La fila local previa conserva `(id_local,T,O,persona@ejemplo)`, sin ID central.
3. Nexo deja de publicar al 20 y publica al **21** con ese mismo correo. El perfil Microsoft conserva ese UPN/SMTP.
4. Una consulta por correo obtiene al 21. Con los campos locales anteriores no es posible demostrar que el vínculo previo era con el 20. La coincidencia actual puede ser única y, aun así, contradecir la identidad anterior.
5. Con el vínculo persistente a **20**, el conflicto se rechaza; no se toman permisos del 21.

Otro caso: el usuario central 20 cambia de correo antes de que su primera cuenta local vuelva a iniciar sesión. Un segundo objeto Microsoft que coincida con el correo nuevo no colisionaría necesariamente con `users_email_key` ni con `users_microsoft_identity_unique`: ambos pares/correos locales podrían ser distintos. La unicidad del ID central en el vínculo impide acreditar dos objetos como el mismo usuario Nexo.

Por último, si el usuario 20 cambia legítimamente de correo y ya no conserva los alias anteriores, buscar por el ID central acreditado permite encontrar la misma persona. Guardar sólo el correo anterior obliga a denegar o a volver a inferir la identidad desde direcciones mutables.

## Qué modifica exactamente la migración pendiente

Exclusivamente en **Tdv2DbContext / base TDV2**, crea `public.tdv2_identity_links`:

| Columna | Tipo | Finalidad |
| --- | --- | --- |
| `microsoft_tenant_id` | UUID, no nulo | Tenant validado |
| `microsoft_id` | UUID, no nulo | Objeto validado |
| `nexo_user_id` | bigint, no nulo | `usuario_id` existente publicado por Nexo |
| `verified_at` | timestamp with time zone, no nulo | Fecha de acreditación |
| `verified_by` | varchar(255), no nulo | Método/verificador |
| `verification_reference` | varchar(512), no nulo | Procedencia de la comprobación, sin secretos |

Incluye:

- PK compuesta `(microsoft_tenant_id, microsoft_id)`.
- Índice único `IX_tdv2_identity_links_nexo_user_id`.
- CHECK `identity_link_verified`: ID central positivo y verificador/referencia no vacíos.
- `REVOKE ALL ON TABLE public.tdv2_identity_links FROM PUBLIC`.
- El registro estándar que EF mantiene en `__EFMigrationsHistory` **sólo si un operador la aplica posteriormente**.

No añade secuencias, datos iniciales, roles, usuarios Nexo ni claves foráneas entre bases. No altera `users`, catálogos, respuestas, informes o `DigitalDbContext`. No cambia las migraciones previas. `Down` describe únicamente eliminar esta tabla nueva; en esta revisión no se ejecutó Up ni Down.

La cuenta web necesita SELECT/INSERT en la tabla propuesta, no UPDATE/DELETE/TRUNCATE. Los permisos por sitio siguen consultándose en Nexo y no se guardan en este vínculo. La propiedad y concesiones operativas pendientes están detalladas en la [guía de implementación](IDENTIDAD_MICROSOFT_ALIAS_20261029.md#persistencia-y-operación-posterior).

## Verificación de esta revisión, sin migrar

Nuevo modo aislado del verificador:

```text
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --review-identity-links .artifacts/identity-link-review-20261029.json
```

Resultado: **compilación sin errores/advertencias y 23 comprobaciones correctas**.

- Metadatos EF: índice Microsoft existente, falta del ID canónico en el modelo anterior, restricciones del vínculo y operaciones exactas de la migración propuesta. Se consultan `Model`, `UpOperations` y `DownOperations`; no se invoca `Migrate`, `EnsureCreated` ni ningún comando SQL.
- Parser productivo Microsoft: UPN y SMTP pertinentes conservados; SIP excluido, sin depender de una tabla para reconocerlos.
- HTTP real mediante ASP.NET TestServer: `/accesos` y entrada a Procedimientos con TD exclusivo, Competencias exclusivo, ambos y ninguno. Sólo se consulta el mismo ID canónico, sin búsqueda alternativa por permisos.
- Servicios productivos `DigitalAccess.Require` y `NexoActorIdentity.ForActor`: encuesta/informe autorizados para el participante correcto, denegación independiente, revocación y falla temporal diferenciadas en ambos sitios.
- Reasignación sintética de correo: resolver por correo devolvería al 21; resolver al actor canónico 20 rechaza esa sustitución en ambos sitios.

Evidencia: [identity-link-review-20261029.json](../../.artifacts/identity-link-review-20261029.json).

Microsoft, identidad autenticada, Nexo y catálogos son dobles sintéticos. **En esta revisión no se abrió PostgreSQL, no se aplicó ninguna migración y no se ejecutó OAuth/Graph institucional.** Se prueba el acceso del backend y el selector; no se repite la captura ni un recorrido visual completo. Las pruebas PostgreSQL de la entrega anterior son evidencia histórica separada, no ejecuciones de esta revisión.

Archivos cambiados ahora: nuevo `tests/TDV2.Verification/IdentityLinkReviewTests.cs`, opción del ejecutable en `EntryPoint.cs`, este documento y referencias en README, ESTADO y GUIA. No se cambia código productivo ni se elimina trabajo local.

El caso real de alias, el registro efectivo AuthSID y los consentimientos siguen pendientes de comprobación institucional. `SII_LECTURA_O_INTEGRIDAD` continúa sin causa confirmada. Sin push ni despliegue.
