param([switch]$Compiled)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifacts=Join-Path $workspace '.artifacts/development'
$null=New-Item -ItemType Directory -Force -Path $artifacts
$backend=$null; $vite=$null
$node=Join-Path $env:ProgramFiles 'nodejs/node.exe'
$profile=if($Compiled) {'https-compiled'} else {'https'}
try {
    foreach($port in @(7136,5064,5173)) {
        $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$port)
        try { $listener.Start() } catch { throw 'Deten las instancias previas; no se detiene ningun proceso ajeno.' }
        finally { $listener.Stop() }
    }
    # El script existente sólo inicia el clúster registrado y pausa programación; no recrea la base.
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Local-Tdv2.ps1') StartDatabase
    if($LASTEXITCODE -ne 0) { throw 'No se preparo la base local.' }
    if(-not $Compiled) {
        $vite=Start-Process -FilePath $node -ArgumentList @('node_modules/vite/bin/vite.js','--host','127.0.0.1','--port','5173','--strictPort') -WorkingDirectory (Join-Path $workspace 'ClientApp') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifacts 'vite.out') -RedirectStandardError (Join-Path $artifacts 'vite.err')
    }
    $backend=Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('run','--project','tdv2/tdv2.csproj','--no-build','--no-restore','--launch-profile',$profile) -WorkingDirectory $workspace -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifacts 'backend.out') -RedirectStandardError (Join-Path $artifacts 'backend.err')
    $ready=$false
    for($attempt=0;$attempt -lt 60;$attempt++) {
        try { $ready=(Invoke-WebRequest -UseBasicParsing 'http://localhost:5064/health/live').StatusCode -eq 200 } catch { }
        if($ready) { break }
        if($backend.HasExited) { throw 'El backend termino antes de responder.' }
        Start-Sleep -Seconds 1
    }
    if(-not $ready) { throw 'El backend no respondio en el plazo.' }
    Push-Location (Join-Path $workspace 'ClientApp')
    try {
        & $node tests/browser/local-startup.mjs
        if($LASTEXITCODE -ne 0) { throw 'Fallo el arranque HTTPS.' }
        if(-not $Compiled) {
            & $node tests/browser/development-flow.mjs
            if($LASTEXITCODE -ne 0) { throw 'Fallo Vite/HMR.' }
            & taskkill /PID $vite.Id /T /F *> $null
            foreach($path in @('/acceso-restringido','/formatos/ajena')) {
                $status=0
                try { $null=Invoke-WebRequest -UseBasicParsing ('http://localhost:5064'+$path) -Headers @{Accept='text/html'} }
                catch { if($_.Exception.Response) { $status=[int]$_.Exception.Response.StatusCode } }
                if($status -ne 503) { throw 'Vite detenido no devolvio un error publico controlado.' }
            }
            Write-Output 'PASS: Vite detenido devuelve 503 controlado incluso al mostrar un rechazo de acceso.'
        }
    } finally { Pop-Location }
} finally {
    # Sólo los árboles de procesos que este script inició; PostgreSQL queda disponible para F5.
    foreach($process in @($backend,$vite)) {
        if($process -and -not $process.HasExited) { & taskkill /PID $process.Id /T /F *> $null }
    }
}
