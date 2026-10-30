# Indicadores, submódulos y reapertura individual — 2026-10-29

## Alcance y diagnóstico comprobado

Se conserva el trabajo local previo: instrumento v2 y sus once equivalencias, captura fluida, suspensión del autoguardado tras fallos, identidad canónica, privacidad, cola Nexo, informes y recursos institucionales. Esta revisión añade una excepción administrativa **individual** al cierre definitivo. No se ejecutaron reaperturas, reinicios ni migraciones institucionales, ni cambios en Nexo/Azure, secretos o publicación.

**Comprobado en código:** la consulta de población exigía además un permiso de módulo a `/competencias-digitales/encuesta`. Esa condición excluía a personas con acceso vigente a la aplicación y se retira conforme a la definición nueva. La consulta también rechazaba correos canónicos con diferencias de mayúsculas/espacios; ahora usa la misma normalización que la resolución personal. Una ausencia de empleado ya no se omite como si la persona no perteneciera a la población: invalida la comprobación.

**Límite institucional pendiente:** no se pudo reproducir la excepción original del caso del usuario. El entorno de ejecución denegó la lectura del archivo User Secrets; el diagnóstico no obtuvo las conexiones y no abrió bases institucionales. Esto **no demuestra** que falten secretos, permisos o datos en el entorno que utiliza el operador. No se declara resuelta la causa concreta de «No disponible» por superar pruebas sintéticas. Se conservan los valores nulos cuando la población no se puede certificar.

Se agrega un comando explícito de lectura en NativeVerification:

```powershell
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.NativeVerification/bin/Release/net10.0/TDV2.NativeVerification.dll --digital-population-readonly
```

El comando utiliza User Secrets del proyecto o las variables de entorno existentes, valida `CompetenciasDigitales:Nexo:ApplicationId` contra `nexo_aplicacion`, lee el catálogo compartido, los submódulos publicados y ejecuta la consulta productiva de población. Compara cantidades con encuestas existentes sin imprimir personas, números de empleado ni correos. Fuerza `default_transaction_read_only=on` y un tiempo límite en **cada conexión**. No arranca ASP.NET, OAuth, auditoría, sincronizadores ni migraciones. Las fallas reportan tipo/SQLSTATE seguros, sin mensaje SQL completo. La comprobación HTTP con la sesión del operador es un paso posterior distinto.

Si el operador continúa viendo información no disponible, este comando permite distinguir: publicación de otra aplicación; lectura denegada/objeto o campo ausente (SQLSTATE); cadena/adscripción/canónico inconsistente. No debe sustituirse el total por cero ni corregirse el padrón desde TDV2. Los contratos y permisos reales deben comprobarse con ese resultado antes de atribuir la falla a Nexo o SII. El error `SII_LECTURA_O_INTEGRIDAD` no se da por resuelto.

## Población y cálculos vigentes

La población son **personas con acceso efectivo vigente a Competencias digitales**, aunque nunca hayan iniciado sesión ni tengan encuesta. Se reutilizan `public.nexo_aplicacion`, `public.nexo_usuarios` y `public.nexo_usuario_rol`, publicados para la cuenta de conexión de esa aplicación. La vigencia de pertenencias y concesiones centrales/delegadas procede de la publicación existente; se inspeccionaron `PostgresPublication.php` y `PostgresUserUnits.php` como referencia de solo lectura. No se implementan concesiones alternativas ni tablas centrales.

La cuenta individual, el empleado institucional y los roles admitidos son los mismos de `DigitalAccess.ValidateEmployee`. Se elimina **únicamente** la exigencia colectiva de módulo Encuesta: los endpoints de captura siguen exigiendo su permiso publicado. No se conceden permisos por pertenecer a la población. No se consulta Microsoft Graph ni se buscan cuentas alternativas por alias al contar.

Una consulta agrupada deduplica por canónico y empleado textual. Roles repetidos no multiplican filas; un mismo canónico con adscripciones incompatibles o diferentes canónicos con el mismo empleado impiden certificar la población. Se conservan ceros iniciales. El correo normalizado no es clave de unión con respuestas: las encuestas existentes se relacionan por el empleado estable. No se crean usuarios locales ni intentos ficticios.

