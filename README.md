# TDV2 · ASP.NET Core 10 + React

**Cabecera compacta de Competencias — 2026-10-30:** logotipo, módulos y cuenta en una sola barra de 72 px. Primero se oculta el nombre al faltar espacio y después se utiliza un menú accesible, conservando el orden autorizado, representación y nombre completo en la cuenta. Sin cambios de captura, sesión o accesos públicos. [Archivos, verificaciones y capturas](docs/migracion/CABECERA_COMPACTA_COMPETENCIAS_20261030.md).

**Accesos públicos tras vencer — 2026-10-30:** los botones de Procedimientos y Competencias, y Mi espacio, entran explícitamente por `/connect` conservando el destino. Corrige el retorno permanente a la portada por la marca de sesión terminada; mantiene los 120 minutos y el vencimiento sin OAuth automático. Diagnóstico dirigido de sólo lectura para la excepción independiente de generación. [Causa, flujo, paquetes, pruebas y límites](docs/migracion/ACCESOS_PUBLICOS_TRAS_VENCIMIENTO_20261030.md).

**Fallos adicionales de pruebas — 2026-10-30:** los diez pendientes y otro caso relacionado se reprodujeron también en una copia aislada de `06e307c`. Corregidos fixtures y expectativas obsoletas en cinco archivos de pruebas, sin cambiar producción o la política de sesión. **37/37 casos afectados/relacionados, cinco grupos de sesión y 116 reglas/HTTP correctos.** Se fortalecen instantáneas de etapa, identidad sintética e ILDA por conjuntos. [Comparación anterior, cada causa, archivos y resultados](docs/migracion/REVISION_FALLOS_SUITE_20261030.md). La selección no acredita la suite ampliada completa.

**Sesión compartida y reportes — 2026-10-30:** Procedimientos y Competencias vencen a las dos horas del inicio original, sin renovación por actividad o procesos automáticos. El servidor invalida el ticket y la interfaz vuelve a `/`, sin iniciar Microsoft automáticamente ni guardar al salir. Tickets anteriores sin inicio fiable requieren un nuevo acceso explícito. Reportes sin leyendas técnicas y etiqueta «Porcentaje de avance», con fórmula/población intactas. [Política efectiva, archivos, pruebas y evidencia](docs/migracion/SESIONES_DOS_HORAS_Y_REPORTES_20261030.md). Sin nuevo esquema, secretos ni permisos.

**Población por canónico — 2026-10-30:** Avance cuenta `usuario_id` de Nexo, sin reducir la población por empleados compartidos. La correspondencia de encuestas se comprueba por separado y deja pendientes sólo los estados afectados; conteos registrados conservados. Nueva etiqueta «Porcentaje de encuestas finalizadas». [Definiciones, cruce seguro, pruebas y límites reales](docs/competencias-digitales/POBLACION_CANONICA_Y_ESTADOS_20261030.md). Sustituye la regla anterior que bloqueaba el denominador por `employee_conflict`.

**Conflictos de empleado y conteos registrados — 2026-10-30:** el operador confirmó 80 candidatos, tres grupos `employee_conflict` y 8 encuestas finalizadas almacenadas. Se conserva el rechazo del denominador ambiguo; el reporte muestra los conteos registrados dentro del alcance por UR. Diagnóstico privado opcional fuera de Git para revisar canónicos, asignaciones, UR, vínculos existentes y referencias de encuesta, sin exportar respuestas. [Significado, comando, pruebas y acción pendiente](docs/competencias-digitales/CONFLICTOS_EMPLEADO_Y_CONTEOS_20261030.md).

**Diagnóstico de Avance no disponible — 2026-10-30:** seguimiento de petición/validación hasta los indicadores nulos, incidencias de población diferenciadas de fallos temporales y diagnóstico ampliado exclusivamente de lectura. `scripts/Diagnose-DigitalPopulation.ps1` comprueba contrato, SELECT, adscripción, esquema y encuestas almacenadas sin imprimir personas o secretos. La causa institucional requiere la salida del operador; este entorno termina en CONFIG_MISSING antes de conectar. [Trazado, comando, pruebas y límites](docs/competencias-digitales/DIAGNOSTICO_POBLACION_20261030.md).

**Indicadores, configuración y reapertura individual — 2026-10-29:** población por acceso vigente a la aplicación, nivel 2 predeterminado y submódulos administrativos publicados. Reapertura conserva captura e informes históricos mediante una nueva migración Digital pendiente de aplicación institucional. La causa concreta de «No disponible» aún requiere lectura del operador: este entorno no pudo acceder a User Secrets. [Implementación, diagnóstico seguro, pruebas y operación](docs/competencias-digitales/INDICADORES_SUBMODULOS_REAPERTURA_20261029.md).

**Presentación de encuesta — 2026-10-29:** instrucciones sólo al inicio de los cinco apartados; respuestas «Inicial»/«No aplica» y acción masiva sin leyendas repetidas. Se conservan captura, evaluación y trabajo previo. [Corrección puntual, pruebas y capturas](docs/competencias-digitales/PRESENTACION_ENCUESTA_20261029.md).

**Competencias: población e instrucciones — 2026-10-29:** Avance utiliza personas únicas con acceso vigente a la aplicación (definición actualizada en la revisión de indicadores), incluidas las que nunca ingresaron, mediante las vistas efectivas de Nexo. Metas manuales conservadas como histórico de sólo lectura; estados y porcentajes coherentes, filtros sin nivel 1/tipo N y detalle adaptable. «Marcar pendientes como Inicial» preserva respuestas y rúbricas parciales. Las once equivalencias ya eran correctas y se mantienen, sin nueva edición ni migración. [Contrato, reglas, archivos, pruebas y evidencia](docs/competencias-digitales/POBLACION_INSTRUCCIONES_20261029.md).

**Revisión del vínculo de identidad — 2026-10-29:** `users` ya conserva `(tid,oid)` y evita duplicados Microsoft; leer UPN/SMTP no necesita otra tabla. Lo que falta en el modelo previo es persistir el `usuario_id` canónico de Nexo para distinguir cambios/reasignaciones de correo después de cerrar sesión. Se conserva la migración propuesta **sin aplicar**; 23 comprobaciones de modelo y acceso a ambos sitios correctas, sin conexiones ni migraciones en esta revisión. [Inventario, alternativas y cambio exacto del esquema](docs/migracion/REVISION_VINCULO_IDENTIDAD_20261029.md).

