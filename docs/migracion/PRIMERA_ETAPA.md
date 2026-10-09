# Procedimientos Institucionales: primera etapa

**Actualización vigente 2026-10-09:** entrega hasta Sistemas y herramientas, incluidos Medios, con la pestaña independiente **Revisar y enviar** inmediatamente después. No suma avance ni exige las secciones posteriores. Se conservan permisos e inmutabilidad. Otro/Externo ya no se capturan; las menciones anteriores son históricas. Para instalar el código actual aplicar hasta **`20261009171656_StageSubmissions`**. [Envíos por etapa, conservación, pruebas y comandos](REVISAR_ENVIO_ETAPAS.md).

Entrega local del 8 de octubre de 2026. No publica TDV2 ni cambia Nexo, credenciales o bases institucionales. Se conserva la edición automática, las fotografías y la captura independiente por registro, sin Resolver ni comparaciones de versiones.

## Captura y conservación

- `ModuleAccess` acepta `procedimientos_institucionales` y el alias transitorio `procesos_operativos`, exclusivamente desde los módulos efectivos de Nexo para TDV2, ruta `/inicio` y módulo raíz. La navegación devuelve una sola entrada, con el nombre **Procedimientos Institucionales**. No se cambian IDs, códigos PO, contratos JSON, rutas ni migraciones anteriores.
- `FormCapture` utiliza el perfil **efectivo**. Los perfiles distintos de administrador capturan Contexto, Identificación general y Sistemas y herramientas (incluye `medios`/`medioOtro`). Reserva, renovación y guardado rechazan secciones posteriores. Representar un responsable no suma el rol del actor; Vista de prueba sigue sin escritura.
- Sólo el administrador efectivo tiene **Mostrar secciones posteriores**, inicialmente apagado. Es una preferencia de la pantalla; no cambia permisos de UR. Al ocultar una sección activa o recuperar una posición no disponible, se abre Contexto. La revalidación periódica también retira las secciones si deja de existir el rol administrativo.
- **Avance de la primera etapa** se calcula en el servidor: siete campos de Identificación (incluido código confirmado) y cuatro de Sistemas (incluido Procedimiento completo, válido y V/A). Contexto es informativo; observaciones, medios y detalle de otro medio siguen siendo opcionales. Se cuentan filas reales, con un denominador mínimo de una por tabla; no se inventan respuestas para alcanzar 100 %.
- Avance y revisión del formato vigente corresponden a esta misma etapa, también en el directorio. Los porcentajes ya guardados de enviados/históricos permanecen intactos. Revisar y enviar siempre puede abrirse; alcanzar 100 % no sustituye la revisión de requisitos, permisos, versiones y reservas antes de enviar. El responsable autorizado confirma el envío definitivo de esta etapa. Mostrar secciones posteriores no amplía requisitos; sus respuestas se preservan y no se marcan completas. Se conserva el bloqueo de enviados sin reapertura.
- Se conservan respuestas ocultas, IDs, prioridades y validaciones históricas D/N. Los enviados conservan contenido, instantánea, fecha y avance almacenados, sin normalización por esta entrega.

## Campos y ayudas

Las claves `tramite`, `usuario`, `resultado` y `responsable` se conservan; sus etiquetas son Trámite o servicio, ¿A quién atiende?, ¿Qué entrega? y Área responsable. `usuario` utiliza las siete categorías vigentes documentadas en USUARIOS_ATENDIDOS.md; Otro y Externo ya no son opciones nuevas. No se reintroduce usuarioOtro. Los formatos enviados e históricos bloqueados conservan su lectura. Un borrador incompleto se guarda y puede corregirse directamente.

`FieldHelp` es reutilizable; `ProcedureFieldHelp` contiene las explicaciones solicitadas, sin instrucciones permanentes. MUI gestiona apertura por clic/toque/teclado, Escape y foco de retorno. La ayuda también se consulta en lectura y su interacción no pide ni libera reservas. El vencimiento normal sigue vigente: abrir una ayuda no renueva indefinidamente una reserva.

Prioridad conserva números y valores históricos. Las opciones y el valor seleccionado muestran círculo/número blanco y nombre corto: 5 Muy alta `#B91C1C`, 4 Alta `#C2410C`, 3 Media `#A16207`, 2 Baja `#1E4FA8`, 1 Puede esperar `#475569`. El indicativo vacío nunca es una opción. Las explicaciones largas viven sólo en la ayuda; Preguntas no es requisito ni enlace de la primera etapa.

Sistemas utiliza **Procedimiento** y los selectores de SISTEMAS_HERRAMIENTAS.md. Ofrece sólo procedimientos confirmados, completos, válidos y V/A según ProcedureEligibility en el servidor. Un vínculo que después quede incompleto se conserva con un pendiente contextual; permite guardar otros campos y bloquea la entrega hasta corregirlo. Medios utilizados ocupa una sección independiente de la misma pestaña.

