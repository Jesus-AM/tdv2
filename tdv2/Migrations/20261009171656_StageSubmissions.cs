using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class StageSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "etapa_activa",
                schema: "public",
                table: "formatos_ur",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "formato_envios_etapas",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    etapa = table.Column<int>(type: "integer", nullable: false),
                    ejercicio = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    operacion = table.Column<Guid>(type: "uuid", nullable: false),
                    enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    efectivo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    nombre = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    instantanea = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formato_envios_etapas", x => new { x.id_ur, x.etapa });
                    table.CheckConstraint("formato_envios_etapas_valores_ck", "etapa > 0 AND version >= 0");
                    table.ForeignKey(
                        name: "FK_formato_envios_etapas_formatos_ur_id_ur",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "formatos_ur",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_formato_envios_etapas_id_ur_operacion",
                schema: "public",
                table: "formato_envios_etapas",
                columns: new[] { "id_ur", "operacion" },
                unique: true);
            // No se atribuyen envíos globales antiguos a una etapa: no contienen evidencia suficiente.
            // Sus columnas, instantáneas y el trigger de bloqueo global permanecen intactos.
            migrationBuilder.Sql("""
                CREATE FUNCTION public.proteger_envio_etapa() RETURNS trigger
                LANGUAGE plpgsql SET search_path=pg_catalog,public AS $$
                DECLARE parent record; part record;
                BEGIN
                    IF TG_OP <> 'INSERT' THEN
                        RAISE EXCEPTION 'El envío de etapa es inmutable.' USING ERRCODE='55000';
                    END IF;
                    SELECT * INTO parent FROM public.formatos_ur WHERE id_ur=NEW.id_ur FOR UPDATE;
                    IF parent.enviado_en IS NOT NULL OR parent.etapa_activa <> NEW.etapa OR parent.ejercicio <> NEW.ejercicio THEN
                        RAISE EXCEPTION 'La etapa no corresponde al formato editable.' USING ERRCODE='55000';
                    END IF;
                    IF jsonb_typeof(NEW.instantanea->'contenido') IS DISTINCT FROM 'object' OR NEW.instantanea->'contenido'='{}'::jsonb THEN
                        RAISE EXCEPTION 'La instantánea de etapa no es válida.' USING ERRCODE='55000';
                    END IF;
                    FOR part IN SELECT key,value FROM jsonb_each(NEW.instantanea->'contenido') LOOP
                        IF parent.contenido::jsonb->part.key IS DISTINCT FROM part.value THEN
                            RAISE EXCEPTION 'La instantánea no coincide con las respuestas confirmadas.' USING ERRCODE='55000';
                        END IF;
                    END LOOP;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER envio_etapa_inmutable BEFORE INSERT OR UPDATE OR DELETE ON public.formato_envios_etapas
                    FOR EACH ROW EXECUTE FUNCTION public.proteger_envio_etapa();
                CREATE FUNCTION public.proteger_respuestas_etapas() RETURNS trigger
                LANGUAGE plpgsql SET search_path=pg_catalog,public AS $$
                DECLARE part record;
                BEGIN
                    IF EXISTS(SELECT 1 FROM public.formato_envios_etapas WHERE id_ur=OLD.id_ur) THEN
                        IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Las respuestas enviadas son inmutables.' USING ERRCODE='55000'; END IF;
                        IF NEW.id_ur<>OLD.id_ur OR NEW.ejercicio<>OLD.ejercicio THEN
                            RAISE EXCEPTION 'El área y ejercicio enviados son inmutables.' USING ERRCODE='55000';
                        END IF;
                        FOR part IN SELECT p.key,p.value FROM public.formato_envios_etapas s,
                            LATERAL jsonb_each(s.instantanea->'contenido') p WHERE s.id_ur=OLD.id_ur LOOP
                            IF NEW.contenido::jsonb->part.key IS DISTINCT FROM part.value THEN
                                RAISE EXCEPTION 'Las respuestas de una etapa enviada son inmutables.' USING ERRCODE='55000';
                            END IF;
                        END LOOP;
                    END IF;
                    IF TG_OP='DELETE' THEN RETURN OLD; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER respuestas_etapas_inmutables BEFORE UPDATE OR DELETE ON public.formatos_ur
                    FOR EACH ROW EXECUTE FUNCTION public.proteger_respuestas_etapas();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.formato_envios_etapas)
                        OR EXISTS(SELECT 1 FROM public.formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se pueden retirar envíos de etapas existentes. Utilice una migración posterior.';
                    END IF;
                END $$;
                DROP TRIGGER respuestas_etapas_inmutables ON public.formatos_ur;
                DROP FUNCTION public.proteger_respuestas_etapas();
                DROP TRIGGER envio_etapa_inmutable ON public.formato_envios_etapas;
                DROP FUNCTION public.proteger_envio_etapa();
                """);
            migrationBuilder.DropTable(
                name: "formato_envios_etapas",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "etapa_activa",
                schema: "public",
                table: "formatos_ur");
        }
    }
}
