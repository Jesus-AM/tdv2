# Inventario funcional y contratos

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

Dependencias a sustituir/adaptar: bootstrap createInertiaApp; Head; usePage y props compartidas; router.visit/post/delete/reload/clearHistory; eventos before/navigate; useForm de VistaPrueba; redirecciones y errores 422; recargas parciales de sincronizaciones/colaboradores. Axios guarda formatos y efectúa búsquedas/altas/bajas. Conservar protección de cambios sin guardar, autosave, conflictos, exportación/impresión y cabecera de contexto.

## Pruebas como criterios de aceptación

EVIDENCIA_FUENTE.md enumera métodos PHP y pruebas frontend; `tests/Support` contiene fábricas/dobles institucionales. Familias: MicrosoftAuthentication, GraphToken, NexoAccess, NexoIdentity, AdministratorAccess, UnitFormAccess, Collaborator, Preview, Representation, ConfigurationSync, InstitutionalSync, IldaInventory, InstitutionalSource, NexoDelegationContract; Example no acredita migración.

Priorizar denegación por defecto, revocación al siguiente request, aislamiento y combinación de roles, conflictos de versión/primer guardado, CSRF real, pestaña obsoleta, representación revocada sin restaurar permisos, publicación atómica, dos ejecutores y recuperación. Pruebas frontend de FormEditor y árbol de áreas deben ejecutarse en el destino con fuentes conservadas.

## Referencias técnicas del destino

Para implementar y verificar el transporte: [antiforgery ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0), [pruebas con WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0), [NpgsqlDataSource y parámetros](https://www.npgsql.org/doc/basic-usage.html). Las reglas de negocio proceden del código local, no de esas referencias.
