# Cabecera azul de Competencias digitales — 2026-10-28

## Implementado

Corrección exclusivamente visual de la cabecera de Competencias sobre el trabajo local existente. Se recupera el fondo institucional `#213455` mediante `--cd-blue` / `--uacj-blue`, incluida la zona detrás del logotipo y la fila de módulos. Nombre de cuenta, flecha y navegación blancos; opción activa con borde amarillo institucional, peso y `aria-current`. Hover/foco aclaran suavemente el azul; el contorno de teclado es blanco en la superficie azul. La banda clara de representación conserva sus colores y su foco azul, con ambas identidades y salida visibles.

Se mantienen las dos filas actuales: logotipo/cuenta y navegación debajo. No se implementa la propuesta de una sola fila. Permanecen el contenedor adaptable de 1400 px, Noto Sans local, fotografía/avatar, menú de cuenta, ancho y ajuste a varias filas de los módulos. La navegación sigue recorriendo la colección autorizada recibida del backend/Nexo, sin reordenar, añadir grupos ni cambiar permisos.

El SVG oficial entregado en `Downloads/TRANSFORMACION_DIGITAL_logo_blanco 2.svg` se copia **sin modificar sus bytes** a `tdv2/wwwroot/images/logos/transformacion-digital-blanco.svg`. Es un SVG autocontenido de 101457 bytes, `viewBox="0 0 2238 484"`, con una imagen embebida; no contiene scripts, manejadores de eventos ni referencias externas. No se redibuja, vectoriza ni invierte mediante filtros. Conserva su proporción, con altura de 48 px en escritorio y 40 px en móvil, dentro de los límites adaptables existentes. El enlace conserva `/`, su nombre accesible y el texto alternativo «Transformación Digital UACJ».

Ruta pública exacta, en minúsculas: **`/images/logos/transformacion-digital-blanco.svg`**. La aplicación no utiliza Downloads, rutas Windows ni `file://` para cargarla. SHA-256 del original y la copia:

```text
6030926EE658F78C39BCA7877325A624617E0A9630D31BE946E4783292D74C47
```

El cuerpo blanco, tarjetas/controles y pie aprobado de seis firmas no cambian. Tampoco el portal público, Procedimientos, encuesta, informe, instrumento, auditoría, correcciones de paquetes/401 ni migraciones. Se comparan por SHA-256 59 archivos protegidos con su estado al comenzar esta tarea; todos permanecen idénticos. Los cambios de CSS respecto al inicio de esta tarea se limitan al fondo/estados de cabecera, bajo `.cd-site`; en el componente solo cambia la URL del logotipo.

## Archivos de esta revisión

Producción:

