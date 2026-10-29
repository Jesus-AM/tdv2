-- Sólo fixture sintético: columnas consumidas por el SQL real de registrar_actividad, no migración Nexo.
CREATE TABLE aplicaciones(id bigint PRIMARY KEY,nombre text,activo boolean);
CREATE TABLE cuentas_postgresql(id bigint PRIMARY KEY,aplicacion_id bigint,usuario_postgresql text,tipo text,habilitada boolean,eliminada_en timestamp,eliminacion_solicitada_en timestamp,estado text,revision int,revision_aplicada int,creada_en_postgresql boolean);
CREATE TABLE cuenta_postgresql_aplicacion(cuenta_postgresql_id bigint,aplicacion_id bigint,habilitada boolean);
CREATE TABLE usuarios(id bigint PRIMARY KEY,email text,nombre text,tipo_cuenta text,activo boolean);
CREATE TABLE usuario_aplicacion(id bigint PRIMARY KEY,usuario_id bigint,aplicacion_id bigint,activo boolean,suspendido boolean);
CREATE TABLE roles(id bigint PRIMARY KEY,clave text,nombre text,aplicacion_id bigint,activo boolean);
CREATE TABLE usuario_rol(usuario_aplicacion_id bigint,rol_id bigint);
CREATE TABLE concesiones_acceso(usuario_aplicacion_id bigint,rol_id bigint,id_ur text,revocado_en timestamp);
CREATE TABLE unidades_responsables_poa(id_ur text,cve_ur text,desc_ur text);
CREATE TABLE tipos_evento(aplicacion_id bigint,categoria text,accion text,activo boolean,permite_datos boolean,created_at timestamp,updated_at timestamp,UNIQUE(aplicacion_id,categoria,accion));
CREATE TABLE registro_actividad(id bigserial PRIMARY KEY,evento_uuid uuid,ocurrido_en timestamp,recibido_en timestamp,aplicacion_origen_id bigint,aplicacion_origen_nombre text,categoria text,accion text,resultado text,motivo text,actor_tipo text,actor_usuario_id bigint,actor_nombre text,actor_email text,actor_tipo_cuenta text,actor_id_ur text,actor_cve_ur text,actor_desc_ur text,actor_roles json,usuario_afectado_id bigint,usuario_afectado_nombre text,usuario_afectado_email text,entidad_tipo text,entidad_id text,antes json,despues json,contexto json,ip text,user_agent text,correlacion_id uuid,cuenta_postgresql_id bigint,huella_payload text,UNIQUE(aplicacion_origen_id,evento_uuid));
INSERT INTO aplicaciones VALUES(87,'Competencias sintéticas',true);
INSERT INTO cuentas_postgresql VALUES(1,87,'cd_native_nexo','aplicacion',true,null,null,'aplicado',1,1,true);
INSERT INTO cuenta_postgresql_aplicacion VALUES(1,87,true);
INSERT INTO usuarios VALUES(20,'persona@uacj.mx','Persona sintética','individual',true),(21,'otra@uacj.mx','Otra persona sintética','individual',true);
INSERT INTO usuario_aplicacion VALUES(10,20,87,true,false),(11,21,87,true,false);
INSERT INTO roles VALUES(30,'participante','Participante',87,true),(31,'administrador','Administrador',87,true);
INSERT INTO usuario_rol VALUES(10,30),(11,30);
