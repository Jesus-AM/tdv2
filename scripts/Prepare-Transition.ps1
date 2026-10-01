param(
    [Parameter(Mandatory=$true)][ValidatePattern('^tdv2_transition_[a-z0-9_]+$')][string]$Database,
    [string]$BackupPath,
    [switch]$Apply
)
# Sólo una copia en el clúster aislado registrado. Nunca lee .env ni conecta tdv2_db.
$ErrorActionPreference='Stop'
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
    $parts=@('database/transition/preflight.sql','database/transition/010_laravel_copy.sql','database/002_aspnet_sessions.sql','database/003_access_contexts.sql')
    $sql="BEGIN;`nSET LOCAL lock_timeout='10s';`nSET LOCAL statement_timeout='5min';`nSELECT set_config('tdv2.transition_target','$Database',true);`n"
    # Bloquear sólo la copia evita comparar una instantánea mientras otro proceso la modifica.
    $protected=@('users','activity_logs','unidades_responsables_poa','formatos_ur','colaboraciones_ur','sincronizaciones_institucionales','sincronizacion_catalogos','ilda_informacion_area','sincronizacion_ejecuciones')
    $sql+='LOCK TABLE '+($protected -join ',')+",sincronizacion_configuracion,ms_graph_tokens IN ACCESS EXCLUSIVE MODE;`n"
    $sql+=[IO.File]::ReadAllText((Join-Path $workspace $parts[0]))+"`n"
    if ($Apply) {
        $sql+=[IO.File]::ReadAllText((Join-Path $workspace 'database/transition/preserve.sql'))+"`n"
        foreach ($file in $parts | Select-Object -Skip 1) {
            $body=[IO.File]::ReadAllText((Join-Path $workspace $file))
            $sql+=[regex]::Replace($body,'(?m)^\s*(BEGIN|COMMIT);\s*$','')+"`n"
        }
        foreach ($file in @($parts + @('database/transition/preserve.sql','database/transition/verify-preservation.sql'))) {
            $hash=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $workspace $file)).Hash
            $sql+="INSERT INTO tdv2_transition_migrations(name,sha256) VALUES('$file','$hash');`n"
        }
        $sql+=[IO.File]::ReadAllText((Join-Path $workspace 'database/transition/verify-preservation.sql'))+"`n"
        $sql+="COMMIT;`n"
    } else { $sql+="ROLLBACK;`n" }
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
