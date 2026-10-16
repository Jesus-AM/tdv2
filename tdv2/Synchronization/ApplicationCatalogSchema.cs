using Tdv2.Domain;

namespace Tdv2.Synchronization;

public static class ApplicationCatalogSchema
{
    // Los identificadores proceden exclusivamente del contrato interno, nunca de la petición.
    public static async Task<CatalogSchemaIssue?> Inspect(SyncSql db, ApplicationKind kind, CancellationToken ct)
    {
        if (await db.Scalar("SELECT to_regclass('public.aplicaciones_catalogo') IS NOT NULL AND to_regclass('public.aplicaciones_importaciones') IS NOT NULL", ct) is true) return null;
        return new("catalogo_aplicaciones_pendiente",
            "Falta el esquema del catálogo local de aplicaciones. Verifica la base de ConnectionStrings:Tdv2 y aplica la migración 20261015165555_LocalApplicationLists antes de importar las listas. Este catálogo no utiliza SQL Server.");
    }
    public static async Task Require(SyncSql db, ApplicationKind kind, CancellationToken ct)
    {
        if (await Inspect(db, kind, ct) is { } issue) throw new SyncProblem(issue.mensaje);
    }
}
