# Captura, presencia y fotografías — 2026-10-07

Esta entrega continúa el código local sin restaurar GitHub. **Las nuevas reglas de participación quedan pendientes.** Se conserva la configuración existente y el cálculo de alcances. [Huellas de las políticas conservadas](evidencia-captura-politicas-conservadas.json).

## Implementado

- `BlockEditor` mantiene los bloques modificados incrementalmente. Cada tecla compara sólo el bloque afectado; volver al valor original limpia su pendiente. No recorre todo el documento ni reprograma la caducidad de todas las reservas por tecla.
- Se conserva el montaje de una sola pestaña y el motor que mantiene datos/propuestas fuera de ella. La renovación de una reserva actualiza su fila y relaciones reales, sin invalidar todas las filas de Identificación general.
- Un estado remoto equivalente no vuelve a notificar a React. Se siguen procesando contenido, versiones, reservas, permisos y caducidad; no se compara únicamente la versión global. SignalR conserva sus comprobaciones cada 15 segundos y la limpieza al desconectar.
- Entrada automática al campo, reserva/versiones antes de escribir, guardado antes de liberar, Select en portal, eliminación directa por ID con reservas de relaciones, propuestas ante fallos e inmutabilidad del envío: conservados.
- Se reutilizan los colores coordinados por PostgreSQL e identidad efectiva, contorno de 2 px, fondo tenue y avatar de 26 px. El avatar incorpora foto opcional y Tooltip MUI con nombre completo para ratón, teclado y tacto. Consultarlo conserva selección/cursor y no adquiere ni libera reservas.

## Fotografías y representación

`ParticipantPhotosController`: `GET /formatos/{ur}/participantes/{identidad-opaca}/foto`. Comprueba sesión y alcance vigente mediante `RequestAccess`, reserva activa del participante en ese formato e identidad local Microsoft verificada. Vuelve a comprobar la reserva después de una descarga lenta. No publica un directorio ni permite buscar fotos por correo.

`ParticipantPhotos` reutiliza `GraphTokens` y `MicrosoftClient.Photo` (48×48). Usa el token propio del participante y contrasta `/me` con su correo y object ID verificados; nunca el token del observador. Un representado sin identidad/token Microsoft propios se muestra con iniciales. La cabecera también usa foto/nombre del objetivo al representar; las acciones e hints de la cuenta real se conservan.

Caché del servidor: renovación a los 15 minutos, límite 32 MiB y descarga concurrente agrupada por identidad. La corrección posterior de [persistencia de fotografías](PERSISTENCIA_FOTOGRAFIAS.md) distingue ausencia confirmada (dos minutos) de error temporal: conserva la última imagen como máximo una hora desde su verificación y aplica espera progresiva. **No cachea permisos.** El navegador comparte una petición por participante dentro del formato/contexto y la cabecera mantiene su almacén entre páginas; limpia al cambiar de identidad. Respuesta `private, no-store`, sin fotos en disco/localStorage ni tokens en el navegador. La carga no bloquea captura ni autoguardado.

No se añaden permisos Graph de directorio ni cambios OAuth/Entra: se reutiliza `User.Read`. Pendiente verificar fotografías con Microsoft institucional real. Las pruebas usan una miniatura PNG sintética de un píxel; no son fotos personales.

## Comparación medida

Edge 154/Windows, 1366×768, HTTPS/ASP.NET/PostgreSQL aislados. Mismo formato sintético: 20, 100 y 200 registros de Identificación general, demás tablas vacías; 58 entradas separadas por 25 ms. Instrumentación de render sólo en el bundle de prueba. Una pestaña montada en ambas fases: eso ya estaba implementado al iniciar.

| Registros | Filas ajenas renderizadas antes → después | Media entrada→frame, ms | p95 entrada→frame, ms | Comandos EF antes → después |
|---|---:|---:|---:|---:|
| 20 | 19 → 0 | 12,17 → 14,66 | 18,0 → 22,2 | 84 → 95 |
| 100 | 99 → 0 | 12,46 → 14,69 | 16,4 → 19,7 | 92 → 95 |
| 200 | 199 → 0 | 21,19 → 20,18 | 27,4 → 27,4 | 92 → 104 |

La eliminación de renderizados ajenos es comprobable; **la latencia a frame no mejora uniformemente** en estas corridas. No se extrapola un porcentaje de mejora a equipos/redes institucionales. El recorrido registra 8 → 9 solicitudes HTTP por formato: la adicional comprueba la fotografía. Durante las 58 entradas sólo hay una renovación de actividad y ningún PATCH por tecla; el PATCH llega tras la pausa. Los comandos EF incluyen apertura, presencia y guardado; excluyen SQL Npgsql manual/consultas publicadas de Nexo. Varían con SignalR y la autorización adicional de la foto.

Medición independiente del motor, sin React/red: 100 cambios de calentamiento y 900 medidos por tamaño.

| Registros | Media por cambio antes → después, ms | p95 antes → después, ms |
|---|---:|---:|
| 20 | 0,0155 → 0,0115 | 0,0213 → 0,0156 |
| 100 | 0,0167 → 0,0079 | 0,0173 → 0,0106 |
| 200 | 0,0229 → 0,0077 | 0,0393 → 0,0098 |

Inicio conserva la optimización previa: **102 áreas / 8 comandos EF**. ILDA por conjuntos conserva igualdad exacta de claves de origen, orden y límite independiente de 200 filas por UR. No se cambian intervalos ni cachés de permisos. [Datos y límites de medición](evidencia-captura-comparacion.json).

