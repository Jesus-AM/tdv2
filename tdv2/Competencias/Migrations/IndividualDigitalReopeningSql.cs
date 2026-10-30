namespace Tdv2.Competencias.Migrations;

// SQL exclusivo de 20261030042854. No se altera ninguna migración aplicada.
internal static class IndividualDigitalReopeningSql
{
    internal const string Up = """
        REVOKE ALL ON public.cd_submissions, public.cd_reopen_operations FROM PUBLIC;
        CREATE FUNCTION public.cd_guard_submission() RETURNS trigger
        LANGUAGE plpgsql SET search_path=pg_catalog AS $$
        BEGIN RAISE EXCEPTION 'El historial de envíos y reaperturas es inmutable.'; END $$;
        REVOKE ALL ON FUNCTION public.cd_guard_submission() FROM PUBLIC;
        CREATE TRIGGER cd_submission_immutable BEFORE UPDATE OR DELETE ON public.cd_submissions
          FOR EACH ROW EXECUTE FUNCTION public.cd_guard_submission();
        CREATE TRIGGER cd_reopen_immutable BEFORE UPDATE OR DELETE ON public.cd_reopen_operations
          FOR EACH ROW EXECUTE FUNCTION public.cd_guard_submission();

        CREATE FUNCTION public.cd_reopen_survey(p_operation uuid,p_actor text,p_actor_employee text,p_survey uuid,p_version integer,p_generation uuid) RETURNS jsonb
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,pg_temp AS $$
        DECLARE s public.cd_surveys%ROWTYPE; r public.cd_reopen_operations%ROWTYPE; generation uuid; affected text; stamp timestamptz;
        BEGIN
          IF p_operation IS NULL OR p_actor IS NULL OR p_actor='' OR p_actor_employee IS NULL OR p_actor_employee='' THEN
            RAISE EXCEPTION 'Administrador requerido.' USING ERRCODE='CD002';
          END IF;
          PERFORM pg_catalog.pg_advisory_xact_lock_shared(2710202601::bigint);
          -- Serializa el mismo recibo aunque intente utilizarse para dos personas diferentes.
          PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(p_operation::text,30102026));
          SELECT * INTO r FROM public.cd_reopen_operations WHERE "Id"=p_operation;
          IF FOUND THEN
            IF r."Actor" IS DISTINCT FROM p_actor OR r."ActorEmployee" IS DISTINCT FROM p_actor_employee
              OR r."SurveyId" IS DISTINCT FROM p_survey OR r."PreviousVersion" IS DISTINCT FROM p_version OR r."Generation" IS DISTINCT FROM p_generation THEN
              RAISE EXCEPTION 'Recibo ajeno.' USING ERRCODE='CD002';
            END IF;
            SELECT "SnapshotJson"->>'Email' INTO affected FROM public.cd_submissions WHERE "Id"=r."SubmissionId";
          ELSE
            SELECT "Employee" INTO affected FROM public.cd_surveys WHERE "Id"=p_survey;
            IF NOT FOUND THEN RAISE EXCEPTION 'Encuesta no vigente.' USING ERRCODE='CD003'; END IF;
            PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(affected,27102026));
            SELECT * INTO STRICT s FROM public.cd_surveys WHERE "Id"=p_survey FOR UPDATE;
            SELECT "Generation" INTO STRICT generation FROM public.cd_control WHERE "Id"=1;
            IF generation IS DISTINCT FROM p_generation OR s."Version" IS DISTINCT FROM p_version OR s."CompletedAt" IS NULL THEN
              RAISE EXCEPTION 'Encuesta modificada.' USING ERRCODE='CD003';
            END IF;
            stamp := pg_catalog.clock_timestamp();
            r."Id":=p_operation; r."SurveyId":=s."Id"; r."SubmissionId":=pg_catalog.gen_random_uuid(); r."Generation":=generation;
            r."Actor":=p_actor; r."ActorEmployee":=p_actor_employee; r."Employee":=s."Employee";
            r."PreviousVersion":=s."Version"; r."ResultVersion":=s."Version"+1; r."CompletedAt":=stamp;
            INSERT INTO public.cd_submissions("Id","SurveyId","Employee","UnitId","Generation","Revision","CompletedAt","SnapshotJson")
              VALUES(r."SubmissionId",s."Id",s."Employee",s."UnitId",generation,s."Revision",s."CompletedAt",pg_catalog.to_jsonb(s));
            INSERT INTO public.cd_reopen_operations SELECT r.*;
            UPDATE public.cd_surveys SET "CompletedAt"=NULL,"ReportJson"=NULL,"Version"=r."ResultVersion",
              "MinimumVersion"=r."ResultVersion","Revision"=s."Revision"+1,"ReopenedAt"=stamp,"UpdatedAt"=stamp WHERE "Id"=s."Id";
            affected:=s."Email";
          END IF;
          RETURN pg_catalog.jsonb_build_object('operation',r."Id",'survey',r."SurveyId",'submission',r."SubmissionId",
            'previousVersion',r."PreviousVersion",'version',r."ResultVersion",'completedAt',r."CompletedAt",'email',coalesce(affected,p_actor));
        END $$;
        REVOKE ALL ON FUNCTION public.cd_reopen_survey(uuid,text,text,uuid,integer,uuid) FROM PUBLIC;

        CREATE OR REPLACE FUNCTION public.cd_guard_survey() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
        BEGIN
          IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se permite eliminar intentos del diagnóstico.'; END IF;
          IF OLD."CompletedAt" IS NOT NULL THEN
            -- Sólo el propietario de la función autorizada, con archivo y recibo exactos.
            -- Ninguna variable de sesión/flag enviado por el cliente desactiva la protección.
            IF current_user = pg_catalog.pg_get_userbyid((SELECT proowner FROM pg_catalog.pg_proc WHERE oid='public.cd_reopen_survey(uuid,text,text,uuid,integer,uuid)'::regprocedure))
              AND NEW."CompletedAt" IS NULL AND NEW."ReportJson" IS NULL AND NEW."Version"=OLD."Version"+1
              AND NEW."Revision"=OLD."Revision"+1 AND NEW."MinimumVersion"=NEW."Version"
              AND NEW."ReopenedAt"=NEW."UpdatedAt"
              AND (pg_catalog.to_jsonb(NEW)-ARRAY['CompletedAt','ReportJson','Version','Revision','MinimumVersion','ReopenedAt','UpdatedAt'])
                = (pg_catalog.to_jsonb(OLD)-ARRAY['CompletedAt','ReportJson','Version','Revision','MinimumVersion','ReopenedAt','UpdatedAt'])
              AND EXISTS(SELECT 1 FROM public.cd_submissions h JOIN public.cd_reopen_operations r ON r."SubmissionId"=h."Id"
                WHERE h."SurveyId"=OLD."Id" AND h."Revision"=OLD."Revision" AND h."SnapshotJson"=pg_catalog.to_jsonb(OLD)
                  AND r."ResultVersion"=NEW."Version" AND r."CompletedAt"=NEW."ReopenedAt") THEN RETURN NEW;
            END IF;
            RAISE EXCEPTION 'La encuesta definitiva es inmutable.';
          END IF;
          IF OLD."Employee" IS DISTINCT FROM NEW."Employee" OR OLD."Id" IS DISTINCT FROM NEW."Id"
            OR OLD."Revision" IS DISTINCT FROM NEW."Revision" OR OLD."MinimumVersion" IS DISTINCT FROM NEW."MinimumVersion" OR OLD."ReopenedAt" IS DISTINCT FROM NEW."ReopenedAt"
            THEN RAISE EXCEPTION 'No se permite sustituir el participante o revisión de un intento.'; END IF;
          IF OLD."InstrumentVersion" IS DISTINCT FROM NEW."InstrumentVersion" OR OLD."InstrumentJson" IS DISTINCT FROM NEW."InstrumentJson" THEN
            IF OLD."ReopenedAt" IS NOT NULL OR OLD."InstrumentVersion" <> 'uacj-digital-2026-10-27.1'
              OR NEW."InstrumentVersion" <> 'uacj-digital-2026-10-28.1'
              OR NEW."CompletedAt" IS NOT NULL OR NEW."Version" <> OLD."Version" + 1
              OR NEW."InstrumentJson"->>'version' IS DISTINCT FROM NEW."InstrumentVersion"
              OR NEW."InstrumentJson"->'previousDraft'->'instrument' IS DISTINCT FROM OLD."InstrumentJson"
              OR NEW."InstrumentJson"->'previousDraft'->'profile' IS DISTINCT FROM OLD."ProfileJson"
              OR NEW."InstrumentJson"->'previousDraft'->'answers' IS DISTINCT FROM OLD."AnswersJson"
              OR NEW."InstrumentJson"->'previousDraft'->>'version' IS DISTINCT FROM OLD."Version"::text
              THEN RAISE EXCEPTION 'La actualización del borrador requiere conservar la edición y captura anteriores.';
            END IF;
          END IF;
          RETURN NEW;
        END $$;
        """;
}
