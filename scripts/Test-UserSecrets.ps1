# Reads the authorized local configuration without printing values. Temporarily edits only
# the client ID to a synthetic GUID, exercises retired commands, then restores exact bytes.
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $workspace 'tests\TDV2.LocalConfigurationVerification\TDV2.LocalConfigurationVerification.csproj'
$xml = [xml][IO.File]::ReadAllText((Join-Path $workspace 'tdv2\tdv2.csproj'))
$id = [string]$xml.Project.PropertyGroup.UserSecretsId
$path = Join-Path ([Environment]::GetFolderPath('ApplicationData')) ('Microsoft\UserSecrets\' + $id + '\secrets.json')
$original = $null; $editedHash = $null; $originalHash = $null
function Check-Config([string]$probe) {
    if ($probe) { & dotnet run --project $project --no-build --no-restore -- $workspace $probe }
    else { & dotnet run --project $project --no-build --no-restore -- $workspace }
    if ($LASTEXITCODE -ne 0) { throw 'No se verifico el proveedor de configuracion.' }
}
try {
    foreach ($port in @(7136,5064)) {
        $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$port)
        try { $listener.Start() } catch { throw 'Deten ASP.NET antes de verificar ediciones de User Secrets.' }
        finally { $listener.Stop() }
    }
    Check-Config
    $original = [IO.File]::ReadAllBytes($path)
    $originalHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $document = [IO.File]::ReadAllText($path) | ConvertFrom-Json
    if (-not $document.PSObject.Properties['Microsoft:ClientId']) { throw 'La prueba de edicion requiere el formato plano inicial; no se modifico el archivo.' }
    $probe = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee'
    $document.'Microsoft:ClientId' = $probe
    [IO.File]::WriteAllText($path,($document | ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false))
    $editedHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    try {
        foreach ($action in @('MigrateSecrets','Import','Configure')) {
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Local-Tdv2.ps1') $action *> $null
            if ($LASTEXITCODE -ne 0 -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $editedHash) { throw 'Una accion anterior altero User Secrets.' }
        }
        Check-Config $probe
    } finally {
        # Do not overwrite concurrent edits made by the user during this short check.
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ceq $editedHash) { [IO.File]::WriteAllBytes($path,$original) }
        else { throw 'El archivo cambio concurrentemente: se conserva para revision personal.' }
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -cne $originalHash) { throw 'No se verifico la restauracion exacta.' }
    Check-Config
    $report = [ordered]@{
        utc=[DateTime]::UtcNow.ToString('o'); migrationRepeatedWithoutOverwrite=$true
        retiredImportAndConfigureWithoutWrites=$true; editedClientIdObserved=$true; originalFileRestored=$true
        institutionalConnectionsAttempted=$false
        configuration=(Get-Content -Raw -Encoding UTF8 (Join-Path $workspace '.artifacts\local-validation\user-secrets-verification.json') | ConvertFrom-Json)
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $workspace 'docs\migracion\evidencia-user-secrets.json')
    Write-Output 'PASS: ediciones conservadas ante MigrateSecrets/Import/Configure. Archivo original restaurado sin cambios.'
} catch {
    Write-Output ('FAIL: verificacion User Secrets. Tipo: ' + $_.Exception.GetType().Name + '; valores omitidos.')
    exit 1
} finally { $original=$null; $document=$null }
