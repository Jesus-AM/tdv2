# Avance no disponible: trazado y diagnóstico dirigido — 2026-10-30

**Actualización de definición:** el diagnóstico distingue ahora [denominador canónico y estados de encuesta](POBLACION_CANONICA_Y_ESTADOS_20261030.md); `employee_conflict` ya no invalida por sí mismo la población y se añade `survey.canonicalStates`.

**Actualización posterior con evidencia del operador:** se identificó `employee_conflict=3` como bloqueo, con configuración/lectura correctas. [Detalle privado y conteos registrados](CONFLICTOS_EMPLEADO_Y_CONTEOS_20261030.md). Sustituye el límite de «causa institucional pendiente» de esta revisión anterior: sigue pendiente identificar los registros concretos y su corrección, pero ya se conoce la validación que falla.

## Resultado y límite de esta revisión

Se localizó el mecanismo que transforma fallas de población en todos los indicadores «No disponible», pero **la causa institucional concreta sigue pendiente de la salida del diagnóstico del operador**. En esta sesión, el comando terminó en `configuration / CONFIG_MISSING / ConnectionStrings:Tdv2`, antes de abrir conexiones. Esto no demuestra que falte esa clave en el PowerShell habitual ni autoriza cambiar permisos del almacén. No se leyeron datos institucionales, no se copiaron credenciales y no se ejecutaron migraciones, reaperturas ni reinicios.

La comparación con Git (`06e307c`, `DigitalAggregates.cs`) explica por qué antes podían verse cantidades: calculaba finalizadas/en curso desde `cd_surveys` y metas manuales locales, sin consultar la nueva población efectiva. Que existan encuestas no demuestra que se haya podido certificar el denominador vigente. No se recupera ese cálculo anterior como sustituto de la población por aplicación.

## Dónde aparece el estado

1. La página `/competencias-digitales/avance-dependencia` y `GET /competencias-digitales/api/avance` llegan a `CompetenciasController` y exigen autorización del módulo mediante `DigitalAccess.Require(..., 0)`.
2. `DigitalAggregates.Read` obtiene generación, aplica alcance, nivel 2 predeterminado y descendientes. Después llama a `DigitalNexo.Population`.
3. `PostgresNexoProfiles.ApplicationId` comprueba la publicación de `nexo_aplicacion` frente a `CompetenciasDigitales:Nexo:ApplicationId`. La consulta colectiva utiliza `nexo_usuarios` y `nexo_usuario_rol` efectivos, sin requisito del módulo Encuesta, sin Graph ni usuarios locales ficticios.
4. Una excepción SQL/conexión se convertía en `DomainProblem(503)`; también había rechazos de validación sin excepción del proveedor. `DigitalAggregates` captura estados 403/503 de **esa consulta**, marca `available=false` y `Summarize` devuelve indicadores nulos con `populationAvailable=false`.
5. `ClientApp/resources/js/competencias/Aggregates.tsx` presenta el aviso y los nulos como «No disponible». Las consultas posteriores usan empleados de la población; si ésta falló, el conjunto está vacío. Las encuestas no se borraron: sencillamente no se pueden incluir en un total certificado.

Una falta de esquema/control que falle **antes** de ese bloque, o una excepción al leer/deserializar encuestas **después**, no es atrapada por ese `catch`: produce un error de la petición. Por eso se inspeccionan esquema y generación por separado, sin atribuirles automáticamente el aviso visible.

## Correcciones justificadas

- La validación agrupada conserva la misma población y el rechazo de conjuntos incompletos, pero devuelve **conteos por causa**, en lugar de un único booleano. El diagnóstico y producción comparten la consulta; no hay una segunda autorización ni consulta por persona.
- Producción registra únicamente fase, código/cantidad de validación, tipo de excepción y SQLSTATE. No registra el mensaje SQL, respuestas, identidades ni conexiones. El diagnóstico explícito puede observar el error original del proveedor antes de su conversión a un mensaje público.
- Un problema de correspondencia produce `populationProblem=integrity`; un fallo de consulta/contrato produce `source`. La interfaz conserva sus estilos y los indicadores nulos. Ante integridad informa que hay correspondencias que requieren revisión, en lugar de sugerir que esperar resolverá necesariamente el problema. No se publican códigos internos ni cantidades de incidencias fuera del alcance.
- Se corrigió un defecto **del diagnóstico anterior**, no demostrado como causa del reporte: construía `Unit` con estado vacío. `UnitDirectory` excluye unidades que no están activas; así podía declarar inválidas todas las cadenas. Ahora lee `estatus_ur` y aplica las mismas reglas de catálogo activo y `DigitalScope.Chain` que producción.
- El diagnóstico anterior se detenía al fallar la población y no mostraba las encuestas existentes. Ahora ejecuta fases independientes y muestra los conteos almacenados aunque la población o alguna vista no puedan leerse. Los cruces incompletos quedan explícitamente **no verificados**.

