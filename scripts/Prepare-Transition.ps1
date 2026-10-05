param(
    [Parameter(Mandatory=$true)][ValidatePattern('^tdv2_transition_[a-z0-9_]+$')][string]$Database,
    [string]$BackupPath,
    [switch]$Apply
)
# Sólo una copia en el clúster aislado registrado. Nunca lee .env ni conecta tdv2_db.
$ErrorActionPreference='Stop'
if ($Apply) { throw 'La conversión SQL anterior está retirada. EF es el único mecanismo de migraciones; una copia Laravel requiere revisar primero su esquema y preparar su adopción EF.' }
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root=Join-Path $workspace '.artifacts/local-validation'
$state=Get-Content -Raw -Encoding UTF8 (Join-Path $root 'environment.json') | ConvertFrom-Json
$expected=[IO.Path]::GetFullPath((Join-Path $root 'data'))
if ($state.kind -ne 'tdv2-local-validation-v1' -or $state.workspace -ne $workspace -or [IO.Path]::GetFullPath($state.data) -ne $expected -or $state.port -le 1024 -or $state.port -eq 5432) { throw 'Marcador de cluster no valido.' }
$oldPassword=$env:PGPASSWORD; $oldOptions=$env:PGOPTIONS; $oldEncoding=$env:PGCLIENTENCODING
$control=$null
function Sql([string]$db,[string]$sql) {
    $result=@($sql | & (Join-Path $state.postgresBin 'psql.exe') -X -h 127.0.0.1 -p $state.port -U tdv2_local_admin -d $db -qAt -v ON_ERROR_STOP=1 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'El contrato o la operacion SQL de la copia no se completo; no se imprimen datos del proveedor.' }
    return ($result -join "`n").Trim()
}
try {
    $control=[IO.File]::Open((Join-Path $root 'control.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $credentials=Import-Clixml -LiteralPath (Join-Path $root 'database.clixml')
    $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($credentials.Admin)
    try { $env:PGPASSWORD=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    $env:PGOPTIONS='-c client_min_messages=warning'; $env:PGCLIENTENCODING='UTF8'
    if ([IO.Path]::GetFullPath((Sql 'postgres' 'SHOW data_directory')) -ne $expected) { throw 'El servidor no es el cluster aislado.' }
    if ($BackupPath) {
        # No sobreescribir ni reutilizar una base existente. Un fallo conserva la copia para diagnóstico privado.
        if ((Sql 'postgres' "SELECT count(*) FROM pg_database WHERE datname='$Database'") -ne '0') { throw 'El destino ya existe. Selecciona un nombre nuevo; no se borro nada.' }
        $backup=[IO.Path]::GetFullPath($BackupPath)
        if (-not $backup.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $backup -PathType Leaf)) { throw 'El respaldo autorizado debe estar en un directorio privado del destino.' }
        $null=Sql 'postgres' "CREATE DATABASE $Database TEMPLATE template0"
        & (Join-Path $state.postgresBin 'pg_restore.exe') -h 127.0.0.1 -p $state.port -U tdv2_local_admin -d $Database --no-owner --no-privileges --exit-on-error --single-transaction $backup *> $null
        if ($LASTEXITCODE -ne 0) { throw 'La restauracion fallo. Se conserva el destino sin aplicar la transicion.' }
    }
    if ((Sql $Database 'SELECT current_database()') -ne $Database) { throw 'Base destino incorrecta.' }
    # Diagnóstico del contrato histórico en una transacción de sólo lectura.
    $sql="BEGIN READ ONLY;`nSET LOCAL lock_timeout='10s';`nSET LOCAL statement_timeout='5min';`nSELECT set_config('tdv2.transition_target','$Database',true);`n"
    $sql+=[IO.File]::ReadAllText((Join-Path $workspace 'database/transition/preflight.sql'))+"`nROLLBACK;`n"
    $null=Sql $Database $sql
    $report=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');database=$Database;restored=[bool]$BackupPath;schemaContractPassed=$true;applied=[bool]$Apply;dataPreservationChecked=[bool]$Apply;sourceConnected=$false;institutionalAcceptance=$false}
    $directory=Join-Path $workspace ('.artifacts/transition/'+$Database)
    $null=New-Item -ItemType Directory -Force -Path $directory
    $report | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $directory 'verification.json')
    Write-Output 'Contrato de la copia verificado. No se conecto al origen; la aceptacion de respuestas y servicios institucionales sigue pendiente.'
} catch {
    Write-Output ('Ensayo no completado. Tipo: '+$_.Exception.GetType().Name+'. Se conservan el origen y la copia; detalles privados omitidos.')
    exit 1
} finally {
    $env:PGPASSWORD=$oldPassword; $env:PGOPTIONS=$oldOptions; $env:PGCLIENTENCODING=$oldEncoding
    if($control) { $control.Dispose() }
}
