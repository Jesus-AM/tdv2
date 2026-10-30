# Población por usuario canónico y estados de encuesta — 2026-10-30

## Definición vigente

Esta revisión sustituye la regla anterior que impedía certificar el total cuando varios usuarios compartían empleado. **La población se cuenta por `nexo_usuarios.usuario_id`**, dentro del acceso efectivo a Competencias digitales y su adscripción/alcance autorizado. `num_empleado` se utiliza para correspondencias con la captura existente; no deduplica ni reduce la población.

| Indicador | Definición |
| --- | --- |
| Personas que deben responder | Usuarios canónicos únicos con acceso vigente a esta aplicación dentro del alcance. Incluye quienes nunca ingresaron y no tienen usuario local/encuesta. |
| Encuestas finalizadas | Usuarios de esa población con encuesta propia definitiva, cerrada y vigente en la generación actual. |
| Encuestas en curso | Usuarios con captura propia real, sin cierre vigente; incluye revisiones reabiertas. |
| Personas sin iniciar | Usuarios sin captura real: sin registro, registro vacío o sólo datos institucionales/objetos pendientes inicializados. No incluye correspondencias sin resolver. |
| Porcentaje de encuestas finalizadas | `100 × Usuarios con cierre vigente / Usuarios con acceso vigente`. Con población cero no se divide ni se muestra 100 %. |
| Madurez | Evaluación congelada de finales atribuidos a esa población, independiente del total de usuarios y del avance de captura. No se añaden ceros para quienes no contestaron. |

Los roles/asignaciones repetidos del mismo canónico no multiplican usuarios. Dos canónicos diferentes con el mismo empleado permanecen como dos usuarios con acceso; tampoco se unen sus permisos. No se exige módulo Encuesta ni se cuentan personas autorizadas sólo para otros sitios. Se conserva el contrato efectivo de pertenencia/roles admitidos utilizado por el acceso actual de Competencias.

## Evidencia real disponible

El operador informó, mediante el diagnóstico anterior: 80 usuarios/candidatos con acceso, cero sin rol aceptado, `employee_conflict=3`, `canonical_conflict=0`, 9 encuestas almacenadas, 8 finalizadas, 0 en curso y una sin captura; sin empleados duplicados en encuestas, sin informes finales inconsistentes y sin encuestas sin correspondencia por empleado. Configuración, conexiones, publicación y SELECT correctos; sin migraciones Digital pendientes.

**Comprobado:** el cálculo anterior reducía a 77 las claves de empleado y rechazaba globalmente esos tres grupos. Eso no representa el total de usuarios canónicos. Se retira ese bloqueo del denominador y el diagnóstico deja de usar `Distinct(Employee)` para `level2Population`.

**No comprobado todavía con los registros reales:** si alguno de esos tres grupos afecta una de las ocho encuestas, o qué vínculo estable identifica su dueño. «Sin encuestas sin correspondencia» sólo probaba que el empleado existía, no una relación uno a uno con canónicos. No se afirma que el porcentaje real sea ya 10 % sin repetir el cruce actualizado. Los registros personales continúan reservados al archivo privado del operador; no se incorporan a Git ni a esta documentación.

Con los mismos datos publicados y las demás validaciones correctas, el denominador debe obtener dinámicamente 80 canónicos, sin reducirse por empleado compartido. El escenario **sintético** resuelto de 80/8/0/72/10 % está probado; las cifras no son constantes de producción.

## Consulta de población y alcance

`PostgresNexoProfiles.PopulationIdentityCte` conserva las vistas reales `nexo_aplicacion`, `nexo_usuarios` y `nexo_usuario_rol`. `ApplicationId` verifica el ID configurado de Competencias. La publicación central ya resuelve vigencia y concesiones; no se añade una autorización alternativa.

`NexoPopulationInspection.Invalid` sigue rechazando contradicciones de canónico, UR, origen y contrato. `employee` y `employee_conflict` se mantienen como incidencias de correspondencia, sin bloquear por sí solas el conteo de acceso. Cada registro devuelve además `EmployeeUnique`, calculado en la publicación **completa**, antes del filtro por UR. Así, una cuenta de otra dependencia con el mismo empleado no desaparece del análisis de ambigüedad ni permite atribuir su encuesta al único candidato visible en una rama.

El total deduplica `UserId`. Unidad, nivel 2 predeterminado, niveles 2–5 y descendientes siguen aplicándose sobre las UR autorizadas. Los nodos N permanecen como puentes internos y no son opciones del selector. El total no suma filas de unidades superiores/dependientes. Responsables 2/3 conservan sus raíces; filtros manipulados siguen rechazándose en backend.

## Correspondencia estadística con las encuestas

El esquema vigente guarda una encuesta por `Employee`; la captura no tiene una columna de ID canónico de Nexo. Esta revisión no cambia la propiedad/autorización de recursos, no modifica respuestas y no crea encuestas por cuenta adicional.