### Validaciones sin excepción SQL

| Código | Qué cuenta |
| --- | --- |
| `canonical_id` | ID canónico inválido |
| `employee` | Empleado ausente, vacío o incompatible con el límite vigente |
| `unit_missing` | Sin UR publicada |
| `unit_or_chain` | UR fuera del catálogo activo o cadena inválida |
| `origin_multiple` | Publicación que no puede resolver una adscripción única |
| `origin_missing_or_unknown` | Origen no comprobable con el contrato actual |
| `canonical_conflict` | Un canónico con distintas correspondencias de empleado/UR/correo |
| `employee_conflict` | Un empleado relacionado con varios canónicos |
| `email` | Correo canónico que no satisface la validación vigente de cuenta |

Los conteos se solapan: **no se suman como número de personas afectadas**. Una sola incidencia global conserva el reporte como no verificado; no se excluye silenciosamente esa persona para publicar un total menor. Los roles repetidos se deduplican; correo normalizado y alias no se utilizan como clave para unir encuestas. El vínculo continúa siendo el empleado textual, conservando ceros iniciales.

## Comando del operador

Desde el PowerShell habitual, en el repositorio:

```powershell
Set-Location 'C:\Users\Jesus Arenas\source\repos\tdv2'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Diagnose-DigitalPopulation.ps1
```

Compila en Release sin competir con el apphost Debug de Visual Studio; después ejecuta **únicamente** la entrada `--digital-population-readonly`. No arranca el sitio ni inicializa fixtures o trabajadores. `-NoBuild` permite reutilizar un binario ya actualizado. No ejecuta restore, migraciones ni cambios de configuración. Si faltan dependencias ya restauradas, la compilación se detiene antes del diagnóstico.

Usa `tdv2/appsettings.json`, el archivo de ambiente habitual cuando corresponde, User Secrets de la aplicación y variables de entorno existentes (éstas prevalecen). Sin ambiente explícito usa Development para esta herramienta del operador. No usa launchSettings para lanzar un proceso web ni lee `.env`. No imprime los valores de configuración. No copiar secretos al repositorio ni modificar ACL para ejecutarlo.

Cada conexión impone `default_transaction_read_only=on`, tiempo máximo de sentencia y errores sin detalles de parámetros. Una escritura accidental queda rechazada por PostgreSQL incluso con un propietario; esto está probado en el clúster sintético. El comando sólo contiene consultas SELECT/metadatos y no llama a funciones de auditoría/administración. No realiza reintentos.

La salida JSON muestra:

- `configuration`, `connection.*`: disponibilidad de claves y conexión de sólo lectura, sin host/usuario/contraseña.
- `object.*`: existencia, SELECT efectivo y nombres/tipos de columnas de los objetos explícitos que consume el reporte. No muestra filas ni definiciones SQL de vistas.
- `nexo.application`: cantidad de publicaciones y coincidencia con el identificador configurado; distingue una publicación de Procedimientos.
- `nexo.membership`: usuarios efectivos, individuales y con roles admitidos por el acceso actual a Competencias; no exige permiso de Encuesta.
- `catalog.hierarchy`: unidades presentes/activas, cadenas válidas, grupos nivel 2 y dependientes, nodos acumulativos, cadenas cíclicas/incompletas/niveles incompatibles. No imprime nombres ni claves de áreas.
- `population.validation`: candidatos, incidencias por causa y si puede certificar la población.
- `schema.migrations`: migraciones Digital conocidas aplicadas/pendientes y cantidad de desconocidas, leídas de `__CDMigrationsHistory`; no asume aplicada la de reapertura ni ejecuta ninguna. Los metadatos de `__EFMigrationsHistory` de TDV2 se comprueban separadamente.
- `survey.counts`: encuestas almacenadas, finalizadas, en curso y filas todavía vacías; correspondencias de población actual, niveles 2/dependientes y filas no emparejadas. Inicial cero cuenta como respuesta; datos institucionales automáticos aislados no cuentan como inicio.
- `survey.generation`, `survey.historical`, `survey.generationStable`: control vigente, histórico de envíos y detección de un cambio de generación durante las lecturas. Las filas activas no tienen una convocatoria adicional: el reinicio global cambia generación y vacía captura; no se filtran finales por versión técnica de instrumento.

