# Identidad Microsoft, alias y usuario canónico — 2026-10-29

**Revisión posterior de necesidad:** la tabla propuesta no es necesaria para leer alias ni para evitar duplicados Microsoft locales. Persiste la correspondencia con el ID Nexo que no existe en el modelo previo; se conserva pendiente, sin aplicar. [Inventario, alternativas, operaciones exactas y verificación sin migraciones](REVISION_VINCULO_IDENTIDAD_20261029.md).

## Causa comprobada y alcance

El código anterior solicitaba `id,mail,userPrincipalName,displayName`, elegía `mail` cuando no estaba vacío y consultaba Nexo por esa dirección. No solicitaba `proxyAddresses`. Además, `PostgresIdentity.Save` buscaba primero por correo: si cambiaba `mail`, intentaba insertar otra fila aun cuando `(microsoft_tenant_id,microsoft_id)` ya identificaba al usuario. El índice único evitaba el duplicado, pero fallaba el inicio de sesión. Un registro antiguo con ambos identificadores vacíos se vinculaba sólo por correo, sin comprobar las alternativas verificadas ni la unicidad institucional.

Se reprodujeron estas limitaciones en PostgreSQL desechable antes de modificar producción: tres comprobaciones dirigidas fallaron en `.artifacts/native-postgres-20261029-162625-80450a81`. Esto acredita el defecto del código; **no acredita todavía el contenido de Graph ni la causa del incidente de una persona real**.

La precisión posterior del usuario autoriza resolver la identidad institucional por UPN o SMTP verificado y único aunque Microsoft tenga otro correo de trabajo. Sustituye la propuesta intermedia de exigir una acreditación manual para todos los vínculos. La implementación final aplica ese criterio antes de consultar roles; no selecciona una cuenta porque tenga más permisos.

El campo **Chat** de una ficha Microsoft no demuestra por sí mismo que la dirección sea UPN o SMTP. Si sólo aparece como `SIP:`, se ignora. No se inspeccionaron tokens, secretos, sesiones reales ni bases institucionales. El caso concreto proporcionado sigue pendiente de contrastar con `/me` real, con la misma cuenta y aplicación.

## Flujo efectivo encontrado

`Program` usa los proveedores estándar ASP.NET; `ServiceRegistration` enlaza la sección `Microsoft` a `MicrosoftSettings`. El registro efectivo lo determina **`Microsoft:ClientId`** del proceso, no el nombre visible de una captura de Entra. User Secrets está asociado al proyecto; las variables del entorno y los argumentos pueden prevalecer. No se inspeccionaron sus valores ni se consultó el directorio Entra. No se confirma que el registro efectivo sea «AuthSID», ni sus consentimientos efectivos.

El código utiliza OAuth v2 **authorization_code delegado**, aplicación confidencial con secreto de servidor, PKCE S256, state de un uso/cookie y nonce. Renueva mediante `refresh_token`. Solicita exactamente `openid profile offline_access User.Read`. Tener un secreto no convierte el flujo en permisos de aplicación. No utiliza `client_credentials`, `/.default`, permisos de escritura ni envío de correo; no se cambian permisos Azure.

Ahora solicita:

```http
GET https://graph.microsoft.com/v1.0/me?$select=id,displayName,mail,userPrincipalName,proxyAddresses
```

