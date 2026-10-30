# Revisión de los fallos adicionales de TDV2 · 2026-10-30

## Resultado y comparación comprobada

Se reprodujeron **los diez fallos pendientes** de la ejecución ampliada registrada en [sesión y reportes](SESIONES_DOS_HORAS_Y_REPORTES_20261030.md). Los diez también fallan en una copia aislada del commit **`06e307c`**, anterior a la política de sesión compartida. Tras corregir los fixtures y expectativas obsoletas, los diez pasan en el estado local actual.

Al ejecutar casos relacionados apareció un undécimo fallo, `Edición/entrega: elegibilidad única…`, también reproducido en el mismo punto del commit anterior. La preparación reutilizaba otra selección retirada de `excel`. Se corrigió en `DeliveryTests.cs` y se reforzó la comprobación del campo rechazado. [Reproducción anterior adicional](evidencia/fallos-suite-20261030/baseline-entrega.json), [ejecución relacionada antes de corregirlo: 29/30](evidencia/fallos-suite-20261030/relacionados-antes.json). La ejecución final reúne ambos conjuntos sin duplicados: **37/37 aprobados**, incluidos los once fallos resueltos; [comparación final de los once](evidencia/fallos-suite-20261030/comparacion-final.json).

**No se encontró una regresión de producción en estos casos ni se necesitó cambiar una expectativa por la sesión de 120 minutos.** Se corrigieron exclusivamente cinco archivos de pruebas. No se debilitaron autenticación, claves únicas, permisos, validaciones de captura, concurrencia ni bloqueos de envíos definitivos. El conjunto completo de la suite ampliada no se ejecutó y no se declara aprobado por esta selección.

El negativo Microsoft que ya se había corregido en la entrega anterior no forma parte de estos diez: conservaba UPN institucional al simular un `mail` externo. Su corrección y resultado anterior siguen registrados en la documentación de sesión.

Se conservó el diff inicial en `.artifacts/tdv2-suite-review-20261030/estado-inicial.diff`. `git archive HEAD` extrajo código, contratos, fixtures y runner a `.artifacts/tdv2-suite-review-20261030/baseline`, sin User Secrets, configuración privada, bases existentes, `.env` ni dependencias de Herd. Se compiló con el mismo SDK y paquetes del caché local y se ejecutaron los mismos diez casos sobre otro clúster PostgreSQL desechable: **anterior 0/10, local antes de corregir 0/10**.

Una segunda ejecución anterior de tres casos añadió sólo diagnóstico de pruebas: estado HTTP, campos, requisitos pendientes y SQLSTATE/restricción, sin cuerpos de respuestas ni excepciones completas. No cambió producción ni aserciones de esa copia; [observaciones de consola saneadas](evidencia/fallos-suite-20261030/diagnostico-baseline.json). La comparación corresponde al commit indicado, no a una reconstrucción supuesta de todos los estados intermedios sin commit. Los resultados de los diez originales están en [comparacion.json](evidencia/fallos-suite-20261030/comparacion.json), [baseline](evidencia/fallos-suite-20261030/baseline-06e307c.json), [local anterior](evidencia/fallos-suite-20261030/actual-antes.json) y [corregido](evidencia/fallos-suite-20261030/actual-corregido.json).

## Cada fallo, causa y corrección

Todos los casos de la tabla fallaron en `06e307c` y en el estado inicial local; todos pasan después de corregirlos.

| Caso (prefijo del nombre exacto) | Causa comprobada | Corrección y comportamiento que sigue probándose |
|---|---|---|
| `Central/concesión sin rol no habilita…` | `Complete` tenía sistema/uso arbitrarios y estado `Funciona`, ajenos a las claves vigentes: tres pendientes y envío **422**, antes de probar la inmutabilidad. | Fixture válido con `limesurvey`, `consultar`, `bien`. Conserva el rechazo de escrituras de una etapa enviada y que una concesión sin rol no habilita formatos. |
| `Rendimiento/ILDA por conjuntos…` | Esperaba dos lecturas. El servicio ya consulta tres conjuntos en el commit anterior: metadatos, inventario y exclusiones. | Tres lecturas justificadas con **2 y 102 áreas**; conserva igualdad con consulta individual, claves exactas `06000`/`6000`, orden/límite de 200 y prueba una exclusión limitada a su área. |
| `Participación/enviado conserva instantánea…` | `SaveAll` intentaba enviar un código que corresponde asignar al servidor: **422, campo `codigo`**. La aserción posterior leía el campo legado en vez de la instantánea de etapa vigente. | Prepara explícitamente la captura del fixture aislado y envía por el API real. Exige una instantánea no vacía de `formato_envios_etapas` y la compara tras excluir/reactivar; no compara un campo legado vacío consigo mismo. |
| `Sync/web: doble solicitud y dos administradores…` | `SetupAccess` ya vinculaba el objeto Microsoft `eeee…` al canónico sintético 21. Reutilizarlo para el 60 provoca **23505, `PK_tdv2_identity_links`**, antes de las solicitudes concurrentes. | Otro objeto para la segunda administradora, con comprobación de que el vínculo del 21 sigue intacto. Conserva tres peticiones concurrentes, un 202, dos 409 y un solo trabajo/descarga. |
| `Edición/regresión: códigos y criterios confirmados…` | Intentaba crear una selección nueva de `excel`, retirado como opción individual del catálogo. | Herramienta seleccionable vigente; conserva altas simultáneas, códigos/criterios asignados por servidor y rechazo de renombrado/borrado del código confirmado. |
| `Edición/regresión: un vínculo inválido histórico…` | Suponía una herramienta vacía en un formato nuevo, que actualmente inicia sin herramientas; acceso al índice 0 inexistente. | Captura histórica explícita con fila y vínculo inválido. Cambiar un comentario no altera el vínculo; corregirlo después es una acción explícita con versión/reserva. |
| `Edición/reproducción: referencia a código local no confirmado…` | La misma suposición de fila automática impedía llegar a la validación. | Construye la fila nueva desde una plantilla válida. Conserva 422 por referencia no confirmada y atomicidad: no se guarda herramienta ni acuerdo independiente; corregir el vínculo permite guardar. |
| `Edición/usuarios múltiples: colección vacía pendiente…` | La selección múltiple funcionaba; el fixture supuestamente completo no llegaba a 100 por los tres valores inválidos de herramienta. | `Complete` válido; conserva inválidos, duplicados, orden, recarga y edición. Vaciar destinatarios reduce avance y produce su pendiente. |
| `Edición/usuarios: envío rechaza colección vacía…` | El fixture añadía pendientes de herramienta ajenos al caso, alterando el número previsto. | Conserva los dos pendientes reales (destinatarios y asociación a procedimiento no elegible), rechazo del envío incompleto, corrección, cierre y prioridad intacta. |
| `Edición/revisión omite sesión…` | Esperaba revisión completa/100 desde el mismo fixture incompleto. | Conserva encabezado histórico no exigible, lecturas sin reescribir el 45 almacenado, 100 calculado consistente y contenido histórico conservado al enviar. |
| Adicional: `Edición/entrega: elegibilidad única…` | Tras corregir el procedimiento asociado intentaba crear `excel` como selección nueva. Falla en la misma línea 61 en el commit anterior. | Usa `power_bi`, vigente y distinta de la herramienta existente. Comprueba que el rechazo inicial es específicamente por `proceso`, la elegibilidad concurrente, conservación del vínculo histórico y guardado posterior de la segunda herramienta. |

