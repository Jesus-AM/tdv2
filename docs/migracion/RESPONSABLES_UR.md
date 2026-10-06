# Responsables, consulta institucional y delegación — 2026-10-05

> Actualización posterior: el rol canónico es `responsable_ur_supervisor`; el anterior se conserva como alias. Configuración vigente: [Nexo y Entra](CONFIGURACION_NEXO_ENTRA.md). Las reservas y el envío incorporan una [tercera migración EF](COLABORACION_ENVIO.md). El detalle inferior documenta la entrega previa.

## Cambios implementados

El botón y las ramas candidatas de TDV2 estaban limitados a nivel 2. Se habilitó nivel 3 en `Domain/Access.cs`, `Services/CollaboratorAccess.cs` y `FormService.cs`. Una persona de la misma UR o cualquier subordinada puede recibir `colaborador_local`, siempre con identidad y concesión vigentes de Nexo y vínculo local coincidente. Para un otorgante de nivel 3 el alcance es exclusivamente su formato. Nivel 4 o inferior no obtiene un formato nuevo. Nivel 2 conserva sus dos tipos de colaboración.

`responsable_ur_institucional` funciona sin `responsable_ur`: añade lectura institucional a la responsabilidad comprobada por `num_empleado` textual. No concede funciones administrativas. Se distinguen Mis áreas y Todas las áreas, con formatos de consulta marcados; PUT y delegación siguen verificando el alcance efectivo en el servidor. La representación no hereda permisos del actor real y el escenario nuevo de vista de prueba no permite escribir.

El campo real es `TIPO_UR` de SII, replicado como `unidades_responsables_poa.tipo_ur` y mapeado por EF en `ResponsibleUnit.Kind`. Corrección vigente: se excluye **sólo tipo N**, comparando sin espacios exteriores y sin distinguir mayúsculas; tipo **0 sí es elegible** si cumple actividad, alcance y nivel. Los formatos siguen limitados a niveles 2/3. Los nodos N se conservan en la réplica y en el cálculo interno de jerarquía, pero no se ofrecen en formatos, selectores ni contadores. La jerarquía pública agrupa nivel 2 con sus dependientes de nivel 3 atravesando nodos auxiliares. No se borran filas de catálogo ni formatos históricos; no se cambia el tipo almacenado.

`displayUnitCode` sólo presenta claves numéricas sin ceros iniciales. La búsqueda acepta `06000` y `6000`; los ID, las claves originales y los vínculos siguen intactos. ILDA continúa usando la igualdad exacta de su clave de origen: dos códigos de origen diferentes no se fusionan por tener la misma presentación.

React/MUI conserva tipografía, barra clara y botones azules. El encabezado de Sincronizaciones usa regreso discreto y distribución adaptable. Actualizar estado conserva el borrador de programación; un fallo temporal permite reintentar sin desmontarlo. Se mantienen los rechazos de autorización. Transiciones de 150–220 ms se desactivan con `prefers-reduced-motion`; las áreas conservan sus componentes y el foco al expandirse.

## Código de Nexo

Esta sección describe la implementación externa de una entrega anterior. La corrección posterior de tipo `N` se aplica a TDV2; no modifica ni ejecuta Herd/Nexo. Cualquier ajuste de esa publicación central requiere un trabajo separado autorizado; TDV2 sigue exigiendo la capacidad vigente de Nexo para delegar.

Se revisó la raíz `C:\Users\Jesus Arenas\Herd\nexo`, sin ejecutar Laravel ni leer credenciales. Nexo ya publicaba delegación para niveles 2 y 3; faltaba limitar el rol asignable de TDV2 por nivel. Se modificaron sólo:

- `app/Services/DelegacionPostgres.php`: una UR tipo 0 no publica delegación para TDV2; sigue disponible como nodo jerárquico interno.
- `app/Services/Postgres/conceder_acceso.sql`: para la aplicación cuya clave es `tdv2`, nivel 3 sólo admite `colaborador_local`; nivel 2 admite también `colaborador_dependencias`.
- `app/Services/Postgres/retirar_acceso.sql`: comprueba el mismo alcance de roles al retirar.
- `app/Services/AdministracionDelegada.php`: rechaza configurar otros roles como asignables en TDV2. Roles de responsable, consulta o administración no son delegables.

Las reglas específicas se condicionan a la clave existente `tdv2`; las demás aplicaciones conservan su comportamiento. Las funciones internas de representación se generan a partir de las mismas plantillas y conservan sus controles de sesión, caducidad y auditoría de actor real/efectivo. El [parche de fuente](nexo-tdv2-delegacion.patch) permite revisar/transportar estos cuatro cambios. Ya está aplicado en la ruta local indicada; no volver a aplicarlo allí. Se verificaron hashes antes de escribir para no sobrescribir ediciones concurrentes.

