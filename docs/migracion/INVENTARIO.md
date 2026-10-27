# Actualización vigente 2026-10-27 — Competencias digitales UACJ

**Identidad visual posterior vigente:** [Guía e inventario 2026-10-27](IDENTIDAD_VISUAL_20261027.md). Noto Sans local, tokens institucionales comunes, nueva fotografía local y pies ordenados con DGTI en Competencias. Las referencias posteriores a Inter/Roboto o pie CD de cuatro firmas son históricas. Sin modificaciones de persistencia ni permisos.

**Revisión posterior vigente:** Perfil agrupado, pies al fondo en flujo normal, portal Noto Sans local y contacto/textos actualizados. Tres PDF públicos originales; encuesta principal y Juego Microsoft 365/Iniciar test separados. Pie público con UACJ/DGTI y cuatro firmas; Competencias conserva cuatro. Sin nuevas migraciones ni reinicios ejecutados. [Inventario de archivos y comprobaciones](PORTAL_Y_PERFIL_20261027.md). Las descripciones del botón único e Inter público siguientes son históricas.

**Implementado:** subsitio con cinco módulos, autorización independiente de Procedimientos, contexto/migración PostgreSQL propios, catálogos locales compartidos sin duplicación, perfil institucional validado, encuesta única e informe definitivo. Privacidad por versión, evaluación central, agregados y exportaciones por alcance, metas directas sin doble conteo y representación Nexo solo de consulta. Iniciar test dirige al subsitio; el resto del portal se conserva. Los recursos del HTML están integrados y el original se mantiene como referencia documental fuera de `wwwroot`.

**Probado y evidencia:** resultados finales, comandos y límites en la entrada 2026-10-27 de [ESTADO_MIGRACION](../../ESTADO_MIGRACION.md). Se distinguen PostgreSQL/HTTP/navegador reales de las fuentes Microsoft/Nexo sintéticas.

**Pendiente:** administración/provisión en Nexo, configuración de conexiones reales, migración explícita de la base nueva y aceptación institucional del aviso, catálogo y contenido. Nada de ello se ejecuta automáticamente. [Inventario de archivos, separación de bases, permisos, fórmulas y lista exacta para el operador](../competencias-digitales/GUIA.md).

# Actualización 2026-10-17 — Acabado del portal público

Ajuste posterior: enlaces públicos sin subrayados en todos sus estados, resaltado suave y contorno de teclado conservados. Build y suite pública antes/después correctos en cuatro ventanas; clics/Enter, foco y móvil emulado revisados. Evidencia `.artifacts/public-links-20261017/`; sin cambios internos ni en el quiz.

Cambios sólo en estilos de Razor y nuevo JS de medición de cabecera: azul institucional más presente, modelo claro, iconos/etiquetas azul–blanco, ruta y controles legibles, FAQ abierto claro y anclas nativas sin ocultar títulos. Contenido, geometría, Inter, logos, quiz y sistema interno conservados. Tipos/Release, comparación de 316 elementos en cuatro ventanas, 83 llegadas, nueve grupos del quiz y 16 recursos HTTP correctos. Fotografía externa inaccesible: pendiente inspección con foto cargada; logos/fuentes locales sí cargan. Sin datos, contratos ni migraciones. [Estado, archivos, evidencia y límites](AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-del-portal--2026-10-17).

# Actualización 2026-10-16 — Restauración de Procedimientos y Colaboradores