El nuevo servicio `DigitalSurveyCorrespondence` se ejecuta sólo al consultar Avance:

1. Inspecciona las encuestas del conjunto de empleados presentes en la población consultada; no descarga respuestas al navegador.
2. Un registro vacío no necesita atribuirse para clasificar «Sin iniciar»: no contiene captura que pueda asignarse a otra persona.
3. Para capturas existentes consulta en un único lote los vínculos de identidad de `users` y `tdv2_identity_links`. Exige coincidencia exacta de tenant/objeto Microsoft entre ambas tablas, un canónico único y acreditación anterior a la creación de la encuesta. El correo guardado sólo localiza ese vínculo: no basta por sí mismo ni se comparan nombres.
4. Para no utilizar un correo reasignado como prueba histórica, la creación y última actualización de la fila local deben ser anteriores o iguales a la creación de la captura. Los timestamps locales sin zona se interpretan como UTC, conforme a su persistencia. Un vínculo/fila modificado después no acredita retrospectivamente el dueño de esa encuesta. Esta regla conservadora puede dejar un caso pendiente tras una actualización local; no atribuye una aceptación ni cambia identidades.
5. La prueba estable válida prevalece sobre el cruce de empleado. Si identifica un dueño fuera de la población/alcance, la encuesta no se asigna a otro canónico visible. Si ese dueño sí está presente pero su empleado publicado contradice la referencia de la encuesta, los usuarios afectados quedan pendientes; no se utiliza la contradicción para declararlos sin iniciar.
6. Sin prueba contradictoria, se conserva la correspondencia institucional preexistente de empleado **globalmente único** con un solo canónico. No se utiliza esta vía para escoger entre varios usuarios que comparten empleado.
7. Un empleado compartido con captura y sin prueba inequívoca deja pendientes los usuarios candidatos afectados. La encuesta no se cuenta varias veces. Una fuente de vínculos no disponible se señala en el diagnóstico y no se interpreta como una vinculación positiva. No se intenta Graph, no se crean usuarios y no se asignan permisos.

El correo actual de Nexo o la sola existencia de un usuario local no son pruebas suficientes para resolver un empleado compartido. Tampoco se elige el primer canónico, el que tenga más roles ni la UR más conveniente. Los vínculos de Microsoft existentes no se reescriben. No se añaden tablas, migraciones, secretos, conexiones ni permisos de Azure. Las lecturas usan la conexión TDV2 y permisos SELECT ya existentes de identidad.

## Disponibilidad por cálculo

`populationAvailable=true` significa que el denominador canónico pudo comprobarse. No depende de la antigua validación global de empleado. Las ambigüedades se analizan por conjunto/UR y tipo de captura:

- Si los empleados compartidos no tienen encuestas capturadas, no bloquean ningún estado: esos usuarios están sin iniciar.
- Una encuesta finalizada ambigua deja pendientes Finalizadas, Sin iniciar y el porcentaje; puede conservarse un En curso comprobado.
- Una captura en curso ambigua deja pendientes En curso y Sin iniciar; si el numerador de finales está completo y comprobado, su porcentaje puede calcularse.
- Un empleado ausente no permite concluir el estado personal, pero el canónico permanece en el total de acceso.
- Una incidencia en otra rama no bloquea automáticamente los estados de un conjunto que no contiene candidatos afectados.

Cuando todos los estados están resueltos se cumple `Finalizadas + En curso + Sin iniciar = Población`. No se completa Sin iniciar por diferencia con estados incompletos. Un único aviso indica que el total está comprobado y algunos estados esperan correspondencia. `summary.populationProblem=correspondence` diferencia este caso de un fallo de consulta/contrato.

Los conteos `summary.registered` se conservan como información independiente, limitada en SQL por la UR **guardada** autorizada. La interfaz usa primero los estados certificados y sólo recurre a «Registradas» para los campos pendientes. Esos conteos no sustituyen el numerador ni el denominador. Madurez no presenta un promedio parcial como si incluyera finales ambiguos.

## Versiones, históricos y rendimiento

Se usa la encuesta/revisión activa dentro del control de generación existente. Una encuesta finalizada de otra edición técnica sigue siendo válida. Una reapertura cambia el estado actual a En curso; el último definitivo archivado permanece en Resultados generales/historial, sin contar como otro usuario ni cierre actual. No se recalculan sus informes ni se reescriben sus instantáneas.

Los agregados conservan transacción de lectura consistente y bloqueo compartido de generación. No hay consultas de población/identidad en las rutas de preguntas, autoguardado o Guardar avance. El cruce añade como máximo una lectura agrupada de vínculos por reporte con captura; los conteos registrados adicionales sólo se consultan cuando falta población o correspondencia. No se ejecutan reintentos ni trabajadores. Encuesta, autoguardado y cola de auditoría conservan su código y comportamiento.