## Migración e instalación posterior

Nueva: **`20261007161549_ParticipantPhotographs`**. Añade `formato_bloques.usuario_participante_id` nullable, índice y FK a `users` con `SET NULL`. No inventa vínculos para reservas antiguas ni altera respuestas, catálogo, colaboraciones o instantáneas. No modifica migraciones anteriores. Down rechaza instalaciones con enviados, conservando el límite de reversión de las migraciones de captura previas.

Probada sólo en PostgreSQL desechable: instalación, repetición sin cambios, upgrade con enviado sintético intacto, FK inválida, script idempotente y rollback protegido. [34 comprobaciones EF](evidencia-captura-fotografias-migraciones.json). **No aplicada institucionalmente.** Desde la raíz:

```powershell
# Revisión sin conectar
dotnet ef migrations list --no-connect --project tdv2
dotnet ef migrations has-pending-model-changes --project tdv2
dotnet ef migrations script 20261006233010_ProcessParticipationAndPresence 20261007161549_ParticipantPhotographs --project tdv2 --output .artifacts/participant-photographs.sql

# Sólo después de revisar destino, historial y migraciones pendientes; un único operador
dotnet ef database update 20261007161549_ParticipantPhotographs --project tdv2 -- --environment Development
```

El último comando utiliza `ConnectionStrings:Tdv2` y aplica también migraciones anteriores pendientes: revisarlas antes. F5 no migra. Esta entrega no requiere cambios de roles/módulos ni nueva publicación Nexo; conserva las vías `central`/delegada y los pendientes institucionales anteriores.

## Reproducir pruebas

Resultado: TypeScript/Vite correctos, 64 casos frontend, 62 dominio/transporte, [144 HTTP/PostgreSQL](evidencia-captura-fotografias-postgresql.json), 34 EF, 31 escenarios de captura en Edge y 2 adicionales de cursor/teclado/tacto. Son grupos con cobertura superpuesta, no una suma de casos únicos. Se conservan propuestas, versiones, aislamiento por UR, revocaciones, reservas de relaciones y enviados inmutables.

Desde la raíz, con Node/npm, PostgreSQL 18 y Edge instalados. El preparador crea/detiene un clúster bajo `.artifacts`, valida loopback/directorio/identidad y no usa User Secrets ni bases existentes.

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
$env:TDV2_TEST_BROWSER_FLOW='formats'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='participants'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW='visual-studio'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

Medir de nuevo con instrumentación exclusiva de pruebas; restituir el bundle normal al terminar:

```powershell
node ClientApp/tests/browser/build-capture-profile.mjs
$env:TDV2_TEST_BROWSER_FLOW='capture-matrix'
$env:TDV2_CAPTURE_PHASE='after'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW,Env:TDV2_CAPTURE_PHASE
npm.cmd --prefix ClientApp run build
node ClientApp/tests/frontend/measure-capture.mjs ClientApp/resources/js/lib .artifacts/engine-current.json
```

La fase anterior utilizó copias de los archivos locales al iniciar en `.artifacts/capture-20261007/before`, sin restaurar GitHub. Las dos fases y sus límites se conservan en el JSON de comparación.

## Visual Studio y aceptación

Abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React** y F5. Se verificaron Vite 5173, espera de Kestrel en 5064, redirección a `https://localhost:7136`, portada, React, rechazo anónimo y HMR `wss://localhost:7136`, con certificado existente y cero consultas EF en esas páginas. [Evidencia](evidencia-captura-arranque-https.json). Se usó el host de pruebas con configuración equivalente y fuentes sintéticas; **no se manejó el depurador gráfico de Visual Studio ni Microsoft institucional**.

Para captura manual, usar una base aislada migrada y dos identidades sintéticas autorizadas; no enviar formatos institucionales para probar. Abrir dos perfiles y dos pestañas del mismo perfil. Entrar a un campo, esperar reserva, escribir/seleccionar texto, usar Tab/Select y salir a otro registro. La otra sesión consulta la fila ocupada, edita otra y recibe liberación/guardado sin recargar. Probar Eliminar sin enfocar campos y Cancelar. Deben coincidir color/miniatura entre observadores y distinguir otra pestaña. Una desconexión conserva la propuesta y exige resolverla explícitamente.

Capturas: [1920×1080](captura-avatar-1920.png), [1366×768](captura-avatar-1366.png), [768×1024](captura-avatar-768.png), [390×844](captura-avatar-390.png). Sin desbordamiento de página; la tabla mantiene desplazamiento horizontal interior y la tipografía original. Movimiento reducido sin transiciones en filas.

La inserción múltiple se prueba con `Input.insertText`, sin leer ni modificar el portapapeles del operador. La emulación táctil/teclado no sustituye aceptación con lectores de pantalla, teléfonos físicos, fotos reales y redes institucionales. [Evidencia de captura completa](evidencia-captura-fotografias-react.json).

La comprobación final usa pantalla táctil emulada y `tap` de Playwright, además de ratón/teclado. Corrigió un defecto que no detectaban los eventos sintéticos: impedir el foco del puntero suprimía los eventos compatibles esperados por el tooltip. La apertura controlada para touch/pen conserva foco y selección. [Dos escenarios finales aprobados](evidencia-captura-avatar-tactil.json).