## Configurar en Nexo, en este orden

Un administrador autorizado de Nexo debe hacerlo en la aplicación existente **TDV2**, clave **`tdv2`**, después de desplegar los cuatro archivos anteriores en el Nexo que publica sus vistas y funciones:

1. En **Roles**, registrar un rol activo con clave **`responsable_ur_institucional`** y nombre **Responsable UR con consulta institucional**. No es administrador y no sustituye ni renombra roles existentes.
2. En **Módulos**, usar el módulo existente **Procesos operativos**, clave **`procesos_operativos`**, ruta **`/inicio`**, sin padre. Vincular el nuevo rol a ese módulo. Conservar los vínculos actuales de responsables y colaboradores. No crear otra clave de módulo ni asociar al nuevo rol `configuracion`, `sincronizaciones` o `pruebas_acceso`.
3. En **Administración delegada → Configuración**, mantener **Habilitar administración delegada** activo. En **Roles que pueden delegar**, incluir `responsable_ur` y `responsable_ur_institucional`, conservando `administrador` si ya estaba autorizado. Este último sólo delega cuando además es encargado institucional coincidente de su rama de nivel 2.
4. En **Roles que pueden asignar**, seleccionar únicamente los roles existentes **`colaborador_local`** y **`colaborador_dependencias`**. El segundo sigue disponible para nivel 2 y las funciones lo rechazan para nivel 3. No registrar un colaborador nuevo/global ni hacer asignables los roles de responsabilidad, consulta o representación.
5. Pulsar **Guardar configuración** y comprobar que la operación queda aplicada y la administración delegada **Activa**. El guardado versiona y aplica la cuenta PostgreSQL existente, incluyendo las funciones generadas. Si no hay cambios de configuración pendientes, Nexo ofrece **Actualizar vistas y aplicar cambios** en su pantalla de sincronización de acceso PostgreSQL. No eliminar ni recrear la conexión o la base. Ante un error, usar el reintento de configuración de la conexión y confirmar que revisión y revisión aplicada coinciden.
6. En **Usuarios** de esa aplicación, el operador asignará el nuevo rol sólo a las cuentas aprobadas institucionalmente. No necesitan recibir también `responsable_ur`. Verificar `num_empleado` textual, responsabilidad vigente de UR nivel 2/3, pertenencia resuelta y módulo publicado. Conceder el rol no convierte por sí mismo a una persona en responsable.

Para diagnosticar ausencia de delegación, la publicación `nexo_delegacion` debe contener exactamente una fila del correo efectivo, `id_ur`, empleado y nivel institucional coincidentes. `nexo_delegacion_roles` debe publicar los ID reales de las dos claves existentes; TDV2 nunca acepta `rol_id` del navegador. No se inspeccionaron los registros institucionales para afirmar que esa configuración ya existe.

## Actualizar TDV2

No hay cambios de esquema ni migraciones nuevas. Se conservan las dos migraciones EF existentes; no se recrea ninguna base ni se aplica DDL en F5. Desde `C:\Users\Jesus Arenas\source\repos\tdv2`, con .NET 10 y Node/npm en PATH:

```powershell
npm --prefix ClientApp run types:check
npm --prefix ClientApp run test:forms
npm --prefix ClientApp run build
dotnet build tdv2.slnx --no-restore
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1 -NexoDelegationOnly
dotnet publish tdv2/tdv2.csproj -c Release --no-restore -p:UseAppHost=false -o .artifacts/publish-maintenance
```

`-NexoDelegationOnly` utiliza PHP instalado (`-Php` permite indicar su ruta) únicamente para generar SQL desde el fuente (`-NexoSource` permite indicar otra copia). No arranca Laravel, Artisan o Composer; ejecuta las funciones reales generadas con datos sintéticos en un clúster nuevo local y lo detiene al terminar. No usa la base de Nexo ni su `.env`.

Para desarrollo, abrir `tdv2.slnx` y ejecutar **TDV2 HTTPS + React**. Se conservan HMR y `https://localhost:7136/connect`. Para despliegue, reemplazar el paquete de TDV2 mediante el procedimiento habitual, conservando configuración y secretos del servidor. La sincronización automática permanece desactivada; estas pruebas no la activan en el destino institucional.

## Pendiente institucional

Publicar las funciones actualizadas de Nexo, registrar/configurar el nuevo rol y validar con cuentas institucionales autorizadas. No se asignaron ni retiraron colaboradores reales, no se cambiaron credenciales, no se conectó a SII/ILDA ni se ejecutaron migraciones o sincronizaciones remotas durante esta adaptación. Los resultados reproducibles y su alcance se registran en [ESTADO_MIGRACION.md](../../ESTADO_MIGRACION.md).
