# Encuesta: instrucciones iniciales y controles breves — 2026-10-29

Corrección puntual posterior a [población e instrucciones](POBLACION_INSTRUCCIONES_20261029.md). Conserva todo el trabajo local previo, sin restaurar archivos completos.

## Presentación vigente

- Las cinco descripciones e instrucciones de `surveyInstructions.ts` se conservan sin cambios y aparecen sólo al inicio de cada apartado.
- Un párrafo breve en esa introducción reúne Inicial como cero válido, No aplica excluido del puntaje, Sin respuesta pendiente y el alcance de la acción masiva.
- Los controles individuales muestran exactamente **Inicial** y **No aplica**, tanto en herramientas como en rúbricas. Se conservan sus valores, manejadores, nombres de controles, actividades y contenido propio.
- Se retiran las leyendas por categoría con cantidades, aclaraciones repetidas y el mensaje de que no quedan preguntas. Se elimina su única regla CSS, ya sin referencias productivas.
- **Marcar pendientes como Inicial** sigue asociado a su categoría. El conteo interno se conserva y deshabilita el botón cuando no quedan preguntas completamente vacías. No sobrescribe No aplica, niveles, respuestas parciales ni otras categorías; las respuestas permanecen editables.

No se alteran métodos de guardado, debounce, recuperación, permisos, representación, fórmulas, versiones, informes, población, filtros, auditoría ni colores. `surveyInstructions.ts` y los archivos de backend pendientes mantienen el mismo SHA-256 que al comenzar esta corrección.

## Archivos de esta corrección

- `ClientApp/resources/js/competencias/Survey.tsx`: etiquetas y ayudas visibles.
- `ClientApp/resources/css/competencias-digitales.css`: eliminación de `.cd-category-help`; los demás ajustes pendientes permanecen.
- `ClientApp/tests/browser/digital-autosave-ui.mjs`: recorrido existente ampliado con presentación de los cinco apartados y acción masiva; admite un directorio de evidencia separado.
- `ClientApp/tests/browser/competencias-native.mjs` y `digital-population-ui.mjs`: selectores adaptados a las etiquetas breves y ausencia de leyenda. No se ejecutaron sus preparadores PostgreSQL en esta corrección.
- README, ESTADO_MIGRACION, GUIA, la guía de población y este documento.

## Verificación y límites

TypeScript, compilación Vite y las 102 pruebas frontend correctos. **34 comprobaciones de navegador correctas (17 por ancho), cero errores JS.** El navegador usa React compilado y Edge reales con el servidor HTTP sintético existente `systemFixture`; respuestas, versiones, fallos y recibos viven en memoria. No es una nueva prueba de ASP.NET, PostgreSQL ni entrega real a Nexo. Las verificaciones de backend de la entrega anterior permanecen documentadas separadamente. [Resultado del recorrido y mediciones de interacción](evidencia/20261029-presentacion-encuesta/report.json).

El recorrido comprueba los cinco apartados a 1440 y 390 px: introducción única, etiquetas exactas, ausencia de leyendas, conservación de actividades y falta de desbordamiento. Incluye acción masiva limitada a vacíos de su categoría, NA/parcial conservados, deshabilitado sin texto adicional, recarga y edición posterior. Reutiliza además los casos existentes de autoguardado lento, foco/selección/desplazamiento, último cambio, suspensión tras fallo, confirmación perdida, conflicto, recuperación manual y validación secuencial. El transporte sintético verifica un PUT para la acción masiva y una confirmación manual explícita; no acredita recepción institucional del evento.

Capturas con identidad y respuestas sintéticas, inspeccionadas visualmente:

| Vista | Escritorio | Móvil |
| --- | --- | --- |
| Instrucciones al inicio de Microsoft 365 | [1440 px](evidencia/20261029-presentacion-encuesta/instrucciones-m365-1440.png) | [390 px](evidencia/20261029-presentacion-encuesta/instrucciones-m365-390.png) |
| Categorías y rúbricas de Ciberseguridad | [1440 px](evidencia/20261029-presentacion-encuesta/categoria-ciber-1440.png) | [390 px](evidencia/20261029-presentacion-encuesta/categoria-ciber-390.png) |
| Fundamentos del uso de IA | [1440 px](evidencia/20261029-presentacion-encuesta/categoria-ia-1440.png) | [390 px](evidencia/20261029-presentacion-encuesta/categoria-ia-390.png) |

Las capturas de los otros apartados quedan en `.artifacts/survey-presentation-20261029/browser/after`. Los conteos de progreso de este fixture son sintéticos y no constituyen una nueva validación de la fórmula del backend.

Comandos desde `ClientApp` (Node disponible en PATH):

```powershell
npm run types:check
npm run test:forms
npm run build
$env:TDV2_VISUAL_ARTIFACTS = '../.artifacts/survey-presentation-20261029/browser'
node tests/browser/digital-autosave-ui.mjs
```

No se crearon ni aplicaron migraciones, se conectó a bases institucionales, cambió configuración, hizo push o desplegó. No se inició ni detuvo el servidor Vite del usuario.
