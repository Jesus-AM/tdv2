# Captura colaborativa y envío

Actualización vigente: pestaña [Revisar y enviar y envíos por etapa](REVISAR_ENVIO_ETAPAS.md), con migración `20261009171656_StageSubmissions`. Se conserva [SignalR y la exclusión por registro](RESERVAS_SIGNALR.md).

## Captura

Cada fila de Identificación, Sistemas, Datos y Acuerdos es un bloque con identificador estable. Medios agrupa sus opciones y otro medio; cada pregunta y criterio de evaluación tiene su propio bloque. Otras personas consultan los bloques reservados y ven el nombre del titular. Los datos de sesión ya no se capturan; el encabezado histórico permanece en el JSON sin generar respuestas ni requisitos nuevos.

La reserva dura **45 segundos**, distingue sesión, revisión de representación y pestaña, y se renueva únicamente por cambios en respuestas. Una pantalla abierta o SignalR no la renuevan. Salir del bloque guarda y libera después de confirmar; cierres inesperados se resuelven por vencimiento. Reabrir/recargar el editor crea otra identidad de pestaña: no se copia mediante sessionStorage al duplicar pestañas.

El autoguardado espera un segundo de pausa. **Guardar borrador** guarda inmediatamente; el estado compacto sólo confirma éxito después del servidor y espera la liberación al terminar manualmente. Ante un fallo real se conserva lo escrito en memoria. La recuperación es interna y limitada; el mismo UUID confirma respuestas perdidas. La validación se corrige en el campo. Si la respuesta compartida cambió de forma incompatible, el texto queda accesible sin guardarse, sin diálogos para elegir versiones. Se conserva la descarga de emergencia. La ocupación normal se muestra dentro de la fila, sin panel global de recuperación. Ningún cambio se acepta antes de confirmar la reserva y cargar su contenido vigente. No se guardan respuestas en localStorage.

Contexto conserva las instrucciones y la identificación de la UR permanece en el encabezado de página. Se retiraron Datos de la sesión y la función Imprimir; permanecen descarga de propuestas, autoguardado y Guardar borrador. Sólo se monta la pestaña activa; el motor conserva el contenido y los pendientes al cambiar de sección. Formatos nuevos abren Contexto; cada operación confirmada recuerda su sección por actor real, usuario efectivo y formato. Claves numéricas se formatean sólo al mostrar/buscar; los identificadores de origen permanecen intactos.

Eliminar usa un diálogo MUI con Cancelar enfocado inicialmente. Identifica el registro por ID y, si cambió mientras se confirmaba, exige revisar de nuevo. El icono disponible es rojo; el tooltip explica bloqueos por consulta, envío, ILDA o reservas. Eliminar un proceso retira su evaluación y desvincula sistemas/datos dentro de una sola operación que necesita todas las reservas implicadas; tras confirmarse, libera también esas relaciones. Si falla, la propuesta sigue disponible para recuperar.

El servidor asigna códigos bajo el bloqueo de UR e inicializa criterios vacíos en el mismo commit. El borrador valida las filas modificadas y los vínculos afectados por retirar un proceso; el envío valida el documento completo. [Regresión y detalles](EDICION_AUTOMATICA.md).

## Autoridad del servidor

- `FormAccess` concentra alcance, consulta institucional y envío. Cada petición revalida identidad, Nexo y contexto efectivo; la vista por rol/área sigue siendo de consulta.
- `FormEditingService` bloquea brevemente sesión → UR. Adquirir, guardar y enviar siguen ese orden. Comprueba catálogo, alcance, ejercicio, estado, titular, vencimiento PostgreSQL y versión del bloque, incluyendo vencimiento antes del commit.
- `FormBlocks` aplica sólo los bloques reservados sobre el JSON vigente. Cambios estructurales que afectan referencias deben reservar todos los bloques afectados. No hay transacciones abiertas mientras alguien escribe.
- `formato_bloques` conserva versiones incluso al retirar filas y genera un testigo nuevo al reasignar. `formato_operaciones` registra UUID, huella y respuesta por sesión/pestaña/contexto. Repetir exactamente la operación confirma una sola escritura; cambiar su contenido reutilizando el ID se rechaza.
- `FormHub.Watch` sólo consulta: antes de **cada entrega** crea un scope, valida ticket/revisión y vuelve a consultar Nexo. Reconectar recupera el estado completo. Las señales internas no contienen respuestas; una comprobación cada 15 segundos detecta revocaciones y cambios de otras instancias. Ninguna entrega renueva reservas.
- El PUT completo anterior devuelve **428**; no existe una vía HTTP que evite reservas. Desplegar backend y assets juntos y recargar pestañas antiguas.

## Envío definitivo

La pestaña Revisar y enviar está después de Sistemas y herramientas y se puede abrir con pendientes. Enviar primera etapa requiere permiso vigente, cambios confirmados, revisión completa y ausencia de reservas ajenas. Antes del diálogo se guarda y consulta el estado; el servidor vuelve a comprobar UR, etapa, permisos, versión, requisitos y reservas. Colaboradores y consulta institucional no conceden envío. El administrador conserva su límite de responsabilidad institucional y rama editable.

Cada envío nuevo conserva fecha UTC, ejercicio, UUID, versión, actor Microsoft, identidad efectiva, nombre y auditoría. Su instantánea guarda el área y las respuestas de esa etapa. Triggers y servicio protegen esas respuestas, incluida Identificación con los procedimientos ILDA confirmados. Los envíos globales anteriores conservan todos sus datos y bloqueo, sin asignación inferida de etapa. **No existe reapertura**.