Participación usa acceso y adscripción **actuales** dentro del alcance autorizado; la jerarquía completa conserva los nodos N como puentes. La agrupación por nivel 2 incluye descendientes de niveles 3–5. El total se calcula desde personas únicas, nunca sumando filas superpuestas.

- Finalizadas: la encuesta actual conserva un envío definitivo, incluso de una edición anterior válida.
- En curso: captura laboral/respuestas reales y sin envío definitivo actual; incluye una encuesta reabierta. Login, perfil institucional automático y aceptación aislada no constituyen inicio.
- Sin iniciar: el resto de la misma población.
- Participación: `100 × Finalizadas / Población`; con población vacía no se divide ni se muestra 100 %.
- Madurez: promedio de los informes actuales finalizados evaluables de esa población, sin añadir ceros para quienes no respondieron.

Se cumple `Finalizadas + En curso + Sin iniciar = Población`. En el ejemplo sintético, 10 = 6 + 2 + 2 (60 %); tras reabrir una: 10 = 5 + 3 + 2 (50 %).

Resultados generales sigue usando el contexto congelado del último envío definitivo por persona dentro del alcance. Mientras una encuesta está reabierta, conserva ese último informe archivado; cuando vuelve a enviarse utiliza el nuevo definitivo, sin duplicar personas. Los envíos anteriores permanecen consultables por su dueño. Esta consulta histórica no es el numerador de participación vigente. Las lecturas compuestas de informe/agregados usan una instantánea transaccional consistente y el bloqueo de generación.

## Filtros

Para administrador/supervisor, ambos reportes ofrecen **Nivel de UR: 2, 3, 4, 5**. El backend aplica 2 cuando no llega un nivel; la interfaz representa ese valor real y Restablecer vuelve a la consulta sin filtro explícito. Una URL con un nivel válido conserva su selección. El selector mide 150 px en escritorio y se adapta en móvil. Las unidades tipo N no son opciones; siguen formando parte de relaciones y agrupaciones. Responsables conservan exclusivamente sus contextos autorizados, sin selector institucional de nivel.

## Configuración y autorización

| Entrada | Clave | Ruta | Orden |
| --- | --- | --- | --- |
| Configuración | La clave ya publicada para el padre, sin renombrarla | `/competencias-digitales/configuracion` | Principal 5, mantenido por Nexo |
| Gestión de encuestas | `gestion_encuestas` | `/competencias-digitales/configuracion/encuestas` | Dentro del padre 1 |
| Actuar como usuario | `actuar_como_usuario` | `/competencias-digitales/configuracion/actuar-como-usuario` | Dentro del padre 2 |

El contrato ya lee `id`, `clave`, `ruta`, `modulo_padre_id` y `orden` de `nexo_modulos`, unido a `nexo_modulo_rol`. Cada hijo requiere su clave/ruta exactas, padre autorizado real y rol administrador. No se infieren permisos del padre ni se muestran hijos en la barra principal. La ruta padre permanece como entrada con enlaces y estado de catálogos/metas históricas. Los endpoints anteriores de reinicio/representación conservan rutas, pero ahora exigen el hijo correspondiente.

La representación bloquea ambos submódulos y toda escritura administrativa. También revalida el hijo de representación del administrador original durante una sesión representada; su salida sigue disponible aunque se revoquen permisos o falle el servicio central. Los registros institucionales creados por el operador no pudieron leerse en esta ejecución; si no aparecen, comprobar publicación y relación módulo–rol administrador mediante el mecanismo administrativo de Nexo, sin crear claves nuevas.

## Reapertura e históricos

Gestión permite buscar encuestas enviadas por nombre o correo, mostrando identidad, UR, estado y último envío. La confirmación identifica una sola encuesta y explica qué se conserva. Se envían UUID de operación, UUID de encuesta, versión observada y generación; ninguno concede autorización.

