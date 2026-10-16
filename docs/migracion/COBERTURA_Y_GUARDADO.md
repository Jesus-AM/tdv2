# Herramientas opcionales y guardado — regla vigente 2026-10-16

**Actualización vigente 2026-10-16 (catálogo):** herramientas opcionales por procedimiento, nuevas familias Microsoft 365/Adobe, opciones históricas conservadas y duplicados nuevos rechazados. Medios utilizados y Otro medio están retirados, incluida su persistencia específica. Requiere `20261016190000_RemoveUsedMedia`; el resto de los envíos permanece intacto. [Catálogo vigente, limpieza exacta, respaldo y actualización Windows/Ubuntu](CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md). Las verificaciones y decisiones anteriores de este documento son evidencia fechada, no instrucciones para reinstalar el catálogo ni Medios.

## Implementado

Esta regla sustituye la cobertura obligatoria del 2026-10-14, cuyo registro histórico se conserva abajo. Un procedimiento admite **cero, una o varias herramientas**. No existe mínimo de herramientas ni requisito de cobertura, incluso si no hay procedimientos relacionables. Identificación general conserva sus validaciones. Las herramientas que sí se inicien deben tener procedimiento confirmado y válido, opción de herramienta, uso y funcionamiento; se evalúan también sus detalles condicionales. La ausencia de herramientas no se convierte en un requisito ni en una respuesta de «no utiliza».

`FormRequirements.Inspect` alimenta Inicio, porcentaje confirmado, resumen, pendientes y envío. El API valida el mismo resultado dentro de su transacción, con permisos, versión, reservas y auditoría vigentes. El dato `cobertura` se conserva como información del contrato anterior, pero ya no genera requisitos. Se retiró la alta dirigida desde pendientes de cobertura. **Ir al campo** sigue localizando filas incompletas, guardando antes de navegar. No se añade un panel de cobertura.

Un formato nuevo usa `FormSchema.New`: Sistemas empieza vacío y **Agregar fila** toma la plantilla existente sólo a petición del usuario. No se borran filas anteriores. La antigua fila `inicial` completamente vacía sigue siendo ayuda y no cuenta; cualquier fila agregada o iniciada sí cuenta, incluso si está incompleta. Borrar la última herramienta deja cero requisitos de herramientas. Borrar un procedimiento conserva las respuestas de herramientas y deja pendiente su asociación; renombrarlo conserva el vínculo estable por código confirmado.

## Fórmula vigente

`avance = floor(100 × V / T)`. `V`: requisitos aplicables válidos; `T`: requisitos aplicables. Sin ponderaciones ni requisito extra de cobertura:

- Cada procedimiento real: **7** requisitos vigentes (nombre, destinatarios, resultado, responsable, validación V/A, prioridad y código confirmado).
- Cada herramienta real iniciada: **4** requisitos (procedimiento, herramienta, uso y funcionamiento).
- Por fila: **1** adicional si requiere módulos SIIv2 o aplicaciones móviles/web; todos los elementos deben ser válidos, pero seleccionar varios no suma requisitos. «Otra herramienta» y «Otro uso» añaden su detalle requerido, uno cada uno cuando corresponda.
- Contexto sigue siendo informativo; comentarios y secciones posteriores no penalizan. Sin procedimientos reales queda un requisito incumplido de Identificación; sin herramientas no se añade ninguno. Si `T=0`, 0 % y revisión no lista.

Ejemplo: dos procedimientos completos y ninguna herramienta = **14/14, 100 %**. Una herramienta asociada a cualquiera, con herramienta y funcionamiento válidos pero sin uso, resulta en **17/18, 94 %**, con un pendiente y envío rechazado. Completar el uso da 18/18; eliminar la herramienta devuelve 14/14. Varias herramientas pueden compartir procedimiento. Inicio y grupos mantienen la media aritmética de los porcentajes confirmados de formatos únicos por `id_ur`, truncada hacia abajo. Captura completa no equivale a envío.

## Terminología y compatibilidad

Portal, instrucciones, ayudas, eliminación y mensajes de servidor usan «procedimiento» para la captura. Se conserva «servicios» cuando significa prestaciones universitarias o continuidad tecnológica, y «proceso institucional» para la transformación como cambio organizacional. Nombres oficiales, catálogos, valores introducidos por usuarios y claves internas (`tramite`, `proceso`, rutas, contratos, migraciones) no se sustituyen. Las preguntas de plantilla y opciones de origen de Datos con términos anteriores tienen etiquetas de presentación explícitas, sin reescribir valores almacenados ni sustituir texto libre.

Ninguna migración, backfill, configuración ni actualización de catálogos se requiere por este cambio. Publicar posteriormente backend y frontend juntos mediante el flujo habitual autorizado; este trabajo no publica. Los GET recalculan proyecciones de borradores sin escribir; el siguiente guardado legítimo persiste el avance. Enviados e históricos conservan contenido, fechas, porcentaje persistido, instantáneas y bloqueo.

## Verificación de esta entrega

