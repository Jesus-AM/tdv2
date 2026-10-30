# Conflictos de empleado y conteos registrados — 2026-10-30

**Definición posterior vigente:** [población por usuario canónico y estados separados](POBLACION_CANONICA_Y_ESTADOS_20261030.md). Los tres grupos de empleados ya no bloquean el denominador de acceso; las correspondencias afectan únicamente sus estados. Las recomendaciones históricas de mantener todo el total pendiente hasta eliminar los grupos quedan sustituidas por esa definición.

## Evidencia institucional recibida

El operador ejecutó el diagnóstico con su configuración habitual y comunicó: conexiones/objetos/SELECT correctos, publicación de Competencias con ID correcto, 80 usuarios/candidatos y cero usuarios sin rol aceptado. La única incidencia fue `employee_conflict=3`. Hay 9 encuestas almacenadas, 8 finalizadas, 0 en curso, ninguna encuesta sin correspondencia de empleado, ningún empleado duplicado en encuestas y ningún informe final inconsistente. No hay migraciones Digital pendientes según esa ejecución. `level2Population=77` se recibió expresamente como **no certificado**.

Esta información confirma el bloqueo `POPULATION_INTEGRITY` por la validación colectiva, sin excepción de conexión/permisos. Es evidencia **aportada por el operador**, no una consulta institucional hecha por Codex. El proceso de Codex continúa sin obtener la configuración de conexión y no accede a los registros reales. No se vuelve a atribuir esta incidencia a permisos, publicación o migraciones ausentes.

## Qué significa exactamente el contador 3

La consulta compartida está en `PostgresNexoProfiles.PopulationIdentityCte`. Conserva las personas individuales con un rol admitido vigente en la publicación de la aplicación, mediante `EXISTS` sobre `nexo_usuario_rol`, sin exigir módulo Encuesta. El empleado se toma **literalmente** de `num_empleado`: no se convierte a número, no se eliminan ceros, no se deduce por nombre o correo. El ID canónico es `usuario_id::bigint`; el correo se normaliza sólo como dato de la identidad, no como llave de unión con encuestas.

```sql
-- identities contiene DISTINCT id, employee, unit, email del universo anterior.
SELECT count(*)
FROM (
    SELECT employee
    FROM identities
    GROUP BY employee
    HAVING count(DISTINCT id) > 1
) x;
```

**3 significa tres grupos de números de empleado**, cada uno relacionado con dos o más IDs canónicos distintos. No significa tres personas ni tres filas duplicadas. Una cuenta con varios roles, varias asignaciones de aplicación o filas idénticas no genera por sí misma esta incidencia: se utiliza `EXISTS`, `DISTINCT` y finalmente `count(DISTINCT id)`. Tampoco dos UR del mismo canónico elevan ese contador; esa contradicción se detecta en `canonical_conflict`.

Con la evidencia recibida se sabe qué validación falla. **Aún no se sabe qué registros forman cada grupo ni si representan cuentas de una misma persona o personas diferentes mal relacionadas**. No se ha demostrado un error de normalización/agrupación; por ello no se cambia la regla para aceptar conflictos, no se fusionan cuentas y no se publica 77 como meta. La coincidencia de empleado entre una encuesta y un grupo ambiguo tampoco prueba qué canónico es su dueño.

## Detalle privado para el operador

```powershell
Set-Location 'C:\Users\Jesus Arenas\source\repos\tdv2'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Diagnose-DigitalPopulation.ps1 -PrivateEmployeeConflicts
```

Conserva el diagnóstico público por fases y agrega un archivo nuevo en:

```text
%LOCALAPPDATA%\TDV2\diagnosticos\employee-conflicts-FECHA-UUID.json
```

El archivo queda fuera del repositorio y de la publicación web. No se sobrescriben archivos anteriores. La consola sólo indica nombre de archivo, cantidad de grupos/canónicos y si todas las fuentes privadas pudieron leerse. No imprime números de empleado, IDs individuales, nombres, correos ni detalles de captura. No compartir el archivo en chat, Git, tickets públicos ni documentación. En Windows hereda los permisos de la carpeta del perfil; en Unix se crea con lectura/escritura exclusiva del usuario. No se cambian ACL de User Secrets ni se copian credenciales.

El contenido mínimo es:

| Sección privada | Datos y propósito |
| --- | --- |
| `groups` | Empleado exacto, cantidad de canónicos y, por registro publicado, ID canónico, ID de asignación a la aplicación, UR, origen de UR, filas publicadas repetidas y filas de roles admitidos. Distingue repetición de consultas, varias asignaciones y varios canónicos. |
| `verifiedIdentity` | ID canónico, cantidad de vínculos verificados locales y usuarios locales cuya identidad Microsoft coincide con ese vínculo. Reutiliza `tdv2_identity_links` y `users`; no llama a Graph ni exporta `oid`, `tid`, tokens o referencias de verificación. La ausencia de vínculo puede corresponder a alguien que nunca ingresó; no desautoriza ni resuelve una contradicción. |
| `surveys` | Empleado, UUID de encuesta, UR guardada, revisión, estado/fecha del envío y cantidad de envíos históricos. No exporta respuestas, perfiles ni informes. |
| `units` | IDs/niveles/padres/tipo/estado del catálogo compartido para las UR observadas; sin nombres de áreas o personas. |

