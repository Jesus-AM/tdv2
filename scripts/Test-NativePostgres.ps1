param([string]$PostgresBin = 'C:\Program Files\PostgreSQL\18\bin', [switch]$SkipBrowser, [switch]$BrowserOnly, [switch]$SyncOnly, [switch]$TransitionOnly, [switch]$MigrationsOnly, [switch]$EditingOnly,
    [switch]$NexoDelegationOnly, [string]$NexoSource = 'C:\Users\Jesus Arenas\Herd\nexo',
    [string]$Php = 'C:\Users\Jesus Arenas\.config\herd\bin\php84\php.exe')
$ErrorActionPreference = 'Stop'
if ($NexoDelegationOnly -and ($SkipBrowser -or $BrowserOnly -or $SyncOnly -or $TransitionOnly -or $MigrationsOnly)) { throw 'NexoDelegationOnly es una selección independiente.' }
if ($MigrationsOnly -and ($SkipBrowser -or $BrowserOnly -or $SyncOnly -or $TransitionOnly)) { throw 'MigrationsOnly es una selección independiente.' }
if ($TransitionOnly -and ($SkipBrowser -or $BrowserOnly -or $SyncOnly)) { throw 'TransitionOnly es una selección independiente.' }
$oldBin=$env:TDV2_TEST_POSTGRES_BIN
$env:TDV2_TEST_POSTGRES_BIN=$PostgresBin
if ($SkipBrowser -and $BrowserOnly) { throw 'SkipBrowser and BrowserOnly are mutually exclusive.' }
if ($SyncOnly -and ($SkipBrowser -or $BrowserOnly)) { throw 'SyncOnly is a separate diagnostic selection.' }
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifacts = [IO.Path]::GetFullPath((Join-Path $workspace '.artifacts'))
$run = Join-Path $artifacts ('native-postgres-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
$data = Join-Path $run 'data'
if (-not ([IO.Path]::GetFullPath($data).StartsWith($artifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase))) { throw 'Unsafe test path.' }
New-Item -ItemType Directory -Path $run | Out-Null
$random = [byte[]]::new(32)
$generator = [Security.Cryptography.RandomNumberGenerator]::Create()
$generator.GetBytes($random)
$password = [Convert]::ToBase64String($random)
$passwordFile = Join-Path $run 'initdb-password'
[IO.File]::WriteAllText($passwordFile, $password, [Text.UTF8Encoding]::new($false))
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$started = $false
$oldPassword = $env:PGPASSWORD
$oldConnection = $env:TDV2_TEST_CONNECTION
$oldData = $env:TDV2_TEST_DATA_DIRECTORY
$oldArtifacts = $env:TDV2_TEST_ARTIFACTS
$oldSkip = $env:TDV2_TEST_SKIP_BROWSER
try {
    & (Join-Path $PostgresBin 'initdb.exe') -D $data -U tdv2_test_admin --auth-host=scram-sha-256 --auth-local=scram-sha-256 --encoding=UTF8 --locale=C --pwfile=$passwordFile | Out-File (Join-Path $run 'initdb.log') -Encoding utf8
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $data 'PG_VERSION'))) { throw 'initdb failed; see isolated run log.' }
    & (Join-Path $PostgresBin 'pg_ctl.exe') -D $data -l (Join-Path $run 'postgres.log') -o "-h 127.0.0.1 -p $port" -w start
    if ($LASTEXITCODE -ne 0) { throw 'Isolated PostgreSQL startup failed.' }
    $started = $true
    $env:PGPASSWORD = $password
    & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p $port -U tdv2_test_admin tdv2_native_test
    if ($LASTEXITCODE -ne 0) { throw 'Isolated database creation failed.' }
    $env:TDV2_TEST_CONNECTION = "Host=127.0.0.1;Port=$port;Database=tdv2_native_test;Username=tdv2_test_admin;Password=$password;Pooling=false;Timeout=5;Command Timeout=15"
    $env:TDV2_TEST_DATA_DIRECTORY = $data
    $env:TDV2_TEST_ARTIFACTS = $run
    $env:TDV2_TEST_SKIP_BROWSER = if ($SkipBrowser -or $SyncOnly) { 'true' } else { 'false' }
    Push-Location $workspace
    try {
        if ($NexoDelegationOnly) {
            & $Php -n (Join-Path $workspace 'tests/TDV2.NativeVerification/Export-NexoDelegation.php') $NexoSource (Join-Path $run 'nexo-generated.json')
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo generar el contrato desde el código fuente de Nexo.' }
        }
        $testArguments = if ($EditingOnly) { @('--', '--editing-only') } elseif ($NexoDelegationOnly) { @('--', '--nexo-delegation-only') } elseif ($MigrationsOnly) { @('--', '--migrations-only') } elseif ($TransitionOnly) { @('--', '--transition-only') } elseif ($BrowserOnly) { @('--', '--browser-only') } elseif ($SyncOnly) { @('--', '--sync-only') } else { @() }
        # Usar Release sin apphost evita disputar el ejecutable Debug abierto por F5 en Visual Studio.
        & dotnet run --project tests/TDV2.NativeVerification -c Release -p:UseAppHost=false -p:NuGetAudit=false @testArguments
        $result = $LASTEXITCODE
        if ($result -ne 0) { throw 'Native verification failed. See results in the isolated artifacts directory.' }
    } finally { Pop-Location }
} finally {
    if ($started) {
        # pg_ctl targets ONLY this script's verified, newly created cluster; no Windows service is stopped.
        & (Join-Path $PostgresBin 'pg_ctl.exe') -D $data -m fast -w stop
        if ($LASTEXITCODE -ne 0) { Write-Warning ('Stop failed for this isolated cluster: ' + $data) }
    }
    $env:PGPASSWORD = $oldPassword
    $env:TDV2_TEST_POSTGRES_BIN=$oldBin
    $env:TDV2_TEST_CONNECTION = $oldConnection
    $env:TDV2_TEST_DATA_DIRECTORY = $oldData
    $env:TDV2_TEST_ARTIFACTS = $oldArtifacts
    $env:TDV2_TEST_SKIP_BROWSER = $oldSkip
    [IO.File]::WriteAllText($passwordFile, '', [Text.UTF8Encoding]::new($false))
    $generator.Dispose()
    Write-Output ('Isolated artifacts: ' + $run)
}