La operación transaccional archiva `to_jsonb(cd_surveys)` completo: respuestas, perfil, contexto institucional, instrumento, informe y fecha del envío. La misma fila de encuesta permanece con su UUID/empleado, respuestas y perfil intactos; cambia a captura, eleva `Version` y `Revision` y registra `MinimumVersion`/`ReopenedAt`. No se borran aceptaciones. Si cambió la versión vigente del aviso, se exige la aceptación pendiente conforme al flujo existente; no se atribuye una aceptación nueva al administrador.

Una revisión reabierta conserva su instrumento original, incluidas preguntas y etiquetas históricas. No exige ni permite actualizar automáticamente su metodología. Al volver a enviar se evalúa con esa versión, se congela un nuevo definitivo y queda cerrada. Las instantáneas antiguas no se recalculan ni sobrescriben. Mi informe distingue captura actual/preliminar e informes históricos; la API de historial comprueba propiedad, aun para administradores.

El bloqueo compartido de generación coordina con el reinicio global exclusivo. El bloqueo por empleado serializa guardados, finalización y reapertura; el UUID de operación tiene además bloqueo propio. Versión observada y estado definitivo impiden dos reaperturas simultáneas distintas. Un reintento de la misma operación devuelve el recibo anterior, incluso si la persona ya envió otra vez; nunca vuelve a abrirla. Solicitudes de versiones anteriores a `MinimumVersion` se rechazan antes de los atajos de idempotencia. El UUID del intento no cambia y el índice único de empleado conserva una sola revisión activa.

La auditoría durable guarda actor real, empleado del actor, persona, fecha, operación, generación, envío archivado y versiones anterior/resultante. El evento `administracion / reabrir_encuesta` usa la cola/recibo Nexo existente con `usuario_afectado_email` y referencia a la operación; no envía respuestas. Una reapertura equivale a un evento, aunque se repita. Los nuevos envíos definitivos distinguen revisión en su clave de idempotencia. El autoguardado conserva cero eventos centrales y Guardar avance su confirmación manual única.

## Migración y permisos — aplicación posterior del operador

Nueva migración **exclusivamente DigitalDbContext**: `20261030042854_IndividualDigitalReopening` (fecha UTC de generación). La inicial y las demás migraciones anteriores permanecen intactas.

- `cd_surveys`: añade `Revision` y `MinimumVersion` (1 para filas existentes), `ReopenedAt` nullable.
- `cd_submissions`: instantáneas definitivas UUID, identidad textual, UR congelada, generación, revisión, fecha y JSON; único `(SurveyId, Revision)`.
- `cd_reopen_operations`: recibos/auditoría UUID inmutables. No guarda respuestas.
- Función restringida `public.cd_reopen_survey(uuid,text,text,uuid,integer,uuid)`; `SECURITY DEFINER`, `search_path=pg_catalog,pg_temp`, EXECUTE público revocado.
- La excepción del trigger exige propietario de esa función, instantánea exacta y recibo concordante; admite sólo el cambio controlado de cierre a captura. No depende de flags manipulables, no desactiva triggers, no permite DELETE ni actualización normal de finales.
- Históricos/recibos rechazan UPDATE/DELETE mediante trigger. No se añaden secuencias ni se renumera ningún identificador.
- El reinicio global conserva confirmación/caducidad/generación/idempotencia y ahora cuenta/vacía también `cd_submissions` explícitamente, sin CASCADE. Conserva `cd_reopen_operations` y todas las bitácoras. Su diálogo muestra los envíos históricos que eliminaría.

El propietario de funciones/tablas debe ser una cuenta de migraciones separada, preferentemente NOLOGIN. El rol web no debe ser propietario, superusuario, miembro del propietario ni tener DDL/TRUNCATE. Conserva los permisos previos y requiere adicionalmente:

```sql
-- Sustituir únicamente por el rol real del diagnóstico, verificado por el operador.
GRANT SELECT ON public.cd_submissions, public.cd_reopen_operations TO "ROL_WEB_DIAGNOSTICO";
GRANT EXECUTE ON FUNCTION public.cd_reopen_survey(uuid,text,text,uuid,integer,uuid) TO "ROL_WEB_DIAGNOSTICO";
```