Tipos TypeScript, compilaciones .NET/Vite, 115 pruebas dominio/HTTP y 89 frontend correctas. Verificados 19 casos nativos distintos con PostgreSQL real aislado, incluidas operaciones simultáneas sobre conexiones independientes, más 8 recorridos Edge con backend y persistencia reales. Los cinco casos nativos con fixtures antiguos de «captura completa» se corrigieron y repitieron; el detalle de las corridas intermedias, evidencias y límites está en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md#2026-10-16--terminología-y-herramientas-opcionales). Fuentes/identidades sintéticas; no es validación institucional. Dispositivos físicos y lector de pantalla pendientes.

Comandos de verificación desde la raíz (Node/npm en PATH):

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tests/TDV2.Verification/TDV2.Verification.csproj -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
$env:TDV2_TEST_CASE_PREFIX='Edición/integridad:|Edición/módulos|Edición/envío|Edición/regresión: códigos|Edición/etapa: cien|Edición/etapas: revisión'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
$env:TDV2_TEST_CASE_PREFIX=''
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
$env:TDV2_TEST_BROWSER_FLOW='coverage'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

El nombre técnico del recorrido `coverage` se conserva por compatibilidad; comprueba ahora la captura opcional. No ejecutarlo contra instalaciones existentes: el script crea y detiene su propio clúster desechable.

---

## Historial del 2026-10-14 — cobertura obligatoria sustituida

# Cobertura y guardado — 2026-10-14

## Implementado

Se conserva el modelo híbrido: `formatos_ur.contenido` (`json`), reservas/versiones por bloque, recibos idempotentes y snapshots en tablas existentes. No hay nuevas tablas ni otro flujo de guardado.

`FormRequirements.Inspect` centraliza campos y cobertura. Reutiliza `ProcedureEligibility.Available`: ID/código y campos obligatorios válidos, con validación V/A. Los procedimientos incompletos y D/N anteriores siguen visibles y pendientes de corregir en Identificación; no son excepciones de entrega. El catálogo actual no incluye «no utiliza sistema». `sin_uso` significa «Ya no se usa» para una herramienta, no exención. CAST histórico conserva sus reglas. No se añaden opciones ni requisitos posteriores.

Cada procedimiento aplicable necesita al menos una fila real asociada con todos sus requisitos válidos, incluidos condicionales. Varias filas siguen permitidas. Una fila válida no excusa otras incompletas. Eliminar la última herramienta devuelve el pendiente. Módulos SIIv2 sigue siendo un único requisito por fila, con al menos un elemento y todos válidos/sin duplicados.

El mismo resultado alimenta Inicio, porcentaje al guardar, revisión y `POST /formatos/{ur}/enviar`. Envío directo sin cobertura: 422. Permisos, reservas y versión vigente siguen comprobándose antes de confirmar.

## Fórmula vigente

`porcentaje = floor(100 × requisitos válidos / requisitos aplicables)`. Sin requisitos: 0 y revisión no lista. No hay pesos por sección.

- Siete comprobaciones por procedimiento real, sin cambios de obligatoriedad.
- Cuatro por herramienta real y una por cada condicional activo: módulos, Otra herramienta, Otro uso.
- **Una cobertura por procedimiento aplicable**, independientemente de la cantidad de herramientas/módulos. Su validez deriva de los requisitos ya evaluados de las filas; no se crea otro conjunto de campos para calcularla.
- Cuando hay procedimientos aplicables, la cobertura sustituye el mínimo genérico de una herramienta. Sin aplicables, se conserva ese mínimo previo junto con los pendientes de Identificación.
- Contexto y comentarios siguen opcionales/informativos. Sólo las filas iniciales intactas se consideran ayudas.

Ejemplo: dos procedimientos completos y una herramienta ordinaria válida para uno suman 14 requisitos de Identificación + 4 de herramienta + 2 de cobertura = 20; cumplen 19: **95 %**, pendiente y envío rechazado. Añadir una herramienta válida para el segundo produce 24/24 = **100 %**, todavía borrador. Borrar esa fila devuelve 19/20. Un procedimiento y una herramienta completos son 12/12; SII añade un requisito tanto con uno como con varios módulos. Si queda sin módulos, incumple ese requisito y su cobertura cuando no hay otra fila válida.

El promedio de Inicio no cambia: `floor(sum(porcentajes individuales) / N)` sobre formatos únicos del alcance. GET recalcula borradores sin reescribirlos; el próximo guardado persiste la nueva evaluación. No hay backfill. Borradores antes completos pueden bajar por cobertura; enviados/históricos conservan porcentaje persistido y bloqueo.

## Presentación y retiro

**Ajuste posterior de presentación, 2026-10-14:** se retira de Sistemas el bloque «Procedimientos y captura de herramientas». La tabla, sus acciones, Medios y el desplazamiento independiente se conservan. La evaluación y la cobertura del servidor no cambian.

Los pendientes de **Revisar y enviar** identifican código/nombre completo y campo. **Agregar registro** abre Sistemas y crea una fila sólo por esta intención explícita, con el código confirmado; revalida la disponibilidad y evita duplicar una fila ya existente. **Ir al campo** localiza/enfoca el control de una fila incompleta. Si el procedimiento dejó de estar disponible durante la consulta, lleva a Identificación. Se espera el guardado antes de navegar y se conserva su error si falla. Preparar el alta mantiene Guardar/Enviar deshabilitados hasta terminar. No se crean filas para mostrar pendientes.

