# Guardar, Colaboradores y distribución de Pruebas de acceso — 2026-10-10

## Ajuste de textos vigente — 2026-10-16

La ayuda ahora dice «Busca una persona por nombre o correo para agregarla como colaboradora.». Se eliminan, sin sustitutos, el pie sobre autorización/adscripción vigentes y el aviso general sobre asignaciones comprobables. `avisoAsignaciones` conserva su contrato: devuelve cadena vacía en la vista habitual y mantiene el aviso específico ya existente de la vista de prueba. Los dos avisos de Administrador sobre área principal/responsabilidad dejan de mencionar niveles numéricos; sus condiciones no cambian. Sin cambios de distribución, estilos, fuente, roles visibles ni reaparición de Origen.

Revisados `CollaboratorService`, `CollaboratorAccess`, `CollaboratorsController`, `CollaborationRules`, `CollaborationListing` y `Access`. Responsables administran sólo raíces autorizadas por responsabilidad y delegación vigente; Administrador no adquiere facultades por el rol solo. Local/dependencias conservan alcance, visibilidad y restricciones actuales; retirar sigue dependiendo del otorgante y de la autorización, no de poder ver una fila. Vigencia de identidad, empleado, adscripción, rol, concesión, vínculo y contexto se comprueba igual. Sólo cambian textos/elementos de texto, no decisiones, consultas ni persistencia. La matriz de abajo sigue vigente.

**Verificado en este ajuste:** tipos/Vite/.NET Release, 116/116 dominio/HTTP y nueve grupos de navegador existentes, incluidos búsqueda, ambos tipos de alta, retiro exacto, perfiles de responsables, representación de consulta, errores y teclado/móvil emulado. ASP.NET/TestServer usa dobles en memoria; Edge usa transporte simulado. Capturas y reportes en `.artifacts/collaborator-texts-20261016/`; fuentes locales y disposición anterior conservadas. Sin bases institucionales, migraciones, push o despliegue. Compilación conserva cuatro advertencias NuGet previas de Pkcs 9.0.18. No quedan pendientes de implementación del ajuste.

## Presentación vigente — 2026-10-16

