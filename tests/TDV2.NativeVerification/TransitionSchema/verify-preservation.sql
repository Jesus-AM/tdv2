DO $verify$
DECLARE preserved record; actual text;
BEGIN
    FOR preserved IN SELECT * FROM transition_preservation LOOP
        EXECUTE format('SELECT md5(coalesce(string_agg(h, '''' ORDER BY h),'''')) FROM (SELECT md5(row_to_json(t)::text) h FROM public.%I t) rows', preserved.table_name) INTO actual;
        IF actual IS DISTINCT FROM preserved.fingerprint THEN
            RAISE EXCEPTION 'La transicion altero datos que debian conservarse; se revierte.';
        END IF;
    END LOOP;
END $verify$;
