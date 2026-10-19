# Roboto para el sistema interno

Se recupera la familia `Roboto, sans-serif` del tema en `a4be53f`. La configuración anterior no incluía archivos ni CSS remoto de carga: dependía de las fuentes disponibles en el sistema. En Edge de esta validación los pesos 400/700 resolvían Roboto, pero 500/600 recurrían a Arial. Los archivos locales garantizan la familia y los pesos de la interfaz sin esa dependencia.

Distribución de [Google Fonts, ofl/roboto](https://github.com/google/fonts/tree/main/ofl/roboto), descargada el 2026-10-16 mediante el conector de GitHub. Binarios originales TTF variables, sin transformación ni subconjuntos; eje de peso 100–900, ancho normal por defecto. Licencia SIL OFL 1.1 original adjunta. No se carga un CDN en ejecución. `font-display: swap`, respaldo `sans-serif`; portal y quiz conservan sus fuentes Inter y no importan este CSS.

| Archivo local | Original | Git blob SHA |
|---|---|---|
| RobotoVariable.ttf | Roboto[wdth,wght].ttf | 5522a368d9072fd88c299916e61fcff369949061 |
| RobotoVariable-Italic.ttf | Roboto-Italic[wdth,wght].ttf | a122c13964daac37f1186a22603a5180d2dba5d6 |
| OFL.txt | OFL.txt | 9c48e05a24fa2267e207ecf17560291b5621c197 |

Se publican dentro de `wwwroot`, con precarga sólo de la redonda. MUI y los controles nativos comparten la misma familia; no se alteran los logos.
