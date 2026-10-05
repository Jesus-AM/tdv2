using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tdv2.Migrations
{
    /// <inheritdoc />
    public partial class InitialTdv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El historial de EF puede existir, pero no se adopta silenciosamente un esquema ajeno.
            migrationBuilder.Sql("""
                DO $bootstrap$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                        WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                          AND n.nspname NOT LIKE 'pg_toast%' AND n.nspname NOT LIKE 'pg_temp_%'
                          AND c.relname <> '__EFMigrationsHistory'
                          AND c.relkind IN ('r','p','S','v','m','f')
                          AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_class'::regclass AND d.objid=c.oid AND d.deptype='e')
                        UNION ALL SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                        WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                          AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_proc'::regclass AND d.objid=p.oid AND d.deptype='e')
                        UNION ALL SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace
                        WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND t.typtype IN ('e','d')
                          AND NOT EXISTS (SELECT 1 FROM pg_depend d WHERE d.classid='pg_type'::regclass AND d.objid=t.oid AND d.deptype='e')
                    ) THEN
                        RAISE EXCEPTION 'TDV2 requiere una base vacía. Conserve y revise los objetos existentes.' USING ERRCODE='55000';
                    END IF;
                END $bootstrap$;
                """);
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "activity_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role_id = table.Column<long>(type: "bigint", nullable: true),
                    ur = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ur2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    entity = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_registro = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    meta = table.Column<string>(type: "json", nullable: true),
                    ip = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("activity_logs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "colaboraciones_ur",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    num_empleado = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    id_ur_origen = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    id_ur_alcance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tipo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    nexo_concesion_id = table.Column<long>(type: "bigint", nullable: false),
                    nexo_rol_id = table.Column<long>(type: "bigint", nullable: false),
                    otorgado_por = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ur_otorgante = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    revocada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    retiro_central_pendiente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("colaboraciones_ur_pkey", x => x.id);
                    table.UniqueConstraint("colaboracion_concesion_alcance_unique", x => new { x.nexo_concesion_id, x.id_ur_alcance });
                });

            migrationBuilder.CreateTable(
                name: "ilda_informacion_area",
                schema: "public",
                columns: table => new
                {
                    id_origen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ur2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    informacion_generada = table.Column<string>(type: "text", nullable: true),
                    datos = table.Column<string>(type: "json", nullable: false),
                    presente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sincronizado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ilda_informacion_area_pkey", x => x.id_origen);
                });

            migrationBuilder.CreateTable(
                name: "ms_graph_tokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    expires = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ms_graph_tokens_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sincronizacion_catalogos",
                schema: "public",
                columns: table => new
                {
                    fuente = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    registros = table.Column<int>(type: "integer", nullable: false),
                    completada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sincronizacion_catalogos_pkey", x => x.fuente);
                });

            migrationBuilder.CreateTable(
                name: "sincronizacion_ejecuciones",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fuentes = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    solicitado_por = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    etapa = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    resultado = table.Column<string>(type: "json", nullable: false),
                    solicitada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    iniciada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    terminada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sincronizacion_ejecuciones_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sincronizaciones_institucionales",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    resumen = table.Column<string>(type: "json", nullable: false),
                    completada_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sincronizaciones_institucionales_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tdv2_oauth_attempts",
                schema: "public",
                columns: table => new
                {
                    state_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    browser_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    verifier = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tdv2_oauth_attempts_pkey", x => x.state_hash);
                });

            migrationBuilder.CreateTable(
                name: "tdv2_sessions",
                schema: "public",
                columns: table => new
                {
                    id_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ticket = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tdv2_sessions_pkey", x => x.id_hash);
                });

            migrationBuilder.CreateTable(
                name: "unidades_responsables_poa",
                schema: "public",
                columns: table => new
                {
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ejercicio = table.Column<int>(type: "integer", nullable: false),
                    cve_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    desc_ur = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    num_empleado = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    encargado = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_ur_pertenece = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    tipo_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    nivel_ur = table.Column<int>(type: "integer", nullable: true),
                    estatus_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    presente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sincronizado_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("unidades_responsables_poa_pkey", x => x.id_ur);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email_verified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    remember_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    microsoft_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    microsoft_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.UniqueConstraint("users_email_key", x => x.email);
                });

            migrationBuilder.CreateTable(
                name: "sincronizacion_configuracion",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    intervalo_minutos = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    hora = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "08:00"),
                    zona_horaria = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "America/Ciudad_Juarez"),
                    incluir_ilda = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    proxima_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    procesador_visto_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ejecucion_activa = table.Column<Guid>(type: "uuid", nullable: true),
                    propietario = table.Column<Guid>(type: "uuid", nullable: true),
                    reserva_hasta = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    actualizado_por = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sincronizacion_configuracion_pkey", x => x.id);
                    table.CheckConstraint("sincronizacion_configuracion_id_check", "id = 1");
                    table.ForeignKey(
                        name: "sincronizacion_configuracion_ejecucion_activa_fkey",
                        column: x => x.ejecucion_activa,
                        principalSchema: "public",
                        principalTable: "sincronizacion_ejecuciones",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tdv2_access_contexts",
                schema: "public",
                columns: table => new
                {
                    session_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    selection = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tdv2_access_contexts_pkey", x => x.session_hash);
                    table.ForeignKey(
                        name: "tdv2_access_contexts_session_hash_fkey",
                        column: x => x.session_hash,
                        principalSchema: "public",
                        principalTable: "tdv2_sessions",
                        principalColumn: "id_hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "formatos_ur",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_ur = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contenido = table.Column<string>(type: "json", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    porcentaje = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    actualizado_por = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("formatos_ur_pkey", x => x.id);
                    table.UniqueConstraint("formatos_ur_id_ur_key", x => x.id_ur);
                    table.ForeignKey(
                        name: "formatos_ur_id_ur_fkey",
                        column: x => x.id_ur,
                        principalSchema: "public",
                        principalTable: "unidades_responsables_poa",
                        principalColumn: "id_ur",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ilda_informacion_area_presente_index",
                schema: "public",
                table: "ilda_informacion_area",
                column: "presente");

            migrationBuilder.CreateIndex(
                name: "ilda_informacion_area_ur2_index",
                schema: "public",
                table: "ilda_informacion_area",
                column: "ur2");

            migrationBuilder.CreateIndex(
                name: "ms_graph_tokens_user_id_key",
                schema: "public",
                table: "ms_graph_tokens",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sincronizacion_configuracion_ejecucion_activa",
                schema: "public",
                table: "sincronizacion_configuracion",
                column: "ejecucion_activa");

            migrationBuilder.CreateIndex(
                name: "sincronizacion_ejecuciones_estado_index",
                schema: "public",
                table: "sincronizacion_ejecuciones",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "tdv2_oauth_attempts_expiry",
                schema: "public",
                table: "tdv2_oauth_attempts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "tdv2_sessions_expiry",
                schema: "public",
                table: "tdv2_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "unidades_responsables_poa_id_ur_pertenece_index",
                schema: "public",
                table: "unidades_responsables_poa",
                column: "id_ur_pertenece");

            migrationBuilder.CreateIndex(
                name: "unidades_responsables_poa_num_empleado_index",
                schema: "public",
                table: "unidades_responsables_poa",
                column: "num_empleado");

            migrationBuilder.CreateIndex(
                name: "users_microsoft_identity_unique",
                schema: "public",
                table: "users",
                columns: new[] { "microsoft_tenant_id", "microsoft_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "colaboraciones_ur",
                schema: "public");

            migrationBuilder.DropTable(
                name: "formatos_ur",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ilda_informacion_area",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ms_graph_tokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sincronizacion_catalogos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sincronizacion_configuracion",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sincronizaciones_institucionales",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tdv2_access_contexts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tdv2_oauth_attempts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "unidades_responsables_poa",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sincronizacion_ejecuciones",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tdv2_sessions",
                schema: "public");
        }
    }
}
