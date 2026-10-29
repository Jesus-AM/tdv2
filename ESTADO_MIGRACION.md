# Estado de migración TDV2

## 2026-10-29 — Revisión de necesidad del vínculo Microsoft–Nexo

**Revisado:** los identificadores e índice único de `users` se reutilizan; no se justifica otra tabla por lectura de alias o duplicados Microsoft locales. El esquema anterior carece del ID canónico Nexo asociado permanentemente a `(tid,oid)`. Correo, sesiones, tokens, empleado y auditoría no son equivalentes. La tabla propuesta aporta ese vínculo; una columna nueva sería una alternativa que también requiere cambiar el esquema. Se mantiene `20261029163206_VerifiedInstitutionalIdentityLinks` pendiente, sin cambios al código productivo ni eliminación de trabajo anterior.

**Probado ahora:** compilación sin errores/advertencias y 23 comprobaciones de metadatos EF, parser UPN/SMTP, HTTP `/accesos`/Procedimientos y servicios de acceso de Competencias, con fuentes sintéticas en memoria. Incluye acceso exclusivo, ambos/ninguno, reasignación de correo, revocación y falla temporal. Cero conexiones y migraciones ejecutadas en esta revisión; no se reetiquetan las pruebas PostgreSQL anteriores como nuevas. [Inventario, justificación, operaciones exactas, alternativas y evidencia](docs/migracion/REVISION_VINCULO_IDENTIDAD_20261029.md).

## 2026-10-29 — Identidad canónica y alias Microsoft

**Implementado:** selección explícita de perfil Graph, correspondencia UPN/SMTP institucional única antes de roles, identidad estable `(tid,oid)` vinculada al `usuario_id` existente de Nexo y conservación del ID local. Se integra la aclaración del usuario: no descartar por `mail` distinto ni elegir por permisos; Chat/SIP no se interpreta como SMTP. Ambigüedades, vínculos contradictorios y fallas de comprobación no amplían acceso. Foto por objeto validado, autorización independiente y Mi espacio conservados. Nueva migración propia `20261029163206_VerifiedInstitutionalIdentityLinks`, sin modificaciones a migraciones previas ni DigitalDbContext.

**Verificación:** tipos/Vite y compilación completa de la solución correctos, 99 pruebas frontend, 116 de reglas/transporte, 53 PostgreSQL de identidad/reservas, 69 de Competencias, 73 de migraciones, seis de catálogos y recorridos de navegador (20 grupos Mi espacio, 24 Competencias). Repeticiones dirigidas: 16 de alias/captura/auditoría/accesos; última ejecución, 37 de identidad/OAuth/401 y cuatro de conservación de datos y auditoría correctas. Las suites se solapan, no representan casos adicionales independientes. Clústeres detenidos. Evidencias y comandos en la [guía de esta revisión](docs/migracion/IDENTIDAD_MICROSOFT_ALIAS_20261029.md). PostgreSQL/ASP.NET reales de prueba, Microsoft y publicaciones Nexo sintéticos; ninguna recepción institucional de actividad se afirma a partir de esos transportes.

**Pendiente operativo:** aplicar explícitamente la migración en TDV2 y conceder SELECT/INSERT, comprobar ClientId/tenant efectivos y consentimientos del registro, y verificar el caso real con los campos seleccionados de `/me`. Nexo no publica un vínculo estable Azure: la tabla propia conserva la acreditación obtenida en login; conflictos o usuarios fuera de las publicaciones requieren revisión institucional. No se tocó Azure, Nexo ni bases institucionales, no hubo push/despliegue. **SII_LECTURA_O_INTEGRIDAD permanece sin causa confirmada.**

Inicio: 2026-09-30. Última adaptación: **2026-10-29**. Proyecto convencional ASP.NET Core 10/EF Core/PostgreSQL/React implementado y verificado en el alcance descrito. **La aceptación institucional y el despliegue Ubuntu continúan pendientes; no se declara terminada la sustitución operativa de Laravel.**

## 2026-10-29 — Auditoría ampliada del costo de autoguardado

**Implementado:** arnés optativo con correlación por solicitud, estadísticas SQL por base/contexto, separación de preparación/login/verificación, carga con identidades distintas y muestreo de bloqueos. Sólo pruebas/documentación; no se modifican consultas, modelos, conexiones ni comportamiento de producción. Se conservan los cambios locales previos y la política de detener envíos tras un fallo.

**Probado:** 4.320 solicitudes de carga con PostgreSQL/Kestrel aislados (1/10/25 personas, dos pasadas sin pool y una con pool), cero errores. Las 35 sentencias de un PUT con cambios son TDV2 12 + diagnóstico 19 + Nexo sintético 4; incluyen transacciones, excluyen preparación y no equivalen a 35 escrituras. Documento idéntico: 32 sentencias, cero UPDATE; manual sin cambios y entrega: 44. Pooling agrega mantenimiento SQL, reduce la latencia y expone contención en el control global del reinicio; no se retira esa protección. Tipos/Vite/.NET correctos, 99 pruebas frontend, 10 casos dirigidos PostgreSQL, 22 escenarios de navegador y 16 manuales correctos. [Desglose, tiempos P50/P95, antes/después, fuentes, archivos y evidencia](docs/competencias-digitales/COSTO_AUTOGUARDADO_20261029.md).

**Conclusión y límites:** no hay evidencia de actualizaciones redundantes ni de una consulta de seguridad que deba eliminarse. La configuración de pool se alteró exclusivamente en el arnés; no se inspeccionó ni modificó la real. Autoguardados sin nuevos eventos Nexo, manuales con recibos únicos y entrega diferida conservada. Catálogos/usuarios sintéticos, sin TLS/red institucional ni aceptación Nexo real. Pendiente sólo si se requiere capacidad operativa: medir con volumen/red representativos y analizar el contrato de respuesta completo, sin debilitar versiones/generación. Clústeres detenidos, sin nuevas migraciones, reinicios reales, push o despliegue.

## 2026-10-29 — Autoguardado medido y recuperación explícita

**Implementado:** Competencias detiene envíos y temporizadores al fallar un guardado; selecciones posteriores, foco y conexión no reactivan ni acumulan operaciones. Guardar avance lee el estado real, conserva datos actuales y versiones, recupera una confirmación perdida sin reenviar la instantánea antigua y reactiva únicamente después de confirmarse. Se omiten PUT sin cambios reales. Indicador breve/estable y captura fluida; Nexo conserva cero eventos automáticos, confirmación manual idempotente y su cola de entrega. No se modifica persistencia de producción, paquetes, 401, esquema ni configuración.

**Probado:** TypeScript/Vite y ASP.NET Release correctos; 99 pruebas frontend, 22 escenarios de autoguardado controlado, 16 manuales, 66 casos de backend PostgreSQL aislado y 24 grupos de navegador integrado. El primer recorrido móvil tenía una expectativa errónea sobre un valor idéntico; se corrigió su precondición y la repetición integrada pasó. [Guía, sentencias/bytes/duraciones, archivos, resultados íntegros y capturas](docs/competencias-digitales/AUTOGUARDADO_POSTGRES_Y_RECUPERACION_20261029.md). Medición SQL: 35 sentencias de nivel superior y una actualización de encuesta por cambio; documentos idénticos sin actualización. El trigger de control se mide por separado. No se infiere optimización de base a partir de fluidez del navegador. Clústeres detenidos; ninguna comprobación pendiente en ejecución.

**Límites:** fuentes e identidades sintéticas; recepción Nexo institucional, latencia operativa y Ubuntu sin verificar. Sin migraciones nuevas, bases institucionales, reinicios reales, push ni despliegues. Esta política sustituye el reintento automático de captura de la entrada 2026-10-28; conserva la entrega diferida de auditoría.

## 2026-10-28 — Mi espacio y selector extensible de sitios

**Implementado:** Mi espacio y entradas genéricas conservan intención durante OAuth y resuelven en backend: ningún sitio muestra un estado claro; uno abre directamente antes de renderizar HTML/React; varios ofrecen **Elige un sitio**. Sólo opciones autorizadas, una por sitio; fallas temporales explícitas, sin inferir denegación ni entrada automática. Registro `PortalSite` con nombre, destino, orden, verificador real y ámbito de rutas; sitios futuros reutilizan selección y comprobación en URL directa. Retorno específico, representación, autorización, autoguardado y actividad manual conservados. Sin cambios de Nexo ni migraciones.

**Probado:** tipos/builds correctos, 99 pruebas frontend, 116 del verificador .NET, 18 casos funcionales de PostgreSQL aislado con repeticiones dirigidas del registro/representación correctas, 20 grupos Edge/ASP.NET/PostgreSQL y 14 con transporte simulado. Cero/uno/dos/cuatro/ocho sitios, deduplicación por sitio, revocación, errores, OAuth, URL directa, teclado, Atrás sin ciclo y escritorio/móvil. La intercepción OAuth del nuevo fixture requirió corrección antes del recorrido final; los informes preservan las interrupciones. [Archivos, resultados, capturas y comandos](docs/migracion/MI_ESPACIO_SITIOS_20261028.md).

**Pendiente/límites:** aceptación institucional y despliegue; Microsoft/Nexo y sitios adicionales de pruebas son sintéticos. Clústeres desechables detenidos; sin bases institucionales, push, despliegue ni cambios operativos nuevos. Las evidencias de la revisión anterior permanecen.

## 2026-10-28 — Acceso independiente y mensajes de guardado

**Implementado:** retirado el mensaje largo posterior a Guardar avance, conservando el indicador breve estable, autoguardado fluido y confirmación manual. Identidad Microsoft separada del permiso de cada aplicación; portal público, `/accesos` como elección genérica y retorno local permitido vinculado al state. Denegaciones específicas sin detalles internos, cambio de cuenta existente con CSRF y enlace a la otra aplicación sólo tras comprobarla. No se cierra sesión por carecer de acceso a una aplicación. Sin cambios en permisos, conexiones, instrumento, informes, cola, paquetes ni migraciones.

**Probado:** tipos y builds correctos; 97 pruebas frontend, 116 del verificador .NET y 20 de autenticación/OAuth/CSRF en PostgreSQL aislado. 55 casos anteriores de Competencias y 8 nuevos de acceso correctos (repetición dirigida tras ajustar un caso que excedía el límite de solicitudes OAuth). Matriz de ambas aplicaciones, una sola o ninguna, revocación y fallas temporales verificada mediante ASP.NET y publicaciones sintéticas. 48 grupos de navegador con transporte controlado y 23 recorridos de Edge/ASP.NET/PostgreSQL correctos, incluido acceso exclusivo, captura fluida, guardado manual e informe. Incidencias de los fixtures, archivos, resultados y capturas en la [guía de esta revisión](docs/migracion/ACCESO_POR_APLICACION_Y_MENSAJES_20261028.md).

**Pendiente/límites:** aceptación con Microsoft/Nexo reales y despliegue Ubuntu. Todas las identidades, vistas publicadas y capturas de las pruebas son sintéticas; no se consultaron bases institucionales ni se ejecutaron migraciones o reinicios institucionales, push o despliegue. Se conserva el trabajo pendiente de la revisión anterior.

## 2026-10-28 — Autoguardado de Competencias sin parpadeo ni bloqueo

**Implementado:** se separa el estado de autoguardado del bloqueo de operaciones explícitas. Las selecciones se actualizan inmediatamente mientras se guarda, con debounce de 1500 ms, un PUT en curso y conservación de ediciones posteriores. Reintento exacto ante respuesta perdida, sin rebasar conflictos de otra sesión. Indicador estable, sin mover foco/paso/scroll por autoguardar. Guardar avance, navegación y finalización esperan toda la captura y validación del backend; auditoría manual UUID/cola intacta. Sin modificaciones del servidor, paquetes, 401, instrumento, aviso, permisos, diseño general ni migraciones.

**Causa y medición:** el estado `busy` deshabilitaba todo el formulario durante cada solicitud y los mensajes alternantes desplazaban la sección. No se observaron remontajes ni renderizado costoso. Con 2000 ms controlados, antes había unos 2017 ms de bloqueo y 34,5/52,78 px de desplazamiento (escritorio/móvil); después ambos son cero, con foco y última respuesta conservados. JavaScript por selección permanece en pocos milisegundos. No se afirma haber acelerado la red institucional.

**Probado:** tipos y builds Vite/ASP.NET correctos; 95 pruebas frontend, 16 escenarios nuevos + 16 de guardado manual, 55 casos backend con PostgreSQL desechable y 22 grupos de navegador integrado correctos. Se probó continuar respondiendo tras un commit retenido, confirmar manualmente la última selección, recargar, errores/reintentos, conflictos entre sesiones, validación de bloques, inmutabilidad y auditoría sin duplicados. Cero errores JS. Clúster aislado detenido. [Archivos, mediciones antes/después, capturas, resultados y comandos](docs/competencias-digitales/AUTOGUARDADO_FLUIDO_20261028.md).

**Límites:** fuentes/autenticación Nexo sintéticas; no se midió recepción o latencia institucional ni se ejecutó en Ubuntu. Sin bases institucionales, push o despliegue; no requiere cambios operativos de permisos, secretos o migraciones.

## 2026-10-28 — Actividad Nexo solo por guardado manual

**Implementado:** autoguardados de perfil/respuestas y transiciones conservan persistencia/bitácora local, sin eventos de guardado en la cola Nexo. «Guardar avance» espera lo pendiente y confirma una operación UUID mediante `POST /competencias-digitales/api/encuesta/guardar`, sin modificar respuestas ni versiones para auditar. Confirmación protegida por permiso de Encuesta, CSRF, sesión real, consentimiento, generación y versión. Doble clic/reintento no duplica; representación y definitivas rechazan la operación. Cola, recibos, entrega aplazada y demás eventos intactos. No hay migraciones, configuración ni permisos nuevos; paquetes, 401, instrumento y presentación conservados.

**Probado:** tipos/build React y ASP.NET Release correctos; 95 pruebas frontend; 55 casos ASP.NET/PostgreSQL desechable, incluidos siete nuevos; 16 casos nuevos de navegador sintético y 21 grupos integrados correctos tras corregir una expectativa de altura natural del arnés visual. Cero eventos automáticos en cola/registro central sintético, manual sin cambio de revisión, concurrencia/reintentos, fallo/recibo perdido de Nexo, rollback y restricciones comprobados. [Archivos, contrato, resultados, incidencias y límites](docs/competencias-digitales/GUARDADO_MANUAL_ACTIVIDAD_20261028.md).

**Límites:** contrato SQL real extraído del código local de Nexo, ejecutado sobre esquema/identidades sintéticos; no acredita recepción institucional. Sin acceso a bases existentes ni cambios en Nexo. Los clústeres desechables se detuvieron. Se conservan los pendientes operativos anteriores de la cola; este ajuste no requiere otros. No se eliminaron eventos históricos, ni se hicieron push o despliegues.

## 2026-10-28 — Recuperación de cabecera azul de Competencias

**Implementado:** fondo `#213455` en cabecera y navegación, con SVG blanco oficial copiado sin alteraciones a `/images/logos/transformacion-digital-blanco.svg`. Texto blanco, indicador activo amarillo y foco legible. Se conservan las dos filas, cuenta/fotografía, representación, Noto Sans, menú recibido de Nexo, cuerpo y pie aprobado. Cambios productivos limitados a CSS de cabecera, URL del logo y recurso nuevo; se preservan los demás cambios locales. Sustituye únicamente la barra blanca de la entrega anterior.

**Probado:** TypeScript, frontend, ASP.NET Release (cero advertencias/errores) y paquete local correctos. Once grupos de navegador con bundles y once con Vite, de 320 a 1920 px; participantes y cinco combinaciones adicionales de módulos. Contraste, teclado, cuenta, representación, proporción/carga del SVG, alineación y ausencia de desbordamiento comprobados. SVG idéntico al original y al publicado por SHA-256, servido como `image/svg+xml` en transporte estático aislado. Cincuenta y nueve archivos protegidos idénticos al inicio, incluidos paquetes, 401, instrumento y auditoría. [Archivos, capturas, resultados y límites](docs/migracion/CABECERA_AZUL_COMPETENCIAS_20261028.md).

**Límites y pendientes:** identidades/transporte sintéticos, Edge con tamaños emulados; no acredita Nexo/Microsoft ni PostgreSQL real. Se conserva el aviso de obsolescencia HMR del arnés Vite. No hay configuración, permisos ni migraciones nuevos. Sin acceso a bases institucionales, reinicios, push ni despliegue; la propuesta de una sola fila no se implementó.

## 2026-10-28 — Cabecera y pie compartidos, acceso al juego

**Implementado:** bloque público con «¿Qué tanto sabes de Microsoft 365?» y «Jugar Ahora», sin subtítulo; encuesta principal conservada. Competencias usa el SVG oscuro oficial de Transformación Digital sobre la barra blanca del portal, con texto/estados azules y cuenta existente. Pie completo compartido (UACJ, DGTI, Sistemas, Tecnología Educativa, Operación y Redes, Inteligencia de Datos), alineado al contenedor de 1400 px y adaptable. No cambia el orden publicado por Nexo ni la lógica de permisos/captura/auditoría.

**Probado:** TypeScript, build React y ASP.NET Release correctos; 116 pruebas de backend sin base; 51 grupos de portal/juego/TDV2; 11 grupos de Competencias con bundles y 11 con Vite, escritorio/tableta/móvil, sin errores JS. Contraste, recursos, teclado, cuenta/representación, pies con contenido corto/largo y ausencia de desbordamiento comprobados. Cincuenta y seis archivos protegidos idénticos por hash al inicio de esta corrección, incluidos instrumento, actividad, migraciones, 401 y paquetes. [Archivos, evidencia y comandos](docs/migracion/CABECERA_PIE_JUEGO_20261028.md).

**Límites:** transporte e identidades sintéticos, sin PostgreSQL ni conexiones institucionales en esta revisión visual. No nuevas migraciones, reinicios, push ni despliegue. No hay ajustes operativos nuevos; los pendientes de la integración de actividad de la revisión anterior permanecen.

## 2026-10-28 — Edición de instrumento y entrega de actividad de Competencias

**Implementado:** once perfiles laborales y cuatro funciones; correspondencias explícitas con las reglas anteriores y aplicabilidad de Personal Académico. Retiradas Bookings, Viva Connections/Org Explorer, Viva Learning y Seguridad en Microsoft 365 de la edición nueva (80 herramientas; Ciberseguridad conservado). Actualización explícita y versionada de borradores con archivo exacto anterior, perfil por revisar y respuestas vigentes conservadas; no hay modificación masiva ni reapertura de definitivas. Aviso `.2`, diseño, menú y fórmulas conservados. `OperationAudit`/`AuthenticationAudit` eran locales: nueva entrega al contrato real `nexo_a{ApplicationId}_registrar_actividad(jsonb)` con cola, UUID, recibos, reintentos y distinción de actor/afectado. Comando explícito `--deliver-digital-activity`. Nueva migración exclusiva `20261028175912_DigitalActivityDelivery`; originales intactas. Paquetes y corrección de identidad/401 conservados.

**Probado:** TypeScript, build React/ASP.NET; 116 pruebas existentes sin base, 95 de frontend, 49 de edición/metodología y 24 de informe/aviso/menú. Navegador con proyecciones reales y transporte sintético: edición en 1440/768/390 px y 32 grupos de regresión del informe. **48/48** con ASP.NET/PostgreSQL desechable, incluida función SQL real extraída del código Nexo sobre esquema sintético y reintentos/recibos. **20 grupos** de navegador integrado, cero errores JS. Publicación local aislada correcta con instrumentos/bundle comprobados por hash. [Archivos, comandos, capturas y límites](docs/competencias-digitales/INSTRUMENTO_ACTIVIDAD_20261028.md).

**Pendiente operativo:** revisión institucional de la correspondencia explícita de perfiles, aplicar sólo la nueva migración del diagnóstico, permisos mínimos de cola y verificar función/cuenta/publicación reales en Nexo. La prueba de su SQL se realiza en esquema desechable sintético; no acredita recepción institucional. Sin acceso a bases existentes, migraciones/reinicios reales, push ni despliegues. Windows es el entorno de prueba; Ubuntu queda para verificación operativa posterior.

## 2026-10-27 — Informe personal organizado, aviso nuevo y orden publicado por Nexo

**Implementado:** resumen/recomendaciones/fases y detalle con una tabla por bloque; selector y fichas en móvil. Estado inicial sin informe cuando sólo hay perfil/autocompletado/pending; preliminar para respuestas reales, cero válido e informes definitivos persistidos sin recalcular. Aviso literal del usuario y URL visible, versión `uacj-digital-aviso-2026-10-27.2`, conservando registros anteriores y aceptación explícita vigente para captura. Menú sin grupos, preservando el orden de Nexo (`m.orden,m.id`) y las autorizaciones existentes; retirado el orden fijo del controlador.

**Probado:** TypeScript, frontend y ASP.NET Release correctos. 116/116 pruebas existentes sin base; 24/24 nuevas de proyección/aviso/orden. Navegador con bundles reales/proyecciones del backend: 32 grupos en 1440/768/390/320 px; menú por seis conjuntos de concesiones en otros cuatro anchos, reportes y foco conservados. Noto Sans local efectiva comprobada, brechas #F0BD10, sin errores JS/desbordamiento. Capturas revisadas y SHA-256 de seis archivos protegidos intactos. Procesos de pruebas terminados.

**Pendiente y límites:** operador debe colocar Posición en menú: Encuesta 1, Informe 2, Avance 3, Resultados 4, Configuración 5. Se respeta lo que Nexo publique; sus posiciones reales no se consultaron ni modificaron. Aceptaciones verificadas mediante transporte sintético e inspección del mecanismo, sin volver a ejecutar persistencia PostgreSQL. Sin migraciones, reinicios, bases institucionales, push ni despliegues. Advertencias NuGet conocidas; actualización pospuesta por el usuario. [Archivos, capturas, comandos y límites](docs/migracion/INFORME_AVISO_MENU_20261027.md).

## 2026-10-27 — Corrección de menú, distribución del pie TDV2 y oración pública

**Implementado:** menú CD restituido a Avance/Resultados/Encuesta/Informe/Configuración, sin agrupación ni divisores, usando las concesiones ordenadas del backend. Ancho/cabecera/cuenta y demás mejoras conservados. Pie interno TDV2 con columnas de ancho natural, espacio disponible distribuido y filas adaptables, conservando letras, firmas y jerarquía; pies público/CD sin cambios. Retirada exclusivamente la oración solicitada del bloque del test.

**Probado:** tipos, frontend y ASP.NET Release correctos. 51 comprobaciones de portal/test/TDV2, ocho tamaños de pie, dos líneas completas por área y flujo corto/largo. Seis conjuntos de módulos sintéticos en cuatro tamaños, orden/visibilidad/foco correctos. Seis anchos adicionales en los límites de cambio de columnas sin desbordamiento. Comparación exacta de Razor confirma una única oración retirada; componente CD coincide con el estado anterior a la prioridad; once archivos protegidos intactos.

**Límites:** datos/identidades sintéticos, dispositivos emulados, advertencias NuGet previas. Pendiente comprobación con dispositivos/sesión institucional durante la fase operativa autorizada. No requiere cambios de Nexo; sin bases, migraciones, reinicios, push ni despliegues. [Archivos, capturas y pruebas](docs/migracion/CORRECCION_MENU_PIE_TDV2_20261027.md).

## 2026-10-27 — Navegación personal, ayuda del test y pie institucional interno (orden posteriormente revertido)

**Implementado:** orden visual Encuesta/Informe/Avance/Resultados/Configuración sobre los módulos ya concedidos, con grupos accesibles sin grupos vacíos. Contenedor 1400 px, cuenta y firmas conservados. Etiqueta «Aviso de privacidad» sin tocar cuerpo, versión ni consentimientos. Áreas con clave - nombre y sin nivel técnico junto al nombre; filtros, datos jerárquicos y alcances intactos. Portal distingue encuesta principal y test de práctica; ayuda interna desplegable con teclado, foco y Escape. Pie compartido del portal en layout autenticado y acceso restringido de TDV2; variante de Competencias conservada.

**Probado:** tipos, Vite y ASP.NET Release correctos. Siete anchos CD con Vite y siete con bundles; seis conjuntos de módulos autorizados sintéticos en cuatro tamaños, reportes y foco con menú completo. 34 comprobaciones portal/test/TDV2, incluidos progreso del juego conservado, XP/resultados originales, inicio vacío, acceso restringido y pies largos/cortos. 24 regresiones de tablas y cuatro de colaboración/Guardar/Enviar correctas. 37 recursos HTTP anónimos por ASP.NET TestServer. Sin errores JS ni desbordamiento horizontal del documento; capturas de escritorio/tableta/móvil revisadas.

**Límites y pendiente:** identidad/datos/alcances sintéticos y dispositivos emulados; no se acredita nueva integración institucional ni persistencia PostgreSQL. Permanecen advertencias NuGet/HMR anteriores. No se requiere cambiar el orden en Nexo: el subsitio ordena la navegación ya autorizada por sus rutas. Pendiente revisión con dispositivos/sesión reales en fase operativa autorizada. Sin bases institucionales, migraciones, reinicios, push o despliegue. [Archivos, mecanismo de Nexo, capturas y comandos](docs/migracion/NAVEGACION_TEST_Y_PIES_20261027.md).

## 2026-10-27 — Distribución de Competencias hasta 1400 px

**Implementado:** contenedor común centrado con márgenes adaptables; UACJ/cuenta alineados en una fila, navegación propia y botón de fotografía en móvil con nombre completo accesible/en el menú. Pie en cinco columnas cuando cabe, Dirección General y subdirecciones en filas adaptables cuando falta espacio. Párrafos con límite de lectura, sin cambiar colores, fuentes ni grosor de bordes. Solo estilos de Competencias y propiedad accesible opcional de su cuenta; portal, Procedimientos y lógica de captura conservados.

**Probado:** TypeScript, Vite y ASP.NET Release correctos. Siete anchos (1920–320 px) con Vite y otros siete con bundles, sin errores JavaScript: alineación medida, cuenta/teclado, perfil, preguntas, informe, representación, ausencia de desbordamiento y pie en páginas cortas/largas. Pasos bloqueados, foco pendiente, fallo de guardado y privacidad de lectura conservados. Diecinueve grupos de portal/juego y revisión interna escritorio/móvil correctos. Hashes de vista pública, estilos compartidos, tema y Survey conservados.

**Límites y pendiente:** proyecciones sintéticas, móvil/tableta emulados; pendiente comprobación con dispositivos físicos y sesión real en la fase operativa autorizada. Advertencias NuGet/HMR previas. No se accedió a bases ni se ejecutaron migraciones, reinicios, push o despliegues. [Archivos, medidas, capturas y comandos](docs/migracion/DISTRIBUCION_COMPETENCIAS_1400_20261027.md).

## 2026-10-27 — Corrección de regresión visual de Competencias en Vite

**Implementado:** dos enlaces estáticos de `ClientApp/index.html` marcados `vite-ignore`. Vite reescribía el CSS de ASP.NET a `/__vite/css/institutional.css`, que devolvía HTML: variables vacías, cabecera transparente y nombre/logotipo blancos sobre blanco. La imagen sí cargaba y no había ocultación por permisos/opacidad. Se conserva el CSS existente, comprobado por hash, y toda la lógica de la encuesta.

**Probado:** reproducción antes del cambio en 1366/390 px; cuatro recorridos corregidos con Vite real y cuatro con bundles en 1366/768/390/320. Inicio, cuenta, encuesta/controles, informe, pie, foco, fallo de guardado y representación sintéticos. Portal/juego: 19 grupos y revisión interna escritorio/móvil con Vite; 37 recursos HTTP con ASP.NET TestServer. TypeScript, Vite y ASP.NET Release correctos. Se añade cobertura que faltaba en la entrega visual anterior: transformación de recursos en desarrollo.

**Límites:** negocio/autenticación simulados, móvil emulado, sin conexiones institucionales; advertencias previas de NuGet y obsolescencia HMR. No se ejecutaron bases, migraciones, reinicios, push ni despliegues. [Causa, archivos y capturas](docs/migracion/REGRESION_VISUAL_COMPETENCIAS_20261027.md).

## 2026-10-27 — Identidad visual común, firmas y fotografía local

**Implementado:** tokens comunes de azul #213455, amarillo #F0BD10, gris #DADADA y superficies claras; Noto Sans local real 400/500/600/700 en portal, sistema interno, Competencias y juego independiente. Se conservan colores semánticos y presencia de editores. Firmas Noto con prefijo regular/nombre en negrita; orden público UACJ/DGTI/Sistemas/Tecnología Educativa/Operación y Redes/Inteligencia de Datos. Cabecera CD con UACJ/cuenta y pie DGTI más esas cuatro subdirecciones. Nueva foto original conservada y dos derivados locales, cover/encuadre adaptable y capa azul. Texto exacto del diagnóstico conservado. Sin cambios de lógica, esquema, conexiones, permisos ni migraciones.

**Probado:** TypeScript/Vite/ASP.NET Release; 95/95 frontend, 116/116 dominio/HTTP sin bases, 37 recursos anónimos por ASP.NET, portal/juego en escritorio/tableta/móvil, cuatro recorridos de Competencias con proyecciones sintéticas, 24 grupos de tablas/presencia/columnas fijas/autoguardado y cuatro de colaboración/Guardar/Enviar. Fuente realmente renderizada por CDP, contraste de portada, firmas completas, pies en páginas cortas/largas, nombre/foto, avatar y representación. Publicación local aislada con el target normal. Hashes de migraciones conservados; ningún reinicio, migración ni conexión institucional ejecutado.

**Límites y pendientes:** Avenirs LS STD no está disponible; se requiere archivo autorizado. Dos recorridos históricos de selectores fallan por expectativas retiradas (SIIv2 y Portal CAST); se conservan y documentan junto a las suites vigentes correctas. No se reejecuta PostgreSQL ni se acredita integración institucional en este ajuste de presentación. Móvil emulado y advertencias NuGet anteriores. [Inventario completo, recursos, evidencia y comandos](docs/migracion/IDENTIDAD_VISUAL_20261027.md). Esta entrada sustituye las decisiones tipográficas y de composición previas; la historia siguiente conserva su fecha y alcance.

## 2026-10-27 — Texto introductorio del diagnóstico público

**Implementado:** sustituido únicamente el párrafo por «Una fotografía honesta de su área en estas 8 dimensiones:». Actualizada la expectativa de texto del recorrido existente. **Verificado:** comparación del archivo antes/después confirma que título, marcado, estilos y demás contenido permanecen idénticos. Sin nueva ejecución de compilación/navegador por este cambio exclusivamente textual; sin bases, migraciones ni despliegue.

## 2026-10-27 — Ampliación de UACJ/DGTI en el pie público

**Implementado:** firma conjunta original ampliada de 400 a 480 px (+20 %), conservando proporción y alineación izquierda. Las cuatro subdirecciones conservan exactamente sus escalas; su grupo pasa a otra fila cuando no cabe junto a la firma principal, evitando dejar una aislada. Solo dos reglas CSS del pie público cambian. En móvil el recurso conjunto se limita al ancho disponible; no se recorta ni se fuerza el aumento fuera de la pantalla.