Se retira el último ajuste visual de ambas vistas mediante hunks del diff: distribución, etiquetas, espacios, botones y clave secundaria originales. Colaboradores conserva exclusivamente la eliminación de Origen y sus textos; datos y reglas de ambos roles intactos. Se mantiene Roboto local en el sistema e Inter en portal/quiz, sin tocar la captura ni sus columnas. Tipos/Vite, 94 pruebas frontend, 116 de dominio/HTTP y 16 grupos de navegador correctos. Capturas nuevas en `.artifacts/views-revert-20261016/`; las de `.artifacts/views-20261016/` documentan el ajuste anterior. Sin cambios de base, contratos ni migraciones. [Estado vigente, fuentes, archivos, verificaciones y límites](VISTAS_PROCEDIMIENTOS_COLABORADORES.md#restauración-visual-vigente).

# Entrega anterior 2026-10-16 — Quiz Microsoft 365 e Inter local

La unificación interna en Inter descrita en esta evidencia queda sustituida por la recuperación de Roboto; el portal y el quiz se conservan.

Diagnóstico público abre el HTML original en diálogo/iframe del mismo sitio, sin login ni persistencia; conserva preguntas, lógica, diseño y textos. Inter oficial variable 100–900 con OFL y respaldo visible en `wwwroot/fonts/inter` para portal/MUI/quiz. Sin CDN de fuentes ni cambios en seguridad, columnas, grupos, captura o migraciones. Tipos/Vite/Release, 94 frontend, once recursos HTTP reales de ASP.NET/TestServer, nueve grupos del quiz y 33 regresiones internas correctos; paquete construido y probado con sus estáticos en aislamiento. [Archivos, alcance, antes/después, límites y reproducción](QUIZ_MICROSOFT_365_E_INTER.md).

# Actualización 2026-10-16 — Reversión visual de columnas

Restaurada la apariencia anterior a la delimitación: fondos, sombras suaves, anchos, espaciado y grupos de una celda completa. Se conservan Edición/Acciones fijas con la excepción móvil previa para Edición, la acción de alta dentro del grupo y las acciones por registro. Portada, jerarquía y alineación UACJ sin cambios. Sólo tres archivos productivos internos, ajuste de pruebas y documentación; sin contratos, datos ni migraciones. [Estado vigente, verificación y alcance](COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente).

# Entrega anterior 2026-10-16 — Columnas fijas, grupos y alineación UACJ

La delimitación interna siguiente fue revertida; su evidencia se conserva como histórica. La alineación del logotipo continúa vigente.

Separadores de 1 px continuos, fondos opacos, filas de grupo con laterales vacíos y franja/alta sólo centrales. Edición y Acciones fijas también en móvil; controles adaptados sin reducir fuente. UACJ sólo se desplaza horizontalmente para compensar el margen transparente original. Tipos/Vite/Release, 94 frontend, 25 comprobaciones nuevas, 24 regresiones internas y 9 de selectores correctos; público comparado en seis tamaños, sólo cambia x del logotipo. Sin contratos, datos o migraciones. Evidencia `.artifacts/fixed-columns-logo-20261016/`; transportes internos simulados y Razor real en host de pruebas sin base. [Estado vigente, archivos, capturas, límites y comandos](COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md). Sustituye la decisión visual anterior de liberar Edición en móvil; conserva las demás mejoras.

# Actualización 2026-10-16 — Jerarquía institucional del bloque principal

Sólo CSS de `.hero`: UACJ 560 px proporcional/adaptable, Dirección 16–18 px/600 inmediatamente debajo, proyecto h1 24–38 px/600 con separación mayor. Textos blancos, archivos/URLs, contenido, fondo y resto de la portada intactos; sin cambios internos, contratos o migraciones. Build Release y Edge antes/después en seis tamaños correctos; 297 elementos exteriores conservan diseño. Evidencia nueva `.artifacts/hero-hierarchy-20261016/`. Recursos externos inaccesibles en ambos pases; desbordamiento previo exterior de 13 px a 1024 px documentado fuera del alcance. [Medidas, pruebas y límites vigentes](AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-jerarquía-institucional-del-bloque-principal). Las verificaciones siguientes pertenecen a entregas anteriores; la restricción de sólo colores queda sustituida exclusivamente dentro del bloque principal.

# Actualización 2026-10-16 — Paleta primaria y blancos en la portada

Releído el manual local actual de seis páginas. La vista pública utiliza principalmente #213455/#DADADA/#FFFFFF y únicamente #174187 para hover; los cuatro textos principales son blancos opacos, navegación y El proyecto permanecen blancos. Sólo se modifican colores de `Views/Home/Index.cshtml`; los ajustes internos existentes pasan nuevamente 24 recorridos más nueve de selectores. Tipos/Vite/Release, 94 frontend y 116 dominio/HTTP correctos; 306 elementos sin cambios geométricos en tres tamaños, contraste público mínimo 8.05:1. Evidencia nueva `.artifacts/visual-primary-20261016/`. Sin migraciones ni bases; externos, dispositivos físicos e institucional siguen pendientes. [Estado, archivos, evidencia y límites](AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

# Actualización 2026-10-16 — Presentación interna y paleta pública

Presencia con ocho colores funcionales y nombre visible, catálogo con títulos 700, resumen/búsqueda sin superposición y Acciones fija a la derecha. Portada sólo recoloreada con el manual local: geometría y contenido idénticos en tres tamaños, sin amarillo. Sin contratos/esquema/migraciones ejecutadas; `RemoveUsedMedia` permanece intacta. Tipos/Vite/Release, 94 frontend, 116 dominio/HTTP, 24 recorridos internos y 9 de selectores. Transportes internos simulados; Razor público real en host de pruebas sin base. [Capturas, archivos, contrastes, límites y comandos](AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

# Actualización 2026-10-16 — Herramientas agrupadas y revisión de textos

**Actualización vigente 2026-10-16 (catálogo):** herramientas opcionales por procedimiento, nuevas familias Microsoft 365/Adobe, opciones históricas conservadas y duplicados nuevos rechazados. Medios utilizados y Otro medio están retirados, incluida su persistencia específica. Requiere `20261016190000_RemoveUsedMedia`; el resto de los envíos permanece intacto. [Catálogo vigente, limpieza exacta, respaldo y actualización Windows/Ubuntu](CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md). Las verificaciones y decisiones anteriores de este documento son evidencia fechada, no instrucciones para reinstalar el catálogo ni Medios.

Sistemas agrupa por la relación estable con el procedimiento dentro de la tabla existente, con alta explícita por grupo y grupo sin asociación. No reordena el JSON ni exige cobertura. Filas React estables, conservación de foco/desplazamiento, mismos controles y permisos. Revisados los textos de todas las secciones; etiquetas históricas conocidas cambian sólo su presentación. Sin migración. [Archivos, pruebas nativas/simuladas y límites](SISTEMAS_HERRAMIENTAS.md#agrupación-dentro-de-la-tabla-y-redacción--2026-10-16).

# Actualización 2026-10-16 — Terminología y herramientas opcionales

Portal y Procedimientos usan terminología uniforme y correcciones de puntuación/concordancia sin modificar datos capturados ni nombres oficiales. Backend compartido permite cero herramientas por procedimiento; las filas iniciadas mantienen requisitos, condicionales y enlaces a pendientes. Nuevos formatos sin filas automáticas; formatos existentes y enviados intactos. Sin migración. La cobertura obligatoria descrita en entradas anteriores queda sustituida. [Fórmula, compatibilidad y operación](COBERTURA_Y_GUARDADO.md); [resultados de esta entrega](../../ESTADO_MIGRACION.md#2026-10-16--terminología-y-herramientas-opcionales).

# Actualización 2026-10-15 — Aplicaciones desde listas versionadas

Origen exclusivo Excel (207 web/26 móviles), manifiesto versionado, importación PostgreSQL explícita e idempotente, correspondencias exactas y UUID estables. Migración **20261015165555_LocalApplicationLists**, sin modificar anteriores ni formatos. SQL Server sólo UR/módulos; auditoría histórica retenida, ILDA independiente. Selectores y validación existentes conservados. [Implementación, pruebas y operación](APLICACIONES_LISTAS.md); [evidencia vigente](../../ESTADO_MIGRACION.md). Las entradas anteriores de aplicaciones SQL Server describen una implementación retirada.

# Actualización 2026-10-14 — Aplicaciones móviles y web

Entidades, mapeo y **20261014180408_SiiApplicationsCatalog** completados con tipos/nulos confirmados; IDs originales sin generación, padre web sin FK. Sincronización SQL Server de cuatro catálogos con resultados/transacciones independientes, ILDA aparte. Multiselección compacta, JSON discriminado e históricos; un requisito condicional. TypeScript/Vite/.NET, 114 dominio/HTTP con dobles, 88 frontend, 52 casos de sincronización y 12 de edición/concurrencia PostgreSQL, 59 comprobaciones EF y 11 recorridos Edge con ASP.NET/PostgreSQL correctos; 12 grupos UI complementarios con transporte simulado. La migración se aplica sólo en aislamiento; pendiente actualización del operador y validación institucional. [Archivos, evidencia, compatibilidad y comandos](APLICACIONES_SQL_SERVER.md).

# Actualización 2026-10-14 — Sistemas compacto y pendientes en revisión

Retirado el bloque de cobertura de Sistemas; sus pendientes conservan Ir al campo/Agregar registro en Revisar y enviar. Selector Módulos compacto: placeholder, un nombre completo o cantidad; búsqueda/casillas, reservas y autoguardado conservados. Sin cambios de backend ni migración. Verificados tipos/Vite, 88 pruebas frontend, 46 grupos Edge simulados y 12 recorridos con ASP.NET/PostgreSQL aislado. Dispositivos físicos/lector de pantalla e institucional pendientes. [Implementación, archivos y evidencia](SISTEMAS_HERRAMIENTAS.md#resumen-compacto-y-pendientes--2026-10-14).

# Actualización 2026-10-14 — Cobertura y guardado

Cobertura por procedimiento aplicable, herramientas completas, pendientes navegables y rechazo del envío API. Retiro informa cantidad y conserva respuestas para reasignación. Versión JSON compatible, sin backfill ni migración. Verificados 108 casos de dominio/HTTP, 88 frontend, 10 casos nativos distintos y 5 recorridos Edge con PostgreSQL real. [Implementación, fórmula, archivos y límites](COBERTURA_Y_GUARDADO.md). Los cambios locales anteriores se conservan.

# Actualización 2026-10-14 — Módulos múltiples por herramienta

Selección múltiple SIIv2 con casillas, eliminación individual y nombres completos; colección estructurada de IDs/instantáneas en el JSON del formato, sin nueva migración. Lectura compatible con la asociación única anterior y validación transaccional del catálogo local. Un requisito condicional por fila para avance/revisión, sin ponderar el número de módulos. [Implementación, pruebas y límites](SISTEMAS_HERRAMIENTAS.md#selección-múltiple-de-módulos--2026-10-14).

# Actualización 2026-10-13 — Catálogo histórico y desplazamiento de captura

CAST retirado de selecciones nuevas, con conservación histórica y rechazo del backend; Procedimiento sustituye el nombre del campo `tramite` sin cambiar persistencia. Ambas tablas comparten scroll en dos ejes, altura adaptable y encabezados fijos, conservando Agregar fuera. Navegación a pendientes y filas nuevas revela/enfoca el control. Tipos/Vite, 104 dominio/HTTP con dobles, 88 frontend y 52 grupos Edge con transporte simulado; no bases ni pruebas institucionales. [Implementado, comprobaciones, archivos y límites](SISTEMAS_HERRAMIENTAS.md#catálogo-histórico-procedimiento-y-desplazamiento-propio--2026-10-13).

# Actualización 2026-10-13 — Procedimiento completo

Procedimiento muestra código y nombre completos, altura automática y ancho compacto; sin ayuda por fila ni tooltip. El nombre se mantiene durante la búsqueda para conservar la apertura desde la primera interacción aun con filas largas. Tipos/Vite, 88 frontend y 40 grupos Edge correctos, con transporte simulado. PO-08/09 probados con nombres sintéticos; registros reales y dispositivos físicos pendientes. Sin acceso a bases. [Implementado, archivos, comandos y límites](SISTEMAS_HERRAMIENTAS.md#procedimiento-completo-y-sin-ayuda-por-registro--2026-10-13).

# Actualización 2026-10-13 — Apertura de selectores y reservas

Módulo y Procedimiento abren desde la primera interacción, con carga mientras se confirma la reserva y selección protegida. Se invalidan aperturas tardías y adquisiciones en cola de filas abandonadas; se conservan foco, nodos, valores, autoguardado y presentación. Tipos/Vite, 88 frontend y 22 recorridos Edge correctos, con transporte simulado y sin bases. [Causas reproducidas, archivos, pruebas y límites](SISTEMAS_HERRAMIENTAS.md#apertura-y-reserva-de-selectores--2026-10-13).

# Actualización 2026-10-13 — Tipografía y nombre completo del módulo

Dependientes con escala uniforme de 13 px y espacio para etiqueta flotante; módulo seleccionado completo/multilínea, buscador conservado y botones separados del texto. Tipos/Vite, 87 pruebas frontend y 13 recorridos Edge correctos; estados vacío/foco/completo/error, teclado, recarga sintética y móvil revisados. Sin bases. [Implementado, comprobaciones, límites y archivos](SISTEMAS_HERRAMIENTAS.md#tipografía-y-módulo-completo--2026-10-13).

# Actualización 2026-10-13 — Agrupación de campos condicionales

Los tres detalles de Sistemas comparten sangría moderada y línea azul suave dentro de su celda, sin cambiar condiciones ni lógica. Tipos/Vite, 87 pruebas frontend y 12 recorridos de navegador con transporte simulado correctos; foco, Tab, errores locales, nombres largos y escritorio/móvil revisados. Sin bases. [Archivos, implementación y límites](SISTEMAS_HERRAMIENTAS.md#campos-condicionales--2026-10-13).

# Actualización 2026-10-12 — Ajustes de captura

Acciones de Sistemas a 112 px con foco completo; módulos sin ID añadido al nombre, conservando la referencia interna; Egresados válido en frontend/backend; Áreas de oportunidad conserva `fallas`. Verificados tipos/compilaciones, 102 casos dominio/HTTP, 87 frontend y 15 recorridos de navegador con transporte simulado. Sin migraciones ni bases. [Cambios, archivos y límites](SISTEMAS_HERRAMIENTAS.md#ajustes-de-captura--2026-10-12).

# Actualización 2026-10-11 — Etiqueta de origen en Colaboradores

Presentación central como **Agregado por Administrador**, sin revelar el sistema de origen en etiquetas o ayudas de la lista. Origen interno, responsables, permisos y acciones conservados. TypeScript, Vite, ASP.NET Release y 4 recorridos de navegador con transporte simulado correctos; sin bases. [Detalle y límites](COLABORADORES_VISIBILIDAD_ACCIONES.md).

# Actualización 2026-10-10 — Colaboradores por asignación y acciones de captura

Guardar secundario sin enviar; Enviar formato azul y confirmado con la versión vigente. Colaboradores integra asignaciones centrales y delegadas por alcance/jerarquía, con permisos de retiro independientes. Pruebas de acceso centra selección/resumen en columnas equilibradas. Verificados 101 casos dominio/HTTP, 87 frontend, 4 recorridos nuevos de acciones/colaboraciones, 4 de acceso, 9 de interfaz y 10 de Sistemas; dobles y transporte simulado, sin bases. [Reglas, limitación del alcance central, archivos y comprobaciones](COLABORADORES_VISIBILIDAD_ACCIONES.md).

# Actualización 2026-10-10 — Avance y contextos de prueba

Backend centraliza requisitos aplicables para avance y revisión; agregados usan formatos únicos, porcentajes sin redondeo hacia 100 y datos confirmados. La interfaz retira la explicación del conjunto y las referencias visibles a primera etapa, conservando su alcance y los cambios previos de distribución. SQL Server reúne UR y módulos en una sola acción real, con ILDA aparte y resultados por catálogo. Pruebas de acceso separa modalidades, selección, contexto activo y cuenta original sin ampliar autoridad. Verificados 87 casos frontend, 87 dominio/HTTP, 9 recorridos de interfaz, 4 de acceso, 10 de Sistemas y 3 grupos de Inicio, con límites de transporte simulado y sin bases. [Implementación, fórmula, archivos y pendientes](AVANCE_Y_PRUEBAS_ACCESO.md).

# Actualización 2026-10-10 — Distribución operativa (ajuste inicial)

Inicio mantiene indicadores y árbol de áreas, con pestañas/filtros compactos y preferencias de vista conservadas. Sistemas usa un selector de Procedimiento limitado y buscable, sin modificar asociaciones o reglas de disponibilidad. Revisión presenta **Ir al campo** y **Enviar formato**; Sincronizaciones distingue resultado, último éxito y acción por UR, módulos SII e ILDA. Sin cambios de backend, dependencias o esquema. Comprobaciones frontend y dominio/HTTP con dobles; navegador con transporte simulado, sin bases. [Implementado, pruebas, límites y archivos](INTERFAZ_OPERATIVA.md).

# Actualización 2026-10-09 — Revisar y enviar por etapa

Pestaña independiente después de Sistemas, accesible con pendientes y sin avance adicional; resumen y envío de primera etapa con reglas del servidor, autoguardado y control de concurrencia. `20261009171656_StageSubmissions` separa estados/instantáneas por etapa y conserva envíos globales anteriores sin atribución inferida. Segunda etapa deshabilitada. Verificados 80 casos frontend, 76 dominio/HTTP, 56 regresiones nativas y 10 casos de etapas, 57 comprobaciones EF y 18 recorridos de navegador real con identidades sintéticas. Aplicación institucional y dispositivos físicos pendientes. [Contrato y comandos](REVISAR_ENVIO_ETAPAS.md), [resultados y límites](../../ESTADO_MIGRACION.md).

# Actualización 2026-10-09 — Procesamiento manual desde ASP.NET

El servicio hospedado `ManualSyncWorker` atiende solicitudes manuales persistidas mediante el coordinador existente; POST 202, señal tras commit y comprobación cada dos segundos. No crea horarios ni altera su habilitación; el CLI conserva programación, exclusión y recuperación compartidas. Interfaz con inicio/ejecución confirmados, actualización automática, diagnóstico y prevención de solicitudes duplicadas. Sin migraciones ni nuevas claves de despliegue. Verificados 44 casos PostgreSQL (nueve nuevos de servicio web), seis recorridos de navegador con hosted service real y 13 de regresión con ciclos explícitos; 76 pruebas de dominio/HTTP. Fuentes institucionales y Ubuntu pendientes. [Operación y límites](SINCRONIZACIONES.md), [evidencia y comandos](../../ESTADO_MIGRACION.md).

# Actualización 2026-10-09 — Portal público

La vista `Views/Home/Index.cshtml` retira Proyectos estratégicos, Indicadores de avance y referencias exclusivas. Usa el PNG blanco UACJ existente, íntegro, a 380 px/adaptable, sobre el texto de la Dirección y sin raya decorativa externa. Footer con únicamente Universidad Autónoma de Ciudad Juárez. Conserva logo Transformación Digital, fuentes configuradas y acceso de sesión. ASP.NET/Edge verificados en escritorio y dos anchos móviles, ruta/bytes de imagen y manifiesto de publicación; fuentes/fondo externos pendientes de carga por restricciones del entorno. [Implementado, probado y límites](../../ESTADO_MIGRACION.md). Sin cambios en área autenticada, datos, permisos ni migraciones.

# Actualización 2026-10-09 — Sistemas y módulos SII

Ajuste posterior: entrega únicamente Contexto/Identificación/Sistemas, reglas de elegibilidad compartidas en backend, presencia en la primera columna fija Edición y foco MUI único en tablas. Edición sustituye la posición anterior en Acciones; Eliminar queda arriba y separado. Desplazar conserva contexto/reserva y los textos muestran de dos a cuatro líneas con scroll interno. [Interacción vigente y evidencia](DESPLAZAMIENTO_TABLAS.md). Diagnóstico local de tabla/migración/esquema para `42P01`; ninguna migración nueva. [Contrato vigente y operación](SISTEMAS_HERRAMIENTAS.md), [edición automática](EDICION_AUTOMATICA.md). Este alcance de envío sustituye la restricción anterior de sólo borrador para la primera etapa; conserva la política de quién puede enviar y la inmutabilidad posterior.

Nueva réplica EF local `sii_modulos` (`id_modulo`, `desc_modulo`, `presente`, `sincronizado_en`), migración `20261009080533_SiiModulesCatalog`. La fuente adicional es exclusivamente `DesarrolloSII.sii.MODULOS_SII`, dos columnas, misma `ConnectionStrings:Sii`. SII manual/automática incluye UR y módulos; ILDA es independiente. Nuevo GET autorizado `/formatos/{ur}/modulos-sii`, sólo PostgreSQL local.

Captura con opciones únicas por fila, detalles JSON separados, referencias/instantáneas de módulo, códigos/orígenes conservados y ayudas compartidas. Sin cambios de navegación, Nexo, permisos ni formatos enviados. PostgreSQL aislado: 34/34 sincronizaciones, 52/52 comprobaciones EF y 7/7 recorridos de navegador del formulario; SII real pendiente. [Implementado, comprobaciones y pendientes institucionales](SISTEMAS_HERRAMIENTAS.md). El inventario histórico siguiente se conserva como referencia; no sustituye este contrato vigente.

# Inventario funcional y contratos

## Actualización ASP.NET vigente — 2026-10-06

- **Identificación, actualización posterior 2026-10-08:** tabla con tamaños compactos originales del tema y encabezados en negritas; siete categorías definitivas, instrucciones/ayudas y retiro sin renovaciones posteriores. `CurrentRecipients` limpia una vez únicamente Otro/Externo/detalle en borradores actuales y versiona/audita; conserva enviados e históricos. Recuperar la densidad anterior no revierte funciones ni migraciones. [Contrato, causa y evidencia](IDENTIFICACION_TABLA.md).

- **Primera etapa 2026-10-08:** Procedimientos Institucionales admite clave nueva y alias Nexo; tres secciones por defecto, posteriores sólo para administrador efectivo, restricciones del servidor y avance parcial separado. Ayudas, nuevas etiquetas, Externo/Otro, Prioridad compacta y Procedimiento V/A confirmado. Retiro persistente de inventario mediante `FormIldaExclusions` sin alterar réplica ni históricos. [Reglas, actualización y verificación](PRIMERA_ETAPA.md). Reemplaza las descripciones de alcance/etiquetas de entregas anteriores, no sus datos ni controles de concurrencia.

- **Edición automática 2026-10-08:** se retira la resolución manual; códigos confirmados por servidor, validación localizada, guardados independientes y reintentos internos limitados con recibos idempotentes. Eliminaciones relacionadas atómicas y protección de primeras teclas/pegado/Tab. Sin migración. [Reglas y evidencia](EDICION_AUTOMATICA.md).

- **Usuarios que atiende (2026-10-07):** Select múltiple MUI con cinco opciones y casillas; JSON estructurado sin duplicados, vacío pendiente de envío. La migración de datos `StructuredUsersServed` limpia una sola vez el campo anterior en borradores, conserva enviados, prioridades y colecciones nuevas, y actualiza versiones/auditoría. Prioridad muestra el indicativo sólo fuera del menú. [Actualización y evidencia](USUARIOS_ATENDIDOS.md).

- **Participación configurable, rendimiento y presencia:** nueva pantalla `/configuracion/procesos`, módulo hijo de `configuracion` y administrador propio. Política de niveles/tipos centralizada, vista previa con impacto, versiones, auditoría y bloqueo transaccional frente a captura/publicación; ninguna exclusión borra historia. Cuatro migraciones y 18 entidades. Editor por bloque con sólo pestaña activa; Inicio consulta por conjuntos; colores coordinados en PostgreSQL e iniciales por identidad efectiva. [Contrato, instalación y verificación](PARTICIPACION_RENDIMIENTO_PRESENCIA.md). Las referencias a niveles 2/3 y exclusión N en las entregas anteriores describen ahora los valores iniciales; la delegación conserva sus restricciones fijas.

- **Colaboradores centrales:** `nexo_concesiones.origen=central` explícito y rol efectivo coincidente permiten local por adscripción o dependencias por rama nivel 2, sin alta local. La vía `aplicacion` mantiene vínculo, empleado, adscripción, otorgante y revocación local; no hay fallback por rol. Cálculo común a consulta/captura/SignalR para identidad efectiva, sin ampliar envío ni facultades administrativas. [Contrato, pruebas y publicación requerida](COLABORADORES_CENTRALES.md).

- **Eliminación directa:** icono disponible sin foco ni reserva previa; confirmación MUI reserva automáticamente registro y relaciones. Valida contenido/versiones por ID, espera guardado, libera después y conserva recuperación ante fallos. Reserva ajena bloquea con nombre o mensaje de otra pestaña; liberar rehabilita sin recargar. La confirmación de reserva usa fecha posterior al commit para descartar lecturas anteriores en vuelo. [Implementación y pruebas](ELIMINACION_DIRECTA.md).

- **Presentación de Inicio:** logo 20 % mayor y 32 px hasta el primer módulo en escritorio, conservando barra blanca compacta y jerarquía. Encabezado 26/24 px semibold, descripción 14 px, acciones adaptables; Colaboradores azul y Actualizar de texto con indicador de carga y foco visible. Verificación visual sintética en siete anchos documentada en [estado](../../ESTADO_MIGRACION.md), sin cambios de lógica/backend.

- **Reservas y SignalR:** intervalo de 15 segundos sin cancelar lecturas; terminación esperada con reautorización y limpieza. Entrada directa al campo/casilla/selector reserva automáticamente, sin botones para iniciar o terminar. Adquirir devuelve contenido/versión confirmados; React bloquea cambios al preparar, vencer o perder reserva. Estado único por fila ocupada, foco lógico para portales, guardado antes de liberar automáticamente y actualización de las demás sesiones sin recarga. Recuperación contextual explícita ante fallos reales. Sin cambios de esquema. [Contrato y pruebas con dos sesiones](RESERVAS_SIGNALR.md).

- **Implementado:** `responsable_ur_supervisor` funciona sin rol adicional, edita su ámbito responsable y consulta las demás áreas. El alias `responsable_ur_institucional` preserva la transición; no eleva el genérico `supervisor`. Nivel 2 edita su rama; nivel 3 conserva su formato y delega sólo colaboración local en su rama. Sólo `tipo_ur=N` (sin espacios y sin distinguir mayúsculas) queda como nodo auxiliar sin ser área elegible; el tipo `0` puede ofrecer formato si es nivel 2/3 y cumple las demás reglas. Presentación/búsqueda 06000/6000 conserva identificadores de SII/Nexo/ILDA.
- **Captura y envío:** `FormEditingService`, `FormBlocks` y `FormHub` centralizan reservas PostgreSQL de 45 segundos, versiones por bloque, UUID de operación, auditoría atómica y consulta SignalR autorizada. PATCH cambia únicamente bloques reservados; PUT completo retirado con 428. Enviar exige responsable, llenado y ausencia de reservas ajenas; congela respuestas/unidad/ILDA y bloquea cambios incluso mediante trigger. Sin reapertura.
- **EF:** 18 entidades, siete relaciones locales, seis migraciones. `CollaborativeFormsAndSubmission` conserva respuestas/relaciones y completa ejercicio; `ProcessParticipationAndPresence` agrega configuración versionada y presencia; `ParticipantPhotographs` añade el vínculo opcional de reserva a identidad Microsoft verificada; `StructuredUsersServed` convierte sólo el campo autorizado de los borradores. Se ejecutan explícitamente; no se aplicaron a la base institucional en esta entrega.
- **Captura y fotografías (2026-10-07):** pendientes incrementales por bloque, reservas de filas independientes y descarte de notificaciones remotas equivalentes sin relajar autorización. Fotos protegidas/cacheadas, iniciales del objetivo si no hay identidad Microsoft verificada y tooltip accesible que conserva foco. Nuevas reglas de participación aplazadas; se mantienen las existentes. [Mediciones, migración y evidencia](CAPTURA_FOTOGRAFIAS_RENDIMIENTO.md).
- **React:** siete secciones, con sólo la activa montada; Contexto sin Datos de la sesión y UR identificada en el encabezado de página. Última sección confirmada por usuario/formato, autoguardado de un segundo, estado compacto y recuperación explícita. Select MUI 1–5 sin defecto ni conversión silenciosa; eliminación por ID con diálogo y Cancelar enfocado. Navegación superior jerárquica según Nexo: Procesos operativos y Configuración, con submódulos autorizados en desplegable. Sin Imprimir; mantiene tema/tipografías, foco, propuestas fuera de la pestaña y movimiento reducido.
- **Revisión del formato:** al final de Acuerdos, `FormReview` proporciona pendientes y enlaces a campos; el envío revalida los mismos requisitos. Datos de sesión se conservan en el JSON histórico, sin validación ni peso de avance. Los 90 puntos restantes se normalizan a 100; las lecturas de borradores recalculan sin escribir. Los enviados conservan avance e instantánea. Este ajuste no necesita cambios de esquema; muestra fecha e identidad efectiva del envío.
- **Microsoft:** ID token validado (firma, emisor, audiencia, caducidad, nonce, oid/tid), login_hint de cuenta real, logout_hint sólo del claim opcional, selección explícita mediante POST protegido. Cuenta representada nunca alimenta los hints.
- **Implementado en Nexo:** cuatro archivos de fuente con restricciones exclusivas de TDV2; esta entrega añade filtros de roles delegantes/asignables a dos de ellos. Representación hereda las comprobaciones. No se publicó configuración en bases institucionales.
- **Probado:** dominio/transporte, frontend, SQL real generado por Nexo en PostgreSQL aislado y regresión HTTP/React sintética. Los comandos y resultados finales se mantienen en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).
- **Pendiente institucional:** aplicar EF, configurar roles/módulos/delegación/representación y publicar Nexo, habilitar claim Entra y validar con identidades reales. Guías: [captura y envío](COLABORACION_ENVIO.md), [Nexo y Entra](CONFIGURACION_NEXO_ENTRA.md). No hay despliegue Ubuntu ni sincronización automática habilitada.

Rutas nuevas vigentes: `GET /formatos/{ur}/estado`, `POST /formatos/{ur}/reservas`, `/reservas/actividad`, `/reservas/liberar`, `PATCH /formatos/{ur}/bloques`, `POST /formatos/{ur}/enviar`, SignalR `/form-events` y `POST /session/use-another-account`. Las mutaciones exigen CSRF/contexto; reservas/guardado/envío comparten autorización y transacciones breves. Tablas nuevas: `formato_bloques`, `formato_operaciones`, `formato_posiciones`. Los bloques siguientes documentan la fuente y entregas anteriores, no sustituyen este contrato vigente.

Distribución vigente de Procesos operativos: indicadores y filas compactos, conteo único, filtros adaptables y cabecera conservada. [Implementación y evidencia visual](DISTRIBUCION_PROCESOS.md); sin cambios en contratos, permisos o captura.

## Inventario histórico de la fuente Laravel

Fuente: código raíz de `C:\Users\Jesus Arenas\Herd\tdv2`, inspeccionado el 2026-09-30. `fuente-manifiesto.json` fija los hashes; `EVIDENCIA_FUENTE.md` enumera rutas, columnas/índices declarados y todos los nombres de pruebas encontrados. No se consultó el esquema ni los datos desplegados.

Conteo estático: **27 rutas HTTP propias, 27 tablas creadas declarativamente, 119 métodos de prueba PHP y 12 pruebas frontend**. Los data providers pueden producir más ejecuciones que métodos. Manifestación de 154 archivos de código; no incluye dependencias instaladas, secretos ni bases.

## Rutas y permisos

| Rutas (método) | Propósito / protección |
|---|---|
| `/` GET | Portada Blade pública, incluso con sesión y Nexo caído |
| `/connect` GET | Inicio/callback Microsoft; limitador microsoft-login |
| `/logout` POST | Salida independiente de Nexo; CSRF |
| `/actuar-como-usuario`, `/vista-prueba` DELETE | Salida de contexto disponible aun si Nexo falla; CSRF |
| `/acceso-restringido` GET | Pantalla React con estado 401/403/503 |
| `/user/photo`, `/user/modules` GET | Sesión Microsoft verificada + Nexo + representación; foto 60/min |
| `/configuracion` GET | Administrador + módulo Configuración vigente; rechaza contextos de prueba |
| `/configuracion/sincronizaciones` GET | Además módulo hijo, ruta y padre exactos |
| `/configuracion/sincronizaciones/programacion` PUT | Versión optimista; 20/min |
| `/configuracion/sincronizaciones/ejecutar` POST | Encola, no descarga durante request; 10/min, respuesta 202 |
| `/configuracion/pruebas-acceso` GET | Administrador + módulo hijo pruebas_acceso |
| `/configuracion/pruebas-acceso/actuar-como-usuario` GET | Capacidad central de representación |
| `/actuar-como-usuario` GET | Redirige a la pantalla anterior |
| `/actuar-como-usuario/personas` GET | Búsqueda central; 30/min |
| `/actuar-como-usuario` POST | Inicia representación central; 10/min |
| `/configuracion/pruebas-acceso/rol-area` GET | También exige procesos_operativos |
| `/vista-prueba` GET / POST | GET redirige; POST inicia escenario por 30 minutos, 20/min |
| `/inicio` GET | Sesión + Nexo + módulo procesos_operativos + contexto efectivo |
| `/formatos/{ur}` GET / PUT | Alcance por UR; PUT además edición, CSRF, contexto, versión; 120/min |
| `/colaboradores` GET / POST | UR delegables y permisos centrales; POST 30/min |
| `/colaboradores/personas` GET | Rama delegada y búsqueda Nexo; 60/min; bloqueado durante escenario |
| `/colaboradores/{collaboration}` DELETE | Comprueba otorgante; revocación local inmediata y retiro central; 30/min |

No hay endpoints propios de descarga: impresión y exportación del formulario se resuelven en React. Los archivos Wayfinder contienen también rutas generadas de framework; no son nuevas funcionalidades TDV2.

## Reglas e identidad

- `MsGraphAuthenticated` exige prueba de sesión versionada y vínculo usuario/tenant/object ID. Identidad nunca tomada de query, formulario ni `auth_email`.
- Microsoft: tenant GUID institucional, state con caducidad de 600 s, PKCE S256, identidad desde Graph `/me`, fallback de mail a userPrincipalName, dominio uacj.mx, Nexo antes de persistir usuario/tokens. El mismo email no permite sustituir otro object ID. Refresco de Graph cifrado y serializado; foto fallida no cierra sesión ni revela tokens.
- Nexo publica exactamente una aplicación, ID positivo, clave tdv2, nivel_control=2. Roles efectivos y módulos se intersectan; rutas y parentesco deben coincidir con configuración local. No se mantienen permisos entre solicitudes sin revalidar.
- UR: sólo presentes y activas; formularios en niveles 2/3. Niveles inferiores comparten ancestro 3 o 2. Recorridos resistentes a ciclos.
- Responsable: determinado por coincidencia textual exacta de num_empleado con catálogo SII, no por adscripción; puede tener varias responsabilidades. Nivel 2 consulta subordinadas y sólo edita su propio formato por defecto.
- Administrador: consulta institucional; edita niveles 2/3 de su única rama nivel 2, derivada de adscripción Nexo válida. Adscripción ambigua, empleado ausente o ancestro inactivo = sin edición. Combinar roles no amplía ese límite.
- Consulta institucional agrega lectura, nunca edición. Colaborador local usa ancestro de formato; de dependencias abarca rama nivel 2 del otorgante. Vínculo local exige concesión central, rol, empleado, adscripción y otorgante coincidentes.
- Delegación: el rol administrador no basta; se intersecta rama candidata con nexo_delegacion y nexo_delegacion_roles. Las funciones reciben actor real o transporte explícito de representación; jamás se suplanta al responsable institucional. Fallar alta central no crea acceso local; fallar retiro central mantiene revocación local y pendiente de reconciliación.
- Prueba por rol/área no cambia identidad Microsoft, no escribe ni busca candidatos; caduca y requiere salida explícita. Configuración y pruebas anidadas bloqueadas.
- Representación revalidada centralmente en cada solicitud; permiso de escritura explícito. Encabezado `X-TDV2-Context` impide guardar desde pestaña con contexto obsoleto. Guardado y auditoría de actor real/representado atómicos; token sólo en servidor.

## Formatos

Definición canónica: `database/plantillas/procesos_operativos.json`. Ocho pestañas, identificación/sistemas/datos/acuerdos hasta 200 filas, IDs únicos de hasta 64 caracteres, texto hasta 4000 (respuestas 8000), contenido máximo 1.5 MB. Códigos PO- de 2–6 dígitos y prioridades 1–9999 únicos. Relaciones proceso/evaluación deben pertenecer al formato. Nueve criterios fijos, doce preguntas fijas y opciones válidas; servidor repone etiquetas y área canónica.

Avance: encabezado 10%, identificación 25%, sistemas 15%, datos 10%, evaluaciones 15%, preguntas 20%, acuerdos 5%; redondeo hacia abajo. Versión inicial 0; primer guardado crea versión 1. Bloqueo de fila UR serializa también primeras creaciones. Conflicto 409 conserva cambios del navegador. GET incorpora nuevos IDs ILDA sin guardar ni sustituir respuestas; coincidencia exacta de cve_ur con ur2, nunca nombres o prefijos.

## Tablas locales declaradas

| Grupo | Tablas / observaciones |
|---|---|
| Identidad y plataforma Laravel | users, password_reset_tokens, sessions, cache, cache_locks, jobs, job_batches, failed_jobs, ms_graph_tokens (la primera migración crea la tabla; la segunda es histórica, con up/down vacíos; posteriores cambios de cifrado/identidad) |
| Catálogos históricos | roles, modules, module_role, catalogo_ur, catalogo, components, codigospostales, pide_estructura; no usar como sustituto de autorización Nexo |
| Auditoría | activity_logs; revisar campos JSON/meta e índices en evidencia |
| UR operativa | unidades_responsables_poa: PK id_ur textual, ejercicio, jerarquía, num_empleado textual, presente; directorio_institucional histórico no autoriza |
| Captura | formatos_ur: UR única, FK restrictiva, JSON contenido, versión, porcentaje, actualizado_por, timestamps |
| Delegación | colaboraciones_ur: identidad/origen/alcance/tipo, IDs Nexo, otorgante, revocada_en, retiro_central_pendiente; única concesión+alcance |
| SII | sincronizaciones_institucionales: resumen y completada_en |
| ILDA | ilda_informacion_area: PK id_origen, ur2, informacion_generada, JSON datos con todas las columnas, presente, sincronizado_en |
| Programador | sincronizacion_catalogos, sincronizacion_ejecuciones, sincronizacion_configuracion (fila 1, versión, próxima, heartbeat, ejecución/propietario/reserva) |

El inventario de columnas, tipos declarados, índices y modificaciones está en EVIDENCIA_FUENTE.md. Antes de producción falta comparar esas declaraciones con un esquema exportado sin datos de una copia aislada.

## Sistemas externos

| Sistema | Contrato |
|---|---|
| Nexo PostgreSQL | Vistas compartidas nexo_aplicacion, nexo_usuarios, nexo_usuario_rol, nexo_modulos, nexo_modulo_rol, nexo_concesiones, nexo_delegacion, nexo_delegacion_roles; contrato 2, Nexo 9.5.25 para identidad institucional |
| Funciones Nexo | public.nexo_a{ID validado}_buscar_personas / conceder_acceso / retirar_acceso; parámetros enlazados; representacion(jsonb), Nexo 9.6.0, READ COMMITTED |
| SII SQL Server | poa.UNIDADES_RESPONSABLES_POA, nombres configurables validados; diez columnas explícitas, último ejercicio, READ COMMITTED, sólo lectura |
| ILDA MySQL | informacion_area completa, orden por id, todas las columnas originales; sólo lectura, habilitación explícita |
| Microsoft | Tenant institucional OAuth y Graph /me, foto, refresco; secretos fuera del repositorio |

## Sincronización y programación

- SII exige descarga no vacía, un solo ejercicio, IDs únicos, jerarquía sin ciclos; empleado conserva ceros. ILDA exige id numérico textual hasta 40 dígitos, ur2 hasta 255 y columnas requeridas; datos preserva nulos, vacíos y columnas adicionales.
- Límite de descarga: 100000 filas; ILDA 128 MiB. Reducción superior al 20% rechazada. Publicación transaccional por fuente, bajas lógicas sin borrar formatos. Comprobación/dry-run no publica.
- Manual acepta sii/ilda/ambas y devuelve 202; una ejecución activa. Cada minuto se procesan también solicitudes manuales aunque automática esté pausada.
- Automática incluye SII; ILDA opcional. Intervalos admitidos: 15, 30, 60, 180, 360 y 1440 minutos; 1440 usa horario diario y zona America/Ciudad_Juarez; no acumula ejecuciones por intervalos perdidos.
- Reserva persistente de 30 minutos con propietario y renovación; publicación exige reserva vigente. Proceso obsoleto no puede publicar ni liberar reserva ajena. Historial por fuente permite fallo parcial, caducidad y reintento. Auditoría fallida revierte publicación.

Comandos Laravel de referencia: `tdv2:sincronizar-institucional --comprobar`, `tdv2:sincronizar-ilda --comprobar`, `tdv2:sincronizaciones-procesar`, `ilda:comprobar --ur=`, `nexo:comprobar --email= --delegacion --configuracion`. Los dos primeros publican sin `--comprobar`; los diagnósticos no deben mutar datos. ASP.NET dispone ahora de `--sync-check=sii|ilda|ambas`, `--sync-once` y `--sync-worker`; las solicitudes manuales se encolan en Configuración. No se portaron los diagnósticos CLI por email/UR ni la publicación CLI directa sin cola. Equivalencias, diferencias y evidencia en [SINCRONIZACIONES.md](SINCRONIZACIONES.md).

Las funciones de delegación reciben parámetros posicionales enlazados: buscar `(actor, ur, q, pagina)`, conceder `(actor, ur, email, rol_id, ur_origen)`, retirar `(actor, ur, concesion_id)`. En representación estas operaciones se transportan por la función JSONB con token del servidor, acción y actor real. SQLSTATE `P0001` se traduce a validación pública sin exponer el mensaje SQL original.

## Interfaz e Inertia

Nueve pantallas: Inicio, FormatoUR, Colaboradores, Configuracion, Sincronizaciones, PruebasAcceso, VistaPrueba, ActuarComoUsuario y AccesoRestringido. Layout, AreaDirectory y PageHeading compartidos. Mantener tema MUI/Roboto y CSS; portada Blade independiente con sus tres familias tipográficas.

Dependencias originales sustituidas/adaptadas en el destino: bootstrap createInertiaApp; Head; usePage y props compartidas; router.visit/post/delete/reload/clearHistory; eventos before/navigate; useForm de VistaPrueba; redirecciones y errores 422; recargas parciales de sincronizaciones/colaboradores. Axios guarda formatos y efectúa búsquedas/altas/bajas. Conservar protección de cambios sin guardar, autosave, conflictos, exportación/impresión y cabecera de contexto.

## Pruebas como criterios de aceptación

EVIDENCIA_FUENTE.md enumera métodos PHP y pruebas frontend; `tests/Support` contiene fábricas/dobles institucionales. Familias: MicrosoftAuthentication, GraphToken, NexoAccess, NexoIdentity, AdministratorAccess, UnitFormAccess, Collaborator, Preview, Representation, ConfigurationSync, InstitutionalSync, IldaInventory, InstitutionalSource, NexoDelegationContract; Example no acredita migración.

Priorizar denegación por defecto, revocación al siguiente request, aislamiento y combinación de roles, conflictos de versión/primer guardado, CSRF real, pestaña obsoleta, representación revocada sin restaurar permisos, publicación atómica, dos ejecutores y recuperación. Pruebas frontend de FormEditor y árbol de áreas deben ejecutarse en el destino con fuentes conservadas.

## Referencias técnicas del destino

### Estructura ASP.NET y EF Core vigente — 2026-10-02 UTC

El backend conserva Controllers, Services, Domain, Security, Synchronization, Web y Hosting. Domain/Entities representa las 14 tablas utilizadas por ASP.NET; Infrastructure/Tdv2DbContext y sus configuraciones usan EF Core 10/Npgsql. Integrations separa Microsoft, Nexo y la lectura SII/ILDA de la persistencia local. Las tablas de infraestructura Laravel y sus catálogos de permisos históricos no se convierten en autoridad local.

Migrations contiene `InitialTdv2`, `InitializePausedSynchronization` y el snapshot estándar. `dotnet ef database update` es el único ejecutor vigente. Ambas se aplicaron al destino autorizado tras confirmar que seguía vacío; la repetición no cambió esquema, historial ni contadores y coincide con la huella del ensayo desechable. Los SQL 001–004 y el DDL de transición anterior se movieron a fixtures de pruebas; Program.cs no ejecuta DDL y el preparador aislado usa EF conservando sus protecciones. El bloque de inicialización SQL de abajo es **evidencia histórica**, sustituida por esta adaptación y por las evidencias EF del estado de migración.

EF se utiliza para lecturas de áreas/formatos/colaboraciones/identidad/inventario y guardado de formatos con token de versión. Se conservan SQL y transacciones especializadas para identidad inicial, tokens, contextos, colaboración central, reservas, publicaciones y auditoría. La política ASP.NET valida acceso antes de servir tanto HTML como JSON; se mantienen los códigos 400/403/409/419/422/503 y contratos React. La regresión actual incluye 25 comprobaciones de migraciones reales, 90 HTTP/PostgreSQL y 31 recorridos React. Evidencia y pendientes: [ESTADO_MIGRACION](../../ESTADO_MIGRACION.md).

### Inicialización SQL anterior (2026-10-01, histórica y reemplazada por EF)

En una ejecución anterior se aplicaron SQL 001–004 y se registraron en `tdv2_schema_migrations`. El usuario volvió a recrear el destino antes de solicitar EF; el preflight de esta adaptación confirmó que ese historial y las tablas anteriores ya no existían. Se conservan las evidencias anteriores sin reutilizar su migrador ni adoptar tablas silenciosamente. Los comandos actuales están en [README](../../README.md#migraciones-ef-core).

### Organización y ensayo de mantenimiento (2026-10-01)

Las rutas inventariadas conservan su contrato y ahora se implementan en `tdv2/Controllers`, con casos de uso en `Services`, SQL en `Infrastructure`/`Synchronization` y límites HTTP en `Web`. `Program.cs` sólo compone el arranque. React se muestra en `ClientApp.esproj`; los perfiles de solución usan Vite por el mismo origen HTTPS y la publicación compila `wwwroot` desde el backend. No se portó nuevamente la interfaz.

La configuración vigente es appsettings/opciones + User Secrets de desarrollo; el importador DPAPI institucional fue retirado después de comprobar el reemplazo. La [guía local](../ARRANQUE_LOCAL_WINDOWS.md) documenta claves y equivalencias Laravel. DPAPI permanece para la administración del clúster Windows.

Corrección posterior de F5 (2026-10-01): `.vscode/launch.json` de ClientApp define Edge, `.slnLaunch` fija ambos destinos y Vite ofrece una redirección de arranque que espera a Kestrel antes de abrir 7136. JSPS comprueba el puerto 5173 durante Implementar. El error original y el arranque en frío se capturaron del IDE mediante EnvDTE; React/HTTPS/HMR se comprobaron en Edge. Implementado, probado y pendientes se separan en el bloque inicial del estado de migración. No cambian los contratos institucionales ni el inventario de pantallas.

Se revisaron de nuevo las tres migraciones Laravel recientes para preparar [la conversión de una copia](TRANSICION_DATOS.md). El ensayo distingue bootstrap de base vacía y DDL aditivo sobre copia no vacía, conserva datos y exige claves/relaciones válidas. Respaldo/restauración y conservación se ensayaron sólo con datos sintéticos; el esquema y los datos desplegados continúan sin consultar. Resultados actuales separados en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).

Para implementar y verificar el transporte: [antiforgery ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [pruebas con WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0), [NpgsqlDataSource y parámetros](https://www.npgsql.org/doc/basic-usage.html). Las reglas de negocio proceden del código local, no de esas referencias.
