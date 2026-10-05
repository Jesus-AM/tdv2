# TDV2: migración Laravel → ASP.NET Core 10

> Publicación: `SERVIDOR_TDV2` y `USUARIO_TDV2` ocultan el destino institucional previamente autorizado; no autorizan destinos alternativos. La referencia exacta se conserva sólo localmente en `.artifacts/github-private-originals/AGENTS.md`. Sin esa referencia, solicitar al operador el destino antes de cualquier acceso institucional.

## Límites del trabajo
- Destino único de modificaciones: `C:\Users\Jesus Arenas\source\repos\tdv2`.
- Excepción expresa del usuario 2026-10-05: corregir también el código fuente de delegación en `C:\Users\Jesus Arenas\Herd\nexo` para TDV2, conservando reglas de otras aplicaciones y cambios locales. No ejecutar Laravel/Artisan/Composer allí, leer .env ni usar bases o concesiones reales. PHP sin bootstrap puede generar SQL para pruebas en PostgreSQL desechable dentro de TDV2. La publicación/configuración institucional sigue siendo un paso explícito del operador.
- `C:\Users\Jesus Arenas\Herd\tdv2` es referencia de **solo lectura**. No ejecutar Artisan, Composer, npm, pruebas ni migraciones allí: pueden escribir cachés, logs o bases.
- No leer/copiar `.env`, tokens, cookies, respaldos, logs, contenido de `storage`, credenciales ni datos personales de las bases existentes. No copiar `public/build` ni el proyecto anidado `tdv2` de Herd como fuente vigente.
- Excepción autorizada expresamente por el usuario el 2026-10-01: leer únicamente `C:\Users\Jesus Arenas\Herd\tdv2\.env` para importar Microsoft/Nexo/SII/ILDA con `scripts/Local-Tdv2.ps1 Import`. Nunca imprimir sus valores, copiar el archivo, modificarlo, sustituir `tdv2_local_validation` ni activar sincronizaciones. Secretos sólo en el almacén DPAPI privado; esta autorización no concede acceso a bases institucionales.
- Autorización posterior del usuario: trasladar esa configuración DPAPI a **User Secrets** de tdv2 para administrarla desde Visual Studio. Se permite escribir exclusivamente su `secrets.json` asociado a UserSecretsId en el perfil Windows (fuera del destino por diseño de .NET), sin imprimir valores. User Secrets es ahora la fuente local vigente. `Import`/`Configure` anteriores quedan retirados; ningún arranque debe reponer valores DPAPI ni sobrescribir ediciones. DPAPI sigue protegiendo las credenciales de administración del clúster aislado.
- No conectar ni migrar bases existentes. Primeras pruebas exclusivamente sintéticas; una prueba en PostgreSQL debe usar una base desechable identificada explícitamente.
- Excepción posterior autorizada el 2026-10-01: inicializar la base recreada `tdv2_db` en `SERVIDOR_TDV2:5432`, como `USUARIO_TDV2`, con `ConnectionStrings:Tdv2` de User Secrets y TLS. Sólo SQL raíz 001–004, comprobaciones de destino/permisos/esquema/historial y repetición; sin `database/transition`, datos sintéticos ni sincronizaciones. El preparador local y sus protecciones se conservan. Las futuras migraciones requieren ejecución explícita del operador, nunca arranque web.
- No aplicar DDL al iniciar la aplicación. No habilitar sincronizadores ni tareas remotas por defecto.
- Autorización posterior en esta sesión: sustituir los ejecutores SQL por migraciones estándar EF Core, adaptar entidades/DbContext/persistencia/controladores y aplicar EF explícitamente al mismo servidor recreado tras validar destino, permisos y ausencia de contenido. Conserva cualquier contenido inesperado. `dotnet ef database update` es el mecanismo vigente; los SQL anteriores son fixtures históricos de pruebas, no un segundo migrador. Esta autorización prevalece sobre la limitación anterior a SQL raíz 001–004.

## Contrato que debe conservarse
- Actualización expresa 2026-10-05: captura con reservas por bloque y envío definitivo. Las reservas de captura se renuevan sólo por cambios del usuario; la regla de no renovar por latido continúa aplicándose a reservas del procesador de sincronizaciones. `responsable_ur_supervisor` es la clave vigente; `responsable_ur_institucional` es un alias transitorio. Un responsable nivel 2 edita su rama autorizada; nivel 3 conserva su formato. Enviados son inmutables y no tienen reapertura.
- React, TypeScript, MUI, tema, tipografía, componentes, portada e interfaz actuales. Los cambios de transporte no justifican rediseñar pantallas.
- PostgreSQL almacena los datos de TDV2; Nexo Laravel sigue siendo autoridad de identidad institucional, roles, módulos, concesiones y representación.
- Ante fallas de Nexo, denegar acceso; nunca sustituirlo con permisos locales, claims antiguos ni parámetros del navegador.
- La cuenta Microsoft real y la identidad efectiva de representación son distintas. Auditar ambas; jamás enviar al navegador el token de representación.
- Administrador: consulta institucional; edición únicamente en una rama válida de nivel 2, incluso al combinar roles. Responsabilidad usa `num_empleado` textual, conservando ceros iniciales.
- Colaboraciones exigen vínculo local y concesión central vigente coincidente. Las vistas por rol/área son de solo lectura y no conceden permisos.
- Validar contexto de edición, CSRF, versión y alcance en el servidor. Persistir avance calculado por servidor. No sobrescribir formatos durante sincronizaciones.
- ILDA conserva todas las filas y columnas, incluidos nulos y vacíos; el límite de 200 filas corresponde al formulario, no a la réplica.
- Sincronizaciones: fuentes remotas de sólo lectura; páginas/formularios consultan exclusivamente catálogos locales. Publicaciones por fuente, metadatos, resultado y auditoría deben compartir transacción.
- El procesador se inicia sólo de forma explícita. No publicar ni liberar reservas vencidas o ajenas; verificar propietario/caducidad también antes del commit. Un latido de actividad nunca renueva una reserva.

## Forma de trabajo y evidencia
- Leer `ESTADO_MIGRACION.md` y `docs/migracion/INVENTARIO.md` antes de continuar. Actualizar implementado, probado y pendiente por separado.
- Inspeccionar el código real antes de portar cada bloque; las pruebas Laravel son criterios de aceptación, no evidencia de ejecución en .NET.
- No declarar migración completa por compilar. Verificar reglas negativas, aislamiento por UR, revocaciones, concurrencia, atomicidad y errores sin secretos.
- No añadir autenticación de demostración a producción ni rutas que acepten identidad/roles arbitrarios. Los dobles sintéticos viven en el proyecto de pruebas.
- Conservar mensajes públicos genéricos; no registrar excepciones SQL/OAuth completas ni cadenas de conexión.
- No ejecutar instaladores ni comandos que modifiquen Herd. Mantener las dependencias nuevas y todos los artefactos de trabajo en el destino.

## Verificación
Los comandos y resultados vigentes se documentan en `ESTADO_MIGRACION.md`. No confundir pruebas en memoria con pruebas del proveedor PostgreSQL ni pruebas de transporte con aceptación visual en navegador.
