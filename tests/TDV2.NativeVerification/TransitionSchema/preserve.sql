-- Huellas temporales dentro de la transacción. No salen datos, respuestas ni identidades del servidor.
CREATE TEMP TABLE transition_preservation(table_name text PRIMARY KEY, fingerprint text) ON COMMIT DROP;
DO $preserve$
DECLARE table_name text; fingerprint text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['activity_logs','unidades_responsables_poa','formatos_ur','colaboraciones_ur',
        'sincronizaciones_institucionales','sincronizacion_catalogos','ilda_informacion_area','sincronizacion_ejecuciones']
    LOOP
        EXECUTE format('SELECT md5(coalesce(string_agg(h, '''' ORDER BY h),'''')) FROM (SELECT md5(row_to_json(t)::text) h FROM public.%I t) rows', table_name) INTO fingerprint;
        INSERT INTO transition_preservation VALUES(table_name,fingerprint);
    END LOOP;
END $preserve$;
