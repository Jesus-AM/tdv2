# Corrección puntual de menú, pie TDV2 y texto público — 2026-10-27

## Implementado

Se modifica únicamente la presentación solicitada, conservando el trabajo local anterior.

- **Menú de Competencias:** Avance por dependencia → Resultados generales → Encuesta → Mi informe y ruta → Configuración. Se retira el reordenamiento personal y los grupos/divisores. El componente vuelve a recorrer directamente `data.navigation`, que ya entrega el backend con ese orden después de comprobar permisos. No cambian rutas, claves, IDs, autorización ni Nexo. Se mantiene el ajuste a varias filas cuando falta ancho, sin separadores. El componente coincide con su estado anterior a la prioridad personal; se conservan 1400 px, cabecera/cuenta, aviso, reportes y demás mejoras.
- **Pie interno de TDV2:** los anchos fijos de 185 px para DGTI y 140 px para las subdirecciones comprimían el texto. La medición de Noto Sans encontró aproximadamente 239 px de texto en el nombre de DGTI y 159 px en Desarrollo de Sistemas, antes de sumar separador/espaciado. Se sustituyen esos límites solo dentro de `.app-footer` por columnas que reservan el ancho natural del texto y distribuyen el espacio disponible. La primera columna conserva UACJ a 220 px; DGTI mantiene su jerarquía y las cuatro subdirecciones sus mismas letras de 14 px. Con más de 1280 px de contenedor, hay seis columnas; hasta 1280, tres; hasta 900, dos; hasta 600, una. Se conservan el orden institucional, los recursos, azul #213455, firmas blancas, Noto Sans, separadores y el flujo vertical. Los pies público y de Competencias permanecen intactos.
- **Portal:** se elimina únicamente «Este test no sustituye la encuesta de competencias digitales.». Permanecen «¿Qué tan bien dominas Microsoft 365?», «Pon a prueba tus conocimientos de Microsoft 365.», «Iniciar test» y la encuesta principal. Comparación exacta contra el archivo previo confirma que no hay otro cambio en Razor.

Archivos productivos: `ClientApp/resources/js/pages/CompetenciasDigitales.tsx`, `ClientApp/resources/css/competencias-digitales.css`, `ClientApp/resources/css/app.css` y `tdv2/Views/Home/Index.cshtml`. Pruebas ajustadas: `identity-digital-ui.mjs`, `quiz-inter-ui.mjs` e `identity-checks.mjs`. Se actualizan README, ESTADO_MIGRACION, guía de Competencias y documentación/evidencia.

## Probado

- TypeScript, Vite y compilación ASP.NET/ejecutable de verificación Release correctos. Permanecen las advertencias NuGet previas NU1902/NU1903 de Pkcs.
- **51 comprobaciones** de portal/test/TDV2. Pie interno revisado en 1920, 1440, 1366, 1280, 1024, 768, 390 y 320 px: fuentes/jerarquía, prefijo y nombre en dos líneas, ancho aprovechado, imágenes proporcionales, ausencia de desbordamiento y pie en páginas largas, cortas, vacías, carga y error. Se mantienen el test, su ayuda, XP/resultados y progreso al cerrar/abrir, además de los PDF y accesos separados.
- **Seis conjuntos sintéticos de módulos en cuatro tamaños** (1366, 768, 390 y 320 px): orden anterior, sin grupos, módulos no concedidos ausentes, reportes/filtros y foco de captura con el menú completo. No se ha cambiado ni probado nuevamente la autorización institucional real.
- Bordes de las consultas de contenedor comprobados por separado: **1281/1280, 901/900 y 601/600 px**, sin desplazamiento horizontal y con prefijo/nombre completos.
- Comparación de once archivos protegidos confirma que cuenta, privacidad, instrumento, captura, reportes, autorización, firmas compartidas y lógica/ayuda del test siguen idénticos. `git diff --check` correcto.

Pruebas en Edge con React real y datos/identidades sintéticos; el portal se exportó con ASP.NET TestServer sin base. Móvil y tableta emulados. No se ejecutaron bases institucionales, migraciones, reinicios de encuestas, push ni despliegues. No hace falta administración de Nexo. Queda pendiente la revisión en dispositivos físicos y sesión real dentro de una fase operativa autorizada.

## Capturas

| Vista | Escritorio | Móvil |
|---|---|---|
| Menú de Competencias restaurado | [1366 px](evidencia/menu-pie-correccion-20261027/digital-navigation-admin-1366.png) | [390 px](evidencia/menu-pie-correccion-20261027/digital-navigation-admin-390.png) |
| Firmas del pie TDV2 | [1366 px](evidencia/menu-pie-correccion-20261027/internal-footer-1366.png) | [390 px](evidencia/menu-pie-correccion-20261027/internal-footer-390.png) |
| Inicio interno y flujo del pie | [Inicio](evidencia/menu-pie-correccion-20261027/internal-inicio-1366.png) | [Inicio](evidencia/menu-pie-correccion-20261027/internal-inicio-390.png) |

[Pie en tableta](evidencia/menu-pie-correccion-20261027/internal-footer-1024.png) · [Texto del portal](evidencia/menu-pie-correccion-20261027/diagnosis-after-1366.png) · [Mediciones del pie/portal](evidencia/menu-pie-correccion-20261027/portal.json) · [Navegación](evidencia/menu-pie-correccion-20261027/digital.json) · [Puntos de cambio de fila](evidencia/menu-pie-correccion-20261027/footer-breakpoints.json).

Artefactos completos en `.artifacts/menu-footer-correction-20261027/`. Comandos desde la raíz: `npm.cmd --prefix ClientApp run types:check`, `npm.cmd --prefix ClientApp run build` y `dotnet build tests/TDV2.Verification/TDV2.Verification.csproj --no-restore -c Release -p:UseAppHost=false`. Exportación: `dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll --export-public .artifacts/menu-footer-correction-20261027/portal/public-after.html`.

Desde `ClientApp`: ejecutar `node tests/browser/quiz-inter-ui.mjs after` con `TDV2_VISUAL_ARTIFACTS=../.artifacts/menu-footer-correction-20261027/portal`; ejecutar `node tests/browser/identity-digital-ui.mjs` con `TDV2_NAVIGATION_ONLY=1` y `TDV2_VISUAL_ARTIFACTS=../.artifacts/menu-footer-correction-20261027/digital`, en una consola temporal. Los arneses no usan User Secrets, `.env` ni servicios institucionales.