La búsqueda privada usa exactamente el CTE y agrupación del contador público; las comprobaciones se realizan por conjuntos, sin solicitar una consulta por cada persona. La publicación de Nexo sigue siendo la fuente de pertenencia/roles. La identidad Microsoft local sólo se inspecciona como evidencia, sin preferir la cuenta que tenga un vínculo o más permisos.

Se revisó el código local de referencia de Nexo, sólo mediante lectura: `PostgresUserUnits::employeeNumber()` publica `num_empleado_manual` cuando no hay `usuario_empleado`; en caso contrario publica el empleado que resuelve `CuentasInstitucionales` para esa cuenta institucional. Esa resolución comprueba una cuenta activa con empleado único y empleado presente/activo, pero no exige que un empleado tenga un único registro en `usuarios` de Nexo. Por ello dos canónicos pueden compartir legítima o erróneamente la misma referencia publicada: hace falta revisar los registros, no inferir la causa del contador. La UR se resuelve por asignación manual, responsabilidad única o adscripción única según el contrato ya documentado. No se ha leído la base de Nexo ni sus tablas centrales para confirmar cuál de esas vías produce cada grupo real.

Si una fuente adicional no se puede leer, el archivo conserva los grupos y marca esa sección `available=false` con código saneado. `SourcesComplete=false` aparece también en consola. No se atribuye «sin vínculo» a un SELECT denegado. Todas las conexiones mantienen `default_transaction_read_only=on`, sin reintentos ni arranque de servidores, auditoría, sincronizadores o migradores.

## Acción pendiente según lo observado

El operador debe revisar cada uno de los tres grupos por **número de empleado e IDs del archivo privado**, contrastando la cuenta/empleado institucional de SII con los registros y asignaciones vigentes de Nexo:

1. Si sólo hay filas/asignaciones repetidas del mismo canónico, comprobar que el diagnóstico y publicación observados pertenecen a la misma ejecución. Esa repetición aislada no explica `employee_conflict`; no se elimina una persona para corregirla.
2. Si varios canónicos corresponden institucionalmente a la misma persona, revisar la duplicación de cuentas y sus referencias institucionales con el administrador de Nexo. Establecer allí el canónico correcto y la relación de cuenta/empleado mediante el procedimiento administrativo autorizado, conservando sus antecedentes. No asumir que nombres parecidos o un alias bastan para fusionar registros. No se proporciona un SQL de unión/borrado ni se inventa una función de Nexo.
3. Si corresponden a personas diferentes, corregir en la fuente institucional/Nexo la asociación incorrecta de cuenta con empleado. No unir las identidades ni transferir respuestas/permisos a otra persona. Verificar explícitamente qué empleado es dueño de la encuesta existente antes de cualquier corrección de origen.
4. Si hay UR o vínculos Microsoft contradictorios, resolverlos institucionalmente; la encuesta guardada y su vínculo no se reescriben para hacer desaparecer la incidencia.

Volver a ejecutar el diagnóstico después de la corrección autorizada de origen. Sólo cuando `employee_conflict=0`, las demás validaciones sean cero y la población se certifique se publican denominador y participación. El total resultante podría cambiar según la causa; no se anticipa que deba ser 77 u 80. **No se han ejecutado esas correcciones ni identificado los IDs reales en esta sesión.**

La migración de identidad que causó el `PathNotFound` al buscarla existe en `tdv2/Migrations/20261029163206_VerifiedInstitutionalIdentityLinks.cs`; se había intentado leer `tdv2/Infrastructure/Migrations/...`, que no es su ubicación. El archivo de pruebas `tests/TDV2.NativeVerification/DigitalPopulationDiagnosticTests.cs` existe. Se corrigió la búsqueda, sin mover, editar o aplicar la migración.

## Indicadores y conteos registrados

El reporte conserva dos significados distintos:

- **Población certificada:** acceso actual, adscripción actual y personas únicas comprobadas. Sus campos son los existentes `target`, `completed`, `inProgress`, `notStarted`, `participation`, `maturity`. Cuando la población falla siguen sin certificarse; no se rellenan con una cifra parcial ni con las encuestas existentes.
- **Conteos registrados:** nuevo `summary.registered`, disponible cuando la población falla. Incluye `stored`, `completed`, `inProgress`, `withoutCapture`; cuenta encuestas únicas por empleado dentro de las UR **guardadas** autorizadas. La selección de UR y nivel 2/dependientes se aplica en SQL antes de leer las filas. No asigna una UR central entre opciones ambiguas ni amplía el alcance de un responsable.

Las tarjetas pasan a «Finalizadas registradas» y «En curso registradas» y las celdas llevan «Registradas». Se explica su adscripción guardada y la pertenencia vigente no certificada. La meta, personas sin iniciar y participación continúan como no disponibles. Las filas vacías creadas al aceptar privacidad son registros sin captura; no son personas «En curso» ni el universo de quienes nunca ingresaron. El índice de madurez de la población no se calcula con estos conteos ni con ceros añadidos.

