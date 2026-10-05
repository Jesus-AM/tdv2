# Captura colaborativa y envío

## Captura

Cada fila de Identificación, Sistemas, Datos y Acuerdos es un bloque con identificador estable. Contexto agrupa fecha/responsable; Medios agrupa sus opciones y otro medio; cada pregunta y criterio de evaluación tiene su propio bloque. Otras personas consultan los bloques reservados y ven el nombre del titular.

La reserva dura **45 segundos**, distingue sesión, revisión de representación y pestaña, y se renueva únicamente por cambios en respuestas. Una pantalla abierta o SignalR no la renuevan. Salir del bloque guarda y libera después de confirmar; cierres inesperados se resuelven por vencimiento. Reabrir/recargar el editor crea otra identidad de pestaña: no se copia mediante sessionStorage al duplicar pestañas.

El autoguardado espera un segundo de pausa. **Guardar borrador** guarda inmediatamente; el estado compacto sólo confirma éxito después del servidor. Ante conflicto/desconexión se conserva la propuesta en memoria, con descarga y recuperación explícita. Comparar y recuperar no guarda automáticamente: después de revisar, Guardar borrador obtiene una reserva nueva. No se guardan respuestas en localStorage.

Contexto conserva los campos del anterior Encabezado y los incluye en impresión. Las secciones permanecen montadas. Formatos nuevos abren Contexto; cada operación confirmada recuerda su sección por actor real, usuario efectivo y formato. Claves numéricas se formatean sólo al mostrar/buscar; los identificadores de origen permanecen intactos.

## Autoridad del servidor

- `FormAccess` concentra alcance, consulta institucional y envío. Cada petición revalida identidad, Nexo y contexto efectivo; la vista por rol/área sigue siendo de consulta.
- `FormEditingService` bloquea brevemente sesión → UR. Adquirir, guardar y enviar siguen ese orden. Comprueba catálogo, alcance, ejercicio, estado, titular, vencimiento PostgreSQL y versión del bloque, incluyendo vencimiento antes del commit.
- `FormBlocks` aplica sólo los bloques reservados sobre el JSON vigente. Cambios estructurales que afectan referencias deben reservar todos los bloques afectados. No hay transacciones abiertas mientras alguien escribe.
- `formato_bloques` conserva versiones incluso al retirar filas y genera un testigo nuevo al reasignar. `formato_operaciones` registra UUID, huella y respuesta por sesión/pestaña/contexto. Repetir exactamente la operación confirma una sola escritura; cambiar su contenido reutilizando el ID se rechaza.
- `FormHub.Watch` sólo consulta: antes de **cada entrega** crea un scope, valida ticket/revisión y vuelve a consultar Nexo. Reconectar recupera el estado completo. Las señales internas no contienen respuestas; una comprobación cada 15 segundos detecta revocaciones y cambios de otras instancias. Ninguna entrega renueva reservas.
- El PUT completo anterior devuelve **428**; no existe una vía HTTP que evite reservas. Desplegar backend y assets juntos y recargar pestañas antiguas.

## Envío definitivo

Enviar guarda los cambios del usuario y solicita confirmación. El backend exige responsabilidad institucional, alcance editable, versión vigente, llenado completo y ausencia de reservas de otras sesiones/pestañas. Colaboradores o consulta institucional no conceden envío. Un administrador necesita ser responsable institucional además de estar en su rama editable.

Se guardan fecha UTC, ejercicio, UUID, versión, actor Microsoft real, identidad efectiva y auditoría. La instantánea conserva unidad y respuestas, incluidos los trámites ILDA incorporados. Consulta e impresión dejan de combinar nuevos inventarios o descripciones. Un trigger impide UPDATE/DELETE del formato enviado incluso desde otros procesos. **No existe reapertura**.

Requisitos visibles: fecha/responsable de Contexto; procesos con código, validación, prioridad y los campos de avance; sistemas; datos; doce preguntas; acuerdos; nueve criterios por proceso. El cálculo de avance es del servidor. Si un ID de UR reaparece con otro ejercicio, se bloquea la sobrescritura del histórico y debe resolverse su correspondencia institucional.

## Prioridad y valores anteriores

Selección única accesible: 5 extremadamente prioritario (rojo), 4 muy prioritario (naranja), 3 moderadamente prioritario (ámbar), 2 poco prioritario (azul), 1 nada prioritario (verde). Sin selección predeterminada; se admite la misma prioridad en varios procesos.

La migración **no convierte ni elimina valores previos**. Los números 1–5 se conservan; la escala anterior era un orden numérico y su significado requiere revisión humana. Los mayores de 5 se muestran como valor anterior, se conservan en borrador y deben reclasificarse explícitamente para enviar. No hay conversión automática fiable de un orden 1–9999 a intensidad 1–5.

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