`/me` requiere usuario y permiso delegado; `User.Read` es el permiso mínimo documentado para leer al usuario autenticado. La concesión de `User.Read.All` en una captura no prueba su uso por este cliente. [Microsoft: Get user](https://learn.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0).

| Dato | Tratamiento |
| --- | --- |
| Graph `id` / ID token `oid` | Deben corresponder. Identificador de objeto, no dirección de correo. |
| ID token `tid` | Debe ser el tenant institucional configurado. Se mantiene la validación de firma, emisor, audiencia, vigencia, algoritmo y nonce. |
| `mail` | Correo de perfil. Se conserva para presentación alternativa y detectar contradicciones; por sí solo no acredita un vínculo nuevo. |
| `userPrincipalName` | Dirección de inicio de sesión incluida en la comprobación de correspondencia del perfil autenticado. |
| `proxyAddresses` | Se solicita explícitamente. Sólo `SMTP:` principal y `smtp:` secundarios institucionales; normalización existente, deduplicación, principal distinguido. X500, SIP y direcciones externas no identifican al usuario institucional. |
| `displayName` | Disponible en el perfil; el nombre institucional canónico sigue procediendo de Nexo. |

No se construyen direcciones desde nombres o `mailNickname`. Un `mail` vacío o fuera del catálogo institucional no detiene la revisión de UPN/SMTP; la dirección alternativa de presentación no concede permisos. Colección omitida/malformada, varios SMTP principales o perfil incompatible fallan sin ampliar acceso. Los correos son mutables; el vínculo persistente usa `(tid,oid)`. [Recurso user](https://learn.microsoft.com/en-us/graph/api/resources/user?view=graph-rest-1.0), [claims de ID token](https://learn.microsoft.com/en-us/entra/identity-platform/id-token-claims-reference).

## Resolución antes de autorización

1. Canjear el código, recuperar `/me` y validar criptográficamente el ID token y su correspondencia con Graph/tenant.
2. Buscar el usuario local por `(tid,oid)` y el vínculo persistido con `usuario_id` de Nexo.
3. Consultar todas las direcciones pertinentes en `public.nexo_usuarios` de las publicaciones configuradas de TDV2 y Competencias. Esta consulta no lee roles: devuelve identificador canónico, correo, nombre y tipo de cuenta. Las coincidencias del mismo ID se consolidan. No se omite una coincidencia porque no tenga roles.
4. Si aparecen IDs distintos o contradicen el vínculo estable, rechazar sin elegir el primero ni combinar accesos. Si no hay vínculo previo y una fuente configurada falla, no afirmar unicidad: respuesta temporal. Una fuente no configurada no se inventa como catálogo de identidad.
5. Sin vínculo previo, una coincidencia única de cuenta individual cuyo correo canónico esté en UPN/SMTP verificado permite acreditar el vínculo, según la aclaración del usuario. `mail` aislado, una etiqueta Chat o el dominio institucional no bastan. Sin coincidencia no se inventa identidad Nexo ni acceso.
6. Con vínculo existente, conservar el ID canónico aunque cambien las direcciones. No buscar otra cuenta para conseguir permiso. Una fuente temporalmente caída no sustituye ese vínculo; cada sitio sigue fallando o denegando conforme a su comprobación real.
7. Consultar roles/módulos por el **mismo `usuario_id`** en cada sitio. Las asignaciones y alcances permanecen bajo sus contratos actuales; no hay unión de permisos entre aplicaciones.
8. En una transacción de TDV2, serializar por identidad/correo, comprobar nuevamente el vínculo y las filas locales, insertar sólo el vínculo nuevo acreditado y guardar usuario/tokens. Una cuenta local antigua con ambos IDs vacíos sólo se conserva y completa si coincide el correo canónico acreditado y no hay otra fila/conflicto. Un ID parcial, distinto o varias filas requieren revisión; no se fusionan.

Los vínculos no se actualizan desde login. Las unicidades de `(tid,oid)` y `nexo_user_id` rechazan carreras entre identidades diferentes. La sesión contiene el ID canónico, **sin permisos**, y lo revalida en la lectura SQL local existente de cada solicitud. No hay consulta a Graph por pregunta/autoguardado. La caché del perfil Nexo sigue limitada a la solicitud; las fotos conservan su caché separada por usuario/tenant/objeto. Para acreditar una foto se compara el objeto Microsoft; comparar además `mail` con el correo canónico provocaba un rechazo falso.

Un cambio del correo canónico de Nexo durante una sesión exige volver a autenticar: no se cambia de persona ni se interpreta como permiso adicional. Los registros históricos conservan sus correos/contextos anteriores. El usuario local conserva su ID; las encuestas siguen identificadas por empleado y conservan JSON, instrumento, aviso, aceptaciones e informes. No se reescriben históricos por el cambio de alias.

Mi espacio conserva cero/uno/varios sitios, destinos específicos y mensajes genéricos. Representación conserva actor real canónico y objetivo autorizado; no transmite tokens al cliente. Actividad local y cola Nexo usan el correo canónico resuelto, con el mismo contrato de actividad. Autoguardado no emite eventos centrales; Guardar avance conserva UUID/recibo único.

## Contrato Nexo comprobado y limitación

Se leyó únicamente código local de `Herd/nexo/app/Services/PostgresPublication.php` y modelos/migraciones pertinentes, sin bootstrap ni configuración. La publicación existente expone `u.id AS usuario_id`, `ua.id AS usuario_aplicacion_id`, correo, tipo, empleado y adscripción. Se reutiliza **`usuario_id`**, no el ID de pertenencia ni el número de empleado. No se encontró vínculo publicado con `oid/tid` ni catálogo central de alias verificados.

La unicidad se comprueba dentro de las publicaciones disponibles para las cuentas configuradas. Estas vistas filtran usuarios y pertenencias vigentes; **no certifican usuarios invisibles fuera de esas publicaciones ni sustituyen al directorio central**. Si el operador necesita resolver un usuario no publicado o un conflicto, falta una acreditación institucional del par `(tenant ID, object ID)` con el `usuarios.id` existente y el tratamiento de revocaciones/cambios de identidad. No se inventa una función o columna Nexo para ello. No se crean usuarios, roles o permisos en Nexo.

## Persistencia y operación posterior

Nueva migración propia **`Tdv2DbContext`: `20261029163206_VerifiedInstitutionalIdentityLinks`**. Crea únicamente `public.tdv2_identity_links`, con UUID tenant/objeto, ID canónico bigint, fecha del servidor, método/verificador y referencia de acreditación. PK compuesta y unicidad canónica; sin FK entre bases, contadores nuevos ni copias de perfiles/alias/respuestas. Conserva migraciones anteriores; **DigitalDbContext no cambia**. No rellena vínculos al migrar ni aplica DDL al arrancar.

El login puede insertar una correspondencia nueva tras las comprobaciones anteriores, junto con la identidad/tokens en la misma transacción. La cuenta web requiere **SELECT e INSERT**, nunca UPDATE/DELETE/TRUNCATE en esta tabla. Propietario: cuenta de mantenimiento designada por el operador, distinta del rol web. `PUBLIC` queda sin permisos. Revisar privilegios heredados y por defecto; un propietario conserva facultades aunque se le haga REVOKE. No asignar propiedad de esta tabla al rol web.

No se añaden secretos ni variables. Se conservan:

| Clave | Variable de entorno |
| --- | --- |
| `Microsoft:TenantId` | `Microsoft__TenantId` |
| `Microsoft:ClientId` | `Microsoft__ClientId` |
| `Microsoft:ClientSecret` | `Microsoft__ClientSecret` |
| `Microsoft:PublicOrigin` | `Microsoft__PublicOrigin` |
| `ConnectionStrings:Tdv2` | `ConnectionStrings__Tdv2` |
| `ConnectionStrings:Nexo` | `ConnectionStrings__Nexo` |
| `ConnectionStrings:CompetenciasDigitales` | `ConnectionStrings__CompetenciasDigitales` |
| `ConnectionStrings:CompetenciasDigitalesNexo` | `ConnectionStrings__CompetenciasDigitalesNexo` |
| `CompetenciasDigitales:Nexo:ApplicationId` | `CompetenciasDigitales__Nexo__ApplicationId` |

Antes de habilitar este código en Windows o Ubuntu, el operador debe verificar destino/propietarios, respaldar TDV2 y revisar/aplicar exclusivamente esta migración sobre TDV2; no sobre diagnóstico ni Nexo. Preparación/revisión sin conexión institucional, desde la raíz:

```text
dotnet tool run dotnet-ef migrations script 20261016190000_RemoveUsedMedia 20261029163206_VerifiedInstitutionalIdentityLinks --project tdv2 --context Tdv2DbContext --output .artifacts/verified-identity-links.sql
```

Sólo en la fase operativa autorizada, con configuración segura del destino verificado:

```text
dotnet tool run dotnet-ef database update 20261029163206_VerifiedInstitutionalIdentityLinks --project tdv2 --context Tdv2DbContext
```

No pasar contraseñas en comandos. En desarrollo se mantienen User Secrets; en Ubuntu se usa el proveedor protegido vigente. Conceder `SELECT, INSERT ON TABLE public.tdv2_identity_links` al **rol web real**, sin inventar su nombre. Las cuentas Nexo conservan su SELECT existente sobre vistas; no requieren permisos Azure adicionales ni escrituras Nexo. La sesión pasa a `aspnet-2`: los tickets anteriores requieren una nueva autenticación, sin confundir su ausencia de ID canónico con falta de permisos ni crear ciclos. Los nuevos tickets sin sitios siguen permitiendo permanecer en el portal.

Para conflictos que no pueda resolver la comprobación automática, no editar sólo el correo ni reutilizar IDs por semejanza. El operador debe acreditar en el directorio institucional el mismo tenant/objeto y usuario Nexo, contrastar vínculos/locales anteriores y conservar la referencia de esa revisión. No hay endpoint web para forzar correspondencias.

Respaldo/recuperación: resguardar la base TDV2 y los vínculos/metadatos por el procedimiento institucional, sin adjuntar respaldos ni secretos al repositorio. Una reversión de la migración elimina sólo la tabla nueva: retirar primero el código dependiente y exportar/restaurar esas correspondencias bajo control del operador. Conservar los IDs Microsoft existentes de `users` y todos los datos de captura. No presentar el retorno al algoritmo antiguo por correo como una solución segura. La disponibilidad de la nueva tabla es requisito del código nuevo; no hay fallback de autorización por correo si falta.

## Archivos

- Microsoft: `Integrations/Microsoft/MicrosoftClient.cs`, nuevo `MicrosoftPerson.cs`, `Services/MicrosoftLoginService.cs`, `Services/ParticipantPhotos.cs`.
- Identidad/autorización: `Security/PostgresIdentity.cs`, nuevo `Security/NexoActorIdentity.cs`, `Integrations/Nexo/PostgresNexoProfiles.cs`, `Domain/Access.cs`, `Infrastructure/Contracts.cs`, `Web/RequestAccess.cs`, `Competencias/DigitalAccess.cs`.
- Persistencia: nueva entidad/configuración `InstitutionalIdentityLink`, `Tdv2DbContext`, nueva migración/designer y snapshot.
- Pruebas: nuevos `MicrosoftAliasTests`, `DigitalMicrosoftIdentityTests`, `VerifiedIdentityMigrationTests`; fixtures Microsoft/Nexo/identidad, `NativeTests`, `DigitalTests`, `MigrationTests`, `DatabaseInspection`, `RemoveUsedMediaMigrationTests`, `ApplicationCatalogTests`, y precondiciones de identidades secundarias de fotos/colaboración/reservas/sincronización/costo. `TDV2.Verification/Fixtures.cs` adapta sólo la identidad sintética.
- Documentación: README, ESTADO, guía de Competencias y autenticación/Mi espacio enlazan esta revisión. No se editan componentes visuales, captura, instrumento, paquetes, cola de actividad, 401 ni migraciones anteriores.

## Verificación y límites

Las verificaciones usan ASP.NET/cookies/CSRF, cifrado y PostgreSQL reales **aislados**. OAuth, Graph, publicaciones Nexo, personas y catálogos son sintéticos. La función SQL de actividad se extrae del código local Nexo y se ejecuta contra tablas sintéticas: sus recibos no son recepción institucional.

| Verificación | Resultado | Evidencia local bajo `.artifacts/` |
| --- | --- | --- |
| TypeScript, formularios y Vite | Correctos; 99 pruebas frontend | `npm run types:check`, `npm run test:forms`, `npm run build` en `ClientApp` |
| Compilación ASP.NET y verificadores Release | 0 errores y 0 advertencias | `dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false` |
| Reglas/transporte en memoria | 116/116 | `identity-verification-final.txt` |
| Regresión PostgreSQL de identidad, sesión, fotos y reservas | 53/53 | `native-postgres-20261029-165146-e929f40f/verification.json` |
| Instrumento, perfil, históricos, captura, cierre, auditoría y alcances de Competencias | 69/69 | `native-postgres-20261029-165146-85864557/competencias-verification.json` |
| Repetición tras ajustar tickets antiguos y configuración diferida | 35/35 identidad/OAuth/Nexo; 16/16 captura, alias y accesos | `native-postgres-20261029-170436-5f117fc1/verification.json`; `native-postgres-20261029-170436-550ceb55/competencias-verification.json` |
| Ejecución final, incluido `mail` fuera del catálogo y UPN/SMTP institucional | 37/37 identidad/OAuth/Nexo y 4/4 alias, históricos, foto y auditoría de Competencias | `native-postgres-20261029-171214-2849b888/verification.json`; `native-postgres-20261029-171214-a90ad95a/competencias-verification.json` |
| Migraciones EF completas, incluida la nueva | 73/73 | `native-postgres-20261029-170851-7f8322e8/ef-migrations.json` |
| Catálogos de aplicaciones tras actualizar las precondiciones de migración | 6/6 | `native-postgres-20261029-171001-2fb0fa07/verification.json` |
| Navegador Mi espacio, escritorio/móvil | 20 grupos correctos | `native-postgres-20261029-165735-cd176daf/sites-browser.json` |
| Navegador Competencias, captura fluida, perfil, informe y representación | 24 grupos correctos | `native-postgres-20261029-165746-1aae4a76/competencias-browser.json` |
| Script EF exclusivo de identidad, sin abrir conexión | Generado y revisado; sólo tabla, índice, permiso público e historial | `verified-identity-links.sql` |

No sumar estos números como casos independientes: las repeticiones dirigidas incluyen casos de las suites anteriores. La ejecución final cubre `aspnet-2`, conflicto de unicidad, ausencia de conexión diferida y lectura de UPN/SMTP pese a `mail` diferente. Las pantallas y lógica de captura no cambiaron después del recorrido de navegador. Los clústeres de estas pruebas quedaron detenidos y sus archivos temporales de contraseña vacíos.

Ejemplos de capturas sintéticas: [Mi espacio, escritorio](../../.artifacts/native-postgres-20261029-165735-cd176daf/sitios-both-1366.png), [Mi espacio, móvil](../../.artifacts/native-postgres-20261029-165735-cd176daf/sitios-both-390.png), [Competencias con acceso exclusivo](../../.artifacts/native-postgres-20261029-165746-1aae4a76/acceso-exclusivo-1366.png), [perfil móvil](../../.artifacts/native-postgres-20261029-165746-1aae4a76/ajuste-perfil-390.png). Estos artefactos locales no son servicios institucionales ni se incorporan a la publicación.

Casos específicos comprobados: UPN y `mail` diferentes; alias SMTP único y duplicado con normalización; SIP excluido; múltiples usuarios canónicos rechazados; correo cambiado con vínculo conservado; conflicto local/objeto/tenant rechazado; Graph caído/incompleto; revocación de vínculo y permiso; acceso exclusivo a cada sitio y ninguno; cuenta anterior sin claim que vuelve a autenticar sin ciclo 401; borrador, aviso e informe congelado intactos. La auditoría comprueba el actor canónico, el representado por separado, cero guardados automáticos centrales y un recibo manual aun con entrega temporalmente pendiente. No se añadió Graph a las solicitudes de captura.

Comandos reproducibles (raíz del repositorio, clúster desechable creado por el script):

```powershell
dotnet build tests/TDV2.NativeVerification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
$env:TDV2_TEST_CASE_PREFIX='Microsoft alias:|OAuth:|Sesión|Nexo:|Edición/OIDC'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser -DirectStart -NoBuild
$env:TDV2_TEST_CASE_PREFIX='Identidad Microsoft:|Guardado manual:|Entrada independiente:|Entrada: HTML'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -SkipBrowser -DirectStart -NoBuild
Remove-Item Env:TDV2_TEST_CASE_PREFIX
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly -DirectStart -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -BrowserOnly -DirectStart -NoBuild
$env:TDV2_TEST_SITE_ENTRY_ONLY='true'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart -NoBuild
Remove-Item Env:TDV2_TEST_SITE_ENTRY_ONLY
```

Las ejecuciones intermedias se conservan: se corrigieron el uso de FOR SHARE sobre una tabla de consulta, una aserción que buscaba texto React dentro del JSON del servidor y precondiciones de dos pruebas antiguas de reservas (bloque de otra etapa y liberación de la pestaña ganadora antes de adquirirlo en otra). No se alteró la lógica productiva de reservas para satisfacer esas pruebas. Se alinearon los IDs canónicos sintéticos de actividad con los de su publicación, distinguiéndolos de los IDs de pertenencia.

El verificador de migraciones reconoce la ausencia legítima de la tabla nueva cuando está pendiente. EF confirma cada reversión: al intentar bajar varias migraciones puede retirar primero la nueva tabla vacía y detenerse después en la protección histórica de `RemoveUsedMedia`. Las aserciones comprueban ese historial exacto y la conservación de formatos; no suponen una transacción única entre todas las migraciones. La prueba específica de identidad confirma Up, repetición, unicidades y Down aislado sin tocar usuarios.

Pendientes reales: comprobar el ClientId/tenant efectivos frente a AuthSID, sus consentimientos delegados y la respuesta seleccionada de `/me` del caso indicado. Hacerlo con sesión de esa persona, sin compartir token, secreto o cabeceras de autenticación. Comparar únicamente los campos requeridos y el identificador canónico institucional. Graph Explorer puede ayudar a inspeccionar el perfil, pero usa otra aplicación y no certifica los consentimientos de TDV2. Verificar después del cambio autorizado el acceso de ambos sitios, foto y conservación de históricos. Windows probado en aislamiento; Ubuntu y servicios reales pendientes.

**`SII_LECTURA_O_INTEGRIDAD` sigue sin causa confirmada. Esta corrección no resuelve ni diagnostica ese error de sincronización.** No hubo consultas a bases institucionales, cambios en Nexo, migraciones institucionales, reinicios reales, push o despliegues.
