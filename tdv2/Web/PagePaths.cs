namespace Tdv2.Web;

public static class PagePaths
{
    public static bool IsPage(PathString path) => Tdv2.Competencias.DigitalRoutes.Page(path) || path == "/inicio" || path.StartsWithSegments("/formatos")
        || path == "/colaboradores" || path.StartsWithSegments("/configuracion") || path == "/vista-prueba"
        || path == "/actuar-como-usuario" || path == "/acceso-restringido" || path == ApplicationEntry.Choices;
}
