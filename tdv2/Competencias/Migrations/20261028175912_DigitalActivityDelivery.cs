using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Competencias.Migrations
{
    /// <inheritdoc />
    public partial class DigitalActivityDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La edición de borradores requiere conservar exactamente la captura anterior.
            // No cambia la prohibición de editar/borrar definitivas ni la identidad del intento.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.cd_guard_survey() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                BEGIN
                  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se permite eliminar intentos del diagnóstico.'; END IF;
                  IF OLD."CompletedAt" IS NOT NULL THEN RAISE EXCEPTION 'La encuesta definitiva es inmutable.'; END IF;
                  IF OLD."Employee" IS DISTINCT FROM NEW."Employee" OR OLD."Id" IS DISTINCT FROM NEW."Id"
                    THEN RAISE EXCEPTION 'No se permite sustituir el participante de un intento.'; END IF;
                  IF OLD."InstrumentVersion" IS DISTINCT FROM NEW."InstrumentVersion" OR OLD."InstrumentJson" IS DISTINCT FROM NEW."InstrumentJson" THEN
                    IF OLD."InstrumentVersion" <> 'uacj-digital-2026-10-27.1'
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
                """);
            migrationBuilder.CreateTable(
                name: "cd_activity_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Session = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LocalRecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DuplicateReceipt = table.Column<bool>(type: "boolean", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Failure = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_activity_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cd_activity_events_Key",
                table: "cd_activity_events",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cd_activity_events_ReceivedAt_NextAttemptAt",
                table: "cd_activity_events",
                columns: new[] { "ReceivedAt", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS(SELECT 1 FROM public.cd_activity_events)
                    OR EXISTS(SELECT 1 FROM public.cd_surveys WHERE "InstrumentVersion"='uacj-digital-2026-10-28.1')
                    THEN RAISE EXCEPTION 'Conservar eventos, recibos y encuestas de la nueva edición antes de retirar el esquema'; END IF;
                END $$;
                CREATE OR REPLACE FUNCTION public.cd_guard_survey() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $$
                BEGIN
                  IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'No se permite eliminar intentos del diagnóstico.'; END IF;
                  IF OLD."CompletedAt" IS NOT NULL THEN RAISE EXCEPTION 'La encuesta definitiva es inmutable.'; END IF;
                  IF OLD."Employee" IS DISTINCT FROM NEW."Employee" OR OLD."Id" IS DISTINCT FROM NEW."Id"
                    OR OLD."InstrumentVersion" IS DISTINCT FROM NEW."InstrumentVersion" OR OLD."InstrumentJson" IS DISTINCT FROM NEW."InstrumentJson"
                    THEN RAISE EXCEPTION 'No se permite sustituir el participante o instrumento de un intento.'; END IF;
                  RETURN NEW;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "cd_activity_events");
        }
    }
}
