using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class InitializePausedSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sólo configuración inicial. No carga usuarios, catálogos ni permisos; no inicia procesadores.
            migrationBuilder.InsertData(schema: "public", table: "sincronizacion_configuracion", column: "id", value: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(schema: "public", table: "sincronizacion_configuracion", keyColumn: "id", keyValue: (short)1);
        }
    }
}
