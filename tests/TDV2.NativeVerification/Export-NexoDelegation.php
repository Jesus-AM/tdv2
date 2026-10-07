<?php
// Genera el SQL real sin arrancar Laravel, cargar .env, resolver credenciales ni abrir conexiones.
function config(string $key, mixed $default = null): mixed { return $default; }
function app(string $class): object { return new $class; }
$root = $argv[1];
foreach (['PostgresPublication', 'PostgresUserUnits', 'CuentasInstitucionales', 'DelegacionPostgres', 'RepresentacionPostgres'] as $name) {
    require $root.'/app/Services/'.$name.'.php';
}
$delegation = new App\Services\DelegacionPostgres;
$representation = new App\Services\RepresentacionPostgres;
file_put_contents($argv[2], json_encode([
    'grants' => (new App\Services\PostgresPublication)->views(47, 1)['concesiones'],
    'views' => $delegation->vistasCompartidas(),
    'functions' => $delegation->procedimientos(47),
    'representation' => array_intersect_key($representation->procedimientos(47), array_flip(['representar_conceder', 'representar_retirar'])),
], JSON_THROW_ON_ERROR));