- `ClientApp/resources/css/competencias-digitales.css`: azul, contraste, interacción y foco de cabecera/navegación.
- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`: referencia al SVG blanco.
- `tdv2/wwwroot/images/logos/transformacion-digital-blanco.svg`: copia exacta del recurso oficial.

Verificación: `ClientApp/tests/browser/identity-digital-ui.mjs`, actualizado para comprobar el recurso/carga MIME, ausencia de filtro, colores calculados, foco de teclado y contraste de interacción. Se reutilizan los recorridos de cuenta, representación, encuesta, informe, módulos autorizados y pie.

Documentación: README, ESTADO_MIGRACION, guía de Competencias, esta nota y aviso de revisión posterior en `CABECERA_PIE_JUEGO_20261028.md`. Las capturas de aquella entrega permanecen como evidencia histórica.

## Verificación

| Comprobación | Resultado |
| --- | --- |
| TypeScript | `tsc --noEmit` correcto |
| Frontend | Build Vite correcto |
| ASP.NET/Verification Release | Cero errores y cero advertencias |
| Navegador con bundles | [11 grupos correctos, cero errores JS](evidencia/cabecera-azul-20261028/digital-build-report.json) |
| Navegador con Vite real | [11 grupos correctos, cero errores JS](evidencia/cabecera-azul-20261028/digital-vite-report.json) |
| Paquete local | `dotnet publish` correcto; SVG original, repositorio y paquete idénticos por SHA-256; [HTTP 200, `image/svg+xml`, nombre exacto en minúsculas](evidencia/cabecera-azul-20261028/published-resource.json) mediante servidor estático de prueba |
| Conservación | [59 archivos protegidos sin cambios](evidencia/cabecera-azul-20261028/conservacion.json), incluidos portal, pie compartido, seguridad/401, paquetes, instrumento, actividad y migraciones |

Siete anchos: **1920, 1440, 1366, 1024, 768, 390 y 320 px**. Además, se recorren seis combinaciones sintéticas de módulos autorizados en cuatro anchos: participante, responsable, supervisor, administrador, solo informe y solo reporte. Se conserva el orden recibido, sin grupos nuevos. Se verifica carga/proporción del SVG, nombre/foto y avatar de respaldo, apertura de cuenta por teclado y retorno de foco, representación y salida, módulo activo, ausencia de desbordamiento horizontal y alineación de las dos filas con contenido/pie. Captura e informe mantienen sus controles, fuente y fondos; el foco del primer pendiente queda debajo de la cabecera. El pie pasa las comprobaciones existentes de contenido corto, largo, vacío, carga y error.

El contraste textual calculado de la cabecera es **12,45:1** en estado normal y al menos **8,56:1** en foco/hover; también se comprueba la banda de representación. La familia realmente renderizada sigue siendo Noto Sans local. Vite conserva una advertencia de obsolescencia de `server.hmr` en el arnés existente; ambas ejecuciones completan correctamente y no se modifican paquetes por ello. Se inspeccionaron las capturas de escritorio, tableta y móvil.

Todas las identidades y respuestas de la prueba visual son sintéticas. Se ejecuta React/MUI real en Edge con transporte de pruebas y puertos loopback aleatorios. No se inicia la aplicación con User Secrets ni se conecta a bases institucionales. Esta revisión no acredita entrega real a Nexo, autenticación Microsoft ni persistencia PostgreSQL; conserva esas implementaciones. No se ejecutan migraciones ni reinicios. Escritorio, tableta y móvil se verifican mediante ventanas emuladas; no se afirma prueba en dispositivos físicos.

## Capturas

| Vista | Escritorio | Tableta / móvil |
| --- | --- | --- |
| Participante, dos módulos | [1440 px](evidencia/cabecera-azul-20261028/digital-home-1440.png) | [768 px](evidencia/cabecera-azul-20261028/digital-home-768.png) · [390 px](evidencia/cabecera-azul-20261028/digital-home-390.png) |
| Administrador, cinco módulos | [1366 px](evidencia/cabecera-azul-20261028/digital-navigation-admin-1366.png) | [768 px](evidencia/cabecera-azul-20261028/digital-navigation-admin-768.png) · [390 px](evidencia/cabecera-azul-20261028/digital-navigation-admin-390.png) |
| Teclado y cuenta | [Foco](evidencia/cabecera-azul-20261028/digital-navigation-focus-1440.png) | [Foco móvil](evidencia/cabecera-azul-20261028/digital-navigation-focus-390.png) · [Cuenta](evidencia/cabecera-azul-20261028/digital-account-390.png) |
| Representación | [1440 px](evidencia/cabecera-azul-20261028/digital-representation-1440.png) | [390 px](evidencia/cabecera-azul-20261028/digital-representation-390.png) |

## Repetición local

Con Node/npm y .NET disponibles en PATH, desde la raíz:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
dotnet build tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false
Set-Location ClientApp
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cabecera-azul-20261028/build'
node tests/browser/identity-digital-ui.mjs
$env:TDV2_TEST_VITE='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cabecera-azul-20261028/vite'
node tests/browser/identity-digital-ui.mjs
Remove-Item Env:TDV2_TEST_VITE
```

El paquete local se genera en una copia de código dentro de `.artifacts/cabecera-azul-20261028/publish-source`, excluyendo configuraciones locales y secretos, con cachés existentes y sin actualizar dependencias. Artefacto final: `.artifacts/cabecera-azul-20261028/publish`. El target normal de `dotnet publish` vuelve a compilar el frontend y copia `wwwroot`; no se cambia el proyecto ni se necesita configuración de servidor adicional para el SVG. No se publica en ningún servidor.

No hay nuevas migraciones, configuración ni permisos. Permanecen los pendientes operativos anteriores de Nexo y del instrumento; este ajuste no los ejecuta ni los declara comprobados.
