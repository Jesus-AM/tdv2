namespace Tdv2.Competencias.Migrations;
internal static class IndividualDigitalResetHistorySql {
internal const string Up = """
CREATE OR REPLACE FUNCTION public.cd_prepare_reset(p_id uuid,p_actor text,p_employee text,p_targets boolean) RETURNS jsonb
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, pg_temp AS $$
                DECLARE c public.cd_control%ROWTYPE; summary jsonb; expiry timestamptz;
                BEGIN
                  IF p_actor IS NULL OR p_employee IS NULL OR p_actor = '' OR p_employee = '' OR p_targets IS NULL THEN
                    RAISE EXCEPTION 'Administrador requerido.' USING ERRCODE = 'CD002';
                  END IF;
                  PERFORM pg_catalog.pg_advisory_xact_lock(2710202601::bigint);
                  LOCK TABLE public.cd_surveys, public.cd_consents, public.cd_targets IN SHARE MODE;
                  SELECT * INTO STRICT c FROM public.cd_control WHERE "Id"=1;
                  expiry := pg_catalog.clock_timestamp() + interval '5 minutes';
                  SELECT pg_catalog.jsonb_build_object(
                    'drafts',count(*) FILTER(WHERE "CompletedAt" IS NULL),
                    'completed',count(*) FILTER(WHERE "CompletedAt" IS NOT NULL),
                    'profiles',count(*) FILTER(WHERE "ProfileJson" <> '{}'::jsonb),
                    'answers',coalesce(sum((SELECT count(*) FROM pg_catalog.jsonb_object_keys("AnswersJson"))),0),
                    'reports',count(*) FILTER(WHERE "ReportJson" IS NOT NULL),
                    'consents',(SELECT count(*) FROM public.cd_consents),
                    'targets',(SELECT count(*) FROM public.cd_targets),'submissions',(SELECT count(*) FROM public.cd_submissions)) INTO summary FROM public.cd_surveys;
                  INSERT INTO public.cd_reset_operations ("Id","Actor","Employee","Generation","Revision","IncludeTargets","SummaryJson","RequestedAt","ExpiresAt")
                    VALUES(p_id,p_actor,p_employee,c."Generation",c."Revision",p_targets,summary,pg_catalog.clock_timestamp(),expiry);
                  RETURN pg_catalog.jsonb_build_object('operation',p_id,'scope','Competencias digitales · todas las unidades',
                    'includeTargets',p_targets,'counts',summary,'expiresAt',expiry,'completedAt',NULL);
                END $$;
                REVOKE ALL ON FUNCTION public.cd_prepare_reset(uuid,text,text,boolean) FROM PUBLIC;

                CREATE OR REPLACE FUNCTION public.cd_execute_reset(p_id uuid,p_actor text,p_employee text) RETURNS jsonb
                LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, pg_temp AS $$
                DECLARE c public.cd_control%ROWTYPE; r public.cd_reset_operations%ROWTYPE; next_generation uuid;
                BEGIN
                  PERFORM pg_catalog.pg_advisory_xact_lock(2710202601::bigint);
                  SELECT * INTO r FROM public.cd_reset_operations WHERE "Id"=p_id FOR UPDATE;
                  IF NOT FOUND OR r."Actor" IS DISTINCT FROM p_actor OR r."Employee" IS DISTINCT FROM p_employee THEN
                    RAISE EXCEPTION 'Confirmación no autorizada.' USING ERRCODE = 'CD002';
                  END IF;
                  IF r."CompletedAt" IS NULL THEN
                    LOCK TABLE public.cd_surveys, public.cd_consents, public.cd_targets IN ACCESS EXCLUSIVE MODE;
                    SELECT * INTO STRICT c FROM public.cd_control WHERE "Id"=1 FOR UPDATE;
                    IF r."ExpiresAt" <= pg_catalog.clock_timestamp() OR r."Generation" <> c."Generation" OR r."Revision" <> c."Revision" THEN
                      RAISE EXCEPTION 'Confirmación vencida o datos modificados.' USING ERRCODE = 'CD001';
                    END IF;
                    -- Excepción exclusiva del reinicio. No desactiva ni modifica cd_survey_immutable.
                    -- Sin CASCADE ni RESTART IDENTITY: estas tablas no contienen secuencias.
                    TRUNCATE TABLE public.cd_surveys, public.cd_consents, public.cd_submissions;
                    IF r."IncludeTargets" THEN TRUNCATE TABLE public.cd_targets; END IF;
                    next_generation := pg_catalog.gen_random_uuid();
                    UPDATE public.cd_control SET "Generation"=next_generation,"Revision"="Revision"+1 WHERE "Id"=1;
                    UPDATE public.cd_reset_operations SET "CompletedAt"=pg_catalog.clock_timestamp(),"GenerationAfter"=next_generation
                      WHERE "Id"=p_id RETURNING * INTO r;
                  END IF;
                  RETURN pg_catalog.jsonb_build_object('operation',r."Id",'scope','Competencias digitales · todas las unidades',
                    'includeTargets',r."IncludeTargets",'counts',r."SummaryJson",'completedAt',r."CompletedAt",'generation',r."GenerationAfter");
                END $$;
                REVOKE ALL ON FUNCTION public.cd_execute_reset(uuid,text,text) FROM PUBLIC;
""";
}