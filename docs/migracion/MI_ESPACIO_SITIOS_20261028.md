# Mi espacio y selector extensible de sitios — 2026-10-28

**Identidad actualizada 2026-10-29:** se conserva esta selección de sitios, pero el actor se resuelve primero mediante el vínculo Microsoft/canónico o UPN/SMTP verificado único. No se mezclan permisos de distintas cuentas encontradas por alias. [Requisitos, nueva migración propia y evidencia](IDENTIDAD_MICROSOFT_ALIAS_20261029.md).

Esta revisión se aplica sobre el trabajo pendiente de acceso independiente y autoguardado fluido. No restaura archivos ni modifica encuesta, instrumento, informe, auditoría manual, permisos Nexo, conexiones, paquetes o migraciones.

## Comportamiento vigente

`Ir a mi espacio` conserva su enlace público a `/accesos`. Las entradas genéricas `/connect` y las que no aportan un destino válido conservan esa intención durante OAuth mediante la cookie de retorno protegida y vinculada al state. Después de autenticar se comprueba el registro de sitios en el backend:

| Resultado de las comprobaciones | Respuesta |
| --- | --- |
| Ningún sitio autorizado, sin fallas | Vista con **Elige un sitio**, **No hay sitios disponibles para tu cuenta.**, Volver al portal y Cambiar de cuenta. La identidad permanece autenticada; no se vuelve a iniciar sesión automáticamente. |
| Un sitio autorizado, sin fallas | Entrada directa a su destino; no se renderiza el selector. |
| Dos o más sitios autorizados | Vista **Elige un sitio** y **Selecciona el sitio que deseas abrir.**, con una opción por sitio. |
| Una o más comprobaciones fallidas | Se muestra **No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.**, junto a los sitios que sí se pudieron comprobar. No se interpreta el error como denegación, ni se afirma que no hay sitios, ni se redirige automáticamente aunque sólo uno esté confirmado. |

El selector no muestra opciones deshabilitadas de sitios denegados. Procedimientos institucionales permanece primero y Competencias digitales segundo. Un sitio con varios módulos, roles o asignaciones produce una única opción. Los enlaces específicos conservan su destino, incluso si otro sitio sería el único disponible; una URL denegada muestra su aviso, no redirige silenciosamente a otra aplicación.

### Sin destellos ni ciclos de historial

`SessionController.Choices` resuelve el acceso antes de servir HTML. La excepción de `/accesos` en `ReactPageMiddleware` permite llegar al controlador sin emitir primero el shell. Si existe un único sitio, la navegación normal recibe una redirección local HTTP 302 sin HTML; el navegador registra el destino final de esa navegación.

En el transporte React, el servidor responde `{ "redirect": "ruta local comprobada" }` y `navigation.tsx` obtiene el destino antes de publicar una página o añadir historial. No hay un efecto de React que dibuje el selector y luego redirija. Al volver por Atrás se reemplaza la entrada actual, sin insertar otra; se rechazan destinos externos, ciclos y cadenas excesivas. Cada destino conserva su autorización propia: entre la comprobación y la navegación pueden revocarse permisos y el backend seguirá rechazando el acceso.

No se almacenan opciones de autorización en el navegador ni se conserva una lista entre solicitudes. La representación sigue usando identidad efectiva, permisos del representado y actor real; si sólo hay un sitio compatible, se abre con su indicador y salida existentes. Una denegación o falla temporal no termina la sesión.

## Registro extensible

`tdv2/Web/PortalSites.cs` contiene el registro del servidor, incorporado mediante `PortalSites.Register` en `ServiceRegistration`. Cada `PortalSite` declara:

- `Name`: nombre visible, sin claves técnicas.
- `Destination`: ruta local canónica de entrada, única en el registro.
- `Order`: orden de presentación; 10 para Procedimientos, 20 para Competencias.
- `CanOpen`: comprobación asíncrona que reutiliza el servicio real de acceso del sitio.
- `OwnsRoute`: identificación de sus rutas, para comprobar acceso directo y excluir el mismo sitio de las alternativas de una denegación.