Se restaura la apariencia anterior al último ajuste de vistas: área autorizante y buscador en la primera fila; Selecciona una persona y Define el alcance debajo; listado flexible original, sin las nuevas columnas/tarjetas. Vuelven sus tamaños, etiquetas, ayudas y espaciados. En el alta se muestran Colaborador local y Colaborador de áreas dependientes; en la lista, Local y Áreas dependientes, como antes. Esas etiquetas no cambian claves, alcance ni facultades. La única excepción es que no aparecen **Origen**, **delegación en TDV2**, **Agregado por Administrador** ni la explicación de procedencia del retiro. Se mantiene Solo consulta y toda la información funcional de estado. Datos internos, matriz de autorización y Roboto local intactos. [Restauración selectiva, flujos N2/N3, pruebas y límites](VISTAS_PROCEDIMIENTOS_COLABORADORES.md#restauración-visual-vigente).

## Evidencia anterior de presentación — 2026-10-11 (etiquetas de origen retiradas)

En Colaboradores, `origen = central` se presenta como **Agregado por Administrador**. No hay columna «Agregado por», por lo que se utiliza la frase completa. Se elimina la explicación «Se administra en NEXO» y se neutralizan los avisos auxiliares de procedencia; se conserva **Solo consulta**, la condición de autorización/adscripción vigente, el aviso de datos insuficientes y el estado de retiro pendiente. Las asignaciones delegadas mantienen su indicación, área y acciones existentes. No se cambia la identidad de TDV2 ni mensajes operativos de otros módulos.

El origen técnico y los identificadores siguen intactos en la proyección interna. El único cambio de servicio es la redacción del aviso de la lista; no se modifican consultas, filtros, permisos, auditoría ni persistencia. Los detalles están en la misma fila; el diálogo de retiro conserva nombre y área, sin mostrar procedencia técnica.

**Comprobado:** `npm.cmd --prefix ClientApp run types:check`, `npm.cmd --prefix ClientApp run build` y `dotnet build tdv2 -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false`, correctos. Desde ClientApp, `node tests/browser/collaboration-actions-ui.mjs`: **4/4**, con comprobaciones de etiqueta exacta, ausencia de procedencia técnica en textos/atributos accesibles/tooltips, delegaciones sin cambios, acciones permitidas, diálogo/teclado y escritorio/móvil 390/320 px con tacto emulado. También pasan los recorridos existentes de Guardar/Enviar de esa suite. Capturas revisadas y reporte en `.artifacts/collaboration-actions-20261011/`.

**Límites:** HTTP, identidad y colaboración simulados en navegador; sin consultas o modificaciones de bases. No se repitió validación institucional ni en dispositivos físicos. Sin migraciones, push o despliegue.

## Implementado

Se trabajó sobre Git con cambios locales previos, sin restaurar archivos. Se conserva tema MUI, barra blanca, tipografía y densidad; no se introduce barra azul ni modo oscuro. No hay dependencia ni migración nueva.

**Guardar** sustituye el nombre visible Guardar borrador y mantiene `engine.flush(true)`: confirma cambios y libera reservas, sin llamar al envío. Usa botón outlined, nombre accesible Guardar, ayuda «Guardar los cambios sin enviar el formato» y estado Guardando… mientras corresponde. La referencia de operación impide repetir la acción manual mientras sigue pendiente; el motor continúa serializando el autoguardado que pudo iniciar por blur. En revisión se muestra una sola vez, antes de **Enviar formato**, con separación, altura mínima de 40 px y ajuste a varias líneas en móvil.

Enviar conserva fondo azul del tema, permiso, captura completa/confirmada, control de reservas y diálogo. Antes de abrir la confirmación termina el guardado y actualiza el estado; la versión confirmada se vuelve a comprobar al enviar. Un cambio remoto invalida el diálogo, sin sobrescritura. El motor comparte una solicitud/recibo ante doble clic o reintento y el diálogo muestra Enviando…; sólo se anuncia Formato enviado después de la confirmación. No se habilita envío con cambios pendientes ni se convierten guardados en envíos.

## Visibilidad por asignación

La causa de las omisiones era `PostgresCollaborationStore.List`: leía únicamente vínculos cuyo `ur_otorgante` coincidía con una raíz administrable del usuario. No incluía altas centrales y omitía asignaciones locales otorgadas desde otra área de la rama.

Ahora se consultan candidatos locales por **`id_ur_alcance`** y se integran con concesiones/roles/identidad vigentes publicados por Nexo. `CollaborationRules` comparte las comprobaciones de alcance con `FormAccess`; `CollaborationListing` produce la proyección final filtrada en backend. La interfaz nunca decide qué rama puede consultar.

| Asignación | Responsable nivel 2 de la rama | Responsable nivel 3 del área asignada | Administración |
|---|---|---|---|
| Central Nexo, rol local válido | Visible | Visible si coincide su área | Sólo Nexo; sin Retirar local |
| Central Nexo, rol dependencias válido | Visible | No se propaga | Sólo Nexo |
| Nivel 2 otorga dependencias | Visible | No se propaga | Área nivel 2 otorgante |
| Nivel 2 otorga local | Visible | Visible | Área nivel 2 otorgante; nivel 3 consulta |
| Nivel 3 otorga local | Visible | Visible | Área nivel 3 otorgante; nivel 2 consulta |

La rama se calcula con el ancestro institucional nivel 2 más cercano, mediante IDs y jerarquía vigente. No se usan prefijos de clave, nombres de roles ni el creador como filtro de visibilidad. Una raíz nivel 2 anidada no convierte a la raíz externa en responsable de sus asignaciones. Los responsables de varias áreas conservan esos ámbitos; no se generaliza a cualquier nivel 2.

Las raíces de consulta de responsables provienen de la responsabilidad vigente por número de empleado textual, sin conceder delegación. Las facultades de modificación siguen requiriendo `nexo_delegacion`. Administradores mantienen su rama delegada autorizada incluso al combinar roles. La vista por rol/área no publica listas personales; la representación conserva la identidad efectiva y la marca de sólo lectura.

Cada fila expone sólo clave estable de asignación, ID local cuando existe, nombre, correo, área asignada, alcance local/dependencias, origen, estado de retiro y `puedeRetirar`. No expone empleado, token ni objetos de perfil completos. La clave combina origen/concesión/área: una asignación no se duplica, pero una persona puede tener varias filas con alcances distintos. Las centrales no reciben un ID local ni un vínculo de autorización nuevo.

`puedeRetirar` sólo es verdadero para una asignación local cuya **área otorgante** está dentro de las raíces de delegación autorizadas y cuyo contexto permite escritura. El endpoint vuelve a comprobar esas raíces; ver una fila no habilita DELETE. No cambia la política de alta ni el alcance del retiro, su confirmación, bloqueo, comprobación de otras concesiones compartidas, auditoría o transacciones. El prechequeo de raíces se realiza antes de abrir la conexión de retiro.

Un vínculo delegado efectivo exige las mismas coincidencias previas: email, empleado, adscripción de origen, concesión de origen `aplicacion`, rol exacto, otorgante, área asignada y jerarquía. Una revocación, rol ausente, persona suspendida/ausente de la publicación o adscripción incompatible impide mostrarlo como efectivo. Los retiros centrales pendientes conservan una fila **Revocada / Retiro pendiente** sólo para el otorgante que puede reintentarlos; no se presentan como acceso activo de las áreas que únicamente consultan.

## Integración Nexo y limitación del modelo

Se reutiliza `NexoOperations` y la conexión existente. La lectura une únicamente vistas publicadas `nexo_usuarios`, `nexo_usuario_rol` y `nexo_concesiones`, filtradas para TDV2 por la cuenta publicada y verificadas con `nexo_aplicacion`. Usa las claves exactas `colaborador_local` / `colaborador_dependencias` y parámetros para limitar candidatos a adscripciones de la rama o emails de vínculos locales candidatos. Después `PostgresNexoProfiles` revalida identidad, roles, módulo Procedimientos y concesiones. No se leen ni escriben tablas internas de Nexo y no hay sincronización/copia de identidades.

La publicación existente filtra acceso, suspensión, revocación y caducidad. Si falla la lectura, se devuelve el error sanitizado y no se sustituye con una lista local supuestamente efectiva. No hay caché de autorizaciones entre solicitudes. La implementación consulta los perfiles secuencialmente por candidato y conserva el timeout/cancelación existentes; el rendimiento con volúmenes institucionales queda pendiente.

**Alcance central:** el contrato vigente de TDV2 (2026-10-06) calcula local desde `FormUnit(ID_UR vigente)` y dependencias desde el nivel 2 de esa adscripción. `id_ur_acceso` es origen del otorgamiento histórico; no se interpreta como un área asignada independiente. En delegaciones, el área autorizada sí es `id_ur_alcance`, que no se reemplaza por la adscripción. Estas reglas se conservan, no se modifica la autorización para satisfacer una lista.

Nexo no publica en este contrato un área central de colaboración distinta de la adscripción vigente. Si se necesita expresar esa diferencia, requiere ampliar explícitamente el contrato institucional; no se inventó aquí. Cuando faltan empleado, adscripción resoluble, rol/concesión o módulo, se omite la asignación no acreditada. El aviso de la lista explica esa limitación sin exponer personas de una rama indeterminada ni asignarles alcance institucional.

## Pruebas de acceso

Contenedor centrado de hasta 1440 px, coherente con el ancho de Configuración; dos columnas iguales con separación de 24 px desde 900 px, una sola columna por debajo. Selección a la izquierda, resumen real a la derecha, con contenido alineado a la izquierda. Campos de rol/área aprovechan el ancho de su bloque y mantienen búsqueda/nombres completos; búsqueda de personas, motivo y opciones quedan en la selección, con la acción principal al final. El resumen muestra únicamente datos recibidos/seleccionados y diferencia la cuenta autenticada.

Se conserva la franja del layout para contexto activo y salida, las pestañas de modalidad, restricciones, estados carga/vacío/error, foco/teclado, duración y revalidación Nexo. No se añaden datos de ejemplo, indicadores decorativos o permisos.

## Verificado

- TypeScript y Vite correctos. ASP.NET y proyecto `TDV2.Verification` Release compilados sin avisos ni errores.
- **101/101 dominio/HTTP**: diez nuevos casos de política y cuatro de HTTP cubren las cuatro reglas, ramas distintas/nivel 2 anidado, jerarquía modificada, múltiples asignaciones, claves de rol frente a nombres, falta de datos centrales, revocación/suspensión simulada, permiso de retiro separado, consulta/representación de lectura, administrador combinado y rechazo de GET/POST/DELETE sin autorización. El GET real se ejecuta en ASP.NET TestServer con publicación/delegación y almacenamiento sustituidos en memoria.
- **87/87 frontend**: regresión de captura, guardado/versiones, reservas, envío, directorio, navegación y estados de sincronización.
- **4/4 `collaboration-actions-ui.mjs`**: lista de asignaciones recibidas sin Retirar ajeno, confirmación/cancelación con teclado, Guardar sin envío y recarga del texto, doble clic/ACK demorado, azul/borde/orden/altura, móvil, cambio remoto invalidando el diálogo y envío con versión reciente confirmado una sola vez.
- **4/4 `access-layout.mjs`**: búsqueda/selección/activación/salida/error con columnas iguales y contenedor centrado, nombres largos, escritorio 1440/1050 y móvil 390/320. Teclado y tacto emulados.
- **9/9 `interface-layout.mjs` y 10/10 `system-tools-ui.mjs`**: regresión de las mejoras previas, incluidas reservas, ayudas, condicionales, pendientes/foco, autoguardado y sólo consulta.

Todos los recorridos de navegador usan React/MUI real en Edge con HTTP, identidades, reservas y SignalR simulados, bloqueando orígenes externos. No acreditan autorización institucional ni transacciones PostgreSQL. Se corrigieron dos expectativas del nuevo recorrido: debía enfocar para confirmar la reserva antes de `fill`, y MUI utiliza clases separadas para variante/color; no se relajó el bloqueo del campo.

Evidencia local ignorada por Git: `.artifacts/collaboration-actions-20261010/verification.json` y capturas de lista/acciones/envío; `.artifacts/access-layout-20261010/verification.json` y capturas de ambas modalidades. Los reportes finales indican éxito; posibles capturas `failure.png` pertenecen a diagnósticos previos.

Desde la raíz, con las dependencias existentes:

```powershell
$env:PATH='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;'+$env:PATH
$env:DOTNET_CLI_HOME=Join-Path (Get-Location) '.artifacts/dotnet-home'
$env:NUGET_PACKAGES=Join-Path (Get-Location) '.artifacts/nuget'
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
Push-Location ClientApp
try {
    node tests/browser/collaboration-actions-ui.mjs
    node tests/browser/access-layout.mjs
    node tests/browser/interface-layout.mjs
    node tests/browser/system-tools-ui.mjs
} finally { Pop-Location }
```

## Pendiente y conservación

No se conectó ni modificó ninguna base. La consulta nueva contra las vistas Nexo, su latencia y la traducción/proveedor PostgreSQL requieren comprobación posterior en un entorno autorizado. No se ejecutaron las suites nativas con bases; sólo se actualizaron sus selectores Guardar y se comprobó su sintaxis. No se consultaron servicios institucionales ni credenciales. Tampoco se modificaron Nexo/Herd ni se hizo push o despliegue. Sin migración nueva ni cambios en la configuración de operación. Dispositivos físicos y aceptación institucional pendientes.

## Archivos de esta entrega

Rutas relativas al repositorio; los cambios de entregas previas se conservan.

- Backend: `tdv2/Domain/CollaborationRules.cs` y `CollaborationListing.cs` (nuevos), `tdv2/Domain/Access.cs`, `tdv2/Services/CollaboratorAccess.cs`, `CollaboratorService.cs`, `tdv2/Infrastructure/Contracts.cs`, `PostgresCollaborationStore.cs`, `tdv2/Integrations/Nexo/NexoOperations.cs`, `tdv2/Hosting/ServiceRegistration.cs`.
- Interfaz: `ClientApp/resources/js/pages/FormatoUR.tsx`, `Colaboradores.tsx`, `VistaPrueba.tsx`, `ActuarComoUsuario.tsx`, `PruebasAcceso.tsx`; `ClientApp/resources/js/Components/FormSubmissionReview.tsx`, `types/tdv2.ts`, `ClientApp/resources/css/app.css`.
- Pruebas nuevas: `tests/TDV2.Verification/CollaborationVisibilityTests.cs`, `CollaborationFixtures.cs`; `ClientApp/tests/browser/collaboration-actions-ui.mjs`.
- Pruebas ampliadas: `tests/TDV2.Verification/EntryPoint.cs`, `Fixtures.cs`; `ClientApp/tests/browser/access-layout.mjs`.
- Selectores de Guardar adaptados en `ClientApp/tests/browser`: `central-collaboration-flow.mjs`, `deletion-flow.mjs`, `delivery-collaboration-native.mjs`, `first-stage-flow.mjs`, `interface-layout.mjs`, `native-flow.mjs`, `participants-browser.mjs`, `performance-flow.mjs`, `photos-flow.mjs`, `procedure-validation-native.mjs`, `stage-review-native.mjs`, `system-tools-native.mjs`, `system-tools-ui.mjs`, `table-scroll-native.mjs`, `users-served-flow.mjs`.
- Documentación: `README.md`, `ESTADO_MIGRACION.md`, `docs/migracion/INVENTARIO.md`, `DELEGACION_REPRESENTACION.md`, `REVISAR_ENVIO_ETAPAS.md`, `COLABORADORES_VISIBILIDAD_ACCIONES.md` (nuevo).
