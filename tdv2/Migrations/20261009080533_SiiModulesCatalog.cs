using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class SiiModulesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sii_modulos",
                schema: "public",
                columns: table => new
                {
                    id_modulo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    desc_modulo = table.Column<string>(type: "text", nullable: false),
                    presente = table.Column<bool>(type: "boolean", nullable: false),
                    sincronizado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sii_modulos_pkey", x => x.id_modulo);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM sii_modulos)
                        OR EXISTS(SELECT 1 FROM formatos_ur WHERE enviado_en IS NOT NULL
                            OR jsonb_path_exists(contenido::jsonb, '$.sistemas[*].moduloSiiId ? (@ != "")')) THEN
                        RAISE EXCEPTION 'No se puede retirar el catálogo mientras existan módulos o referencias históricas.';
                    END IF;
                END $$;
                """);
            migrationBuilder.Sql("DELETE FROM sincronizacion_catalogos WHERE fuente='sii_modulos'");
            migrationBuilder.DropTable(
                name: "sii_modulos",
                schema: "public");
        }
    }
}
