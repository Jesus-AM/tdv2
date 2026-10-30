using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Competencias.Migrations
{
    /// <inheritdoc />
    public partial class IndividualDigitalReopening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumVersion",
                table: "cd_surveys",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReopenedAt",
                table: "cd_surveys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "cd_surveys",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "cd_reopen_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Generation = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ActorEmployee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Employee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousVersion = table.Column<int>(type: "integer", nullable: false),
                    ResultVersion = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_reopen_operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cd_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Employee = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UnitId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Generation = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_submissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cd_submissions_Employee",
                table: "cd_submissions",
                column: "Employee");

            migrationBuilder.CreateIndex(
                name: "IX_cd_submissions_SurveyId_Revision",
                table: "cd_submissions",
                columns: new[] { "SurveyId", "Revision" },
                unique: true);
            migrationBuilder.Sql(IndividualDigitalReopeningSql.Up);
            migrationBuilder.Sql(IndividualDigitalResetHistorySql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La recuperación conserva recibos e históricos; no hay rollback destructivo automático.
            migrationBuilder.Sql("DO $$ BEGIN RAISE EXCEPTION 'Utilice recuperación explícita desde respaldo; esta migración conserva históricos y auditoría.'; END $$;");
            migrationBuilder.DropTable(
                name: "cd_reopen_operations");

            migrationBuilder.DropTable(
                name: "cd_submissions");

            migrationBuilder.DropColumn(
                name: "MinimumVersion",
                table: "cd_surveys");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                table: "cd_surveys");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "cd_surveys");
        }
    }
}
