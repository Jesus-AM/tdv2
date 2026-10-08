# Persistencia privada de fotografías — 2026-10-07

## Implementado

`AccountPhotoProvider` vive sobre las páginas en `app.tsx`. La cabecera se suscribe al mismo almacén durante la navegación SPA: desmontar `AuthenticatedLayout` no borra la imagen ni genera otra descarga. La renovación mantiene la imagen mientras espera al servidor. Se reutiliza el almacén de miniaturas de participantes, sin modificar colores, nombres, reservas, autoguardado ni renderizados del editor.

La fotografía queda vinculada a la identidad Microsoft verificada (usuario local, tenant, object ID y correo vigente). Cada petición comprueba el acceso actual antes de consultar la caché. La cabecera utiliza además un identificador opaco de sesión/cuenta/contexto efectivo; el servidor rechaza una cabecera de contexto antiguo. Al terminar una descarga comprueba nuevamente sesión y revisión de representación. Las fotografías de participantes siguen exigiendo acceso al formato y una reserva vigente del participante, antes y después de cargar.

Cerrar sesión, usar otra cuenta, entrar/salir de representación o de vista de prueba invalida inmediatamente la imagen visible. Las respuestas pendientes se cancelan/ignoran. Una notificación efímera `BroadcastChannel` limpia las otras pestañas del mismo origen; no transmite identidades, imágenes ni credenciales. En representación se utiliza exclusivamente el usuario objetivo con vínculo Microsoft verificado, o sus iniciales. La vista de prueba muestra iniciales.

El transporte reconoce `/connect` también con parámetros: «Usar otra cuenta» navega a `/connect?account=other` como documento OAuth completo, en lugar de solicitarlo como página JSON. No cambia los hints, CSRF, cookies OAuth ni reglas de representación.

## Vigencia y fallos

| Situación | Comportamiento |
|---|---|
| Imagen verificada | Renovación a los **15 minutos**; no se descarga de nuevo al navegar. |
| Renovación en curso | Se mantiene la última imagen válida de esa identidad. |
| Red, timeout, HTTP 408/429/5xx de Microsoft | Se conserva sólo hasta **una hora desde la última verificación correcta**, sin extender ese límite por reintentos. Espera progresiva de **30, 60, 120… hasta 900 segundos**; `Retry-After` se respeta dentro de ese máximo. |
| Vence el límite aunque la red siga caída | Se retira la imagen y aparecen iniciales. |
| Microsoft confirma 404 y `/me` coincide | Ausencia confirmada: se retira la imagen inmediatamente y se vuelve a comprobar en dos minutos. |
| 401 de Graph | Conserva la renovación de token existente: un canje serializado y un reintento; si persiste, se retira la imagen. |
| Identidad distinta, respuesta inválida, autorización Microsoft rechazada o token indescifrable | Iniciales, causa interna sanitizada y reintento en dos minutos. No se encubre con una foto anterior. |
| 401/403/409 o error 5xx de TDV2 | Se retira la imagen; no se trata como una indisponibilidad temporal de Microsoft. Un fallo de transporte sin respuesta o 408/429 admite la conservación acotada. |

El servidor conserva miniaturas sólo en memoria privada del proceso, con límite de **32 MiB** y peticiones coordinadas por identidad. El navegador conserva únicamente memoria efímera; ni imágenes ni tokens se guardan en cookies, `localStorage`, `sessionStorage` o historial. Las respuestas siguen siendo `private, no-store`. Recargar la página vuelve a autorizar la petición y recupera la miniatura de esa caché del servidor, si existe; reiniciar/evictar el proceso puede requerir otra descarga. No se cachean permisos de Nexo entre solicitudes.

La renovación completa tiene un límite de 30 segundos, incluida lectura del cuerpo; mantiene el timeout HTTP de 15 segundos existente. Una desconexión del solicitante propaga su cancelación sin sustituir la entrada válida. Las causas sanitizadas distinguen red, timeout, límite Microsoft, indisponibilidad, identidad no coincidente y protección de tokens.

## Cifrado Windows/Ubuntu