`matchingAvailable=false` significa que **no se realizó** una correspondencia de población; `populationVerified=false` impide usar el cruce como denominador certificado. `unmatchedRows` no significa necesariamente acceso revocado: con una fuente incompleta no permite esa conclusión. Conteos de distintas fuentes se toman en lecturas sucesivas, no son una transacción distribuida; no usar el diagnóstico como un reporte histórico o una medición de carga.

La salida conserva SQLSTATE y una explicación de una lista controlada: `42501` permisos, `42P01` objeto, `42703` columna, `42883` operador/tipo, `57014` tiempo límite, etc. Omite mensaje/detail/hint/where completos y rutas privadas. Los estados `COUNTS` contienen datos agregados; `INCIDENCE` señala una validación no satisfecha aunque la lectura termine correctamente; `ERROR` señala una fase que falló. Salidas: 0 completado, 1 incidencia/fase incompleta, 2 configuración no accesible. `OK` de una fase confirma su lectura, no subsana una incidencia indicada.

**Se necesita la salida de ese comando para corregir la causa institucional.** Compartir sus líneas `COUNTS`, `ERROR`, `INCIDENCE` y `result`; no compartir secretos ni registros externos completos. Por ahora no se solicita un permiso/contrato nuevo ni aplicar una migración para intentar resolver el aviso a ciegas. Una vez identificado el código, comprobar sólo el objeto, correspondencia o versión señalado. Nexo se conserva sin modificaciones.

## Pruebas y archivos

Pruebas dirigidas con PostgreSQL real desechable y fixture mínimo sintético, **sin ejecutar migraciones**:

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -PopulationDiagnosisOnly -SkipBrowser
```

Ocho grupos: 10=6+2+2, defecto de estado del catálogo corregido, aplicación sin módulo Encuesta, nivel 2 y descendientes N, alcances supervisor/responsable 2/3, adscripción incompleta, permiso/columna faltantes con SQLSTATE, historial pendiente sin aplicación, ID de aplicación incorrecto, roles/correos y empleado duplicados, vacío verificado, protección real de sólo lectura y salida sin secretos. El fixture simula las filas del historial EF para verificar su lectura; no equivale a haber aplicado migraciones en esta revisión. No se ejecutan casos de reinicio/reapertura.

Cambios: `PostgresNexoPopulation.cs`, `DigitalAggregates.cs`, `Aggregates.tsx`, `types.ts`; herramienta `DigitalPopulationReadOnly.cs`, nuevo `scripts/Diagnose-DigitalPopulation.ps1`, nuevo `DigitalPopulationDiagnosticTests.cs` y selección explícita en `NativeTests.cs`/`Test-NativePostgres.ps1`; README, ESTADO y esta guía. Se conservan archivos previos pendientes, migraciones, submódulos, edición, autoguardado, auditoría, instrumentos y diseño.

**Resultados finales:** ocho grupos PostgreSQL correctos, cero fallos; 116 pruebas generales de reglas/transporte y 102 frontend correctas; TypeScript, Vite y compilación Release correctos (0 errores/advertencias). `git diff --check` sin errores. El clúster desechable fue detenido. No se reejecutó la batería visual ni toda la suite de 79 casos que incluye migraciones y operaciones administrativas; los resultados de esa entrega anterior no se atribuyen a esta revisión.

[Resultados de las ocho pruebas](evidencia/20261030-diagnostico-poblacion/population-diagnosis-tests.json), [salida de población sintética válida](evidencia/20261030-diagnostico-poblacion/diagnostic-valid.jsonl) y [salida con una adscripción sintética incompleta](evidencia/20261030-diagnostico-poblacion/diagnostic-incidence.jsonl). Las tres conexiones de este fixture apuntan deliberadamente a una misma base desechable con vistas/tablas mínimas y rol lector; esto verifica consultas y controles de lectura, **no** separación física de bases, OAuth ni publicación real de Nexo. El servicio real de agregados se probó directamente con alcances sintéticos, sin iniciar el servidor.

La repetición del comando operativo en este proceso terminó otra vez en `configuration / CONFIG_MISSING`, sin conectar. No se afirma que las pruebas sintéticas hayan confirmado el caso institucional ni que se haya corregido ya el aviso real. No se requieren migraciones o permisos nuevos por esta instrumentación; cualquier ajuste institucional depende del resultado dirigido pendiente.