No se requieren permisos nuevos de Microsoft, secretos, conexión, IDs ni tablas centrales. Población requiere SELECT en las vistas existentes de aplicación, usuarios y roles; submódulos conserva SELECT de módulos/relaciones. La auditoría utiliza la función de actividad ya provisionada.

Antes de aplicar, respaldar **toda la base del diagnóstico**, verificar restauración en un destino aislado y coordinar una ventana sin escritores. Con autenticación segura del operador y nombres reales comprobados (no copiar literalmente los marcadores):

```text
pg_dump --host HOST_VERIFICADO --username OPERADOR_RESPALDO --dbname BASE_DIAGNOSTICO --format=custom --file=diagnostico_pre_reapertura.dump --no-password
```

Windows/desarrollo: User Secrets existentes y credenciales del propietario para la operación explícita. Ubuntu: mismas claves de producción, `ConnectionStrings__CompetenciasDigitales`, `ConnectionStrings__CompetenciasDigitalesNexo`, `ConnectionStrings__Tdv2`, `CompetenciasDigitales__Nexo__ApplicationId`, mediante su mecanismo seguro habitual. **No hay una configuración nueva ni una contraseña en este documento.**

```text
dotnet ef migrations script 20261028175912_DigitalActivityDelivery 20261030042854_IndividualDigitalReopening --project tdv2/tdv2.csproj --context DigitalDbContext --configuration Release --output reapertura_diagnostico.sql
dotnet ef database update 20261030042854_IndividualDigitalReopening --project tdv2/tdv2.csproj --context DigitalDbContext --configuration Release
```

Son comandos para la fase operativa posterior; **no se ejecutó database update institucional**. Verificar destino exclusivo, permisos del propietario/web, los dos hijos publicados y diagnóstico de población antes de habilitar. No apuntar a TDV2 ni aplicar su migración de identidad en la base del diagnóstico.

No hay Down destructivo automático: rechaza borrar históricos/recibos. Para recuperación, detener escritores y restaurar el respaldo completo en un destino verificado, probarlo aislado y decidir el corte con el operador. No borrar sólo una tabla ni restaurar respuestas parcialmente. Si hay reaperturas/envíos posteriores, preservar primero esos datos y recibos para evitar pérdida; volver al binario anterior sin revisar la compatibilidad del nuevo estado no es una recuperación válida.

## Archivos y verificación

Backend: `DigitalAccess`, `DigitalAggregates`, `DigitalSurveyService`, `DigitalEditions`, `DigitalActivity`, `DigitalResetService`, `DigitalRepresentation`, nuevo `DigitalReopenService`, `DigitalDbContext`, `CompetenciasController`, registro DI y `PostgresNexoPopulation`. Migración nueva, Designer, snapshot y dos recursos SQL C# de esa migración. No se modifica la migración inicial.

Frontend: `Aggregates.tsx`, `Configuration.tsx`, nuevo `ReopenSurvey.tsx`, `PersonalReport.tsx`, `ResetSurveys.tsx`, `types.ts`, `CompetenciasDigitales.tsx` y CSS aislado. Survey e instrucciones simplificadas se conservan tal como estaban al comenzar.

Pruebas: nuevo `DigitalReopenTests`, `DigitalPopulationReadOnly`; fixtures/casos de `DigitalTests`, `DigitalPopulationTests`, entrada NativeTests y recorridos de navegador de Competencias/población. Fuentes Microsoft y publicación Nexo sintéticas; PostgreSQL, restricciones SQL, ASP.NET, React y Edge reales en entornos aislados. La función central de actividad se prueba con su SQL existente y tablas sintéticas; no equivale a un recibo de Nexo institucional.

### Resultados finales

