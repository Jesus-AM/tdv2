namespace Tdv2.Synchronization;

public sealed record CatalogSchemaIssue(string codigo, string mensaje);
/// <summary>Diagnóstico local, sin abrir SII ni registrar destinos o cadenas de conexión.</summary>
public static class SiiCatalogSchema
{
    public const string Migration = "20261009080533_SiiModulesCatalog";
    public static async Task<CatalogSchemaIssue?> Inspect(SyncSql db, CancellationToken ct)
    {
        if (await db.Scalar("SELECT to_regclass('public.sii_modulos') IS NOT NULL", ct) is true) return null;
        var elsewhere = await db.Scalar("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE c.relname='sii_modulos' AND n.nspname<>'public' AND c.relkind IN ('r','p'))", ct) is true;
        var history = await db.Scalar("SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL", ct) is true;
        var readable = history && await db.Scalar("SELECT has_table_privilege(current_user,'public.\"__EFMigrationsHistory\"','SELECT')", ct) is true;
        var applied = readable && await db.Scalar("SELECT EXISTS(SELECT 1 FROM public.\"__EFMigrationsHistory\" WHERE \"MigrationId\"=$1)", ct, Migration) is true;
        var code = elsewhere ? "esquema_distinto" : applied ? "historial_incoherente" : history && !readable ? "historial_no_accesible" : "migracion_pendiente";
        var reason = elsewhere ? "Existe sii_modulos en otro esquema, pero falta public.sii_modulos."
            : applied ? "El historial registra la migración, pero falta public.sii_modulos."
            : history && !readable ? $"Falta public.sii_modulos. La cuenta de la aplicación no puede consultar el historial para comprobar {Migration}."
            : $"Falta public.sii_modulos y la migración {Migration} no consta aplicada en esta base.";
        return new(code, reason + " Verifica que ConnectionStrings:Tdv2 apunte a la base prevista y revisa sus migraciones EF Core. Si la migración está pendiente, aplícala explícitamente en esa base. Este diagnóstico corresponde a PostgreSQL local, no a la conexión SII.");
    }
    public static async Task Require(SyncSql db, CancellationToken ct)
    {
        if (await Inspect(db, ct) is { } issue) throw new SyncProblem($"[{issue.codigo}] {issue.mensaje}");
    }
}
