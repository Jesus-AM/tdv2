using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class SiiApplicationsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sii_aplicaciones_moviles",
                schema: "public",
                columns: table => new
                {
                    id_aplicacion_movil = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    definicion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emp_responsable = table.Column<int>(type: "integer", nullable: true),
                    prog_responsable = table.Column<int>(type: "integer", nullable: true),
                    id_ur = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    num_empleado_dir_gral = table.Column<int>(type: "integer", nullable: true),
                    presente = table.Column<bool>(type: "boolean", nullable: false),
                    sincronizado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sii_aplicaciones_moviles_pkey", x => x.id_aplicacion_movil);
                });

            migrationBuilder.CreateTable(
                name: "sii_aplicaciones_web",
                schema: "public",
                columns: table => new
                {
                    id_aplicacion_web = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    definicion = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    emp_responsable = table.Column<int>(type: "integer", nullable: true),
                    prog_responsable = table.Column<int>(type: "integer", nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    num_empleado_dir_gral = table.Column<int>(type: "integer", nullable: true),
                    id_aplicacion_web_pertenece = table.Column<int>(type: "integer", nullable: true),
                    presente = table.Column<bool>(type: "boolean", nullable: false),
                    sincronizado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sii_aplicaciones_web_pkey", x => x.id_aplicacion_web);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.sii_aplicaciones_moviles)
                        OR EXISTS(SELECT 1 FROM public.sii_aplicaciones_web)
                        OR EXISTS(SELECT 1 FROM public.formatos_ur WHERE
                            jsonb_path_exists(contenido::jsonb, '$.sistemas[*].aplicacionesMoviles[*]')
                            OR jsonb_path_exists(contenido::jsonb, '$.sistemas[*].aplicacionesWeb[*]'))
                        OR EXISTS(SELECT 1 FROM public.formato_envios_etapas WHERE
                            jsonb_path_exists(instantanea::jsonb, '$.sistemas[*].aplicacionesMoviles[*]')
                            OR jsonb_path_exists(instantanea::jsonb, '$.sistemas[*].aplicacionesWeb[*]')) THEN
                        RAISE EXCEPTION 'No se pueden retirar los catálogos mientras existan aplicaciones o referencias históricas.';
                    END IF;
                END $$;
                DELETE FROM public.sincronizacion_catalogos WHERE fuente IN ('sii_moviles','sii_web');
                """);
            migrationBuilder.DropTable(
                name: "sii_aplicaciones_moviles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sii_aplicaciones_web",
                schema: "public");
        }
    }
}
