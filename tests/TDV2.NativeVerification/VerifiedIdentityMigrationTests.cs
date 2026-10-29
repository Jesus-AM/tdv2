using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Tdv2.NativeVerification;

internal static class VerifiedIdentityMigrationTests
{
    internal static async Task Run(string connection, Func<string, string, Task<object?>> sql, Action<bool, string> check)
    {
        await using var db = MigrationTests.Context(connection);
        var migrator = db.GetService<IMigrator>(); const string previous = "20261016190000_RemoveUsedMedia";
        await migrator.MigrateAsync(previous);
        await sql(connection, "INSERT INTO users(email,name,password,microsoft_tenant_id,microsoft_id) VALUES('identity@uacj.mx','Persona sintética','not-login','aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb')");
        var before = await sql(connection, "SELECT row_to_json(u)::text FROM users u");
        await db.Database.MigrateAsync();
        check(Equals(before, await sql(connection, "SELECT row_to_json(u)::text FROM users u")) && Convert.ToInt64(await sql(connection, "SELECT count(*) FROM tdv2_identity_links")) == 0,
            "identidad: migración aditiva conserva usuarios y no acredita vínculos por correo");
        await sql(connection, "INSERT INTO tdv2_identity_links VALUES('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',20,now(),'operador sintético','EVIDENCIA-SINTETICA')");
        foreach (var insert in new[] {
            "INSERT INTO tdv2_identity_links SELECT * FROM tdv2_identity_links",
            "INSERT INTO tdv2_identity_links SELECT microsoft_tenant_id,'dddddddd-dddd-4ddd-8ddd-dddddddddddd'::uuid,nexo_user_id,verified_at,verified_by,verification_reference FROM tdv2_identity_links" })
        {
            var rejected = false; try { await sql(connection, insert); } catch (PostgresException e) when (e.SqlState == "23505") { rejected = true; }
            check(rejected, "identidad: no admite múltiples correspondencias para Microsoft ni Nexo");
        }
        await db.Database.MigrateAsync();
        check(Convert.ToInt64(await sql(connection, "SELECT count(*) FROM tdv2_identity_links")) == 1, "identidad: repetir Up conserva acreditación");
        await migrator.MigrateAsync(previous);
        check(Equals(before, await sql(connection, "SELECT row_to_json(u)::text FROM users u")) && await sql(connection, "SELECT to_regclass('public.tdv2_identity_links')::text") is DBNull,
            "identidad: Down sólo retira la tabla nueva; usuarios y datos previos intactos");
        await db.Database.MigrateAsync();
        check(Convert.ToInt64(await sql(connection, "SELECT count(*) FROM tdv2_identity_links")) == 0, "identidad: Up tras Down no inventa correspondencias; requiere restaurar evidencia");
    }
}