## Retiro de inventario ILDA

`FormRemoval` calcula en el servidor la cascada por ID estable: retira la identificación/evaluación y desvincula sistemas/datos conservando el resto de sus respuestas. El navegador muestra las consecuencias y reserva los bloques necesarios al confirmar. La intención de retiro acompaña reservas y guardado; las dependencias ocultas sólo admiten **exactamente** esa cascada, nunca modificaciones arbitrarias. Cambios de versión, contenido o relaciones y reservas ajenas rechazan la operación. Escritura, exclusión, versiones, recibo idempotente y auditoría se confirman en la misma transacción breve.

La tabla `formato_exclusiones_ilda` guarda UR, ejercicio, ID de inventario, fecha y actor/identidad efectiva. `LocalCatalog` aplica las exclusiones tanto a lectura individual como por conjuntos. La réplica completa y la fuente externa permanecen intactas. El registro no vuelve al formato tras una nueva publicación del mismo ID; no hay borrado global de catálogo. La reversión se detiene si hay exclusiones o formatos enviados.

## Instalación posterior por el operador

**Nueva migración: `20261009032858_FormIldaExclusions`.** Es aditiva; no vuelve a ejecutar la limpieza histórica de `usuario`. No se ejecuta con F5. Se prepara y prueba únicamente en PostgreSQL desechable durante esta entrega.

Desde la raíz del proyecto, después de revisar destino y pendientes del historial EF:

```powershell
dotnet tool restore
dotnet ef migrations list --project tdv2 -- --environment Development
dotnet ef database update 20261009032858_FormIldaExclusions --project tdv2 -- --environment Development
```

El último comando utiliza `ConnectionStrings:Tdv2` de la configuración habitual. **No se ejecutó contra la base institucional.** No arrancar esta versión contra un esquema sin actualizar. Mantener un único operador para migraciones. Si la cuenta de ejecución difiere del propietario migrador, debe poder consultar e insertar la nueva tabla conforme al esquema de permisos existente; no se cambian credenciales.

Orden en Nexo: primero instalar TDV2 compatible con ambas claves; después editar **el módulo existente** de TDV2 (mismo ID, padre/posición y asignaciones), nombre Procedimientos Institucionales, clave `procedimientos_institucionales`, ruta `/inicio`; finalmente publicar con el flujo vigente y comprobar los módulos efectivos. No crear otro módulo ni recrear aplicación/conexión. `configuracion_procesos` permanece igual. [Matriz y publicación de Nexo](CONFIGURACION_NEXO_ENTRA.md).

## Verificación reproducible y Visual Studio

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -MigrationsOnly
$env:TDV2_TEST_CASE_PREFIX='Edición/etapa:|Edición/dos sesiones|Edición/bloques distintos|Edición/vencimiento|Edición/lectura pasiva|Edición/adquisición|Edición/reintento|Edición/revocación|Edición/operación y auditoría|Edición/reserva que vence|Edición/SignalR'
$env:TDV2_TEST_BROWSER_FLOW='first-stage'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -EditingOnly
Remove-Item Env:TDV2_TEST_CASE_PREFIX
Remove-Item Env:TDV2_TEST_BROWSER_FLOW
```

El ejecutor valida base `tdv2_native_test`, usuario sintético, loopback y puerto distinto de 5432, dentro de `.artifacts`; aplica EF sólo allí y apaga el clúster. Edge usa React/ASP.NET/PostgreSQL reales con Microsoft/Nexo y catálogos sintéticos. Las rutas de fixture sólo existen en el ejecutable de pruebas. Si Node no está en PATH, usar la consola de desarrollo de Visual Studio o añadir temporalmente su carpeta NodeJs al PATH del proceso.

Abrir `tdv2.slnx`, seleccionar **TDV2 HTTPS + React**. Para probar escrituras usar exclusivamente el host sintético del ejecutor; F5 conserva la configuración local existente y no constituye autorización para escribir en una base institucional. En dos sesiones: entrar a una fila, comprobar nombre/avatar/bloqueo en la otra y editar otra fila; abrir ayuda y Select sin liberación; salir/Guardar borrador y observar habilitación automática. Comprobar Otro/recarga, eliminación sin enfoque y Cancelar. Como administrador, activar/desactivar secciones y comprobar que al volver a abrir inicia apagado. Confirmar que un responsable representado sólo ve la primera etapa.

Los resultados, capturas y límites de aceptación institucional se registran en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md). Microsoft/Nexo reales, dispositivos físicos e instalación posterior siguen siendo validaciones del operador.