| Comprobación | Resultado | Alcance |
| --- | --- | --- |
| TypeScript y compilación Vite | Correctos | Frontend actual, sin actualización de paquetes |
| Compilación Release de la solución | 0 errores, 0 advertencias | ASP.NET y proyectos de verificación |
| Pruebas frontend | 102 correctas | Formularios y reglas existentes |
| Verificación general de reglas/transporte | 116 correctas | Dobles sintéticos, no proveedor PostgreSQL |
| Competencias con PostgreSQL aislado | **79 correctas, 0 fallidas** | Migraciones, permisos SQL, captura, informes, reinicio, reapertura, concurrencia, auditoría, identidad y acceso independiente |
| Navegador con ASP.NET y PostgreSQL aislado | 6 grupos correctos, 0 errores JS | Edge/React reales; identidades y publicación Nexo sintéticas; 1440, 900 y 390 px |
| Regresión de autoguardado en navegador | 34 comprobaciones correctas, 0 errores JS | Transporte sintético con latencia/fallos, 1440 y 390 px; no es una medición SQL |
| Modelo EF frente a nueva migración | Sin diferencias pendientes | Script generado para revisión; migración aplicada sólo al PostgreSQL desechable |
| Diagnóstico institucional de lectura | **No ejecutado contra la base** | Acceso al archivo de configuración segura denegado por el entorno; sin conexiones institucionales |

Las suites se solapan y no representan un total acumulable de escenarios distintos. El recorrido comprueba reapertura mediante teclado, histórico personal separado del estado actual, confirmación móvil y filtros 2–5/restablecimiento a 2. La prueba PostgreSQL comprueba el ejemplo 10 = 6 + 2 + 2 → 10 = 5 + 3 + 2, conservación del informe anterior, exclusión de accesos revocados, personas sin módulo Encuesta, alias/canónico y roles duplicados. Un envío repetido conserva un solo recibo de reapertura; las solicitudes antiguas, cambios normales de finales y escrituras representadas se rechazan. Se conserva un evento manual confirmado y cero eventos automáticos de guardado.

En el paquete compilado, navegación y pie tienen `rgb(33, 52, 85)` en los tres tamaños con dos y cinco módulos, sin imagen de fondo ni transparencia que altere el color base. No fue necesario modificar sus colores. Se conservan dos filas, SVG blanco, cuenta y foco visible. No se detectó desbordamiento horizontal en esos recorridos. No se repitió toda la batería visual de Procedimientos ni una prueba de carga; no se afirma aceptación institucional de Nexo.

Comandos reproducibles (Node/npm disponibles en PATH; los scripts de PostgreSQL crean y cierran su clúster aislado):

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
dotnet tests/TDV2.Verification/bin/Release/net10.0/TDV2.Verification.dll
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -SkipBrowser
$env:TDV2_DIGITAL_POPULATION_UI = '1'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -DirectStart -NoBuild -DigitalOnly -BrowserOnly
Remove-Item Env:TDV2_DIGITAL_POPULATION_UI
```

El diagnóstico institucional de sólo lectura tiene un comando separado al principio del documento y no forma parte de esos fixtures.

### Evidencias

- [Resultados PostgreSQL: 79/79](evidencia/20261029-reapertura/competencias-verification.json).
- [Recorrido compilado y colores calculados](evidencia/20261029-reapertura/digital-population-compiled.json) y [autoguardado: 34 comprobaciones](evidencia/20261029-reapertura/autoguardado.json).
- [Avance en escritorio](evidencia/20261029-reapertura/compiled-avance-1440.png), [filtro móvil](evidencia/20261029-reapertura/compiled-avance-dependencia-filtros-390.png) y [Resultados generales](evidencia/20261029-reapertura/compiled-resultados-generales-filtros-1440.png).
- Gestión de encuestas en [escritorio](evidencia/20261029-reapertura/compiled-gestion-1440.png), [tableta](evidencia/20261029-reapertura/compiled-gestion-900.png) y [móvil](evidencia/20261029-reapertura/compiled-gestion-390.png).
- [Confirmación de reapertura móvil](evidencia/20261029-reapertura/compiled-confirmar-reapertura-390.png), [informe reabierto en tableta](evidencia/20261029-reapertura/compiled-informe-reabierto-900.png) e [informe histórico móvil](evidencia/20261029-reapertura/compiled-informe-historico-390.png).
- [Instrucciones y controles de encuesta móvil](evidencia/20261029-reapertura/compiled-instrucciones-390.png).

Todas las personas de las capturas y comprobaciones son sintéticas. Se conservan separados implementado, probado en aislamiento y pendiente institucional.