**Identidad Microsoft y alias — 2026-10-29:** Graph consulta UPN/SMTP explícitos; una coincidencia institucional única acredita el usuario canónico antes de consultar permisos. El vínculo `(tid,oid) → usuario_id` conserva la cuenta local aunque cambie `mail`; conflictos no combinan accesos. Requiere la nueva migración **Tdv2DbContext `20261029163206_VerifiedInstitutionalIdentityLinks`** y SELECT/INSERT para el rol web en su tabla propia; no fue aplicada institucionalmente. No cambia DigitalDbContext ni Nexo. El caso real y el registro efectivo AuthSID requieren comprobación del operador. [Causa, criterio actualizado, archivos, pruebas y operación](docs/migracion/IDENTIDAD_MICROSOFT_ALIAS_20261029.md).

**Auditoría de costo — 2026-10-29:** desglose de las 35 sentencias de una captura, separado de preparación/login/verificación, carga aislada con 1/10/25 personas y comparación de conexiones reutilizadas. Instrumentación optativa sólo en pruebas; no se retiran controles ni se cambian conexiones o persistencia de producción. [Mediciones, comparación, archivos y límites](docs/competencias-digitales/COSTO_AUTOGUARDADO_20261029.md).

**Autoguardado y recuperación — 2026-10-29:** un fallo detiene los envíos automáticos de Competencias hasta un Guardar avance confirmado; no se reenvían al editar, volver la conexión o recuperar foco. Conserva selección inmediata, controles de versión y auditoría manual. Se omiten cambios revertidos al valor guardado. [Medición SQL/HTTP en PostgreSQL aislado, pruebas y límites](docs/competencias-digitales/AUTOGUARDADO_POSTGRES_Y_RECUPERACION_20261029.md). Esta política sustituye el reintento de captura descrito en las entradas históricas siguientes; la cola Nexo conserva sus reintentos.

**Mi espacio — 2026-10-28:** un único sitio comprobado se abre directamente, sin mostrar el selector; con varios se presenta **Elige un sitio** y con ninguno se conserva la sesión con regreso al portal/cambio de cuenta. Registro extensible de sitios, retorno específico intacto y fallas temporales diferenciadas. [Archivos, verificaciones y capturas](docs/migracion/MI_ESPACIO_SITIOS_20261028.md).

**Accesos independientes y mensajes — 2026-10-28:** el portal sigue público; una identidad institucional válida puede consultar sus accesos sin requerir permiso de Procedimientos. `/accesos` ofrece sólo aplicaciones verificadas, conserva el retorno solicitado y distingue denegación de falla temporal sin cerrar sesión. Avisos específicos y cambio de cuenta sin detalles internos. Se retira el mensaje largo de guardado, conservando el indicador breve, la captura fluida y la auditoría manual. [Archivos, comprobaciones, capturas y límites](docs/migracion/ACCESO_POR_APLICACION_Y_MENSAJES_20261028.md).

**Captura fluida — 2026-10-28:** el autoguardado de Competencias continúa en segundo plano sin deshabilitar preguntas ni mover el formulario. Conserva selecciones posteriores a una solicitud, recupera respuestas de red perdidas y vacía lo pendiente antes de guardar manualmente, navegar o finalizar. Auditoría Nexo y validación del backend intactas. [Causa medida, archivos, comprobaciones y límites](docs/competencias-digitales/AUTOGUARDADO_FLUIDO_20261028.md).

**Actividad de guardado — 2026-10-28:** autoguardados y transiciones conservan captura y bitácora local, sin eventos de guardado en Nexo. «Guardar avance» espera la persistencia y confirma una operación manual idempotente, incluso sin cambios adicionales. Cola, reintentos y demás eventos intactos; sin nuevas migraciones. [Archivos, contrato y verificación](docs/competencias-digitales/GUARDADO_MANUAL_ACTIVIDAD_20261028.md).

**Cabecera azul de Competencias — 2026-10-28:** se recupera `#213455` en identidad y navegación, con el SVG blanco oficial proporcionado, copiado sin modificaciones. Se conservan las dos filas, cuenta/foto, orden Nexo, Noto Sans, cuerpo y pie compartido. Esta revisión sustituye únicamente la barra blanca descrita a continuación. [Archivos, capturas, comprobaciones y paquete local](docs/migracion/CABECERA_AZUL_COMPETENCIAS_20261028.md).

**Cabecera/pie de Competencias y acceso al juego — 2026-10-28:** barra blanca como el portal, logo oficial oscuro de Transformación Digital y estados/cuenta azules; pie con UACJ, DGTI y las cuatro subdirecciones usando el componente y estilos existentes. En el portal, sólo «¿Qué tanto sabes de Microsoft 365?» y «Jugar Ahora», conservando juego y encuesta separados. Tipos/builds, 116 pruebas de backend, 51 grupos de portal/juego/TDV2 y 11 grupos de Competencias tanto en bundles como Vite correctos con datos sintéticos. Sin cambios de lógica, migraciones, paquetes, 401 ni actividad Nexo. [Archivos, capturas y límites](docs/migracion/CABECERA_PIE_JUEGO_20261028.md).

**Instrumento y actividad de Competencias — 2026-10-28:** edición `uacj-digital-2026-10-28.1`, once perfiles laborales con correspondencias metodológicas explícitas, cuatro funciones seleccionables y 80 herramientas. Actualización confirmada de borradores con copia de la captura anterior; definitivas y aviso `.2` intactos. Actividad mediante el contrato SQL real de Nexo, con cola transaccional, UUID/recibos, reintentos y actor real diferenciado del representado. Requiere la nueva migración **DigitalDbContext `20261028175912_DigitalActivityDelivery`** y comprobar publicación/EXECUTE de Nexo con el operador; no se aplicó en bases institucionales. Se conservan paquetes corregidos y solución del 401. [Cambios, correspondencias, pruebas, capturas y operación Windows/Ubuntu](docs/competencias-digitales/INSTRUMENTO_ACTIVIDAD_20261028.md).