La confirmación MUI informa el número de herramientas/datos que perderán asociación. Conserva respuestas y facilita reasignación en Sistemas. El backend mantiene `FormRemoval.Changes`, reserva conjunta, versiones y comparación del plan vigente; cambios relacionados rechazan el plan obsoleto. Renombrar conserva código y relaciones.

## Evolución JSON y actualización

`FormDocument` define `versionEstructura: 1`, independiente de las versiones de concurrencia. La ausencia significa versión 0. Consultar documentos anteriores no los transforma. Nuevos documentos llevan 1; un guardado confirmado añade la marca al borrador anterior conservando otros bloques/IDs. Un fallo revierte también la marca. Estructuras desconocidas o mal formadas se rechazan sin reinterpretar respuestas.

Contenido y snapshots enviados existentes permanecen intactos. Nuevos snapshots incluyen la versión de estructura como metadato. La compatibilidad SII múltiple sigue leyendo el escalar anterior como un elemento; una colección vacía prevalece y no reactiva el escalar.

**Sin migración EF nueva, backfill, SQL ni configuración adicional.** Para una futura actualización autorizada, publicar backend y bundle frontend de la misma versión y retirar procesos anteriores para que todos los escritores apliquen cobertura. Las migraciones previas del proyecto siguen siendo prerrequisito; no se modifican ni se ejecutan al arrancar. No se publicó nada en esta entrega.

## Probado

- 108/108 dominio/HTTP con dobles: cobertura, fórmula, filas, condicionales, módulos, D/N, estructura y coherencia Inicio/formato/revisión.
- 88/88 frontend; TypeScript, Vite y .NET Release correctos.
- **10 casos nativos distintos** ASP.NET/PostgreSQL 18 aislado. Primera corrida 9/9 en `.artifacts/native-postgres-20261014-143727-2d175a8f`; repetición ampliada 6/6 en `.artifacts/native-postgres-20261014-144329-cc5c4878`, con rollback de estructura e histórico enviado. Incluyen cobertura/envío API, última herramienta, renombre/retiro/conflicto, selección múltiple/recarga, bloque compartido, filas distintas, caducidad, solicitudes atrasadas y recibos.
- Dos clientes HTTP/sesiones independientes. La prueba observa **dos conexiones esperando `wait_event_type='Lock'` en PostgreSQL**, libera el bloqueo y verifica ambas escrituras. No es transporte simulado. Misma identidad sintética en sesiones distintas; no se presenta como dos usuarios diferentes.
- **5/5 recorridos Edge → ASP.NET → PostgreSQL** en la última corrida: pendientes/foco/alta, captura/recarga, última herramienta/recreación sin duplicación, eliminación/cantidad/reasignación, nombres largos, escritorio/móvil emulado 1366/390/320, teclado, envío y bloqueo. Capturas y `coverage-native-browser.json` en esa carpeta. Corridas anteriores de UI se conservan como diagnóstico y no se suman como aprobadas.
- **12/12 grupos Edge con transporte simulado** para regresión de módulos múltiples, nombres, foco, errores y último elemento: `.artifacts/sii-multiple-ui/report.json`.

No se consultaron fuentes institucionales. ASP.NET/PostgreSQL reales en pruebas nativas; identidad/fuentes sintéticas. Dispositivos físicos, lector de pantalla, carga y aceptación institucional pendientes.

## Comandos reproducibles

Desde la raíz, dependencias existentes:

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:NuGetAudit=false -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:NuGetAudit=false -p:UseAppHost=false
$env:PATH='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;'+$env:PATH
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
$env:TDV2_TEST_CASE_PREFIX='Edición/integridad:|Edición/módulos múltiples:|Edición/dos sesiones|Edición/reintento idéntico|Edición/bloques distintos|Edición/vencimiento'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
$env:TDV2_TEST_BROWSER_FLOW='coverage'
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs/node.exe'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

## Archivos de esta mejora

Backend: `Domain/FormRequirements.cs`, `FormDocument.cs` (nuevo), `FormSchema.cs`, `FormStages.cs`, `Infrastructure/PostgresFormStore.cs`, `Services/FormEditingService.cs`.

Frontend inicial: `Components/ProcedureCoverageList.tsx` (retirado en el ajuste de presentación posterior), `FormSubmissionReview.tsx`, `RemoveRowDialog.tsx`, `pages/FormatoUR.tsx`, `types/tdv2.ts`.

Pruebas: `TDV2.Verification/CaptureProgressTests.cs`, `EntryPoint.cs`; `TDV2.NativeVerification/FormIntegrityTests.cs` (nuevo), `NativeTests.cs`, `NativeApplication.cs`; `ClientApp/tests/browser/procedure-coverage-native.mjs` (nuevo). Se conservan los cambios locales anteriores de módulos múltiples y su regresión.