`Complete` conserva la fuente `Nuevo`, usa claves reales y deja vacíos detalles condicionales ajenos a la herramienta. Ahora exige mediante una aserción que el fixture satisfaga todos los requisitos vigentes. Si cambia el contrato, falla al prepararlo; no omite la revisión del servidor.

El aviso inicial `initdb` sobre token restringido de Windows (87/3) no explica estos fallos: ambos clústeres se crearon y ejecutaron los casos. Los síntomas se reprodujeron en las aserciones/clave primaria descritas y desaparecieron con los ajustes. No se identificó una causa de entorno para ninguno de los diez.

## Archivos modificados

- `tests/TDV2.NativeVerification/EditingTests.cs`: fixture completo, filas históricas/nuevas explícitas y opción vigente en el caso de códigos.
- `tests/TDV2.NativeVerification/ParticipationTests.cs`: fixture inicial y comprobación de la instantánea real de etapa.
- `tests/TDV2.NativeVerification/ManualSyncTests.cs`: identidad sintética independiente, sin reemplazar un vínculo previo.
- `tests/TDV2.NativeVerification/PerformanceTests.cs`: expectativa justificada de tres lecturas y cobertura de escala/exclusiones.
- `tests/TDV2.NativeVerification/DeliveryTests.cs`: herramienta nueva vigente y rechazo atribuido al campo de asociación correcto.
- README, ESTADO_MIGRACION, este documento y enlace de actualización en `SESIONES_DOS_HORAS_Y_REPORTES_20261030.md`.

Sin cambios de producción, frontend, paquetes, esquema, migraciones o configuración. El resto del trabajo local se conserva.

Se compararon además los parches Git iniciales y finales: los **48 parches previos ajenos a esta revisión** conservan exactamente su contenido. Los archivos nuevos de trabajos anteriores no se restauraron ni descartaron.

## Verificación reproducible

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
$env:TDV2_TEST_CASE_PREFIX = 'Central/concesión sin rol|Rendimiento/ILDA|Participación/enviado|Sync/web: doble solicitud|Edición/regresión:|Edición/reproducción:|Edición/usuarios múltiples:|Edición/usuarios: envío|Edición/revisión omite'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX

$env:TDV2_TEST_CASE_PREFIX = 'Central/|Rendimiento/|Participación/|Edición/entrega:|Edición/catálogo vigente:|Edición/etapas: revisión y permiso|Sync/web: doble solicitud|Edición/regresión:|Edición/reproducción:|Edición/usuarios múltiples:|Edición/usuarios: envío|Edición/revisión omite'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX

$env:TDV2_TEST_CASE_PREFIX = 'Sesión dos horas'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
```

**Aprobado:** [37/37 casos afectados y relacionados](evidencia/fallos-suite-20261030/verificacion-final.json), compilación Release sin advertencias/errores, 116 comprobaciones de reglas/HTTP y cinco grupos específicos de sesión con reloj controlado. No quedan fallos pendientes entre los once investigados. [Medición ILDA](evidencia/fallos-suite-20261030/ilda-performance.json): tres lecturas con dos áreas y tres con 102. [Sesión](evidencia/fallos-suite-20261030/sesiones.json): límite exacto, actividad que no extiende, cambio de sitio/representación, tickets antiguos y protección de la nueva sesión frente a peticiones anteriores.

No se repitieron navegador/TypeScript/Vite porque no cambió frontend ni producción; la evidencia previa del regreso real a `/` de ambos sitios sigue en el documento de sesión. La política de 120 minutos, representación, autoguardado, auditoría y textos/cálculos de reportes permanecen intactos.

**Límites:** ASP.NET y PostgreSQL reales y aislados en Windows; Microsoft, Nexo y lectores remotos sintéticos. No se certifica integración institucional ni ejecución Ubuntu. No se requieren nuevos permisos, configuración o migraciones. Sin modificaciones institucionales, reinicios/reaperturas reales, push o despliegue. Cada runner detiene únicamente su clúster desechable.