**Mi informe y ruta, aviso y menú — 2026-10-27:** resumen personal, recomendaciones y fases ordenadas; una tabla por bloque con pestañas y fichas móviles. Sin respuestas reales se muestra el estado inicial; cero conserva su validez y los informes definitivos su JSON histórico. Aviso literal proporcionado por el usuario, versión `uacj-digital-aviso-2026-10-27.2`, sin sobrescribir aceptaciones. El menú conserva el orden publicado por Nexo: el operador debe fijar Encuesta=1, Mi informe y ruta=2, Avance=3, Resultados=4, Configuración=5; no se alteraron datos reales. Sin grupos nuevos ni cambios de permisos. Tipos/builds correctos, 116 pruebas existentes, 24 nuevas de backend y 32 grupos de navegador, con datos sintéticos y sin PostgreSQL ni migraciones. [Archivos, capturas, comandos y límites](docs/migracion/INFORME_AVISO_MENU_20261027.md). NuGet queda para otra tarea.

**Revisión anterior de menú y pie — 2026-10-27 (orden sustituido por la revisión siguiente):** Competencias recupera Avance/Resultados/Encuesta/Informe/Configuración, sin agrupaciones, conservando 1400 px y sus demás mejoras. Pie interno TDV2 distribuido en todo su ancho, con nombres completos en dos líneas y filas adaptables. Del portal se retira únicamente la oración indicada del test; se conservan ayuda, progreso y acceso principal a la encuesta. Sin cambios de Nexo ni migraciones. [Capturas, archivos y verificación](docs/migracion/CORRECCION_MENU_PIE_TDV2_20261027.md). Las demás mejoras de la [entrega anterior](docs/migracion/NAVEGACION_TEST_Y_PIES_20261027.md) permanecen.

**Distribución de Competencias — 2026-10-27:** contenedor común centrado de hasta 1400 px, UACJ/cuenta en una fila y navegación debajo. En móvil, botón de fotografía con nombre accesible y completo en el menú. Pie con cinco firmas espaciadas y distribución adaptable, conservando colores, tipografía y lógica. Verificado en siete anchos con Vite y bundles. [Capturas, archivos y límites](docs/migracion/DISTRIBUCION_COMPETENCIAS_1400_20261027.md).

**Corrección de carga visual en desarrollo — 2026-10-27:** Vite anteponía `/__vite/` al CSS institucional de ASP.NET y recibía HTML en su lugar; las variables quedaban vacías y la cabecera mostraba blanco sobre blanco. Corregidos únicamente los enlaces estáticos del shell con `vite-ignore`, sin modificar estilos ni lógica. Verificado con Vite real, bundles, escritorio/móvil y regresión de portal/Procedimientos. [Causa, capturas y pruebas](docs/migracion/REGRESION_VISUAL_COMPETENCIAS_20261027.md).

**Identidad visual vigente — 2026-10-27:** azul `#213455`, amarillo `#F0BD10`, gris `#DADADA` y Noto Sans local unificados en portal, Procedimientos, Competencias y juego. Nuevo fondo fotográfico local con versiones optimizadas; firmas de áreas en Noto Sans, pies en el orden institucional solicitado y DGTI trasladada al pie de Competencias. Cuenta, captura, permisos y lógica del juego conservados. Avenirs LS STD queda pendiente de un archivo autorizado; no se simula su carga. [Archivos, recursos, evidencias, pruebas y límites](docs/migracion/IDENTIDAD_VISUAL_20261027.md).

**Portal y perfil — revisión anterior 2026-10-27:** perfil de Competencias organizado en tres grupos, dos columnas adaptables y foco en orden visible; pies al fondo mediante flujo normal. Portal con Noto Sans local, contacto `td.pi@uacj.mx`, textos de etapas actualizados, tres PDF públicos y pie con identidad UACJ/DGTI más cuatro subdirecciones. **Responder encuesta de competencias digitales** lleva al subsitio; **Juego de Microsoft 365 / Iniciar test** abre el juego existente. Esta separación sustituye el enlace único descrito en entregas anteriores; en aquella entrega el juego conservaba Inter y Procedimientos Roboto; la revisión de identidad posterior sustituye ambas por Noto Sans. Tipos, frontend, ASP.NET y recorridos aislados comprobados, sin nuevas migraciones ni reinicios ejecutados. [Archivos, documentos, publicación local y evidencia](docs/migracion/PORTAL_Y_PERFIL_20261027.md).

Backend ASP.NET con controladores, EF Core 10/Npgsql y PostgreSQL; frontend React, TypeScript y MUI. Abre **tdv2.slnx** y selecciona **TDV2 HTTPS + React** para F5. El retorno Microsoft permanece en **https://localhost:7136/connect**.

La adaptación técnica no acredita todavía el acceso institucional real ni el despliegue Ubuntu. Implementado, probado y pendiente se registran por separado en [ESTADO_MIGRACION.md](ESTADO_MIGRACION.md).

