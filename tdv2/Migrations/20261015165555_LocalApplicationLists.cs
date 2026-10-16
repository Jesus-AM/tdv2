using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class LocalApplicationLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aplicaciones_catalogo",
                schema: "public",
                columns: table => new
                {
                    categoria = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nombre = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    clave_lista = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    disponible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aplicaciones_catalogo", x => new { x.categoria, x.id });
                });

            migrationBuilder.CreateTable(
                name: "aplicaciones_importaciones",
                schema: "public",
                columns: table => new
                {
                    categoria = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    edicion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    importado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    resultado = table.Column<string>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aplicaciones_importaciones", x => x.categoria);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aplicaciones_catalogo_categoria_clave_lista",
                schema: "public",
                table: "aplicaciones_catalogo",
                columns: new[] { "categoria", "clave_lista" },
                unique: true);

            // Copia exclusivamente la réplica PostgreSQL existente. No toca respuestas ni la fuente.
            // Nada queda disponible hasta confirmar su presencia en la lista versionada.
            migrationBuilder.Sql("""
                INSERT INTO public.aplicaciones_catalogo(categoria,id,nombre,disponible)
                SELECT 'moviles',id_aplicacion_movil::text,nombre,false FROM public.sii_aplicaciones_moviles;
                INSERT INTO public.aplicaciones_catalogo(categoria,id,nombre,disponible)
                SELECT 'web',id_aplicacion_web::text,nombre,false FROM public.sii_aplicaciones_web;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS(SELECT 1 FROM public.aplicaciones_importaciones)
                        OR EXISTS(SELECT 1 FROM public.aplicaciones_catalogo WHERE clave_lista IS NOT NULL OR id LIKE 'local:%') THEN
                        RAISE EXCEPTION 'No se pueden retirar las listas después de importarlas: se perderían identificadores y referencias locales.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "aplicaciones_catalogo",
                schema: "public");

            migrationBuilder.DropTable(
                name: "aplicaciones_importaciones",
                schema: "public");
        }
    }
}
