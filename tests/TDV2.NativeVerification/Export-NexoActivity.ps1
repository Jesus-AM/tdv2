param([Parameter(Mandatory)][string]$NexoSource,[Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference='Stop'
# Extracción textual sin PHP/Laravel/configuración: las sustituciones son las de procedure(87).
$publication=[IO.File]::ReadAllText((Join-Path $NexoSource 'app/Services/PostgresPublication.php'))
$template=[IO.File]::ReadAllText((Join-Path $NexoSource 'app/Services/Postgres/registrar_actividad.sql'))
$method=[regex]::Match($publication,'(?s)public static function effective\(.*?(?=public static function gate)').Value
$body=[regex]::Match($method,'(?s)return "(\(NOT EXISTS.*?)";').Groups[1].Value
if (!$body -or !$method.Contains('eg.rol_id = {$role}') -or !$publication.Contains("self::effective('ua.id', 'r.id')")) {throw 'Contrato de publicación cambió; revisar antes de generar fixture.'}
$effective=$body.Replace('{$membership}','ua.id').Replace('{$roleFilter}','eg.rol_id = r.id')
if ($effective.Contains('$')) {throw 'Sustitución SQL incompleta.'}
$sql='CREATE FUNCTION public.nexo_a87_registrar_actividad'+$template.Replace('{{APP_ID}}','87').Replace('{{ROLE_EFFECTIVE}}',$effective)+';'
[IO.File]::WriteAllText($Destination,$sql,[Text.UTF8Encoding]::new($false))
