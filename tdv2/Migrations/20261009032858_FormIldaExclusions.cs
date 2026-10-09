using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class FormIldaExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "formato_exclusiones_ilda",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ejercicio = table.Column<int>(type: "integer", nullable: false),
                    registro = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    efectivo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_formato_exclusiones_ilda", x => new { x.id_ur, x.ejercicio, x.registro });
                    table.ForeignKey(
                        name: "FK_formato_exclusiones_ilda_formatos_ur_id_ur",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "formatos_ur",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No perder retiros persistentes ni desmontar protección de un esquema con enviados.
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.formato_exclusiones_ilda)
                       OR EXISTS(SELECT 1 FROM public.formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'Conservar exclusiones e históricos: la reversión requiere revisión explícita.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "formato_exclusiones_ilda",
                schema: "public");
        }
    }
}