Procedimientos reutiliza `RequestAccess.Resolve`. Competencias reutiliza `DigitalAccess.Resolve` y exige al menos un módulo navegable autorizado. Se conservan los controles de empleado, representación y alcance de esos servicios. Las consultas son secuenciales porque comparten servicios y DbContexts de la solicitud. 403/409 representan denegación o contexto incompatible; otros errores de comprobación se marcan indisponibles. 401 conserva el flujo de identidad vigente.

El registro valida rutas locales sin codificación, fragmentos, parámetros, segmentos de escape ni destinos de control como `/connect` o `/accesos`. Rechaza destinos duplicados. Los nombres, orden y verificadores provienen de código del servidor, nunca de parámetros del usuario. Los destinos registrados se incorporan a la lista permitida de retorno mediante `PortalSites.SafeReturn`, manteniendo las restricciones anteriores de OAuth.

Agregar un sitio requiere integrar su servicio real de autorización y sus endpoints, y registrar un descriptor. No requiere modificar el conteo, la presentación, la lógica de selección ni enumerar casos para tres/cuatro sitios. `AccessBoundary` comprueba también las rutas de un sitio registrado nuevo; sus controladores deben mantener además las reglas propias del recurso y sus alcances. El registro **no sustituye permisos ni aprovisiona aplicaciones** en Nexo. Los sitios adicionales utilizados en pruebas sólo existen dentro del proyecto de verificación y no se publican con la aplicación.

## Archivos de esta revisión

- Nuevo `tdv2/Web/PortalSites.cs`; adaptación de `ApplicationEntry.cs`, `AccessBoundary.cs`, `DomainProblemResponse.cs` y la clase `ReactPageMiddleware` en `RequestBoundaryMiddleware.cs`.
- `tdv2/Hosting/ServiceRegistration.cs`, `Controllers/SessionController.cs` y `Controllers/AuthenticationController.cs`: registro, resolución antes del HTML y retorno permitido extensible.
- `ClientApp/resources/js/pages/Accesos.tsx`: terminología solicitada y opciones adaptables, con diseño y pie existentes. `lib/navigation.tsx`: resolución de redirección antes de render/historial. No se altera `Survey.tsx` en esta revisión.
- `tests/TDV2.NativeVerification/ApplicationEntryTests.cs`, `NativeApplication.cs`, `DigitalTests.cs`: pruebas de entrada, varias asignaciones, registro ampliado y controles sintéticos. Nuevo `ClientApp/tests/browser/sites-native.mjs`; actualización de `application-entry-ui.mjs`, expectativas de `competencias-native.mjs` y pruebas de `tests/frontend/navigation.test.mjs`.
- README, ESTADO_MIGRACION, GUIA, AUTENTICACION_POSTGRESQL y la nota de acceso anterior enlazan el comportamiento vigente. Los demás cambios locales previos se conservan.

## Verificación