Con los datos reales informados, las 8 finalizadas pueden mostrarse como registradas **si sus UR guardadas pertenecen al alcance/nivel consultado**; no se fuerza esa cifra en un filtro que no las incluya. Las 9 almacenadas y 0 en curso no constituyen una meta de participación. El escenario visual sintético equivalente muestra 9 registradas, 8 finalizadas y 0 en curso, sin porcentaje. Una revisión reabierta conserva captura y deja de contarse como finalizada registrada hasta su nuevo envío; el informe anterior permanece en Resultados generales/historial conforme a las reglas existentes.

La lectura adicional sólo ocurre al consultar el reporte con población no disponible. No se agrega a preguntas, autoguardado, autenticación ni Graph. No se envían respuestas al navegador del reporte. La auditoría, los recibos manuales, permisos, submódulos, filtros 2–5, nivel 2, tipografía/azul y herramientas de administración permanecen vigentes.

## Archivos y pruebas

- Producción: `PostgresNexoPopulation.cs` (CTE compartido, sin relajar la validación), `DigitalAggregates.cs` (conteos registrados con alcance SQL), `Aggregates.tsx` y `types.ts` (etiquetas y advertencia).
- Herramienta del operador: `Diagnose-DigitalPopulation.ps1`, entrada de `NativeTests.cs`, `DigitalPopulationReadOnly.cs` y nuevo `DigitalPopulationPrivateConflicts.cs`. No hay endpoint web para el detalle privado.
- Pruebas: fixture/casos de `DigitalPopulationDiagnosticTests.cs`, nuevo recorrido `digital-registered-counts-ui.mjs`. README, ESTADO, GUIA y documentación de diagnóstico actualizados. Sin migración nueva ni modificación de las existentes.

Resultados de esta revisión:

| Comprobación | Resultado y límite |
| --- | --- |
| Compilación Release de la solución | 0 errores, 0 advertencias; sin actualización de paquetes |
| TypeScript / Vite | Correctos sobre el frontend actual |
| Pruebas frontend existentes | 102 correctas |
| PostgreSQL desechable | **11 grupos correctos, 0 fallos**, sin ejecutar migraciones |
| React/Edge con paquete compilado | 3 recorridos correctos, sin errores JS ni desbordamiento a 1440, 900 y 390 px; transporte HTTP sintético |
| Configuración institucional desde Codex | `CONFIG_MISSING` antes de conectar; no se generó un archivo privado institucional desde este proceso |
| Lectura institucional del operador | Evidencia agregada recibida y descrita al principio; registro concreto de cada grupo pendiente de la nueva exportación privada |

Los casos PostgreSQL incluyen tres grupos reales dentro de un fixture sintético (seis canónicos en ese fixture, **no una afirmación sobre los tres grupos institucionales**), filas/roles/asignaciones repetidas sin canónicos adicionales, lectura de vínculos existentes, fuentes privadas incompletas explícitas, permiso/columna faltantes, población vacía, conteos registrados con alcance de responsable y contenido sin alteraciones tras consultar. Una revisión reabierta simulada cambia 6 finalizadas/2 en curso a 5/3 y conserva el informe archivado; se prueba la lectura de ese estado, no se ejecuta una reapertura real ni su función administrativa. No se reejecutó la suite completa de 79 escenarios que aplica migraciones.

El fixture usa una base mínima sintética para las conexiones del diagnóstico; no acredita la publicación institucional ni una transacción distribuida entre bases. Las tablas/historial del fixture se crean como preparación sintética, sin ejecutar EF Migrate. Los archivos privados de prueba permanecen en `.artifacts`, excluidos de Git, y no se copian a la evidencia pública. El clúster se cerró al terminar.

Comandos de comprobación:

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -PopulationDiagnosisOnly -SkipBrowser
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
Push-Location ClientApp
node tests/browser/digital-registered-counts-ui.mjs
Pop-Location
```

[Resultados PostgreSQL](evidencia/20261030-conflictos-conteos/population-diagnosis-tests.json), [resultado de navegador](evidencia/20261030-conflictos-conteos/report.json) y capturas [escritorio](evidencia/20261030-conflictos-conteos/registradas-1440.png), [tableta](evidencia/20261030-conflictos-conteos/registradas-900.png), [móvil](evidencia/20261030-conflictos-conteos/registradas-390.png). Las capturas contienen exclusivamente identidades y datos sintéticos.

**Pendiente concreto:** ejecutar la exportación privada con la configuración habitual y contrastar los tres grupos en Nexo/SII. Puede compartirse un resumen sin empleados/IDs: número de canónicos/asignaciones/UR/vínculos verificados/encuestas por grupo y la clasificación institucional comprobada. Hasta obtener esa evidencia no se declara resuelta la contradicción de origen ni se publica meta/participación certificadas. No se necesita una nueva migración o permiso de Azure por estos cambios; no se modificaron datos institucionales, secretos, Nexo ni permisos. Sin reinicios/reaperturas reales, push o despliegue.
