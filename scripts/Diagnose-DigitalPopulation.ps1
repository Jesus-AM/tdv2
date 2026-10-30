param([switch]$NoBuild, [switch]$PrivateEmployeeConflicts)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $workspace
try {
    if (-not $NoBuild) {
        & dotnet build tests/TDV2.NativeVerification/TDV2.NativeVerification.csproj -c Release --no-restore -p:UseAppHost=false -p:NuGetAudit=false
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    # Esta entrada sale antes de inicializar fixtures, servidor, auditoría o trabajadores.
    $diagnosticArguments = @('--digital-population-readonly')
    if ($PrivateEmployeeConflicts) { $diagnosticArguments += '--private-employee-conflicts' }
    & dotnet tests/TDV2.NativeVerification/bin/Release/net10.0/TDV2.NativeVerification.dll @diagnosticArguments
    exit $LASTEXITCODE
} finally { Pop-Location }
