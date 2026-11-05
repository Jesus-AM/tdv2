# Instrucciones de los seis apartados · 2026-11-05

Se sustituyen las introducciones de Perfil, Microsoft 365, Ciberseguridad, Gobernanza de datos, Sistema Integral de Información e Inteligencia artificial por los textos completos proporcionados por el usuario. Esta revisión sustituye las instrucciones anteriores, incluida la entrada directa «Elige tu nivel…» de la revisión del 4 de noviembre. Conserva el cambio pendiente de la categoría visible a «Inteligencia artificial» y su contenido de bienestar.

Los textos de presentación están en `surveyInstructions.ts`. `SurveyIntroduction.tsx` los muestra una sola vez al inicio del apartado: títulos/subtítulos, párrafos, cuatro elementos `li` dentro de `ul` y notas en párrafos separados. Sólo los fragmentos solicitados usan `strong`, con la puntuación indicada. Perfil conserva sus grupos y campos; no tiene viñetas. «Sin respuesta» es explicativo y no crea un control.

La lista utiliza sangría de 24 px y separación de 8 px entre elementos. El contenido tiene un ancho de lectura máximo de 95 caracteres tipográficos y se adapta al ancho disponible. Los estilos quedan dentro de `.cd-site .cd-survey-intro`; conserva Noto Sans, colores, cabecera compacta y pie. No se añaden saltos de página, subrayados ni alturas artificiales.

No hay nueva edición del instrumento ni cambios en los JSON versionados, identificadores, herramientas, actividades, respuestas, fórmulas, validaciones, consentimientos, informes o históricos. No cambia la coordinación del guardado, auditoría, representación, sesión o navegación. No se requiere migración, configuración o permiso nuevo.

## Archivos modificados

- `ClientApp/resources/js/competencias/surveyInstructions.ts`: textos completos; conserva los helpers de captura existentes.
- `ClientApp/resources/js/competencias/SurveyIntroduction.tsx`: presentación común de los seis apartados.
- `ClientApp/resources/js/competencias/Survey.tsx`: sustituye las introducciones anteriores sin cambiar campos, controles ni manejadores de captura.
- `ClientApp/resources/css/competencias-digitales.css`: presentación aislada de párrafos y listas.
- `ClientApp/tests/frontend/digital-category-initial.test.mjs`: actualiza las expectativas de los textos y conserva las pruebas de Inicial masivo.
- `ClientApp/tests/browser/digital-autosave-ui.mjs` y `survey-instructions-checks.mjs`: verifican los seis textos en el DOM, su estructura y la captura existente.
- README, ESTADO_MIGRACION, GUIA y este documento/evidencias. Los cambios locales anteriores se conservaron.

## Verificación

| Comprobación | Resultado |
|---|---|
| TypeScript (`types:check`) | Correcto |
| Frontend de producción (`build`, Vite 8.3.2) | Correcto |
| Pruebas frontend existentes | 105/105 |
| Navegador Edge, React y bundles reales | 36/36, cero errores JavaScript |
| Seis apartados a 1440 y 390 px | Texto completo literal, una introducción por apartado, listas semánticas, negritas exactas, notas fuera de listas y sin instrucciones anteriores duplicadas |
| Captura existente | Autoguardado lento, foco, selecciones, recarga, suspensión tras error, recuperación manual, conflictos, bloqueo de avance incompleto y acción masiva correctos |

Las comprobaciones del navegador usan identidad y transporte HTTP sintéticos con el fixture existente; las versiones, respuestas y confirmaciones manuales se conservan en memoria. Se verifica ausencia de desbordamiento horizontal y las capturas se inspeccionaron en escritorio y móvil. No se inició ASP.NET ni se ejecutaron pruebas sobre bases institucionales. El recorrido confirma ausencia de eventos manuales en autoguardados y una confirmación por Guardar avance en el transporte simulado; no acredita recepción real en Nexo.

Comandos desde la raíz, con Node/npm existentes en PATH:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
Push-Location ClientApp
$env:TDV2_VISUAL_ARTIFACTS = '..\.artifacts\instrucciones-20261105'
node tests/browser/digital-autosave-ui.mjs
Remove-Item Env:TDV2_VISUAL_ARTIFACTS
Pop-Location
```

No se ejecutaron migraciones, reinicios, operaciones institucionales, push ni despliegue. No hay pendientes operativos nuevos para estos textos.

## Evidencias

[Resultado completo del recorrido y mediciones](evidencia/instrucciones-20261105/report.json). Capturas del inicio de cada apartado; el contenido largo continúa mediante el desplazamiento natural de la página:

| Apartado | Escritorio | Móvil |
|---|---|---|
| Perfil | [1440 px](evidencia/instrucciones-20261105/instrucciones-perfil-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-perfil-390.png) |
| Microsoft 365 | [1440 px](evidencia/instrucciones-20261105/instrucciones-m365-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-m365-390.png) |
| Ciberseguridad | [1440 px](evidencia/instrucciones-20261105/instrucciones-ciber-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-ciber-390.png) |
| Gobernanza de datos | [1440 px](evidencia/instrucciones-20261105/instrucciones-datos-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-datos-390.png) |
| Sistema Integral de Información | [1440 px](evidencia/instrucciones-20261105/instrucciones-sii-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-sii-390.png) |
| Inteligencia artificial | [1440 px](evidencia/instrucciones-20261105/instrucciones-ia-1440.png) | [390 px](evidencia/instrucciones-20261105/instrucciones-ia-390.png) |
