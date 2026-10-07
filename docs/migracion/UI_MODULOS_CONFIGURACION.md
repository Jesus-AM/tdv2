# Módulos y Configuración: presentación — 2026-10-07

## Implementado

- `PageHeading` comparte un único H1 de 24–26 px/600, descripción de 14 px, separación de 8 px y margen de 24 px antes del contenido. Acciones alineadas arriba a la derecha; se acomodan debajo cuando falta espacio.
- Rutas accesibles de navegación en Configuración procesos, Sincronizaciones, Pruebas de acceso, Rol y área y Actuar como usuario. Sustituyen los botones de regreso duplicados. Los enlaces conservan el destino y la navegación interna.
- Barra blanca con logo SVG y tipografía conservados. Contenedor alineado con las páginas, 32 px entre logo y navegación de escritorio, menú móvil cuando falta espacio. `ResizeObserver` reserva la altura real de la barra; no se agregan barras fijas.
- Menú de Configuración con iconos, orden existente y selección marcada mediante borde, fondo y `aria-current`. Menú de cuenta independiente, sin desplazamiento del contenido al abrirlo. La agrupación sigue usando exclusivamente la jerarquía autorizada publicada por Nexo.
- `SettingsSection` agrupa títulos, explicaciones y controles. Accesos compactos en Configuración; separación entre consulta por rol/área y representación. Se conserva la advertencia de cambios reales.
- Sincronizaciones separa fuentes/acciones, programación y ejecuciones. Guardar sigue junto a la programación; indicador de cambios pendientes. Actualizar estado conserva su ancho y no desplaza el contenido. El aviso existente también muestra errores persistentes y su resolución, sin mover los campos.
- Historial vacío legible en móvil; la tabla con ejecuciones mantiene desplazamiento horizontal interno y puede recibir foco de teclado.
- Configuración procesos conserva exactamente sus opciones, confirmación, concurrencia y operaciones. Sólo cambia su presentación y la asociación accesible del texto explicativo.

Los estilos de secciones están acotados a Configuración. No se modifican el tema ni las fuentes, contratos, permisos, participación de UR, OAuth, sincronizadores, edición colaborativa o esquema. [119 archivos de backend/lógica protegida conservaron su hash](ui-modulos-20261007/codigo-conservado.json), incluidos `FormatoUR.tsx`, el motor de bloques, eliminación y avatares. Se integró el trabajo local previo sin restaurar versiones de GitHub.

## Comprobaciones

- TypeScript: `npm --prefix ClientApp run types:check`.
- Producción frontend: `npm --prefix ClientApp run build`.
- Pruebas frontend: `npm --prefix ClientApp run test:forms`, **64/64**.
- Recorridos Edge/ASP.NET/PostgreSQL desechable: **32** de captura, **13** de acceso, **4** de alcance y **11** de sincronización, todos aprobados. Incluyen reservas concurrentes, eliminación directa, Select, primera escritura/foco, fotografía/tooltip, propuestas pendientes, tres intervalos de SignalR, revocación, enviados, representación, auditoría y conflictos de programación. [Captura](ui-modulos-20261007/browser.json), [acceso](ui-modulos-20261007/browser-access.json), [alcance](ui-modulos-20261007/browser-scope.json), [sincronización](ui-modulos-20261007/browser-sync.json).
- Presentación: siete páginas completas a **360, 768, 1366 y 1920 px**, textos largos, menú móvil, teclado/Escape/restauración de foco, permisos distintos, cambios pendientes y confirmación/cancelación. Verifica un H1, ausencia de desbordamiento de página y contenido por debajo de la barra.
- Ampliación **CSS al 200 %** en las siete pantallas, con altura reservada de barra y cambio a menú móvil; se identifica explícitamente como emulación, no como operación manual del zoom del navegador.
- Carga y error de actualización mantienen las coordenadas de la primera sección y el borrador. Cancelar la confirmación de participación conserva la versión almacenada y los valores propuestos. El guardado de programación de la prueba mantiene desactivada la ejecución automática.
- Nexo, Microsoft y las fuentes remotas son dobles sintéticos; no se contactaron servicios institucionales. Las fotos sintéticas pueden verse como iniciales o miniatura blanca. Se bloquearon las fuentes externas durante las capturas; la definición tipográfica de la aplicación se conserva.

Las 28 capturas comparables anteriores proceden de `native-postgres-20261007-172826-6db361cf`. Se descartaron siete capturas de zoom prematuras (antes de montar React); el verificador se corrigió para esperar el H1. El intento que detectó ese defecto no se cuenta como prueba aprobada.

La regresión completa procede de `native-postgres-20261007-174835-2f9c9823`. Las comprobaciones finales de presentación y avisos se documentan en [after.json](ui-modulos-20261007/after.json), corrida `native-postgres-20261007-181024-1b50e32d`: 44 capturas con comprobaciones de geometría y una captura adicional del menú móvil, sin errores JavaScript. Junto con las 28 anteriores, se conservan **73 imágenes**.

