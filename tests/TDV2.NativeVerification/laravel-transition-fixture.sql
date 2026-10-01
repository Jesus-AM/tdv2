-- Fixture sintético del contrato declarativo Laravel revisado; no se importan datos institucionales.
-- Sin tablas ASP.NET. Conserva las diferencias de CHECK/FK del programador Laravel.
BEGIN;
CREATE TABLE users (
    id bigserial PRIMARY KEY, name varchar(255) NOT NULL, email varchar(255) NOT NULL UNIQUE,
    email_verified_at timestamp, password varchar(255) NOT NULL, remember_token varchar(100),
    microsoft_tenant_id uuid, microsoft_id uuid, created_at timestamp, updated_at timestamp,
    CONSTRAINT users_microsoft_identity_unique UNIQUE(microsoft_tenant_id,microsoft_id)
);
CREATE TABLE ms_graph_tokens (
    id serial PRIMARY KEY, user_id integer UNIQUE, email varchar(255),
    access_token text NOT NULL, refresh_token text, expires varchar(255) NOT NULL, created_at timestamp, updated_at timestamp
);
CREATE TABLE activity_logs (
    id bigserial PRIMARY KEY, user_email varchar(255), user_name varchar(255), role_id bigint,
    ur varchar(255), ur2 varchar(255), entity varchar(255), id_registro bigint, action varchar(255),
    meta json, ip varchar(255), user_agent varchar(255), created_at timestamp, updated_at timestamp
);
CREATE TABLE unidades_responsables_poa (
    id_ur varchar(32) PRIMARY KEY, ejercicio integer NOT NULL, cve_ur varchar(32) NOT NULL, desc_ur varchar(500),
    num_empleado varchar(32), encargado varchar(255), id_ur_pertenece varchar(32), tipo_ur varchar(32), nivel_ur integer,
    estatus_ur varchar(32), presente boolean NOT NULL DEFAULT true, sincronizado_en timestamp
);
CREATE INDEX unidades_responsables_poa_num_empleado_index ON unidades_responsables_poa(num_empleado);
CREATE INDEX unidades_responsables_poa_id_ur_pertenece_index ON unidades_responsables_poa(id_ur_pertenece);
CREATE TABLE formatos_ur (
    id bigserial PRIMARY KEY, id_ur varchar(32) NOT NULL UNIQUE REFERENCES unidades_responsables_poa(id_ur) ON DELETE RESTRICT,
    contenido json NOT NULL, version integer NOT NULL DEFAULT 1, porcentaje smallint NOT NULL DEFAULT 0,
    actualizado_por varchar(254) NOT NULL, created_at timestamp, updated_at timestamp
);
CREATE TABLE colaboraciones_ur (
    id bigserial PRIMARY KEY, email varchar(254) NOT NULL, num_empleado varchar(32) NOT NULL, nombre varchar(255) NOT NULL,
    id_ur_origen varchar(32) NOT NULL, id_ur_alcance varchar(32) NOT NULL, tipo varchar(24) NOT NULL,
    nexo_concesion_id bigint NOT NULL, nexo_rol_id bigint NOT NULL, otorgado_por varchar(254) NOT NULL,
    ur_otorgante varchar(32) NOT NULL, revocada_en timestamp, retiro_central_pendiente boolean NOT NULL DEFAULT false,
    created_at timestamp, updated_at timestamp, CONSTRAINT colaboracion_concesion_alcance_unique UNIQUE(nexo_concesion_id,id_ur_alcance)
);
COMMIT;

BEGIN;
CREATE TABLE sincronizaciones_institucionales (id bigserial PRIMARY KEY, resumen json NOT NULL, completada_en timestamp NOT NULL);
CREATE TABLE sincronizacion_catalogos (fuente varchar(12) PRIMARY KEY, registros integer NOT NULL, completada_en timestamp NOT NULL);
CREATE TABLE ilda_informacion_area (
 id_origen varchar(40) PRIMARY KEY, ur2 varchar(255), informacion_generada text, datos json NOT NULL,
 presente boolean NOT NULL DEFAULT true, sincronizado_en timestamp NOT NULL
);
CREATE INDEX ilda_informacion_area_ur2_index ON ilda_informacion_area(ur2);
CREATE INDEX ilda_informacion_area_presente_index ON ilda_informacion_area(presente);
CREATE TABLE sincronizacion_ejecuciones (
 id uuid PRIMARY KEY, fuentes varchar(12) NOT NULL, origen varchar(20) NOT NULL, solicitado_por varchar(254) NOT NULL,
 estado varchar(20) NOT NULL, etapa varchar(12), resultado json NOT NULL, solicitada_en timestamp NOT NULL,
 iniciada_en timestamp, terminada_en timestamp
);
CREATE INDEX sincronizacion_ejecuciones_estado_index ON sincronizacion_ejecuciones(estado);
CREATE TABLE sincronizacion_configuracion (
 id smallint PRIMARY KEY , activa boolean NOT NULL DEFAULT false, intervalo_minutos integer NOT NULL DEFAULT 60,
 hora varchar(5) NOT NULL DEFAULT '08:00', zona_horaria varchar(64) NOT NULL DEFAULT 'America/Ciudad_Juarez',
 incluir_ilda boolean NOT NULL DEFAULT false, version integer NOT NULL DEFAULT 1, proxima_en timestamp,
 procesador_visto_en timestamp, ejecucion_activa uuid, propietario uuid,
 reserva_hasta timestamp, actualizado_por varchar(254), updated_at timestamp
);
INSERT INTO sincronizacion_configuracion(id) VALUES(1);
COMMIT;