La revisión vigente sólo comprende Contexto, Identificación y Sistemas con Medios. Usa `FormStages` y las reglas existentes de `FormReview` y `FormCapture`; los pendientes D/N se conservan. Datos, evaluaciones, preguntas y acuerdos posteriores no impiden enviar ni se sustituyen en guardados parciales. Un 100 % no sustituye la comprobación final del servidor.

El avance utiliza únicamente los campos vigentes de la etapa activa. Consultar recalcula la proyección sin escribir respuestas; el siguiente guardado o envío persiste el valor vigente. La pestaña de revisión no suma avance. Los enviados históricos conservan su porcentaje. Si un ID de UR reaparece con otro ejercicio se mantiene el bloqueo histórico. La segunda etapa sigue sin habilitarse; [persistencia y aplicación posterior de la migración](REVISAR_ENVIO_ETAPAS.md).

## Prioridad y valores anteriores

Select de MUI de selección única, con texto indicativo «Selecciona una prioridad» únicamente en el campo vacío, fuera del menú: 5 extremadamente prioritario (rojo), 4 muy prioritario (naranja), 3 moderadamente prioritario (ámbar), 2 poco prioritario (azul), 1 nada prioritario (verde). Sin selección predeterminada; se admite la misma prioridad en varios procesos. El menú pertenece al bloque de la fila aunque MUI lo renderice en un portal; abrirlo no finaliza la reserva.

El cambio a Select **no convierte ni elimina valores previos**. Los números 1–5 conservan su significado vigente. Los valores del orden numérico histórico mayores de 5 se muestran como valor anterior, se conservan en borrador y deben reclasificarse explícitamente para enviar. No hay conversión automática fiable de un orden 1–9999 a intensidad 1–5.

## Comprobar los ajustes en Visual Studio

1. Abre `tdv2.slnx`, selecciona **TDV2 HTTPS + React** e inicia F5. Se conservan el perfil, HMR, User Secrets y `https://localhost:7136/connect`. F5 no aplica migraciones. Este cambio no requiere una migración adicional; la tercera migración de la entrega previa sigue siendo requisito de la captura colaborativa.
2. En una sesión autorizada, comprueba Contexto sin Datos de la sesión y la UR en el encabezado. Verifica la ausencia de Imprimir y la presencia de Guardar borrador/estado. No uses un formato institucional para ensayar eliminaciones o envío.
3. Para las escrituras sintéticas, ejecuta desde la terminal de la solución `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly`. Crea su propia base desechable y sesiones; no utiliza la conexión institucional de User Secrets. Prueba las cuatro tablas, Cancelar, ID estable ante otra pestaña, prioridad con teclado, móvil y movimiento reducido, revisión de pendientes y envío bloqueado después de confirmar.
4. Para concurrencia y casos negativos, ejecuta `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser`. Incluye reservas ajenas/vencidas, reintentos, envío concurrente, roles sin permiso e instantáneas inmutables. Sus datos y auditoría son sintéticos.
5. En Configuración, comprueba el desplegable de Sincronizaciones y Pruebas de acceso únicamente cuando Nexo publique esos módulos y su padre. Cuenta, Usar otra cuenta, Cerrar sesión y Actuar como usuario conservan su flujo independiente. No hay nuevas claves ni cambios de OAuth/representación.

Las pruebas de navegador usan Microsoft y Nexo sintéticos con ASP.NET, React y PostgreSQL reales. No equivalen a F5 interactivo autenticado ni a aceptación institucional. Las fuentes externas están bloqueadas en ese navegador; la tipografía configurada en el producto permanece intacta.

Las suites necesitan `node` en PATH. En esta estación se usó el Node ya incluido en Visual Studio; si la terminal no lo encuentra, agregarlo sólo a esa sesión antes de ejecutar los comandos:

```powershell
$env:PATH = 'C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Microsoft/VisualStudio/NodeJs;' + $env:PATH
```

## Actualizar desde la raíz

```powershell
dotnet restore tdv2.slnx
dotnet tool restore
npm.cmd --prefix ClientApp ci --ignore-scripts
dotnet ef migrations has-pending-model-changes --project tdv2
dotnet ef migrations script 20261002051340_InitializePausedSynchronization 20261005135417_CollaborativeFormsAndSubmission --project tdv2 --output .artifacts/colaboracion-envio.sql
# Operador único; comprobar ConnectionStrings:Tdv2 antes de ejecutar.
dotnet ef database update --project tdv2 -- --environment Development
dotnet ef database update --project tdv2 -- --environment Development
dotnet ef migrations list --project tdv2 -- --environment Development
```

La tercera migración añade seis columnas y tres tablas locales, completa el ejercicio desde su UR y preserva el contenido y relaciones anteriores. Su Down rechaza retirar la protección si hay envíos. No modifica Nexo/SII/ILDA ni programación de sincronizaciones. **No se aplica con F5** ni se ha aplicado a la base institucional como prueba.

En producción usa su configuración propia, no User Secrets. Aplica la migración con un solo operador, sin actualizaciones concurrentes, y reinicia después. Habilita WebSocket del proxy para `/form-events`; conserva HTTPS y host público. No activar sincronización automática.

```powershell
dotnet run --project tests/TDV2.Verification -p:UseAppHost=false
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
# Diagnóstico puntual, también aislado:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser
dotnet publish tdv2/tdv2.csproj -c Release -o .artifacts/publish-tdv2
```

Las suites crean PostgreSQL desechable y usan identidades, concesiones y envíos sintéticos. Microsoft se simula con ID tokens firmados; no se inicia sesión institucional ni se otorgan accesos a personas reales. Resultados y límites: [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). Configuración externa: [Nexo y Entra](CONFIGURACION_NEXO_ENTRA.md).
