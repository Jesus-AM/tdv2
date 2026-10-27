# Distribución de Competencias digitales — 2026-10-27

## Implementado

El contenedor común `.cd-wrap` pasa a un máximo de **1400 px**, centrado, con separación interior adaptable entre 16 y 32 px y respeto de las áreas seguras del dispositivo. Cabecera, navegación, banda de título, contenido y pie comparten sus bordes. Las tarjetas aprovechan el ancho disponible; los párrafos de bienvenida y del informe conservan un límite de lectura de 75 caracteres tipográficos. Los bordes siguen midiendo 1 px.

La cabecera utiliza dos columnas: UACJ a la izquierda y cuenta a la derecha, centrados verticalmente. La navegación ocupa una fila propia. Desde 701 px, la cuenta muestra fotografía, nombre de 14 px e indicador del menú. Hasta 700 px muestra únicamente el botón de fotografía de 44 × 44 px; su nombre accesible incluye la identidad efectiva completa y el menú permite leer el nombre completo. El menú compartido solo activa esta etiqueta ampliada cuando lo solicita Competencias; Procedimientos conserva su comportamiento anterior. La representación mantiene su aviso y salida visibles.

El pie mantiene el orden Dirección General, Desarrollo de Sistemas, Tecnología Educativa, Operación y Redes e Inteligencia de Datos. Distribuye cinco columnas por encima de 1040 px, con separación de 20–32 px y mayor espacio para la Dirección General. Hasta 1040 px, la Dirección ocupa la primera fila y las subdirecciones se organizan en dos columnas; hasta 360 px pasan a una columna. No se reducen las letras de las firmas: se conservan los tamaños y pesos institucionales vigentes. El pie permanece en el flujo normal y el contenido flexible lo mantiene al fondo en páginas cortas.

Las reglas nuevas están limitadas a Competencias. No se modificaron colores, fuentes, recursos gráficos, permisos, transporte, validación, autoguardado, evaluación, reinicio ni migraciones. La vista pública, sus estilos compartidos, el tema MUI y `Survey.tsx` conservan sus hashes previos al ajuste.

### Archivos

- `ClientApp/resources/css/competencias-digitales.css`: ancho común, cabecera, cuenta adaptable, límite de lectura y distribución específica del pie.
- `ClientApp/resources/js/Components/AccountMenu.tsx`: propiedad opcional `includeNameInLabel`, desactivada por defecto.
- `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`: activa esa etiqueta accesible únicamente en el subsitio.
- `ClientApp/tests/browser/identity-digital-ui.mjs`: mediciones de alineación y separación, siete anchos, cuenta mediante teclado y nombre completo.
- README, ESTADO_MIGRACION, guía del diagnóstico, esta nota y sus evidencias.

## Probado

- TypeScript y compilación Vite correctos; ASP.NET Release correcto, con las dos advertencias NuGet previas de `System.Security.Cryptography.Pkcs`.
- **Siete recorridos con Vite real y siete con bundles**: 1920, 1440, 1366, 1024, 768, 390 y 320 px. Sin errores JavaScript. Contenedor centrado de `min(ancho disponible, 1400 px)`, bordes interiores coincidentes, logo/cuenta en una fila y navegación debajo.
- Inicio, perfil, preguntas, informe, pie, cuenta, iniciales y representación. Nombre completo accesible, apertura del menú con Enter, cierre con Escape y devolución del foco. Noto Sans realmente renderizada, verificada mediante CDP.
- Sin desplazamiento horizontal del documento. Firmas uniformes y separación suficiente, pie después del contenido largo y al fondo en estados vacío, carga y error breves, sin altura artificial.
- Pasos bloqueados y foco en el primer pendiente. El fallo de guardado sintético mantiene la respuesta y no desbloquea el siguiente paso. Aviso de privacidad de lectura sin escrituras. No se cambió la lógica de captura.
- Regresión pública/juego: **19 grupos** correctos; revisión de Procedimientos en escritorio y móvil con su presentación existente. La vista pública usada por el arnés es el HTML previamente exportado por ASP.NET, cuyo archivo fuente no cambió.

Pruebas de presentación en Edge con React/MUI reales y datos/transporte sintéticos; no acreditan autenticación institucional, permisos reales ni persistencia PostgreSQL. Móvil y tableta emulados. Se conserva la advertencia de obsolescencia HMR del arnés Vite. No se accedió a bases institucionales ni se ejecutaron migraciones, reinicios, push o despliegues. Pendiente únicamente la revisión en dispositivos físicos y sesiones institucionales dentro de la fase operativa autorizada.

## Evidencia

| Vista | Captura |
|---|---|
| Escritorio: ancho, cabecera y pie | [1920 px](evidencia/cd-layout-1400-20261027/digital-home-1920.png) |
| Tableta: perfil y pie | [1024 px](evidencia/cd-layout-1400-20261027/digital-profile-1024.png) |
| Móvil: distribución y firmas | [390 px](evidencia/cd-layout-1400-20261027/digital-home-390.png) · [320 px](evidencia/cd-layout-1400-20261027/digital-home-320.png) |
| Cuenta móvil: nombre completo | [Menú](evidencia/cd-layout-1400-20261027/digital-account-390.png) |
| Representación móvil | [Aviso y salida](evidencia/cd-layout-1400-20261027/digital-representation-390.png) |

[Mediciones Vite](evidencia/cd-layout-1400-20261027/vite-report.json) · [Mediciones bundles](evidencia/cd-layout-1400-20261027/build-report.json) · [Portal/Procedimientos](evidencia/cd-layout-1400-20261027/portal-report.json). Las capturas completas permanecen también en `.artifacts/cd-layout-1400/`.

Repetición desde `ClientApp`, con Node disponible y sin iniciar el host institucional:

```powershell
npm run types:check
npm run build
$env:TDV2_TEST_VITE='1'
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cd-layout-1400/vite'
node tests/browser/identity-digital-ui.mjs
Remove-Item Env:TDV2_TEST_VITE
$env:TDV2_VISUAL_ARTIFACTS='../.artifacts/cd-layout-1400/build'
node tests/browser/identity-digital-ui.mjs
```

El arnés usa puertos loopback aleatorios y no lee `.env` ni User Secrets. Compilación ASP.NET desde la raíz: `dotnet build tdv2/tdv2.csproj --no-restore -c Release -p:UseAppHost=false`.
