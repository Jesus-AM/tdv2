using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;

namespace Tdv2.NativeVerification;

// Sólo vistas efectivas de cada aplicación y cadena local. Nunca escribe datos personales en el informe.
internal static class SiteProfileReadOnly
{
    internal static async Task<int> Run(string lookup, IConfiguration? configuration = null, TextWriter? output = null)
    {
        output ??= Console.Out;
        var failed = false;
        var people = new List<(long Id, Person Person)>();
        var byId = long.TryParse(lookup, out var id);
        try
        {
            configuration ??= DigitalPopulationReadOnly.LocalConfiguration();
            var raw = configuration.GetConnectionString("Tdv2") ?? throw new InvalidOperationException();
            await using var local = new NpgsqlConnection(DigitalPopulationReadOnly.ReadOnlyConnection(raw));
            await local.OpenAsync();
            await using var link = new NpgsqlCommand("""
                SELECT l.nexo_user_id FROM public.users u JOIN public.tdv2_identity_links l
                  ON l.microsoft_tenant_id=u.microsoft_tenant_id AND l.microsoft_id=u.microsoft_id WHERE
                """ + (byId ? " l.nexo_user_id=$1" : " u.email=$1"), local);
            link.Parameters.AddWithValue(byId ? (object)id : PostgresNexoProfiles.Email(lookup.Trim().ToLowerInvariant()));
            var linked = await link.ExecuteScalarAsync();
            if (linked is long canonical) { id = canonical; byId = true; }
            output.WriteLine(JsonSerializer.Serialize(new { vinculoLocalConfirmado = linked is long, busqueda = byId ? "usuario_id" : "email" }));
        }
        catch (Exception error)
        {
            failed = true;
            output.WriteLine(JsonSerializer.Serialize(new { phase = "vinculo_local", error = DigitalPopulationReadOnly.SafeError(error) }));
        }
        foreach (var site in new[] { "Nexo", "CompetenciasDigitalesNexo" })
        {
            var phase = "configuracion";
            try
            {
                configuration ??= DigitalPopulationReadOnly.LocalConfiguration();
                string ReadOnly(string key) => DigitalPopulationReadOnly.ReadOnlyConnection(
                    configuration.GetConnectionString(key) ?? throw new InvalidOperationException());
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                    ["ConnectionStrings:" + site] = ReadOnly(site), ["ConnectionStrings:Tdv2"] = ReadOnly("Tdv2") }).Build();
                var connections = new DatabaseConnections(config);
                var expected = long.TryParse(configuration["CompetenciasDigitales:Nexo:ApplicationId"], out var appId) ? (long?)appId : null;
                var source = new PostgresNexoProfiles(connections, NullLogger<PostgresNexoProfiles>.Instance, site, expected);
                phase = "publicacion";
                var publication = await source.ApplicationId(default);
                output.WriteLine(JsonSerializer.Serialize(new { site, publication }));
                phase = "usuario_efectivo";
                await using var publicationConnection = await connections.Open(site);
                await using var query = new NpgsqlCommand("""
                    SELECT u.usuario_id,u.email,u.nombre,u.num_empleado,u."ID_UR",u."CVE_UR",u.origen_ur,u."ID_UR_SUPERIOR",u.tipo_cuenta,
                        (SELECT count(*) FROM public.nexo_usuario_rol r WHERE r.usuario_aplicacion_id=u.usuario_aplicacion_id AND r.email=u.email)
                    FROM public.nexo_usuarios u WHERE
                    """ + (byId ? " u.usuario_id=$1" : " u.email=$1") + " LIMIT 2", publicationConnection);
                query.Parameters.AddWithValue(byId ? (object)id : PostgresNexoProfiles.Email(lookup.Trim().ToLowerInvariant()));
                await using var rows = await query.ExecuteReaderAsync();
                if (!await rows.ReadAsync())
                {
                    output.WriteLine(JsonSerializer.Serialize(new { site, phase, estado = "sin_fila_efectiva" }));
                    continue;
                }
                string? Field(int index) => rows.IsDBNull(index) ? null : rows.GetString(index);
                var canonical = rows.GetInt64(0);
                var person = new Person(rows.GetString(1), Field(2) ?? "", Field(3), Field(4), Field(6) ?? "sin_ur", Field(8) ?? "", Field(5), Field(7));
                var roles = rows.GetInt64(9);
                if (await rows.ReadAsync())
                {
                    failed = true;
                    output.WriteLine(JsonSerializer.Serialize(new { site, phase, estado = "varias_filas_para_la_persona" }));
                    continue;
                }
                people.Add((canonical, person));
                output.WriteLine(JsonSerializer.Serialize(new { site, phase, usuarioCanonico = canonical, nombrePresente = person.Name.Length > 0,
                    empleadoTextualValido = UnitDirectory.Employee(person) is not null, rolesEfectivos = roles,
                    ID_UR = person.UnitId, CVE_UR = person.UnitCode, ID_UR_SUPERIOR = person.PublishedParent, origen_ur = person.Origin }));
                phase = "catalogo_tdv2";
                await using var catalog = await connections.Open("Tdv2");
                await using var chain = new NpgsqlCommand("""
                    WITH RECURSIVE cadena AS (
                        SELECT u.*, ARRAY[id_ur::text] recorrido FROM public.unidades_responsables_poa u WHERE id_ur=$1
                        UNION ALL
                        SELECT p.*, array_append(h.recorrido,p.id_ur::text) FROM cadena h
                          JOIN public.unidades_responsables_poa p ON p.id_ur=h.id_ur_pertenece
                          WHERE NOT p.id_ur=ANY(h.recorrido))
                    SELECT jsonb_build_object('ejercicioVigente',(SELECT max(ejercicio) FROM public.unidades_responsables_poa WHERE presente),
                        'cadena',COALESCE(jsonb_agg(jsonb_build_object('id',id_ur,'clave',cve_ur,'nivel',nivel_ur,'superior',id_ur_pertenece,
                        'ejercicio',ejercicio,'presente',presente,'estado',estatus_ur) ORDER BY cardinality(recorrido)), '[]'::jsonb))::text FROM cadena
                    """, catalog);
                chain.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Text, (object?)person.UnitId ?? DBNull.Value);
                output.WriteLine((string)(await chain.ExecuteScalarAsync())!);
            }
            catch (Exception e)
            {
                failed = true;
                output.WriteLine(JsonSerializer.Serialize(new { site, phase, error = DigitalPopulationReadOnly.SafeError(e) }));
            }
        }
        if (people.Count == 2)
        {
            var (firstId, a) = people[0]; var (secondId, b) = people[1];
            output.WriteLine(JsonSerializer.Serialize(new { mismoCanonico = firstId == secondId, mismoCorreo = a.Email == b.Email,
                mismoNombre = a.Name == b.Name, mismoEmpleado = a.Employee == b.Employee, mismaAdscripcion = a.UnitId == b.UnitId && a.UnitCode == b.UnitCode && a.Origin == b.Origin && a.PublishedParent == b.PublishedParent }));
        }
        return failed ? 1 : 0;
    }
}