## Diagnóstico del operador

```powershell
Set-Location 'C:\Users\Jesus Arenas\source\repos\tdv2'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Diagnose-DigitalPopulation.ps1
```

Ahora `population.validation.verified` comprueba el denominador por canónico, sin rechazarlo sólo por `employee_conflict`. La fase nueva `survey.canonicalStates` muestra población, finalizadas, en curso, sin iniciar, porcentaje, cantidad de usuarios pendientes y disponibilidad de la fuente de vínculos. `survey.counts.level2Population` usa el número de canónicos; sus estados proceden del mismo cruce, no de restar empleados distintos. Los conteos almacenados continúan separados.

Si quedan pendientes, usar la exportación privada existente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Diagnose-DigitalPopulation.ps1 -PrivateEmployeeConflicts
```

El archivo permanece en `%LOCALAPPDATA%\TDV2\diagnosticos`, fuera de Git, con IDs mínimos y fechas de creación/acreditación/actualización para revisar el cruce. No se imprimen personas, respuestas o credenciales. No compartir su contenido en documentación pública. El operador debe contrastar sólo los grupos que afecten a capturas: determinar si son cuentas de la misma persona o asociaciones incorrectas en origen. Compartir empleado no requiere eliminar un usuario para obtener un total de acceso; cualquier corrección institucional exige su procedimiento autorizado y no se ejecuta aquí.

## Archivos y verificación

Producción: `PostgresNexoPopulation.cs`, nuevo `DigitalSurveyCorrespondence.cs`, `DigitalAggregates.cs`, `Aggregates.tsx`, `types.ts`. Diagnóstico/pruebas: `DigitalPopulationReadOnly.cs`, `DigitalPopulationPrivateConflicts.cs`, `DigitalPopulationDiagnosticTests.cs`, ajuste del caso existente en `DigitalPopulationTests.cs`, recorrido `digital-registered-counts-ui.mjs`. README, ESTADO y GUIA actualizados. Los nombres internos `Participation`/`participation` se conservan por compatibilidad del contrato; la etiqueta visible en Avance es **Porcentaje de encuestas finalizadas**.

Comprobaciones realizadas: **13 grupos PostgreSQL aislado correctos, cero fallos**, sin migraciones; TypeScript y Vite correctos; compilación Release sin errores/advertencias; 102 pruebas frontend correctas. Seis escenarios de navegador sobre el paquete compilado (estado pendiente y resuelto a 1440/900/390 px), sin errores JS ni desbordamiento, con la etiqueta completa y fondo calculado de navegación `rgb(33, 52, 85)`.

La batería dirigida PostgreSQL verifica población por canónico, tres grupos de empleados compartidos no bloqueantes cuando no hay captura asociada, deduplicación de roles/asignaciones, ejemplo 80/8/0/72/10 %, registro inicializado sin respuestas reales, vínculos anteriores/posteriores, actualización local posterior, falta de SELECT de identidad, contradicción de empleado/vínculo, separación de UR y consultas fuera de alcance. Conserva pruebas de fuente caída, población vacía, histórico y revisión reabierta simulada. No se ejecuta EF Migrate; el fixture mínimo y sus filas de historial son sintéticos. No se reejecuta la suite completa que aplica migraciones o ejecuta administración.

Comandos de reproducción:

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

[Resultados PostgreSQL](evidencia/20261030-poblacion-canonica/population-diagnosis-tests.json) y [recorrido de navegador](evidencia/20261030-poblacion-canonica/report.json); estado resuelto en [escritorio](evidencia/20261030-poblacion-canonica/resolved-1440.png), [tableta](evidencia/20261030-poblacion-canonica/resolved-900.png) y [móvil](evidencia/20261030-poblacion-canonica/resolved-390.png); correspondencia pendiente en [escritorio](evidencia/20261030-poblacion-canonica/pending-1440.png) y [móvil](evidencia/20261030-poblacion-canonica/pending-390.png). Las capturas usan datos y transporte sintéticos; no prueban HTTP/SQL institucional. Las pruebas PostgreSQL usan el proveedor real en una base desechable y las tres conexiones del fixture mínimo apuntan deliberadamente a esa base. El clúster fue detenido al terminar.

**Límite real:** este proceso vuelve a terminar en `CONFIG_MISSING` al ejecutar la herramienta operativa. La evidencia real disponible sigue siendo la proporcionada por el operador. Para confirmar el estado de las ocho encuestas y el resultado institucional actualizado, repetir el diagnóstico y revisar `survey.canonicalStates`. No se certifican estados reales únicamente con las pruebas sintéticas. No se aplicó una migración ni se ejecutó una reapertura/reinicio real, cambio de permisos/secreto, push o despliegue.
