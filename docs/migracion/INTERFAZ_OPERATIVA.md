# Distribución de la interfaz operativa — 2026-10-10

Este documento registra el ajuste inicial. El [complemento posterior de avance y Pruebas de acceso](AVANCE_Y_PRUEBAS_ACCESO.md) añade una evaluación compartida en backend, cambia la presentación a un solo botón SQL Server con ILDA aparte y actualiza la redacción de revisión. Sus resultados y archivos complementan los siguientes; las referencias aquí a tres botones o a ausencia de cambios de backend corresponden exclusivamente al ajuste inicial.

## Implementado

- **Inicio / Procedimientos:** conserva tres indicadores y listado jerárquico a todo el ancho. Colaboradores y Actualizar alineados; pestañas en franja propia y filtros/acciones en una barra adaptable. Sin encabezado redundante. Filas compactas con permiso, porcentaje, barra y Ver formato en columnas constantes; avance del conjunto separado del individual. Las preferencias de búsqueda, filtro, alcance, página, grupos y desplazamiento viven en memoria por identidad/contexto, sin almacenar permisos o respuestas. Actualizar conserva la vista; regresar desde un formato la restaura. Cambiar identidad limpia la memoria. Solo consulta se determina por los permisos recibidos, también para administradores.
- **Sistemas:** Procedimiento tiene ancho de referencia 280–360 px dentro de la tabla fija; las otras columnas conservan espacio y desplazamiento horizontal. Campo cerrado con elipsis, búsqueda por código/nombre, opciones completas con salto de línea y menú limitado a la pantalla. Tooltip y ayuda compartida muestran el nombre completo con mouse, teclado o tacto. Vaciar la búsqueda conserva la relación; seleccionar una opción o usar Quitar selección cambia el código mediante el motor existente. Abrir desde foco espera la reserva confirmada: evita que el foco posterior a mousedown invalide la apertura inicial. Sin cambios en la elegibilidad del servidor, varias herramientas por procedimiento, controles condicionales, Edición fija, eliminación ni reservas. Medios sigue debajo de la tabla, con reserva propia y opciones distribuidas uniformemente.
- **Revisar y enviar:** resumen por sección con cantidades y estados de la proyección existente del backend. Pendientes con sección/registro/campo y botón Ir al campo descrito accesiblemente por su mensaje. Navega y enfoca el control; el desplazamiento horizontal respeta Edición. Autoguardado en posición estable y acción Enviar formato al final, separada del resumen. No muestra el mensaje de etapa lista. Conserva comprobación de versión, reglas de primera etapa, permisos, confirmación, recibos e inmutabilidad; sólo anuncia el envío después de la respuesta del servidor.
- **Sincronizaciones:** paneles homogéneos para UR, módulos SII e ILDA con fecha del último éxito, estado con texto/icono, copia local, resultado disponible y acción Sincronizar/Reintentar. Los fallos no sustituyen el último éxito. SII sigue sincronizando UR y módulos juntos y lo explica junto a ambos botones; ILDA es independiente. Conserva inicio/ejecución confirmados y protección de duplicados. El historial devuelve hasta 50 ejecuciones: cuando falta un resultado o contador se informa su ausencia, sin deducir éxito o inventar cifras. No cambia la operación Windows/Ubuntu.

No cambian la portada, barra blanca, fuentes, autenticación, delegación, modelo/persistencia, reglas de negocio, procesadores ni servicios. No hay dependencias nuevas ni migración por esta entrega. Las migraciones pendientes anteriores mantienen su estado; no se aplicó ninguna.

## Verificado

| Comprobación | Resultado | Alcance real |
|---|---|---|
| TypeScript, Vite, compilación ASP.NET | Correctos | Sin conexión a bases; ASP.NET sin avisos/errores |
| `test:forms` | 85/85 | Motor frontend, directorio, reservas, fotos y cinco casos nuevos de presentación de resultados por catálogo |
| `TDV2.Verification` | 76/76 | Dominio/HTTP con dobles en memoria; no proveedor PostgreSQL |
| `interface-layout.mjs` | 7/7 | Bundle React/MUI en Edge; transporte simulado para los cuatro módulos |
| `system-tools-ui.mjs` | 10/10 | Regresión: catálogo ausente/error/búsqueda, Otra/Otro, estados, ayudas, dos sesiones, varias herramientas, orígenes, móvil, históricos/enviados; transporte simulado |
| `operational-layout.mjs` | 3/3 grupos | Indicadores y directorio, filtros/paginación, permisos recibidos, actualización, teclado y adaptación; servidor de prueba |

Los recorridos nuevos comprueban restaurar búsqueda/filtro/expansión/scroll al regresar; administrador con formato de consulta; nombre largo sin ampliar la tabla y sin perder la búsqueda al recibir presencia; ayuda sin liberar; campos condicionales y Medios independiente; enlace al pendiente con foco visible en móvil; fallo de guardado conservando texto/foco, ACK demorado con Guardando… y envío deshabilitado, actualización de pendientes tras confirmación; reintento SII por doble clic sin duplicar, éxito UR/fallo módulos con última fecha correcta preservada; envío anunciado sólo después del ACK. Móviles 390/320 px, tableta 768 px y escritorios hasta 1920 px, sin desbordamiento horizontal de página. Teclado y tacto emulados.

Evidencia local ignorada por Git:

- `.artifacts/interface-layout-20261010/verification.json` y capturas de los cuatro módulos, revisión incompleta/completa/enviada, destino del pendiente en móvil y sincronización parcial/en curso.
- `.artifacts/system-tools-ui/report.json` y capturas `systems-*.png`.
- `.artifacts/operational-layout/after/verification.json` y capturas de Inicio en cinco anchos.

Los posibles archivos `failure.png` o `selection-failure.png` de esas carpetas corresponden a diagnósticos previos; los reportes finales y las capturas nombradas por módulo son la evidencia vigente.

## Reproducir sin bases

Desde la raíz del repositorio, con las dependencias existentes instaladas:

```powershell
$env:PATH='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;'+$env:PATH
$env:DOTNET_CLI_HOME=Join-Path (Get-Location) '.artifacts/dotnet-home'
$env:NUGET_PACKAGES=Join-Path (Get-Location) '.artifacts/nuget'
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet build tdv2 --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
node ClientApp/tests/browser/operational-layout.mjs
Push-Location ClientApp
try {
    node tests/browser/interface-layout.mjs
    node tests/browser/system-tools-ui.mjs
} finally { Pop-Location }
```

Los servidores de prueba sólo sirven el bundle local y los fixtures en memoria. Los contextos de navegador bloquean recursos de otros orígenes. No arrancar el host web con conexiones institucionales para estas pruebas.

## Pendiente y límites

Esta entrega no ejecutó suites con PostgreSQL ni sincronizaciones reales, conforme a la instrucción de no modificar bases. Los recorridos nuevos no acreditan autenticación, autorización o reservas reales del backend: usan transporte simulado, incluido SignalR. La prueba de dos sesiones no sustituye una prueba de dos identidades reales. Las suites nativas anteriores conservan sus casos, con selectores adaptados a Enviar formato, Ir al campo y las acciones/estados de catálogo; no se declaran ejecutadas aquí. Quedan pendientes dispositivos físicos y aceptación institucional. No se modificaron credenciales, Nexo o Herd; sin push ni despliegue.

## Archivos de esta entrega

- [ClientApp/resources/css/app.css](../../ClientApp/resources/css/app.css)
- [ClientApp/resources/js/Components/AreaDirectory.tsx](../../ClientApp/resources/js/Components/AreaDirectory.tsx)
- [ClientApp/resources/js/Components/FormSubmissionReview.tsx](../../ClientApp/resources/js/Components/FormSubmissionReview.tsx)
- [ClientApp/resources/js/Components/ProcedureSelect.tsx](../../ClientApp/resources/js/Components/ProcedureSelect.tsx)
- [ClientApp/resources/js/Components/SyncState.tsx](../../ClientApp/resources/js/Components/SyncState.tsx)
- [ClientApp/resources/js/Components/SystemField.tsx](../../ClientApp/resources/js/Components/SystemField.tsx)
- [ClientApp/resources/js/lib/sync-presentation.ts](../../ClientApp/resources/js/lib/sync-presentation.ts)
- [ClientApp/resources/js/pages/FormatoUR.tsx](../../ClientApp/resources/js/pages/FormatoUR.tsx)
- [ClientApp/resources/js/pages/Inicio.tsx](../../ClientApp/resources/js/pages/Inicio.tsx)
- [ClientApp/resources/js/pages/Sincronizaciones.tsx](../../ClientApp/resources/js/pages/Sincronizaciones.tsx)
- [ClientApp/tests/browser/central-collaboration-flow.mjs](../../ClientApp/tests/browser/central-collaboration-flow.mjs)
- [ClientApp/tests/browser/delivery-collaboration-native.mjs](../../ClientApp/tests/browser/delivery-collaboration-native.mjs)
- [ClientApp/tests/browser/first-stage-flow.mjs](../../ClientApp/tests/browser/first-stage-flow.mjs)
- [ClientApp/tests/browser/interface-layout.mjs](../../ClientApp/tests/browser/interface-layout.mjs)
- [ClientApp/tests/browser/native-flow.mjs](../../ClientApp/tests/browser/native-flow.mjs)
- [ClientApp/tests/browser/operational-layout.mjs](../../ClientApp/tests/browser/operational-layout.mjs)
- [ClientApp/tests/browser/stage-review-native.mjs](../../ClientApp/tests/browser/stage-review-native.mjs)
- [ClientApp/tests/browser/sync-flow.mjs](../../ClientApp/tests/browser/sync-flow.mjs)
- [ClientApp/tests/browser/sync-manual-native.mjs](../../ClientApp/tests/browser/sync-manual-native.mjs)
- [ClientApp/tests/browser/system-tools-ui.mjs](../../ClientApp/tests/browser/system-tools-ui.mjs)
- [ClientApp/tests/browser/system-ui-fixture.mjs](../../ClientApp/tests/browser/system-ui-fixture.mjs)
- [ClientApp/tests/frontend/sync-presentation.test.mjs](../../ClientApp/tests/frontend/sync-presentation.test.mjs)
- [docs/migracion/INTERFAZ_OPERATIVA.md](../../docs/migracion/INTERFAZ_OPERATIVA.md)
- [docs/migracion/INVENTARIO.md](../../docs/migracion/INVENTARIO.md)
- [docs/migracion/REVISAR_ENVIO_ETAPAS.md](../../docs/migracion/REVISAR_ENVIO_ETAPAS.md)
- [docs/migracion/SINCRONIZACIONES.md](../../docs/migracion/SINCRONIZACIONES.md)
- [docs/migracion/SISTEMAS_HERRAMIENTAS.md](../../docs/migracion/SISTEMAS_HERRAMIENTAS.md)
- [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md)
- [README.md](../../README.md)
