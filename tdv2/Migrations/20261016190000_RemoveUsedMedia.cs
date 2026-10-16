using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tdv2.Infrastructure;

namespace Tdv2.Migrations;

// Migración exclusivamente de datos. No cambia el modelo EF ni añade un segundo migrador.
[DbContext(typeof(Tdv2DbContext))]
[Migration("20261016190000_RemoveUsedMedia")]
public sealed class RemoveUsedMedia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            LOCK TABLE public.formatos_ur, public.formato_envios_etapas, public.formato_bloques, public.formato_operaciones IN ACCESS EXCLUSIVE MODE;
            -- Excepción acotada a esta transacción; rollback también restaura los triggers.
            ALTER TABLE public.formatos_ur DISABLE TRIGGER formato_enviado_inmutable;
            ALTER TABLE public.formatos_ur DISABLE TRIGGER respuestas_etapas_inmutables;
            ALTER TABLE public.formato_envios_etapas DISABLE TRIGGER envio_etapa_inmutable;

            UPDATE public.formatos_ur SET contenido=(contenido::jsonb - 'medios' - 'medioOtro')::json
              WHERE jsonb_typeof(contenido::jsonb)='object' AND contenido::jsonb ?| ARRAY['medios','medioOtro'];
            -- Instantáneas con envoltorio y documentos anteriores guardados directamente.
            UPDATE public.formatos_ur SET instantanea_envio=instantanea_envio - 'medios' - 'medioOtro'
                #- '{contenido,medios}' #- '{contenido,medioOtro}'
              WHERE jsonb_typeof(instantanea_envio)='object' AND (instantanea_envio ?| ARRAY['medios','medioOtro']
                OR coalesce(instantanea_envio->'contenido' ?| ARRAY['medios','medioOtro'],false));
            UPDATE public.formato_envios_etapas SET instantanea=instantanea - 'medios' - 'medioOtro'
                #- '{contenido,medios}' #- '{contenido,medioOtro}'
              WHERE jsonb_typeof(instantanea)='object' AND (instantanea ?| ARRAY['medios','medioOtro']
                OR coalesce(instantanea->'contenido' ?| ARRAY['medios','medioOtro'],false));

            -- Sólo el bloque retirado; versiones/reservas de todas las demás filas permanecen intactas.
            DELETE FROM public.formato_bloques WHERE bloque='medios';
            UPDATE public.formato_operaciones SET respuesta=respuesta #- '{entrega,resumen,medios}'
              WHERE (respuesta #> '{entrega,resumen}') ? 'medios';
            UPDATE public.formato_operaciones op SET respuesta=jsonb_set(respuesta,'{bloques}',
                (SELECT coalesce(jsonb_agg(value ORDER BY ordinality),'[]'::jsonb)
                 FROM jsonb_array_elements(op.respuesta->'bloques') WITH ORDINALITY WHERE value->>'key' IS DISTINCT FROM 'medios'))
              WHERE jsonb_typeof(respuesta->'bloques')='array'
                AND EXISTS(SELECT 1 FROM jsonb_array_elements(respuesta->'bloques') b WHERE b->>'key'='medios');

            ALTER TABLE public.formato_envios_etapas ENABLE TRIGGER envio_etapa_inmutable;
            ALTER TABLE public.formatos_ur ENABLE TRIGGER respuestas_etapas_inmutables;
            ALTER TABLE public.formatos_ur ENABLE TRIGGER formato_enviado_inmutable;
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Un esquema vacío puede desmontarse en pruebas. Con formatos, recuperar exige un respaldo real.
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS(SELECT 1 FROM public.formatos_ur) THEN
                    RAISE EXCEPTION 'La limpieza de Medios utilizados requiere recuperar el respaldo previo; Down no restaura respuestas.';
                END IF;
            END $$;
            """);
    }
}
