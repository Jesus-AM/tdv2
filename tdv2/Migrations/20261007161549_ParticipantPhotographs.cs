using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantPhotographs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "usuario_participante_id",
                schema: "public",
                table: "formato_bloques",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_formato_bloques_usuario_participante_id",
                schema: "public",
                table: "formato_bloques",
                column: "usuario_participante_id");

            migrationBuilder.AddForeignKey(
                name: "FK_formato_bloques_users_usuario_participante_id",
                schema: "public",
                table: "formato_bloques",
                column: "usuario_participante_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mantener el mismo límite de reversión de captura: no iniciar un rollback parcial
            // del historial de colaboración cuando la instalación contiene formatos enviados.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM formatos_ur WHERE enviado_en IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se puede retirar la presencia mientras existan formatos enviados';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_formato_bloques_users_usuario_participante_id",
                schema: "public",
                table: "formato_bloques");

            migrationBuilder.DropIndex(
                name: "IX_formato_bloques_usuario_participante_id",
                schema: "public",
                table: "formato_bloques");

            migrationBuilder.DropColumn(
                name: "usuario_participante_id",
                schema: "public",
                table: "formato_bloques");
        }
    }
}
