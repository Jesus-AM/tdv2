using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class CurrentRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Retiro único de categorías exactas: conserva otras selecciones, enviados y ejercicios bloqueados.
            migrationBuilder.Sql("""
                LOCK TABLE public.formatos_ur, public.formato_bloques IN SHARE ROW EXCLUSIVE MODE;
                LOCK TABLE public.unidades_responsables_poa IN SHARE MODE;
                DO $users$
                DECLARE f record; doc jsonb; item jsonb; changed_ids text[]; item_index integer;
                    recipients jsonb; cleaned jsonb; section_name text; fields text[]; weight numeric; row_count integer; filled numeric;
                    points numeric; code_count integer; criterion_count integer; progress smallint;
                BEGIN
                    -- Un ejercicio anterior tampoco es editable según FormEditingService.Editable.
                    FOR f IN SELECT saved.id_ur, saved.contenido, saved.version FROM public.formatos_ur saved
                        JOIN public.unidades_responsables_poa unit ON unit.id_ur=saved.id_ur
                        WHERE saved.enviado_en IS NULL AND (saved.ejercicio=0 OR saved.ejercicio=coalesce(unit.ejercicio,0))
                            AND jsonb_typeof(saved.contenido::jsonb->'identificacion')='array'
                        ORDER BY saved.id_ur FOR UPDATE OF saved
                    LOOP
                        doc := f.contenido::jsonb; changed_ids := ARRAY[]::text[];
                        FOR item, item_index IN SELECT value, ordinality::integer-1
                            FROM jsonb_array_elements(doc->'identificacion') WITH ORDINALITY
                        LOOP
                            cleaned := item - 'usuarioOtro';
                            IF jsonb_typeof(item->'usuario')='array' THEN
                                SELECT coalesce(jsonb_agg(value ORDER BY ordinality),'[]'::jsonb) INTO recipients
                                    FROM jsonb_array_elements(item->'usuario') WITH ORDINALITY
                                    WHERE value NOT IN ('"Otro"'::jsonb,'"Externo"'::jsonb);
                                cleaned := jsonb_set(cleaned, '{usuario}', recipients);
                            END IF;
                            IF cleaned IS DISTINCT FROM item THEN
                                doc := jsonb_set(doc, ARRAY['identificacion',item_index::text], cleaned);
                                changed_ids := array_append(changed_ids, item->>'id');
                            END IF;
                        END LOOP;
                        IF cardinality(changed_ids)=0 THEN CONTINUE; END IF;
                        -- Pesos vigentes congelados en esta migración; coinciden con FormSchema.Progress.
                        points := 0;
                        FOR section_name, fields, weight IN SELECT * FROM (VALUES
                            ('identificacion', ARRAY['tramite','usuario','resultado','responsable','validacion','prioridad'],25),
                            ('sistemas',ARRAY['sistema','uso','estado'],15),
                            ('datos',ARRAY['dato','fuente','origen'],10),
                            ('preguntas',ARRAY['respuesta'],20),
                            ('acuerdos',ARRAY['acuerdo','responsable','fecha'],5)) AS scores(section,fields,weight)
                        LOOP
                            SELECT greatest(count(*),1),coalesce(sum((SELECT count(*) FROM unnest(fields) AS field WHERE
                                CASE WHEN field='usuario' THEN CASE WHEN jsonb_typeof(r->field)='array'
                                    THEN jsonb_array_length(r->field)>0
                                        AND NOT EXISTS(SELECT 1 FROM jsonb_array_elements(r->field) v WHERE v NOT IN
                                            ('"Comunidad universitaria"','"Docentes"','"Estudiantes"','"Personal administrativo"',
                                             '"Público en general"','"Instituciones públicas externas"','"Empresas y organizaciones privadas"'))
                                        AND jsonb_array_length(r->field)=(SELECT count(DISTINCT v) FROM jsonb_array_elements(r->field) v)
                                        ELSE false END
                                ELSE btrim(coalesce(r->>field,''))<>'' END)),0)
                            INTO row_count,filled FROM jsonb_array_elements(coalesce(doc->section_name,'[]'::jsonb)) r;
                            points := points + weight * filled / (row_count * cardinality(fields));
                        END LOOP;
                        SELECT greatest(count(*),1),coalesce(sum((SELECT count(*)
                            FROM jsonb_array_elements(coalesce(doc->'evaluaciones'->(r->>'codigo'),'[]'::jsonb)) e
                            WHERE btrim(coalesce(e->>'valor',''))<>'')),0)
                            INTO code_count,criterion_count FROM jsonb_array_elements(doc->'identificacion') r
                            WHERE btrim(coalesce(r->>'codigo',''))<>'';
                        progress := floor((points+15.0*criterion_count/(code_count*9))*100/90+0.000001);
                        UPDATE public.formatos_ur SET contenido=doc::json, version=version+1, porcentaje=progress
                            WHERE id_ur=f.id_ur AND enviado_en IS NULL;
                        -- Versionar incluso bloques nunca reservados impide readquirir desde una copia antigua.
                        INSERT INTO public.formato_bloques(id_ur,bloque,version,revision_contexto,color)
                            SELECT f.id_ur,'identificacion:'||id,1,0,0 FROM unnest(changed_ids) id
                            ON CONFLICT (id_ur,bloque) DO UPDATE
                            SET version=formato_bloques.version+1,reserva_id=NULL,vence_en=NULL;
                        INSERT INTO public.activity_logs(entity,ur,action,meta,created_at,updated_at)
                            VALUES('formato_ur',f.id_ur,'actualizar_destinatarios',
                                jsonb_build_object('migracion','CurrentRecipients','versionAnterior',f.version,
                                    'versionNueva',f.version+1,'filas',changed_ids,'campos',jsonb_build_array('usuario','usuarioOtro'))::json,
                                timezone('UTC',clock_timestamp()),timezone('UTC',clock_timestamp()));
                    END LOOP;
                END $users$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Detener la reversión antes de retirar cualquier entrada del historial de un esquema con enviados.
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se puede revertir un esquema que contiene formatos enviados.';
                    END IF;
                END $guard$;
                """);
            // Limpieza irreversible: no restaurar categorías retiradas ni texto eliminado. No hay DDL que retirar.
            // El historial EF garantiza la ejecución única; repetir la actualización conserva las categorías vigentes.
        }
    }
}
