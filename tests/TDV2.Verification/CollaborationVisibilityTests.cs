using Tdv2.Domain;
using Tdv2.Domain.Entities;
namespace Tdv2.Verification;

internal static class CollaborationVisibilityTests
{
    internal static Profile Person(string unit = "A4") => new(new("colabora@uacj.mx", "Persona de prueba", "0010", unit),
        [new(30, "colaborador_local", "Apoyo"), new(31, "colaborador_dependencias", "Equipo")],
        [new(1, ModuleAccess.Procedures, "Procedimientos", "/inicio")]);
    internal static UnitCollaboration Link(long id, string kind = "local", string grantor = "A", string scope = "A3") => new()
    {
        Id = id, Email = Person().User.Email, Name = Person().User.Name, EmployeeNumber = "0010", OriginUnitId = "A4",
        ScopeUnitId = scope, Kind = kind, NexoGrantId = id, NexoRoleId = kind == "local" ? 30 : 31, GrantorUnitId = grantor
    };
    internal static CollaborationIdentity Identity(params Grant[] grants) => new(Person(), grants);
    internal static Grant Grant(UnitCollaboration link) => new(link.NexoGrantId, link.NexoRoleId, link.OriginUnitId, "aplicacion");
    internal static void Register(List<(string Name, Func<Task> Run)> cases)
    {
        var directory = new UnitDirectory(Fixtures.Units);
        IReadOnlyList<CollaborationListItem> List(string root, UnitCollaboration[] links, CollaborationIdentity[] people, bool readOnly = false) =>
            CollaborationListing.Build(directory, new Dictionary<string, Unit> { [root] = directory.Get(root)! }, links, people, readOnly);
        void Check(bool condition) { if (!condition) throw new Exception("Visibilidad de colaboración inesperada."); }
        void Test(string name, Action run) => cases.Add(("Colaboradores/" + name, () => { run(); return Task.CompletedTask; }));
        Test("alta central aparece en nivel 2 por rol real y alcance vigente, sin vínculo ni retiro local", () =>
        {
            var person = Identity(new Tdv2.Domain.Grant(100, 30, "B", "central")); // origen histórico distinto de alcance vigente
            var rows = List("A", [], [person]); Check(rows.Count == 1 && rows[0].id_ur_alcance == "A3" && rows[0].origen == "central" && !rows[0].puedeRetirar && rows[0].id is null);
            Check(List("B", [], [person]).Count == 0);
        });
        Test("dependencias otorgada por nivel 2 no se propaga a nivel 3", () =>
        {
            var link = Link(1, "dependencias", "A", "A"); var people = new[] { Identity(Grant(link)) };
            Check(List("A", [link], people).Single().puedeRetirar && List("A3", [link], people).Count == 0);
        });
        Test("local otorgada por nivel 2 se ve en ambos; nivel 3 no adquiere retiro", () =>
        {
            var link = Link(2); var people = new[] { Identity(Grant(link)) };
            Check(List("A", [link], people).Single().puedeRetirar && !List("A3", [link], people).Single().puedeRetirar);
            Check(List("A3", [link], people).Single().id_ur_alcance == "A3"); // no A4 (adscripción)
        });
        Test("local otorgada por nivel 3 se ve en su nivel 2; superior no adquiere retiro", () =>
        {
            var link = Link(3, grantor: "A3"); var people = new[] { Identity(Grant(link)) };
            Check(!List("A", [link], people).Single().puedeRetirar && List("A3", [link], people).Single().puedeRetirar);
        });
        Test("ramas distintas, nivel 2 anidado y jerarquía actual no filtran personas", () =>
        {
            var link = Link(4); var people = new[] { Identity(Grant(link)) }; Check(List("B", [link], people).Count == 0);
            var nested = new UnitDirectory([.. Fixtures.Units, new("X", "x", "Otra raíz", 2, "A", 2026), new("X3", "x3", "Dependencia", 3, "X", 2026)]);
            Check(!CollaborationRules.Visible(nested, [nested.Get("A")!], "X3", "local"));
            var moved = new UnitDirectory(Fixtures.Units.Select(u => u.Id == "A3" ? u with { Parent = "B" } : u));
            Check(CollaborationListing.Build(moved, new Dictionary<string, Unit> { ["A"] = moved.Get("A")! }, [link], people, false).Count == 0);
        });
        Test("una persona con asignaciones diferentes conserva alcances sin duplicar concesión", () =>
        {
            var local = Link(5); var branch = Link(6, "dependencias", "A", "A");
            var rows = List("A", [local, local, branch], [Identity(Grant(local), Grant(branch), new(100, 30, "A4", "central"), new(100, 30, "A4", "central"))]);
            Check(rows.Count == 3 && rows.Select(r => r.key).Distinct().Count() == 3 && rows.Select(r => r.id_ur_alcance).Distinct().Count() == 2);
        });
        Test("revocación local, suspensión o concesión ausente no figuran como efectivas", () =>
        {
            var link = Link(7); var people = new[] { Identity(Grant(link)) };
            Check(List("A", [link], []).Count == 0); // publicación no devuelve identidad suspendida
            Check(List("A", [link], [Identity()]).Count == 0);
            link.RevokedAt = DateTime.UtcNow; Check(List("A", [link], people).Count == 0);
            link.CentralRemovalPending = true;
            Check(List("A", [link], []).Single() is { revocada: true, pendiente: true, puedeRetirar: true });
            Check(List("A3", [link], people).Count == 0);
        });
        Test("central sin adscripción/empleado/rol/concesión/módulo no recibe alcance inventado", () =>
        {
            var identity = Identity(new Tdv2.Domain.Grant(100, 30, "A", "central"));
            foreach (var person in new[] { Person() with { User = Person().User with { UnitId = null } }, Person() with { User = Person().User with { Employee = null } },
                Person() with { Roles = [new(30, "otro", "colaborador local")] }, Person() with { Modules = [] } })
                Check(List("A", [], [identity with { Profile = person }]).Count == 0);
            Check(List("A", [], [Identity(new Tdv2.Domain.Grant(100, 30, "A", ""))]).Count == 0);
            Check(List("A", [], [Identity(new Tdv2.Domain.Grant(100, 31, "A", "central"))]).Single().id_ur_alcance == "A");
        });
        Test("consulta y representación sin escritura no adquieren retiro", () =>
        {
            var link = Link(8); Check(!List("A", [link], [Identity(Grant(link))], true).Single().puedeRetirar);
            Check(!CollaborationListing.Build(directory, new Dictionary<string, Unit> { ["A"] = directory.Get("A")! },
                [link], [Identity(Grant(link))], false, new HashSet<string>()).Single().puedeRetirar);
        });
        Test("traslado, empleado, rol y origen incompatibles invalidan cada vínculo", () =>
        {
            var link = Link(9); var identity = Identity(Grant(link));
            Check(List("A", [link], [identity with { Profile = Person("B3") }]).Count == 0);
            Check(List("A", [link], [identity with { Profile = Person() with { User = Person().User with { Employee = "10" } } }]).Count == 0);
            Check(List("A", [link], [Identity(Grant(link) with { RoleId = 31 })]).Count == 0);
            Check(List("A", [link], [Identity(Grant(link) with { Origin = "central" })]).All(r => r.origen == "central"));
        });
    }
}
