-- Contrato leído de las migraciones Laravel y contrastado con el SQL que consume ASP.NET.
-- No modifica datos. Lo ejecuta el ensayo antes de su DDL dentro de la misma transacción.
DO $preflight$
DECLARE expected record;
BEGIN
    IF current_database() !~ '^tdv2_transition_[a-z0-9_]+$'
       OR current_setting('tdv2.transition_target', true) IS DISTINCT FROM current_database() THEN
        RAISE EXCEPTION 'Se requiere una copia local de transicion identificada expresamente.';
    END IF;
    FOR expected IN SELECT * FROM (VALUES
        ('users','id','int8'),
        ('users','name','varchar,text'),
        ('users','email','varchar,text'),
        ('users','email_verified_at','timestamp'),
        ('users','password','varchar,text'),
        ('users','remember_token','varchar,text'),
        ('users','microsoft_tenant_id','uuid'),
        ('users','microsoft_id','uuid'),
        ('users','created_at','timestamp'),
        ('users','updated_at','timestamp'),
        ('ms_graph_tokens','id','int4'),
        ('ms_graph_tokens','user_id','int4'),
        ('ms_graph_tokens','email','varchar,text'),
        ('ms_graph_tokens','access_token','text'),
        ('ms_graph_tokens','refresh_token','text'),
        ('ms_graph_tokens','expires','varchar,text'),
        ('ms_graph_tokens','created_at','timestamp'),
        ('ms_graph_tokens','updated_at','timestamp'),
        ('activity_logs','id','int8'),
        ('activity_logs','user_email','varchar,text'),
        ('activity_logs','user_name','varchar,text'),
        ('activity_logs','role_id','int8'),
        ('activity_logs','id_registro','int8'),
        ('activity_logs','ur','varchar,text'),
        ('activity_logs','ur2','varchar,text'),
        ('activity_logs','entity','varchar,text'),
        ('activity_logs','action','varchar,text'),
        ('activity_logs','meta','json,jsonb'),
        ('activity_logs','ip','varchar,text'),
        ('activity_logs','user_agent','varchar,text'),
        ('activity_logs','created_at','timestamp'),
        ('activity_logs','updated_at','timestamp'),
        ('unidades_responsables_poa','id_ur','varchar,text'),
        ('unidades_responsables_poa','ejercicio','int4'),
        ('unidades_responsables_poa','cve_ur','varchar,text'),
        ('unidades_responsables_poa','desc_ur','varchar,text'),
        ('unidades_responsables_poa','num_empleado','varchar,text'),
        ('unidades_responsables_poa','encargado','varchar,text'),
        ('unidades_responsables_poa','id_ur_pertenece','varchar,text'),
        ('unidades_responsables_poa','tipo_ur','varchar,text'),
        ('unidades_responsables_poa','nivel_ur','int4'),
        ('unidades_responsables_poa','estatus_ur','varchar,text'),
        ('unidades_responsables_poa','presente','bool'),
        ('unidades_responsables_poa','sincronizado_en','timestamp'),
        ('formatos_ur','id','int8'),
        ('formatos_ur','id_ur','varchar,text'),
        ('formatos_ur','contenido','json,jsonb'),
        ('formatos_ur','version','int4'),
        ('formatos_ur','porcentaje','int2,int4'),
        ('formatos_ur','actualizado_por','varchar,text'),
        ('formatos_ur','created_at','timestamp'),
        ('formatos_ur','updated_at','timestamp'),
        ('colaboraciones_ur','id','int8'),
        ('colaboraciones_ur','email','varchar,text'),
        ('colaboraciones_ur','num_empleado','varchar,text'),
        ('colaboraciones_ur','nombre','varchar,text'),
        ('colaboraciones_ur','id_ur_origen','varchar,text'),
        ('colaboraciones_ur','id_ur_alcance','varchar,text'),
        ('colaboraciones_ur','tipo','varchar,text'),
        ('colaboraciones_ur','nexo_concesion_id','int8'),
        ('colaboraciones_ur','nexo_rol_id','int8'),
        ('colaboraciones_ur','otorgado_por','varchar,text'),
        ('colaboraciones_ur','ur_otorgante','varchar,text'),
        ('colaboraciones_ur','revocada_en','timestamp'),
        ('colaboraciones_ur','retiro_central_pendiente','bool'),
        ('colaboraciones_ur','created_at','timestamp'),
        ('colaboraciones_ur','updated_at','timestamp'),
        ('sincronizaciones_institucionales','id','int8'),
        ('sincronizaciones_institucionales','resumen','json,jsonb'),
        ('sincronizaciones_institucionales','completada_en','timestamp'),
        ('sincronizacion_catalogos','fuente','varchar,text'),
        ('sincronizacion_catalogos','registros','int4'),
        ('sincronizacion_catalogos','completada_en','timestamp'),
        ('ilda_informacion_area','id_origen','varchar,text'),
        ('ilda_informacion_area','ur2','varchar,text'),
        ('ilda_informacion_area','informacion_generada','text'),
        ('ilda_informacion_area','datos','json,jsonb'),
        ('ilda_informacion_area','presente','bool'),
        ('ilda_informacion_area','sincronizado_en','timestamp'),
        ('sincronizacion_ejecuciones','id','uuid'),
        ('sincronizacion_ejecuciones','fuentes','varchar,text'),
        ('sincronizacion_ejecuciones','origen','varchar,text'),
        ('sincronizacion_ejecuciones','solicitado_por','varchar,text'),
        ('sincronizacion_ejecuciones','estado','varchar,text'),
        ('sincronizacion_ejecuciones','etapa','varchar,text'),
        ('sincronizacion_ejecuciones','resultado','json,jsonb'),
        ('sincronizacion_ejecuciones','solicitada_en','timestamp'),
        ('sincronizacion_ejecuciones','iniciada_en','timestamp'),
        ('sincronizacion_ejecuciones','terminada_en','timestamp'),
        ('sincronizacion_configuracion','id','int2,int4'),
        ('sincronizacion_configuracion','activa','bool'),
        ('sincronizacion_configuracion','intervalo_minutos','int4'),
        ('sincronizacion_configuracion','hora','varchar,text'),
        ('sincronizacion_configuracion','zona_horaria','varchar,text'),
        ('sincronizacion_configuracion','incluir_ilda','bool'),
        ('sincronizacion_configuracion','version','int4'),
        ('sincronizacion_configuracion','proxima_en','timestamp'),
        ('sincronizacion_configuracion','procesador_visto_en','timestamp'),
        ('sincronizacion_configuracion','ejecucion_activa','uuid'),
        ('sincronizacion_configuracion','propietario','uuid'),
        ('sincronizacion_configuracion','reserva_hasta','timestamp'),
        ('sincronizacion_configuracion','actualizado_por','varchar,text'),
        ('sincronizacion_configuracion','updated_at','timestamp')
    ) AS required(table_name,column_name,types)
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
              JOIN pg_attribute a ON a.attrelid=c.oid JOIN pg_type t ON t.oid=a.atttypid
            WHERE n.nspname='public' AND c.relname=expected.table_name AND c.relkind='r'
              AND a.attname=expected.column_name AND NOT a.attisdropped
              AND t.typname=ANY(string_to_array(expected.types,','))
        ) THEN
            RAISE EXCEPTION 'Esquema incompatible: revisar tabla y columna requeridas: %.%', expected.table_name,expected.column_name;
        END IF;
    END LOOP;
    -- ON CONFLICT necesita claves únicas reales; que existan columnas no acredita compatibilidad.
    FOR expected IN SELECT * FROM (VALUES
        ('users',ARRAY['id']),('users',ARRAY['email']),('users',ARRAY['microsoft_tenant_id','microsoft_id']),
        ('unidades_responsables_poa',ARRAY['id_ur']),('formatos_ur',ARRAY['id_ur']),
        ('colaboraciones_ur',ARRAY['nexo_concesion_id','id_ur_alcance']),
        ('sincronizacion_configuracion',ARRAY['id']),('sincronizacion_catalogos',ARRAY['fuente']),
        ('ilda_informacion_area',ARRAY['id_origen']),('sincronizacion_ejecuciones',ARRAY['id'])
    ) AS required(table_name,columns)
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM pg_index i WHERE i.indrelid=to_regclass('public.'||expected.table_name)
              AND i.indisunique AND i.indisvalid AND i.indpred IS NULL
              AND ARRAY(SELECT a.attname::text FROM unnest(i.indkey) WITH ORDINALITY k(num,ord)
                  JOIN pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=k.num ORDER BY k.ord)=expected.columns
        ) THEN RAISE EXCEPTION 'Falta una clave unica requerida en la copia: %.', expected.table_name;
        END IF;
    END LOOP;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint c
        WHERE c.contype='f' AND c.conrelid='public.formatos_ur'::regclass
          AND c.confrelid='public.unidades_responsables_poa'::regclass AND c.confdeltype IN ('a','r')
          AND c.conkey=ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid=c.conrelid AND attname='id_ur')]
          AND c.confkey=ARRAY[(SELECT attnum FROM pg_attribute WHERE attrelid=c.confrelid AND attname='id_ur')]) THEN
        RAISE EXCEPTION 'Falta la relacion restrictiva entre formato y UR.';
    END IF;
    IF to_regclass('public.tdv2_sessions') IS NOT NULL OR to_regnamespace('laravel_archive') IS NOT NULL THEN
        RAISE EXCEPTION 'La copia ya contiene estructuras ASP.NET o un archivo previo; no se reaplica.';
    END IF;
    IF EXISTS (SELECT 1 FROM sincronizacion_configuracion WHERE ejecucion_activa IS NOT NULL OR propietario IS NOT NULL OR reserva_hasta IS NOT NULL)
       OR EXISTS (SELECT 1 FROM sincronizacion_ejecuciones WHERE estado IN ('pendiente','ejecutando')) THEN
        RAISE EXCEPTION 'La copia contiene trabajo pendiente o reservado; requiere resolver el corte antes del ensayo.';
    END IF;
    IF (SELECT count(*) FROM sincronizacion_configuracion) <> 1 OR NOT EXISTS (SELECT 1 FROM sincronizacion_configuracion WHERE id=1) THEN
        RAISE EXCEPTION 'Configuracion de sincronizacion incompatible.';
    END IF;
    IF EXISTS (SELECT lower(email) FROM users GROUP BY lower(email) HAVING count(*)>1)
       OR EXISTS (SELECT 1 FROM formatos_ur f LEFT JOIN unidades_responsables_poa u USING(id_ur) WHERE u.id_ur IS NULL)
       OR EXISTS (SELECT id_ur FROM formatos_ur GROUP BY id_ur HAVING count(*)>1)
       OR EXISTS (SELECT 1 FROM formatos_ur WHERE version<1 OR porcentaje<0 OR porcentaje>100 OR actualizado_por IS NULL OR updated_at IS NULL
           OR jsonb_typeof(contenido::jsonb) IS DISTINCT FROM 'object') THEN
        RAISE EXCEPTION 'Datos incompatibles: revisar identidades, formatos o relaciones en la copia privada.';
    END IF;
END $preflight$;
