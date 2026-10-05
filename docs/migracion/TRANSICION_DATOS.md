# Ensayo de transición de datos Laravel → ASP.NET

**Documento del ensayo histórico de conversión, separado de la base vacía recreada por el usuario.** TDV2 usa ahora migraciones EF Core y `__EFMigrationsHistory`. `SERVIDOR_TDV2:5432/tdv2_db` es el destino ASP.NET autorizado; su inicialización no importa datos Laravel. No se obtuvo ni convirtió una copia institucional con datos.

`Prepare-Transition.ps1` conserva la restauración en un destino nuevo del clúster verificado y el diagnóstico de sólo lectura. **La opción `-Apply` se retiró** al sustituir el ejecutor SQL por EF. El antiguo DDL se conserva exclusivamente en `tests/TDV2.NativeVerification/TransitionSchema`, para comprobar las reglas históricas sobre fixtures. Una copia real requiere verificar su esquema, diseñar su adopción EF y revisar el SQL generado antes de ejecutarlo. Las instrucciones de conversión de las secciones históricas no son un mecanismo vigente de migraciones.

## Qué está preparado y qué se comparó

Se revisaron las migraciones Laravel `2026_09_24_000001_secure_microsoft_identity`, `2026_09_25_000001_create_tdv2_ur_forms` y `2026_09_30_000001_create_sync_management`, exclusivamente como código. Se contrastaron con los SELECT/INSERT/UPDATE Npgsql actuales. No se consultó el esquema desplegado.

| Datos/estructura | Tratamiento del ensayo |
|---|---|
| users, UUID Microsoft | Conservar IDs y vínculo institucional; rechazar correos ambiguos y exigir claves únicas |
| formatos_ur | Conservar JSON, versión, porcentaje, autor, timestamps y FK restrictiva a UR; no recalcular ni normalizar respuestas |
| UR y colaboradores | Conservar empleado textual, ceros, origen/alcance, IDs Nexo y estado de revocación; los vínculos no sustituyen concesiones centrales vigentes |
| SII, ILDA, auditoría e historial | Conservar todas las filas y columnas; comparar huellas antes/después dentro de la transacción |
| Catálogos y tablas históricas adicionales | Mantenerlos sin convertirlos en permisos locales |
| ms_graph_tokens | Mover tabla heredada a laravel_archive, restringida al propietario; crear tabla vacía compatible con ASP.NET |
| Cookies/sesiones Laravel | Mantener las tablas históricas; no importarlas como sesiones ASP.NET |
| Sesiones y contextos ASP.NET | Añadir los SQL 002 y 003 existentes dentro de la transacción de conversión |
| Programación | Pausar automática e ILDA, quitar próxima fecha y avanzar versión; no liberar reservas existentes |

Los esquemas operativos declarados comparten tipos y columnas. El bootstrap 001/004 añade tablas que ya existen en Laravel: **no se ejecuta en el ensayo**. `preflight.sql` valida columnas/tipos, claves únicas, relación formato–UR, versiones/rangos básicos, JSON objeto, identidad no ambigua y ausencia de trabajos pendientes o reservados. Si el esquema real difiere, aborta antes del DDL; no intenta corregirlo silenciosamente.

La comparación es de contrato estructural y conservación; no acredita por sí sola semántica de todas las respuestas históricas, integridad institucional del catálogo, zonas horarias ni vigencia de permisos Nexo. Esas verificaciones requieren la copia real y revisión institucional.

## Obtener una copia autorizada

El responsable debe aportar: autorización para leer tdv2_db, servidor/puerto/base identificados, cuenta limitada a lectura de sus objetos, versión PostgreSQL, espacio disponible y destino privado bajo `.artifacts`. No usar `.env` ni imprimir contraseñas. Las credenciales deben suministrarse con un servicio/libpq o mecanismo privado aprobado, no por argumentos del proceso.

Ejemplo para el operador autorizado, **no ejecutado sobre datos reales**:

```powershell
# El servicio libpq tdv2_origen_lectura debe estar configurado privadamente.
# PGOPTIONS limita la sesión de respaldo a lectura; pg_dump no escribe el origen.
$env:PGOPTIONS = '-c default_transaction_read_only=on'
pg_dump --dbname='service=tdv2_origen_lectura' --format=custom --no-owner --no-privileges --file='.artifacts/transition-private/tdv2_autorizado.dump'
Remove-Item Env:PGOPTIONS
```

Preparar previamente el directorio con permisos privados. No incorporar dumps, datos personales, logs ni huellas por fila a Git. Registrar privadamente procedencia, fecha, versión y checksum del respaldo. No ejecutar Artisan, Composer ni comandos dentro de Herd para obtener la copia.

## Restaurar y revisar sin convertir

Con PostgreSQL aislado activo, desde la raíz y con la cuenta Windows que lo preparó:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Prepare-Transition.ps1 -Database tdv2_transition_ensayo01 -BackupPath .artifacts/transition-private/tdv2_autorizado.dump
```

El script valida marcador y `SHOW data_directory`, rechaza nombres que no comiencen por `tdv2_transition_`, exige un destino nuevo para restaurar y usa pg_restore con `--single-transaction --no-owner --no-privileges`. No conecta al origen. Revisa el contrato sin convertir; un fallo conserva la copia para diagnóstico privado, no borra ni reutiliza otra base. `tdv2_local_validation` y su User Secrets no se reemplazan.

El diagnóstico actual no ejecuta DDL de conversión. El ensayo histórico sintético agrupa preflight, huellas temporales, conversión, sesiones/contextos y verificación de conservación en una transacción. Esos criterios deben conservarse al preparar la futura adopción EF sobre una copia autorizada. No liberar reservas vencidas o ajenas para forzar una conversión.

Los informes quedan en `.artifacts/transition/<base>/verification.json`, sin cadenas, respuestas ni identidades. La copia todavía requiere una cuenta de aplicación con permisos sobre sus tablas/secuencias, y una conexión de prueba **separada** suministrada al proceso; no cambiar la conexión vigente de tdv2_local_validation. Nunca iniciar el sitio con la cuenta administradora que restaura/migra. No se inicia procesador ni se activan fuentes remotas.

## Ensayo sintético reproducible

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -TransitionOnly
```

Crea otro clúster nuevo y lo detiene al finalizar. Allí genera una base sintética no vacía con estructura declarativa Laravel, la respalda con pg_dump y la restaura en otra base antes de convertir. Verifica rechazo de destino/esquema/clave única/reserva, rollback del DDL al cambiar una huella, respuestas/versiones/avance intactos, ceros iniciales, 205 filas ILDA con nulos/vacíos, vínculos/catálogos adicionales, aislamiento de tokens, pausa y origen sin cambios. No acredita un respaldo de tdv2_db ni una migración institucional.

## Pendiente antes de un corte

Obtener la copia autorizada; comparar tipos, longitudes, índices, secuencias y relaciones reales con el contrato; revisar formatos heredados con su plantilla, actividad histórica y timestamps UTC; reconciliar concesiones y retiros pendientes con Nexo; ensayar nuevo login y aceptación funcional/visual sobre la copia. Las llaves de Data Protection necesitan administración y respaldo propios. El cifrado APP_KEY y cookies Laravel no se convierten.

El plan histórico de reversión conserva el origen y la copia. No se modifica ninguna conexión automáticamente. El corte institucional, la procedencia de los datos y su reversión requieren un plan posterior; no forman parte de la inicialización EF de la base vacía.
