# Contratos: colaboradores, representación y auditoría

Revisión previa de código Laravel, exclusivamente lectura. Fuente: controladores CollaboratorController, RepresentationController, PreviewController y ConfigurationController; NexoDelegation, NexoRepresentation, PreviewSession, ViewContext, FormAccess, RepresentationAudit; middleware ApplyRepresentation, ApplyPreview y RequireConfiguration; rutas/configuración y pruebas CollaboratorTest, RepresentationTest, PreviewTest y NexoDelegationContractTest.

## Delegación

- public.nexo_delegacion: email, id_ur, num_empleado y nivel_ur. Intersectar con ramas candidatas locales y empleado textual. Administrador sólo presenta su rama de adscripción nivel 2; combinar roles no amplía esa rama. Nexo confirma que es encargado y puede delegar.
- public.nexo_delegacion_roles: clave y rol_id. Sólo mapear colaborador_local y colaborador_dependencias publicados; no aceptar rol_id del navegador.
- Funciones public.nexo_a{ID publicado y validado}_buscar_personas(actor, ur, q, pagina), conceder_acceso(actor, ur, email, rol_id, ur_origen), retirar_acceso(actor, ur, concesion_id). Parámetros enlazados, READ COMMITTED. Nunca escribir tablas internas Nexo. P0001 es rechazo público 422; otros errores son 503 sin texto SQL.
- Configuración Laravel vigente: niveles_delegacion=[2], niveles_formato=[2,3], edicion_subordinadas=false. No habilitar delegación nivel 3 aunque exista una condición genérica en el controlador.
- Local: formato del ancestro nivel 3 o 2. Dependencias/global: formatos 2/3 dentro de la UR nivel 2 que autoriza. No crear formato para nivel 4 o inferiores.
- Alta central antes de vínculo local; volver a leer identidad publicada después de conceder. Sin vínculo local validado no hay permiso de edición, incluso si ya existe rol central. Retiro local confirmado antes de intentar retiro central; error conserva revocación y retiro_central_pendiente para reintento. No retirar una concesión con otro vínculo local activo.

## Representación

Función public.nexo_a{ID}_representacion(jsonb), Nexo 9.6.0. Objeto siempre lleva accion y actor_email obtenidos en servidor. Acciones: capacidad; buscar(q,pagina); iniciar(email,motivo,escritura); validar(token,requiere_escritura); finalizar(token). Bajo representación, buscar_personas/conceder_acceso/retirar_acceso se transportan por esta misma función con actor real, token servidor y parámetros ur/q/pagina/email/rol_id/ur_origen/concesion_id correspondientes.

Selección central: id, token, actor_email, email, nombre, id_ur, escritura, expira_en, motivo. El token queda cifrado en servidor; sólo se publican datos explícitos de la franja. Verificar id/correo/actor, vigencia, escritura y perfil vigente en cada solicitud. Mantener identidad Microsoft real; nunca sustituirla por la representada ni volver automáticamente a permisos reales tras caducidad/revocación.

Administrador y módulo Pruebas de acceso son requisitos locales; la capacidad central de representación es adicional. Configuración, escenarios y representaciones anidadas se bloquean mientras haya contexto activo. DELETE de salida sigue disponible con CSRF aunque Nexo falle.

## Vista y auditoría

Vista de prueba sólo mode=escenario, roles responsable/local/dependencias/consulta/administrador y UR activa; duración 30 minutos. Prohibidos modo usuario/email, escrituras y búsqueda delegada. Roles y áreas simulados jamás constituyen una autorización de escritura.

Laravel RepresentationAudit sólo registra cambios durante representación. Este bloque amplía el registro a operaciones propias por petición del usuario, conservando actor real, representado, acción, recurso y resultado. Guardado/vínculo local y auditoría comparten transacción. Entre Nexo y TDV2 no existe transacción distribuida: documentar altas centrales sin vínculo local y retiros pendientes, sin inventar permisos para recuperarlos.

Diferencias implementadas: comprobar explícitamente caducidad y escritura de la respuesta central además de llamar validar; serializar cambios de contexto con escrituras locales; exponer un mecanismo de salida también en AccesoRestringido, que actualmente en la fuente no incluye botón de salida aunque conserva props del contexto. Mantener separados vacío, denegación y error de conexión en búsquedas.

## Adaptación, recuperación y límites

