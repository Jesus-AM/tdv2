using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class ProcessParticipationAndPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "color",
                schema: "public",
                table: "formato_bloques",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "participante",
                schema: "public",
                table: "formato_bloques",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "configuracion_procesos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    niveles = table.Column<int[]>(type: "integer[]", nullable: false),
                    tipos_excluidos = table.Column<string[]>(type: "text[]", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_procesos", x => x.id);
                    table.CheckConstraint("configuracion_procesos_unica_ck", "id = 1 AND version > 0");
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "configuracion_procesos",
                columns: new[] { "id", "tipos_excluidos", "niveles", "actualizado_en", "version" },
                values: new object[] { 1, new[] { "N" }, new[] { 2, 3 }, null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No retirar política ni presencia de una instalación con formatos definitivos.
            // El operador debe conservar también el historial de esta migración al rechazar el rollback.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se puede retirar la configuración mientras existan formatos enviados';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "configuracion_procesos",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "public",
                table: "formato_bloques");

            migrationBuilder.DropColumn(
                name: "participante",
                schema: "public",
                table: "formato_bloques");
        }
    }
}
