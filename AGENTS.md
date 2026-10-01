# TDV2: migración Laravel → ASP.NET Core 10

## Límites del trabajo
- Destino único de modificaciones: `C:\Users\Jesus Arenas\source\repos\tdv2`.
- `C:\Users\Jesus Arenas\Herd\tdv2` es referencia de **solo lectura**. No ejecutar Artisan, Composer, npm, pruebas ni migraciones allí: pueden escribir cachés, logs o bases.
- No leer/copiar `.env`, tokens, cookies, respaldos, logs, contenido de `storage`, credenciales ni datos personales de las bases existentes. No copiar `public/build` ni el proyecto anidado `tdv2` de Herd como fuente vigente.
- Excepción autorizada expresamente por el usuario el 2026-10-01: leer únicamente `C:\Users\Jesus Arenas\Herd\tdv2\.env` para importar Microsoft/Nexo/SII/ILDA con `scripts/Local-Tdv2.ps1 Import`. Nunca imprimir sus valores, copiar el archivo, modificarlo, sustituir `tdv2_local_validation` ni activar sincronizaciones. Secretos sólo en el almacén DPAPI privado; esta autorización no concede acceso a bases institucionales.
- Autorización posterior del usuario: trasladar esa configuración DPAPI a **User Secrets** de tdv2 para administrarla desde Visual Studio. Se permite escribir exclusivamente su `secrets.json` asociado a UserSecretsId en el perfil Windows (fuera del destino por diseño de .NET), sin imprimir valores. User Secrets es ahora la fuente local vigente. `Import`/`Configure` anteriores quedan retirados; ningún arranque debe reponer valores DPAPI ni sobrescribir ediciones. DPAPI sigue protegiendo las credenciales de administración del clúster aislado.
- No conectar ni migrar bases existentes. Primeras pruebas exclusivamente sintéticas; una prueba en PostgreSQL debe usar una base desechable identificada explícitamente.
- No aplicar DDL al iniciar la aplicación. No habilitar sincronizadores ni tareas remotas por defecto.

## Contrato que debe conservarse
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