**Reinicio administrativo e informe de Competencias — 2026-10-27:** Configuración permite al administrador real revisar y confirmar un reinicio global; metas opcionales, generación persistente contra solicitudes antiguas y recibo idempotente con auditoría. Requiere aplicar explícitamente **`20261027131404_AdministrativeDigitalReset`** y conceder sus permisos mínimos antes de habilitar esta versión. La migración inicial aplicada permanece intacta. Informe por bloques con brechas ámbar, sin descarga HTML; pie azul (ampliado posteriormente con DGTI) y aviso de lectura en la cuenta. [Alcance, respaldo, permisos, operación y pruebas](docs/competencias-digitales/GUIA.md#reinicio-administrativo-informe-y-pie--2026-10-27).

**Ajuste anterior de Competencias digitales — 2026-10-27:** presentación del HTML recuperada con Noto Sans local, cuenta/fotografía compartidas con TDV2 y avance obligatorio por seis secciones validado en el servidor. El aviso antes situado en el pie pasó al menú de cuenta en la revisión vigente. [Reglas, archivos y evidencia](docs/competencias-digitales/GUIA.md).

**Implementación inicial de Competencias digitales UACJ — 2026-10-27:** subsitio `/competencias-digitales` con Avance por dependencia, Resultados generales, Encuesta, Mi informe y ruta y Configuración. Autorización propia en Nexo, catálogo institucional compartido por los servicios existentes y base PostgreSQL independiente mediante `DigitalDbContext`. Encuesta única, privacidad por versión, evaluación del servidor, informe definitivo inmutable y representación únicamente de consulta. El botón público **Iniciar test** ahora dirige al subsitio; este comportamiento sustituye el acceso al quiz descrito en las entradas históricas siguientes. El diseño público y Procedimientos se conservan.

La nueva configuración es opcional para el arranque de TDV2: `CompetenciasDigitales:Nexo:ApplicationId`, `ConnectionStrings:CompetenciasDigitalesNexo` y `ConnectionStrings:CompetenciasDigitales`. No hay fallback de persistencia ni migración automática. Antes de habilitarlo institucionalmente, completar Nexo, base/usuario, secretos y revisión del aviso. [Guía de arquitectura, permisos, fórmulas, claves de entorno, migraciones y lista concreta para el operador](docs/competencias-digitales/GUIA.md). Las pruebas utilizan autenticación y publicaciones Nexo sintéticas sobre PostgreSQL aislado; no acreditan integración institucional real.

**Portal público — acabado visual 2026-10-17:** mayor presencia del azul institucional, modelo más claro, etiquetas/iconos azul–blanco, ruta azul y controles/preguntas abiertas mejor diferenciados. `#CFD6E8` se limita a estados; sin amarillo. Anclas ajustadas a la altura real de la cabecera. Ajuste posterior: enlaces sin subrayados, con resaltado suave y contorno de teclado; clics/foco y móvil nuevamente verificados. Textos, geometría, Inter, logos, jerarquía y quiz intactos; sistema interno sin cambios. Comparación de 316 elementos en cuatro ventanas, anclas, contraste, quiz y recursos HTTP verificados en aislamiento. La foto externa no cargó: queda pendiente verla con ese recurso disponible. [Archivos, colores, capturas, pruebas y límites](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-del-portal--2026-10-17).

**Procedimientos y Colaboradores — restauración visual vigente 2026-10-16:** recuperadas la distribución, etiquetas, estilos y espacios anteriores al último ajuste de estas vistas. En Procedimientos el nombre vuelve a la línea principal y la clave a la secundaria, con los controles y botones previos. Colaboradores recupera su formulario y lista anteriores; la única excepción es la ausencia de Origen y sus textos. Se mantiene **Roboto local** dentro del sistema; portal y quiz conservan **Inter** y su presentación. Ambos roles mantienen claves, permisos y reglas N2/N3. Tipos/Vite, 94 pruebas frontend, 116 de dominio/HTTP y 16 grupos de navegador correctos. Sin cambios de contratos, datos ni migraciones. [Alcance, archivos, pruebas, capturas y límites](docs/migracion/VISTAS_PROCEDIMIENTOS_COLABORADORES.md#restauración-visual-vigente).

El ajuste posterior de Colaboradores sólo simplifica la ayuda de búsqueda y retira los dos textos informativos sobre vigencia/asignaciones, además de las referencias a niveles numéricos; distribución y reglas intactas. [Textos y alcance](docs/migracion/COLABORADORES_VISIBILIDAD_ACCIONES.md#ajuste-de-textos-vigente--2026-10-16).

Las entradas siguientes conservan evidencia de entregas anteriores. El último rediseño de estas dos vistas queda retirado; la recuperación de Roboto y la eliminación visual de Origen permanecen. El portal y el quiz mantienen Inter.

**Quiz Microsoft 365 e Inter local — 2026-10-16:** Diagnóstico de madurez digital abre el reto público en un diálogo amplio/pantalla completa con iframe, cierre/Escape y retorno de foco/desplazamiento. Conserva íntegros preguntas, lógica, textos y diseño del original. Portal y quiz conservan Inter variable 100–900 desde `wwwroot/fonts/inter`, con licencia OFL y respaldo visible, sin Google Fonts. Se conservan portada, logo y reversión de columnas. Tipos/Vite/.NET, 94 frontend, 33 regresiones internas, recorrido completo de los cuatro niveles y recursos HTTP anónimos correctos; paquete construido y recursos verificados en aislamiento. Sin nuevas rutas de captura, datos ni migraciones. [Archivos, integración, fuentes, capturas, comandos y límites](docs/migracion/QUIZ_MICROSOFT_365_E_INTER.md).

**Columnas — reversión visual vigente (2026-10-16):** retirado únicamente el último ajuste de delimitación. Se recuperan fondos, sombras suaves, anchos y espaciados previos; Edición fija a la izquierda y Acciones a la derecha, con la excepción móvil anterior (hasta 600 px Edición se desplaza y Acciones sigue fija). Los grupos vuelven a una celda de ancho completo con su botón de agregar; las acciones de registros conservan su columna. Portada, jerarquía y alineación UACJ intactas. Sin cambios funcionales, contratos, datos ni migraciones. [Alcance, comprobaciones y evidencia de la reversión](docs/migracion/COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente). Las entradas siguientes conservan la evidencia de las entregas anteriores.

**Jerarquía de portada — ajuste vigente (2026-10-16):** sólo el bloque principal cambia tamaños, pesos y espacios: conjunto UACJ de 560 px adaptable, Dirección a 16–18 px/600 y h1 de 24–38 px/600. Separaciones de 14 px entre institución/dependencia y 36 px antes del proyecto. Se conservan blanco, fondo, fotografía, archivos de logotipos, marcado y resto de la portada; controles internos intactos. Build Release y comparación Edge antes/después en seis anchos correctos, sin recortes del bloque; 297 elementos exteriores sin cambios de diseño. Evidencia `.artifacts/hero-hierarchy-20261016/`. Este ajuste autorizado sustituye **sólo dentro del bloque** la restricción anterior de cambiar únicamente colores. [Capturas, pruebas, límites y comandos](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-jerarquía-institucional-del-bloque-principal). Los resultados de las revisiones siguientes son evidencia anterior, no nuevas ejecuciones de esta entrega.

**Revisión visual anterior — paleta primaria y textos blancos (2026-10-16):** releído el manual local actual de seis páginas. Portada basada en **#213455, #DADADA y #FFFFFF**, con **#174187 sólo en hover**; título, Dirección General, lema y párrafo principal blancos al 100 %. Navegación y El proyecto conservan fondo blanco. Único cambio productivo de esta revisión: colores de la vista pública; ajustes internos existentes conservados y nuevamente probados. Tipos/Vite/Release, 94 frontend, 116 dominio/HTTP y 33 recorridos Edge correctos; 306 elementos sin diferencias geométricas en tres tamaños, contraste público mínimo 8.05:1. Evidencia nueva `.artifacts/visual-primary-20261016/`; Google Fonts/Unsplash inaccesibles en ambos pases. Sin bases ni migraciones. [Archivos, uso del complementario, capturas, límites y comandos](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

**Ajustes visuales — 2026-10-16:** presencia con paleta propia, indicador lateral y nombre de quien edita; encabezados del catálogo en Roboto 700 y búsqueda sin superposición; **Acciones fija a la derecha** en las tablas de captura. La portada conserva su diseño y sólo cambia a los colores institucionales del manual, sin amarillo. Verificados tipos/Vite/Release, 94 pruebas frontend, 116 dominio/HTTP, 24 recorridos internos y 9 de regresión de selectores; comparación de 306 elementos de portada antes/después en tres tamaños. Sin cambios de contratos, base de datos ni migraciones ejecutadas. Recursos externos del portal, dispositivos físicos e institucional pendientes. [Archivos, capturas, contrastes, comandos y límites](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

**Catálogo y retirada de Medios — 2026-10-16:** selector agrupado con búsqueda, Microsoft 365 y Adobe con aplicaciones múltiples, duplicados impedidos por procedimiento con validación transaccional y compatibilidad histórica. Medios utilizados se retira de captura, contratos, revisión y persistencia mediante **`20261016190000_RemoveUsedMedia`**. Conserva herramientas opcionales, agrupación, reservas, autoguardado y listas locales. Requiere respaldo y actualización conjunta backend/frontend/EF; aplicación institucional pendiente. [Detalle, propiedades retiradas, recuperación, comandos y pruebas](docs/migracion/CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md).

**Aplicaciones web y móviles — origen vigente 2026-10-15:** listas Excel versionadas, **207 web / 26 móviles**, sin duplicados ni filas inválidas. Importación local transaccional/idempotente mediante `--check-applications` y `--import-applications`; PostgreSQL conserva IDs coincidentes inequívocos, UUID locales nuevos e históricos. SQL Server vuelve a UR + módulos, ILDA independiente. Nueva migración **20261015165555_LocalApplicationLists**, sin alterar migraciones anteriores. Se conservan selectores, reservas, JSON y enviados. [Archivos, correspondencias, compatibilidad y comandos Windows/Ubuntu](docs/migracion/APLICACIONES_LISTAS.md).

Presentación compacta (2026-10-14): Sistemas conserva la tabla sin el bloque «Procedimientos y captura de herramientas». Los pendientes permanecen en **Revisar y enviar**, con **Ir al campo** para las filas incompletas. La captura opcional sustituye los antiguos pendientes de cobertura. Módulos del Sistema Integral de Información muestra «Selecciona módulos», el nombre completo de una selección o la cantidad si hay varias; búsqueda/casillas en el desplegable, sin chips ni ayuda repetida bajo el campo. [Cambios, pruebas y límites](docs/migracion/SISTEMAS_HERRAMIENTAS.md#resumen-compacto-y-pendientes--2026-10-14).

**Agrupación de herramientas y redacción — 2026-10-16:** una sola tabla con encabezados de procedimiento (código y nombre completos), altas explícitas desde cada grupo y registros sin asociación visibles para reasignar. El orden es sólo visual; el JSON, los IDs, reservas, autoguardado y captura opcional se conservan. Revisados los textos de todas las secciones sin transformar respuestas ni claves históricas. Sin migración. [Implementación, archivos, comprobaciones y límites](docs/migracion/SISTEMAS_HERRAMIENTAS.md#agrupación-dentro-de-la-tabla-y-redacción--2026-10-16).

**Herramientas opcionales y terminología — 2026-10-16:** cada procedimiento admite cero, una o varias herramientas. No registrar herramientas no genera pendientes, no resta avance ni bloquea el envío. Las filas iniciadas conservan todos sus requisitos y condicionales. Los formatos nuevos empiezan sin filas de herramientas; los datos anteriores y enviados permanecen intactos. Avance, revisión y API de envío utilizan el mismo evaluador. Textos visibles usan «procedimiento», sin renombrar contratos ni respuestas. Sin migración nueva. [Regla, fórmula, compatibilidad y pruebas](docs/migracion/COBERTURA_Y_GUARDADO.md).

Módulos del Sistema Integral de Información (2026-10-14): una fila permite seleccionar varios módulos con búsqueda, casillas y nombres completos. Comparte procedimiento, uso, funcionamiento y áreas de oportunidad; otras filas siguen siendo independientes. Se guardan IDs e instantáneas por módulo en el JSON existente, con compatibilidad para el módulo único anterior. El conjunto cuenta como **un requisito** de captura. Sin migración nueva. [Contrato, archivos y verificaciones](docs/migracion/SISTEMAS_HERRAMIENTAS.md#selección-múltiple-de-módulos--2026-10-14).

Tablas de captura (2026-10-13): Identificación y Sistemas tienen desplazamiento propio en ambos ejes, altura adaptable, encabezados fijos y navegación a pendientes/nuevas filas. **Procedimiento** conserva la clave `tramite`. Portal del CAST deja de ofrecerse y el servidor rechaza nuevas asignaciones, conservando respuestas históricas. [Implementación, archivos, pruebas sintéticas y límites](docs/migracion/SISTEMAS_HERRAMIENTAS.md#catálogo-histórico-procedimiento-y-desplazamiento-propio--2026-10-13). Sin migración nueva.

Ajustes de captura (2026-10-12): Acciones dispone de 112 px sin comprimir otros campos; SIIv2 muestra el nombre del módulo y conserva su ID interno; **Egresados** es una opción válida de atención; **Áreas de oportunidad** conserva los comentarios existentes. [Cambios, archivos y pruebas sintéticas](docs/migracion/SISTEMAS_HERRAMIENTAS.md#ajustes-de-captura--2026-10-12). Sin migración nueva.

Guardar, Colaboradores y Pruebas de acceso (2026-10-10): **Guardar** es secundario y no envía; **Enviar formato** conserva confirmación y versión vigente. Colaboradores integra concesiones centrales publicadas por Nexo y vínculos delegados, filtrando cada asignación por área/jerarquía y separando visibilidad de retiro. Las modalidades de acceso usan selección/resumen en dos columnas equilibradas, centradas y adaptables. [Reglas, límites de Nexo, archivos y comprobaciones](docs/migracion/COLABORADORES_VISIBILIDAD_ACCIONES.md).

Distribución y claridad (2026-10-10): Inicio conserva su directorio jerárquico, con pestañas separadas, acciones alineadas y recuperación de filtros/expansión/desplazamiento. Procedimiento usa un buscador compacto con nombre completo accesible. Revisar y enviar ofrece **Ir al campo** y la acción final **Enviar formato**. [Ajuste inicial de distribución](docs/migracion/INTERFAZ_OPERATIVA.md).

Complemento vigente (2026-10-10): avance y pendientes comparten requisitos válidos del backend, con condicionales y sin penalizar opcionales; Inicio promedia formatos únicos sin redondear a 100 antes de completar. Revisión usa **Resumen del formato** sin cambiar el alcance del envío. **Sincronizar SQL Server** ejecuta UR y módulos juntos, con resultados individuales; ILDA tiene su propia acción. Pruebas de acceso distingue las dos modalidades, búsqueda y resumen previo, contexto activo y cuenta original. Sin dependencias, migraciones ni cambios de permisos. [Fórmula, archivos, pruebas y límites](docs/migracion/AVANCE_Y_PRUEBAS_ACCESO.md).

Sincronización manual desde la aplicación (2026-10-09): los botones de Configuración → Sincronizaciones guardan la solicitud y despiertan un servicio hospedado de ASP.NET. El mismo coordinador atiende la cola persistente sin una terminal CLI adicional, incluso con los horarios pausados. La pantalla confirma inicio, avance y resultados por catálogo. Los procesadores externos siguen siendo compatibles y los horarios conservan su operación explícita. [Funcionamiento, recuperación, pruebas y operación Windows/Ubuntu](docs/migracion/SINCRONIZACIONES.md).

Sistemas y herramientas (2026-10-09): selección única por fila, varias herramientas por procedimiento, detalles estructurados de Otra/Otro y módulos del Sistema Integral de Información desde réplica local. SII manual/automática sincroniza **UR y módulos** con la misma conexión, resultados separados e ILDA independiente. Requiere **`20261009080533_SiiModulesCatalog`**, preparada sin aplicación institucional. [Formulario, SELECT necesario, comando EF, operación y límites de pruebas](docs/migracion/SISTEMAS_HERRAMIENTAS.md).

Primera etapa vigente: **Contexto, Identificación general y Sistemas y herramientas**. La pestaña **Revisar y enviar**, inmediatamente después, muestra avance, pendientes con enlaces, resumen y autoguardado; no añade requisitos ni porcentaje. **Enviar formato** conserva el envío de primera etapa, permisos, comprobaciones concurrentes y bloqueo definitivo. Los procedimientos relacionables deben estar confirmados, completos, válidos y V/A; D/N y relaciones incompletas siguen pendientes. Requiere aplicar explícitamente **`20261009171656_StageSubmissions`**: separa los envíos por etapa y conserva bloqueados los envíos anteriores sin atribuirles una etapa. La segunda permanece deshabilitada. [Funcionamiento, conservación y comandos EF](docs/migracion/REVISAR_ENVIO_ETAPAS.md).

Foco y presencia (2026-10-09, revisado 2026-10-16): tablas compactas, indicador MUI y presencia en **Edición**, con fotografía, nombre visible/accesible y estado textual. **Acciones** queda fija a la derecha; Edición también se fija en contenedores mayores de 600 px. Eliminar conserva área de 44 × 44 px. Los textos muestran de dos a cuatro líneas y permiten recorrer el contenido completo. Desplazar no abandona la edición; una salida real guarda y libera tras confirmación. [Reproducción y pruebas de desplazamiento](docs/migracion/DESPLAZAMIENTO_TABLAS.md).

El diagnóstico de `42P01` comprueba `public.sii_modulos` y el historial local: no atribuye el fallo a SQL Server ni presenta ausencia de esquema como catálogo vacío. **Sin nueva migración en estos ajustes de interacción**; sigue siendo necesaria `20261009080533_SiiModulesCatalog`. [Reglas de edición](docs/migracion/EDICION_AUTOMATICA.md) y [diagnóstico/comandos EF](docs/migracion/SISTEMAS_HERRAMIENTAS.md).

Identificación conserva la tabla con los tamaños compactos originales del tema, encabezados en negritas, selección múltiple con siete categorías definitivas y eliminación directa sin renovar filas retiradas. Requiere **`20261009052502_CurrentRecipients`**, que limpia una vez sólo Otro/Externo y su detalle en borradores vigentes, conservando el resto y los enviados/históricos. Recuperar la densidad visual no añade ni revierte migraciones. [Migración, causa comprobada, capturas y pruebas](docs/migracion/IDENTIFICACION_TABLA.md).

Fotografías: estado compartido entre páginas y caché privada, renovación a los 15 minutos y conservación ante fallos temporales durante un máximo de una hora desde la última verificación correcta. Limpieza al cambiar identidad y diagnóstico de cifrado entre entornos; **sin nueva migración**. [Política, comprobaciones y límites](docs/migracion/PERSISTENCIA_FOTOGRAFIAS.md).

Entrega 2026-10-07: escritura por bloque sin renderizados de filas ajenas, presencia con tooltip accesible y miniaturas Microsoft protegidas y cacheadas. Conserva reservas automáticas, autoguardado, eliminación directa y las reglas de participación vigentes; nuevas reglas de participación quedan pendientes. Requiere la migración **`20261007161549_ParticipantPhotographs`**, probada sólo en PostgreSQL aislado. [Instalación, mediciones 20/100/200, fotografías y comprobación desde Visual Studio](docs/migracion/CAPTURA_FOTOGRAFIAS_RENDIMIENTO.md).

La [configuración de participación, optimización del editor y presencia colaborativa](docs/migracion/PARTICIPACION_RENDIMIENTO_PRESENCIA.md) incluye la nueva migración y el registro del submódulo `configuracion_procesos` en Nexo. Su instalación es explícita; F5 no modifica el esquema.

Código: [Jesus-AM/tdv2](https://github.com/Jesus-AM/tdv2), rama `main`. [Archivos compartidos, exclusiones y revisión previa de publicación](docs/REPOSITORIO.md).

Actualización 2026-10-05: `responsable_ur_supervisor`, colaboradores locales para nivel 3, reservas por bloque, autoguardado parcial, SignalR y envío definitivo. Aplicar explícitamente **CollaborativeFormsAndSubmission** antes de usar esta versión. Véanse [captura y migración](docs/migracion/COLABORACION_ENVIO.md) y [configuración de Nexo/Entra](docs/migracion/CONFIGURACION_NEXO_ENTRA.md). Los cambios de fuente de Nexo no publican funciones automáticamente.

Ajustes de captura: Contexto sin Datos de la sesión, sin Imprimir, prioridad mediante Select, eliminación con diálogo y navegación jerárquica según Nexo. **Revisar y enviar** tiene su propia pestaña y usa requisitos del servidor. Se conservan históricos, propuestas pendientes y formatos enviados. La organización por etapas añade la migración indicada arriba.

Reservas y SignalR: entrar en un campo reserva automáticamente el registro y confirma su versión antes de aceptar cambios. Salir guarda y libera; las demás sesiones reciben el estado y pueden entrar directamente, sin botones de edición ni recarga. La fila ocupada muestra nombre y candado. El intervalo de 15 segundos ya no cancela lecturas. Los fallos temporales conservan lo escrito y se reintentan de forma limitada con el mismo recibo. Los errores de validación se corrigen en el campo; no hay comparación ni resolución manual de versiones. Un texto incompatible permanece visible sin sobrescribir la respuesta compartida. [Comportamiento y pruebas con dos sesiones desde Visual Studio](docs/migracion/RESERVAS_SIGNALR.md). Sin migración adicional.

Corrección 2026-10-08: códigos de proceso confirmados por el servidor, validación de borrador por registro, operaciones relacionadas atómicas y eliminación directa sin reserva previa. **Sin nueva migración**. [Causa reproducida, recuperación interna, evidencia y comandos](docs/migracion/EDICION_AUTOMATICA.md).

## Dónde modificar cada cosa

| Responsabilidad | Ubicación | Equivalente Laravel |
|---|---|---|
| Rutas, entrada HTTP, SignalR y respuestas | `tdv2/Controllers`; atributos `HttpGet/HttpPost/HttpPatch/HttpDelete` | routes + controllers + broadcasting |
| Entidades persistidas | `tdv2/Domain/Entities` | app/Models |
| Reglas de acceso, plantilla, validación y cálculo de avance | `tdv2/Domain` | reglas/modelos de dominio |
| Casos de uso | `tdv2/Services` | services/actions |
| DbContext y mapeos de tablas | `tdv2/Infrastructure/Tdv2DbContext.cs`, `Infrastructure/Configurations` | Eloquent + configuración de relaciones |
| Migraciones y snapshot | `tdv2/Migrations` | database/migrations |
| Microsoft, Nexo y fuentes SII/ILDA | `tdv2/Integrations` | clientes/conectores externos |
| Publicación de catálogos, cola y reservas | `tdv2/Synchronization` | jobs + servicios de sincronización |
| Sesiones, cifrado, autorización y auditoría | `tdv2/Security` | guards/policies/middleware |
| Composición del host y límites HTTP | `tdv2/Hosting`, `tdv2/Web` | providers + middleware |
| Pantallas y componentes | `ClientApp/resources/js/pages`, `Components` | resources/js |
| Opciones / credenciales locales | appsettings / User Secrets | config / .env |
| Pruebas separadas | `tests`, `ClientApp/tests` | tests |

Se reutiliza Infrastructure como carpeta de persistencia; no existe una segunda capa Data. EF se utiliza en formatos, áreas, colaboraciones, identidad local e inventario. El SQL especializado conserva bloqueos de filas, reservas y publicación/auditoría en una misma transacción. Nexo sigue siendo la autoridad de permisos y sus tablas no pertenecen al DbContext. La portada pública existente se conserva; las nueve pantallas siguen en React.

## Configurar y ejecutar

Requisitos: Visual Studio con ASP.NET y JavaScript, .NET SDK 10 y Node/npm compatible con el lockfile. PostgreSQL 18 y Edge son necesarios para la suite nativa, no para compilar.

Todos los comandos de esta guía se ejecutan **desde la raíz del repositorio**:

```powershell
# Cachés y herramientas de esta sesión dentro del destino.
$env:NUGET_PACKAGES = Join-Path (Get-Location) '.artifacts/nuget'
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.artifacts/dotnet-home'
$env:npm_config_cache = Join-Path (Get-Location) '.artifacts/npm-cache'
dotnet restore tdv2.slnx
dotnet tool restore
npm.cmd --prefix ClientApp ci --ignore-scripts
dotnet build tdv2.slnx
```

En Visual Studio: **tdv2 → Administrar secretos de usuario**. Se conserva UserSecretsId `0397954e-63d2-48b6-a581-a586951257cc`. Claves: `Microsoft:TenantId`, `ClientId`, `ClientSecret`, `PublicOrigin`; `ConnectionStrings:Tdv2`, `Nexo`, `Sii`, `Ilda`; `Synchronization:IldaEnabled`. Mantén `Microsoft:PublicOrigin=https://localhost:7136`. User Secrets es JSON local para desarrollo; no se copia al frontend ni al repositorio.

`WebApplication.CreateBuilder` carga appsettings, appsettings del ambiente y User Secrets en Development; entorno y argumentos tienen precedencia. La aplicación no importa DPAPI/Herd ni sobrescribe las ediciones. Las credenciales vigentes de TDV2 apuntan a la base recreada del servidor por decisión del usuario.

F5 inicia Vite y ASP.NET mediante el perfil compartido. Para trabajar con dos terminales:

```powershell
# Terminal 1: React con HMR
npm.cmd --prefix ClientApp run dev

# Terminal 2: ASP.NET en 7136, proxy /__vite/ y callback /connect
dotnet run --project tdv2 --launch-profile https
```

Abre https://localhost:7136. Cambia componentes en ClientApp y controladores/servicios en tdv2. Para ejecutar sin Vite:

```powershell
npm.cmd --prefix ClientApp run build
dotnet run --project tdv2 --launch-profile https-compiled
```

F5 y el arranque web **no migran ni administran PostgreSQL**. La aplicación atiende las solicitudes manuales ya aceptadas o solicitadas desde la pantalla, sin activar horarios ni crear sincronizaciones por abrir una página. La programación automática conserva el procesador externo explícito. Guía detallada: [arranque local](docs/ARRANQUE_LOCAL_WINDOWS.md).

## Migraciones EF Core

Usuarios que atiende utiliza selección múltiple y requiere la migración de datos `20261007195019_StructuredUsersServed`: limpia sólo el campo anterior de los borradores y conserva enviados y prioridades. [Comando de actualización, alcance y pruebas](docs/migracion/USUARIOS_ATENDIDOS.md). Aplicación institucional pendiente; F5 no ejecuta la limpieza.

EF es el único mecanismo vigente; historial `public."__EFMigrationsHistory"`. No usar el antiguo `--migrate` ni aplicar SQL 001–004. Esos archivos se conservan exclusivamente como fixtures en `tests/TDV2.NativeVerification/LegacySchema`.

```powershell
# Inspeccionar y comprobar que el modelo tenga su migración (sin conectar).
dotnet ef migrations list --no-connect --context Tdv2DbContext --project tdv2
dotnet ef migrations has-pending-model-changes --context Tdv2DbContext --project tdv2

# Después de editar entidades y mapeos; revisar Up, Down y snapshot.
dotnet ef migrations add NombreDelCambio --context Tdv2DbContext --project tdv2 --output-dir Migrations
dotnet ef migrations script --idempotent --context Tdv2DbContext --project tdv2 --output .artifacts/tdv2-migrations.sql

# Aplicar explícitamente ConnectionStrings:Tdv2 de User Secrets.
dotnet ef database update --context Tdv2DbContext --project tdv2 -- --environment Development
```

Equivalencias: `make:migration` → `dotnet ef migrations add`; `migrate` → `dotnet ef database update`; `migrate:status` → `dotnet ef migrations list`. Confirmar migración y snapshot junto con el cambio de modelo. Ejecutar siempre con **un solo operador**, sin actualizaciones concurrentes. La inicial exige una base vacía; las siguientes utilizan el historial EF existente. Revisar SQL y recuperación antes de aplicar sobre datos.

Antes y después de aplicar al destino autorizado, este verificador consulta sólo metadatos y contadores, en una transacción de lectura. Sustituir `SERVIDOR_TDV2` y `USUARIO_TDV2` por los valores del destino autorizado en User Secrets; la documentación pública omite esos identificadores:

```powershell
dotnet run --project tests/TDV2.NativeVerification -- --database-check --environment Development --target-host=SERVIDOR_TDV2 --target-port=5432 --target-database=tdv2_db --target-user=USUARIO_TDV2
```

Comprueba conexión/destino, TLS, permisos, tablas, historial, contadores y pausa. Si encuentra objetos ajenos se conservan y el resultado no es válido. La primera migración también rechaza una base ocupada sin historial EF. No usar `EnsureCreated`, `Migrate` ni DDL en Program.cs.

Migraciones iniciales: `InitialTdv2` crea las 14 tablas y `InitializePausedSynchronization` inserta únicamente la configuración pausada. Las pruebas verifican su equivalencia con el esquema anterior. Las dos unicidades nullable se expresan como índices UNIQUE; la relación de ejecución activa tiene un índice adicional.

Ambas migraciones están aplicadas en el servidor autorizado; repetir el comando no cambió esquema, historial ni contadores. [Evidencia de la aplicación](docs/migracion/evidencia-ef-servidor-verificacion.json) y [repetición](docs/migracion/evidencia-ef-servidor-repeticion.json).

La nueva `20261005135417_CollaborativeFormsAndSubmission` está pendiente de aplicación por el operador. Se verificó sobre datos sintéticos, sin convertir prioridades históricas ni enviar formatos institucionales. [Comandos de actualización y cambios de esquema](docs/migracion/COLABORACION_ENVIO.md#actualizar-desde-la-raíz).

Referencia del framework: [migraciones EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) y [proveedor Npgsql](https://www.npgsql.org/efcore/).

El preparador `scripts/Local-Tdv2.ps1 Prepare` conserva marcador, DPAPI y validación de clúster, y usa ahora `dotnet ef database update` sólo en su base aislada. Si esa base conserva el esquema SQL anterior, se detiene para revisión: no lo elimina ni lo adopta automáticamente. `Prepare-Transition.ps1` conserva restauración/diagnóstico de una copia autorizada; su antigua conversión SQL `-Apply` está retirada. La conversión de datos Laravel requiere una adopción EF revisada sobre esa copia.

## Probar

```powershell
dotnet build tdv2.slnx
dotnet run --project tests/TDV2.Verification --no-build --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms

# Con F5 ya iniciado: páginas anónimas, HTTPS y WebSocket HMR.
node ClientApp/tests/browser/visual-studio-startup.mjs
```

Ejecutar las suites .NET secuencialmente y con la depuración detenida para no bloquear DLL en Windows. Los verificadores .NET son ejecutables con salida no cero al fallar; no son suites descubiertas por `dotnet test`. La suite nativa crea un clúster desechable bajo .artifacts, verifica su identidad y lo detiene al terminar; las fuentes externas son sintéticas. Admite `-PostgresBin` y `-SkipBrowser`.

Los antiguos Test-UserSecrets/Test-Development corresponden al entorno aislado anterior y no validan el destino remoto vigente. La evidencia distingue memoria, PostgreSQL real, navegador y servicios institucionales.

## Publicar

Detén la depuración y el servidor Vite antes de publicar: en Windows, `npm ci` necesita reemplazar los archivos de node_modules.

```powershell
dotnet publish tdv2/tdv2.csproj -c Release -p:UseAppHost=false -o .artifacts/publish-maintenance
node ClientApp/tests/browser/publish-flow.mjs
```

El publish restaura/compila React e incluye wwwroot; producción ejecuta `dotnet tdv2.dll` sin Node/Vite. No incluye credenciales de desarrollo. Preparar configuración externa, llaves persistentes, HTTPS y aplicar las migraciones en una ventana explícita. [Publicación y operación Ubuntu](docs/PUBLICACION.md).

Pendientes institucionales: Microsoft/Graph reales, contratos y revocaciones de Nexo, conectividad SII/ILDA, eventual conversión autorizada de datos históricos, aceptación visual y operación Ubuntu. La sincronización automática permanece desactivada.
