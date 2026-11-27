-- Synthetic publisher, NOT the Nexo Laravel service. Actual PostgreSQL views, separate read-only account.
CREATE TABLE fixture_app(id bigint, clave text, nombre text, nivel_control int);
CREATE TABLE fixture_users(usuario_aplicacion_id bigint,usuario_id bigint,email text,nombre text,tipo_cuenta text,
    num_empleado text,"ID_UR" text,origen_ur text,"CVE_UR" text,"ID_UR_SUPERIOR" text);
CREATE TABLE fixture_suspensions(usuario_aplicacion_id bigint PRIMARY KEY);
CREATE TABLE fixture_roles(usuario_aplicacion_id bigint,email text,rol_id bigint,rol_clave text,rol_nombre text);
CREATE TABLE fixture_modules(id bigint,aplicacion_id bigint,clave text,nombre text,ruta text,icono text,orden int,modulo_padre_id bigint);
CREATE TABLE fixture_module_roles(rol_id bigint,modulo_id bigint);
CREATE TABLE fixture_grants(concesion_id bigint,email text,rol_id bigint,id_ur_acceso text,origen text);
CREATE VIEW nexo_aplicacion AS SELECT * FROM fixture_app;
CREATE VIEW fixture_effective_users AS SELECT u.* FROM fixture_users u
    WHERE NOT EXISTS (SELECT FROM fixture_suspensions s WHERE s.usuario_aplicacion_id=u.usuario_aplicacion_id);
CREATE VIEW nexo_usuarios AS SELECT * FROM fixture_effective_users;
CREATE VIEW nexo_usuario_rol AS SELECT r.* FROM fixture_roles r
    WHERE NOT EXISTS (SELECT FROM fixture_suspensions s WHERE s.usuario_aplicacion_id=r.usuario_aplicacion_id);
CREATE VIEW nexo_modulos AS SELECT * FROM fixture_modules;
CREATE VIEW nexo_modulo_rol AS SELECT * FROM fixture_module_roles;
CREATE VIEW nexo_concesiones AS SELECT * FROM fixture_grants;