La comprobación adicional del selector de área detectó que personalizar la etiqueta sin conservar los otros `slotProps` de MUI retiraba las propiedades de Autocomplete. Se corrigió combinando las propiedades originales y se verificó apertura, selección y habilitación de la acción. La corrida que detectó ese defecto no se cuenta como aprobada.

Después del último cambio de presentación de errores, se repitieron los **11/11** casos de sincronización: [evidencia final](ui-modulos-20261007/browser-sync-final.json), `native-postgres-20261007-175933-719f3118`. No son casos adicionales a los once anteriores.

## Capturas comparables

| Pantalla | Antes, 1366 px | Después, 1366 px | Antes, 360 px | Después, 360 px |
|---|---|---|---|---|
| Procesos operativos | [Antes](ui-modulos-20261007/before-inicio-1366.png) | [Después](ui-modulos-20261007/after-inicio-1366.png) | [Antes](ui-modulos-20261007/before-inicio-360.png) | [Después](ui-modulos-20261007/after-inicio-360.png) |
| Configuración | [Antes](ui-modulos-20261007/before-configuracion-1366.png) | [Después](ui-modulos-20261007/after-configuracion-1366.png) | [Antes](ui-modulos-20261007/before-configuracion-360.png) | [Después](ui-modulos-20261007/after-configuracion-360.png) |
| Sincronizaciones | [Antes](ui-modulos-20261007/before-sincronizaciones-1366.png) | [Después](ui-modulos-20261007/after-sincronizaciones-1366.png) | [Antes](ui-modulos-20261007/before-sincronizaciones-360.png) | [Después](ui-modulos-20261007/after-sincronizaciones-360.png) |
| Pruebas de acceso | [Antes](ui-modulos-20261007/before-pruebas-1366.png) | [Después](ui-modulos-20261007/after-pruebas-1366.png) | [Antes](ui-modulos-20261007/before-pruebas-360.png) | [Después](ui-modulos-20261007/after-pruebas-360.png) |
| Configuración procesos | [Antes](ui-modulos-20261007/before-procesos-1366.png) | [Después](ui-modulos-20261007/after-procesos-1366.png) | [Antes](ui-modulos-20261007/before-procesos-360.png) | [Después](ui-modulos-20261007/after-procesos-360.png) |
| Rol y área | [Antes](ui-modulos-20261007/before-rol-area-1366.png) | [Después](ui-modulos-20261007/after-rol-area-1366.png) | [Antes](ui-modulos-20261007/before-rol-area-360.png) | [Después](ui-modulos-20261007/after-rol-area-360.png) |
| Actuar como usuario | [Antes](ui-modulos-20261007/before-representacion-1366.png) | [Después](ui-modulos-20261007/after-representacion-1366.png) | [Antes](ui-modulos-20261007/before-representacion-360.png) | [Después](ui-modulos-20261007/after-representacion-360.png) |

El mismo directorio contiene las parejas a 768/1920 px y las capturas finales de zoom, carga, error, permisos, confirmación, aviso de escritura y menú móvil. [Inventario anterior](ui-modulos-20261007/before.json).

## Repetir desde la raíz

Con Node/npm y .NET disponibles en PATH:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run build
npm --prefix ClientApp run test:forms
$env:TDV2_TEST_BROWSER_FLOW = 'presentation'
$env:TDV2_UI_PHASE = 'after'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
$env:TDV2_TEST_BROWSER_FLOW = 'all'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly
Remove-Item Env:TDV2_TEST_BROWSER_FLOW, Env:TDV2_UI_PHASE
```

El script requiere PostgreSQL 18 local y Edge, crea un clúster/base desechables bajo `.artifacts` y los detiene al terminar. No usa User Secrets ni conexiones institucionales. Sus fixtures permanecen exclusivamente en pruebas.

Para revisión manual, abrir `tdv2.slnx` y conservar el perfil **TDV2 HTTPS + React**. Recorrer Configuración y sus hijos con Tab/Enter/Escape, reducir el ancho y usar zoom 200 %. Probar guardados únicamente con entorno/cuentas sintéticos; no hay instalación de esquema nueva en esta entrega.

## Pendiente

Validación manual con zoom nativo del navegador, lector de pantalla y dispositivos físicos; las pruebas automáticas cubren reflujo, ampliación CSS y teclado. Las nuevas reglas de participación siguen aplazadas y no se ampliaron permisos. No se requiere migración nueva ni publicación de Nexo. Las migraciones anteriores pendientes de instalación institucional no cambian de estado por esta entrega.

No hubo cambios en Nexo, Herd, credenciales o bases institucionales; no se publicó ni desplegó.
