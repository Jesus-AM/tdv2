-- Sólo lo ejecuta Prepare-Transition.ps1 en una copia verificada y tras preflight.sql.
-- El llamador engloba preflight, este archivo y 002/003 en una sola transacción.
DO $guard$
BEGIN
    IF current_database() !~ '^tdv2_transition_[a-z0-9_]+$'
       OR current_setting('tdv2.transition_target', true) IS DISTINCT FROM current_database() THEN
        RAISE EXCEPTION 'Destino de transicion no autorizado.';
    END IF;
END $guard$;

-- No intentar descifrar APP_KEY ni convertir sesiones Laravel a permisos ASP.NET.
-- Conservar los tokens antiguos en un esquema inaccesible al rol público y empezar sin tokens.
CREATE SCHEMA laravel_archive;
REVOKE ALL ON SCHEMA laravel_archive FROM PUBLIC;
ALTER TABLE ms_graph_tokens SET SCHEMA laravel_archive;
CREATE TABLE ms_graph_tokens (
    id bigserial PRIMARY KEY, user_id bigint UNIQUE, email varchar(255),
    access_token text NOT NULL, refresh_token text, expires varchar(255) NOT NULL,
    created_at timestamp, updated_at timestamp
);

-- Las respuestas, versiones, avances, autores, catálogos, relaciones e historial no se transforman.
-- La copia declarativa Laravel ya comparte esos contratos; si el esquema real difiere, se aborta.
UPDATE sincronizacion_configuracion SET activa=false,incluir_ilda=false,proxima_en=null,version=version+1 WHERE id=1;

CREATE TABLE tdv2_transition_migrations (
    name text PRIMARY KEY, sha256 text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now()
);