**Probado:** compilación Release y comparación visual Edge en ocho anchos, 1440–320 px. Firma principal de 480 px cuando cabe, subdirecciones al 28 % sin variaciones, orden, proporciones, alineación, flujo y pie sin desbordamiento. Resto del portal idéntico antes/después y Competencias intacto por hash. [Capturas y límites](docs/migracion/PORTAL_Y_PERFIL_20261027.md#ampliación-de-uacjdgti-en-el-pie).

**Límites:** móvil emulado; a 320/390 px la imagen ya ocupaba el ancho disponible y conserva ese límite. Persisten las advertencias NuGet y el desbordamiento previo externo al pie a 1024 px. Sin bases, migraciones, push ni despliegue.

## 2026-10-27 — Escala uniforme de las subdirecciones del pie público

**Implementado:** corrección posterior limitada a CSS del pie público. Se mantienen las firmas originales como imágenes, tomando Inteligencia de Datos como referencia. Anchos individuales de 153.44, 168.84, 134.12 y 154 px, correspondientes al 28 % de sus dimensiones naturales; no se estiran los nombres cortos ni se llenan contenedores de ancho común. Las cajas comparten alineación superior y altura suficiente para conservar las dos líneas y sus márgenes. Móvil apila las subdirecciones sin reducir sus letras. UACJ/DGTI, imágenes originales y pie de Competencias sin cambios.

**Probado:** compilación Release y revisión visual Edge en ocho anchos, 1440–320 px. Altura visible de la primera línea 10.92–11.20 px y desviación de alineación menor a 0.31 px, respetando los márgenes originales. Proporciones, orden, flujo de página y ausencia de desbordamiento del pie comprobados; elementos exteriores idénticos antes/después. [Evidencia y detalle](docs/migracion/PORTAL_Y_PERFIL_20261027.md#escala-uniforme-de-las-cuatro-subdirecciones).

**Límites:** móvil emulado; conserva el desbordamiento previo ajeno al pie a 1024 px y advertencias NuGet anteriores. Sin bases, migraciones, push o despliegue.

## 2026-10-27 — Pie público en una franja (ajuste anterior)

**Implementado:** únicamente CSS del pie en `tdv2/Views/Home/Index.cshtml`. Firma original conjunta UACJ/DGTI al inicio izquierdo del contenedor, seguida de las cuatro subdirecciones en una franja cuando hay espacio; ajuste a varias filas sin reordenar. Sin recortar ni modificar imágenes. Conserva fondo azul, firmas blancas y pie al fondo mediante flujo normal. Pie de Competencias intacto por hash.

**Probado:** compilación ASP.NET Release y comparación visual Edge antes/después en 1440, 1366, 1280, 1100, 1024, 768, 390 y 320 px. Una franja desde 1100 px, proporciones originales, orden, alineación izquierda, pie sin desbordamiento y estados breves/largos correctos. Geometría y estilos de todos los elementos externos al pie sin cambios. Evidencia en `.artifacts/public-footer-strip-20261027/` y [documentación visual](docs/migracion/PORTAL_Y_PERFIL_20261027.md#ajuste-posterior-pie-público-en-una-franja).

**Límites:** móvil emulado y Razor de TestServer sin bases. A 1024 px se conserva un desbordamiento previo externo al pie; no se alteró por el alcance limitado solicitado. Sin bases, migraciones, publicación ni despliegue. Persisten las advertencias NuGet anteriores.

## 2026-10-27 — Portal público y organización del perfil

**Implementado:** perfil en Datos institucionales, Perfil laboral y Uso de herramientas e información, sin cambiar IDs, respuestas, resolución institucional, autoguardado ni validación. Orden de teclado/foco coherente y unidades completas. Pies de Competencias/portal al fondo mediante contenedor flexible, sin posición fija. Portal Noto Sans local 400/500/600/700; contacto y textos solicitados; encuesta principal y juego secundario separados; tres PDF originales públicos y pie azul con firma completa UACJ/DGTI antes de las cuatro subdirecciones. Competencias conserva solo las cuatro firmas y aviso desde cuenta. Sin cambios de instrumentos, fórmulas, permisos, conexiones ni migraciones.

**Probado:** TypeScript, Vite, compilaciones ASP.NET Release, 95 pruebas frontend, 30 recursos HTTP anónimos, 20 grupos de navegador con ASP.NET/PostgreSQL aislados. Perfil, persistencia, secuencia, fallos, definitivas, cuenta/foto, representación y permisos conservados. Portal/juego en 1366/390/320 px, cuatro pesos Noto reales, Inter del juego y Roboto interno; PDF/MIME, textos, enlaces, pies breves/largos y seis partidas originales. La prueba de reinicio se omitió expresamente; no se ejecutó ningún reinicio. Evidencia y comandos en [Portal y perfil](docs/migracion/PORTAL_Y_PERFIL_20261027.md).

**Límites y pendientes:** móvil emulado; Microsoft/Nexo/catálogos sintéticos. Publicación local aislada por bloqueo Windows de un binario Node, sin cerrar procesos del usuario; dependencias locales restauradas. Persisten advertencias NuGet anteriores. No se validó integración institucional real ni Ubuntu. Publicación, comprobación del dominio/proxy y pendientes administrativos previos corresponden a una fase posterior. Sin cambios institucionales, secretos, push o despliegue. Las entradas siguientes describen entregas anteriores: el portal ahora usa Noto y dispone de ambos accesos separados.

## 2026-10-27 — Reinicio administrativo, informe y pie de Competencias

**Implementado:** excepción administrativa global a la encuesta única. Configuración prepara un resumen con conteos, opción de metas desmarcada, caducidad de cinco minutos y operación UUID; ejecutar exige `REINICIAR ENCUESTAS`, administrador real de esta aplicación, módulo vigente, sesión y CSRF, fuera de representación. Se limpian encuestas completas/en captura —incluidos perfiles, respuestas, contexto e informes— y todas sus confirmaciones; metas solo por opción explícita. Identidades, Nexo, catálogos, TDV2, auditorías y migraciones se conservan.

Nueva migración **`20261027131404_AdministrativeDigitalReset`**, exclusivamente DigitalDbContext: control persistente de generación/revisión, recibos/auditoría y funciones SQL de preparación/ejecución con permisos restringidos. `TRUNCATE` transaccional de tablas calificadas explícitas, sin CASCADE ni permisos generales de truncado para la cuenta web. El trigger original de inmutabilidad permanece intacto. No hay secuencias numéricas de captura: UUID y referencias institucionales conservan sus tipos. Bloqueo global coordinado con guardado/finalización y validación de generación impiden restaurar capturas antiguas; reintentos de una operación completada devuelven el recibo sin borrar datos nuevos. La revisión invalida resúmenes ante cualquier cambio; un fallo SQL revierte limpieza, generación y finalización de auditoría. Frontend invalida vistas mediante comprobación de generación, foco y aviso entre pestañas.

Informe personal recuperado por bloques: resumen/índice, cinco tarjetas, distribuciones/leyenda, brechas ámbar, recomendaciones, tres fases y cinco tablas en dos columnas o una en móvil, sin desplazamiento vertical interno. Usa el informe e instrumento persistidos; fórmulas intactas. Retirados botón, endpoint y generador HTML personal; CSV agregado conservado. Pie azul `#213455` con solo las cuatro firmas blancas existentes, y aviso de lectura trasladado al menú de cuenta. Encabezado, identidad/foto, permisos, representación de consulta y secuencia obligatoria conservados. Se corrigió además el tamaño de los inputs MUI dentro del subsitio, observado al revisar Configuración en móvil.

**Probado:** **43/43** grupos de diagnóstico con ASP.NET/PostgreSQL 18 aislado (incluyen ocho grupos de reinicio); **21/21** recorridos Edge; **38/38** regresiones de OAuth, sesión, fotografía y representación; **95/95** pruebas frontend. Tipos TypeScript, Vite y compilación Release correctos; modelo EF sin cambios pendientes y ambas migraciones aplicadas/repetidas únicamente en bases desechables. Comprobados roles/representación/CSRF, confirmación incorrecta/vencida/de otro actor, cambio de datos/metas, limpieza completa, metas opcionales, UUID/sin secuencias, catálogo/TDV2/Nexo conservados, auditoría/historial, rollback provocado, doble envío, respuesta de red perdida, solicitudes antiguas y las dos ordenaciones de concurrencia (guardado antes o después del reinicio). Nuevo intento vacío desde Perfil y protección definitiva normal verificados.

**Evidencia:** [reportes y capturas](docs/competencias-digitales/GUIA.md#archivos-y-verificación-de-esta-revisión) en `docs/competencias-digitales/evidencia/reinicio-20261027/`. Originales: `.artifacts/native-postgres-20261027-133053-b6899b0a` (43 backend), `native-postgres-20261027-133734-47d30fe8` (21 UI), `native-postgres-20261027-133734-8f8cd1ee` (38 regresiones). Los tres PostgreSQL se detuvieron. Comparación visual del HTML y del informe a 1366/390/320 px; diálogo destructivo en escritorio/móvil, foco/teclado, menú de cuenta, pie y tablas revisados. Se conserva SQL incremental generado sin conexión en `.artifacts/competencias-reset-incremental.sql`, solo para revisión; EF continúa siendo el migrador único.

**Incidencias resueltas:** la expectativa inicial de descarga retirada se ajustó al fallback 501 existente, sin alterar el enrutamiento general; no produce archivo. Una interacción de Configuración durante la recarga posterior al reinicio podía perder el diálogo recién abierto: ahora el cierre espera la recarga antes de liberar controles. Capturas finales sustituyen esos intentos intermedios. Cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18; avisos Vite de fuentes resueltas por ASP.NET, comprobadas en navegador. La suite histórica completa y sus 15 fallos previos no se declaran resueltos.

**Pendiente institucional:** respaldo verificado, propietario protegido distinto del rol web, aplicación explícita de la nueva migración y GRANT mínimos, actualización coordinada de clientes/backend, revisión operativa y aceptación con infraestructura real. [Guía de permisos, respaldo, recuperación y comandos](docs/competencias-digitales/GUIA.md#reinicio-administrativo-informe-y-pie--2026-10-27). Pruebas con identidades/Microsoft/Nexo sintéticos, PostgreSQL y HTTP locales reales, móvil emulado; faltan dispositivos físicos/otros motores/lector de pantalla. La migración inicial y el HTML conservan sus hashes anteriores. Sin reinicio real, conexiones institucionales, cambios de secretos, Nexo, push ni despliegue.

## 2026-10-27 — Ajuste de presentación y secuencia de Competencias digitales

**Implementado:** recuperación de la presentación del HTML: Noto Sans local y aislada, firma UACJ/DGTI, cuenta a la derecha, menú horizontal autorizado, banda de título, pasos numerados, progreso, distribución de perfil, tarjetas/rúbricas, botones y pie con las cuatro subdirecciones. Alturas de cabecera/progreso medidas para desplazamiento y foco en escritorio/móvil. El aviso del pie abre lectura, sin aceptar ni modificar el borrador. Original de Downloads y copia documental intactos.

Menú de cuenta extraído a `AccountMenu.tsx` y reutilizado por Procedimientos y Competencias. Conserva fuente de nombre/fotografía, iniciales, logout y cambio de cuenta. `/user/photo` utiliza el contexto y autorización de Competencias para su participante exclusivo; mantiene comprobación Graph de identidad y separación de aplicaciones. En representación, cuenta original y efectiva permanecen identificadas, sin mezclar fotografía ni conservar privilegios y con salida visible. Sin permisos nuevos Microsoft/Nexo.

Secuencia Perfil → Microsoft 365 → Ciberseguridad → Gobernanza → SII → IA calculada por `DigitalInstrument` desde el estado persistido. Cada paso exige anteriores completos y guardados; páginas, parámetro `paso` y API aplican la misma regla. `DigitalSurveyService` comprueba cambios reales por bloque bajo bloqueo y versión; admite respuestas posteriores conservadas, las mantiene al retroceder y recalcula bloqueos cuando un paso anterior queda incompleto. Cero, No aplica, complementarias y perfil docente conservan metodología. Guardado fallido no avanza; reintento idéntico tras respuesta perdida es idempotente. Navegación a otros módulos espera guardado sin exigir terminar la sección. Encuesta definitiva, consentimiento, alcances y representación de consulta permanecen protegidos.

Se conservaron los cambios locales previos. **La migración `20261027070211_InitialDigitalDiagnosis`, que el usuario informa aplicada en `competencias_digitales_db`, permanece intacta; no se agregó migración ni se accedió a esa base.** Las indicaciones de aprovisionamiento de la entrada inicial siguiente son históricas y no ordenan repetir acciones ya realizadas. Archivos modificados, detalles y diferencias necesarias con el prototipo en la [guía actualizada](docs/competencias-digitales/GUIA.md#ajuste-anterior-presentación-cuenta-y-avance-por-secciones--2026-10-27).

**Probado en esta revisión:** **35/35** grupos del diagnóstico y **38/38** regresiones compartidas de OAuth, sesión, fotografía y representación, con PostgreSQL 18 desechable; **19/19** recorridos Edge con ASP.NET/PostgreSQL aislado, sin errores JS; **95/95** pruebas frontend. Tipos TypeScript, Vite y build Release del verificador/aplicación correctos. Cubiertos seis campos del perfil, cada bloque incompleto, saltos por URL/API/pasos, aplicabilidad, cero/NA/complementarias, foco, reintentos antes/después de commit, recarga/otro cliente, solicitudes obsoletas, retroceso sin pérdida, finalización inmutable, cuenta exclusiva, foto/iniciales/identidades, permisos del menú y aviso de lectura.

**Evidencia visual:** comparación del HTML y la aplicación a 1366/390/320 px para perfil y a 1366/390 px para preguntas; cabecera, menú, rúbricas, progreso, botones y pie revisados. Noto real comprobada con CDP y ausencia de desbordamiento/controles ocultos verificada en navegador. [Reportes y capturas finales](docs/competencias-digitales/GUIA.md#verificación-y-evidencia-del-ajuste) conservados en `docs/competencias-digitales/evidencia/ajuste-20261027/`. Ejecuciones originales: `.artifacts/native-postgres-20261027-102241-37965cd7` (35), `native-postgres-20261027-102527-a23ef494` (19), `native-postgres-20261027-100807-ed8e4c4b` (38); servidores de prueba detenidos.

**Comandos:** `npm.cmd --prefix ClientApp run types:check`, `run build` y `run test:forms`; `dotnet build tests/TDV2.NativeVerification -c Release --no-restore`; `scripts/Test-NativePostgres.ps1 -DirectStart -DigitalOnly -SkipBrowser -NoBuild` y `-DirectStart -DigitalOnly -BrowserOnly -NoBuild`. La guía conserva invocaciones PowerShell completas, Node utilizado y selección exacta de las 38 regresiones.

**Límites y pendiente:** fuentes Microsoft/Nexo/empleados sintéticas; sin nueva comprobación institucional ni encuestas reales finalizadas. Fotografía de prueba sintética y móvil emulado; pendientes dispositivos físicos, otros motores y lector de pantalla. Cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18; dependencias intactas. No se repitió toda la suite histórica ni se declaran corregidos sus 15 fallos ya documentados. La configuración institucional pendiente se debe contrastar con lo que el operador ya completó. No hubo cambios de secretos, conexiones, Nexo, bases institucionales, push o despliegue.

## 2026-10-27 — Entrega inicial de Competencias digitales UACJ

**Implementado:** los cinco módulos solicitados, en sus rutas definitivas y orden: Avance por dependencia, Resultados generales, Encuesta, Mi informe y ruta y Configuración. Se conserva el stack y se aísla el estilo. El botón público Iniciar test enlaza al subsitio; las entradas anteriores sobre abrir el quiz describen el comportamiento sustituido. Original HTML preservado y recursos incorporados sin dependencias del entorno de autoría. No había cambios pendientes al comenzar.

Autenticación institucional compartida con retorno profundo; autorización Nexo separada por aplicación, conexión y módulos. Participante independiente de Procedimientos. Supervisor institucional sin Configuración; responsables N2/N3 solo sus ramas válidas; otros niveles personales. Perfil institucional sin edición, procedente del contexto validado de Nexo y del catálogo local SII, con distinción entre No aplica e información incompleta. Sin réplica ni sincronizador nuevo.

`DigitalDbContext`, tres tablas y migración propia `20261027070211_InitialDigitalDiagnosis`; historial `__CDMigrationsHistory`, sin fallback a TDV2. Encuesta única por empleado textual, consentimiento por versión, evaluación central de 84 herramientas/1008 indicadores, seis preguntas editables del perfil, control de versión, bloqueo transaccional por empleado, finalización idempotente y protección SQL de respuestas definitivas. Instrumento, perfil, adscripción e informe se conservan al finalizar. Representación Nexo existente adaptada a la aplicación, solo consulta, sin tokens en navegador, con inicio/fin auditados y salida siempre disponible. No se añade reapertura ni reinicio.

Agregados calculados tras aplicar el alcance y filtros; Resultados utiliza solo finalizadas. Metas directas por UR sumadas una vez, sin porcentajes con cobertura incompleta; índices por participantes únicos, nulos diferenciados de cero. Exportación agregada sin identidad ni respuestas personales. Configuración exclusiva del administrador: representación, metas y estado del catálogo. Comandos de TDV2 actualizados con `--context Tdv2DbContext`; migraciones anteriores intactas.

**Probado:** tipos TypeScript, Vite y compilaciones Release de aplicación/verificadores correctos (0 errores/advertencias .NET). 94/94 pruebas frontend y 116/116 de dominio/HTTP. PostgreSQL 18 aislado: **28/28 grupos nuevos** y **14/14 recorridos Edge**, **50/50 regresiones** de OAuth, Nexo, alcance, guardado, sesiones, representación y auditoría; **67/67 comprobaciones EF** de TDV2, con sus trece migraciones intactas. Además, 12 grupos del portal/juego histórico, Inter y Roboto reales y cero errores JS. Vite conserva el aviso de resolución de Roboto en runtime; la fuente local se comprobó en navegador.

Los recorridos nuevos incluyen rutas directas/recargas, perfil y aviso accesible, valor Inicial cero, rúbricas, guardado fallido sin éxito falso, finalización incompleta/definitiva, descarga propia/agregada, metas, representación de consulta/salida incluso desde otra aplicación, permisos por rol y escritorio/móvil emulado de 390/320 px. Inspección visual de avance, perfil, resultados y ramas N2/N3; corregidas etiquetas largas superpuestas en móvil. Fuentes Microsoft/Nexo sintéticas; HTTP, navegador y persistencia PostgreSQL reales. Esto no acredita integración institucional.

**Evidencia:** `.artifacts/native-postgres-20261027-080638-0cd2c68c/` (subsitio y capturas), `native-postgres-20261027-080649-1054b08f/` (50 regresiones), `native-postgres-20261027-074919-4f15df6f/` (67 EF) y `.artifacts/competencias-portal-20261027/` (portal, juego, fuentes y 116 dominio/HTTP). Reportes revisados y dos capturas conservados en [docs/competencias-digitales/evidencia](docs/competencias-digitales/evidencia/competencias-postgresql.json).

**Límite de la regresión ampliada:** permanecen **15 fallos históricos**, todos reproducidos con el mismo tipo de excepción en una copia intacta del código/pruebas de HEAD `27b5f248b376351a260d54cbd966d4a50f22f0c1`. La batería ampliada produjo 75 aprobaciones y 16 fallos; uno era el payload del adaptador antiguo de transporte y pasó después de actualizarlo al contrato de primera etapa (incluido en las 50 regresiones finales). Los otros 15 se documentan en la [comparación con HEAD](docs/competencias-digitales/evidencia/regresion-base.json): fixtures históricos de herramientas/avance, vínculos, usuarios, reserva de colaboradores y envío de etapas. No se declara que toda la suite histórica esté verde. Mantener esos escenarios requiere trabajo separado; las reglas productivas de Procedimientos no se alteraron para forzar su aprobación.

**Comandos vigentes de esta entrega:** `npm.cmd --prefix ClientApp run types:check`, `npm.cmd --prefix ClientApp run build`, `npm.cmd --prefix ClientApp run test:forms`; `dotnet build tdv2/tdv2.csproj -c Release -p:UseAppHost=false -p:NuGetAudit=false`; `dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false -p:NuGetAudit=false`; `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -DigitalOnly`; el mismo script con `-MigrationsOnly` para EF y `-SkipBrowser`/`TDV2_TEST_CASE_PREFIX` para la selección de regresión documentada en la guía. `-NoBuild` reutiliza un verificador Release compilado; no valida cambios C# pendientes. Node de la instalación existente se indicó mediante `TDV2_TEST_NODE`; no se instalaron dependencias nuevas.

**Pendiente institucional:** confirmar ID, claves reales de roles y rutas/grants de módulos publicados en Nexo; completar Configuración y capacidad de representación; aprovisionar base/usuario, configurar las tres claves nuevas y aplicar explícitamente solo `DigitalDbContext`. Revisar el aviso y sus afirmaciones/enlace, licencias y oferta de cursos antes de uso real. Validar luego con empleados/adscripciones reales autorizados, dispositivos físicos y lector de pantalla. No se han usado ni modificado bases institucionales, secretos, Nexo, publicación o destinos remotos; sin push ni despliegue.

La [guía específica](docs/competencias-digitales/GUIA.md) detalla archivos, contratos inspeccionados, permisos, metodología y correcciones explícitas del HTML, claves exactas de desarrollo/producción, comandos y lista para la fase posterior.

## 2026-10-19 — Etiqueta del botón del quiz

**Implementado:** el botón público ahora dice «Iniciar test». Su nombre accesible procede del texto; no tiene etiqueta separada. Clases, ID, atributos del diálogo y lógica del quiz intactos. Actualizadas las expectativas existentes de pruebas.

**Probado:** compilación Release de aplicación/verificador y suite de navegador del quiz correctas, incluido localizar el botón por su nuevo nombre accesible, abrir/cerrar, Escape y retorno del foco. Evidencia `.artifacts/quiz-button-20261019/`.

**Límites:** Edge con móvil emulado, Razor exportado desde TestServer y transporte interno sintético; sin bases, migraciones, push o despliegue. Dos advertencias NuGet previas. Sin pendientes de este cambio de texto.

## 2026-10-17 — Color, acabado y anclas del portal público

**Ajuste posterior de enlaces:** retirados los subrayados en normal/visitado/hover/foco sólo del portal; resaltado suave, iconos/flechas y contorno de teclado conservados. Build Release de aplicación/verificador, suite pública antes/después en cuatro tamaños y 16 recursos HTTP correctos. Inspección Edge de clics, Enter, foco y geometría estable en escritorio/móvil emulado; `mailto:` interceptado para no abrir correo. Dos advertencias NuGet previas; no se repiten tipos ni juego, sin cambios React o quiz. Evidencia `.artifacts/public-links-20261017/`; foto externa sigue inaccesible. Sin migraciones, push o despliegue.

**Implementado:** releído/renderizado el manual local actual. Primarios #213455/#FFFFFF/#DADADA; único complementario #CFD6E8 para preguntas abiertas e interacciones. Velo azul más presente y respaldo azul, modelo claro/sombras moderadas/etiquetas azules, seis iconos blanco sobre azul, ruta azul, enlaces sin subrayado conforme al ajuste posterior, hover/foco sin desplazamiento. Las tarjetas informativas dejan de elevarse. JS público mide la cabecera con ResizeObserver para el margen CSS de anclas, conservando navegación/historial nativos. Contenido Razor posterior a estilos idéntico; sin cambios de textos, tipografía, dimensiones, logos, jerarquía, internos o quiz.

**Probado:** TypeScript, sintaxis JS y solución Release; 316 elementos comparados en cuatro ventanas (1366/768/390/320), 172 registros de comprobación con 83 llegadas por anclas y cuatro cabeceras de sesión sintética. Inter real, teclado, hover/foco, preguntas múltiples, no desbordamiento, geometría/tipografía/contenido iguales. Contraste mínimo 8.55:1; hero 9.22:1 bajo extremos de fotografía. Quiz 9/9 grupos existentes, seis partidas completas; 16/16 estáticos HTTP ASP.NET/TestServer con políticas intactas. Capturas y reportes `.artifacts/public-finish-20261017/`. [Archivos, colores, pruebas, evidencia y comandos](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-del-portal--2026-10-17).

**Pendientes y límites:** Unsplash inaccesible; foto/URL conservadas, comparación visual con fotografía cargada pendiente. Logos y fuentes locales cargan. Ventanas/tacto emulados, sin equipos físicos u otros motores. HTTP con TestServer/dobles, navegador con Razor exportado; captura interna de la prueba del quiz usa transporte simulado. Sin PostgreSQL, Nexo real, OAuth institucional o paquete nuevo. Cuatro advertencias NuGet previas Pkcs 9.0.18. Sin migraciones, push ni despliegue; no cambia la condición no confirmada de RemoveUsedMedia.

## 2026-10-16 — Textos de Colaboradores

**Implementado:** ayuda «Busca una persona por nombre o correo para agregarla como colaboradora.», sin referencia a niveles. Retirados sin sustitución el pie sobre vigencia y el aviso general de asignaciones comprobables; se conserva el aviso propio de vista de prueba. Los dos avisos de Administrador también omiten niveles numéricos. Revisadas las reglas reales de responsables, Administrador, Local y dependencias: permisos, alcance, visibilidad, altas, retiros, autorización y vigencia no cambian. `CollaboratorService` sólo modifica tres literales. Sin cambios de distribución, estilos, fuente, contratos, bases ni migraciones; Origen sigue oculto. [Detalle](docs/migracion/COLABORADORES_VISIBILIDAD_ACCIONES.md#ajuste-de-textos-vigente--2026-10-16).

**Probado:** TypeScript, Vite y solución .NET Release correctos; **116/116 dominio/HTTP** y **9/9 grupos de navegador** existentes. ASP.NET/TestServer con dobles en memoria; Edge/React/MUI con transporte simulado, perfiles de responsables, ambos tipos de colaboración, alta/retiro, búsqueda, consulta, teclado y móvil emulado. Capturas revisadas en 1440/390 px; suite también cubre 768/320 px y zoom CSS 200 %. Evidencia nueva `.artifacts/collaborator-texts-20261016/`. Se actualizaron únicamente los avisos sintéticos de dos fixtures para reflejar el texto retirado. Diff sin errores; ocho archivos de código, fixtures y documentación modificados respecto al inicio.

**Límites y pendientes:** cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18; sin PostgreSQL/Nexo reales ni dispositivos físicos. Ningún pendiente de implementación de estos textos. No hubo migraciones, push ni despliegue.

## 2026-10-16 — Restauración visual de Procedimientos y Colaboradores

**Implementado:** reversión selectiva de los últimos cambios visuales de `Inicio`, `AreaDirectory`, `Colaboradores` y sus reglas CSS específicas. Procedimientos recupera el nombre arriba, la clave debajo con cantidades/estado, controles Expandir visibles/Contraer y botones anteriores. Colaboradores vuelve a su alta y listado previos, incluidas etiquetas y espaciados; sólo se elimina la presentación de Origen, Agregado por Administrador, delegación en TDV2 y la explicación de procedencia del retiro. Se conservan datos internos, asignaciones independientes, facultades N2/N3, búsqueda, altas, retiro y confirmación. Roboto local, portal, quiz, tablas de captura y cambios locales ajenos permanecen. Se aplicaron hunks del diff, sin reset ni sustitución indiscriminada de archivos.

**Probado:** TypeScript/Vite, **94/94 pruebas frontend**, **116/116 dominio/HTTP** (ASP.NET/TestServer con dobles en memoria, binario Release existente sin cambios de backend) y **16 grupos de navegador** (9 de ambas vistas, 3 del directorio previo y 4 de colaboradores/Guardar/Enviar). Búsqueda, filtros, pestañas, expansión/contracción, apertura de formatos, ambos tipos de alta con payload exacto, errores conservando selección, retiro/cancelación/pendiente/reintento y responsables N2/N3. Edge con React/MUI reales y transporte sintético; escritorio, móvil emulado, teclado, foco, menú, diálogo y zoom CSS 200 %. Roboto realmente renderizada con fuente web local en 400/500/600/700 y español. Capturas antes/después y reportes en `.artifacts/views-revert-20261016/`.

**Límites:** sin PostgreSQL ni Nexo institucional, dispositivos físicos o pruebas nuevas del portal/quiz. No se ejecutaron migraciones, publicación ni despliegue. Las verificaciones de la entrega siguiente son históricas, salvo las repetidas expresamente en esta restauración. [Detalle vigente, alcance y reproducción](docs/migracion/VISTAS_PROCEDIMIENTOS_COLABORADORES.md#restauración-visual-vigente).

## 2026-10-16 — Entrega anterior: ajustes de vistas y recuperación de Roboto interno

La distribución y los estilos descritos en esta entrada fueron revertidos por solicitud posterior. Permanecen Roboto local y la eliminación visual de Origen; los resultados siguientes corresponden a aquella versión.

**Implementado:** se conserva el trabajo del quiz/Inter pendiente de Git; la ejecución interrumpida no había cambiado Colaboradores. Historial confirma `Roboto, sans-serif`; restaurado en tema/estilos internos, con archivos locales variables originales y OFL para evitar el respaldo Arial observado en 500/600. Portal y quiz conservan Inter y sus recursos intactos. Procedimientos muestra CVE_UR original (incluidos ceros) junto al nombre, detalles debajo, estados reales diferenciados, nombres 17/600, resumen institucional o restringido según la proyección existente y Abrir formato con borde. Colaboradores reúne búsqueda/resultados y resumen/alta, muestra roles completos y asignaciones independientes en columnas/tarjetas; elimina sólo procedencia visible. Sin cambios de servicios, contratos, permisos, datos ni migraciones.

**Probado:** tipos/Vite/.NET Release; **94/94 frontend**, **116/116 dominio/HTTP** (incluidos diez casos de visibilidad y cuatro HTTP de Colaboradores), **15/15 recursos HTTP ASP.NET/TestServer**. Edge: **9/9 grupos nuevos**, tres del directorio anterior, 4/4 de colaboradores/Guardar/Enviar, 24/24 de tablas, 9/9 selectores y 9/9 del quiz. Ambos roles y responsables N2/N3 con datos sintéticos, alta/retiro/errores/pendiente/reintento, varias asignaciones, lectura, teclado, fuentes CDP y español, móvil emulado/zoom CSS 200 %, nombres completos, búsqueda/filtros y estados. Antes/después y reportes en `.artifacts/views-20261016/`. Portal/quiz/logos/migraciones conservan hashes previos.

**Límites e incidencias:** UI usa transporte HTTP/SignalR y mutaciones simulados; dominio/endpoints y recursos pasan por ASP.NET con dobles en memoria. Sin PostgreSQL ni Nexo institucional; no se acredita una nueva aceptación transaccional. Corregidos el recorte móvil del área autorizante y el contraste de selección; prueba antigua de portada actualizada porque seguía exigiendo las fuentes previas al quiz. Se ajustaron orden/esperas del verificador. Persisten cuatro advertencias NuGet previas de Pkcs 9.0.18. Unsplash bloqueado deliberadamente; fuentes locales sí cargan. No se reconstruyó el paquete ni se ejecutó Ubuntu; faltan aceptación institucional, dispositivos físicos, otros motores y lector de pantalla. Ningún pendiente de implementación de este alcance. Sin push, despliegue, cambios de base ni migraciones nuevas/ejecutadas; `RemoveUsedMedia` intacta, aplicación institucional no confirmada.

[Archivos, configuración de fuentes, matriz de roles, evidencia y comandos](docs/migracion/VISTAS_PROCEDIMIENTOS_COLABORADORES.md).

## 2026-10-16 — Entrega anterior: quiz Microsoft 365 e Inter local

**Implementado:** acceso público desde Diagnóstico de madurez digital, diálogo nativo/iframe local con Escape desde ambos documentos, cierre visible y restitución de foco/scroll. La portada Razor no disponía de diálogo público; no se carga el editor React. CSS/JS originales del quiz separados, sin alterar preguntas, lógica, XP, rachas, medallas, niveles ni diseño; adaptador de eventos/foco. `srcdoc` de recurso local fijo conserva `X-Frame-Options: DENY`; CSP del quiz restringida al mismo origen, sin relajar políticas productivas. Inter oficial 4.1 normal/cursiva 100–900 y OFL en `wwwroot/fonts/inter`, precarga y swap, tema MUI/estilos globales/portada actualizados. Sin Google Fonts, persistencia del quiz, autenticación nueva ni migraciones. Apariencia de columnas, grupos, motores de captura, jerarquía/logo UACJ e imágenes conservados.

**Probado:** tipos/Vite/Release y 94/94 frontend correctos; 11/11 recursos anónimos con ASP.NET/TestServer, MIME y cabeceras intactas; nueve grupos de comprobaciones del quiz (seis partidas completas y tres recorridos de diálogo) en Edge 1366/390/320 px. Cuatro niveles, errores/aciertos, rachas interrumpidas, 1900 XP con diez aciertos, medallas, ayuda, reinicio, Tab/Shift+Tab, Escape, foco/scroll, Inter realmente renderizada y español/pesos 400–900. 24/24 regresiones internas y 9/9 selectores con transporte simulado, incluyendo zoom CSS 200 %, permisos y autoguardado. Publicación completa en copia aislada, once recursos esenciales iguales al origen y nuevo recorrido con estáticos del paquete. Evidencia `.artifacts/quiz-inter-20261016/`; fuentes anteriores observadas Arial/Roboto/Segoe UI, posteriores Inter Variable web local.

**Límites e incidencias:** build de solución conserva cuatro advertencias previas NU1902/NU1903. Primer publish bloqueado por binario Node ajeno; dependencias reconciliadas desde caché sin cambiar lockfile y publish final en copia aislada con caché NuGet raíz. HTTP usa dobles de negocio en memoria; navegador público usa Razor exportado y servidor local, interior usa transporte simulado. Sin PostgreSQL ni inicio del host operativo publicado. Externos bloqueados para comparación (Unsplash y las fuentes Google anteriores); imágenes/URLs originales conservados. Pendientes de aceptación externa: dispositivos físicos, otros motores, lector de pantalla, Ubuntu/proxy institucional. No queda implementación pendiente del alcance; sin push, despliegue ni cambios de bases. Original de Downloads, migraciones y archivos de logotipos intactos.

[Integración, políticas, archivos, fuentes oficiales, evidencias y comandos](docs/migracion/QUIZ_MICROSOFT_365_E_INTER.md). Esta entrada conserva la evidencia de esa entrega. La revisión posterior mantiene Inter en portal/quiz y recupera Roboto en el sistema interno.

## 2026-10-16 — Reversión exclusiva de la delimitación de columnas

**Implementado:** restauradas las copias locales anteriores del CSS de tablas, `CaptureTableScroll` y `FormatoUR`, después de comprobar sus diferencias. Vuelven fondos, sombras suaves, anchos 124/112 px, espaciados y el encabezado de grupo con una celda completa. Se retiran los separadores interiores nuevos, celdas laterales vacías y adaptación móvil de 68 px; se conserva la excepción previa hasta 600 px que libera Edición horizontalmente y mantiene Acciones fija. Presencia, alta por procedimiento, acciones por registro, claves, reservas y autoguardado conservan su implementación anterior. Portada y compensación horizontal del PNG no se modifican.

**Verificación y límites:** tipos, Vite, 94/94 pruebas frontend, 24/24 recorridos de tablas y 9/9 de selectores correctos. Edge con React/MUI reales y transporte/identidades/reservas/persistencia simulados; escritorio, móvil emulado y zoom CSS 200 %. Desplazamiento, menús, eliminación con confirmación/asociaciones y autoguardado con nodo/foco/scroll conservados. Evidencia nueva `.artifacts/columns-revert-20261016/`; auditoría de alcance y límites en el [detalle de la reversión](docs/migracion/COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md#reversión-parcial-vigente). Se retira la prueba exclusiva de los separadores/encabezados de tres celdas porque exigía la apariencia descartada; su fuente e informes se conservan como evidencia histórica. Sin backend, PostgreSQL, migraciones, push ni despliegue en esta revisión; no se reutilizan las pruebas públicas anteriores como ejecuciones nuevas.

## 2026-10-16 — Entrega anterior: delimitación de columnas y alineación del logotipo

La delimitación interna descrita en esta entrada fue revertida por solicitud posterior; **sólo permanece vigente la alineación UACJ**. Los resultados siguientes documentan aquella versión.

**Implementado:** separadores interiores continuos de 1 px y fondos opacos en los laterales de encabezados/registros/grupos. Encabezados de Sistemas con Edición vacía, centro `colSpan=5` y Acciones vacía; franja sólo central, nombre completo y alta visibles entre los extremos fijos. Se conserva la acción/asociación del procedimiento y las acciones de cada registro. En contenedores hasta 600 px ambos laterales quedan fijos a 68 px; campos centrales adaptables, texto de presencia apilado y controles de selectores en línea propia sin reducir fuente. Se sustituyó la excepción móvil anterior que liberaba Edición. Altura del encabezado medida en unidades CSS para zoom. UACJ: compensación horizontal de -3.272 % por 96 px transparentes en el PNG de 2934 px; archivo, proporciones, tamaños, jerarquía, color, fondo y espacios verticales intactos.

**Probado ahora:** TypeScript/Vite/Release correctos; 94/94 frontend, 25/25 nuevas comprobaciones Edge, 24/24 regresiones de tablas y 9/9 de apertura/reservas. Pocas/muchas filas, grupos largos/vacíos/sin asociación, tres posiciones horizontales, límites/alturas, móvil 320/390 px, 768/1366 px y zoom CSS 200 %, alta asociada, confirmar/cancelar eliminación, menús, campos extremos de Sistemas e Identificación, dos actores, permisos, familias, guardar/recargar y autoguardado sin perder nodo/foco/scroll. Público antes/después en seis tamaños: 306 elementos, única diferencia geométrica la coordenada horizontal de UACJ; borde visible calculado desde alfa alineado a menos de 1 px, marca completa. Contraste público mínimo 8.05:1 y 84 entradas de estados. Build mantiene cuatro advertencias NuGet previas NU1902/NU1903. Evidencia nueva `.artifacts/fixed-columns-logo-20261016/`, con referencias anteriores conservadas y auditoría de hashes.

**Límites y pendientes reales:** React/MUI/Edge reales con transporte, identidad, reservas y persistencia simulados. Público exportado por Razor real en ASP.NET de pruebas con dobles en memoria. Sin PostgreSQL ni nueva ejecución de suites de dominio/HTTP de entregas anteriores. Google Fonts/Unsplash inaccesibles en ambos pases; pendientes externos cargados, dispositivos físicos, otros motores, zoom nativo y lector de pantalla. Continúa el desbordamiento público previo de 13 px a 1024 px fuera del bloque, sin modificación por alcance. Sin cambios de contratos, datos, migraciones, bases institucionales, push ni despliegue. `RemoveUsedMedia` conserva SHA-256 `FEB7B9E04500E6ED52BB3368D36303B025DB2A105C3B375BEE6BE2BDA546FB5C`; no se presume aplicada.

[Detalle, archivos, incidencias visuales corregidas, capturas y comandos](docs/migracion/COLUMNAS_FIJAS_Y_ALINEACION_UACJ.md).

## 2026-10-16 — Jerarquía institucional del bloque principal

**Implementado:** sobre todos los cambios locales existentes, únicamente CSS del bloque `.hero` en `tdv2/Views/Home/Index.cshtml`. Conjunto UACJ original de 440 a 560 px, máximo 100 % y altura proporcional; Dirección inmediatamente debajo a 16–18 px/600, separación de 14 px. H1 semántico conservado, 24–38 px/600, separado 36 px de la dependencia. Padding vertical 96 px en escritorio/48 px hasta 960 px para compensar el logotipo mayor. Subtítulo/párrafo, texto blanco, fondo azul, fotografía/velo, familias, imágenes y marcado intactos. La autorización actual cambia tamaños/pesos/espacios sólo en este bloque; sustituye allí la restricción de sólo colores de las entradas anteriores. Ningún archivo productivo interno fue modificado.

**Probado en esta revisión:** build Release correcto (0 errores, cuatro advertencias NU1902/NU1903 existentes); Razor real exportado mediante ASP.NET de pruebas con dobles en memoria. Edge 154: antes/después a 1920, 1366, 1024, 768, 390 y 320 px, 306 elementos comparados por tamaño; los 297 exteriores conservan dimensiones/estilos y sólo se desplazan por la nueva altura del bloque. Logotipo original cargado/proporcional, h1 único, alineación, separación, blanco/opacidad y ausencia de recortes/desbordamientos del bloque; paleta/contraste mínimo 8.05:1, navegación, preguntas y 84 entradas de estados correctas. Revisión visual de escritorio y móvil emulado. Evidencia `.artifacts/hero-hierarchy-20261016/`; capturas previas conservadas. Comparación de hashes del árbol de trabajo para conservar los cambios ajenos y la migración.

**Límites y pendientes:** Google Fonts/Unsplash inaccesibles en ambos pases; falta comprobación con esos recursos cargados y dispositivos físicos. Se detectó en la referencia anterior un desbordamiento exterior de 13 px a 1024 px en la columna lateral; persiste igual y queda fuera de este ajuste exclusivo del bloque. Sin PostgreSQL, migraciones, contratos, cambios de datos, push ni despliegue. Las suites internas/TypeScript/Vite y dominio de la entrada anterior **no se reejecutaron**: no cambiaron sus archivos; no se presentan como pruebas nuevas. `20261016190000_RemoveUsedMedia` conserva el hash anterior y no se presume aplicada.

[Archivos, medidas, capturas y reproducción del ajuste](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md#revisión-vigente-jerarquía-institucional-del-bloque-principal).

## 2026-10-16 — Revisión de paleta primaria y textos blancos

**Estado recuperado:** cambios pendientes de Git preservados. Presencia diferenciada, encabezados 700, agrupación, multiselectores, duplicados y Acciones fija ya estaban implementados. Se conservaron sus archivos productivos y se repitieron las verificaciones. El pendiente era el uso generalizado de complementarios en la portada y `#CFD6E8` en lema/párrafo.

**Implementado:** único archivo productivo modificado en esta revisión, `tdv2/Views/Home/Index.cshtml`, exclusivamente colores. Base **#213455 / #DADADA / #FFFFFF**; **#174187 únicamente en hover** para distinguir enlaces/botones manteniendo contraste. Retirados los demás complementarios de los estilos. Dirección General, título, lema y párrafo se calculan blancos con opacidad efectiva 1, sin filtros. Navegación blanca opaca y El proyecto blanco. Mismo marcado, texto, imágenes, logotipos, dimensiones, fuentes, animaciones, navegación y distribución. Manual local actual de **seis páginas** renderizado/leído, incluyendo paleta primaria de la página 141 y combinaciones/complementarios 143–145.

**Probado nuevamente:** tipos/Vite/Release correctos (0 errores, cuatro advertencias NuGet previas NU1902/NU1903 de Pkcs 9.0.18); **94/94 frontend**, **116/116 dominio/HTTP**, **24/24 recorridos de tablas** y **9/9 de selectores**. Capturas nuevas antes/después a 1366×900, 390×844 y 320×800: 306 elementos con geometría/tipografía/espaciados idénticos. Textos blancos comprobados por estilos calculados; normal/hover/pulsación/foco y preguntas abiertas, mínimo público normal conservador **8.05:1**. Encabezados internos Roboto-Bold/700 y 11.75:1. Datos/Acuerdos, dos actores, reservas, bloqueo de consulta, muchas filas, zoom CSS 200 %, confirmaciones, asociaciones, autoguardado/foco/scroll correctos. Evidencia `.artifacts/visual-primary-20261016/`, sin sobrescribir `.artifacts/visual-refresh/`.

**Límites y pendientes reales:** Razor real exportado desde ASP.NET de pruebas con dobles en memoria; captura interna en Edge con transporte/reservas/persistencia simulados y móvil emulado. Sin PostgreSQL ni pruebas institucionales en esta revisión. Google Fonts y Unsplash volvieron a resultar inaccesibles; ambos pases usan los mismos recursos locales y fuentes de respaldo, conservando las URLs originales. Pendientes comprobación con externos cargados, dispositivos físicos, zoom nativo/otros motores y lector de pantalla. `RemoveUsedMedia` permanece con SHA-256 `FEB7B9E04500E6ED52BB3368D36303B025DB2A105C3B375BEE6BE2BDA546FB5C`; no se modifica/ejecuta ni se presume aplicada. Sin contratos, datos, permisos, conexiones institucionales, push ni despliegue.

[Detalle vigente, archivos, complementario justificado, capturas y comandos Windows/Ubuntu](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md). La entrada siguiente conserva la evidencia de la primera entrega visual, anterior a esta revisión de colores.

## 2026-10-16 — Ajustes visuales y paleta pública

**Implementado:** ocho colores de presencia independientes del portal, sin alterar los índices/reservas del servidor; indicador lateral discreto, avatar, «Estás editando» y nombre visible de otra persona. Encabezados de catálogo 700, oscuros, agrupados y no seleccionables; resumen/búsqueda en líneas separadas. Acciones fija al borde derecho del contenedor, encabezado/celdas opacos, capas y ancho suficientes, sin duplicar controles. En contenedores de hasta 600 px Edición puede desplazarse horizontalmente para dejar espacio a los campos. Conservados catálogo, familias, duplicados, grupos, respuestas, eliminación de Medios, permisos y autoguardado.

**Portada:** se leyeron visualmente las tres páginas del manual local y se aplicaron únicamente colores mediante tokens de la vista. Sin amarillo, incluidos avisos. Navegación clara y superficies oscuras originales; mismos elementos, fuentes declaradas, dimensiones, contenido, imágenes, logotipos y comportamiento. Comparación antes/después de **306 elementos** en 1366×900, 390×844 y 320×800 sin diferencias de geometría/tipografía/espaciado/referencias.

**Probado:** TypeScript/Vite y .NET Release (0 errores; cuatro advertencias NU1902/NU1903 existentes de Pkcs 9.0.18); **94/94 frontend**, **116/116 dominio/HTTP en memoria**, **24/24 recorridos Edge internos** y **9/9 regresiones de selectores** con transporte simulado. Dos actores sintéticos, filas altas/cortas/muchas, zoom CSS 200 %, lectura, confirmaciones/asociaciones, primer clic, guardar/recargar/familias, foco y desplazamiento. Encabezados renderizados en Roboto-Bold, contraste 11.75:1; 153 textos públicos en escritorio, mínimo conservador 5.53:1. Capturas y estilos inspeccionados.

**Alcance y pendientes:** sólo presentación en producción; auxiliares nuevos exclusivamente en pruebas. Sin PostgreSQL ni ejecución/modificación de migraciones en esta entrega; `20261016190000_RemoveUsedMedia` conserva su hash y su aplicación institucional no se presume. El HTML público procede del Razor real mediante el host de pruebas en memoria. Google Fonts/Unsplash fueron inaccesibles por red; ambos pases usan la misma indisponibilidad y conservan sus URLs. Restan revisión con recursos externos cargados, zoom nativo/otros motores, dispositivos físicos, lector de pantalla y aceptación institucional. Sin push, despliegue ni conexiones institucionales.

Evidencia `.artifacts/visual-refresh/`: informes, capturas antes/después, geometrías, contrastes y salidas de compilación/pruebas. [Archivos, incidencias corregidas, evidencia y comandos Windows/Ubuntu sin EF](docs/migracion/AJUSTES_VISUALES_Y_PALETA_PUBLICA.md).

## 2026-10-16 — Catálogo agrupado, familias, duplicados y retirada de Medios

**Implementado:** catálogo exacto con búsqueda y encabezados no seleccionables; Sistema Integral de Información conserva `sii_v2`; Microsoft 365 (incluye Forms/Outlook) y Adobe (20 productos) guardan colecciones propias con claves estables y validación por familia. LimeSurvey/Power BI/Tableau son directos. Las opciones retiradas sólo permanecen en respuestas históricas. Dependientes muestran cero/nombre/cantidad, conservan respuestas al alternar y las etiquetas largas no se superponen. Duplicados se impiden por ID de procedimiento/opción, incluida Otra con espacios/caso normalizados; UI incluye filas pendientes y backend valida contra el documento vigente bajo bloqueo transaccional. Duplicados históricos permiten editar otros campos sin ampliarlos. Se mantienen agrupación visual, filas/orden JSON, herramientas opcionales, reservas, permisos, foco, scroll y autoguardado.

**Limpieza implementada:** Medios y Otro medio retirados de UI, contrato, bloques, revisión y envío. Nueva migración EF de datos **`20261016190000_RemoveUsedMedia`**: retira únicamente `medios`/`medioOtro` en contenido/instantáneas, la fila de bloque exclusiva y las rutas correspondientes de recibos. Incluye enviados e históricos por autorización expresa, sin recalcular porcentajes ni alterar otras respuestas, versiones o metadatos. No elimina columnas/tablas. Bloqueo exclusivo, suspensión acotada/restauración transaccional de triggers, respaldo obligatorio y Down sin falsa recuperación.

**Probado:** TypeScript/Vite/Release, **116/116** dominio/HTTP con dobles, **94/94** frontend, **36 casos nativos distintos** ASP.NET/PostgreSQL (5 nuevos + 31 de regresión), **67 comprobaciones EF**, **17 recorridos Edge con backend/PostgreSQL** (8 catálogo + 9 agrupación) y **9** con transporte simulado. Concurrencia observa dos conexiones bloqueadas antes de decidir un único guardado; limpieza prueba rollback, repetición, conservación exacta fuera de rutas retiradas e inmutabilidad posterior. Capturas 1366/390/320 px inspeccionadas. Identidades y fuentes externas sintéticas.

**Evidencia e incidencias:** `.artifacts/tools-*.txt`; corridas nativas `192002-91c1510d` (5 casos), `192115-f15446bd` (EF), `192322-4619feec` (28/31), `192757-4ed73330` (tres casos corregidos), `192938-6081b239` (catálogo navegador) y `193115-259221a8` (agrupación), todas bajo `.artifacts/native-postgres-20261016-`. Los fixtures anteriores y una frase residual de Medios se corrigieron; hubo un intento de build bloqueado por otra suite y esperas/localizadores de navegador ajustados. Los resultados intermedios fallidos están descritos y no se cuentan como aprobados. Los clústeres nuevos se detuvieron.

**Pendiente:** aplicar EF/publicar por el operador con respaldo real; no está confirmado el historial institucional. Ubuntu, lector de pantalla, dispositivos físicos y aceptación institucional no comprobados. No hubo acceso a bases existentes, Nexo/Herd, secretos, push ni despliegue. Los comandos Windows/Ubuntu y el orden paquete → detener → respaldar → EF → actualizar backend/frontend → iniciar están en el detalle. Las decisiones previas que conservaban Medios o permitían nuevas opciones/duplicados quedan sustituidas; las evidencias anteriores siguen siendo históricas.

[Contrato exacto, archivos, pruebas, rutas retiradas, respaldo/recuperación y comandos](docs/migracion/CATALOGO_HERRAMIENTAS_Y_RETIRO_MEDIOS.md).

## 2026-10-16 — Agrupación de herramientas y textos de todas las secciones

**Implementado:** grupos dentro de una única tabla, ordenados por Identificación general y relación estable `proceso`/`codigo`; nombres completos, alta explícita asociada y «Sin procedimiento asignado». No modifica el orden del JSON ni crea filas para mostrar grupos vacíos. Mantiene cero/una/varias herramientas opcionales, requisitos de filas iniciadas, multiselección, permisos y enviados. Filas con claves estables y conservación del punto de lectura/foco; encabezados de grupo accesibles durante desplazamiento horizontal. El envío espera también un alta manual en curso.

**Textos:** revisión de Contexto, Identificación, Sistemas, Medios, Datos, Evaluación, Preguntas, Acuerdos y Revisión. Ayudas de Área responsable/Prioridad, concordancia de registros/ILDA, confirmación de eliminación con explicación de reasignación, preguntas con «procedimiento» y trato de tú, opciones más claras y respeto a la escritura de SIIv2. Sólo etiquetas conocidas cambian: se conservan valores de preguntas/opciones, respuestas libres, nombres oficiales y snapshots.

**Probado:** TypeScript, Vite, .NET, 91/91 frontend y 115/115 dominio/HTTP con dobles. Nueve casos PostgreSQL nativos (opcionalidad/envío, conflictos, transacciones, historia y módulos), 20 recorridos Edge → ASP.NET → PostgreSQL (9 de agrupación/captura/textos y 11 de catálogos) y 9 recorridos adicionales de selectores con transporte simulado. Capturas a 1366/390/320 px revisadas; teclado, toque emulado, nombres largos, autoguardado, scroll, Ir al campo, reasignación, renombrado y eliminación. Evidencia `.artifacts/native-postgres-20261016-161608-6fa565a3`, `.artifacts/native-postgres-20261016-161846-ea0c4d59` y `.artifacts/grouped-tools-*.txt`. Las correcciones de preparación de fixtures/esperas y las advertencias NuGet anteriores se documentan en el detalle.

**Pendiente:** dispositivos físicos, lector de pantalla y aceptación institucional. Identidades/fuentes son sintéticas; dos pestañas de la misma cuenta no se presentan como dos identidades distintas. Los clústeres desechables se detuvieron; ningún cambio en bases existentes, Nexo/Herd, credenciales, push o despliegue.

**Actualización:** sólo frontend; sin migración, datos por transformar, configuración ni conexión institucional. Los archivos de backend modificados previamente se conservan y sus reglas se verifican como regresión. [Detalle y archivos de esta entrega](docs/migracion/SISTEMAS_HERRAMIENTAS.md#agrupación-dentro-de-la-tabla-y-redacción--2026-10-16).

## 2026-10-16 — Terminología y herramientas opcionales

**Implementado:** revisión de textos del portal y Procedimientos: ayudas, instrucciones, campos, navegación, mensajes del servidor y resumen con concordancia singular/plural. Se conserva el término «servicios» para prestaciones/continuidad tecnológica y «proceso institucional» para el cambio organizacional, no para registros de captura. Etiquetas explícitas para preguntas/orígenes antiguos, sin reescribir claves, valores libres, contratos ni catálogos. `FormRequirements` retira cobertura y mínimo de herramientas; cada procedimiento admite cero, una o varias. Sólo las filas iniciadas suman sus campos y condicionales. `FormSchema.New` deja Sistemas vacío sin tocar formatos existentes. Revisar y enviar mantiene Ir al campo para pendientes reales; no hay panel ni altas automáticas de cobertura. Medios sigue opcional. Permisos, reservas, confirmación, auditoría y bloqueo de enviados se mantienen.

**Fórmula:** `floor(100 × requisitos válidos / requisitos aplicables)`, sin cobertura ni pesos adicionales. Dos procedimientos completos sin herramientas: 14/14 = 100 %. Una herramienta cuyo único pendiente sea el uso: 17/18 = 94 %. Módulos/aplicaciones múltiples siguen siendo un requisito condicional por fila. [Detalle, archivos conceptuales y compatibilidad](docs/migracion/COBERTURA_Y_GUARDADO.md).

**Probado:** compilación .NET y Vite, TypeScript, **115/115** casos dominio/HTTP con dobles y **89/89** frontend. **19 casos nativos distintos correctos** con PostgreSQL 18 desechable y ASP.NET: cero herramientas y envío directo, nueva captura sin filas automáticas, fila incompleta rechazada, varias herramientas para el mismo procedimiento, eliminación de la última, porcentaje Inicio/formato/revisión, condicionales/múltiples módulos, historia, permisos (responsable/administrador/colaborador/consulta), doble envío, conexiones independientes y conflictos/reintentos. **8/8 recorridos Edge → ASP.NET → PostgreSQL**: portada/acceso, ausencia de cobertura, alta manual, pendientes e Ir al campo con foco, guardado/recarga, eliminación/reasignación, ayudas Escape/foco, envío sin herramientas y bloqueo. Capturas de escritorio 1366 px y móvil emulado 390/320 px revisadas, sin desbordamiento de página; la tabla mantiene su desplazamiento horizontal.

**Evidencia y ajustes de pruebas:** `.artifacts/optional-tools-{unit,frontend,types,vite,build}.txt`; resumen nativo por caso `.artifacts/optional-tools-native-evidence.json`. La corrida `.artifacts/native-postgres-20261016-144630-5e0d0e6e` terminó 14/19: cinco fixtures todavía consideraban «completa» una herramienta con opciones retiradas. Se adaptaron esos casos para verificar formatos completos **sin herramientas** y pasaron los cinco al repetirlos en `.artifacts/native-postgres-20261016-144905-95031747`, junto a los 8 recorridos del navegador (`coverage-native-browser.json` y capturas `optional-*.png`). En el primer pase de preparación también se corrigieron expectativas numéricas, plantillas que asumían una herramienta inicial y la consulta de prueba `json_array_length` (el contenido es JSON); un arranque de navegador sin certificado se repitió con TLS efímero del entorno aislado. No se cambió producción para hacer pasar esos fixtures. La compilación conserva dos advertencias NuGet existentes de `System.Security.Cryptography.Pkcs` 9.0.18; no se actualizaron dependencias en este alcance.

**Pendiente:** dispositivos físicos, lector de pantalla y aceptación institucional. Las identidades/fuentes externas son sintéticas; las pruebas de concurrencia utilizan sesiones y conexiones PostgreSQL independientes, no acreditan integración institucional. Ninguna base existente fue inspeccionada o alterada.

**Actualización:** no requiere migración, importación, backfill ni configuración nueva. Actualizar backend y frontend conjuntamente mediante el flujo habitual autorizado. Borradores recalculan al consultar y persisten avance al guardar; enviados/históricos conservan porcentaje persistido, contenido, instantáneas y bloqueo. Ningún acceso institucional, push o despliegue en esta entrega.

## 2026-10-15 — Aplicaciones exclusivamente desde listas Excel

**Implementado:** extracción de Sheet1/Aplicación a `Contracts/aplicaciones-2026-10-14.json`, 207 web y 26 móviles, sin duplicados ni filas inválidas; UUID versionados, sin valores institucionales inventados. Nueva migración **20261015165555_LocalApplicationLists** añade catálogo local por categoría/ID y metadatos; copia IDs/nombres anteriores sin activarlos, no altera respuestas ni instantáneas. Importación explícita de ambas listas en una transacción con auditoría y bloqueo; comprobación previa, correspondencia exacta inequívoca, ambigüedades reportadas con selección local independiente, bajas conservadas como historia. Retirados lectores/SELECT/publicaciones y controles de aplicaciones SQL Server; quedan UR/módulos e ILDA independiente. Selectores múltiples, apertura inicial, guardado/recarga, autorizaciones y requisito único conservados.

**Probado:** extracción real repetida produce los mismos bytes e IDs; **4/4** pruebas XLSX sintéticas. Tipos TypeScript, compilación .NET/Vite, **88/88** frontend y **114/114** dominio/HTTP con dobles. **55 casos PostgreSQL nativos distintos correctos**: 51 de sincronización/importación y 4 de edición de aplicaciones; incluyen migración con enviado histórico, 233 entradas, repetición/simultaneidad, ambigüedad, rollback y guardado/concurrencia. El pase amplio fue 49/50 porque una expectativa todavía contaba cuatro catálogos; se corrigió a UR/módulos y ese caso pasó al repetirlo. No se oculta ese resultado intermedio. **59/59** comprobaciones EF; **11/11** recorridos Edge → ASP.NET → PostgreSQL de sistemas/aplicaciones y **7/7** de sincronizaciones, a 1366/1440 y 360 px, teclado y toque emulado, sin errores JavaScript. Capturas inspeccionadas; transporte real, fuentes/autenticación sintéticas. No sustituyen aceptación institucional o pruebas táctiles físicas.

Paquete local `.artifacts/publish-application-lists` construido con el target normal; catálogo incluido e idéntico por SHA-256. Los comandos **reales del paquete** `--environment Production --check-applications` y `--import-applications` pasaron como procesos independientes contra PostgreSQL aislado, sin conexiones institucionales ni consumidor de cola. El build inicial informó advertencias NuGet preexistentes sobre `System.Security.Cryptography.Pkcs`; no se cambiaron dependencias.

Evidencia de esta actualización:

- `.artifacts/application-lists-native-evidence.json`: consolida resultados por nombre, sin contar repeticiones como casos nuevos.
- `.artifacts/native-postgres-20261015-165938-940df949/verification.json`: 10/10 focalizados.
- `.artifacts/native-postgres-20261015-170228-6540b180/verification.json`: regresión 49/50 y diagnóstico de la expectativa anterior.
- `.artifacts/native-postgres-20261015-170719-14ee7393/verification.json`: caso corregido y manejador del comando, 2/2.
- `.artifacts/native-postgres-20261015-171354-5924bd96/verification.json`: proceso CLI real del paquete, 1/1.
- `.artifacts/native-postgres-20261015-172100-0ca9bf86/verification.json`: repetición de catálogo/IDs y lectura de `public` con `search_path=pg_catalog`, 1/1.
- `.artifacts/native-postgres-20261015-170829-c488c05f/ef-migrations.json`: 59 comprobaciones.
- `.artifacts/native-postgres-20261015-171112-21eb1266/systems-native-browser.json` y sus PNG: 11 recorridos.
- `.artifacts/native-postgres-20261015-171500-0669e54a/browser-sync-manual.json` y sus PNG: 7 recorridos.

Comandos reproducibles: `npm --prefix ClientApp run types:check`, `npm --prefix ClientApp run test:forms`, `npm --prefix ClientApp run build`, `dotnet build tdv2.slnx -c Release -p:UseAppHost=false`, `dotnet run --project tests/TDV2.Verification -c Release -p:UseAppHost=false`; `python -B scripts/test_application_extraction.py`. Pruebas nativas: `scripts/Test-NativePostgres.ps1 -DirectStart -SyncOnly`, `-MigrationsOnly`, o `-SkipBrowser` con `TDV2_TEST_CASE_PREFIX='Sync/listas|Edición/aplicaciones'`. Navegador: `-BrowserOnly` con `TDV2_TEST_EPHEMERAL_TLS=true`, `TDV2_TEST_NODE` apuntando al Node disponible y `TDV2_TEST_BROWSER_FLOW=systems` / `sync-manual`. Todas crean clústeres desechables bajo `.artifacts`, nunca usan una conexión institucional.

**Pendiente:** actualización e importación institucional por el operador, correspondencias contra la réplica real (no inspeccionada), ejecución en Ubuntu y dispositivos físicos. No se modificaron bases existentes/institucionales, secretos, Nexo ni Herd; sin push/despliegue. La migración es aditiva, pero el binario anterior no debe seguir operando después de importar/guardar UUID. [Criterio de IDs, archivos, pruebas y orden/comandos exactos](docs/migracion/APLICACIONES_LISTAS.md).

## 2026-10-14 — Aplicaciones móviles y web: implementación anterior de SQL Server

Origen retirado por la actualización del 15 de octubre. Se conserva esta evidencia histórica; sus instrucciones de sincronización no son las vigentes.

**Implementado:** tipos/nulabilidad confirmados por el usuario, entidades/configuraciones EF y migración aditiva **20261014180408_SiiApplicationsCatalog**. IDs int32 originales sin identidad local, nulos/textos preservados y padre web sin FK. SELECT explícitos con ACTIVA=1, publicación transaccional por catálogo con rollback de metadatos/resultado/auditoría y retirada lógica sólo tras lectura completa. SQL Server ejecuta UR, módulos, móviles y web; ILDA independiente. Selectores compartidos cero/nombre/cantidad, JSON separado con nombres confirmados y referencias históricas, un requisito condicional, permisos/reservas/autoguardado/avance conservados. Sin cambios de instantáneas enviadas ni reintroducción del bloque de cobertura.

**Probado:** TypeScript/Vite y 88/88 frontend; dominio/HTTP con dobles. PostgreSQL 18 nativo: 9/9 casos iniciales de aplicaciones, ampliados en **52/52 casos de sincronización** (manual web sin CLI, doble solicitud/dos administradores, CLI/web simultáneos, programación/ILDA, reinicio, errores y resultados parciales). Verificados 1501 activas por catálogo, repetición/update/IDs/nulos/longitudes 200/8000/500, padres ausentes, EOF interrumpido, duplicados incompatibles, retiradas y rollback al auditar. **11/11 recorridos Edge con ASP.NET/PostgreSQL reales**: aplicaciones cero/una/varias, guardado/recarga, nombres completos, búsqueda, teclado/tacto emulado, pendientes/foco; regresiones SIIv2, reservas, SignalR y enviados. Capturas 1366/360 px revisadas. Fuentes SQL Server/MySQL e identidades siempre sintéticas; las pruebas anteriores con transporte simulado se distinguen de esta evidencia nativa.

**Cierre de comprobaciones:** **114/114** dominio/HTTP con dobles, **88/88** frontend, **52/52** sincronización y **12/12** edición/concurrencia ASP.NET/PostgreSQL, **59/59** comprobaciones EF nativas, **11/11** recorridos Edge nativos y **12/12** grupos adicionales con transporte simulado. Incluye rechazo de nuevas selecciones frente a retirada concurrente con conexiones independientes, preservación de históricos enviados, cobertura y guardados en filas distintas. Compilación final Release: 0 avisos/errores; tipos/Vite correctos. La generación EF había informado advertencias NuGet preexistentes de Pkcs 9.0.18; sin cambios de dependencias. Corridas/artefactos y correcciones del fixture/verificador documentados en el detalle.

**Actualización pendiente del operador:** aplicar explícitamente **20261014180408_SiiApplicationsCatalog** y publicar backend/frontend/procesadores de la misma versión. Migración compatible con el esquema anterior del repositorio; no se inspeccionó el servidor publicado. Un binario antiguo puede no admitir las nuevas claves JSON una vez capturadas. No hay configuración nueva ni DDL al iniciar. Se necesita SELECT sobre las columnas solicitadas de las dos tablas SQL Server; no se concedieron permisos. [Contrato, archivos, pruebas y comandos exactos Windows/Ubuntu](docs/migracion/APLICACIONES_SQL_SERVER.md).

**Límites:** no acceso a fuentes/bases institucionales, Nexo, Herd ni credenciales; sin push/despliegue. Migraciones sólo en clústeres nuevos dentro de .artifacts, detenidos al terminar. Dispositivos físicos, lector de pantalla y aceptación institucional pendientes. La colaboración de navegador usa dos sesiones de la misma identidad sintética; la prueba de solicitudes simultáneas de sincronización sí utiliza dos administradores sintéticos distintos.

## 2026-10-14 — Sistemas compacto y pendientes en la revisión

**Implementado:** se retira el bloque «Procedimientos y captura de herramientas»; la tabla conserva acciones/scroll. Revisar y enviar mantiene pendientes con Ir al campo o Agregar registro, confirmando el procedimiento y evitando duplicaciones. Módulos de SIIv2 muestra «Selecciona módulos», un nombre completo o la cantidad seleccionada; sin chips ni ayuda repetida. Conserva búsqueda/casillas, agrupación, errores, nombres largos, apertura inmediata, reservas y autoguardado. Sin cambios de backend, cobertura, avance o envío.

**Probado:** TypeScript/Vite, 88/88 frontend; 46 grupos Edge con transporte simulado y 12 recorridos Edge con ASP.NET/PostgreSQL 18 aislado. Estados cero/uno/varios, guardar/recargar, teclado/tacto emulado, reserva lenta/denegada, cambios rápidos, scroll, pendientes/foco/alta, eliminación de última herramienta y envío/bloqueo. Capturas de escritorio/móvil revisadas. [Archivos, comandos y evidencia](docs/migracion/SISTEMAS_HERRAMIENTAS.md#resumen-compacto-y-pendientes--2026-10-14).

**Actualización y pendiente:** sólo frontend; sin migración/configuración nueva. No se consultaron bases existentes ni fuentes institucionales; clústeres sintéticos detenidos al terminar. Dispositivos físicos y lectores de pantalla no comprobados. Dos sesiones sintéticas no acreditan dos identidades institucionales. Sin push/despliegue.

## 2026-10-14 — Cobertura, evolución del JSON y guardado colaborativo

**Implementado:** evaluación central de cobertura por procedimiento aplicable, compartida por avance, revisión y envío API. Sistemas lista todos los procedimientos reales y permite capturar/localizar sin filas ficticias; retiro informa registros afectados y conserva respuestas para reasignación. Marca `versionEstructura: 1` independiente de concurrencia, compatible con ausencia anterior, sólo actualizada al guardar; snapshots enviados intactos. Conserva modelo híbrido, módulos múltiples, reservas, autoguardado y cambios locales.

**Probado:** 108/108 dominio/HTTP con dobles, 88/88 frontend, TypeScript/Vite y compilación Release. PostgreSQL 18 aislado: 10 casos distintos de ASP.NET/HTTP (9/9 iniciales y repetición ampliada 6/6), incluyendo dos conexiones bloqueadas realmente, escrituras distintas, mismo bloque, caducidad, solicitudes atrasadas, recibos idénticos, cobertura/envío, retiros relacionados, módulos, rollback y enviados anteriores. Edge con ASP.NET/PostgreSQL: 5/5 recorridos, navegación/foco/alta, guardado/recarga, última herramienta, reasignación, nombres largos, envío/bloqueo y 1366/390/320 px. Regresión módulos con transporte simulado: 12/12. Capturas revisadas. [Fórmula, archivos, corridas y comandos](docs/migracion/COBERTURA_Y_GUARDADO.md).

**Actualización:** ninguna migración nueva ni backfill. Actualizar conjuntamente backend y frontend en una publicación posterior autorizada; no hay nuevas claves de configuración. Borradores recalculan cobertura al consultar/guardar; enviados conservan porcentaje persistido y bloqueo.

**Pendiente/límites:** sesiones independientes de la misma identidad sintética, sin afirmar dos identidades distintas; fuentes institucionales, dispositivos físicos, lector de pantalla y carga real no comprobados. Sin acceso a bases institucionales, Nexo/Herd, cambios de credenciales, push ni despliegue. Las bases nuevas de pruebas se crearon dentro de `.artifacts` y se detuvieron al terminar.

## 2026-10-14 — Selección múltiple de módulos SIIv2

**Implementado:** Módulos de SIIv2 permite uno o varios módulos por fila mediante búsqueda/casillas y etiquetas removibles con nombres completos. Conserva menú abierto al seleccionar, foco, reservas, agrupación y autoguardado. `modulosSii` guarda objetos con ID institucional e instantánea de descripción confirmada por servidor; lee el módulo único anterior sin reescribirlo al consultar. Rechaza repeticiones, IDs inválidos o nuevas referencias ausentes; conserva asociaciones históricas retiradas y detalles dormidos de otras herramientas. El conjunto aporta exactamente un requisito al avance y a los pendientes; una colección vacía no reutiliza el ID antiguo. No une filas ni cambia permisos, bloqueos o sincronizaciones.

**Probado:** TypeScript/Vite, compilación Release y **106/106 dominio/HTTP**; **88/88 frontend**; **55 grupos Edge con transporte simulado** (12 selección múltiple, 13 Sistemas, 9 apertura/reservas, 12 scroll y 9 Procedimiento). PostgreSQL 18 aislado: **3/3 casos ASP.NET/HTTP** y **7/7 recorridos de navegador con ASP.NET, persistencia y SignalR reales**, fuentes/identidad sintéticas. Verificados IDs duplicados/inválidos, descripciones duplicadas, rechazo sin publicación parcial, compatibilidad escalar, retirada histórica, filas independientes, guardado/recarga, avance, menú abierto durante ACK, errores, lectura de enviados, escritorio, teclado y móvil emulado. Capturas revisadas.

**Migraciones:** ninguna nueva. Se reutiliza `formatos_ur.contenido` (`json`) y los recibos/instantáneas existentes. Las migraciones normales ya existentes se aplicaron únicamente en clústeres desechables de pruebas; ningún arranque productivo ejecuta DDL.

**Pendiente/límites:** aceptación institucional, lectores de pantalla y dispositivos físicos. La concurrencia de navegador usa sesiones separadas de una cuenta sintética; no acredita dos identidades distintas. No acceso a servicios o bases institucionales, Nexo/Herd, credenciales reales, push ni despliegue. [Contrato, archivos, comandos y evidencia](docs/migracion/SISTEMAS_HERRAMIENTAS.md#selección-múltiple-de-módulos--2026-10-14).

## 2026-10-13 — CAST histórico, etiqueta Procedimiento y scroll de captura

**Implementado:** Portal del CAST se retira de nuevas selecciones y se rechaza en nuevas asignaciones del backend; conserva etiqueta, datos y validez de respuestas históricas sin penalizar adicionalmente su avance. Procedimiento sustituye Trámite o servicio en campo, ayuda, accesibilidad, instrucciones, alta y pendientes, conservando `tramite`. Las dos tablas tienen una región estable con ambos ejes, altura natural o limitada al viewport, encabezados fijos sobre las presencias, barra horizontal en su borde inferior y Agregar fuera. Medios permanece independiente. Ir al campo y nuevas filas revelan el control después del render del foco; no restauran foco ni scroll en actualizaciones ordinarias.

**Probado:** tipos/Vite, compilación Release, **104/104 dominio/HTTP** con dobles y **88/88 frontend**. **52 grupos Edge** (12 scroll/catálogo, 13 Sistemas, 9 Procedimiento, 9 apertura/reservas, 9 interfaz) con transporte simulado. Tablas de 1/30/31 filas, escritorio/móvil, capas, extremos, ambas barras, teclado/rueda/tacto emulado, continuidad hacia la página, nombres completos, menús, dependientes, navegación a pendientes, alta visible, conservación de captura/scroll/reserva, CAST histórico y rechazo de nuevas asignaciones. Sintaxis de suites nativas actualizadas, comprobada; capturas revisadas.

**Pendiente/límites:** sin acceso ni cambios de bases, migraciones o sincronizaciones. No se ejecutaron suites con PostgreSQL ni validación institucional. Persistencia/reservas de navegador simuladas; dispositivos físicos, panel táctil físico y lector de pantalla pendientes. Sin push/despliegue. [Archivos, comandos y detalle](docs/migracion/SISTEMAS_HERRAMIENTAS.md#catálogo-histórico-procedimiento-y-desplazamiento-propio--2026-10-13).

## 2026-10-13 — Procedimiento completo en Sistemas y herramientas

**Implementado:** código y nombre completos dentro del Autocomplete, sin elipsis/límite de líneas, tooltip ni ayuda por fila; se conserva la ayuda del encabezado. La columna sigue compacta, con altura natural y espacio propio para botones. Reutiliza el CSS multilínea de Módulo SII. La búsqueda se mantiene debajo del nombre al abrir para evitar la contracción de una fila larga entre pulsar y soltar, reproducida en navegador. Sin cambios en identificadores, reservas, autoguardado, validación o backend.

**Probado:** tipos y Vite; **88/88 frontend** y **40/40 grupos Edge** (9 Procedimiento, 9 interfaz, 9 reservas/selectores, 13 Sistemas). Nombres sintéticos PO-08/09 y otros largos, cerrado/abierto, búsqueda, asociaciones al guardar/recargar, consulta histórica, teclado, primer clic y tacto emulado, scroll horizontal, 1366/390/320 px; regresiones de módulos y edición conservadas. Capturas revisadas.

**Pendiente/límites:** no se consultaron bases. Los nombres reales de PO-08/09 no están en las fuentes/fixtures revisados; su aceptación real queda pendiente. El componente usa íntegramente los nombres recibidos del formato. HTTP, SignalR, identidad, reservas y persistencia simulados; no validación ASP.NET/PostgreSQL o institucional. Dispositivos físicos/lector de pantalla pendientes. Sin migraciones, push ni despliegue. [Archivos, comandos y evidencia](docs/migracion/SISTEMAS_HERRAMIENTAS.md#procedimiento-completo-y-sin-ayuda-por-registro--2026-10-13).

## 2026-10-13 — Apertura de selectores y reserva al cambiar de fila

**Causa comprobada:** un clic en Módulo producía `pointerdown → mousedown → focusin`; la apertura esperaba un `engine.focus` cuya intención invalidaba el foco posterior de la fila. El navegador terminó con reserva propia y `aria-expanded=false`. Las claves por ID eran estables. Otra prueba A → B → A con reserva lenta detectó que B podía adquirir una reserva en cola después de salir de ella.

**Implementado:** Módulo y Procedimiento comparten apertura inmediata con estado «Preparando edición…». Abrir/buscar no autoriza cambios; opciones y limpiar esperan `canEdit` confirmado. Una reserva ocupada o fallida muestra el motivo, conserva el valor y permite cerrar/reintentar. La intención visual se invalida al cerrar, cambiar de registro/sección o desmontar; las respuestas tardías no abren ni enfocan otro control. Los estados del menú pertenecen al contexto de edición, incluido su contenido sin opciones. Entrar a otro registro se reconoce aunque haya un menú abierto. `BlockEditor.focus` revalida la intención después de esperar las operaciones de reserva/liberación, antes de adquirir una reserva en cola. Se conservan guardado por registro, autoridad/versiones, caducidad, presentación y controles existentes.

**Probado:** TypeScript/Vite, **88/88 frontend** (incluye regresión de reserva obsoleta) y **22/22 recorridos Edge**: nueve nuevos de apertura y 13 regresiones de Sistemas. Primer clic en campo/flecha, teclado con Tab/flechas/Enter, tacto emulado, respuesta lenta, clic dentro del estado de carga, conservación del mismo nodo/foco, carrera con rechazo HTTP 409 entre sesiones, fallo 503, recuperación, Escape, cambios rápidos A/B/A y salida de sección. Guardado anterior confirmado y módulo persistido por ID en fixture; textos completos, errores, ayudas, solo consulta y tablas compactas conservados. [Archivos y comandos](docs/migracion/SISTEMAS_HERRAMIENTAS.md#apertura-y-reserva-de-selectores--2026-10-13).

**Pendiente/límites:** React/MUI real con transporte, identidad, reservas y SignalR sintéticos; no verifica autorización institucional ni persistencia ASP.NET/PostgreSQL. Escritorio y móvil emulado; dispositivos físicos pendientes. Sin cambios de backend, bases, migraciones, dependencias, push ni despliegue.

## 2026-10-13 — Tipografía de dependientes y nombre completo del módulo

**Implementado:** medición real confirmó etiquetas de 16 px frente a contenido de 13 px y abertura del borde calculada sobre 13 px. Se unifica la escala base/familia/peso sólo en los dependientes de Sistemas, conservando la reducción flotante de MUI. El grupo mantiene línea/sangría y reserva espacio superior para la etiqueta. Módulo de SIIv2 muestra el nombre completo en varias líneas mediante `renderValue`, con búsqueda MUI, opciones multilínea y botones en una columna propia que contempla su tamaño táctil. Mantiene ancho de tabla, ID interno, instantánea, condiciones, validaciones y edición existente.

**Probado:** TypeScript/Vite, **87/87 pruebas frontend** y **13/13 recorridos Edge**. Mediciones de los tres campos vacíos, enfocados, completos y con error; nombre largo sin recorte, búsqueda/flechas/Enter/Escape/Tab, sin superposición con botones; persistencia/recarga por ID en fixture, lectura sin edición, scroll, reserva y errores locales. Escritorio/móvil 1366/360 px y regresiones de Acciones hasta 1920/768 px; capturas revisadas. Sintaxis de la suite nativa adaptada, correcta.

**Pendiente/límites:** transporte/identidad/SignalR simulados, sin pruebas de PostgreSQL/ASP.NET ni dispositivos físicos. Ninguna base modificada, migración, dependencia, push o despliegue. [Archivos, mediciones y evidencia](docs/migracion/SISTEMAS_HERRAMIENTAS.md#tipografía-y-módulo-completo--2026-10-13).

## 2026-10-13 — Agrupación de campos condicionales de Sistemas

**Implementado:** `SystemField` comparte la presentación de Módulo de SIIv2, Nombre de la herramienta y Describe el uso: selector arriba, detalle dentro de la misma celda, sangría total de 14–18 px, línea vertical de 2 px en azul institucional suave y separación corta. Etiquetas, errores y avisos del catálogo permanecen en su grupo accesible. No se añaden tarjetas, explicaciones ni controles de foco; se conservan condiciones, valores, IDs, validación, reservas y autoguardado. Módulos mantienen el nombre sin ID añadido. Revisado Medios utilizados: Otro medio siempre está visible actualmente y no se convierte en condicional.

**Probado:** TypeScript, Vite y **87/87 pruebas frontend**, correctos. **12/12 recorridos Edge** de `system-tools-ui.mjs`: los tres dependientes conservan foco en el selector al aparecer, Tab llega al detalle, ancho de tabla constante, errores 422 simulados junto al campo y asociados accesiblemente, textos largos, corrección/guardado y reserva sin liberar. Capturas en 1366 y 360 px; la suite conserva verificaciones en 1920/1366/768/360 px de Acciones, scroll, ayudas, nombres de módulos, Egresados, sesiones separadas y consulta de enviados. Evidencia en `.artifacts/system-tools-ui/report.json` y `dependent-{moduloSiiId,sistemaOtro,usoOtro}-{1366,360}.png`.

**Pendiente/límites:** navegador con React/MUI real y HTTP/identidad/SignalR simulados; no verifica SII o persistencia PostgreSQL real. Móvil emulado, sin dispositivos físicos. Sólo presentación frontend en este ajuste, sin cambios de backend ni pruebas de bases. No migraciones, push ni despliegue. [Detalle de presentación y archivos](docs/migracion/SISTEMAS_HERRAMIENTAS.md#campos-condicionales--2026-10-13).

## 2026-10-12 — Acciones, módulo SIIv2, Egresados y Áreas de oportunidad

**Implementado:** Acciones de Sistemas pasa de 76 a 112 px, con encabezado en una línea, alineación centrada, padding lateral y aumento equivalente del mínimo de tabla (1590 → 1626 px); conserva otros anchos, fuente, scroll, botones de 44 × 44 px y foco. Módulos muestran sólo descripción real/instantánea, sin concatenar ID en opciones ni selección; ID, asociaciones y guardado siguen intactos. Egresados se agrega al catálogo de selección múltiple y a `FormSchema.UserChoices`: la misma validación alimenta elegibilidad, avance y revisión. Áreas de oportunidad sustituye el nombre visible y accesible/ayuda, conservando `fallas`, textos y obligatoriedad actuales. No cambia Funciona con fallas.

**Probado:** TypeScript, Vite y compilación ASP.NET/proyecto de verificación Release correctos, sin avisos ni errores. **102/102 dominio/HTTP** con dobles en memoria; incluyen Egresados solo/combinado, serialización/recarga, elegibilidad y concordancia de avance/revisión/Inicio. **87/87 frontend** y **15/15 recorridos Edge** (11 Sistemas, 4 Guardar/Enviar/Colaboradores). Verificados nombres duplicados con IDs distintos, selección guardada, instantánea ante cambio de descripción, nombres legítimos con texto «ID», comentarios anteriores/multilínea y enviados protegidos. Medidas de Acciones, foco, confirmación y scroll en 1920/1366/768/360 px; capturas revisadas. Reservas/ayudas/autoguardado y Egresados seleccionados/recuperados con transporte simulado. Sintaxis de cuatro suites nativas adaptadas comprobada.

**Pendiente/límites:** no se ejecutaron pruebas de persistencia PostgreSQL ni las suites nativas que requieren preparar/migrar su base. HTTP usa TestServer con almacenamiento sustituido; navegador simula HTTP/identidad/SignalR. Móvil y tacto emulados, sin dispositivos físicos. Ninguna migración creada o ejecutada, datos de producción, sincronización institucional, push ni despliegue. [Archivos, comandos y evidencia](docs/migracion/SISTEMAS_HERRAMIENTAS.md#ajustes-de-captura--2026-10-12).

## 2026-10-11 — Presentación del origen en Colaboradores

**Implementado:** las asignaciones centrales muestran únicamente **Agregado por Administrador** como identificación del origen. Se retiran las referencias al sistema de origen de los textos auxiliares de la lista, conservando Solo consulta, la explicación de alcance comprobable y los retiros pendientes. Las delegaciones de responsables, sus áreas y acciones conservan la presentación existente. No se modifican origen interno, identificadores, auditoría, permisos ni reglas de visibilidad; en backend cambia sólo el texto del aviso de asignaciones.

**Probado:** TypeScript, Vite y compilación ASP.NET Release correctos (0 avisos/errores). **4/4 recorridos** de `collaboration-actions-ui.mjs` con React/MUI real en Edge y transporte simulado. Lista, detalles de asignación y diálogo de retiro revisados; etiqueta exacta, ausencia de referencias al origen en texto, nombres accesibles y tooltips, conservación de acciones. Capturas de escritorio y móvil 390/320 px, teclado y tacto emulado; regresiones existentes de Guardar/Enviar también correctas. Evidencia: `.artifacts/collaboration-actions-20261011/verification.json` y capturas.

**Pendiente/límites:** no se ejecutaron consultas institucionales ni pruebas sobre bases de datos; no es validación de la integración real. Dispositivos físicos no probados. Sin migraciones, push ni despliegue. [Detalle y comandos](docs/migracion/COLABORADORES_VISIBILIDAD_ACCIONES.md).

## 2026-10-10 — Guardar, visibilidad de Colaboradores y distribución de acceso

**Implementado:** Guardar sustituye Guardar borrador, conserva guardado sin envío y usa borde; precede a Enviar formato azul en la revisión, con alturas consistentes y adaptación móvil. Mantiene flush/confirmación/versión, recibos y reservas; evita llamadas manuales repetidas mientras guarda o comprueba el envío. Estados Guardando…/Enviando… sin anticipar éxito. Pruebas de acceso centra el contenedor, equilibra selección y resumen en dos columnas y los apila en móvil, con acciones al final de la selección y franja de contexto existente. Tema, barra blanca y tipografía conservados.

**Colaboradores:** la lista ya no depende exclusivamente de `ur_otorgante`. Integra lecturas de las vistas Nexo existentes con los vínculos locales candidatos; revalida rol, módulo, identidad y concesión. Reglas compartidas con autorización conservan los alcances existentes. Dependencias se muestra sólo al nivel 2 correspondiente; local también al responsable del área asignada, tanto si otorga nivel 2 como nivel 3. Identifica origen, área asignada y alcance por asignación; no duplica concesión/área ni crea vínculos centrales locales. Ver no habilita Retirar: las centrales se administran en Nexo y las delegadas conservan el área otorgante autorizada. Administradores mantienen su rama delegada; consulta y representación de lectura no adquieren retiro. Suspensiones/revocaciones no aparecen como asignaciones efectivas; retiros centrales pendientes siguen identificados para reintento del otorgante.

**Probado:** TypeScript y Vite correctos, ASP.NET y proyecto de pruebas Release compilados sin avisos/errores. **101/101 dominio/HTTP** con dobles en memoria, incluidos diez casos de visibilidad y cuatro de HTTP nuevos; **87/87 frontend**. Edge/bundle real con transporte simulado: **4/4** recorridos nuevos de Colaboradores/Guardar/Enviar, **4/4** de acceso con medición de columnas/centrado, **9/9** de interfaz y **10/10** regresiones de Sistemas. Verificados los cuatro casos, ramas distintas y nivel 2 anidado, asignaciones múltiples, ausencia/revocación de concesión, cambios de adscripción/empleado, solo consulta, permisos de retiro, doble clic, captura guardada y recargada sin enviar, versión remota que invalida confirmación y ACK demorado de envío. Escritorio, 1050 px y móvil 390/320 px, teclado y capturas revisadas.

**Pendiente/límites:** el contrato central vigente usa clave de rol y adscripción actual para determinar alcance; `id_ur_acceso` es origen histórico. No publica un área de colaboración central independiente de esa adscripción. Sin los datos necesarios se omite la asignación y se explica la limitación, sin inferir alcance institucional. La consulta nueva sobre vistas Nexo y el proveedor PostgreSQL no se ejecutaron: esta entrega no conectó ni modificó bases. Los fixtures de HTTP sustituyen publicación/delegación/almacenamiento; navegador simula también reservas y SignalR. No acreditan autorización institucional ni transacciones reales. Dispositivos físicos pendientes. Sin nueva migración, cambios de Nexo/Herd/credenciales, sincronización real, push o despliegue. [Detalle, comandos y archivos](docs/migracion/COLABORADORES_VISIBILIDAD_ACCIONES.md).

## 2026-10-10 — Complemento: avance, SQL Server y Pruebas de acceso

**Implementado:** `FormRequirements` centraliza el avance y los pendientes de la captura activa. Cuenta requisitos válidos y condicionales, excluye opcionales y filas iniciales intactas de ayuda, mantiene el mínimo existente de registros y distingue datos completos de envío confirmado. Fórmula: parte entera de `100 × requisitos válidos / requisitos aplicables`; sin requisitos, 0 y revisión no lista. Inicio y grupos promedian porcentajes de formatos únicos por `id_ur`, sin redondear hacia 100 ni duplicar ramas. Se retira la explicación del avance del conjunto, conservando ambos indicadores. Revisión muestra **Resumen del formato**, **Pendientes** y **Enviar formato** sin referencias visibles a primera etapa; conserva su alcance técnico. Ir al campo también enfoca Agregar cuando faltan todos los registros. Sistemas conserva buscador compacto, reservas y Medios independiente.

**Sincronizaciones y acceso:** único botón **Sincronizar SQL Server** solicita la ejecución SII existente (UR y después módulos), presenta ambos resultados y estado conjunto confirmado; ILDA tiene acción/estado propios. Fallo y parcial preservan las fechas correctas; no se mezclan éxitos de ejecuciones distintas. Pruebas de acceso separa sus dos modalidades con búsqueda, resumen previo, carga/vacío/error, acciones explícitas, franja persistente y cuenta autenticada diferenciada. Sin cambios en permisos, autoridad, cola, programación, credenciales o configuración de despliegue.

**Probado en este complemento:** TypeScript, Vite y compilación ASP.NET **Release** correctos, ésta sin avisos/errores. **87/87 frontend**, **87/87 dominio/HTTP con dobles**; incluyen captura vacía/parcial/completa, SII/Otra/Otro, históricos D/N y herramientas antiguas, eliminación, redondeo y concordancia de Inicio/formato/revisión tras recarga desde contenido controlado. Edge con bundle real y transporte simulado: **9/9** recorridos de interfaz, **4/4** de Pruebas de acceso, **10/10** de Sistemas; **3/3** grupos de Inicio. Verificados SQL Server completo/parcial/fallido e ILDA independiente, acciones/navegación/errores de las dos modalidades, solo consulta, foco, teclado, escritorio y móvil hasta 320 px. Sintaxis de suites nativas adaptadas comprobada. Capturas de los resultados revisadas.

**Pendiente/límites:** no se conectó ni modificó ninguna base; las nuevas pruebas de navegador simulan identidad, transporte, reservas y SignalR. No sustituyen integración nativa PostgreSQL, autorización Nexo o prueba institucional. Dispositivos físicos pendientes. Debug encontró `tdv2.dll` bloqueada por otro proceso (MSB3021/MSB3027); se conservó ese proceso y se verificó Release. La fecha conjunta SQL Server depende de encontrar un éxito de ambos catálogos entre las 50 ejecuciones disponibles; se informa cuando no existe evidencia. Sin nueva migración, aplicación de migraciones, push ni despliegue. [Fórmula detallada, comandos, evidencia y archivos del complemento](docs/migracion/AVANCE_Y_PRUEBAS_ACCESO.md).

## 2026-10-10 — Distribución de Inicio, Sistemas, Revisión y Sincronizaciones (ajuste inicial)

**Implementado:** directorio jerárquico compacto, pestañas en franja propia, barra de filtros adaptable y Actualizar secundario visible. Se retiran únicamente encabezado y descripción redundantes; permisos individuales y tres indicadores conservan los datos del servidor. Preferencias de vista en memoria por identidad/contexto, restauradas al volver y conservadas al actualizar. Sistemas limita Procedimiento a 280–360 px, con búsqueda código/nombre, opciones completas, elipsis y ayuda accesible; conserva códigos, Edición fija, campos condicionales, varias filas y Medios independiente. Revisión compacta desde la proyección del backend, botones **Ir al campo** con foco y desplazamiento hasta el control, acción final **Enviar formato** separada, sin mensaje de etapa lista. Sincronizaciones presenta tres catálogos con fecha de último éxito independiente del intento, texto/icono de estado, recuentos disponibles y Sincronizar/Reintentar. SII sigue ejecutando UR y módulos juntos; no se crea otra cola ni se modifica la programación.

**Probado:** TypeScript y Vite correctos; compilación ASP.NET sin avisos ni errores; **85/85** pruebas frontend y **76/76** de dominio/HTTP con dobles en memoria. Edge con bundle real: **7/7** recorridos nuevos, **10/10** regresiones de sistemas y **3/3** grupos de comprobaciones de Inicio. Cubren nombres largos, ancho del selector, búsqueda sin borrar la relación, apertura con clic/teclado/tacto, ayudas y reserva, múltiples herramientas, Medios, consulta, filtros/expansión/scroll al regresar, pendientes y foco móvil fuera de Edición, error/confirmación demorada de guardado, envío sólo tras respuesta y resultados parciales sin alterar último éxito. Escritorio hasta 1920 px, tableta 768 px y móvil hasta 320 px. Capturas finales revisadas. La regresión detectó y permitió corregir la apertura por mousedown invalidada por un foco posterior de la misma fila; `openOnFocus` reutiliza la reserva confirmada, sin cambiar el motor.

**Pendiente/límites:** navegador con transporte, identidad, reservas y SignalR simulados; no acredita integración ASP.NET/PostgreSQL ni autorización institucional. No se ejecutaron suites nativas ni se modificó ninguna base; sus selectores se adaptaron a los nuevos controles, sin declarar esas suites verificadas. Móvil/tacto emulados, dispositivos físicos pendientes. Historial limitado a 50 ejecuciones: si falta un intento o su recuento, se indica expresamente, sin inventar cero o éxito. Sin dependencias, migraciones nuevas, credenciales, fuentes institucionales, push ni despliegue. [Comandos, evidencia y relación completa de archivos](docs/migracion/INTERFAZ_OPERATIVA.md).

## 2026-10-09 — Revisar y enviar por etapa

**Implementado:** pestaña **Revisar y enviar** después de Sistemas y herramientas, accesible con pendientes y sin sumar avance. Reutiliza la revisión, el diálogo y los servicios existentes. Presenta Primera etapa, área/ejercicio, estado de las tres secciones, resumen de procedimientos/herramientas/medios, autoguardado y enlaces por sección, registro y campo. Conserva Contexto inicial, recuperación de ubicación, tablas compactas, ayudas, presencia, reservas y eliminación directa. Se corrigió una carrera reproducida al seguir un pendiente: ahora termina el guardado/liberación anterior antes de enfocar y reservar el destino.

**Enviar primera etapa** conserva permisos y añade comprobación explícita de etapa. Se habilita sólo con respuestas completas y confirmadas, sin errores ni reservas ajenas. D/N históricos continúan pendientes. El diálogo explica el bloqueo y queda invalidado ante cambios remotos de versión/etapa o edición ajena. El servidor revalida requisitos, permisos, contexto, CSRF, ejercicio, versión y reservas dentro de la transacción existente. Petición única en curso y recibo idempotente evitan duplicados; envío, recibo y auditoría son atómicos. Tras confirmar se muestra **Primera etapa enviada**, fecha y nombre del autor efectivo.

La migración nueva **`20261009171656_StageSubmissions`** añade etapa activa y envíos independientes con instantánea parcial, fecha, actores, nombre, versión y operación. Triggers y servicio protegen cada etapa enviada. No atribuye envíos globales anteriores a una etapa: sus datos, autores, fechas, auditorías y bloqueo permanecen intactos. No borra respuestas posteriores. La segunda etapa permanece deshabilitada y sin definición de campos/requisitos; su futuro envío no heredará el de la primera. [Contrato, persistencia y operación](docs/migracion/REVISAR_ENVIO_ETAPAS.md).

**Probado:** TypeScript, Vite y compilación .NET correctos; **80/80** pruebas frontend (incluyen doble clic, reintento y SignalR anterior al ACK) y **76/76** dominio/HTTP con dobles. **57/57** comprobaciones de migración en PostgreSQL 18 desechable: esquema aditivo, modelo/snapshot, aplicación inicial/repetida, scripts idempotentes, Up/Down sin contenido, rechazo de reversión con enviados, protección y conservación de históricos. Las expectativas de las suites antiguas se ajustaron a la columna aditiva `etapa_activa`; las migraciones anteriores no cambiaron. [Resultados EF](docs/migracion/revisar-enviar-20261009/migraciones.json).

**56 casos de regresión nativa y 10 casos de etapas comprobados:** responsables/administradores/colaboradores/consulta, rechazo de UR ajena, D/N, revisión incompleta, ubicación sin avance, primera etapa sin requisitos posteriores, solicitudes concurrentes y reintentos, reservas/versiones obsoletas, fallo de auditoría con rollback, históricos y separación de etapas. La corrida completa inicial dio 65/66: la prueba nueva esperaba incorrectamente que consulta escribiera su ubicación; el servidor conservó su HTTP 403. Se corrigió esa expectativa y se repitieron las diez pruebas de etapas: **10/10**, sin ampliar permisos. [Corrida inicial](docs/migracion/revisar-enviar-20261009/regresion-inicial.json), [repetición de etapas](docs/migracion/revisar-enviar-20261009/etapas-postgresql.json).

**7/7 recorridos de revisión y 11/11 de colaboración en Edge → ASP.NET → PostgreSQL/SignalR reales**, con dos identidades sintéticas distintas y contextos separados: revisión incompleta, enlaces/foco, recuperación de pestaña, colaboración sin envío, guardado retenido, cambio remoto durante confirmación, rechazo directo del backend, doble clic/reintento, recarga con fecha/autor/bloqueo, tablas, reservas, primera escritura/pegado/Tab, eliminación, Medios independiente y reconexión/vencimiento. Escritorio 1440 px, móvil 390/320 px y teclado; sin errores JavaScript. Capturas revisadas: [escritorio](docs/migracion/revisar-enviar-20261009/stage-review-1440.png), [móvil](docs/migracion/revisar-enviar-20261009/stage-review-390.png), [envío confirmado](docs/migracion/revisar-enviar-20261009/stage-submitted-mobile.png). [Revisión](docs/migracion/revisar-enviar-20261009/revision-navegador.json), [colaboración](docs/migracion/revisar-enviar-20261009/colaboracion-navegador.json).

**Pendiente/límites:** migración sin aplicar a bases institucionales; segunda etapa sin habilitar; dispositivos físicos y aceptación institucional pendientes. Microsoft/Nexo son pares sintéticos; las pruebas unitarias sí simulan transporte. En navegador las carreras retienen solicitudes reales y la desconexión se emula, sin reemplazar la API de edición. La independencia de una segunda etapa se comprueba sólo con persistencia y datos sintéticos: no demuestra una API de captura de segunda etapa habilitada. Sin acceso institucional ni cambios en Nexo, Herd o credenciales; sin push ni despliegue.

Aplicación **posterior y explícita por el operador**, desde la raíz y sobre el destino previamente verificado/configurado:

```powershell
dotnet ef database update 20261009171656_StageSubmissions --project tdv2 -- --environment Development
```

Development utiliza User Secrets vigentes sin modificarlos. No hay migraciones al arrancar. [Comandos reproducibles de pruebas, revisión SQL y operación](docs/migracion/REVISAR_ENVIO_ETAPAS.md).

## 2026-10-09 — Icono de ayuda en la instrucción de Identificación

**Implementado:** `IdentificationInstructions.tsx` sustituye únicamente el carácter ⓘ por `HelpOutlineOutlined` de MUI, el mismo componente utilizado en `FieldHelp.tsx`: 19 px, `text.primary` y alineación vertical al centro del texto. Referencia visual sin interacción, con nombre accesible «Ayuda». Conserva el párrafo y «procedimientos institucionales» en negritas.

**Probado:** TypeScript (`npm.cmd --prefix ClientApp run types:check`) y Vite (`npm.cmd --prefix ClientApp run build`) correctos. Edge a 1440/390/320 px con transporte sintético, sin bases: SVG idéntico al botón real, tamaño y color iguales, negritas de peso 700, icono dentro del párrafo y sin desbordamiento ni errores JavaScript. Capturas revisadas y métricas en `.artifacts/instruccion-ayuda-20261009/`.

**Pendiente/límite:** móvil emulado, sin validación en dispositivo físico. Sin cambios de lógica, permisos, datos o credenciales; sin push ni despliegue.

## 2026-10-09 — Acceso público y validación Vigente/Ajustar

**Implementado:** el botón principal del portal muestra «Registrar un procedimiento institucional», con texto adaptable al ancho y flecha alineada. Conserva exactamente el destino condicional `/connect` o `/inicio` y la autenticación. Identificación general ofrece sólo `V · Vigente` y `A · Ajustar`; «Selecciona una opción» es indicativo, sin entrada seleccionable. Se mantienen las instrucciones de eliminar registros ajenos al área.

D/N existentes conservan su etiqueta y, en formatos editables, muestran «Validación pendiente de actualizar». El servidor sólo los admite sin modificación cuando coinciden con el valor persistido del mismo registro, dentro de la operación de escritura existente. Permite guardar otros campos de esa fila y otros bloques; rechaza nuevas asignaciones, cambios D↔N y copias a nuevos IDs. La regla compartida de elegibilidad exige V/A en avance, revisión, relaciones y envío; corregir a V/A o eliminar utiliza reservas/autoguardado/retiro existentes. No hay conversión, limpieza ni migración nueva; enviados e históricos bloqueados conservan contenido e instantáneas. [Contrato de captura](docs/migracion/IDENTIFICACION_TABLA.md).

**Probado:** TypeScript y compilaciones Vite/ASP.NET correctas; **78/78** pruebas de frontend y **76/76** de dominio/HTTP con dobles. **5/5** casos nuevos ASP.NET → PostgreSQL 18 nativo desechable: rechazo HTTP 422 de D/N en filas actuales y nuevas, atomicidad de varios bloques, conservación al editar otros campos, pendientes/avance y rechazo de envío, corrección V/A persistida y envío permitido, eliminación directa y lecturas/escrituras bloqueadas de enviados e históricos con comparación íntegra de la fila almacenada. [Resultados PostgreSQL](docs/migracion/validacion-procedimientos-20261009/postgresql.json); el contador del runner incluye además un grupo de navegador.

**6/6** recorridos Edge → ASP.NET → PostgreSQL: acceso desde el portal antes/después de autenticar, botón completo a 1440/390/320 px, menú exactamente V/A, indicativo no seleccionable, reserva, autoguardado, guardado/recarga, D/N legibles y pendientes, edición de otros campos sin conversiones, corrección por mouse/teclado, eliminación directa y enviado de consulta. **11/11** recorridos de regresión con dos identidades sintéticas distintas: exclusión por fila, edición simultánea de filas distintas, fotografías/presencia, primera escritura/pegado/Tab, actualización SignalR, eliminación libre/ajena, renovación tardía, Medios independiente, menús/ayudas, otra pestaña, desconexión/reconexión/caducidad y envío de primera etapa. Cero errores JavaScript. [Navegador](docs/migracion/validacion-procedimientos-20261009/navegador.json), [colaboración](docs/migracion/validacion-procedimientos-20261009/colaboracion.json). Capturas finales revisadas en `.artifacts/native-postgres-20261009-163112-0531b958/`.

**Pendiente/límites:** móvil/tacto emulados, sin comprobación en dispositivos físicos ni con identidades institucionales. Microsoft es un par HTTP sintético y Nexo vistas/funciones locales de prueba; la captura, las reservas y la persistencia no usan transporte simulado. La regresión de carrera retiene solicitudes reales y la de desconexión emula pérdida de red. Recursos externos del portal bloqueados durante la verificación, referencias de fuentes conservadas. Sin consultas ni cambios a bases/fuentes institucionales, Nexo, Herd o credenciales. Sin push ni despliegue; sin migraciones que aplicar por este ajuste.

Comandos reproducibles desde la raíz (Node instalado con Visual Studio; cada ejecución nativa crea y detiene su clúster aislado):

```powershell
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:PATH=(Split-Path $env:TDV2_TEST_NODE)+';'+$env:PATH
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run build
npm.cmd --prefix ClientApp run test:forms
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_CASE_PREFIX='Edición/validación:'
$env:TDV2_TEST_BROWSER_FLOW='validation'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -DirectStart
Remove-Item Env:TDV2_TEST_CASE_PREFIX
$env:TDV2_TEST_BROWSER_FLOW='delivery'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

## 2026-10-09 — Contacto público compacto

**Implementado:** únicamente el bloque Contacto y sus estilos en `tdv2/Views/Home/Index.cshtml`: Subdirección de Inteligencia de Datos, enlace `mailto:sid@uacj.mx`, «Extensiones telefónicas: 4374, 4274, 4174» y «Lunes a viernes, 8:00 a 15:00 h». Se retiran de esa tarjeta Universidad, Dirección y nombres/títulos personales. Encabezado, colores y tipografía conservados; separación de 8 px entre datos, sin márgenes adicionales ni estilos de elementos retirados.

**Probado:** Razor/ASP.NET compilado y servido en aislamiento; Edge a 1440×1000, 390×844 y 320×740, contenido exacto, mailto, tarjeta compacta, ausencia de desbordamientos y capturas revisadas. Comprobación SHA-256 antes/después confirma que el resto de la portada, el layout autenticado y el PNG no cambiaron: logo de 440 px, Dirección debajo y footers centrados conservados. Evidencia local en `.artifacts/contacto-20261009/`.

**Pendiente/límite:** móvil emulado y recursos externos bloqueados durante la comprobación, sin alterar sus referencias. Sin bases, credenciales ni servicios institucionales; sin cambios funcionales, push o despliegue.

## 2026-10-09 — Logo público de 440 px y footers centrados

**Implementado:** `.uacj-logo` pasa de 380 a 440 px conservando `max-width: 100%` y altura automática. El PNG original permanece intacto; Dirección debajo, alineada a la izquierda y sin raya decorativa externa. La vista pública centra el texto del footer y `AuthenticatedLayout.tsx` centra el footer compartido con flex y alineación textual. Ambos mantienen únicamente «Universidad Autónoma de Ciudad Juárez», fuente y espaciado compacto. Sin cambios funcionales.

**Probado:** compilación Razor/ASP.NET, TypeScript y Vite correctos. Edge a 1440, 390 y 320 px: logo de 440 px en escritorio, proporción original y ajuste móvil sin desbordamiento, Dirección alineada y líneas del footer público centradas. Inicio, Configuración y Formato con transporte sintético verifican el footer compartido en los mismos tres anchos: centrado con desviación menor a 1 px, fuente de 11 px y padding de 16 × 24 px conservados. Texto exacto, imágenes locales sin errores y SHA-256 del logo servido igual al original. Capturas revisadas; evidencia local en `.artifacts/logo-footer-20261009/`.

**Pendiente/límite:** móvil emulado; fuentes y fondo externos bloqueados durante la comprobación y sus referencias conservadas. Portada servida por ASP.NET aislado sin configuración institucional ni servicio de sincronización; área autenticada con transporte sintético, sin bases. No se modificaron funcionalidades, credenciales ni bases; sin push ni despliegue.

## 2026-10-09 — Sincronizaciones manuales desde el proceso web

**Implementado:** `ManualSyncWorker` integra la cola manual en el ciclo de vida de ASP.NET mediante `BackgroundService`, consulta persistente cada dos segundos y señal tras commit. Reutiliza `SyncCoordinator`, reservas de 30 minutos, recuperación, publicación y auditoría; no hay tareas sueltas en el endpoint. El POST devuelve 202 sin esperar descargas y el servidor excluye solicitudes incompatibles. `manualOnly` no crea horarios, no cambia su configuración ni suplanta el latido del CLI. Web y procesadores externos comparten la misma reserva. Sin nueva migración, variables, credenciales ni servicio de despliegue adicional.

React distingue «Iniciando…» de «Sincronizando…» según confirmación real, actualiza estado/contadores por catálogo sin recarga y protege dobles clics y ACK perdido. Error local del procesador y demoras de inicio aparecen de forma contextual; el trabajo aceptado se conserva y se reintenta consultar la misma cola, sin duplicarlo. El administrador puede solicitar otro trabajo cuando el anterior haya terminado. Permisos, CSRF, contexto, representación, SII UR/módulos e ILDA independiente se conservan. No se modifican formularios ni respuestas.

**Probado:** **44/44** casos sobre PostgreSQL nativo desechable, incluidos nueve nuevos con el hosted service real: horarios pausados/vencidos, HTTP breve durante lectura retenida, ILDA deshabilitada, doble solicitud y dos administradores distintos, proceso CLI hijo concurrente, fallos separados de los tres catálogos, error al reclamar con recuperación de la misma solicitud, pendiente sin señal, terminación real de proceso web hijo/reinicio/caducidad y rechazos directos de autorización/CSRF/representación/revocación. La caducidad se adelanta únicamente en la base sintética; no se espera media hora real. **76/76** dominio/HTTP con dobles, TypeScript y compilación Vite/ASP.NET correctos.

**6/6** recorridos nuevos Edge → ASP.NET → PostgreSQL con servicio hospedado real: inicio manual sin CLI, doble clic, ACK demorado, recarga/navegación/cierre de página, fallo parcial, diagnóstico de cola, pérdida de ACK/consulta, recuperación y escritorio/móvil emulado. **13/13** recorridos de regresión: configuración versionada, horarios con/sin ILDA, permisos, catálogos y conservación de respuestas. En esta regresión histórica el host automático se retira sólo del verificador y los ciclos se invocan explícitamente para observar cada estado; no es evidencia de autoarranque. Ninguna ejecución de navegador tuvo errores JavaScript. [Cola y reservas](docs/migracion/sincronizacion-manual-20261009/postgresql.json), [servicio web y navegador](docs/migracion/sincronizacion-manual-20261009/navegador-manual.json), [regresión](docs/migracion/sincronizacion-manual-20261009/navegador-regresion.json).

**Pendiente/límites:** SQL Server/MySQL son lectores ADO.NET sintéticos, Microsoft un par HTTP simulado y Nexo vistas/funciones locales sintéticas. Sólo los casos de ACK/red demoran o pierden respuestas HTTP reales; no simulan la cola, el servicio, publicaciones o reservas. Faltan SII/ILDA institucionales, dispositivos móviles físicos y operación Ubuntu/supervisor/SIGTERM. No se leyeron credenciales ni se accedió a fuentes/bases institucionales, Nexo o Herd. No hubo push, despliegue ni migración aplicada fuera del aislamiento.

Comandos de esta comprobación (Node de Visual Studio ya instalado; cada script crea y detiene su propio clúster):

```powershell
dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SyncOnly -DirectStart
$env:TDV2_TEST_EPHEMERAL_TLS='true'
$env:TDV2_TEST_NODE='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe'
$env:TDV2_TEST_BROWSER_FLOW='sync-manual'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
$env:TDV2_TEST_BROWSER_FLOW='sync'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly -DirectStart
```

Operación vigente y cambios de despliegue documentados en [Sincronizaciones](docs/migracion/SINCRONIZACIONES.md), [Windows](docs/ARRANQUE_LOCAL_WINDOWS.md) y [Ubuntu](docs/PUBLICACION.md). Basta el botón de Configuración → Sincronizaciones con ASP.NET en ejecución; los horarios habilitados continúan usando su procesador externo. Sin nuevos comandos de migración para este ajuste.

## 2026-10-09 — Footer compartido del área autenticada

**Implementado:** `ClientApp/resources/js/Layouts/AuthenticatedLayout.tsx` muestra únicamente «Universidad Autónoma de Ciudad Juárez». Conserva posición, tipografía de 11 px, espaciado y estilos originales. El mismo layout sirve a todos los módulos y submódulos. La portada pública mantiene exactamente su contenido anterior (SHA-256 verificado antes/después); sin cambios en permisos, formatos, colaboración ni persistencia.

**Probado:** TypeScript y build Vite correctos. Edge con transporte sintético local: Inicio, Configuración y Formato en 1440×900, 390×844 y 320×740 (9 comprobaciones). Un solo footer, texto exacto, después del contenido, altura de 49.5 px, sin desbordamiento horizontal ni errores JavaScript. Capturas de escritorio y móvil revisadas; evidencia local en `.artifacts/authenticated-footer-20261009/`.

**Pendiente/límite:** móvil emulado, sin comprobación en dispositivos físicos ni autenticación institucional. Sin acceso a bases, Nexo, Herd o credenciales; sin push ni despliegue.

## 2026-10-09 — Ajuste exclusivo de la portada pública

**Implementado:** únicamente `tdv2/Views/Home/Index.cshtml` cambia comportamiento público. Se retiran Proyectos estratégicos, Indicadores de avance, sus enlaces/estilos exclusivos y la FAQ que remitía al tablero retirado. Se elimina el nombre de la Subdirección y la etiqueta «Proyecto institucional · UACJ». El logo existente `/images/logos/firmaLogotipo-blancosinfondo-3.png` aparece antes de la Dirección, ambos a la izquierda, con 18 px de separación. Logo de 380 px y máximo 100%, altura automática, proporción/transparencia/colores intactos; desaparecen borde decorativo y sangría de la Dirección. Footer compacto con únicamente «Universidad Autónoma de Ciudad Juárez». Se conserva Transformación Digital, la configuración tipográfica y el acceso `/connect`. No cambia React autenticado ni backend de permisos/formatos.

**Probado:** compilación Razor/ASP.NET Release; portada servida por ASP.NET con configuración vacía y protección efímera, sin bases ni servicios institucionales. Edge en 1440×1000, 390×844 y 320×740: sin desbordamiento horizontal, texto retirado ausente, destinos de sección existentes, imágenes locales cargadas, proporción/alineación y footer verificados. HTTP 200 `image/png` desde la ruta exacta; bytes idénticos al original SHA-256 `0bac7d97c01a83dcb4c3c7233947ea4bd0b6151d6c4eb223b4a9baf3254481bf`. `StaticWebAssetsPrepareForPublish` incluye el PNG como `All / PreserveNewest`, sin modificar el proyecto ni publicar. [Resultado](docs/migracion/portal-publico-20261009/verification.json), [publicación](docs/migracion/portal-publico-20261009/logo-publish.json), [escritorio](docs/migracion/portal-publico-20261009/desktop.png), [móvil](docs/migracion/portal-publico-20261009/mobile.png), [footer](docs/migracion/portal-publico-20261009/mobile-footer.png).

**Pendiente/límite:** las capturas no cargaron Google Fonts ni el fondo remoto de Unsplash por restricciones de red del entorno; sus referencias originales permanecen sin cambios y se muestran las alternativas del navegador. No se probó autenticación institucional ni se accedió a bases, Nexo, Herd o credenciales. Sin migraciones, push ni despliegue. Los documentos con enlaces `#` preexistentes conservan su estado; no se inventaron URLs de documentos.

## 2026-10-09 — Desplazamiento sin liberar, columna Edición y alturas compactas

### Implementado

Se conservaron los cambios locales y el contrato anterior de entrega, permisos y persistencia. Se reprodujo que arrastrar la barra horizontal nativa movía el foco del textarea al contenedor DIV; `onBlurCapture` lo interpretaba como salir de la fila y llamaba a guardar/liberar. `RecordEditingContext` utiliza destinos de foco y clics reales, distingue barras/gestos y conserva el contexto ante blur sin destino. No fuerza foco ni renueva por desplazamiento. Otro registro/sección conserva el guardado y liberación confirmados; ayudas y menús pertenecen al contexto original.

`RowEditingPresence` ocupa la primera columna **Edición**, fija y de 124 px, con fotografía/iniciales, color y estado textual; sustituye expresamente la posición anterior en Acciones. Eliminar queda separado y arriba, con área de 44 × 44 px, icono rojo, nombre «Eliminar registro», foco y explicación accesible al deshabilitarse. Se conserva la eliminación directa atómica y el tratamiento de renovaciones tardías. Multilínea: dos a cuatro líneas visibles y scroll interno, íntegro también en consulta; ¿Qué entrega? amplía su mínimo a 280 px sin cambiar la fuente. Las dos tablas dejan el scroll vertical a la página y conservan horizontal propio.

### Verificado

- Reproducción anterior con barra nativa, ASP.NET y PostgreSQL desechable: reserva propia → libre al desplazar. Traza conservada.
- **9/9** recorridos nuevos de barras, selección/cursor, texto largo, lectura enviada, presencia, eliminación, teclas/menús/ayudas, SII/Otra/Otro y móvil/tacto emulado, con **dos identidades distintas** y transporte real de escritura/reservas/SignalR.
- **11/11** regresiones E2E: carrera de usuarios, otra pestaña, filas independientes, actualización entre usuarios, Medios, conexión/vencimiento, eliminación/renovación concurrentes, envío de primera etapa e inmutabilidad.
- **51/51** comprobaciones de edición/autorización en PostgreSQL nativo aislado: atomicidad, concurrencia, permisos, testigos, relaciones, primera etapa e inmutabilidad.
- **78/78** frontend con transporte simulado donde corresponde y **76/76** dominio/HTTP con dobles; TypeScript y build Vite correctos. No se confunden con los recorridos reales anteriores.

[Causa, resultados, archivos y comandos de repetición](docs/migracion/DESPLAZAMIENTO_TABLAS.md). No hay nueva migración ni cambios al modelo de persistencia en este ajuste.

### Pendiente

Mouse/panel táctil/pantallas táctiles físicos, navegadores móviles reales y fuentes institucionales. Las pruebas sólo acreditan aislamiento sintético y emulación. Sin accesos ni cambios a Nexo, Herd, credenciales o bases institucionales; sin push ni despliegue. Las migraciones anteriores siguen requiriendo aplicación explícita del operador, nunca arranque web.

## 2026-10-09 — Entrega hasta Sistemas, elegibilidad, foco y diagnóstico SII

### Implementado

Se conservó la versión local y todos sus cambios. Identificación y Sistemas mantienen tablas, 13 px, encabezados en negritas, contenido multilínea, fotografías y reservas por registro. El doble foco procedía de los outlines de `input/textarea` dentro de tablas más el fieldset enfocado de MUI: se retira únicamente el outline interior de `MuiOutlinedInput`, conservando foco de teclado y contorno de presencia. `RowEditingActions` reserva el mismo espacio en Acciones de ambas tablas; el avatar muestra el nombre también al enfocar, sin cambiar el foco lógico de edición.

La entrega y el avance vigentes comprenden Contexto, Identificación y Sistemas con Medios. El backend reutiliza esa revisión para enviar, conserva permisos intrínsecos del responsable, control de versión/reservas, snapshot e inmutabilidad. Mostrar secciones posteriores como administrador no amplía requisitos. Se conservan sus respuestas, sin convertirlas en completadas ni normalizarlas en guardados ajenos. Medios es una sección independiente dentro de Sistemas y usa su propia reserva/autoguardado. El campo vigente `Otro medio` no tenía placeholder de CAST; continúa sin ejemplo y la opción Portal del CAST permanece en herramientas.

`ProcedureEligibility` centraliza campos obligatorios actuales, validez y V/A. Lectura/ACK entregan opciones confirmadas; SignalR actualiza las otras sesiones. La escritura comprueba disponibilidad dentro de la transacción. Los borradores incompletos se guardan, pero no se ofrecen para nuevas relaciones. Un vínculo que después quede incompleto se conserva y genera pendiente contextual de entrega.

Se comprobó que EF/modelo/migración `20261009080533_SiiModulesCatalog` corresponden a `public.sii_modulos`. Las consultas especializadas ahora explicitan el esquema. El diagnóstico local distingue migración pendiente, historial aplicado sin tabla, otro esquema e historial sin permiso de lectura. La pantalla administrativa informa error/contador no disponible; el procesador marca la etapa fallida sin abrir la fuente de módulos. No se crea el catálogo manualmente, no se oculta el fallo como lista vacía correcta y no se añade DDL al arranque. **No hay nueva migración en este ajuste ni cambios a migraciones anteriores.**

### Verificado

**76/76 .NET** de dominio/HTTP en memoria y lector ADO.NET sintético; **78/78 frontend** con transporte simulado; **10/10 recorridos Edge/React con transporte simulado**, cero errores JavaScript. Compilación Release y TypeScript/Vite correctos. **35/35 sincronizaciones en PostgreSQL nativo aislado**, incluida ausencia de esquema, fallo por catálogo sin consulta de su fuente, 1501 módulos, integridad, rollback, manual/automática e ILDA independiente (`native-postgres-20261009-115252-a9b6af89`).

La verificación nativa de este ajuste pasó **51/51 casos de edición** (`native-postgres-20261009-115720-67ee7190`): alcance/permiso, revocaciones, responsables y colaboradores, reservas, versiones, atomicidad, errores, eliminación/renovación tardía, respuestas históricas y enviados. Pasó **57/57 comprobaciones EF/PostgreSQL** (`native-postgres-20261009-120236-eb1453e0`), incluidos cinco diagnósticos nuevos de módulos y aplicación/repetición mediante EF. Las pruebas de migraciones históricas conservan sus porcentajes históricos esperados; no se editaron migraciones para adaptar esos porcentajes al cálculo actual. [Edición](docs/migracion/entrega-sistemas-20261009/edicion-postgresql.json), [EF](docs/migracion/entrega-sistemas-20261009/migraciones-postgresql.json), [sincronizaciones](docs/migracion/entrega-sistemas-20261009/sincronizaciones-postgresql.json), [UI con transporte simulado](docs/migracion/entrega-sistemas-20261009/navegador-transporte-simulado.json).

**11/11 recorridos Edge → ASP.NET → PostgreSQL con dos identidades distintas**, más otra pestaña de la misma cuenta, cero errores JavaScript (`native-postgres-20261009-120744-82efeb72`). Reservas simultáneas, filas independientes, identidad/presencia, guardado/salida, primeros caracteres/pegado/Tab, elegibilidad remota, eliminación y renovaciones tardías, Medios independiente, ayudas, desconexión/caducidad/reconexión, móvil y entrega sin posteriores. Se prueban rechazos directos del backend y enviados. [Reporte](docs/migracion/entrega-sistemas-20261009/dos-usuarios-postgresql.json), [foco de escritorio](docs/migracion/entrega-sistemas-20261009/escritorio-foco.png), [presencia móvil](docs/migracion/entrega-sistemas-20261009/movil-presencia.png). Las pruebas de conexión bloquean peticiones externas; no son validación institucional.

**7/7 recorridos nativos de módulos** (`native-postgres-20261009-120504-4fb94e16`) y **13/13 de Sincronizaciones** (`native-postgres-20261009-120549-793841be`), cero errores JavaScript. Incluyen buscador/ID/instantánea, sin catálogo, Otra/Otro, varias herramientas, sesiones, ayudas, móvil, enviados, cola/programación/ILDA, fallos parciales y diagnóstico visible al faltar el esquema local. [Módulos](docs/migracion/entrega-sistemas-20261009/modulos-navegador-postgresql.json), [Sincronizaciones](docs/migracion/entrega-sistemas-20261009/sincronizaciones-navegador.json), [error administrativo](docs/migracion/entrega-sistemas-20261009/sincronizaciones-esquema-ausente.png). Motores aislados detenidos tras cada corrida.

### Pendiente y límites

No se consultó la base usada por la instalación que produjo la captura: `42P01` prueba ausencia de una relación resoluble en PostgreSQL; no determina por sí solo si la configuración apunta a otra base. La reproducción aislada con migración pendiente no acredita esa causa en una base institucional. El operador debe verificar el destino previsto y aplicar las migraciones pendientes explícitamente. Siguen pendientes SII real (SELECT de las dos columnas, DesarrolloSII, TLS/permisos/tipos/volumen), dispositivos físicos y aceptación institucional. Sin cambios en Nexo, Herd, credenciales, bases institucionales, push ni despliegue.

[Contrato y comandos EF/operación](docs/migracion/SISTEMAS_HERRAMIENTAS.md) · [Edición automática](docs/migracion/EDICION_AUTOMATICA.md).

## 2026-10-09 — Sistemas y herramientas y módulos de SIIv2 (entrega anterior)

### Implementado

Se preservaron cambios locales, tablas, tipografía y densidad compacta. Identificación muestra Código/origen sin renumerar; Prioridad pierde sólo el consejo solicitado. Ayudas compartidas con X accesible, Cerrar, Escape/foco y secciones opcionales. Sistemas utiliza una herramienta por fila, varias filas por procedimiento, selección única de herramienta/uso/funcionamiento, detalles separados y búsqueda de módulos locales. Se preservan históricos, enviados, primera etapa, participación, reservas/autoguardado y eliminación directa. El servidor valida opciones nuevas/referencias, calcula pendientes y conserva la descripción del módulo como instantánea.

SII incorpora UR y módulos a la cola/procesador existente, manual/automático e independiente de ILDA. Consulta fija de dos columnas de `sii.MODULOS_SII` por la misma conexión Sii; réplica EF con IDs estables y bajas locales, publicaciones/resultados/auditoría transaccionales por catálogo y fallos sanitizados. Nueva migración **`20261009080533_SiiModulesCatalog`**, sin alterar migraciones anteriores ni respuestas. No se activa programación, se aplica DDL al arranque ni se modifica Nexo.

### Verificado

**74/74 .NET** (dominio/HTTP en memoria y lector ADO.NET sintético), **78/78 frontend**, **10/10 Edge con React y transporte simulado**, cero errores JavaScript. Incluyen 1501 módulos sin truncamiento, IDs/duplicados/lectura parcial, conexión compartida sin abrir, borradores, detalles, múltiples herramientas, históricos, códigos/orígenes, endpoint autorizado, ayudas, teclado, dos sesiones y anchos 1920/1366/768/360. TypeScript/Vite y solución Release correctos; .NET sin advertencias. EF confirma snapshot/modelo y genera el SQL sin conectarse. Capturas inspeccionadas. [Reporte y capturas](docs/migracion/sistemas-herramientas-20261009/navegador.json).

**34/34 sincronizaciones sobre PostgreSQL 18.6 desechable**, incluidos cuatro casos nuevos de módulos: 1501/repetición/IDs, fallos preservando copia, rollback de catálogo/metadatos/resultado/auditoría y manual/automática con/sin ILDA. Corrida `native-postgres-20261009-084200-2373f124`. **7/7 recorridos Edge → ASP.NET → PostgreSQL**: borrador sin catálogo, búsqueda/ID/persistencia, renombre/baja histórica, Otra/Otro, dos sesiones/SignalR/reserva/autoguardado, filas del mismo procedimiento, móvil/teclado/enviado. Corrida `native-postgres-20261009-084018-c939ad44`, cero errores JavaScript. [Evidencia navegador PostgreSQL](docs/migracion/sistemas-herramientas-20261009/navegador-postgresql.json). Motores detenidos al finalizar.

**52/52 comprobaciones EF/PostgreSQL**: aplicación desde cero/con esquema anterior, repetición, rollback, rechazos de contenido inesperado y conservación de enviados/respuestas. Se amplió el verificador de sólo lectura para reconocer `SiiModulesCatalog` pendiente. Corrida `native-postgres-20261009-084557-d4d2a18e`. [Evidencia EF](docs/migracion/sistemas-herramientas-20261009/migraciones-postgresql.json).

**12/12 recorridos Edge de Sincronizaciones con ASP.NET/PostgreSQL**, cero errores JavaScript: cola/exclusión, resultado separado UR/módulos/ILDA, parcial por módulos, programación con/sin ILDA, revocación, recuperación y escritorio/móvil. Se actualizaron selectores de pruebas antiguas a «Trámite o servicio»; no se cambió la etiqueta vigente para satisfacerlas. Corrida `native-postgres-20261009-084856-06da90d2`; [reporte](docs/migracion/sistemas-herramientas-20261009/sincronizaciones-navegador.json).

**49 escenarios de edición comprobados en PostgreSQL**: `native-postgres-20261009-085429-706ad183` pasó 48/49; el caso restante esperaba rechazar una referencia histórica intacta. Se ajustó a la regla vigente de conservarla y permitir su corrección explícita. La repetición `native-postgres-20261009-085927-33d92b32` pasó 2/2, incluidos ese caso y el rechazo de borrar/renombrar un código confirmado (también con null). [Evidencia agregada con ambas corridas](docs/migracion/sistemas-herramientas-20261009/edicion-postgresql.json). Se actualizaron fixtures antiguos de PUT retirado, Otro destinatario, eliminación sin intención y envío fuera de primera etapa; no se relajó producción para satisfacerlos. Las restricciones de primera etapa, revocaciones, concurrencia, reservas, auditoría, enviados y ambos casos nuevos de persistencia de módulos pasaron.

### Pendiente y límites

Los primeros intentos con `pg_ctl` fallaron por relanzamiento con token restringido (87/3). Se resolvió mediante `-DirectStart`, que inicia sólo el nuevo clúster verificado en `.artifacts`, con el mismo usuario/restricciones y sin elevación. Para Edge, `TDV2_TEST_EPHEMERAL_TLS=true` usa HTTPS efímero en loopback sin instalar certificados. Son adaptaciones del entorno sintético; no modifican el sitio productivo ni recurren a bases existentes.

Falta SII real: SELECT sobre `sii.MODULOS_SII`, DesarrolloSII, autenticación/TLS, tipos, volumen/rendimiento y aceptación institucional/dispositivos físicos. La migración institucional queda para el operador. Sin acceso a servicios/bases institucionales, cambios en Herd/Nexo/credenciales, push ni despliegue Ubuntu. [Contrato del formulario, migración/comandos exactos y operación](docs/migracion/SISTEMAS_HERRAMIENTAS.md).


## 2026-10-08 — Recuperación de densidad compacta en Identificación

### Implementado

Se retiraron exclusivamente las ampliaciones de tamaño/espaciado de la última entrega. Captura vuelve a 13 px de `capture-scroll`, encabezados a 12 px del tema con peso **700**, instrucciones a `body2`, ayudas/opciones de destinatarios a 14 px. Se recuperaron los tamaños pequeños, padding de celdas y anchos mínimos anteriores de los componentes; se quitaron las columnas y el ancho fijo de 2045 px. El tema global se conserva; no se restauraron archivos completos.

Se mantienen contraste/foco visible, tabla, texto multilínea, instrucciones, siete categorías, ayudas, colores de prioridad y todos los cambios de eliminación/reservas/autoguardado. Ningún cambio de backend, autorización, datos o migraciones. La referencia a 16 px de la entrega anterior describe su estado histórico, sustituido por este ajuste visual.

### Verificado

TypeScript y build Vite correctos; diff sin errores de espacios. **16/16 recorridos Edge/React/ASP.NET/PostgreSQL sintético**, cero errores JavaScript, sobre el código local final. Incluyen teclado, selectores/ayudas, dos sesiones/pestañas, reserva automática, autoguardado, eliminación directa/renovación en vuelo y conservación de históricos. Las expectativas visuales existentes se ajustaron a los valores originales, sin añadir una suite nueva.

[Reporte](docs/migracion/identificacion-compacta-20261008/navegador.json) y capturas: [1366 px](docs/migracion/identificacion-compacta-20261008/tabla-1366.png), [1920 px](docs/migracion/identificacion-compacta-20261008/tabla-1920.png), [768 px](docs/migracion/identificacion-compacta-20261008/tabla-768.png), [móvil 360 px](docs/migracion/identificacion-compacta-20261008/tabla-360.png). Escritorio y móvil inspeccionados; sin desbordamiento de página y con scroll dentro de la tabla. Corrida `native-postgres-20261009-055750-b66f6187`; clúster aislado detenido.

### Pendiente y límites

No requiere nueva migración ni reversión. Continúan pendientes las instalaciones/validaciones institucionales anteriores y la aceptación en dispositivos físicos. No se modificaron bases institucionales, Nexo, Herd ni credenciales; no hubo push ni despliegue.

## 2026-10-08 — Tabla de Identificación, categorías vigentes y renovación tras eliminar

### Implementado

Identificación conserva tabla de ocho columnas, código junto a Fuente, tipografía original y edición directa. Texto/controles de 16 px, encabezados 700, valores normales, columnas estables, textos multilínea y acciones de 44 px; scroll contenido en móvil. Instrucciones y ayudas actualizadas; Prioridad conserva números/colores y no muestra Ejemplo en su ayuda.

Destinatarios ofrece las siete categorías definitivas, sin Otro/Externo ni captura de detalle. Nueva migración **`20261009052502_CurrentRecipients`**: retira sólo esos valores JSON exactos y `usuarioOtro` en borradores del ejercicio vigente, conserva otras selecciones/respuestas, versiona filas/formato, invalida sus reservas y audita transaccionalmente. Rechazo localizado de peticiones antiguas; vacío queda pendiente. Enviados e históricos bloqueados conservan originales, mostrados como consulta. Las referencias anteriores a Otro/Externo como opciones vigentes quedan sustituidas por esta entrega.

**Causa reproducida:** `commitChange` renovaba también al retirar; una renovación con intención de eliminación podía llegar después del PATCH y `FormRemoval.Changes` buscaba la fila ya borrada. Ahora espera actividad anterior, no renueva retiros y limpia estado después del ACK. El backend acredita un retiro mediante su recibo y responde 409 `registro_eliminado` sin excepción esperada; ID desconocido es 404, nunca éxito. Revalida autorización primero. Se mantienen cascada/versiones/auditoría/exclusión ILDA, reintentos idempotentes y protección de enviados. También se evita reaplicar una lectura inicial consumida tras un 422, conservando estados remotos pendientes necesarios para recibos antiguos.

### Verificado

- TypeScript, build Vite y solución Release finales correctos; .NET con cero errores y advertencias. La primera compilación había emitido NU1900 por auditoría de NuGet no disponible, sin errores de código.
- **78/78 frontend**, **62/62 dominio/HTTP en memoria**, **20/20 PostgreSQL más 2/2 ampliaciones** (21 escenarios diferentes; uno repetido/reforzado), **50/50 migraciones EF**. Destinatarios/avance, permisos, revocación, bloqueos de etapa, históricos/enviados, rollback, versiones, reservas simultáneas, respuesta perdida, eliminación/relaciones y sincronización ILDA sintética.
- **16/16 Edge/React/ASP.NET/PostgreSQL**, cero errores JS. Dos sesiones/pestañas, escritura/Tab/Select, nuevas categorías y recarga, ayudas, eliminación directa/Cancelar, renovación en vuelo sin nueva actividad del retiro, liberación automática y consulta histórica. Escritorio 1920/1366, tableta 768, móvil táctil 360 y viewport 683 equivalente al contenido al 200 %. Capturas inspeccionadas; todos los clústeres desechables detenidos.

[Causa, evidencia, capturas, comandos y alcance exacto](docs/migracion/IDENTIFICACION_TABLA.md). Nueva migración preparada/probada únicamente en bases sintéticas. No cambian participación, roles, autenticación ni Nexo; fotos y edición automática se conservan.

### Pendiente

Aplicación explícita posterior de la migración por el operador. Zoom nativo al 200 %, lector de pantalla, dispositivos físicos y aceptación institucional: Edge headless no aplicó el atajo de zoom (`nativeZoom200=1`), por lo que no se acredita esa comprobación con la captura equivalente. No se modificaron Nexo, Herd, credenciales ni bases institucionales; no hubo push ni despliegue.

## 2026-10-08 — Procedimientos Institucionales, primera etapa

### Implementado

- Nombre visible nuevo y autorización centralizada compatible con `procedimientos_institucionales`/`procesos_operativos`, misma ruta `/inicio`, sin duplicar navegación ni ampliar permisos. Se documentó la edición posterior del módulo existente de Nexo conservando ID, jerarquía y asignaciones; no se ejecutó sobre Nexo. `configuracion_procesos` y participación de UR siguen iguales.
- Inicio oculta las dos descripciones solicitadas sólo para administrador efectivo. Contexto, Identificación general y Sistemas son las pestañas de primera etapa. Administrador efectivo con interruptor **Mostrar secciones posteriores**, apagado al entrar. El servidor controla reservas, renovación y guardado, incluidos medios; no basta con ocultar pestañas. Representación usa el objetivo y la vista de prueba conserva consulta.
- Avance/revisión parciales del servidor separados del avance total: no exigen secciones ocultas ni generan un envío parcial. La etapa permanece borrador; completar el porcentaje no envía el formato. Se conservan respuestas ocultas y enviados inmutables. El administrador mantiene sólo sus facultades previas por UR y responsabilidad para envío completo.
- Ayudas MUI reutilizables con los textos solicitados, ejemplo/consejo, Cerrar con X, Escape y retorno de foco; se consultan sin adquirir reservas, también en lectura. Etiquetas nuevas; destinatarios con **Externo**, colección única y **Otro** con `usuarioOtro` separado que permanece al desmarcar. Vacíos se guardan como borrador, pero cuentan pendientes. Sin nueva limpieza de respuestas.
- Prioridad con círculos, números, etiquetas cortas y paleta solicitada, sin opción seleccionable para el indicativo. Sistemas ofrece Procedimiento sólo con código confirmado y V/A; el backend valida asociaciones nuevas/cambiadas y permite corregir históricos sin comparaciones.
- Retiro ILDA permitido mediante exclusión local por UR/ejercicio/ID. `FormRemoval` calcula la cascada en servidor; verifica todas las reservas/versiones y rechaza cambios ajenos a esa operación, especialmente en secciones ocultas. Retiro, exclusión, auditoría y recibo idempotente son transaccionales. La réplica completa permanece intacta y las consultas individuales/por conjuntos omiten el registro excluido. Nueva migración **`20261009032858_FormIldaExclusions`**, exclusivamente preparada/probada en entorno sintético. Reversión protegida ante exclusiones o enviados.

[Contrato, comando exacto de instalación, pruebas y Visual Studio](docs/migracion/PRIMERA_ETAPA.md). Se conservaron los cambios locales previos, fotografías/cachés, reservas automáticas, autoguardado, eliminación directa y ausencia de Resolver.

### Verificado

- TypeScript y build Vite correctos. Build Release ASP.NET/solución: cero errores/advertencias. **76/76** pruebas frontend, incluyendo motor de captura, versiones, respuesta perdida, desconexión, reservas, navegación y fotografías. **62/62** dominio/transporte ASP.NET (dobles en memoria; no sustituyen PostgreSQL).
- **19/19** casos HTTP/PostgreSQL aislado, nueve nuevos de primera etapa más diez regresiones de concurrencia/seguridad. Alias y revocación, perfil efectivo/representación/prueba, secciones prohibidas y medios, progreso parcial, Otro/Externo, conservación de respuestas ocultas/enviados, V/A/códigos, cascada de eliminación, reserva ajena y obsoleta, rollback de auditoría, reintento idempotente y exclusiones en lectura individual/conjunta.
- **13/13** recorridos Edge/React/ASP.NET/PostgreSQL; cero errores JavaScript. Dos sesiones y dos pestañas, primera escritura retenida durante adquisición, Tab, Select múltiple y Prioridad, ayudas por clic/teclado/toque/Escape sin liberar, Otro al desmarcar/recargar, eliminación ILDA directamente sin enfocar y Cancelar sin cambios, no reaparición tras nueva publicación del inventario sintético y control administrativo. Escritorio 1366/1920 y móvil táctil emulado 360 px con movimiento reducido. Capturas inspeccionadas.
- **44/44** comprobaciones de migraciones EF en PostgreSQL aislado: snapshot/modelo, siete migraciones, actualización desde seis sin limpieza de respuestas, repetición sin cambios, datos/instantáneas/roles conservados, reversión protegida y recibos/versión del proveedor. Corrida `native-postgres-20261009-035816-30007467`.
- Prueba adicional de retiro: ejecutó el **sincronizador de producción con lector ILDA sintético**, conservó la réplica y la exclusión evitó reaparición. También rechazó restaurar la fila con un parche directo. Arranque equivalente a **TDV2 HTTPS + React**: Vite 5173, HTTPS 7136, React, protección anónima 401 y HMR por `wss://localhost:7136`, sin consultas de datos ni servicios institucionales. No se automatizó la GUI de Visual Studio. Corrida `native-postgres-20261009-040001-a4946d37`; 2/2 agregados, cero errores JavaScript. Todos los clústeres se detuvieron.
- Durante la validación se corrigieron expectativas de la prueba anterior (secciones posteriores ahora restringidas) y del helper de navegador (buscar una fila de una pestaña desmontada). Se corrigió el icono para el export real de MUI instalado. La prueba de reversión motivó proteger también la nueva tabla antes de retirar su historial. No se modificaron migraciones anteriores.

Evidencia sintética: [frontend](docs/migracion/primera-etapa-20261008/frontend.txt), [19 casos PostgreSQL](docs/migracion/primera-etapa-20261008/postgresql.json), [13 recorridos finales del navegador](docs/migracion/primera-etapa-20261008/navegador.json), [44 comprobaciones EF](docs/migracion/primera-etapa-20261008/migraciones.json), [sincronizador y arranque](docs/migracion/primera-etapa-20261008/sincronizacion-arranque.json), [HTTPS/React/HMR](docs/migracion/primera-etapa-20261008/https-react-hmr.json). Capturas inspeccionadas: [administrador 1920 px](docs/migracion/primera-etapa-20261008/etapa-admin.png), [prioridad 1366 px](docs/migracion/primera-etapa-20261008/etapa-prioridad.png), [selector móvil](docs/migracion/primera-etapa-20261008/etapa-mobile.png) y [ayuda móvil](docs/migracion/primera-etapa-20261008/etapa-ayuda-mobile.png). Última corrida de navegador `native-postgres-20261009-040334-46ba8511`: se esperó el estado autorizado antes de capturar el administrador, 13/13 y cero errores JavaScript. Sólo se copiaron reportes/capturas sintéticos, nunca configuración privada del clúster.

### Pendiente y límites

El operador debe aplicar explícitamente la migración a su destino revisado e instalar TDV2 compatible **antes** de renombrar/publicar el módulo existente en Nexo. No se modificaron Nexo, Herd, credenciales ni bases institucionales; no hubo push ni despliegue. La aceptación con Microsoft/Nexo reales y dispositivos físicos sigue pendiente. El recorrido `first-stage` verifica la experiencia vigente; las evidencias de recorridos completos de entregas anteriores corresponden a su fecha y no acreditan la nueva restricción. Las propuestas siguen en memoria de la pestaña y con descarga ante fallos reales; no se promete recuperación tras cierre forzado.

## 2026-10-08 — Edición automática sin resolución manual

### Implementado

Se mantiene entrar, editar, guardar al salir y liberar automáticamente. Se retiraron **Resolver**, **Resolver eliminación**, la comparación de propuestas y sus métodos/componentes sin uso. Se conservan reservas por fila/pregunta/criterio, permisos, enviados inmutables, fotos persistentes, avatares/colores, administración central/delegada y participación vigente. Las menciones a recuperación manual en entregas anteriores de este archivo quedan como evidencia histórica, sustituidas por esta entrega.

**Causa reproducida antes del cambio:** una referencia de Sistemas a un código no confirmado devolvió 422 con reservas válidas y rechazó también un acuerdo independiente enviado en la misma operación. El motor bloqueaba después todos los bloques enviados como si fuera un conflicto. [Reproducción original](docs/migracion/edicion-automatica-20261008/reproduccion-previa.json), `native-postgres-20261008-174322-d8a19265`. El código generaba y ofrecía códigos desde el navegador antes del commit; el guardado parcial validaba todo el documento. No se examinó contenido institucional.

Ahora el servidor confirma códigos y criterios vacíos bajo la transacción de UR. El borrador valida filas cambiadas y vínculos afectados, identifica bloque/campo y permite corregir con reserva vigente. El envío conserva validación completa. Los registros independientes tienen operaciones/recibos separados; las eliminaciones con relaciones siguen siendo atómicas. No se limpian vínculos históricos para evitar errores.

Se conserva texto durante peticiones y se confirman respuestas perdidas con el mismo UUID, con reintentos limitados/progresivos. Volver al valor original mientras viaja un guardado espera el recibo y el cambio posterior antes de liberar. Tras vencimiento se exige nueva reserva y base idéntica; el texto incompatible queda accesible sin sobrescribir ni comparar. Se protegen primeras teclas, pegado y Tab mientras se prepara la edición. Siguen autoguardado de un segundo, descarga ante fallo real y revalidaciones SignalR cada 15 segundos.

[Detalle, límites y comandos desde Visual Studio](docs/migracion/EDICION_AUTOMATICA.md). **Sin nueva migración** ni dependencias. No se modificaron bases institucionales, credenciales, Nexo, Herd, autenticación ni Ubuntu. Sin push/despliegue.

### Probado

- TypeScript, Vite y solución Release correctos; .NET sin errores ni advertencias. **75/75** pruebas frontend (41 del motor/captura) y **62/62** de dominio/aplicación. Incluyen cachés de fotografías, referencias estables, validación corregible, pendientes independientes, primeras teclas/pegado/Tab, recibos y edición durante guardado.
- **28/28** escenarios de edición en PostgreSQL 18 aislado: [evidencia](docs/migracion/edicion-automatica-20261008/postgresql.json), `native-postgres-20261008-180443-63d43575`. Altas simultáneas, pertenencia sin borrar históricos, usuarios/pestañas, exclusión mutua, registros distintos, reservas vencidas/reasignadas, revocación, auditoría/rollback, eliminación con dependencias, ILDA y envío concurrente/inmutable.
- **33/33** recorridos Edge/React/ASP.NET/PostgreSQL: [evidencia](docs/migracion/edicion-automatica-20261008/navegador.json), `native-postgres-20261008-181904-0008b450`. Primera escritura/pegado/Tab durante adquisición, Select, dos sesiones/pestañas, eliminación directa/Cancelar en cuatro tablas, carreras durante diálogo, respuesta perdida, reconexión sin comparar, criterios independientes, foto/tooltip, tres ciclos SignalR, envío y retiro central. Cero errores JavaScript. [Captura](docs/migracion/edicion-automatica-20261008/captura.png) y [eliminación ocupada](docs/migracion/edicion-automatica-20261008/eliminacion-ocupada.png), inspeccionadas. Fuentes externas bloqueadas sólo en el host sintético.
- Una corrida previa de Edge se detuvo porque una aserción esperaba el texto anterior al nuevo caso de respuesta perdida; se actualizó al texto que éste guarda. La ampliación del selector múltiple también detectó que el verificador debía esperar la liberación: recibir contenido confirmado todavía no autoriza la fila. Se conserva esa separación en las pruebas.
- Verificación específica final: **6/6** de selección múltiple/Prioridad en Edge, sin errores JS, con dos sesiones, recarga, teclado/Escape, foco y móvil táctil emulado de 360 px con movimiento reducido: [resultado](docs/migracion/edicion-automatica-20261008/seleccion-multiple.json), [captura móvil](docs/migracion/edicion-automatica-20261008/movil.png). **2/2** regresiones PostgreSQL repetidas con validación de bloques malformados (422 con campo, no error 500): [resultado](docs/migracion/edicion-automatica-20261008/regresion-final.json). Corrida `native-postgres-20261008-182826-7563c721`; estos dos casos amplían los ya incluidos en los 28, no son casos adicionales. Todos los clústeres aislados detenidos. Los últimos ajustes de liberación tras volver al valor original y limpieza de grupos confirmados están cubiertos también por las 41 pruebas del motor; no se modificaron luego las reglas de servidor.

### Pendiente y límites

Aceptación institucional de Nexo/Microsoft/proxy y móviles físicos/IME. El texto no confirmado vive en la pestaña, con aviso de salida y descarga; no se persisten respuestas privadas en almacenamiento del navegador ni se promete recuperarlas tras un cierre forzado. Las pruebas locales no acreditan despliegue ni conectividad real. Se conserva **TDV2 HTTPS + React**; las escrituras desde Visual Studio se prueban con el ejecutor aislado, sin usar User Secrets institucionales.

## Persistencia de fotografías — actualización 2026-10-07

### Implementado

La cabecera utiliza `AccountPhotoProvider` sobre las páginas: conserva su miniatura durante navegación SPA y renovación en segundo plano. Al recargar recupera la caché privada del servidor tras autorizar la petición. Caché de 32 MiB por proceso, renovación a los 15 minutos y conservación ante fallos temporales por **un máximo de una hora desde la última verificación correcta**, sin prolongarla por reintentos. Espera progresiva de 30 segundos a 15 minutos; ausencia confirmada y errores permanentes retiran la imagen y usan iniciales.

Contexto opaco ligado a sesión, cuenta Microsoft e identidad efectiva; cancelación de respuestas antiguas y limpieza inmediata al cambiar cuenta/representación o cerrar sesión, también en otras pestañas mediante una notificación efímera. El endpoint comprueba nuevamente sesión/revisión tras una descarga lenta. Un 401/403/409 de TDV2 no conserva la foto como si Microsoft estuviera temporalmente caído. Se mantienen fotos verificadas del objetivo, participantes autorizados con reserva vigente y renovación serializada de tokens. Sin imágenes/tokens en almacenamiento web ni caché de permisos.

Se corrigió además el transporte de «Usar otra cuenta»: `/connect?account=other` requiere navegación completa, no consulta JSON. Sin cambios de hints ni reglas OAuth. Avatares, colores, nombres, reservas, autoguardado, eliminación y configuración de participación conservados. **No requiere nueva migración**; no modifica las migraciones ni pendientes anteriores.

Se identificó en el código el conflicto posible entre Windows/Ubuntu con la misma base y anillos Data Protection incompatibles. Un fallo de descifrado registra `token_protection` sin tokens ni excepción original y retira la foto al renovar. Probado con cifrado sintético de otro proveedor, sin inspeccionar ni compartir llaves reales. [Política, diagnóstico y comandos](docs/migracion/PERSISTENCIA_FOTOGRAFIAS.md).

### Probado

Compilación Release de la solución: cero errores y advertencias. TypeScript y Vite correctos; **71/71** pruebas frontend y **62/62** de dominio/transporte. Regresión HTTP/PostgreSQL: **152/152**, corrida `native-postgres-20261007-230704-85dcb3ca`, [evidencia](docs/migracion/fotografias-persistencia-20261007/postgresql.json). Incluye permisos, revocación, token refresh, cifrado incompatible, solicitudes simultáneas, colaboración y enviados. Clúster desechable detenido al terminar.

Tras el refuerzo final de revisión de contexto y límite/cancelación de la descarga: **15/15** casos específicos HTTP/PostgreSQL de fotos/tokens, más **9/9** escenarios en Edge. Corrida final `native-postgres-20261007-231448-59f7ff6f`: [backend](docs/migracion/fotografias-persistencia-20261007/fotografias.json) y [navegador](docs/migracion/fotografias-persistencia-20261007/navegador.json). El runner cuenta el recorrido Edge como un caso adicional (16/16), no como nueve casos HTTP.

Edge verificó navegación sin nuevas peticiones, recarga sin nueva descarga Graph, renovación lenta, error temporal, ausencia confirmada, representación, identidad objetivo verificada, teclado/móvil de 360 px, otra pestaña, cambio de cuenta y logout; sin errores JavaScript. Los primeros intentos detectaron una espera incorrecta del reloj sintético y el problema real de navegación OAuth con parámetros; se corrigieron, no se cuentan como aprobados.

Regresión del formato en Edge sobre el código final: **32/32**, sin errores JavaScript, `native-postgres-20261007-231643-6b8a7779`. [Evidencia](docs/migracion/fotografias-persistencia-20261007/regresion-formato.json): dos sesiones/pestañas, fotos/colores/tooltip sin perder foco, reservas automáticas, autoguardado, Select, eliminación directa, propuestas ante desconexión, tres ciclos SignalR de 15 segundos, envío definitivo y revocación. Todos los clústeres aislados quedaron detenidos.

### Pendiente

Aceptación con Microsoft real y comprobación de la topología institucional de cifrado Windows/Ubuntu. No se conectó a bases institucionales ni se modificaron Nexo, Herd, credenciales o llaves. Sin push ni despliegue. Las pruebas usan miniaturas sintéticas; no acreditan disponibilidad de fotos reales ni accesibilidad con dispositivos físicos.

## Usuarios que atiende y Prioridad — actualización 2026-10-07

### Implementado

«Usuarios que atiende» sustituye el texto libre por selección múltiple MUI con cinco opciones, casillas y menú que permanece abierto al marcar/desmarcar. Colección JSON bajo la clave existente `usuario`, sin duplicados ni valores predeterminados. Nuevos registros manuales y de ILDA comienzan vacíos. Validación, avance y revisión de envío comparten la regla: una lista vacía es pendiente. El texto histórico de los enviados permanece en consulta.

Prioridad muestra «Selecciona una prioridad» sólo como indicativo del campo vacío, fuera de las opciones del menú; conserva colores, selección única y valores existentes. Ambos controles reutilizan las reservas automáticas y autoguardado del registro, sin cambios en el motor colaborativo, permisos, avatares ni eliminación directa.

Migración de datos **`20261007195019_StructuredUsersServed`**, sin cambio de esquema: limpieza explícita, única y transaccional del campo anterior en borradores del ejercicio editable, preservando otras respuestas, registros, prioridades y listas nuevas. Actualiza avance/versiones, invalida reservas anteriores sólo de los bloques modificados y audita. Los enviados, sus instantáneas y los borradores históricos bloqueados por cambio de ejercicio no se modifican. No existe limpieza al abrir ni guardar. [Detalle y comando de actualización](docs/migracion/USUARIOS_ATENDIDOS.md).

### Probado

Compilación de `tdv2.slnx` en Release: cero errores y advertencias; **62/62** pruebas de dominio/transporte. **146/146** casos HTTP/PostgreSQL: [evidencia](docs/migracion/usuarios-atendidos-20261007/postgresql.json), corrida `native-postgres-20261007-204401-b254e2a9`. Se verificaron listas inválidas/duplicadas, persistencia sin limpieza posterior, envío rechazado con lista vacía y aceptado con opciones guardadas, prioridades intactas, permisos, reservas, revocación, ILDA y enviados. Clúster desechable detenido al terminar.

TypeScript y Vite correctos; **64/64** pruebas frontend. **41/41** comprobaciones EF/PostgreSQL aislado, incluida limpieza limitada, conservación exacta de enviado/instantánea y borrador de otro ejercicio, prioridades 4/17, rollback si falla la auditoría y repetición del comando/script sin borrar nuevas selecciones. [Evidencia EF](docs/migracion/usuarios-atendidos-20261007/ef.json), corrida final `native-postgres-20261007-205841-e6131401`. El primer intento detectó que `Down` retiraba parcialmente el historial antes de rechazar un enviado; se corrigió. La corrida anterior de 40 casos se amplió con la protección del ejercicio histórico; no se suman ejecuciones repetidas. SQL final generado sin conexión en `.artifacts/usuarios-atendidos.sql`.

**6/6** escenarios específicos en Edge con ASP.NET/PostgreSQL reales y Microsoft/Nexo sintéticos: reserva confirmada antes de abrir, selección múltiple y deselección sin cerrar, autoguardado/recarga, dos sesiones independientes con bloqueo y liberación en vivo, teclado/Tab/Escape, foco, scroll, indicativo de Prioridad ausente de las cinco opciones y móvil táctil emulado de **360 px** con movimiento reducido. Sin errores JavaScript. [Evidencia](docs/migracion/usuarios-atendidos-20261007/navegador.json), corrida `native-postgres-20261007-202927-9c2c554e`; [captura escritorio](docs/migracion/usuarios-atendidos-20261007/usuarios-desktop.png) y [móvil](docs/migracion/usuarios-atendidos-20261007/usuarios-mobile.png), inspeccionadas. El verificador espera la revalidación inicial antes de actuar; los intentos que pulsaban durante la carga no se cuentan como aprobados.

Regresión completa del formato sobre el bundle final: **32/32** escenarios en Edge, sin errores JavaScript. Incluye enlaces de pendientes al nuevo selector, reservas de dos sesiones/pestañas, eliminación directa en cuatro tablas, Cancelar sin escrituras, conflictos de versión, recuperación, foco/Select, avatares/fotos, tres ciclos SignalR de 15 segundos, envío, revocación central y cierre de sesión. [Evidencia](docs/migracion/usuarios-atendidos-20261007/regresion-formato.json), `native-postgres-20261007-205248-59628bfd`.

### Pendiente

Aplicar la migración explícitamente por el operador al destino revisado, con la aplicación detenida; pruebas con dispositivos físicos y lector de pantalla. **No se aplicó a bases institucionales** ni se modificaron Nexo, Herd o credenciales. Sin push/despliegue. Se conservan los pendientes institucionales y las reglas de participación actuales.

## Presentación de módulos y Configuración — actualización 2026-10-07

### Implementado

Encabezado compartido con título único de 24–26 px/600, descripción de 14 px, acciones adaptables y rutas de navegación en submódulos. Barra blanca alineada con el contenido, logo y tipografía conservados, separación de 32 px en escritorio y altura reservada medida mediante `ResizeObserver`. Menús con selección accesible, jerarquía publicada y cuenta separada sin desplazar la página.

Accesos compactos y componente `SettingsSection` para Configuración. Sincronizaciones separa fuentes, programación y ejecuciones; carga y errores no desplazan los campos, y el historial vacío es legible en móvil. Pruebas de acceso diferencia consulta de representación con escritura; se conserva su advertencia real. Configuración procesos sólo cambia presentación, etiquetas y ubicación de acciones/errores. Sin cambios de opciones, reglas de participación, autorización, autenticación, sincronización ni edición colaborativa.

[Detalle, comandos y capturas antes/después](docs/migracion/UI_MODULOS_CONFIGURACION.md). Se conservaron los cambios locales; [119 archivos de backend/lógica protegida sin variación](docs/migracion/ui-modulos-20261007/codigo-conservado.json). No se crea ninguna migración en esta entrega.

### Probado

TypeScript y compilación Vite correctos; **64/64** pruebas frontend. Edge con ASP.NET y PostgreSQL desechable: **32/32** recorridos de captura, **13/13** de acceso, **4/4** de alcance y **11/11** de sincronización (`native-postgres-20261007-174835-2f9c9823`). Después del ajuste de los avisos, **11/11** de sincronización repetidos sobre esa versión final (`native-postgres-20261007-175933-719f3118`); no se suman como casos diferentes. Sin errores JavaScript. Se comprobaron reservas, eliminación directa, foco, avatares/fotografía, autoguardado, propuestas, revocación, enviados y representación con dobles institucionales sintéticos.

Siete pantallas completas a **360/768/1366/1920 px** y ampliación **CSS 200 %**, con capturas comparables, un H1, sin desbordamiento de página ni títulos ocultos por la barra. Pruebas de teclado, menú móvil, selección activa, movimiento reducido, textos largos, permisos distintos, carga/error sin salto, conservación del borrador, Select y cancelación de confirmación sin escrituras. [Evidencia visual y funcional](docs/migracion/ui-modulos-20261007/after.json), corrida final `native-postgres-20261007-181024-1b50e32d`; **73 imágenes** anteriores/posteriores conservadas. Los intentos que detectaron capturas prematuras o pérdida de propiedades del selector se descartaron: el verificador espera que React monte el título y el ajuste de etiqueta conserva todos los `slotProps` originales de Autocomplete.

### Pendiente

Revisión manual con zoom nativo del navegador, lector de pantalla y dispositivos físicos: la evidencia automática de 200 % es ampliación CSS, no una sesión manual del navegador. Las nuevas reglas de participación continúan aplazadas y los pendientes institucionales anteriores permanecen vigentes. No se modificaron Nexo, Herd, User Secrets ni bases institucionales; no hubo push ni despliegue. Las bases de prueba fueron exclusivamente desechables.

## Respuesta de captura, avatares y fotografía — actualización 2026-10-07

### Implementado

Seguimiento incremental de bloques modificados; una tecla no compara todo el documento ni reprograma todas las reservas. Las filas memoizadas dependen de su reserva y sus relaciones reales. Estados remotos equivalentes conservan referencias y evitan avisar otra vez a React; mantienen comprobaciones de autorización/caducidad. Se integra el montaje previo de pestaña activa, sin perder propuestas ni cambiar autoguardado, eliminación directa, envío o SignalR.

Avatar con fotografía Microsoft opcional, tooltip MUI con nombre, foco visible y manejo de ratón/teclado/tacto sin adquirir/liberar reservas. Colores coordinados por identidad efectiva conservados. Endpoint protegido por acceso al formato y participante con reserva vigente, identidad Microsoft verificada, caché renovable sin permisos cacheados ni directorio público. Representación usa foto del objetivo o iniciales, nunca la foto del administrador.

Nueva migración `20261007161549_ParticipantPhotographs`: FK opcional en reservas a usuario Microsoft verificado, sin cambiar contenido ni instantáneas. **Las nuevas reglas de participación permanecen pendientes**; configuración/alcance existentes conservados, con [huellas antes/después](docs/migracion/evidencia-captura-politicas-conservadas.json). Continúan separadas las vías central explícita y delegada. [Detalle, comandos y recorrido de prueba](docs/migracion/CAPTURA_FOTOGRAFIAS_RENDIMIENTO.md).

### Probado

TypeScript y Vite correctos; **64/64** casos frontend y **62/62** de dominio/transporte. **34/34** comprobaciones EF en PostgreSQL desechable, incluyendo upgrade con enviado intacto, FK, repetición y script idempotente: [evidencia](docs/migracion/evidencia-captura-fotografias-migraciones.json), corrida `native-postgres-20261007-163801-c1df0a41`. Los intentos anteriores que detectaron el rollback parcial y un dato sintético incompleto no se cuentan como aprobados.

Compilación final `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false`: cero errores y advertencias. `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build`, en Production y sin conexión, confirma snapshot vigente. SQL de la nueva migración generado en `.artifacts/participant-photographs.sql`. El bundle final está reconstruido sin contadores de instrumentación.

**144/144** casos HTTP/PostgreSQL de regresión completos: [evidencia](docs/migracion/evidencia-captura-fotografias-postgresql.json), `native-postgres-20261007-165619-f90bd19c`. Incluyen fotografía protegida/cacheada/renovable, rechazo de identidad Graph ajena, representación con objetivo verificado o iniciales, permisos, colaboradores centrales/delegados, concurrencia, revocación, auditoría, ILDA y bloqueo de enviados. La corrida anterior interrumpida por el reinicio de la sesión no se usa como evidencia final. Todos los clústeres de prueba terminaron detenidos.

**31/31** escenarios de captura en Edge, sin errores JS: [evidencia](docs/migracion/evidencia-captura-fotografias-react.json), `native-postgres-20261007-162753-76a64950`. Reservas simultáneas, distintas filas/pestañas/sesiones, cancelación y eliminación sin foco en cuatro tablas, versiones/dependencias, desconexión/propuestas, revocación central, envío, Select, avatar/tooltip y tres ciclos de SignalR de 15 segundos.

**2/2** escenarios adicionales sobre la versión final del avatar: primera escritura confirmada, selección/cursor, inserción múltiple, Tab, fotografía, tooltip con ratón/teclado/toque real emulado y liberación sin recarga. [Evidencia táctil](docs/migracion/evidencia-captura-avatar-tactil.json), `native-postgres-20261007-165504-84389d5b`. La prueba táctil detectó que impedir el foco del puntero suprimía los eventos compatibles que esperaba el tooltip; se corrigió con apertura controlada para touch/pen, sin adquirir/liberar reservas. No se cuenta como aprobada la corrida que detectó ese defecto.

Medición antes/después con 20/100/200 registros del código local: filas ajenas renderizadas **19/99/199 → 0/0/0**. Una sola renovación durante 58 entradas, sin PATCH por tecla. La fotografía agrega una solicitud protegida por participante. La latencia a frame no mejora uniformemente; medias/p95 y comandos EF se publican sin porcentajes inferidos en [comparación](docs/migracion/evidencia-captura-comparacion.json). El motor reduce trabajo por cambio; Inicio conserva **8 comandos EF para 102 áreas**, optimización previa. No se confunde el contador EF con todas las consultas Npgsql/Nexo.

Arranque equivalente al perfil **TDV2 HTTPS + React**: Vite 5173, Kestrel 5064/7136, certificado existente, portada/React, HMR por 7136 y rechazo anónimo; cero consultas EF durante esas páginas. [Evidencia](docs/migracion/evidencia-captura-arranque-https.json), `native-postgres-20261007-163851-a21dc7a0`. Host sintético con el pipeline real; no se operó la interfaz gráfica del depurador ni se usaron User Secrets. Capturas en 1920, 1366, 768 y 390 px, sin desbordamiento de página y con movimiento reducido.

### Pendiente

Aplicación institucional de la nueva migración por el operador, después de revisar destino e historial; fotografías/consentimiento Microsoft reales, lectores de pantalla y dispositivos físicos. No hacen falta nuevos roles, permisos Graph de directorio ni publicación Nexo para esta entrega. Se conservan los pendientes institucionales anteriores. No hubo cambios de Nexo/Herd/User Secrets/bases institucionales, push ni despliegue. F5 continúa sin DDL ni sincronización automática.

## Participación, rendimiento y presencia — actualización 2026-10-06

### Implementado

Submódulo `configuracion_procesos` bajo `configuracion`, exclusivo de administrador en contexto propio. Configuración EF versionada de niveles/tipos, revisión de impacto, confirmación protegida contra cambios de catálogo/formatos y auditoría transaccional. Participación separada de catálogo y permisos; conservación de históricos y distinción central/delegada. Reservas/escrituras se coordinan con cambios de participación mediante bloqueos breves. Migración nueva `20261006233010_ProcessParticipationAndPresence`, sin modificar las anteriores.

Sólo se monta la pestaña activa; propuestas y colaboración permanecen en el motor. Cambios parciales con referencias estables, filas memoizadas y consulta de Inicio por conjuntos. Presencia con color coordinado por identidad efectiva, avatar de iniciales, nombre e indicadores de reserva; sin modificar OAuth ni los intervalos de SignalR. [Detalle, instalación, Nexo y pruebas desde Visual Studio](docs/migracion/PARTICIPACION_RENDIMIENTO_PRESENCIA.md).

### Probado

TypeScript y Vite correctos; **60/60** pruebas frontend y **62/62** de dominio/transporte. Compilación Release con cero advertencias y errores; `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build` confirma el modelo sin cambios pendientes.

**135/135** casos de regresión HTTP/PostgreSQL ([evidencia](docs/migracion/evidencia-participacion-regresion.json), corrida `native-postgres-20261006-234641-10f1571c`). **13/13** comprobaciones finales específicas de participación, presencia y rendimiento ([evidencia](docs/migracion/evidencia-participacion-postgresql.json), `native-postgres-20261007-003025-959cb54a`): incluyen contexto propio, permisos, vista previa obsoleta, auditoría atómica, carreras con captura, reactivación sin pérdida, enviados inmutables, restricciones de delegación, colisión de colores y límite/asociaciones ILDA. Son grupos parcialmente superpuestos; no se suman como 148 casos distintos.

**31/31** comprobaciones EF en PostgreSQL desechable ([evidencia](docs/migracion/evidencia-participacion-migraciones.json), `native-postgres-20261007-001318-f3d1379c`): preservación del esquema previo, repetición sin cambios, recuperación transaccional, actualización y protección de enviados. Dos aplicadores EF simultáneos pueden competir por DDL: se verificó convergencia al repetir secuencialmente, no éxito simultáneo garantizado. Instalación con un solo operador.

**30/30** comprobaciones Edge de captura ([evidencia](docs/migracion/evidencia-participacion-captura-react.json), `native-postgres-20261007-002227-02515193`), sin errores JavaScript: eliminación directa y concurrente, Select, foco, pestañas/sesiones, reconexión, criterios individuales, enviados, revocación central y tres ciclos de 15 segundos de SignalR. Se corrigió la prioridad CSS del contorno frente a MUI tras detectarla en navegador. No se cuentan como aprobados los dos intentos anteriores que detectaron la expectativa fija de ámbar y el contorno oculto.

Medición sintética del motor con 800 filas: media **4,87 → 0,075 ms** por cambio. Inicio con 102 áreas: **311 → 8 consultas EF**; 21,4 s antes y 1,02–2,66 s después en estas corridas locales. [Mediciones y límites](docs/migracion/evidencia-participacion-rendimiento.json). El bundle anterior montaba siete pestañas y 7.445 controles; no completó su ensayo de escritura, por lo que no es una latencia válida ni una prueba aprobada.

Revisión final del bundle actual con 800 filas y **tres escenarios agrupados aprobados** ([evidencia](docs/migracion/evidencia-participacion-navegador.json), `native-postgres-20261007-003326-318d776e`): sólo una pestaña montada; apertura 2,23 s; escritura a siguiente frame media 28 ms/p95 45,4 ms en 31 eventos. Configuración y presencia revisadas en **1920×1080, 1366×768, 768×1024 y 390×844**, sin desbordamiento de página ni errores JavaScript; las tablas conservan su desplazamiento horizontal interior. Contorno de 2 px, avatar, lectura en otra pestaña, liberación automática, movimiento reducido, Cancelar sin cambios y confirmación comprobados. [Capturas y reproducción](docs/migracion/PARTICIPACION_RENDIMIENTO_PRESENCIA.md#capturas). Todos los clústeres de estas pruebas quedaron detenidos.

### Pendiente

Publicar el módulo en Nexo y aplicar la migración institucional corresponde al operador, después de su revisión. La aceptación con identidades y jerarquías institucionales reales continúa pendiente. El mensaje recibido termina en «Al pasar el mouse sobre el»; el avatar tiene tooltip con el nombre, sin asumir el texto faltante. No hubo conexiones institucionales, cambios de credenciales/Herd, push ni despliegue.

## Colaboradores asignados desde Nexo — entrega anterior 2026-10-06

### Implementado

Dos vías de autorización independientes en `FormAccess.Scopes`: concesión explícita `central` + rol efectivo coincidente, calculada por adscripción vigente, o vínculo local + concesión `aplicacion`, con las validaciones delegadas conservadas. No hay fallback por `Has`, altas locales artificiales ni conversión de una revocación local pendiente en acceso central. Local resuelve un solo formato nivel 2/3; dependencias, su nivel 2 y la rama nivel 3. Tipo N sigue excluido y los IDs de jerarquía no se sustituyen por claves visibles.

`RequestAccess` consulta concesiones vigentes aun sin enlaces locales y usa la identidad representada cuando corresponde. Es la misma autorización para listado, apertura, reservas, guardado/eliminación y SignalR. No concede envío, administración ni representación a los colaboradores. El estado vacío explica problemas de empleado, adscripción, jerarquía o asignación. Se conserva el trabajo previo de eliminación directa y captura colaborativa.

La fuente de Nexo ya publica los orígenes reales `central` y `aplicacion`; no necesita cambios de código para distinguirlos. No requiere migración EF. [Implementación, comandos y pruebas](docs/migracion/COLABORADORES_CENTRALES.md); [configuración de ambas vías](docs/migracion/CONFIGURACION_NEXO_ENTRA.md).

### Probado

TypeScript y Vite correctos; **57/57** pruebas frontend y **61/61** de dominio/transporte. Compilación Release de la solución con cero advertencias y errores. **31/31** pruebas HTTP/PostgreSQL de captura y colaboradores (23 de captura + 8 centrales), incluidos enviados inmutables y cambios/retiros de alcance. [Evidencia seleccionada de backend](docs/migracion/evidencia-colaboradores-centrales-postgresql.json), extraída de `.artifacts/native-postgres-20261006-184324-cf999ac5/verification.json`; su intento de navegador se registra aparte.

**30/30** comprobaciones finales Edge sobre React → ASP.NET → PostgreSQL, sin errores JavaScript: los 27 casos de captura conservados y tres de colaboración central. La revocación durante una escritura pendiente devuelve 403, conserva la propuesta visible, no altera la versión compartida y termina SignalR por revalidación. El estado vacío explica empleado ausente. [Evidencia de navegador](docs/migracion/evidencia-colaboradores-centrales-react.json), corrida `.artifacts/native-postgres-20261006-185205-3459ed50`, código 0 y clúster detenido. `-BrowserOnly` registra un grupo agregado, no una sola comprobación de interfaz.

**8/8** pruebas del SQL generado desde la fuente local de Nexo en otra base sintética, incluida su consulta de concesiones: confirma separación `central`/`aplicacion`, revocación y suspensión. [Evidencia SQL Nexo](docs/migracion/evidencia-colaboradores-centrales-nexo.json). Corrida `.artifacts/native-postgres-20261006-184131-6bced401`, clúster detenido. No fue una consulta a Nexo institucional.

Intentos previos conservados: `.artifacts/native-postgres-20261006-183043-648c2348` tuvo 120 casos aprobados y seis fallidos por dos sentencias parametrizadas juntas en el preparador central (cinco casos centrales y navegador). Se separaron. La corrida `184324-cf999ac5` pasó los 31 casos de backend, pero su navegador encontró que una prueba previa había retirado el módulo del colaborador; el servidor denegó correctamente. Se corrigió la publicación de módulos **del fixture** y se repitió sólo el navegador completo, con los 30 resultados aprobados indicados arriba. No se cuentan esos intentos como suites completas aprobadas. Todos sus clústeres quedaron detenidos.

### Pendiente y límites

Comprobar por el operador la publicación institucional de `nexo_concesiones` y las asignaciones explícitas. Si la vista existente ya coincide con el contrato de la fuente, no hace falta republicar Nexo; si está desactualizada, aplicar la publicación existente sin recrear recursos. No hubo conexiones a bases institucionales, cambios de credenciales, modificación de Herd, push ni despliegue. La validación sintética no certifica la configuración institucional real.

## Eliminación directa de registros — entrega anterior 2026-10-06

### Implementado

El icono rojo permite abrir la confirmación sin enfocar campos ni tener reserva propia; foco del icono y Cancelar no adquieren reservas. `removalReason` conserva permisos, carga, enviados, ILDA, propuestas pendientes y reservas ajenas con nombre/otra pestaña. Al confirmar, `engine.prepare(keys)` reserva el registro y sus relaciones. Se comparan contenido y versiones por ID antes y después de esperar; cambios requieren cancelar y revisar. El diálogo espera el guardado, bloquea doble clic y después libera; los fallos conservan recuperación contextual incluso si ya no aparece la fila en la propuesta.

Se corrigió también el orden de la confirmación de reserva: su fecha PostgreSQL se obtiene después del commit, con apertura/cierre explícitos de conexión EF, para no invalidarla con una lectura simultánea tardía. No prolonga el vencimiento ni concede escritura desde SignalR. Se conservan `canEdit`, versiones, auditoría, relaciones y enviados inmutables. Sin migraciones ni dependencias nuevas.

Archivos corregidos, comportamiento y comandos: [ELIMINACION_DIRECTA.md](docs/migracion/ELIMINACION_DIRECTA.md).

### Probado

TypeScript y Vite correctos; **57/57** pruebas frontend. Compilación Release de `tdv2.slnx` sin errores ni advertencias. **23/23** casos de captura en PostgreSQL desechable, incluida la lectura concurrente durante adquisición, relaciones, ILDA, revocación, sesiones/pestañas, auditoría, envío e inmutabilidad.

**27/27** comprobaciones Edge sobre React → ASP.NET → PostgreSQL, sin errores JavaScript. Incluyen cargar y eliminar sin enfocar campos en las cuatro tablas, Cancelar sin reservas ni cambios, otra pestaña y relaciones ocupadas, liberación que rehabilita automáticamente, contenido actualizado durante el diálogo/adquisición, competencia de dos sesiones (200/409, un PATCH y un incremento de versión) y recuperación explícita tras fallo SQL. La regresión también conserva autoguardado, prioridad, sesiones revocadas, tres intervalos SignalR y envío inmutable.

Evidencia: [PostgreSQL](docs/migracion/evidencia-eliminacion-directa-postgresql.json), [navegador](docs/migracion/evidencia-eliminacion-directa-react.json), [captura del conflicto entre sesiones](docs/migracion/eliminacion-reserva-ajena.png). Corrida final `.artifacts/native-postgres-20261006-174139-1410854d`, código 0 y clúster detenido (sin `postmaster.pid`). El total agregado 24 incluye 23 casos de backend y una entrada para el grupo de navegador; no representa 24 pruebas de backend.

Los intentos previos se conservan en `.artifacts`: `native-postgres-20261006-172736-869505a2` detectó la lectura que invalidaba la reserva; `native-postgres-20261006-173308-99b8ee8e` permitió corregir la expectativa del fallo SQL (503); `native-postgres-20261006-173943-b9769ef5` detectó que EF cerraba la conexión al confirmar. Se corrigieron antes de la corrida final y no se cuentan como ejecuciones aprobadas.

### Pendiente y límites

Publicación y aceptación institucional por el operador: el servidor publicado no se actualizó. No se modificaron bases institucionales, credenciales ni Herd; sin push ni despliegue. Las pruebas usan identidades y contenido sintéticos.

## Distribución compacta de Procesos operativos — entrega anterior 2026-10-05

### Implementado

Indicadores de 80 px en escritorio, números de 26 px y separación de 14 px; etiquetas completas en móvil. Alcance y pestañas más próximos, conteo único agrupado con título y acciones del listado, filtros sin contenedor de relleno adicional. Filas normales de 80 px separadas 8 px, nombre prioritario, clave/cantidades secundarias y avance a la derecha; crecimiento libre para nombres largos. Se conservan cabecera, tipografía, permisos, cálculos, tipo N, acceso a formatos sin dependientes y cambios locales. No se modificó captura, backend ni esquema.

### Probado

TypeScript, Vite y compilación Release correctos, cero advertencias/errores .NET; **55/55** frontend. Revisión visual y funcional del bundle real con catálogo sintético en 1920×1080, 1366×768, tableta 768×1024 y móviles 390×844/320×740, sin desbordamientos ni errores JavaScript. En 1366×768 la primera área sube de 600 a 459 px y caben tres filas completas en lugar de una; en 1920×1080 pasa de cuatro a siete filas. Comprobados búsqueda, filtros, pestañas, expansión, paginación, formato propio sin dependientes, carga de Actualizar, teclado y movimiento reducido.

Capturas, medidas antes/después, comandos y reproducción en Visual Studio: [DISTRIBUCION_PROCESOS.md](docs/migracion/DISTRIBUCION_PROCESOS.md).

### Pendiente y límites

Aceptación institucional por el operador. Las capturas usan props sintéticas y no certifican de nuevo la autorización del servidor. No hubo conexiones a bases, cambios de credenciales/Herd, migraciones, push ni despliegue.

## Cabecera y elegibilidad de UR tipo N — entrega anterior 2026-10-05

### Implementado

- Se integra la cabecera ya ajustada: logo original a 180/150 px, separación de 32 px hasta el primer módulo en escritorio y barra blanca compacta. Procesos operativos mantiene título 26/24 px semibold, descripción solicitada de 14 px, separación de 4 px y acciones separadas 10 px. Colaboradores sigue azul y autorizado; Actualizar es de texto, con icono azul, carga y foco visible. En móvil las acciones pasan debajo cuando falta espacio; no se cambia la tipografía ni la organización de módulos.
- `UnitDirectory.IsEligible` y `eligibleArea` excluyen **únicamente tipo N**, normalizando espacios exteriores y mayúsculas para comparar. Se retira la exclusión por tipo 0: esas UR pueden aparecer si cumplen actividad, alcance y demás condiciones. La regla compartida alcanza formatos, directorios, contadores y selectores, también del administrador; los formatos siguen siendo sólo de niveles **2 y 3**.
- Los nodos N permanecen internamente para resolver adscripciones y ramas. El directorio no toma un nodo excluido como agrupador aunque llegue como principal explícito. Se conservan catálogo, formatos históricos y valores originales de tipo, ID y clave; `06000` se muestra como `6000` y ambas representaciones se pueden buscar.
- No hay cambios de esquema, migraciones ni dependencias. Autenticación, permisos, representación, reservas automáticas, autoguardado, SignalR y envío conservan su implementación.

### Probado

TypeScript, Vite y compilación Release de la solución correctos, sin errores ni advertencias de .NET. **55/55** pruebas frontend y **54/54** de dominio/transporte. Incluyen variantes `N`, `n`, espacios, tipo `0` en niveles 2/3, exclusión del nivel 4, ramas con nodos auxiliares y conservación de claves originales.

**117/117** casos de servidor/PostgreSQL y **4/4** recorridos Edge correctos, cero errores JavaScript. La prueba HTTP del administrador excluye `N`, `n` y espacios de formatos/selectores; permite tipo 0 en niveles 2/3; conserva el registro histórico completo antes y después del cambio sintético de tipo. La regresión cubre permisos, reservas por sesión/pestaña, autoguardado, versiones atrasadas, revocación, auditoría, envío concurrente e inmutabilidad. El navegador comprueba además supervisor, colaboradores de nivel 3, claves, jerarquía y móvil con movimiento reducido. Evidencias: [PostgreSQL](docs/migracion/evidencia-ur-tipo-n-postgresql.json), [React](docs/migracion/evidencia-ur-tipo-n-react.json). Corrida `.artifacts/native-postgres-20261006-000019-c8c8e5e8`; el total agregado 118 incluye una entrada para el grupo de navegador, no son 118 pruebas de backend.

Vista del bundle real en Edge con datos sintéticos del administrador en siete anchos: 320, 390, 600, 768, 901, 1024 y 1440 px. Tipo N ausente, tipos 0 de niveles 2/3 visibles y agrupados, contadores y búsqueda `06000`/`6000` correctos. Sin desbordamientos; comprobados separación del logo, alineación de acciones, carga de Actualizar, foco, permisos de Colaboradores, submódulos, menú móvil y movimiento reducido. Capturas revisadas: [escritorio](docs/migracion/ur-tipo-n-escritorio.png), [móvil](docs/migracion/ur-tipo-n-movil.png). [Medidas y comprobaciones visuales](docs/migracion/evidencia-ur-tipo-n-visual.json).

Comandos desde la raíz, con Node/npm y .NET 10 en PATH:

```powershell
npm.cmd --prefix ClientApp run types:check
npm.cmd --prefix ClientApp run test:forms
npm.cmd --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false
$env:TDV2_TEST_BROWSER_FLOW='scope'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El último comando de pruebas crea y detiene un PostgreSQL desechable local, usa dobles de Microsoft/Nexo y no toma la conexión institucional de User Secrets. La corrida terminó con código 0 y el clúster detenido. No ejecutar `database update` para esta corrección.

### Pendiente y límites

La aceptación institucional con cuentas y catálogos reales queda pendiente del operador. No se accedió a bases institucionales, credenciales, Herd ni Ubuntu; no hubo push ni despliegue. La publicación externa de Nexo documentada en entregas anteriores no se modifica: se siguen exigiendo sus capacidades vigentes, y cualquier cambio de esa publicación necesita un trabajo separado autorizado. La prueba visual aislada usa props sintéticas y no sustituye las pruebas del servidor.

Para revisar desde Visual Studio: abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React** y ejecutar F5. En Procesos operativos comprobar la cabecera, Actualizar y la adaptación al estrechar la ventana. Para escribir o alterar tipos, usar exclusivamente el verificador sintético indicado en la evidencia; no modificar catálogos institucionales para probar. El arranque habitual no aplica migraciones.

## Cabecera y encabezado de Procesos operativos — entrega anterior 2026-10-05

### Implementado

Logo original ampliado 20 % (180 px escritorio, 150 px móvil), proporción conservada, barra blanca de 68 px y separación de 32 px hasta el primer módulo en escritorio. Se conserva la jerarquía de módulos/submódulos. Sólo Procesos operativos usa título 26/24 px semibold, descripción solicitada de 14 px en gris secundario y separación de 4 px. Acciones a la derecha y al nivel del título, con 10 px entre ellas; se acomodan debajo de la descripción cuando falta espacio. Colaboradores conserva icono, botón azul y condición de permiso. Actualizar usa texto/icono azul sin contorno, indicador MUI de carga, bloqueo durante la solicitud y foco visible con teclado. Tipografía y cambios locales conservados.

### Probado

TypeScript y Vite correctos; `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false` sin advertencias ni errores. Vista del bundle real en Edge con datos sintéticos, sin ASP.NET ni conexiones a bases/autenticación: siete anchos (320, 390, 600, 768, 901, 1024 y 1440 px), sin desbordamientos. Verificados carga/GET de Actualizar, foco por teclado, ocultación de Colaboradores según su prop, menú de submódulos y navegación móvil con movimiento reducido. Revisión visual de las capturas: [escritorio](docs/migracion/cabecera-inicio-escritorio.png), [móvil](docs/migracion/cabecera-inicio-movil.png). [Medidas y comprobaciones](docs/migracion/evidencia-cabecera-inicio.json).

### Alcance y pendientes

No se cambió lógica de formatos, permisos del servidor, autenticación ni bases. No hubo despliegue ni push. La comprobación visual utiliza props sintéticas; no representa una nueva verificación de autorización institucional. Los pendientes institucionales de las entregas anteriores se conservan.

## Reserva automática al entrar en el registro — entrega anterior 2026-10-05

### Implementado

- Retirado **Editar registro**: entrar directamente en un campo, casilla, criterio o Select solicita la reserva. No hay botones ni confirmaciones para iniciar/terminar edición. Consultar el texto estático de una fila no la reserva.
- Antes de aceptar respuestas se confirma reserva, versión y contenido vigente. Se muestra «Preparando edición…» durante la espera. El Select abre tras confirmar; una intención pendiente no se aplica después de abandonar el registro. Un menú cerrado al perder reserva no se reabre por una adquisición posterior.
- Al salir se guarda y después se libera automáticamente. Cambiar campos o abrir Select/Dialog conserva el bloque. Las otras sesiones reciben por SignalR el valor/estado y pueden entrar directamente sin recarga. También se admite una nueva interacción si el campo conservaba foco después de liberarse o vencer.
- La fila ocupada conserva fondo ámbar suave, candado y nombre una sola vez; otra pestaña de la misma sesión tiene su texto específico. Reserva propia y preparación son indicadores discretos, sin acciones de edición. Se conserva accesibilidad, tipografía y movimiento reducido.
- Se mantienen las correcciones de `FormHub.Watch`, permisos, versiones, auditoría, autoguardado, recuperación contextual explícita ante fallos y formatos enviados inmutables. No se añaden dependencias ni cambios de esquema.
- El verificador aislado usa Release sin apphost para evitar el bloqueo del ejecutable Debug abierto por Visual Studio; no se detuvo ese proceso.

### Probado

- TypeScript y Vite correctos. Compilación de `tdv2.slnx` Release sin advertencias ni errores. **54/54** pruebas frontend y **53/53** dominio/transporte.
- **22/22** casos de captura HTTP/PostgreSQL en base desechable: reservas de usuarios/sesiones/pestañas, bloques distintos, versión vigente, vencimiento/reasignación, solicitudes atrasadas, idempotencia, revocación y carreras de envío. [Evidencia PostgreSQL](docs/migracion/evidencia-reserva-automatica-postgresql.json), corrida `225426-54c0e628`. Su total 23 incluye además una entrada agregada para el navegador; no son 23 casos de backend.
- **21/21** recorridos Edge del frontend compilado, cero errores JavaScript: [evidencia React](docs/migracion/evidencia-reserva-automatica-react.json), corrida `225926-48c179e4`. Dos sesiones autenticadas con cookies independientes compiten al enfocar (una respuesta 200 y otra 409); salir guarda/libera y la otra entra sin recarga. También pasan dos pestañas, bloques/criterios distintos, preparación sin escritura, Select, casillas con teclado, Cancelar y eliminación por ID, desconexión/recuperación, permisos y enviados inmutables. [Captura móvil, movimiento reducido](docs/migracion/reserva-automatica-movil.png).
- SignalR abierto durante tres intervalos de 15 segundos, reserva vencida sin actividad, cancelación/reapertura limpia y revocación por logout. Ambos clústeres de evidencia finalizaron detenidos. El primer intento de iniciar PostgreSQL fue impedido por el token restringido de Windows; se ejecutó fuera del aislamiento y exclusivamente en el clúster desechable. Un intento posterior encontró Debug ocupado por Visual Studio; se corrigió el verificador para usar Release antes de las corridas aprobadas.

Comandos exactos y recorrido con dos sesiones: [RESERVAS_SIGNALR.md](docs/migracion/RESERVAS_SIGNALR.md). Las evidencias de la entrega anterior se conservan por separado.

### Pendiente y límites

La aceptación manual con F5, identidades institucionales y proxy real corresponde al operador en un entorno de pruebas. Las pruebas locales usan ASP.NET, React/MUI, Edge y PostgreSQL reales con Nexo/Microsoft sintéticos. No se modificaron bases institucionales, credenciales, Herd ni Ubuntu; sin push, despliegue ni migración. Una propuesta sin confirmar vive en la pestaña; se mantienen el aviso de salida, la recuperación explícita y la descarga de emergencia.

## Exclusión por registro y SignalR — entrega anterior 2026-10-05

### Implementado

- `Watch` usa un temporizador periódico de 15 segundos, sin cancelar la lectura por intervalo. Cancelar el stream/desconectar termina limpiamente; las cancelaciones de consultas se filtran por sus tokens. Se mantienen reautorización, revocación, contexto y limpieza del iterador. No se alteró el depurador.
- La adquisición PostgreSQL devuelve contenido y versión confirmados. React no acepta modificaciones antes de reservar, ni durante liberación, desconexión, vencimiento o pérdida de titular. Altas/cascadas reservan los bloques necesarios; preguntas y criterios mantienen su granularidad.
- Un indicador por registro distingue reserva propia, otra persona y otra pestaña de la misma sesión. Consulta/copia disponibles, eliminación bloqueada si no hay reserva propia. Fondo ámbar, candado y texto legible; transición de 180 ms con movimiento reducido.
- Autoguardado y Guardar borrador conservados. Guardar precede a liberar; Select/Dialog no liberan por cambiar foco dentro del registro. El foco durante la carga inicial se retoma después de verificar acceso. Reintentos conservan UUID/cuerpo; recibos antiguos no sustituyen versiones compartidas recientes.
- Se sustituye el panel global habitual por mensaje y **Resolver** junto al registro afectado. Comparación contextual, confirmación explícita, conservación de la respuesta compartida y descarga de emergencia. Una reconexión no recupera propuestas automáticamente.
- Sin nuevas dependencias ni migración; se preservaron cambios locales, estructura, permisos, tipografía, OAuth y bloqueo de enviados. [Diseño y recorrido en Visual Studio con dos sesiones](docs/migracion/RESERVAS_SIGNALR.md).

### Probado

- TypeScript, Vite y compilación Release correctos; backend sin advertencias. **52/52** frontend y **53/53** dominio/transporte. EF: sin cambios pendientes del modelo.
- **117/117** casos HTTP/PostgreSQL: [evidencia](docs/migracion/evidencia-reservas-postgresql.json). Incluye 22 de captura/envío, adquisición con contenido vigente, competencia simultánea entre dos usuarios, sesiones/pestañas, escrituras en bloques distintos, versiones, vencimiento/reasignación, auditoría e inmutabilidad. También valida permisos, representación y sincronizaciones. Los 22 casos dirigidos están incluidos en los 117, no se suman otra vez.
- **47/47** recorridos Edge: [19 formatos](docs/migracion/evidencia-reservas-react-formatos.json), [13 acceso](docs/migracion/evidencia-reservas-react-acceso.json), [4 alcance](docs/migracion/evidencia-reservas-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-reservas-react-sincronizaciones.json). Cero errores JS. Incluye reserva pendiente sin aceptar texto, foco mantenido, criterios independientes, Select, Cancelar, eliminación por ID, desconexión/reconexión con propuesta conservada, envío inmutable y revocación del stream por logout.
- SignalR permaneció abierto al menos 45 segundos, con cuatro entregas durante tres intervalos de 15 segundos. La reserva venció sin actividad; cancelar/reabrir el stream terminó sin error y sin falso conflicto. [Captura móvil con reserva ajena y movimiento reducido](docs/migracion/reserva-otra-pestana-movil.png).

Comandos exactos desde la raíz: [guía de verificación](docs/migracion/RESERVAS_SIGNALR.md#verificación-desde-la-raíz). Regresión: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1`, corrida `220508-35bbd093`. Sus 117 casos de backend pasaron; el grupo navegador detectó una importación faltante de `expect` en la prueba de sincronizaciones. La evidencia PostgreSQL conserva explícitamente ese resultado original. Se corrigió la prueba y se ejecutó nuevamente **todo** el grupo con `-BrowserOnly`: corrida `221547-584400be`, 47 recorridos aprobados. No se presenta la corrida inicial como aprobación del navegador. Se corrigieron además el foco durante carga y la visualización de recibos antiguos, con pruebas específicas. Todos los clústeres de prueba terminaron detenidos.

### Pendiente y límites

Aceptación interactiva con F5 y sistemas institucionales reales. Las pruebas usan identidades, concesiones, respuestas y envíos sintéticos; no se tocaron bases institucionales, Herd, credenciales ni Ubuntu. No se hizo push ni despliegue. No se creó/aplicó una migración. Los borradores sin confirmar permanecen en la pestaña y pueden descargarse; un cierre forzado no implica persistencia local automática. La guía anterior separa la reproducción sintética desde Visual Studio de la aceptación institucional.

## Ajustes de captura y navegación — entrega anterior 2026-10-05

### Implementado

- Se retiraron Datos de la sesión e Imprimir, conservando Contexto, identificación de UR, Guardar borrador, autoguardado y descarga/recuperación de propuestas. El JSON histórico de encabezado no se normaliza ni se exige para enviar.
- El servidor recalcula la proyección de avance de borradores con los 90 puntos restantes normalizados a 100, sin escribir durante GET ni inventar respuestas. Guardar/envío persisten el cálculo. Los enviados conservan avance, contenido e instantánea, incluso ante nuevas sincronizaciones.
- Prioridad usa Select MUI con placeholder y escala vigente 1–5. Los valores históricos se conservan hasta una reclasificación explícita. El menú en portal mantiene el bloque de edición; solicitudes de foco canceladas no adquieren reservas tardías y salir espera adquisiciones en curso antes de liberar. Guardar borrador sigue recibiendo el clic aunque el blur inicie autoguardado; el editor serializa las operaciones.
- Eliminación en las cuatro tablas mediante diálogo MUI, Cancelar enfocado, icono rojo disponible y tooltip para bloqueo real. La confirmación usa ID estable y comprueba la fila actual; eliminar proceso incluye evaluación y desvinculación de sistemas/datos. Se mantienen todas las reservas y restricciones de ILDA.
- Navegación superior agrupa sólo la jerarquía autorizada por Nexo: Procesos operativos y Configuración con submódulos. Selección activa, teclado, móvil y movimiento reducido; acciones de cuenta y reglas OAuth/representación sin cambios.
- Revisión y envío está únicamente al final de Acuerdos. `FormReview` comparte pendientes concretos entre interfaz y requisitos del servidor. Enviar exige respuestas guardadas y una revisión válida, además de permisos, versión, reservas y auditoría ya existentes. Confirmación MUI con UR y bloqueo definitivo; consulta muestra fecha e identidad efectiva. Sin reapertura.
- No se añadieron librerías ni migraciones. Los cambios reutilizan ASP.NET/EF/React y se separan en componentes pequeños.
- Guardar borrador indica actividad hasta terminar la liberación; una eliminación confirma y libera toda su cascada. Una elección explícita de Contexto durante la carga prevalece sobre la última sección recuperada del servidor.

### Probado

| Comando desde la raíz | Resultado y alcance |
|---|---|
| `dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false` | Correcto, cero errores y advertencias |
| `dotnet run --project tests/TDV2.Verification -c Release --no-build --no-restore -p:UseAppHost=false` | 53/53 dominio/transporte; encabezado histórico conservado y formato completo sin datos de sesión |
| `npm.cmd --prefix ClientApp run types:check`, `run test:forms` y `run build` | TypeScript y Vite correctos; 41/41 frontend: concurrencia, respuestas atrasadas, foco/reservas, recuperación, eliminación por ID y jerarquía |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser` | [115/115 regresión PostgreSQL](docs/migracion/evidencia-ajustes-regresion.json). Posteriormente se agregó el caso de enviado histórico de la fila siguiente |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser` | [21/21 captura y envío](docs/migracion/evidencia-ajustes-edicion.json): incluye proyección sin escribir, 100 % con prioridad histórica aún pendiente, enviados históricos inmutables, cascadas incompletas/reserva ajena/ILDA rechazadas, sesiones independientes y carreras |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Sin cambios de modelo; no requiere conexión institucional |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly` (recorridos dirigidos indicados debajo) | **43/43**: [15 formatos](docs/migracion/evidencia-ajustes-react-formatos.json), [13 acceso](docs/migracion/evidencia-ajustes-react-acceso.json), [4 alcance](docs/migracion/evidencia-ajustes-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-ajustes-react-sincronizaciones.json). Escritorio/móvil, movimiento reducido, teclado, Cancelar en cuatro tablas, borrado concurrente por ID, pendientes, confirmación/envío y bloqueo posterior. Permisos, representación, revocaciones e ILDA conservados; cero errores JS |

Las escrituras se hicieron sólo en clústeres desechables con datos sintéticos. No se cuentan intentos fallidos como aprobación. Los 21 casos de edición incluyen casos ya presentes en la regresión, no se suman como pruebas independientes adicionales.

Capturas sintéticas revisadas: [prioridad móvil](docs/migracion/ajustes-prioridad-movil.png), [enviado en consulta](docs/migracion/ajustes-formato-enviado.png), menú en [escritorio](docs/migracion/ajustes-modulos-escritorio.png) y [móvil](docs/migracion/ajustes-modulos-movil.png). Los intentos previos detectaron carreras de foco/guardado y liberación, y una elección de Contexto ignorada durante carga. Se corrigieron antes de las corridas aprobadas. Las pruebas esperan carga y finalización de solicitudes, no un `networkidle` que ya había ocurrido; la prueba de requisitos de envío vence reservas de casos anteriores para aislar esa validación. Se corrigieron también expectativas de menú para respetar los módulos realmente publicados por cada fixture de Nexo, sin ampliar sus permisos.

Corridas aprobadas bajo `.artifacts/native-postgres-20261005-*`: backend `195115-6ef85165`; edición `201428-c72e92e8`; formatos `205514-516f6638`; acceso `205842-7072f304`; alcance `210050-d091fed9`; sincronizaciones `210320-d7aab87d`. Todos los clústeres terminaron detenidos. En la corrida de formatos pasó ese recorrido y falló la expectativa inicial del menú de acceso; acceso se corrigió y verificó por separado. No se presenta esa corrida como aprobación del grupo completo. Para reproducir cada recorrido: `$env:TDV2_TEST_BROWSER_FLOW='formats'` (o `access`, `scope`, `sync`), ejecutar `-BrowserOnly` y después `Remove-Item Env:TDV2_TEST_BROWSER_FLOW`. Sin esa variable se ejecutan los cuatro.

### Pendiente y límites de esta entrega

La aceptación con Nexo/Microsoft y usuarios institucionales reales sigue pendiente. No se iniciaron sesiones institucionales, no se modificaron Herd, secretos ni bases institucionales, y no hubo push, despliegue ni acceso a Ubuntu. No se aplicaron migraciones remotas. La tercera migración de la entrega anterior y la configuración institucional documentada siguen siendo pasos del operador; estos ajustes no añaden requisitos de esquema.

Abrir `tdv2.slnx` → **TDV2 HTTPS + React** → F5. El perfil y `/connect` no cambiaron; F5 no migra. [Recorrido y comandos de prueba](docs/migracion/COLABORACION_ENVIO.md#comprobar-los-ajustes-en-visual-studio). La evidencia de F5 de la entrega anterior es histórica; no se presenta como una nueva validación interactiva de este cambio.

## Captura colaborativa, supervisión y envío — entrega anterior 2026-10-05

### Implementado

- `responsable_ur_supervisor` concentra responsabilidad y consulta institucional sin administración. Nivel 2 edita su rama autorizada; nivel 3 su formato. `responsable_ur_institucional` continúa como alias transitorio. El catálogo compartido de Nexo no se renombró; se conservan roles y asignaciones de consulta. La representación mantiene separados actor real y efectivo.
- La delegación valida encargado, acceso vigente, rama y rol en TDV2 y Nexo. Nivel 3 sólo agrega colaboradores locales de su UR/subordinadas a su mismo formato; nivel 2 conserva ambos tipos. Se ampliaron los filtros TDV2 de las dos fuentes Nexo ya corregidas, preservando cambios locales y las reglas de otras aplicaciones. [Configuración y transición exactas](docs/migracion/CONFIGURACION_NEXO_ENTRA.md).
- PostgreSQL arbitra reservas de 45 segundos por fila/grupo de campos, sesión/pestaña/contexto y versión. Autoguardado parcial, UUID y recibos idempotentes, renovación por cambios, liberación tras confirmar, recuperación explícita de propuestas y auditoría atómica. No hay transacciones abiertas durante la captura. El guardado completo anterior se rechaza con 428 y se retiró su implementación.
- SignalR sólo entrega contenido autorizado, revalida Nexo/ticket/contexto y recupera estado al reconectar. Origin se compara con la configuración pública; se admite WebSocket HTTP/2 CONNECT únicamente en este canal de consulta. Las mutaciones y negociación HTTP siguen exigiendo CSRF. No se renuevan reservas mediante latidos de pantalla.
- Enviar guarda pendientes, confirma, exige responsable/avance/requisitos/ausencia de reservas ajenas y se serializa con reservas/guardados. Conserva ejercicio, fecha UTC, versión, identidad real/efectiva, auditoría e instantánea de unidad/respuestas/ILDA. Trigger de inmutabilidad para todos, incluidos administradores y sincronizadores. Sin reapertura.
- React conserva MUI/Roboto e identidad visual: Contexto incorpora los campos de Encabezado, siete secciones montadas, última sección confirmada, navegación superior/móvil, prioridad 1–5 accesible sin defecto y estado compacto sin barra fija. No hay conversión automática de prioridades históricas. Se conservan jerarquías/históricos tipo 0 e identificadores; las claves se formatean sólo al mostrar/buscar.
- Microsoft valida el ID token antes de usar el claim opcional login_hint. Inicio habitual sin selección forzada, hints de cuenta real y Usar otra cuenta con revocación/CSRF. logout_hint nunca se deriva del correo ni de la representación.
- EF: `20261005135417_CollaborativeFormsAndSubmission`, seis columnas y tres tablas adicionales; 17 entidades y seis relaciones. Backfill de ejercicio sin alterar respuestas. Down rechaza retirar la protección si existen envíos. Migración explícita, nunca F5. [Diseño, instalación y comandos](docs/migracion/COLABORACION_ENVIO.md).

### Probado

| Comando desde la raíz | Evidencia y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification -c Release --no-restore -p:UseAppHost=false` | 53/53 dominio y transporte. Las pruebas del guardado completo retirado se sustituyeron por pruebas de reservas/parches con PostgreSQL real |
| `npm.cmd --prefix ClientApp run types:check` y `npm.cmd --prefix ClientApp run test:forms` | TypeScript correcto; 33/33 frontend: respuestas atrasadas, reintentos, propuesta pendiente, recuperación sin autoguardado, renovación atrasada y selección de sección durante carga |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly` | [29/29 PostgreSQL real](docs/migracion/evidencia-colaboracion-migraciones.json): base vacía y actualización con contenido, repetición, script idempotente, restricciones, rollback y rechazo de Down tras envío. [Huella de esquema nuevo](docs/migracion/evidencia-colaboracion-esquema.json) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -SkipBrowser` | [112/112 backend/PostgreSQL](docs/migracion/evidencia-colaboracion-regresion.json), con fuentes externas sintéticas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly -SkipBrowser` | [17/17 casos de captura](docs/migracion/evidencia-colaboracion-edicion.json): sesiones independientes, dos pestañas, mismo/distinto bloque, vencimiento/reasignación, caducidad antes del commit, revocación, concurrencia con envío, colaborador sin envío, inmutabilidad, prioridad y OIDC firmado |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -BrowserOnly` | 40 recorridos Edge: [13 formatos](docs/migracion/evidencia-colaboracion-react-formatos.json), [12 acceso](docs/migracion/evidencia-colaboracion-react-acceso.json), [4 alcance](docs/migracion/evidencia-colaboracion-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-colaboracion-react-sincronizaciones.json). Cero errores JS; escritorio, móvil, teclado y movimiento reducido |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-supervision` | [7/7 con SQL generado por las fuentes reales](docs/migracion/evidencia-supervision-nexo.json), exclusivamente datos y PostgreSQL sintéticos. [Parche adicional](docs/migracion/nexo-supervision.patch) y [hashes coincidentes con las fuentes aplicadas](docs/migracion/evidencia-supervision-nexo-fuentes.json) |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Modelo y snapshot coinciden, sin conexión ni secretos. Script incremental generado en `.artifacts/colaboracion-envio.sql` |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` y `node ClientApp/tests/browser/publish-flow.mjs` | [Paquete final Production/HTTPS](docs/migracion/evidencia-colaboracion-publicacion.json), React compilado sin Vite, sin credenciales, rechazos seguros y cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [tdv2.slnx / TDV2 HTTPS + React](docs/migracion/evidencia-colaboracion-visual-studio.json): Vite 200, lanzamiento 302, React/props 200, privado 401, HTTPS confiable y HMR wss 7136. Sólo páginas anónimas |

Se revisaron capturas de [prioridad móvil](docs/migracion/colaboracion-prioridad-movil.png), [formato enviado](docs/migracion/colaboracion-formato-enviado.png) y [Sincronizaciones móvil](docs/migracion/colaboracion-sincronizaciones-movil.png). Las fuentes externas se bloquean en navegador sintético; la configuración tipográfica original se conserva.

La prueba final de Imprimir generó un [PDF sintético del formato enviado](docs/migracion/colaboracion-formato-enviado.pdf), verificando las siete secciones. Corridas finales: backend `.artifacts/native-postgres-20261005-154331-a579677d`; migraciones `154018-02f01596`; edición `154049-d16e5e8a`; formatos/impresión `154847-ac17674c`; acceso/alcance `152920-19ce7115`; sincronizaciones `153805-e2794a9b`. Los clústeres desechables terminaron detenidos. [Compilación y depuradores de Visual Studio](docs/migracion/evidencia-colaboracion-visual-studio.txt) confirmados; no se autenticaron personas institucionales.

Los intentos fallidos no se cuentan como aprobación. Permitieron corregir CONNECT/WebSocket, el clic Enviar durante blur/autoguardado y una respuesta inicial que reemplazaba la sección seleccionada. Se actualizaron fixtures que aún usaban la clave anterior. El Vite antiguo bloqueó Rolldown durante el primer publish y se detuvo sólo ese proceso de TDV2. El arranque de navegador afectado por `npm ci` se repitió después de terminar la publicación. La migración inicial y las actualizaciones se operan con un único ejecutor: el ensayo concurrente sólo acredita una migración pendiente con historial establecido, no autoriza varios operadores sobre una cadena de migraciones.

Para repetir un recorrido aislado, establecer `$env:TDV2_TEST_BROWSER_FLOW='formats'` o `'sync'` antes de `-BrowserOnly`, y después `Remove-Item Env:TDV2_TEST_BROWSER_FLOW`; los valores admitidos son all/formats/access/scope/sync. Las repeticiones dirigidas figuran junto con los archivos de evidencia, sin sumar varias veces un mismo caso.

### Pendiente institucional

Aplicar la tercera migración con un operador al destino previamente verificado; se conserva la base existente y las dos migraciones iniciales ya aplicadas. Registrar/asociar el rol canónico, revisar asignaciones/catálogos compartidos y publicar la delegación en la conexión Nexo existente. Agregar el claim opcional login_hint al ID token en Entra y probar SSO/salida con las políticas reales. Validar proxy WebSocket, usuarios institucionales, fuentes SII/ILDA y despliegue Ubuntu. No se enviaron formatos institucionales, no se concedieron accesos reales ni se modificaron User Secrets; sincronización automática sigue desactivada. Instrucciones en [Nexo/Entra](docs/migracion/CONFIGURACION_NEXO_ENTRA.md) y [captura/migración](docs/migracion/COLABORACION_ENVIO.md).

## Responsables y consulta institucional — entrega anterior 2026-10-05

### Implementado

- Nivel 3: botón Colaboradores, búsqueda y alta local en su misma UR o descendientes, siempre para el formato del responsable nivel 3. Backend y funciones Nexo rechazan rama ajena y roles de alcance global/dependencias; nivel 2 conserva sus facultades. Vínculo local, concesión central, empleado textual, revocación y auditoría siguen siendo obligatorios.
- Rol `responsable_ur_institucional`: funciona solo como responsable y añade lectura institucional. La responsabilidad sigue siendo institucional; no concede Configuración, Sincronizaciones ni Pruebas de acceso. Representación y vista de prueba conservan sus restricciones.
- EF lee el campo ya existente `tipo_ur`; tipo 0 no es área elegible ni formato, aunque permanece como nodo auxiliar y con su historia. El directorio público conserva ID/código/padre originales y añade `id_ur_principal` para agrupar nivel 2/3 a través de auxiliares. No se añadieron migraciones ni se modificaron las existentes.
- React: claves numéricas sin ceros sólo para presentación/búsqueda, Mis áreas/Todas las áreas, etiquetas Solo consulta, encabezado compacto/adaptable de Sincronizaciones, transiciones breves y movimiento reducido. Actualizar estado no sustituye el borrador ante un error temporal; un rechazo de acceso sigue retirando la vista privada.
- Fuente de Nexo corregida en cuatro archivos, sólo para clave `tdv2`; las demás aplicaciones conservan su comportamiento. Cambios locales previos preservados y hashes comprobados antes de escribir fuera del workspace. [Parche](docs/migracion/nexo-tdv2-delegacion.patch) y [configuración exacta](docs/migracion/RESPONSABLES_UR.md).

### Probado

| Comando desde la raíz | Resultado y alcance |
|---|---|
| `dotnet run --project tests/TDV2.Verification --no-restore -p:UseAppHost=false` | 58/58 dominio y transporte; dobles en memoria donde corresponde |
| `npm --prefix ClientApp run types:check` y `npm --prefix ClientApp run test:forms` | TypeScript correcto y 24/24 frontend: árbol, tipo 0, búsquedas 06000/6000, autosave, conservación del borrador y revocación |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1` | [95/95 HTTP/PostgreSQL + grupo navegador, 96/96 grupos](docs/migracion/evidencia-ur-postgresql.json). Incluye conservación del padre original, historia tipo 0, ILDA sin mezclar claves, alta/retiro sintéticos, revocaciones, concurrencia y atomicidad |
| Edge de la suite anterior | [9 formatos](docs/migracion/evidencia-ur-react-formatos.json), [12 acceso](docs/migracion/evidencia-ur-react-acceso.json), [4 alcance/UR](docs/migracion/evidencia-ur-react-alcance.json), [11 sincronizaciones](docs/migracion/evidencia-ur-react-sincronizaciones.json): 36 recorridos, sin errores JS. Escritorio 1440×1000, móvil 390×844 y movimiento reducido; fuentes externas bloqueadas |
| `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly -NexoSource .artifacts/nexo-tdv2-scope` | [7/7 con funciones y vistas generadas por el código real de Nexo](docs/migracion/evidencia-ur-nexo.json), datos/base sintéticos. Las cuatro fuentes aplicadas coinciden byte a byte con las probadas: [hashes](docs/migracion/evidencia-ur-nexo-fuentes.json). PHP `-n -l` correcto; no se ejecutó la suite Laravel ni su bootstrap |
| `dotnet ef migrations has-pending-model-changes --project tdv2 --configuration Release --no-build -- --environment Production` | Sin cambios pendientes de modelo; sin conexión a base ni secretos |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Correcto, incluye npm ci/build; auditoría npm: 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production, HTTPS confiable, React compilado, sin Vite ni secretos](docs/migracion/evidencia-ur-publicacion.json); rechazos 503/401/419 esperados, cero errores JS |
| F5 real y `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, lanzamiento 302 a HTTPS, React/props 200, privado 401 y HMR wss 7136](docs/migracion/evidencia-ur-visual-studio.json), cero errores JS. No autentica personas institucionales |
| Parche y whitespace | `git -c core.autocrlf=false apply --check --directory=.artifacts/nexo-tdv2-scope/before docs/migracion/nexo-tdv2-delegacion.patch` y `git -c core.autocrlf=false -c core.whitespace=cr-at-eol diff --check`: correctos |

Se revisaron las capturas de [Sincronizaciones escritorio](docs/migracion/ur-sincronizaciones-escritorio.png), [móvil](docs/migracion/ur-sincronizaciones-movil.png) y [consulta institucional móvil](docs/migracion/ur-consulta-institucional-movil.png). Los estilos/tipografías no se sustituyeron; la prueba aislada bloquea descargas externas de fuentes.

Regresión final: `.artifacts/native-postgres-20261005-121554-1445277a`; Nexo: `.artifacts/native-postgres-20261005-115608-3fa5659f`. Clústeres de prueba detenidos. El IDE conserva los documentos y el perfil HTTPS + React quedó en ejecución tras comprobar F5.

Intentos corregidos, no contados como evidencia: dos expectativas iniciales usaban una redirección como JSON y 403 en lugar del 409 existente para representación de consulta. Se corrigieron y repitió toda la regresión. La revisión final añadió padre original e historia tipo 0 y se repitió la suite con ese código. El primer publish encontró un Vite previo bloqueando Rolldown; se detuvo sólo el proceso cuya ruta correspondía a TDV2 y se publicó correctamente. El sandbox no accedía al certificado HTTPS del usuario; la verificación se repitió fuera del sandbox manteniendo validación de certificado. El parche de Nexo se regeneró conservando los finales de línea originales y pasó la comprobación sin modificar la copia previa.

### Pendiente institucional

Registrar/asociar `responsable_ur_institucional` a `procesos_operativos` (`/inicio`), configurar los roles delegantes/asignables y publicar las funciones actualizadas de la conexión existente de Nexo. Instrucciones en [RESPONSABLES_UR.md](docs/migracion/RESPONSABLES_UR.md). No se ejecutó Artisan/Composer en Herd, no se leyó .env, no hubo altas/retiros reales ni conexiones a bases institucionales, migraciones remotas o sincronizaciones. Las pruebas de concesiones/retiros y programación fueron exclusivamente sintéticas. La sincronización automática institucional permanece sin cambios.

## Proyecto convencional y migraciones EF Core — vigente

### Implementado

- Se reutilizan Controllers para HTTP, Domain para reglas/entidades, Services para casos de uso e Infrastructure para persistencia. Se añadió `Tdv2DbContext`, 14 entidades en Domain/Entities y configuraciones `IEntityTypeConfiguration`. No se añadieron capas ni proyectos de aplicación duplicados. Program.cs sólo compone el host y permite los procesadores explícitos ya existentes.
- Integrations/Microsoft y Integrations/Nexo contienen los clientes externos; Integrations/Catalogs conserva el lector SII/ILDA. La publicación, cola y reservas siguen en Synchronization. EF sólo administra public de TDV2: no modela ni migra esquemas externos, roles locales ni tablas de plataforma Laravel sin uso.
- EF Core/Relational/Design y dotnet-ef **10.0.12**, proveedor Npgsql EF **10.0.0**, Npgsql directo **10.0.0**. Manifiesto local de herramientas en .config; paquetes/artefactos dentro del destino. Npgsql directo se conserva por su uso comprobado en bloqueos, tokens, operaciones centrales, reservas y auditoría.
- EF participa en consultas de áreas, formatos, colaboraciones, identidad de sesión e inventario local, además del guardado de formatos. Se mantienen empleado textual, tipos/longitudes/defaults/nulabilidad, JSON completo, secuencias serial/bigserial y relaciones locales. No se inventó FK desde el user_id integer histórico de tokens hacia users.id bigint ni hacia identidades/jerarquías externas.
- El guardado usa una transacción EF compartida con el bloqueo SQL de UR, revisión del contexto y auditoría. Version es token de concurrencia; se conserva el 409 y el cálculo de avance del servidor. Los SQL especializados de publicación y reservas mantienen propietario/caducidad antes del commit y latido sin renovación.
- Autorización mediante política ASP.NET, AuthorizationHandler y `IAuthorizationMiddlewareResultHandler`: revalida Nexo antes de HTML/JSON y conserva denegaciones, mensajes y auditoría. Se usa el mismo formateador de DomainProblem para middleware y política. Continúan el binding/ModelState MVC, filtros, DI, Options, cookie de sesión, antiforgery y rate limiting.
- Migraciones estándar `20261002051005_InitialTdv2` y `20261002051340_InitializePausedSynchronization`, con designers y snapshot. Historial único vigente: `public."__EFMigrationsHistory"`. La primera exige base vacía; la segunda sólo inserta configuración pausada. El modelo conserva todas las restricciones semánticas anteriores: dos unicidades nullable pasan de constraints UNIQUE a índices UNIQUE y se añade el índice de la FK de ejecución activa.
- Se retiró el ejecutor `--migrate`, sus recursos SQL compilados y el historial SQL como mecanismo vigente. SQL 001–004 queda exclusivamente en tests/TDV2.NativeVerification/LegacySchema; el DDL de conversión anterior, en TransitionSchema como fixture. El único archivo database/transition vigente es el diagnóstico de lectura.
- Local-Tdv2 conserva DPAPI administrativo, marcadores y Verify-Cluster, e invoca dotnet ef sólo para su destino aislado. Si encuentra la base local anterior sin historial EF, la conserva y exige revisión. Prepare-Transition mantiene restauración en una copia nueva y diagnóstico READ ONLY; `-Apply` SQL está retirado. La futura adopción de una copia real requiere revisión EF, no bootstrap automático.
- Se conservan tdv2.slnx, TDV2 HTTPS + React, HMR, UserSecretsId, configuración estándar, https://localhost:7136/connect y todas las pantallas/componentes/tema/tipografías existentes. No se editaron credenciales ni se leyó Herd en esta adaptación. README contiene comandos desde la raíz y equivalencias con Laravel.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore` | 0 errores y 0 advertencias |
| `dotnet ef migrations has-pending-model-changes --no-build --project tdv2` | Sin cambios de modelo pendientes; migrations list descubre las dos migraciones sin conectar |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [25/25 en PostgreSQL desechable](docs/migracion/evidencia-ef-migraciones.json): equivalencia SQL, tipos/defaults/secuencias, relaciones/índices, cuenta sin superusuario, repetición, contenido previo conservado, rollback/reanudación/Down/Up, migraciones pendientes concurrentes, script idempotente y concurrencia EF |
| `dotnet run --project tests/TDV2.Verification --no-build --no-restore` | 56/56 dominio/transporte; incluye dobles en memoria, no equivale a PostgreSQL |
| `Test-NativePostgres.ps1` final tras ajustar el resultado de autorización | [90/90 HTTP/PostgreSQL + un grupo navegador](docs/migracion/evidencia-ef-postgresql.json), 91 grupos correctos; fuentes institucionales sintéticas |
| React/Edge en la suite anterior | [9 recorridos formatos](docs/migracion/evidencia-ef-react-formatos.json) + [12 acceso](docs/migracion/evidencia-ef-react-acceso.json) + [10 sincronizaciones](docs/migracion/evidencia-ef-react-sincronizaciones.json), cero errores JS |
| TypeScript, `test:forms` y `build` de ClientApp | Correctos; 21/21 pruebas frontend, sin cambios de pantallas ni lockfile |
| F5 real desde EnvDTE en Visual Studio | [Compilación 2 correctos, implementación 1 correcta y depuradores .NET/JavaScript](docs/migracion/evidencia-ef-visual-studio.txt); solución/perfil conservados |
| `node ClientApp/tests/browser/visual-studio-startup.mjs` | [Vite 200, redirección 302, HTTPS confiable, React/props, privado 401 y HMR wss en 7136](docs/migracion/evidencia-ef-visual-studio-react.json); sin OAuth ni consultas institucionales |
| `dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance` | Paquete generado con npm ci/build; auditoría npm informó 0 vulnerabilidades |
| `node ClientApp/tests/browser/publish-flow.mjs` | [Production/HTTPS Windows, React compilado sin Vite, 503 sin credenciales, 401 privado, 419 sin CSRF, cero errores JS](docs/migracion/evidencia-ef-publicacion.json) |
| Verificador de configuración `--production` | Production no carga User Secrets; no se imprimieron valores ni se abrieron fuentes |
| Scripts PowerShell y `git diff --check` | Sintaxis y whitespace correctos. No se ejecutó Prepare contra la base local histórica |

Los intentos fallidos no cuentan como resultados: el sandbox impidió iniciar PostgreSQL/restaurar algunos paquetes y acceder a User Secrets, por lo que se ejecutaron las operaciones necesarias con aprobación. Se ajustó el verificador a la excepción PostgreSQL envuelta por EF. Dos bootstrap simultáneos sin historial provocaron una carrera del proveedor: **la primera aplicación tiene un solo operador**; las pendientes con historial se serializan con el bloqueo estándar EF/Npgsql. Un ensayo concurrente intentó recompilar una DLL en uso y se repitió secuencialmente. F5 detectó una pausa de Just My Code al propagar una denegación por el framework; se corrigió usando el resultado estándar de autorización y se repitieron F5, 56 pruebas y toda la regresión PostgreSQL/React. El primer publish encontró Rolldown bloqueado por el Vite remanente del IDE; se identificó y detuvo sólo ese proceso TDV2 antes de publicar correctamente.

Clúster final de migraciones: `.artifacts/native-postgres-20261002-053757-b02b1e9f`. Regresión final: `.artifacts/native-postgres-20261002-055330-864db610`. Todos los clústeres de esta ejecución quedaron detenidos. Se detuvo la depuración iniciada para verificar F5; el IDE y los documentos del usuario se conservaron.

### Aplicado en la base definitiva autorizada

- [Preflight inmediatamente anterior](docs/migracion/evidencia-ef-servidor-preflight.json): `SERVIDOR_TDV2:5432/tdv2_db`, `USUARIO_TDV2`, TLS confirmado, Search Path public, CONNECT/USAGE/CREATE y propietario verificados. Base vacía, sin objetos ajenos ni historial. La inicialización SQL anterior ya había sido eliminada por la recreación del usuario; no se adoptó contenido ni se eliminó ninguna base/conexión.
- Comando aplicado: `dotnet ef database update --no-build --project tdv2 -- --environment Development`, con ConnectionStrings:Tdv2 de User Secrets. [Resultado](docs/migracion/evidencia-ef-servidor-aplicacion.txt) y [verificación](docs/migracion/evidencia-ef-servidor-verificacion.json): ambas migraciones registradas, 14 tablas operativas más historial EF, 0 pendientes.
- Se repitió **el mismo comando**: [salida](docs/migracion/evidencia-ef-servidor-repeticion.txt) y [metadatos posteriores](docs/migracion/evidencia-ef-servidor-repeticion.json). Sin cambios de esquema, historial ni contadores. La huella de columnas/defaults/restricciones/índices/secuencias coincide también con [el ensayo sintético](docs/migracion/evidencia-ef-esquema.json).
- Todas las tablas operativas tienen 0 filas salvo sincronizacion_configuracion, que tiene la fila 1 pausada; automática/ILDA desactivadas, próxima ejecución y reservas nulas. Sin usuarios, sesiones, intentos OAuth, catálogos, formatos, auditoría ni trabajos sintéticos remotos. No se abrió Nexo/SII/ILDA ni se inició procesador.

### Pendiente institucional y operativo

Validar Microsoft/Graph/Entra reales, contratos y revocaciones Nexo, conectividad/tipos/volumen SII e ILDA; revisar una eventual copia autorizada con datos históricos y preparar su adopción EF; aceptación visual con fuentes externas, teclado/móvil/impresión; despliegue Ubuntu con proxy, supervisor, llaves persistentes y recuperación. Las pruebas Windows y los proveedores sintéticos no acreditan esos puntos. No se habilitaron sincronizaciones automáticas ni se desplegó la aplicación.

**Los apartados siguientes son evidencia histórica. Los comandos y mecanismos vigentes son los del bloque EF y del README; no reutilizar los ejecutores SQL/DPAPI retirados.**

## Inicialización SQL anterior del servidor — 2026-10-01 (histórica)

### Implementado

- Comando `--migrate=check|apply` en `tdv2/Migrations`, separado antes de componer el host web. Carga los proveedores normales y User Secrets en Development; sólo utiliza `ConnectionStrings:Tdv2`. Exige host/puerto/base/usuario explícitos y los contrasta con la conexión y con PostgreSQL; comprueba TLS remoto, search_path, escritura, CONNECT/USAGE/CREATE y propietario de tablas administradas. No inicia procesos de sincronización ni servicios institucionales.
- Reutiliza los bytes originales de 001–004 como recursos compilados; sólo SQL de la raíz, sin transition. SHA-256 íntegro, secuencia sin huecos, historial como prefijo verificado antes de escribir, exclusión entre sesiones y transacción por archivo incluyendo `public.tdv2_schema_migrations` (nombre/hash/fecha/usuario). Rechaza objetos preexistentes sin historial, cambios de hash y sincronización activa/reservada. El sitio no ejecuta DDL al arrancar.
- `Local-Tdv2.ps1` queda intacto, con Verify-Cluster, DPAPI, marcadores y tdv2_local_migrations. User Secrets no se editó ni se reemplazó: el usuario lo actualizó para la base recreada antes de esta tarea. Interfaz, F5 y Herd se conservaron. README contiene el comando exacto y el procedimiento para nuevas migraciones.

### Probado y ejecutado

| Verificación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | Compilación sin errores ni advertencias tras corregir la prueba nueva |
| `Test-NativePostgres.ps1 -MigrationsOnly` | [19/19 en PostgreSQL desechable](docs/migracion/evidencia-migrador-postgresql-sintetico.json): cuenta sin superusuario, check sin DDL, primera aplicación, repetición/fechas, destino erróneo, base ocupada, hash cambiado, permisos, dos conexiones, rollback inicial/posterior/al registrar hash, reanudación, automática, COMMIT interno y errores sin secretos |
| Preflight del servidor, sólo metadatos | [Destino exacto SERVIDOR_TDV2:5432 / tdv2_db / USUARIO_TDV2](docs/migracion/evidencia-postgresql-servidor-preflight.json), PostgreSQL 18.6 Ubuntu, TLS confirmado, proveedor secrets.json, public vacío y permisos suficientes |
| Aplicación real autorizada | [001, 002, 003 y 004 aplicadas](docs/migracion/evidencia-postgresql-servidor-aplicacion.json); 14 tablas TDV2 más historial, claves primarias/índices válidos/restricciones validadas y sincronización pausada |
| Segunda ejecución del mismo apply | [0 aplicadas, 4 omitidas, 0 pendientes](docs/migracion/evidencia-postgresql-servidor-repeticion.json); misma huella del esquema |
| Comparación de metadatos | [Columnas, tipos, nulabilidad y defaults coinciden con el ensayo desechable](docs/migracion/evidencia-postgresql-servidor-verificacion.json); automática/ILDA desactivadas, próxima ejecución y reservas nulas |

La primera ejecución sintética fue impedida por el sandbox al iniciar PostgreSQL; se repitió fuera del aislamiento sobre un clúster nuevo y sólo dentro de `.artifacts`. Otra ejecución detectó que `inet::text` incluye máscara; se corrigió a `host(inet_server_addr())` y se obtuvo 19/19 antes de conectar al servidor. Los clústeres desechables quedaron detenidos. No se ejecutaron SQL de transition, cargas sintéticas remotas, login ni consultas a Nexo/SII/ILDA. Las únicas inserciones de bootstrap remoto son la configuración pausada prevista en 004 y los cuatro registros del historial.

### Pendiente

Inicialización e idempotencia verificadas en el servidor autorizado. No se acredita con ello el login institucional, conectividad Nexo/SII/ILDA, conversión de datos anteriores, publicación Ubuntu del sitio ni cierre de la migración funcional. Las evidencias anteriores sobre bases locales conservan su alcance histórico; `ConnectionStrings:Tdv2` vigente apunta ahora al servidor por decisión del usuario.

## Corrección del arranque de Visual Studio — 2026-10-01

### Implementado

- Se obtuvo de la instancia abierta el [error completo de implementación](docs/migracion/evidencia-vs-implementacion-antes.txt): configuración vacía de `launch.json`, `Value cannot be null. Parameter name: source`. ClientApp no tenía `.vscode/launch.json`; JSPS fallaba antes de ejecutar npm. El rechazo TCP de ReactShell era consecuencia de no haber Vite escuchando.
- Nuevo `ClientApp/.vscode/launch.json` para Edge; perfil compartido con `DebugTarget` explícito de cada proyecto y directorio de trabajo explícito en `.esproj`. Se conserva `npm run dev` y su dirección estricta `127.0.0.1:5173`. `https` no abre un segundo navegador; `https-compiled` sí conserva su apertura independiente.
- JSPS comprueba el puerto del destino antes de iniciar ASP.NET. El destino de implementación usa Vite y su ruta de desarrollo `/__vite/__launch`, que espera la salud local de Kestrel y redirige al HTTPS fijo. Espera acotada, respuesta genérica si no arranca; no se publica esa ruta. Edge conserva el depurador JavaScript y el mapeo de fuentes a través de `/__vite/`.
- [Sobrescrituras verificadas](docs/migracion/evidencia-vs-configuracion.json): `appsettings.Development.json` conserva `UseVite=false`, sobrescrito por `ReactDevelopment__UseVite=true` del perfil `https`. Sin otra clave React en User Secrets ni variables ambientales del proceso de comprobación/usuario/equipo. UserSecretsId, las nueve claves, el retorno `https://localhost:7136/connect`, interfaz y datos permanecen; Herd no se leyó ni modificó.
- La modificación previa de `tdv2.slnx` (Build/Deploy de ClientApp y organización) ya estaba presente al iniciar este trabajo y se conservó.

### Probado

| Comprobación | Resultado / alcance |
|---|---|
| `dotnet build tdv2.slnx --no-restore -p:NuGetAudit=false` | 0 errores, 0 advertencias |
| F5 real mediante EnvDTE `Debug.Start` en VS Community 2026 18.10.3 | [Implementación 1 correcta, 0 con errores](docs/migracion/evidencia-vs-implementacion-despues.txt); depuradores .NET y JavaScript, portada Transformación Digital; arranque en frío con 5173/7136/5064 inicialmente libres |
| `node tests/browser/visual-studio-startup.mjs` desde ClientApp, contra los procesos del IDE | [Vite 200, arranque 302, HTTPS confiable, portada y React renderizados, props 200, HTML privado 401, WebSocket HMR en 7136 y cero errores JS](docs/migracion/evidencia-vs-react.json); fuentes externas bloqueadas |
| `dotnet run --project tests/TDV2.LocalConfigurationVerification --no-build --no-restore -- <raíz>` | 9 claves desde User Secrets; cadenas analizadas sin abrir conexiones, callback/base/ILDA conservados |
| `node node_modules/typescript/bin/tsc --noEmit` y `npm.cmd run build` desde ClientApp | Correctos; Vite 8.3.2 y dependencia/lockfile conservados |

Se usó el Node ya incluido en Visual Studio para los verificadores; el IDE ejecutó su propio `npm run dev`. No se ejecutó StartDatabase, SQL, migración, sincronización ni inicio de sesión. Los intentos intermedios no se cuentan como éxito: el IDE requirió recargar el nuevo perfil; un destino que apuntaba directamente a 7136 falló durante Implementar y un destino Node no inició Vite. La configuración final usa Edge con comprobación de 5173 y espera de Kestrel.

### Pendiente

Este fallo de F5 queda verificado en el IDE. No se validaron breakpoints en todas las pantallas, autenticación institucional ni aceptación visual completa con fuentes externas. Continúan los pendientes funcionales de la migración descritos debajo; no se declara la migración terminada.

## Preparación para mantenimiento y Visual Studio — 2026-10-01 (evidencia anterior)

### Implementado

- Program.cs queda centrado en el host y la selección explícita del procesador. Controllers atiende HTTP; Services coordina formatos, colaboradores, Microsoft y contextos; Infrastructure conserva Npgsql y SQL local. Se extrajeron consultas de colaboradores/sincronizaciones y middleware de errores/autorización. No se añadieron proyectos .NET, ORM ni autenticación de demostración. Se conservan rutas, JSON, 419/409 y límites por operación.
- Comentarios y contratos XML en español explican UR/empleado textual, administrador combinado, intersección local/Nexo, identidad real/efectiva, tokens sólo de servidor, avance, revisión de contexto, transacciones y reservas. El latido sigue sin renovar reservas; no se movieron commits fuera de sus comprobaciones ni de la auditoría.
- ClientApp.esproj reutiliza React mediante el sistema JavaScript de Visual Studio. tdv2.slnLaunch inicia ClientApp y tdv2; launchSettings conserva 7136/5064 y ofrece https con Vite y https-compiled. El proxy de desarrollo /__vite/ usa SpaServices.Extensions; OAuth, cookies, JSON, CSRF y HMR conservan el origen 7136. No rediseño.
- Configuración general y límites en appsettings, opciones de desarrollo en appsettings.Development, credenciales en User Secrets y puertos en launchSettings. UserSecretsId y las nueve claves importadas se conservan. Tras verificar el reemplazo, se retiró el código que leía institutional.clixml: Import/Configure/MigrateSecrets sólo orientan al editor. DPAPI permanece para administrar el clúster aislado y proteger llaves ASP.NET de Windows. User Secrets no es cifrado ni fuente de producción.
- Publicación del backend compila React mediante npm ci/build y empaqueta wwwroot, sin Vite de producción. DataProtection:KeyDirectory permite llaves persistentes externas. El paquete dependiente de ASP.NET Runtime se genera con UseAppHost=false. Preparación Ubuntu documentada, sin instalar ni desplegar.
- Ensayo separado database/transition + Prepare-Transition.ps1: sólo admite una copia local identificada, valida esquema/claves/relaciones y datos básicos, conserva catálogos/respuestas con huellas transaccionales, aísla tokens Laravel y añade 002/003. Rechaza reservas y reaplicación; no usa 001/004 sobre Laravel. Pruebas sintéticas en el proyecto nativo existente. **Falta acceso/copia real autorizada de tdv2_db; no se respaldó ni consultó esa base.**
- README y guía local reorganizados. Historial DPAPI/User Secrets anterior trasladado a HISTORIAL_LOCAL.md, conservando su evidencia. Guías específicas de transición y publicación. Se registró el estado inicial en Git (`0e12049`) antes de incorporar los cambios revisables; no se publicó a un remoto.

### Probado

| Verificación | Resultado y alcance |
|---|---|
| Compilación de la solución | 0 errores/advertencias; ejecuciones indicadas con NuGetAudit=false |
| TDV2.Verification | 56/56, dominio/HTTP con dobles en memoria |
| Test-NativePostgres.ps1 | 91/91 grupos: 90 HTTP/SQL y un grupo con 31 recorridos Edge; PostgreSQL aislado real, proveedores institucionales sintéticos |
| Test-UserSecrets.ps1 | Nueve claves efectivas; ediciones conservadas ante comandos retirados y archivo original restaurado byte por byte; sin conexiones remotas |
| Verificador con --production | Production no carga User Secrets; credenciales externas requeridas |
| Ensayo de transición final | 16/16; respaldo/restauración sintéticos, contrato, rollback y validador real de formatos |
| Regresión con dependencias corregidas | 31 recorridos Edge repetidos; sin repetir ni sumar los 90 casos SQL |
| TypeScript / pruebas frontend | TypeScript sin errores; 21/21 casos conservados |
| Test-Development.ps1 | Cuatro comprobaciones HTTPS/React/401/CSRF/Microsoft 302, más HMR sin navegación, cookies seguras, rechazo de HTML privado y error 503 controlado con Vite detenido |
| dotnet publish + publish-flow.mjs | Paquete Production inicia con HTTPS y React compilado sin Vite; /connect 503 sin secretos, /inicio 401 y CSRF 419 |
| Conservación por hashes | 30 archivos de interfaz/estilos/portada/logos/SQL 001–004 sin cambios; 154/154 archivos de referencia Laravel sin cambios |

Evidencia: [regresión PostgreSQL](docs/migracion/evidencia-organizacion-postgresql.json), [regresión del navegador con el lockfile final](docs/migracion/evidencia-organizacion-regresion-navegador.json), [arranque y redirección local](docs/migracion/evidencia-organizacion-arranque.json), [configuración](docs/migracion/evidencia-user-secrets.json), [Vite y HMR](docs/migracion/evidencia-vite.json), [publicación](docs/migracion/evidencia-publicacion.json), [conservación](docs/migracion/evidencia-conservacion-organizacion.json) y [transición sintética](docs/migracion/evidencia-transicion-sintetica.json). La interfaz/depurador de Visual Studio **no se operó**; los perfiles se ejecutaron por CLI. El 302 Microsoft no siguió la redirección ni acreditó login real. Los navegadores bloquearon fuentes web externas; no son aceptación visual exacta.

La primera preparación del fixture de transición falló por agrupar SQL parametrizado con múltiples sentencias; se separó el INSERT parametrizado y se repitió. La primera carga fría de Vite agotó 30 segundos durante la optimización; se ajustó la espera y se verificó después de restaurar dependencias. No se cuentan los intentos fallidos como éxitos. Los clústeres de prueba quedaron detenidos; el clúster local de validación permanece disponible para F5, sin automática ni trabajadores. Se conservó su usuario ya existente: los contadores observados fueron 1 usuario, 0 formatos, 0 UR y 0 ejecuciones; no se consultaron identidades ni se cargaron fixtures allí.

### Retirado y motivo

- Cinco archivos Web/*Endpoints.cs: reemplazados por controladores, servicios y consultas; regresión HTTP/SQL/navegador pasada.
- scripts/Import-LaravelSettings.ps1, scripts/Test-LocalImport.ps1 y lector institucional DPAPI de Local-Tdv2.ps1: reemplazo en User Secrets comprobado. Se conservan evidencia histórica y credenciales cifradas originales sin lecturas del arranque.
- tdv2/tdv2.http: plantilla de WeatherForecast sin ruta correspondiente.
- Microsoft.AspNetCore.OpenApi: paquete sin uso, reemplazado por la dependencia concreta del proxy Vite.
- concurrently: ninguna referencia de ejecución; el inicio múltiple lo gestiona Visual Studio. La auditoría inicial npm encontró 8 vulnerabilidades; se aplicaron correcciones compatibles del lockfile. Vite incluía un [aviso de rutas Windows](https://github.com/advisories/GHSA-fx2h-pf6j-xcff). La [auditoría posterior](docs/migracion/evidencia-npm-audit.json) informó cero. Vite pasó de 8.0.8 a 8.3.2 y PostCSS de 8.5.10 a 8.5.28; React 19.3.0, MUI 9.4.0 y TypeScript 5.9.3 permanecieron iguales ([versiones](docs/migracion/evidencia-dependencias.json)). La publicación, HMR y los 31 recorridos de navegador se repitieron con el lockfile corregido.

### Pendiente concreto

Operar el perfil compartido/F5 en la interfaz de Visual Studio; validar Entra/Microsoft/Graph y permisos institucionales Nexo; obtener y contrastar una copia autorizada de tdv2_db (tipos/longitudes/secuencias/datos históricos/UTC); conectores SII/ILDA reales; aceptación visual con fuentes, teclado/móvil/impresión; ejecución Ubuntu, proxy, supervisor, llaves persistentes y recuperación operativa. **No se desplegó ni se declara terminada la migración institucional.**

Comandos actuales y ubicaciones: [README](README.md), [arranque Visual Studio](docs/ARRANQUE_LOCAL_WINDOWS.md), [transición](docs/migracion/TRANSICION_DATOS.md), [publicación](docs/PUBLICACION.md). No usar el historial como instrucciones para reimportar configuración.

## Inspección y límites

Preparaciones anteriores: [historial local](docs/migracion/HISTORIAL_LOCAL.md). Se conservan resultados, limitaciones y referencias; sus lanzadores/importadores anteriores no son instrucciones vigentes.

- Inventario: 27 rutas propias, 27 tablas declaradas, 119 métodos PHP y 12 pruebas frontend originales. **154/154 hashes de Herd siguen iguales** al manifiesto inicial. Las pruebas PHP son criterios de aceptación; no se ejecutaron.
- Para este bloque se revisaron antes de implementar CollaboratorController, RepresentationController, PreviewController, ConfigurationController, NexoDelegation, NexoRepresentation, RepresentationAudit, PreviewSession/ViewContext, FormAccess, middleware, rutas/configuración y pruebas asociadas.
- Para sincronizaciones se revisaron InstitutionalSource/InstitutionalSync, IldaSource/IldaSync/IldaInventory, SyncManager/SyncController, comandos, configuración y migraciones 2026_09_25_000001/2026_09_30_000001, además de InstitutionalSourceTest, InstitutionalSyncTest, IldaInventoryTest y ConfigurationSyncTest. Contratos y diferencias: [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md).
- Contratos, diferencias y recuperación: [DELEGACION_REPRESENTACION.md](docs/migracion/DELEGACION_REPRESENTACION.md). Configuración de identidad/SQL: [AUTENTICACION_POSTGRESQL.md](docs/migracion/AUTENTICACION_POSTGRESQL.md). Inventario original: [INVENTARIO.md](docs/migracion/INVENTARIO.md).

## Implementado

### Base, autenticación y formatos conservados

- React, TypeScript, nueve pantallas, MUI, tema, CSS, declaración de tipografías, portada y navegación originales. Comunicación JSON del mismo origen sustituye Inertia. El historial sólo guarda URLs, nunca props ni identidades.
- Microsoft conserva /connect inicio/callback, tenant GUID, state de un uso con 600 segundos de vigencia, vínculo al navegador y PKCE S256. Canje e identidad Graph /me desde servidor, dominio institucional y object ID GUID; Nexo se valida antes de persistir. Logout revoca la sesión y redirige al destino Microsoft fijo.
- Sesión cifrada PostgreSQL de dos horas, cookie Secure/HttpOnly/SameSite=Lax y CSRF. Graph cifra/refresca tokens con bloqueo; foto fallida no concede permisos ni termina sesión.
- Nexo sigue siendo autoridad. Aplicación única resuelta por **clave literal tdv2**, ID positivo publicado y nivel 2; identidad individual, empleado textual, roles, módulos y parentesco/ruta vigentes. No se aceptan identidad, roles o UR de React como autorización.
- Formatos sólo para UR activas nivel 2/3. Áreas subordinadas a nivel 3 comparten su formato cuando están autorizadas; no se crean formatos por áreas inferiores. Administrador conserva consulta institucional y edición limitada a su rama nivel 2, incluso combinando roles.
- Respuestas canónicas, JSON, versión, avance calculado en servidor, autor y timestamps. GET no crea filas; bloqueo de UR serializa primera creación y actualización. Versión obsoleta devuelve 409; error SQL revierte el cambio.
- Autorización en HTML y endpoints, revalidada por solicitud. Nexo caído no concede permisos de respaldo.

### Administración delegada

- Búsqueda, alta y retiro mediante vistas y funciones publicadas por Nexo. Nexo permanece Laravel; no se escriben sus tablas internas.
- Intersección de responsabilidad/adscripción válida con nexo_delegacion; niveles_delegacion=[2], como la configuración Laravel. Ser administrador no basta para delegar.
- Roles asignables sólo desde nexo_delegacion_roles; rol_id enviado por React se ignora. Nexo comprueba persona/origen/alcance y, tras el alta central, TDV2 vuelve a leer identidad publicada antes de crear el vínculo.
- Colaborador local: formato 2/3 correspondiente a su adscripción. Colaborador de áreas dependientes/global: formatos de la rama nivel 2 otorgante. Siempre se requiere vínculo local y concesión central vigente coincidente.
- Altas concurrentes serializadas por persona/origen/rol. Retiro local y auditoría se confirman antes de llamar Nexo; fallo central devuelve 202, conserva acceso revocado y muestra retiro pendiente con reintento. Se conserva la concesión si otro vínculo local activo la utiliza.
- Búsquedas distinguen vacío, falta de autorización y fallo de conexión, sin convertir errores en listas vacías.

### Actuar como usuario y vista por rol/área

- Ambas herramientas permanecen en Configuración → Pruebas de acceso. Configuración exige administrador y módulos vigentes; representación además exige capacidad independiente de Nexo.
- Buscar/iniciar/validar/finalizar usan nexo_a{ID}_representacion(jsonb), actor real en servidor y token cifrado. Delegación durante representación usa esa misma función con actor real + token; nunca suplanta directamente al actor ante las funciones de delegación.
- Estado efectivo verificado en servidor por solicitud: actor/persona/ID, caducidad local y central, autorización vigente, módulos, duración y escritura. No se amplían permisos si Nexo falla, revoca o devuelve datos ambiguos.
- Identidad Microsoft real separada del perfil representado. Franja original muestra persona, actor, vigencia, consulta/escritura y salida. Caducidad/revocación bloquean el acceso; volver a permisos propios requiere salida explícita. La pantalla de acceso restringido también conserva un botón para terminar el contexto.
- Vista de prueba sólo escenario por rol/área, 30 minutos y consulta. No acepta modo usuario/email; bloquea escrituras y búsqueda delegada. Configuración, contextos anidados y mutaciones desde pestañas con contexto obsoleto se rechazan en servidor.
- Contexto cifrado/revisión por sesión en tdv2_access_contexts. Bloqueos y comparación de revisión evitan sustituciones concurrentes durante escrituras. Dos inicios simultáneos admiten uno y rechazan el otro.
- DELETE de salida y logout siguen disponibles ante falla de Nexo, con CSRF. Se descarta el contexto local; se registra si el cierre central pudo confirmarse.

### Auditoría y persistencia local

- Cambios propios y representados registran actor real, identidad representada cuando aplica, acción, recurso, contexto y resultado. Se registran también búsquedas y rechazos del bloque, incluido CSRF. No se serializan tokens, cabeceras, credenciales, errores SQL ni el motivo libre.
- Guardado de formato, alta/revocación/retiro local, inicio/salida de contexto y logout comparten transacción con su auditoría. Si la bitácora falla, el cambio local se revierte.
- No hay transacción distribuida entre TDV2 y Nexo. Un alta central puede permanecer sin vínculo local tras un error; no habilita edición y se recupera por reintento. Un inicio central sin commit local intenta finalizarse. Retiros y cierres centrales fallidos conservan estados explícitos; ver límites en el documento de contratos.
- database/001_core.sql es bootstrap sólo de base vacía; 002_aspnet_sessions.sql añade sesiones/OAuth y **003_access_contexts.sql** el estado cifrado. No convertir ni aplicar sobre bases existentes. La aplicación nunca ejecuta DDL al arrancar.
- Llaves Data Protection bajo tdv2/.runtime/keys, DPAPI Windows; las pruebas usan llaves efímeras. Nuevo login necesario para cookies Laravel y tickets ASP.NET anteriores a este bloque.

### Configuración → Sincronizaciones

- Conectores de producción SQL Server/MySQL con consultas fijas: SII sólo `poa.UNIDADES_RESPONSABLES_POA`, diez columnas y MAX(EJERCICIO) por defecto; ILDA toda `ilda_db.informacion_area`, todas las filas/columnas en JSON original y auxiliares locales. No hay escrituras remotas ni consultas remotas al abrir formatos.
- Descarga completa antes de publicar, validación de columnas/IDs/ejercicio/ciclos, límites 100000 filas y 128 MiB ILDA, reducción superior al 20% rechazada. Bajas lógicas mantienen relaciones/respuestas/versiones. Empleado SII conserva ceros y se compara con Nexo textual.
- Inventario exclusivamente PostgreSQL con igualdad exacta ur2 ↔ clave; formulario hasta 200 filas, fuente ILDA y Trámite / servicio desde informacion_generada. Incorporar nuevos IDs al GET no persiste ni sustituye respuestas guardadas, aun si el origen cambia o desaparece.
- Cola persistente y manual sii/ilda/ambas (HTTP 202); automática siempre incluye SII, ILDA optativa, intervalos/horario originales y zona America/Ciudad_Juarez mediante base IANA NodaTime. Configuración optimista y autorizada sólo al administrador real con módulos Nexo; rechaza CSRF ausente, vista de prueba y representación en páginas/endpoints.
- Reserva PostgreSQL con propietario UUID, 30 minutos, exclusión entre procesos, comprobación antes y después de cada transacción y recuperación de interrupciones sin publicar desde trabajadores vencidos. Cada catálogo/resultados/auditoría son atómicos; éxito parcial se muestra como parcial. Un cambio concurrente de catálogo antes de guardar formato exige revalidación (409).
- `--sync-worker` opera por separado del sitio, publica latido cada minuto sin renovar por ello la reserva; `--sync-once` hace un ciclo y `--sync-check=sii|ilda|ambas` sólo comprueba. Automática e ILDA deshabilitadas por defecto. No se instalaron tareas, cron ni servicios.
- Pantalla Sincronizaciones idéntica a Laravel salvo import de transporte; historial, progreso, estados, alertas y tipografías conservados. Dependencias, diferencias de tipos MySQL, zona horaria, esquema y operación Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). DDL explícito `database/004_synchronizations.sql` únicamente para la base ASP.NET aislada preparada.

## Evidencia anterior de los bloques funcionales

| Ejecución | Resultado | Alcance |
|---|---|---|
| dotnet build tdv2.slnx -p:NuGetAudit=false | 0 errores, 0 advertencias | Compilación; auditoría NuGet desactivada en esa ejecución |
| dotnet run --project tests/TDV2.Verification --no-restore | **56/56** | Dominio/HTTP con identidad, Nexo y almacenamiento en memoria sintéticos |
| npm.cmd run types:check | Código 0 | TypeScript |
| npm.cmd run test:forms | **21/21** | 12 originales + 9 de transporte; Axios/navegador simulados |
| npm.cmd run build | Código 0 | Bundle Vite |
| scripts/Test-NativePostgres.ps1 | **91/91 grupos** | 90 casos HTTP/SQL (30 del bloque) + un grupo de 31 recorridos en Edge |
| scripts/Test-NativePostgres.ps1 -SyncOnly (ajuste final SII) | **30/30** | Tras conservar booleanos textuales SII como 1/0; fuentes ADO.NET sintéticas |
| dotnet publish tdv2/tdv2.csproj -c Release --no-restore -o .artifacts/publish | Código 0 | Paquete construido; CLI inválida devuelve 2 sin conectar a fuentes |

PostgreSQL **18.6 nativo para Windows**, Edge **154.0.4258.37** sin interfaz. SQL, transacciones, bloqueos, cookies, CSRF y pantallas reales; Microsoft simulado por HTTP y Nexo simulado mediante vistas/funciones publicadas en PostgreSQL. La cuenta Nexo de pruebas sólo lee vistas y ejecuta funciones autorizadas, sin acceso directo a tablas internas. No se probó el publicador Laravel institucional. SII/ILDA usan lectores ADO.NET sintéticos con el SQL/mapeo/publicación de producción: no hubo conexión TDS/MySQL ni validación TLS o tipos nativos institucionales.

Se validaron loopback, puerto no convencional, base, usuario y SHOW data_directory antes de escribir. La suite creó un clúster nuevo dentro del destino y **lo dejó detenido**. No inició/detuvo servicios PostgreSQL existentes ni tocó sus bases.

Evidencia actual sin credenciales:

- [90 casos PostgreSQL y grupo de navegador](docs/migracion/evidencia-sincronizaciones-postgresql.json).
- [10 recorridos del módulo Sincronizaciones](docs/migracion/evidencia-sincronizaciones-navegador.json).
- [Regresión de 12 recorridos de acceso](docs/migracion/evidencia-sincronizaciones-regresion-acceso.json) y [9 recorridos de autenticación/formatos](docs/migracion/evidencia-sincronizaciones-regresion-formatos.json).
- [30 casos tras el ajuste final de conversión SII](docs/migracion/evidencia-sincronizaciones-ajuste-conector.json).

Suite completa: `.artifacts/native-postgres-20261001-065537-4cce3a7f/`. Ajuste posterior de conector: `.artifacts/native-postgres-20261001-070037-0c2a3c8e/`. Capturas: sincronizaciones-completada.png, sincronizaciones-historial.png y formato-ilda-local.png. La comprobación posterior -SyncOnly verifica el ajuste puntual de booleanos SII (1/0, igual que Laravel); no repite ni se suma a los 31 flujos del navegador.

Evidencia histórica de delegación:

- [60 casos PostgreSQL y grupo navegador](docs/migracion/evidencia-delegacion-postgresql.json).
- [12 recorridos de colaboradores/representación/vista/auditoría](docs/migracion/evidencia-delegacion-navegador.json).
- [9 recorridos de regresión de autenticación/formatos](docs/migracion/evidencia-regresion-navegador.json).

Artefactos históricos de delegación: `.artifacts/native-postgres-20261001-060357-784fc391/`, incluidos JSON, logs sintéticos y capturas representacion-activa.png, vista-rol-area.png, formato-postgresql.png y ur-denegada.png. Los informes anteriores [PostgreSQL](docs/migracion/evidencia-postgresql.json) y [navegador](docs/migracion/evidencia-navegador.json) se conservan como evidencia histórica del bloque anterior.

Casos verificados de sincronizaciones:

- SELECT fijo SII, ejercicio único, diez columnas, empleado textual y espacios; vacío, duplicados, ciclos, descarga interrumpida/incompleta, tamaño y caída inesperada conservan el catálogo anterior.
- ILDA completa (más de 200 registros), columnas adicionales, nulos/vacíos/binarios reversibles, coincidencia exacta de claves. GET sólo local incluso con fuente caída/deshabilitada; agrega IDs sin persistir y conserva respuestas/versiones tras cambios y desapariciones.
- Exclusión entre solicitudes y trabajadores, 202 sin descarga HTTP, recuperación de proceso hijo terminado, reserva vencida antes/durante publicación y trabajador viejo incapaz de liberar/publicar sobre reserva nueva. Latido no prolonga reserva.
- Publicación por fuente transaccional; ambas direcciones de fallo parcial, errores SQL/auditoría revierten catálogo/metadatos/resultado, solicitud y configuración. Comprobación CLI sin cola/publicación; reintentos explícitos convergen.
- Programación diaria y cambio estacional Juárez, intervalo y zona inválidos, dos versiones concurrentes, automática siempre SII/ILDA opcional, intervalos perdidos agrupados y activación ILDA validada.
- Administrador real, módulos padre/hijo, Nexo, CSRF y contexto exigidos en páginas y endpoints. Vista de prueba y representación rechazan configuración; cambio SII concurrente exige revalidar antes de guardar.
- React muestra pendiente, ejecutando, completada, parcial y fallida; conserva borrador de programación ante 409, historial y opciones automáticas, recupera tras error SQL/interrupción y guarda información ILDA local sin tocar MySQL. **Cero errores JavaScript de página** en los tres scripts.

Casos conservados de delegación y representación:
- Búsqueda permitida/vacía/denegada/fallida; administrador sin delegación, empleado no coincidente, rol no asignable, nivel 3 no delegable (criterio histórico sustituido el 2026-10-05 por colaboración local en su rama), persona/origen/rama forjados, administrador combinado sin ampliación.
- Persona de nivel 4 autorizada en formato nivel 3; colaborador local/global; GET/PUT de UR ajenas o inferiores denegados. Identidad no publicada tras alta impide vínculo; reintento y altas simultáneas convergen.
- Revocación local persiste si Nexo falla; reintento confirma retiro. Falla inducida en auditoría revierte alta y revocación locales.
- Administrador sin capacidad no representa; capacidad sin escritura no la concede. Búsqueda fuera de alcance, token cifrado/no publicado, actor real intacto, módulos revocados, escritura/consulta y alcance.
- Vencimiento local y central, revocación, Nexo no disponible, pestaña obsoleta, contextos anidados y dos inicios simultáneos. Regreso explícito al actor, incluido Nexo caído.
- Formato y auditoría atómicos durante representación; fallo de auditoría al iniciar intenta compensación central. Logout representado revoca sesión/contexto y audita atómicamente; CSRF rechazado identifica ambas personas sin cambios.
- Vista por rol/área sólo consulta; modo usuario, área inválida, búsqueda delegada, configuración y escrituras directas rechazados. Caducidad no restaura permisos reales automáticamente.
- Navegador: altas/retiros y reintento, mensajes diferenciados, ramas local/global, herramientas separadas, capacidad ausente, representación de consulta/escritura, autoguardado, revocación/vencimiento, salida con Nexo caído, vista de prueba y elusión de endpoints desde el navegador. PostgreSQL confirma actor/representado y ausencia de tokens en auditoría. **Cero errores JavaScript de página** en ambos scripts.

La regresión conserva OAuth/state/PKCE, identidad Microsoft, aplicación/roles/módulos Nexo, logout/CSRF, UR, guardados concurrentes/versionado, rollback/reintento y Graph cifrado/refresco. Son 31 recorridos en tres scripts (9 + 12 + 10), contenidos en un solo grupo; no se cuentan otra vez como grupos HTTP.

Tema, CSS, layout y lógica de directorio conservados (layout con sustitución de import de transporte). Ajustes puntuales en páginas y texto “áreas dependientes” documentados; no hay rediseño. Capturas inspeccionadas como evidencia funcional, **no comparación visual exacta con Herd**. Todas las solicitudes externas del navegador se bloquean, incluidas fuentes web; no acredita descarga de tipografías, MFA ni SSO real.

Ejecuciones fallidas previas se conservaron en .artifacts y no se cuentan como éxitos. Se corrigió una ambigüedad de variable en el publicador SQL sintético y un error productivo de lectura de confirmaciones delegadas de una columna; el retiro/reintento fue repetido exitosamente. En este bloque fallaron inicialmente cuatro casos por ausencia de la zona IANA en Windows (resuelto con NodaTime) y una espera prematura de carga en la prueba de dos pestañas (corregida). Una ejecución diagnóstica simultánea no pudo compilar porque Windows mantenía la DLL abierta; se repitió secuencialmente. Los fallos no cuentan como evidencia de éxito. No se ampliaron permisos para hacer pasar las pruebas.

## Comandos reproducibles

Desde ClientApp:

```powershell
npm.cmd ci --ignore-scripts
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
```

Desde la raíz del destino:

```powershell
dotnet build tdv2.slnx -p:NuGetAudit=false
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

La última orden requiere PostgreSQL nativo, Edge y certificado ASP.NET HTTPS ya existente. Usa C:\Program Files\PostgreSQL\18\bin; admite -PostgresBin. En esta sesión fue necesaria aprobación para ejecutarlo fuera del sandbox. Crea siempre un clúster nuevo, con contraseña sintética aleatoria no impresa, vacía el archivo de contraseña inicial y detiene su clúster en finally. Conserva datos/logs sintéticos. **-SkipBrowser ejecuta 90 casos HTTP/SQL; -BrowserOnly ejecuta 31 recorridos; -SyncOnly ejecuta los 30 casos del bloque sin navegador**. Ninguna ejecución parcial equivale a la suite completa. El host HTTPS lee el certificado existente; no instala certificados.

No ejecutar dos suites mientras una recompila la misma salida en Windows: el ejecutable mantiene DLL abiertas. Operación manual/continua, cancelación, reserva de 30 minutos, recuperación y comandos Windows/Ubuntu en [SINCRONIZACIONES.md](docs/migracion/SINCRONIZACIONES.md). No se instalaron tareas ni servicios.

Los proyectos de verificación son ejecutables con salida no cero al fallar, no suites descubiertas por dotnet test. Construir ClientApp antes del navegador. Los controles de fallos/datos sólo existen en el ejecutable de pruebas; no hay autenticación de demostración ni endpoints de fixture en producción.

Revalidar el manifiesto de Herd, sólo lectura:

```powershell
$reference = 'C:\Users\Jesus Arenas\Herd\tdv2'
$manifest = Get-Content -Raw -Encoding UTF8 docs/migracion/fuente-manifiesto.json | ConvertFrom-Json
$changed = @($manifest | Where-Object {
    (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $reference $_.path)).Hash.ToLowerInvariant() -ne $_.sha256
})
if ($changed.Count) { throw 'La referencia cambió; revisar el inventario.' }
$manifest.Count
```

## Pendiente de validación real y configuración faltante

- **Microsoft institucional:** TenantId, ClientId, ClientSecret y PublicOrigin local ya trasladados a User Secrets desde DPAPI. Pendientes callback registrado, cuentas autorizadas, vigencia del secreto, consentimiento/User.Read/offline_access, MFA/acceso condicional, Graph/rotación reales y logout SSO. El 302 local no valida esos aspectos. No publicar secretos en archivos o conversación.
- **Nexo Laravel institucional:** conexión importada, sin probar. Pendientes red/TLS, publicación tdv2, vistas filtradas y EXECUTE de las funciones públicas de delegación/representación (contrato Nexo 9.6.0 citado por Laravel). Sin validar firmas/tipos efectivos, publicador Laravel, idempotencia real, duración/formato de selección, responsables/roles asignables, revocaciones desde Nexo, concesiones compartidas ni auditoría central. Pruebas que alteren concesiones centrales requieren una publicación/personas autorizadas para pruebas.
- **SII/ILDA institucionales:** conexiones importadas, sin probar ni ejecutar. Pendientes acceso autorizado SELECT, certificados TLS cuando correspondan y muestra de tipos/volumen. Sin validar protocolo TDS/MySQL, autenticación real, configuración de servidor, ejercicio desplegado, tipos/precisión/texto de fechas ILDA, claves/empleados reales ni rendimiento institucional. No basta el lector ADO.NET sintético para acreditar el conector de red.
- **Procesador en despliegue:** comandos Windows/Ubuntu documentados; no se ejecutó Ubuntu, SIGTERM, supervisor, reinicio de máquina ni operación prolongada. No se habilitó ningún servicio/tarea. El fallo de fuente se probó por excepciones/lecturas incompletas, no por cortar una conexión remota real.
- **Datos existentes:** intactos y sin consultar. Ensayo sintético de conversión disponible; falta ejecutarlo sobre una copia real autorizada y contrastar esquema/tipos/índices, colaboraciones, actividad y UTC, además del corte/reversión institucional. No reutilizar cookies/cifrado Laravel ni aplicar el bootstrap sobre una base existente.
- **Operación y recuperación:** reinicio con llaves persistidas bajo cuenta del servicio, varias instancias, rotación/respaldo, limpieza de sesiones/contextos caducados, proxy/HTTPS, límites distribuidos y caída física de motor/red. Los triggers/fallos SQL no representan pérdida de confirmación de COMMIT. No hay reconciliador automático entre TDV2/Nexo.
- **Paridad documentada:** el identificador Microsoft de sesión no se regenera como en Laravel; se usa contexto cifrado con revisión atómica y rechazo de contexto obsoleto. Auditoría extendida a operaciones propias; motivo libre omitido. Login conserva bitácora básica separada de la persistencia de identidad; falta acordar metadatos históricos, retención y consulta institucional de auditoría.
- **Interfaz:** comparación visual con fuentes descargadas, móvil/teclado, impresión/exportación y borradores al caducar sesión. Capturas funcionales y código conservado no certifican estos aspectos.

## Bloques restantes

| Bloque | Estado |
|---|---|
| B0 — inventario y trazabilidad | Listo a nivel de código; esquema desplegado no consultado |
| B1 — dominio/módulos/esquema/avance | Implementado; falta paridad exhaustiva institucional |
| B2 — transporte/CSRF/formatos | Flujo principal y regresión verificados; visual y despliegue pendientes |
| B3 — Microsoft/identidad/Nexo/Graph | Implementado con proveedores simulados; **validación institucional pendiente** |
| B4 — colaboradores/delegación | Búsqueda/alta/retiro/recuperación implementados y probados con funciones sintéticas; **Nexo real pendiente** |
| B5 — Configuración/pruebas/representación/auditoría | Vista y representación separadas, permisos y auditoría local implementados y probados; **aceptación institucional pendiente** |
| B6 — ILDA/UR SII | Implementado y probado con copia PostgreSQL real/fuentes sintéticas; **conectividad, tipos y aceptación institucional pendientes** |
| B7 — programador/recuperación | Cola, reserva, caducidad, exclusión, parcial/rollback y recuperación implementados/verificados; **operación institucional/Ubuntu pendiente**, sin tareas instaladas |
| B8 — integración/corte | PostgreSQL aislado/navegador disponibles; servicios reales, visual, despliegue y corte pendientes |

Las rutas de sincronización ya tienen implementación, permisos y pruebas. Queda optimizar el listado PostgreSQL para evitar una consulta por formato. La migración exige evidencia de todos los bloques y aceptación institucional; compilar o pasar simuladores no cumple esos criterios.
