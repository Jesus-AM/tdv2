param(
    [Parameter(Mandatory=$true)][ValidateSet('Prepare','Configure','Import','MigrateSecrets','StartDatabase','Start','Stop','Status')][string]$Action,
    [string]$PostgresBin = 'C:\Program Files\PostgreSQL\18\bin'
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localRoot = Join-Path $workspace '.artifacts\local-validation'
$dataPath = Join-Path $localRoot 'data'
$manifestPath = Join-Path $localRoot 'environment.json'
$credentialsPath = Join-Path $localRoot 'database.clixml'
$projectPath = Join-Path $workspace 'tdv2\tdv2.csproj'
$projectXml = [xml][IO.File]::ReadAllText($projectPath)
$secretsId = [string]$projectXml.Project.PropertyGroup.UserSecretsId
if ($secretsId -notmatch '^[a-zA-Z0-9-]+$') { throw 'Falta un UserSecretsId valido en el proyecto tdv2.' }
$secretsPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) ('Microsoft\UserSecrets\' + $secretsId + '\secrets.json')
$configurationKeys = @('Microsoft:TenantId','Microsoft:ClientId','Microsoft:ClientSecret','Microsoft:PublicOrigin',
    'ConnectionStrings:Tdv2','ConnectionStrings:Nexo','ConnectionStrings:Sii','ConnectionStrings:Ilda','Synchronization:IldaEnabled')
$databaseName = 'tdv2_local_validation'
$adminName = 'tdv2_local_admin'
$appName = 'tdv2_local_app'
$launchProfile = (Get-Content -Raw -Encoding UTF8 (Join-Path $workspace 'tdv2\Properties\launchSettings.json') | ConvertFrom-Json).profiles.https
$urls = @($launchProfile.applicationUrl.Split(';') | ForEach-Object { [Uri]$_ })
$origin = @($urls | Where-Object { $_.Scheme -eq 'https' })
if ($origin.Count -ne 1 -or @($urls | Where-Object { $_.Host -ne 'localhost' -or $_.AbsolutePath -ne '/' }).Count) { throw 'Revisa el perfil https: se requiere un origen HTTPS localhost.' }
$origin = $origin[0].GetLeftPart([UriPartial]::Authority)
$state = $null
$credentials = $null
$controlLock = $null
function Lock-Control {
    try { $script:controlLock = [IO.File]::Open((Join-Path $localRoot 'control.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None) }
    catch { throw 'Ya hay un arranque o preparacion local en curso. Usa Ctrl+C en esa terminal antes de continuar.' }
}
function Plain([Security.SecureString]$value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
function New-Password {
    $bytes = [byte[]]::new(32)
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes); ([BitConverter]::ToString($bytes)).Replace('-','') }
    finally { $rng.Dispose() }
}
function Require-UserSecrets {
    if (-not (Test-Path -LiteralPath $secretsPath -PathType Leaf)) { throw 'User Secrets: crea la configuracion desde Administrar secretos de usuario del proyecto tdv2.' }
}
function Check-EnvironmentOverrides {
    # Environment providers precede User Secrets in ASP.NET. Fail visibly instead of silently
    # replacing either source; do not change persistent user/machine environment variables.
    foreach ($key in $configurationKeys) {
        foreach ($name in @($key,$key.Replace(':','__'))) {
            if ($null -ne [Environment]::GetEnvironmentVariable($name)) { throw ('User Secrets: variable de entorno tiene precedencia: ' + $name + '. Quitala del proceso y reinicia Visual Studio o la terminal.') }
        }
    }
}
function Read-State {
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Primero ejecuta Local-Tdv2.ps1 Prepare.' }
    $script:state = Get-Content -Raw -Encoding UTF8 $manifestPath | ConvertFrom-Json
    if ($state.kind -ne 'tdv2-local-validation-v1' -or $state.workspace -ne $workspace -or $state.data -ne $dataPath -or $state.database -ne $databaseName -or $state.port -le 1024 -or $state.port -eq 5432) {
        throw 'El marcador local no corresponde a este destino. No se modifico ninguna base.'
    }
    $resolved = [IO.Path]::GetFullPath($state.data)
    if ($resolved -ne $dataPath -or -not $resolved.StartsWith($localRoot + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Directorio de datos fuera del entorno local.' }
    $script:PostgresBin = $state.postgresBin
    if (-not (Test-Path -LiteralPath (Join-Path $PostgresBin 'pg_ctl.exe'))) { throw 'No se encuentran los binarios PostgreSQL registrados.' }
    try { $script:credentials = Import-Clixml -LiteralPath $credentialsPath }
    catch { throw 'No se pudieron leer las credenciales locales. Usa la misma cuenta Windows que preparo el entorno.' }
    # Decrypt only to validate DPAPI ownership; never print values.
    try { $null = Plain $credentials.Admin; $null = Plain $credentials.App }
    catch { throw 'Las credenciales DPAPI pertenecen a otra cuenta o maquina.' }
}
function Pg-Running {
    $ErrorActionPreference = 'Continue' # pg_ctl reports an ordinary stopped state on stderr.
    & (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataPath status *> $null
    return ($LASTEXITCODE -eq 0)
}
function Start-Database {
    $ErrorActionPreference = 'Continue'
    if (Pg-Running) { return }
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,[int]$state.port)
    try { $listener.Start() } catch { throw 'El puerto PostgreSQL local esta ocupado. No se detuvo ningun proceso existente.' }
    finally { $listener.Stop() }
    # Do not capture the starter's streams: on Windows the server can inherit a pipe and
    # keep PowerShell waiting after pg_ctl has exited. The server itself logs to -l.
    & (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataPath -l (Join-Path $localRoot 'postgres.log') -o "-h 127.0.0.1 -p $($state.port)" -w start
    if ($LASTEXITCODE -ne 0) { throw 'No arranco el cluster local. Revisa el log privado de este entorno.' }
}
function Stop-Database {
    $ErrorActionPreference = 'Continue'
    if (Pg-Running) {
        & (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataPath -m fast -w stop
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo detener el cluster local registrado.' }
    }
}
function Sql([string]$statement,[string]$database=$databaseName,[switch]$AsApp) {
    $ErrorActionPreference = 'Continue'
    $oldPassword = $env:PGPASSWORD
    $oldOptions = $env:PGOPTIONS
    $oldEncoding = $env:PGCLIENTENCODING
    $oldTimeout = $env:PGCONNECT_TIMEOUT
    try {
        $env:PGPASSWORD = if ($AsApp) { Plain $credentials.App } else { Plain $credentials.Admin }
        $env:PGOPTIONS = '-c client_min_messages=warning'
        $env:PGCLIENTENCODING = 'UTF8'
        $env:PGCONNECT_TIMEOUT = '5'
        $account = if ($AsApp) { $appName } else { $adminName }
        # Input through stdin, no passwords/SQL in process arguments; never echo provider errors.
        $result = @($statement | & (Join-Path $PostgresBin 'psql.exe') -X -h 127.0.0.1 -p $state.port -U $account -d $database -qAt -v ON_ERROR_STOP=1 2>&1)
        if ($LASTEXITCODE -ne 0) { throw 'SQL local rechazado.' }
        return ($result -join "`n").Trim()
    } catch { throw 'La operacion SQL del entorno aislado fallo; no se imprimen detalles del proveedor.' }
    finally { $env:PGPASSWORD=$oldPassword; $env:PGOPTIONS=$oldOptions; $env:PGCLIENTENCODING=$oldEncoding; $env:PGCONNECT_TIMEOUT=$oldTimeout }
}
function Verify-Cluster([string]$database='postgres') {
    $actual = (Sql 'SHOW data_directory' $database).Replace('/','\').TrimEnd('\')
    if ([IO.Path]::GetFullPath($actual) -ne $dataPath) { throw 'data_directory no corresponde al cluster aislado. Operacion cancelada.' }
    if ((Sql 'SELECT current_database()' $database) -ne $database) { throw 'La base conectada no es la esperada.' }
}
function Disable-Automatic {
    $null = Sql "UPDATE sincronizacion_configuracion SET activa=false,incluir_ilda=false,proxima_en=null WHERE id=1"
}
function Status {
    Write-Output ('URL: ' + $origin)
    Write-Output ('Retorno Microsoft (Web): ' + $origin + '/connect')
    Write-Output ('Salida Microsoft: ' + $origin + '/')
    Write-Output ('Base local: ' + $databaseName + ' @ 127.0.0.1:' + $state.port)
    $running = Pg-Running
    Write-Output ('PostgreSQL local activo: ' + $running)
    if ($running) {
        Verify-Cluster $databaseName
        $summary = Sql "SELECT json_build_object('migraciones',(SELECT count(*) FROM tdv2_local_migrations),'usuarios',(SELECT count(*) FROM users),'formatos',(SELECT count(*) FROM formatos_ur),'unidades',(SELECT count(*) FROM unidades_responsables_poa),'automatica',(SELECT activa FROM sincronizacion_configuracion WHERE id=1),'ejecuciones',(SELECT count(*) FROM sincronizacion_ejecuciones))::text"
        Write-Output ('Estado local (solo contadores): ' + $summary)
    }
    Write-Output ('User Secrets: ' + $secretsPath)
    Write-Output ('Archivo disponible para esta cuenta: ' + (Test-Path -LiteralPath $secretsPath -PathType Leaf))
    Write-Output 'ASP.NET Development carga User Secrets. El lanzador no lee ni inyecta credenciales institucionales DPAPI.'
    Write-Output 'Start/StartDatabase pausan automatica en la base aislada; no se inicia procesador. Conexiones institucionales no validadas.'
}
try {
    if ($Action -eq 'Prepare') {
        if (-not (Test-Path -LiteralPath $manifestPath)) {
            if (Test-Path -LiteralPath $localRoot) { throw 'El directorio local existe sin marcador. Revisalo antes de preparar; no se borro ni reutilizo.' }
            foreach ($binary in @('pg_ctl.exe','psql.exe','initdb.exe')) {
                if (-not (Test-Path -LiteralPath (Join-Path $PostgresBin $binary))) { throw 'Faltan los binarios nativos PostgreSQL. Indica -PostgresBin.' }
            }
            $null = New-Item -ItemType Directory -Path $localRoot
            # Private directory, inherited by credentials, logs and PostgreSQL data; no ACL changes elsewhere.
            $acl = Get-Acl -LiteralPath $localRoot
            $acl.SetAccessRuleProtection($true,$false)
            $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
            $rule = [Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow')
            $acl.AddAccessRule($rule)
            Set-Acl -LiteralPath $localRoot -AclObject $acl
            $adminPassword = New-Password
            $appPassword = New-Password
            [PSCustomObject]@{ Admin=(ConvertTo-SecureString $adminPassword -AsPlainText -Force); App=(ConvertTo-SecureString $appPassword -AsPlainText -Force) } | Export-Clixml -LiteralPath $credentialsPath
            $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
            $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
            [PSCustomObject]@{ kind='tdv2-local-validation-v1'; workspace=$workspace; data=$dataPath; database=$databaseName; port=$port; postgresBin=[IO.Path]::GetFullPath($PostgresBin) } | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath $manifestPath
            $passwordFile = Join-Path $localRoot 'initdb-password'
            try {
                [IO.File]::WriteAllText($passwordFile,$adminPassword,[Text.UTF8Encoding]::new($false))
                & (Join-Path $PostgresBin 'initdb.exe') -D $dataPath -U $adminName --auth-host=scram-sha-256 --auth-local=scram-sha-256 --encoding=UTF8 --locale=C --pwfile=$passwordFile *> $null
                if ($LASTEXITCODE -ne 0) { throw 'initdb no completo la creacion del cluster aislado.' }
            } finally { [IO.File]::WriteAllText($passwordFile,'',[Text.UTF8Encoding]::new($false)); $adminPassword=$null; $appPassword=$null }
        }
        Read-State
        Lock-Control
        try {
            Write-Output 'Iniciando PostgreSQL aislado...'
            Start-Database
            Write-Output 'Comprobando data_directory antes de aplicar SQL...'
            Verify-Cluster
            if ((Sql "SELECT count(*) FROM pg_database WHERE datname='$databaseName'" 'postgres') -eq '0') {
                $null = Sql "CREATE DATABASE $databaseName" 'postgres'
            }
            Verify-Cluster $databaseName
            Write-Output 'Base aislada confirmada. Revisando migraciones...'
            $null = Sql 'CREATE TABLE IF NOT EXISTS tdv2_local_migrations(name text PRIMARY KEY,sha256 text NOT NULL,applied_at timestamptz NOT NULL DEFAULT now())'
            foreach ($name in @('001_core.sql','002_aspnet_sessions.sql','003_access_contexts.sql','004_synchronizations.sql')) {
                $path = Join-Path $workspace ('database\' + $name)
                $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
                $applied = Sql "SELECT sha256 FROM tdv2_local_migrations WHERE name='$name'"
                if ($applied -eq $hash) { continue }
                if ($applied) { throw ('La migracion aplicada cambio: ' + $name + '. No se sobrescribio el esquema local.') }
                $ddl = [IO.File]::ReadAllText($path)
                if ([regex]::Matches($ddl,'COMMIT;').Count -ne 1) { throw 'Formato de migracion no admitido.' }
                $ddl = $ddl.Replace('COMMIT;',"INSERT INTO tdv2_local_migrations(name,sha256) VALUES('$name','$hash');`nCOMMIT;")
                $null = Sql $ddl
                Write-Output ('Aplicada solo en la base local: ' + $name)
            }
            if ((Sql "SELECT count(*) FROM pg_roles WHERE rolname='$appName'") -eq '0') {
                $password = Plain $credentials.App
                try { $null = Sql "CREATE ROLE $appName LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD '$password'" }
                finally { $password=$null }
            }
            $null = Sql @"
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE CONNECT ON DATABASE $databaseName FROM PUBLIC;
GRANT CONNECT ON DATABASE $databaseName TO $appName;
GRANT USAGE ON SCHEMA public TO $appName;
GRANT SELECT,INSERT,UPDATE,DELETE ON users,ms_graph_tokens,activity_logs,tdv2_sessions,tdv2_oauth_attempts,formatos_ur,tdv2_access_contexts,colaboraciones_ur,sincronizaciones_institucionales,sincronizacion_catalogos,ilda_informacion_area,sincronizacion_ejecuciones,sincronizacion_configuracion TO $appName;
GRANT SELECT,INSERT,UPDATE ON unidades_responsables_poa TO $appName;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO $appName;
"@
            Disable-Automatic
            if ((Sql "SELECT count(*) FROM sincronizacion_configuracion WHERE id=1 AND activa=false AND incluir_ilda=false" -AsApp) -ne '1') { throw 'No se confirmo la configuracion pausada con la cuenta de la aplicacion.' }
            Write-Output 'Migraciones verificadas. La preparacion no inserta usuarios, permisos Nexo ni catalogos ficticios; no carga proveedores simulados.'
        } finally { Stop-Database }
        Push-Location $workspace
        try {
            & dotnet build tdv2/tdv2.csproj -p:NuGetAudit=false --nologo
            if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion ASP.NET.' }
            $npm = (Get-Command npm.cmd -ErrorAction SilentlyContinue).Source
            if (-not $npm) { $npm = Join-Path $env:ProgramFiles 'nodejs\npm.cmd' }
            if (-not (Test-Path -LiteralPath $npm)) { throw 'Falta Node/npm. No se instalo ningun programa.' }
            Push-Location (Join-Path $workspace 'ClientApp')
            try {
                if (-not (Test-Path -LiteralPath 'node_modules\.bin\vite.cmd')) {
                    & $npm ci --ignore-scripts
                    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar las dependencias React.' }
                }
                & $npm run build
                if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion React.' }
            } finally { Pop-Location }
        } finally { Pop-Location }
        Status
    } elseif ($Action -in @('Import','Configure','MigrateSecrets')) {
        Write-Output 'Configuracion administrada desde Visual Studio: Administrar secretos de usuario.'
        Write-Output ('Archivo: ' + $secretsPath)
        Write-Output 'Accion anterior retirada: no se leyo Herd ni se modifico DPAPI o User Secrets.'
    } elseif ($Action -eq 'StartDatabase') {
        Read-State
        Lock-Control
        Require-UserSecrets
        Check-EnvironmentOverrides
        Start-Database
        Verify-Cluster $databaseName
        Disable-Automatic
        Status
        Write-Output 'Base local lista para Visual Studio. Esta terminal puede cerrarse; inicia tdv2 con el perfil https y F5.'
    } elseif ($Action -eq 'Start') {
        Read-State
        Lock-Control
        Require-UserSecrets
        Check-EnvironmentOverrides
        if (-not (Test-Path -LiteralPath (Join-Path $workspace 'tdv2\wwwroot\index.html')) -or -not (Test-Path -LiteralPath (Join-Path $workspace 'tdv2\bin\Debug\net10.0\tdv2.dll'))) { throw 'Primero ejecuta Prepare para compilar ASP.NET y React.' }
        & dotnet dev-certs https --check --trust --quiet
        if ($LASTEXITCODE -ne 0) { throw 'Falta certificado HTTPS confiable para esta cuenta. Ejecuta personalmente: dotnet dev-certs https --trust' }
        foreach ($url in $urls) {
            $probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$url.Port)
            try { $probe.Start() } catch { throw ('Puerto del perfil ocupado: ' + $url.Port + '. No se detuvo ningun proceso.') }
            finally { $probe.Stop() }
        }
        try {
            Start-Database
            Verify-Cluster $databaseName
            Disable-Automatic
            Status
            Write-Output 'Deja esta terminal abierta. Para detener: Ctrl+C y luego Local-Tdv2.ps1 Stop. Solo se administra este PostgreSQL local.'
            Push-Location $workspace
            try { & dotnet run --project tdv2 --no-build --no-restore --launch-profile https-compiled }
            finally { Pop-Location }
        } finally {
            Stop-Database
        }
    } elseif ($Action -eq 'Stop') {
        Read-State
        Lock-Control
        Stop-Database
        Write-Output 'PostgreSQL local detenido. Los datos permanecen. Para ASP.NET usa Ctrl+C en su terminal.'
    } else { Read-State; Status }
} catch {
    # Deliberately exclude exception objects, provider responses and configuration values.
    Write-Output ('Operacion ' + $Action + ' no completada. Tipo: ' + $_.Exception.GetType().Name)
    if ($_.Exception.Message -match '^(User Secrets:|Primero ejecuta|Ya hay un arranque|El marcador local|Directorio de datos fuera|No se encuentran los binarios|No se pudieron leer las credenciales|Las credenciales DPAPI|El puerto PostgreSQL|No arranco el cluster|No se pudo detener|La operacion SQL del entorno|data_directory no corresponde|La base conectada|El directorio local existe|Faltan los binarios|initdb no completo|La migracion aplicada cambio|Formato de migracion|No se confirmo|Fallo la compilacion|Falta Node/npm|No se pudieron restaurar|Falta certificado HTTPS|Puerto del perfil ocupado)') { Write-Output $_.Exception.Message }
    exit 1
} finally { if ($controlLock) { $controlLock.Dispose() } }
