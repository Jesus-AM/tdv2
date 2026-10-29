using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class VerifiedInstitutionalIdentityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tdv2_identity_links",
                schema: "public",
                columns: table => new
                {
                    microsoft_tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    microsoft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nexo_user_id = table.Column<long>(type: "bigint", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    verification_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tdv2_identity_links", x => new { x.microsoft_tenant_id, x.microsoft_id });
                    table.CheckConstraint("identity_link_verified", "nexo_user_id > 0 AND length(trim(verified_by)) > 0 AND length(trim(verification_reference)) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tdv2_identity_links_nexo_user_id",
                schema: "public",
                table: "tdv2_identity_links",
                column: "nexo_user_id",
                unique: true);
            // Sin concesiones públicas. El operador otorga SELECT/INSERT al rol web;
            // UPDATE/DELETE quedan reservados al mantenimiento de identidad acreditado.
            migrationBuilder.Sql("REVOKE ALL ON TABLE public.tdv2_identity_links FROM PUBLIC;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tdv2_identity_links",
                schema: "public");
        }
    }
}
