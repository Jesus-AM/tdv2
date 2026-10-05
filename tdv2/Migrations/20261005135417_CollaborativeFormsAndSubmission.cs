using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class CollaborativeFormsAndSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ejercicio",
                schema: "public",
                table: "formatos_ur",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "enviado_como",
                schema: "public",
                table: "formatos_ur",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "enviado_en",
                schema: "public",
                table: "formatos_ur",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enviado_por",
                schema: "public",
                table: "formatos_ur",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "envio_id",
                schema: "public",
                table: "formatos_ur",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instantanea_envio",
                schema: "public",
                table: "formatos_ur",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "formato_bloques",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bloque = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    reserva_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sesion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    pestana = table.Column<Guid>(type: "uuid", nullable: true),
                    revision_contexto = table.Column<long>(type: "bigint", nullable: false),
                    titular = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    vence_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formato_bloques", x => new { x.id_ur, x.bloque });
                    table.CheckConstraint("formato_bloques_version_ck", "version >= 0");
                    table.ForeignKey(
                        name: "FK_formato_bloques_formatos_ur_id_ur",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "formatos_ur",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "formato_operaciones",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    operacion = table.Column<Guid>(type: "uuid", nullable: false),
                    sesion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revision_contexto = table.Column<long>(type: "bigint", nullable: false),
                    pestana = table.Column<Guid>(type: "uuid", nullable: false),
                    huella = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    respuesta = table.Column<string>(type: "jsonb", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formato_operaciones", x => new { x.id_ur, x.operacion });
                    table.ForeignKey(
                        name: "FK_formato_operaciones_formatos_ur_id_ur",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "formatos_ur",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "formato_posiciones",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    efectivo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    seccion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formato_posiciones", x => new { x.id_ur, x.actor, x.efectivo });
                    table.ForeignKey(
                        name: "FK_formato_posiciones_formatos_ur_id_ur",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "formatos_ur",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_formato_bloques_id_ur_vence_en",
                schema: "public",
                table: "formato_bloques",
                columns: new[] { "id_ur", "vence_en" });
            migrationBuilder.Sql("""
                UPDATE public.formatos_ur f SET ejercicio=u.ejercicio
                  FROM public.unidades_responsables_poa u WHERE u.id_ur=f.id_ur;
                CREATE FUNCTION public.proteger_formato_enviado() RETURNS trigger
                LANGUAGE plpgsql SET search_path=pg_catalog,public AS $$
                BEGIN
                    IF OLD.enviado_en IS NOT NULL THEN
                        IF TG_OP='DELETE' THEN RAISE EXCEPTION 'El formato enviado es inmutable.' USING ERRCODE='55000'; END IF;
                        IF to_jsonb(NEW) IS DISTINCT FROM to_jsonb(OLD) THEN
                            RAISE EXCEPTION 'El formato enviado es inmutable.' USING ERRCODE='55000';
                        END IF;
                    END IF;
                    IF TG_OP='DELETE' THEN RETURN OLD; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER formato_enviado_inmutable BEFORE UPDATE OR DELETE ON public.formatos_ur
                  FOR EACH ROW EXECUTE FUNCTION public.proteger_formato_enviado();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se puede retirar la protección de formatos enviados. Utilice una migración posterior.';
                    END IF;
                END $$;
                DROP TRIGGER formato_enviado_inmutable ON public.formatos_ur;
                DROP FUNCTION public.proteger_formato_enviado();
                """);
            migrationBuilder.DropTable(
                name: "formato_bloques",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formato_operaciones",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formato_posiciones",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "ejercicio",
                schema: "public",
                table: "formatos_ur");

            migrationBuilder.DropColumn(
                name: "enviado_como",
                schema: "public",
                table: "formatos_ur");

            migrationBuilder.DropColumn(
                name: "enviado_en",
                schema: "public",
                table: "formatos_ur");

            migrationBuilder.DropColumn(
                name: "enviado_por",
                schema: "public",
                table: "formatos_ur");

            migrationBuilder.DropColumn(
                name: "envio_id",
                schema: "public",
                table: "formatos_ur");

            migrationBuilder.DropColumn(
                name: "instantanea_envio",
                schema: "public",
                table: "formatos_ur");
        }
    }
}
