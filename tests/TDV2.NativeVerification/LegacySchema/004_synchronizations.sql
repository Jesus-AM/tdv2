-- Explicit application to a prepared isolated TDV2 database only. No remote DDL.
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
 id smallint PRIMARY KEY CHECK(id=1), activa boolean NOT NULL DEFAULT false, intervalo_minutos integer NOT NULL DEFAULT 60,
 hora varchar(5) NOT NULL DEFAULT '08:00', zona_horaria varchar(64) NOT NULL DEFAULT 'America/Ciudad_Juarez',
 incluir_ilda boolean NOT NULL DEFAULT false, version integer NOT NULL DEFAULT 1, proxima_en timestamp,
 procesador_visto_en timestamp, ejecucion_activa uuid REFERENCES sincronizacion_ejecuciones(id), propietario uuid,
 reserva_hasta timestamp, actualizado_por varchar(254), updated_at timestamp
);
INSERT INTO sincronizacion_configuracion(id) VALUES(1);
COMMIT;
