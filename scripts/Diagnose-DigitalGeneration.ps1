param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $workspace
try {
    if (-not $NoBuild) {
        & dotnet build tests/TDV2.NativeVerification/TDV2.NativeVerification.csproj -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    # Sólo SELECT de cd_control con default_transaction_read_only=on.
    # No inicia servidor, fixtures, auditoría, sincronización ni migraciones.
    & dotnet tests/TDV2.NativeVerification/bin/Release/net10.0/TDV2.NativeVerification.dll --digital-generation-readonly
    exit $LASTEXITCODE
} finally { Pop-Location }
