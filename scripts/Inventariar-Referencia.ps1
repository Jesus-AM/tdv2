param([string]$Reference = 'C:\Users\Jesus Arenas\Herd\tdv2')
$ErrorActionPreference = 'Stop'
$target = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../docs/migracion'))
New-Item -ItemType Directory -Force -Path $target | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
$files = @()
foreach ($folder in @('routes','app','config','database/migrations','database/plantillas','tests','resources/js','resources/css','resources/views')) {
    $files += Get-ChildItem -LiteralPath (Join-Path $Reference $folder) -File -Recurse |
        Where-Object { $_.Extension -in '.php','.tsx','.ts','.css','.json','.mjs' }
}
$manifest = foreach ($f in ($files | Sort-Object FullName)) {
    [ordered]@{ path = $f.FullName.Substring($Reference.Length + 1).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
}
[IO.File]::WriteAllText((Join-Path $target 'fuente-manifiesto.json'), (ConvertTo-Json -Depth 5 -InputObject @($manifest)), $utf8)
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# Evidencia estatica del codigo Laravel')
$lines.Add('')
$lines.Add('Generado por scripts/Inventariar-Referencia.ps1. Lectura de codigo solamente; no prueba el esquema desplegado ni ejecuta pruebas. Los hashes identifican la version inspeccionada.')
foreach ($category in @('routes','database/migrations','tests')) {
    $lines.Add(''); $lines.Add('## ' + $category)
    foreach ($f in $files | Where-Object { $_.FullName.StartsWith((Join-Path $Reference $category)) } | Sort-Object FullName) {
        $name = $f.FullName.Substring($Reference.Length + 1).Replace('\','/')
        $content = [IO.File]::ReadAllLines($f.FullName)
        $selected = for ($i = 0; $i -lt $content.Length; $i++) {
            if (($category -eq 'routes' -and $content[$i] -match 'Route::|Schedule::') -or
                ($category -eq 'database/migrations' -and $content[$i] -match 'Schema::|\$(table|t)->') -or
                ($category -eq 'tests' -and $content[$i] -match 'function test_|^test\(')) {
                '{0}: {1}' -f ($i + 1), $content[$i].Trim()
            }
        }
        if ($selected) { $lines.Add(''); $lines.Add('### ' + $name); $lines.Add(''); $lines.Add('```text'); $lines.AddRange([string[]]$selected); $lines.Add('```') }
    }
}
[IO.File]::WriteAllLines((Join-Path $target 'EVIDENCIA_FUENTE.md'), $lines, $utf8)
Write-Output ('Inventario escrito. Archivos de código identificados: ' + $manifest.Count)