- ASP.NET conserva las rutas HTTP, los campos de formularios y las acciones publicadas por Nexo. Las redirecciones Inertia pasan a JSON con redirect; el transporte React vuelve a consultar la página y conserva borradores cuando corresponde.
- La sesión Microsoft sigue identificando al actor real. Una tabla local adicional, tdv2_access_contexts, guarda la selección cifrada y una revisión por sesión. Ningún token de representación se publica en props, errores, auditoría ni historial del navegador.
- Laravel regenera su identificador de sesión al entrar/salir. ASP.NET conserva el ticket Microsoft y cambia atómicamente la revisión del contexto en servidor; las mutaciones comprueban además X-TDV2-Context. Son mecanismos de sesión diferentes, no cookies intercambiables. Sesiones ASP.NET anteriores a este bloque requieren nuevo login por el vínculo interno de sesión añadido al ticket.
- Se rechazan respuestas centrales malformadas, actor/persona distintos, ID no UUID, duración superior a la capacidad, ampliación de vigencia y escritura no solicitada. La publicación institucional debe confirmar esos tipos y formatos antes del corte; no se ha consultado. No se relajan comprobaciones para aceptar respuestas ambiguas.
- Las búsquedas exigen capacidad y módulos vigentes. P0001 se presenta como rechazo de negocio y los fallos de disponibilidad como 503. En capacidad, un rechazo explícito se distingue de una conexión fallida; ambos dejan permitido=false. Laravel marcaba ambos como no_disponible.
- El SQL usa parámetros posicionales para valores; los nombres de funciones sólo se construyen con un ID publicado, positivo y validado. El subselect de las funciones delegadas conserva filas incluso cuando el resultado contiene una sola columna. Referencias: [Npgsql](https://www.npgsql.org/doc/basic-usage.html) y [expresiones PostgreSQL](https://www.postgresql.org/docs/18/sql-expressions.html).
- Las altas/retiros se serializan con un bloqueo PostgreSQL por email/origen/rol. Guardados y cambios de contexto bloquean la sesión y comparan su revisión antes de persistir. Dos inicios no pueden reemplazarse silenciosamente.
- Si Nexo confirma un alta pero falla la identidad publicada o el commit local, no se concede acceso al formato. El rol/concesión central puede permanecer. El reintento requiere una concesión central idempotente: se verificó en el publicador sintético, queda pendiente comprobarlo en Nexo real. No se retira automáticamente una concesión que podría servir a otro vínculo.
- El retiro primero confirma revocación local + auditoría. Si falla Nexo, devuelve 202 y conserva retiro pendiente, visible y reintentable. Si hay otro vínculo local activo con la misma concesión, no la retira centralmente. Una pérdida física de conexión durante COMMIT exige reconciliación; no se probó y no hay worker automático.
- Inicio de representación: contexto y auditoría locales en una transacción. Si ésta falla después del inicio central, se intenta finalizar centralmente. Si esa compensación también falla, el token no se habilita localmente y Nexo conserva su caducidad.
- Salida y logout descartan el contexto local aun si Nexo falla; registran cierre_central=false. Logout elimina sesión/contexto y audita ambas identidades en una transacción. Si la auditoría local no puede escribirse, el cambio local se revierte. Un cierre central ya confirmado no se deshace: el contexto restante queda revocado hasta una salida explícita.
- Auditoría: actividades propias y representadas, búsquedas, altas, retiros, guardado, inicio/salida, logout y rechazos de endpoints del bloque (incluido CSRF). Metadatos permitidos explícitamente; se omiten token y motivo libre para evitar registrar secretos introducidos en texto. Laravel incluía motivo en RepresentationAudit. Rechazos intentan registrar resultado sin sustituir la respuesta por permisos si la bitácora falla. Login conserva su auditoría básica previa, separada de la creación de identidad.
- Pantallas MUI, navegación, tema, tipografía y banners originales conservados. Cambios puntuales: mensaje “áreas dependientes”, búsqueda distingue vacío/denegación/conexión y AccesoRestringido conserva contexto público y permite terminarlo.

## Preparación y aceptación institucional pendiente

No se accedió al Nexo Laravel institucional ni a Microsoft real. Las funciones SQL de tests son un **publicador sintético**, no el código del servidor Nexo portado a .NET. La cuenta sintética Nexo tiene SELECT sobre vistas y EXECUTE sobre funciones SECURITY DEFINER publicadas; no tiene permisos directos sobre sus tablas internas.

Se necesita un entorno institucional de pruebas separado, aplicación publicada con clave tdv2, cuenta PostgreSQL con SELECT de sus vistas (incluidas nexo_delegacion y nexo_delegacion_roles) y EXECUTE de sus funciones publicadas. Requiere las funciones de representación del contrato Nexo 9.6.0 citado en Laravel; confirmar firma jsonb, tipos de argumentos, campos, caducidades, autorización y comportamiento idempotente en la versión realmente instalada. No ejecutar las funciones fixture_* ni nexo_a47_* de tests en Nexo.

Casos de aceptación real: encargado nivel 2 con/sin delegación, roles asignables revocados, persona subordinada nueva en TDV2, ramas ajenas, concesión compartida, revocación originada desde Laravel, capacidad de representación independiente del rol administrador, consulta/escritura, vencimiento, finalizar y caída de publicación. Necesita también credenciales Microsoft de pruebas suministradas por un proveedor seguro y cuentas/UR sintéticas institucionales. No se solicitaron ni copiaron secretos de Herd.

Preparar únicamente la base TDV2 aislada con 001_core.sql, 002_aspnet_sessions.sql y 003_access_contexts.sql mediante una cuenta de DDL separada. Ejecución: permisos sobre tablas locales de sesiones/contextos/colaboraciones/auditoría, además de formatos/identidad; sin DDL ni acceso directo a tablas internas de Nexo. La aplicación no aplica esquemas al arrancar.

Evidencia y comandos vigentes: [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). Ninguna prueba sintética certifica las funciones del Nexo institucional.
