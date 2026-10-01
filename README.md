# TDV2 · ASP.NET Core 10

Migración en curso desde Laravel. Consultar [estado, pruebas y pendientes](ESTADO_MIGRACION.md), [inventario](docs/migracion/INVENTARIO.md) y [configuración de autenticación/PostgreSQL](docs/migracion/AUTENTICACION_POSTGRESQL.md).

Implementados Microsoft OAuth/Graph, sesiones revocables, acceso Nexo, formatos versionados, colaboradores mediante administración delegada, Actuar como usuario, vista de consulta por rol/área y auditoría transaccional de sus cambios. También están implementadas SII/ILDA y la cola/programación persistente con recuperación. Contratos y operación en [delegación y representación](docs/migracion/DELEGACION_REPRESENTACION.md) y [sincronizaciones Windows/Ubuntu](docs/migracion/SINCRONIZACIONES.md). **Microsoft, Nexo y las fuentes remotas se simularon; falta validación institucional y la migración no está completa.** No hay cuentas de demostración en la aplicación.

## Compilar y verificar

Requisitos: .NET SDK 10 y Node compatible con Vite 8. Dependencias fijadas en ClientApp/package-lock.json.

Desde ClientApp:

```powershell
npm.cmd ci --ignore-scripts
npm.cmd run types:check
npm.cmd run test:forms
npm.cmd run build
```

Desde la raíz:

```powershell
dotnet build tdv2.slnx
dotnet run --project tests/TDV2.Verification --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-NativePostgres.ps1
```

La última orden requiere PostgreSQL nativo, Edge y certificado ASP.NET HTTPS existente. Crea y detiene un clúster nuevo dentro del destino, con datos sintéticos y sin conectarse a bases existentes. Admite -PostgresBin, -SkipBrowser, -BrowserOnly y -SyncOnly. Los resultados quedan en .artifacts y devuelve error si falla una comprobación.

TDV2.Verification usa autenticación/Nexo/almacenamiento en memoria sintéticos (56 verificaciones). TDV2.NativeVerification conserva proveedores productivos y usa PostgreSQL real (90 casos HTTP/SQL y 31 recorridos de navegador), Microsoft simulado, vistas/funciones Nexo sintéticas y lectores ADO.NET sintéticos para SII/ILDA. La selección -SyncOnly contiene 30 casos; los detalles de cada ejecución están en el estado. Ambos son ejecutables; no se descubren con dotnet test. La suite frontend contiene 21 pruebas.

## Ejecutar

Para la validación personal en Windows, abrir tdv2.slnx en Visual Studio, establecer tdv2 como proyecto de inicio y editar **Administrar secretos de usuario** desde ese proyecto. Microsoft/Nexo/SII/ILDA y la conexión local ya están en User Secrets. Ver [archivo, arranque y parada](docs/ARRANQUE_LOCAL_WINDOWS.md). Antes de F5, desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Local-Tdv2.ps1 StartDatabase
```

La terminal puede cerrarse: sólo deja PostgreSQL aislado activo. Seleccionar **https** y pulsar **F5**: https://localhost:7136. React compilado se sirve desde ASP.NET; no necesita Vite. Para terminar: Mayús+F5 y el script con `Stop`. Se conserva `tdv2_local_validation`, automática apagada y sin trabajadores. Falta validar los servicios reales y registrar el retorno Web https://localhost:7136/connect si no existe en Entra. `Prepare` recompila/verifica migraciones sin vaciar la base ni modificar User Secrets. `Import`/`Configure` anteriores ya no escriben configuración.

Alternativa: `Local-Tdv2.ps1 Start` o `dotnet run --project tdv2 --launch-profile https` (con la base activa). Ambos usan el mismo User Secrets de Development; el lanzador dejó de imponer valores DPAPI por variables de entorno. User Secrets guarda JSON en el perfil Windows fuera del repositorio, sin cifrado DPAPI. /health/live sólo indica que el proceso responde; no certifica Microsoft/Nexo ni PostgreSQL.

La aplicación no aplica DDL, no activa sincronizadores ni crea cuentas de prueba al iniciar. database/001_core.sql sirve únicamente para una base vacía; no ejecutarlo sobre Laravel. No copiar .env de Herd ni usar bases existentes para pruebas.

## Estructura

- tdv2/Domain: UR, roles/módulos, esquema canónico y avance.
- tdv2/Security: Microsoft, identidad local, cifrado, sesiones/contextos, PKCE, tokens y auditoría.
- tdv2/Infrastructure: conexiones, consultas Nexo y persistencia de formatos.
- tdv2/Synchronization: conectores, copias locales, validación, publicación y procesador persistente.
- tdv2/Web: límites de autorización, endpoints y contrato JSON para React.
- ClientApp/resources: pantallas, componentes, MUI y estilos originales; transporte en lib/navigation.tsx.
- database: bootstrap aislado y tablas adicionales ASP.NET, sin aplicación automática.
- tests y scripts: verificaciones, clúster nativo aislado e inventario de referencia.
- docs/migracion: contratos, hashes y evidencia con alcance explícito.

Permanecen pendientes la conectividad y aceptación institucional de Microsoft/Nexo/SII/ILDA, pruebas operativas Ubuntu/supervisor, aceptación visual y corte. Aplicar 003_access_contexts.sql y 004_synchronizations.sql sólo en la base TDV2 aislada preparada; no hay DDL automático. Conservar llaves de Data Protection fuera del repositorio y planificar nuevo login: cookies y cifrado Laravel no son intercambiables. Los tickets ASP.NET anteriores al bloque de contextos también requieren nuevo login.
