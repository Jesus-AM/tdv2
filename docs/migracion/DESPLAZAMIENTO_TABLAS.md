# Desplazamiento y presentación de tablas — 2026-10-09

## Causa comprobada

Se reprodujo sobre el bundle local anterior, con Edge, dos identidades sintéticas separadas, ASP.NET y PostgreSQL nativo desechable. Arrastrar la barra horizontal nativa produjo `pointerdown DIV`, `focusout TEXTAREA` con `relatedTarget=DIV`, `focusin DIV` y eventos `scroll`. La reserva pasó de propia a libre, aunque sólo se había desplazado la tabla. [Traza anterior](desplazamiento-tablas-20261009/reproduccion-anterior.json).

`FormatoUR.blockEvents.onBlurCapture` consultaba `document.activeElement` después del blur. Como el contenedor desplazable no pertenece a una fila, llamaba a `endBlock`, guardaba y liberaba. El problema no estaba en PostgreSQL ni en SignalR. La pérdida de foco hacia `body` sin destino tenía el mismo riesgo; no se presupone que todo blur de scrollbar tenga `relatedTarget=null`.

## Implementado

- `RecordEditingContext` distingue destino real de foco, clic completado fuera del contexto y gesto de desplazamiento. El foco en el contenedor de captura y el blur sin destino conservan el contexto. Detecta barras mediante geometría, gestos por movimiento/cancelación y desplazamiento durante la pulsación. No cancela eventos, fuerza foco, captura el puntero ni añade una espera arbitraria para liberar.
- El cambio real de registro o sección conserva el guardado y liberación tras confirmación del motor existente. Las primeras teclas pendientes se completan antes de salir, comprobando que el usuario no haya vuelto a la fila durante la espera. Ayudas, menús y consulta del avatar pertenecen al contexto original. Desplazar no adquiere ni renueva reservas; caducidad, revocación y cambios pendientes siguen gobernados por el servidor y el motor.
- Ambas tablas comparten `RowEditingPresence` en una primera columna **Edición** de 124 px, fija al desplazar horizontalmente, con fondo opaco, separación, foto/iniciales, estado breve y nombre completo accesible. Sustituye la posición anterior en Acciones. No hay nombres largos dentro de la celda ni presencia duplicada.
- Acciones mide 76 px y se alinea arriba. Eliminar mantiene icono rojo pequeño, área de 44 × 44 px, tooltip, nombre accesible y foco de teclado. Si está deshabilitado, el contenedor enfocable comunica el motivo. Se conserva el diálogo MUI y la adquisición atómica del servidor; no cambió el protocolo de eliminación ni de renovaciones tardías.
- Campos multilínea de ambas tablas, incluidos detalles Otra/Otro: mínimo dos y máximo cuatro líneas visibles, con desplazamiento interno, sin recortar texto ni añadir límites de caracteres. También funciona en consulta. ¿Qué entrega? tiene el mismo mínimo de 280 px que Trámite; no se redujo la tipografía. La página desplaza verticalmente y la tabla horizontalmente, sin el límite adicional de 65vh.

El inicio de un gesto táctil sobre un campo de otra fila tampoco adquiere su reserva: la interacción espera foco o un toque completado. Un arrastre sobre la barra interna de un textarea no inicia otra edición. El gesto sobre una fila libre y el toque posterior que sí cambia de registro están comprobados con ambas identidades.

Se conservan tablas, fuente de 13 px, encabezados en negritas, datos, permisos, fotografías, autoguardado, SignalR, versiones, Medios independiente, entrega hasta Sistemas y protección de enviados. No hay cambios de persistencia ni nueva migración.

## Verificado

- **9/9 recorridos nuevos**, Edge con dos identidades sintéticas distintas, sesiones separadas, ASP.NET, SignalR y PostgreSQL nativo. Barras horizontal/vertical nativas; selección y continuación en el cursor; rechazo directo de escritura y reserva de eliminación ajenas; filas independientes; salida real con guardado/liberación y actualización sin recargar; pegado largo, scroll interno y persistencia; primera tecla/Tab; ayudas/menús; SII y Otra/Otro; Medios independiente; Eliminar rojo, foco de teclado, 44 × 44 y confirmación; escritorio/móvil, avatar accesible y gesto táctil emulado; consulta enviada con textos completos y rechazo del backend. [Resultado](desplazamiento-tablas-20261009/table-scroll-browser.json).
- **11/11 recorridos de regresión** con las mismas dos identidades y transporte real del editor: carrera simultánea de reserva, otra pestaña como caso adicional, eliminación/renovación concurrentes, conexión interrumpida/reconectada, vencimiento y autorización, opciones de procedimiento actualizadas entre usuarios, entrega real hasta Sistemas sin requisitos posteriores y bloqueo del enviado. [Resultado](desplazamiento-tablas-20261009/delivery-browser.json).
- **78/78 pruebas del frontend**, incluidas reservas/autoguardado con transporte simulado, y **76/76 verificaciones de dominio/HTTP con dobles de pruebas**. No se presentan como aceptación de navegador ni como pruebas institucionales. TypeScript y build Vite correctos.
- **51/51 comprobaciones de edición y autorización en PostgreSQL nativo aislado**: atomicidad, permisos, representación, dos usuarios/pestañas, testigos vencidos, contenido/versiones concurrentes, primera etapa, eliminación, módulos y enviados. [Resultado](desplazamiento-tablas-20261009/editing-native.json).

Las capturas de escritorio/móvil se revisaron desde el directorio sintético `.artifacts/native-postgres-20261009-125521-e3639789`. Los recorridos del navegador anteriores usaron los servicios de escritura/reservas reales. El caso de carrera en la regresión detiene peticiones para hacerlas coincidir y luego obtiene sus respuestas del ASP.NET real; no fabrica concesiones ni respuestas de guardado.

## Ejecutar la comprobación aislada

Desde la raíz, después de construir el frontend:

```powershell
$env:PATH = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;' + $env:PATH
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
$env:TDV2_TEST_NODE = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:TDV2_TEST_EPHEMERAL_TLS = 'true'
$env:TDV2_TEST_BROWSER_FLOW = 'table-scroll'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
```

El runner crea y detiene exclusivamente un clúster PostgreSQL aislado. Desactiva el argumento predeterminado de Playwright `--hide-scrollbars` para probar barras nativas con el mouse. Las escrituras, reservas y notificaciones usan ASP.NET/PostgreSQL/SignalR reales; Microsoft, autorización institucional y origen SII son dobles sintéticos del proyecto de pruebas. No se consulta ningún servicio institucional. El gesto táctil usa eventos CDP, sin simular respuestas del editor.

El portapapeles Windows puede entregar CRLF donde el textarea representa LF: la comprobación conserva todas las líneas y su contenido, sin imponer una conversión nueva a las respuestas guardadas.

Regresiones adicionales, siempre en bases desechables separadas:

```powershell
$env:TDV2_TEST_BROWSER_FLOW = 'delivery'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -BrowserOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -EditingOnly -SkipBrowser
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
```

## Pendiente fuera del aislamiento

Comprobar mouse, panel táctil y pantalla táctil físicos, incluidos navegadores móviles y sus barras superpuestas. La emulación de escritorio/móvil y de tacto no acredita dispositivos físicos ni integración institucional. No se ejecutaron migraciones institucionales, no se modificaron Nexo/Herd/credenciales y no se publicó ni desplegó.