El código vigente usa Data Protection (`TDV2.AspNet.v1`), llaves en `.runtime/keys` o `DataProtection:KeyDirectory` y protección DPAPI en Windows. Los tokens Graph están cifrados en `ms_graph_tokens`, por usuario. Compartir PostgreSQL **no comparte un anillo de llaves compatible**. Dos entornos con anillos distintos no pueden descifrar los tokens escritos por el otro; una llave envuelta con DPAPI de Windows tampoco es portable a Ubuntu.

Ese conflicto está demostrado con dos proveedores criptográficos sintéticos: no es una inspección de los servidores institucionales. `GraphTokens` registra `token_protection` sin imprimir tokens, contenido HTTP ni la excepción criptográfica original. La renovación retira la foto ante ese fallo y conserva intacta la fila de tokens. Hasta la próxima renovación una entrada todavía fresca puede seguir sirviéndose tras autorizar el acceso; la caché no prueba que ambos entornos puedan descifrar tokens.

Queda al operador comprobar si ambos entornos usan la misma base y qué entorno escribió la fila afectada. No se compartieron llaves ni se modificaron credenciales. Un nuevo inicio puede reponer tokens cifrados para el entorno local, pero **no resuelve** el uso simultáneo de esa fila por dos entornos incompatibles. Cualquier separación de almacenamiento por entorno requeriría otra decisión y entrega; no se introduce aquí.

## Instalación y verificación

Evidencia: compilación Release sin errores/advertencias, TypeScript/Vite correctos, 71 pruebas frontend y 62 de dominio/transporte. [152 casos de regresión PostgreSQL](fotografias-persistencia-20261007/postgresql.json), [15 casos de fotos/tokens sobre el código final](fotografias-persistencia-20261007/fotografias.json) y [9 escenarios específicos de Edge](fotografias-persistencia-20261007/navegador.json). Los grupos se solapan; no deben sumarse como casos únicos. En el informe enfocado, Edge figura como un caso adicional (16/16).

[Regresión del formato en Edge: 32/32](fotografias-persistencia-20261007/regresion-formato.json), sin errores JavaScript: avatares, nombres y colores, foco y tooltip, dos sesiones/pestañas, reservas automáticas, autoguardado, eliminación directa, reconexión con propuesta, SignalR, envío y revocación permanecen funcionales.

Esta corrección **no añade migraciones** ni cambios de esquema. Conserva las migraciones anteriores y sus pendientes, incluida `StructuredUsersServed`. No ejecutar migraciones institucionales para probar fotografías.

Desde la raíz, con .NET y Node/npm disponibles:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
$env:TDV2_TEST_CASE_PREFIX = 'Foto/|Graph:'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_BROWSER_FLOW = 'photos'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El runner crea una base PostgreSQL nueva en loopback, configura únicamente dobles sintéticos de Microsoft/Nexo y detiene el clúster al terminar. El navegador bloquea destinos externos. Las miniaturas son PNG sintéticos, no fotografías personales. El reloj acelerado del escenario de navegador permite comprobar renovaciones sin esperar una hora; los límites completos también se verifican en backend y pruebas del almacén frontend.

En Visual Studio abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React** y F5 sobre un entorno autorizado. Navegar Inicio → Configuración → Sincronizaciones → Pruebas de acceso: la imagen debe conservarse sin una nueva petición `/user/photo` por página. Recargar: habrá una nueva petición autorizada a TDV2, pero una caché fresca evita Graph. En dos sesiones/pestañas, comprobar cierre/cambio de cuenta y representación: la foto anterior desaparece antes de la respuesta; el objetivo sin vínculo verificado muestra iniciales. El endpoint no revela si falló el descifrado: revisar sólo los códigos sanitizados del servidor.

Pendiente: aceptación con Microsoft real (foto cambiada/eliminada, limitación 429, permisos y refresh reales), topología real Windows/Ubuntu, dispositivos físicos y tecnologías de asistencia. No se probaron ni modificaron bases institucionales, Nexo, Herd, secretos o llaves; sin push ni despliegue.
