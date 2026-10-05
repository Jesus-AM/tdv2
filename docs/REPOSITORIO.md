# Repositorio de código

Destino: https://github.com/Jesus-AM/tdv2. Rama principal: `main`. La visibilidad comprobada el 2026-10-05 es **pública**; no se cambió. El remoto estaba vacío y se conserva el historial local previo.

## Contenido compartido

Se versionan la solución y los proyectos, ASP.NET/React, migraciones EF Core, pruebas sintéticas, documentación y evidencias revisadas, `ClientApp/package-lock.json`, `.config/dotnet-tools.json`, las opciones generales de `appsettings` y `ClientApp/.vscode/launch.json` para F5. Configuración y ejecución: [README](../README.md).

`.gitignore` excluye dependencias descargadas, compilaciones, cachés, registros, respaldos, instaladores, `.vs`, `.runtime`, `.artifacts`, User Secrets, credenciales DPAPI y certificados privados. Las exclusiones no eliminan archivos del equipo ni purgan commits anteriores. No usar `git add -f` para publicar archivos locales.

## Revisión previa del 2026-10-05

- Se revisaron los dos commits existentes (`0e12049` y `e5aa1f0`), sus 224 versiones de archivos y el contenido pendiente. No se detectaron credenciales reales, claves privadas, copias de User Secrets ni registros institucionales. Los literales de pruebas y sus capturas corresponden a datos sintéticos.
- Se ocultaron dirección y usuario del destino institucional en la documentación pendiente y siete evidencias JSON. Se conservan resultados, esquema y contadores; `publicationRedaction` identifica las evidencias editadas. `SERVIDOR_TDV2` y `USUARIO_TDV2` son marcadores, no configuración ejecutable.
- Los originales de esos doce documentos quedan exclusivamente en `.artifacts/github-private-originals`, ignorado por Git. No se reescribió el historial ni se modificaron las credenciales locales.
- Esta preparación no ejecuta migraciones, pruebas contra bases ni despliegues. Las verificaciones funcionales anteriores y los pendientes institucionales siguen en [ESTADO_MIGRACION](../ESTADO_MIGRACION.md).

Antes de nuevos commits, revisar `git diff --cached` y los archivos añadidos. No publicar diagnósticos de usuarios reales, conexiones ni respaldos. La visibilidad del repositorio no sustituye la revisión de secretos.