Comandos desde la raíz; los binarios nativos de prueba validan su clúster desechable y nunca usan conexiones institucionales:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tests/TDV2.NativeVerification -c Release --no-restore
dotnet run --project tests/TDV2.Verification -c Release --no-restore
$env:TDV2_TEST_CASE_PREFIX='Entrada|Representación|Guardado manual'
$env:TDV2_TEST_SITE_ENTRY_ONLY='true'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DigitalOnly -NoBuild -DirectStart
Remove-Item Env:TDV2_TEST_CASE_PREFIX, Env:TDV2_TEST_SITE_ENTRY_ONLY
# Desde ClientApp: interfaz con transporte sintético, complemento del recorrido ASP.NET real.
node tests/browser/application-entry-ui.mjs
```

Resultados:

| Comprobación | Resultado |
| --- | --- |
| TypeScript, Vite y ASP.NET Release | Correctos; .NET sin errores ni advertencias. |
| Frontend | **99/99**, incluidos destino único sin publicación del selector, una sola entrada de historial, Atrás con reemplazo y rechazo de ciclos/destinos externos. |
| Verificador .NET | **116/116** con fuentes sintéticas. |
| ASP.NET + PostgreSQL aislado | **18/18 casos funcionales**: diez de entrada/registro, siete de guardado manual y uno de representación. Verifican cero/uno/dos/cuatro sitios, asignaciones repetidas, revocación, fallas, HTML 302 sin shell, retorno OAuth y CSRF. Las reglas de representación y del registro se repitieron tras completar el control de acceso directo de sitios futuros; el registro se repitió nuevamente con destinos duplicados y controles en mayúsculas. Todas esas repeticiones pasaron. |
| Edge + ASP.NET + PostgreSQL | **20/20 grupos**, sin errores JS: escritorio 1366 px y móvil 390 px; Mi espacio, intención anónima, cero/uno/dos sitios, ocho sitios registrados sintéticamente, teclado, Atrás, revocación, fallas parciales/totales y enlace específico conservado tras OAuth. El observador de la prueba no detectó renderizado del selector en las entradas directas. |
| Edge con transporte simulado | **14/14 grupos**, sin errores JS, a 1440/390 px: términos exactos, opciones y estados, ausencia de referencias internas, acciones y CSRF, diseño extensible y sin desbordamiento. |
| Revisión final | `git diff --check` correcto; paquetes, contratos del instrumento y migraciones sin modificaciones. |

Las dos primeras ejecuciones del recorrido nuevo de navegador se detuvieron en el transporte de prueba de OAuth: la intercepción sólo de `/connect` no interceptaba toda la cadena que empezaba en `/accesos`. El entorno bloqueó la salida. Se corrigió **únicamente el fixture** para resolver el inicio local sin seguir al proveedor y devolver el callback sintético, conservando state/cookies/canje reales del backend. La repetición completó los 20 grupos. Los informes con las interrupciones se conservan junto a los resultados correctos; no se presentan esas ejecuciones como aceptación completa del navegador.

Evidencia: [mi-espacio-sitios-20261028](evidencia/mi-espacio-sitios-20261028/). Las fuentes Microsoft/Nexo de los informes son sintéticas. Los clústeres de `.artifacts/native-postgres-20261028-215404-d19436f5`, `...-220119-254dfe4f`, `...-220441-d5e2f806` y `...-221012-d8565f11` se detuvieron al terminar.

Capturas revisadas del backend real de prueba:

- Dos sitios: [escritorio](evidencia/mi-espacio-sitios-20261028/sitios-both-1366.png) y [móvil](evidencia/mi-espacio-sitios-20261028/sitios-both-390.png).
- [Sin sitios](evidencia/mi-espacio-sitios-20261028/sitios-none-390.png), [falla parcial con un sitio comprobado](evidencia/mi-espacio-sitios-20261028/sitios-unavailable-390.png) y [falla sin sitios comprobados](evidencia/mi-espacio-sitios-20261028/sitios-failed-390.png).
- Ocho sitios sintéticos: [escritorio](evidencia/mi-espacio-sitios-20261028/sitios-ocho-1366.png) y [móvil](evidencia/mi-espacio-sitios-20261028/sitios-ocho-390.png), con el pie en el flujo y todas las opciones legibles.
- Entrada directa: [Competencias](evidencia/mi-espacio-sitios-20261028/directo-cd-1366.png) y [Procedimientos](evidencia/mi-espacio-sitios-20261028/directo-td-1366.png).

## Límites

Microsoft, identidades, publicaciones Nexo y sitios adicionales de las pruebas son sintéticos. ASP.NET, cookies, PostgreSQL desechable, React y Edge sí se ejecutan. No se acredita acceso institucional real ni un despliegue Ubuntu. No hay permisos, secretos, tablas, migraciones ni configuración nueva que el operador deba aplicar por este ajuste. No se hizo push ni despliegue.
