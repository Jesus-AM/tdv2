using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Competencias.Migrations
{
    /// <inheritdoc />
    public partial class InitialDigitalDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF to_regclass('public.formatos_ur') IS NOT NULL OR to_regclass('public.tdv2_sessions') IS NOT NULL THEN
                    RAISE EXCEPTION 'La base del diagnóstico debe ser independiente de TDV2.';
                  END IF;
                END $$;
                """);
            migrationBuilder.CreateTable(
                name: "cd_consents",
                columns: table => new
                {
                    Employee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NoticeVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_consents", x => new { x.Employee, x.NoticeVersion });
                });

            migrationBuilder.CreateTable(
                name: "cd_surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Employee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InstitutionJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    AnswersJson = table.Column<string>(type: "jsonb", nullable: false),
                    InstrumentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    InstrumentJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReportJson = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_surveys", x => x.Id);
                    table.CheckConstraint("cd_final_consistent", "(\"CompletedAt\" IS NULL) = (\"ReportJson\" IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "cd_targets",
                columns: table => new
                {
                    UnitId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_targets", x => x.UnitId);
                    table.CheckConstraint("cd_target_nonnegative", "\"Value\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_cd_surveys_Employee",
                table: "cd_surveys",
                column: "Employee",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cd_surveys_UnitId",
                table: "cd_surveys",
                column: "UnitId");
            migrationBuilder.Sql("""
                CREATE FUNCTION public.cd_guard_survey() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se permite eliminar intentos del diagnóstico.'; END IF;
                  IF OLD."CompletedAt" IS NOT NULL THEN RAISE EXCEPTION 'La encuesta definitiva es inmutable.'; END IF;
                  IF OLD."Employee" IS DISTINCT FROM NEW."Employee" OR OLD."Id" IS DISTINCT FROM NEW."Id"
                    OR OLD."InstrumentVersion" IS DISTINCT FROM NEW."InstrumentVersion" OR OLD."InstrumentJson" IS DISTINCT FROM NEW."InstrumentJson"
                    THEN RAISE EXCEPTION 'No se permite sustituir el participante o instrumento de un intento.'; END IF;
                  RETURN NEW;
                END $$;
                CREATE TRIGGER cd_survey_immutable BEFORE UPDATE OR DELETE ON public.cd_surveys
                  FOR EACH ROW EXECUTE FUNCTION public.cd_guard_survey();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS(SELECT 1 FROM public.cd_surveys) OR EXISTS(SELECT 1 FROM public.cd_consents)
                    OR EXISTS(SELECT 1 FROM public.cd_targets) THEN
                    RAISE EXCEPTION 'Se conservan los datos del diagnóstico; no se permite revertir una base con contenido.';
                  END IF;
                END $$;
                DROP TRIGGER cd_survey_immutable ON public.cd_surveys;
                DROP FUNCTION public.cd_guard_survey();
                """);
            migrationBuilder.DropTable(
                name: "cd_consents");

            migrationBuilder.DropTable(
                name: "cd_surveys");

            migrationBuilder.DropTable(
                name: "cd_targets");
        }
    }
}
