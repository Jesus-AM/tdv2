-- Base desechable exclusivamente sintética para ejecutar SQL generado por el código real de Nexo.
CREATE TABLE aplicaciones(id bigint PRIMARY KEY, clave text, nombre text, activo bool);
CREATE TABLE cuentas_postgresql(id bigint, aplicacion_id bigint, usuario_postgresql text, tipo text,
 habilitada bool, creada_en_postgresql bool, eliminada_en timestamp, eliminacion_solicitada_en timestamp,
 estado text, revision int, revision_aplicada int);
CREATE TABLE cuenta_postgresql_aplicacion(cuenta_postgresql_id bigint,aplicacion_id bigint,habilitada bool);
CREATE TABLE delegaciones_aplicacion(aplicacion_id bigint,habilitada bool);
CREATE TABLE delegacion_roles(aplicacion_id bigint,rol_id bigint,uso text);
CREATE TABLE usuarios(id bigserial PRIMARY KEY,email text UNIQUE,nombre text,usuario_empleado text,tipo_cuenta text,
 activo bool,created_at timestamp,updated_at timestamp);
CREATE TABLE usuario_aplicacion(id bigserial PRIMARY KEY,usuario_id bigint,aplicacion_id bigint,activo bool,suspendido bool,
 retirado_en timestamp,retirado_por_usuario_id bigint,created_at timestamp,updated_at timestamp);
CREATE TABLE roles(id bigint PRIMARY KEY,aplicacion_id bigint,clave text,nombre text,activo bool);
CREATE TABLE usuario_rol(usuario_aplicacion_id bigint,rol_id bigint);
CREATE TABLE concesiones_acceso(id bigserial PRIMARY KEY,usuario_aplicacion_id bigint,rol_id bigint,id_ur text,origen text,
 otorgado_por_usuario_id bigint,otorgado_en timestamp,revocado_en timestamp,revocado_por_usuario_id bigint,created_at timestamp,updated_at timestamp);
CREATE TABLE unidades_responsables_poa(id_ur text PRIMARY KEY,cve_ur text,desc_ur text,nivel_ur int,id_ur_pertenece text,
 num_empleado text,tipo_ur text,ejercicio int,estatus_ur text);
CREATE TABLE presencia_institucional(catalogo text,clave text,presente bool);
CREATE TABLE usuarios_empleado(usuario text,numero_empleado text,estatus_usuario text);
CREATE TABLE empleados_rhum(num_empleado text,nombre text,apellido_paterno text,apellido_materno text,estatus_empleado text);
CREATE TABLE empleados_nominales_rhum(num_empleado text,id_ur text);
CREATE TABLE sesiones_representacion(id uuid,aplicacion_id bigint,actor_id bigint,representado_id bigint,conexion text,
 escritura bool,finalizada_en timestamp,expira_en timestamp);
CREATE TABLE registro_actividad(evento_uuid uuid,ocurrido_en timestamp,recibido_en timestamp,aplicacion_origen_id bigint,
 aplicacion_origen_nombre text,aplicacion_afectada_id bigint,aplicacion_afectada_nombre text,categoria text,accion text,resultado text,
 actor_tipo text,actor_usuario_id bigint,actor_nombre text,actor_email text,actor_tipo_cuenta text,actor_num_empleado text,
 actor_id_ur text,actor_cve_ur text,actor_desc_ur text,actor_ejercicio int,actor_origen_ur text,actor_roles json,
 usuario_afectado_id bigint,usuario_afectado_nombre text,usuario_afectado_email text,entidad_tipo text,entidad_id text,
 antes json,despues json,contexto json);
CREATE VIEW nexo_usuario_rol AS SELECT u.email,r.id rol_id,r.clave rol_clave,r.nombre rol_nombre FROM usuarios u
 JOIN usuario_aplicacion ua ON ua.usuario_id=u.id JOIN usuario_rol ur ON ur.usuario_aplicacion_id=ua.id JOIN roles r ON r.id=ur.rol_id;
INSERT INTO aplicaciones VALUES(47,'tdv2','TDV2 sintético',true);
INSERT INTO cuentas_postgresql VALUES(1,47,'nexo_scope_client','aplicacion',true,true,null,null,'aplicado',1,1);
INSERT INTO cuenta_postgresql_aplicacion VALUES(1,47,true);
INSERT INTO delegaciones_aplicacion VALUES(47,true);
INSERT INTO roles VALUES(10,47,'responsable_ur','Responsable',true),(30,47,'colaborador_local','Local',true),
 (31,47,'colaborador_dependencias','Dependencias',true),(32,47,'consulta_institucional','Consulta',true),(33,47,'otro','No delegable',true);
INSERT INTO delegacion_roles VALUES(47,10,'delegar'),(47,30,'asignar'),(47,31,'asignar'),(47,32,'asignar');
INSERT INTO unidades_responsables_poa VALUES('A','06000','Principal',2,null,'0001','1',2026,'Activo'),
 ('A3','06010','Responsabilidad',3,'A','0002','1',2026,'Activo'),
 ('A4','06011','Subordinada',4,'A3',null,'1',2026,'Activo'),('B','07000','Ajena',2,null,null,'1',2026,'Activo');
INSERT INTO empleados_rhum VALUES('0001','Responsable dos','Sintético','','Activo'),('0002','Responsable tres','Sintético','','Activo'),
 ('0050','Persona rama','Sintética','','Activo'),('0060','Persona ajena','Sintética','','Activo'),('0070','Persona misma UR','Sintética','','Activo');
INSERT INTO usuarios_empleado VALUES('jefe2','0001','Activo'),('jefe3','0002','Activo'),('persona','0050','Activo'),('ajena','0060','Activo'),('misma','0070','Activo');
INSERT INTO empleados_nominales_rhum VALUES('0001','A'),('0002','A3'),('0050','A4'),('0060','B'),('0070','A3');
INSERT INTO presencia_institucional SELECT 'unidades_responsables_poa',id_ur,true FROM unidades_responsables_poa;
INSERT INTO presencia_institucional SELECT 'empleados_rhum',num_empleado,true FROM empleados_rhum;
INSERT INTO presencia_institucional SELECT 'usuarios_empleado',usuario,true FROM usuarios_empleado;
INSERT INTO usuarios(email,nombre,usuario_empleado,tipo_cuenta,activo) VALUES('jefe2@uacj.mx','Responsable dos','jefe2','individual',true),('jefe3@uacj.mx','Responsable tres','jefe3','individual',true);
INSERT INTO usuario_aplicacion(usuario_id,aplicacion_id,activo,suspendido) SELECT id,47,true,false FROM usuarios;
INSERT INTO usuario_rol SELECT id,10 FROM usuario_aplicacion;
