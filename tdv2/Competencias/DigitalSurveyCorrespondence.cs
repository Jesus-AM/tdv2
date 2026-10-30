using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;

namespace Tdv2.Competencias;

public sealed record DigitalSurveyReference(string Employee, string Email, DateTimeOffset CreatedAt, bool Started, bool Finalized);
public sealed record DigitalPendingState(bool Finalized, bool InProgress, bool NotStarted);
public sealed record DigitalCorrespondence(Dictionary<string,long> Owners, Dictionary<long,DigitalPendingState> Pending, bool ProofAvailable = true);

// Estadística exclusivamente. No crea vínculos, no modifica autorización ni persiste
// decisiones por nombres/correos. Un empleado compartido jamás multiplica encuestas.
public static class DigitalSurveyCorrespondence
{
    public static async Task<DigitalCorrespondence> Resolve(DatabaseConnections connections, NexoPopulationPerson[] population, DigitalSurveyReference[] surveys, CancellationToken ct)
    {
        var owners = new Dictionary<string,long>(StringComparer.Ordinal); var proofAvailable=true;
        var pending = new Dictionary<long,DigitalPendingState>();
        void Pend(long id,DigitalPendingState value)
        {
            var old=pending.GetValueOrDefault(id);
            pending[id]=new(value.Finalized || old?.Finalized==true,value.InProgress || old?.InProgress==true,value.NotStarted || old?.NotStarted==true);
        }
        var groups = population.Where(p=>!string.IsNullOrWhiteSpace(p.Employee)).GroupBy(p=>p.Employee).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal);
        foreach(var person in population.Where(p=>string.IsNullOrWhiteSpace(p.Employee))) pending[person.UserId]=new(true,true,true);
        var captured = surveys.Where(s=>s.Started && groups.ContainsKey(s.Employee)).ToArray();
        var proofs = new Dictionary<string,List<(long Id,DateTimeOffset VerifiedAt)>>(StringComparer.Ordinal);
        if (captured.Length>0)
        {
            try
            {
                await using var c=await connections.Open("Tdv2",ct);
                await using var q=new NpgsqlCommand("""
                    SELECT lower(btrim(u.email)),l.nexo_user_id,
                      greatest(l.verified_at,u.created_at AT TIME ZONE 'UTC',u.updated_at AT TIME ZONE 'UTC')
                    FROM public.users u JOIN public.tdv2_identity_links l
                      ON l.microsoft_tenant_id=u.microsoft_tenant_id AND l.microsoft_id=u.microsoft_id
                    WHERE lower(btrim(u.email))=ANY($1) AND u.created_at IS NOT NULL AND u.updated_at IS NOT NULL
                    """,c);
                q.Parameters.AddWithValue(captured.Select(s=>s.Email.Trim().ToLowerInvariant()).Distinct().ToArray());
                await using var r=await q.ExecuteReaderAsync(ct);
                while(await r.ReadAsync(ct))
                {
                    var email=r.GetString(0);
                    if(!proofs.TryGetValue(email,out var list)) proofs[email]=list=[];
                    list.Add((r.GetInt64(1),r.GetFieldValue<DateTimeOffset>(2)));
                }
            }
            catch(Exception e) when(e is not OperationCanceledException)
            { proofAvailable=false; /* La ausencia de fuente no se transforma en una atribución. */ }
        }
        foreach(var survey in surveys.Where(s=>s.Started))
        {
            if(!groups.TryGetValue(survey.Employee,out var members)) continue;
            // Además del empleado, exige identidad Microsoft coincidente con un vínculo
            // canónico acreditado ANTES de crear la captura, con la fila local sin
            // cambios posteriores (evita atribuir por un correo reasignado). El correo localiza
            // ese vínculo; nunca constituye por sí solo la prueba ni un permiso.
            var known=proofs.GetValueOrDefault(survey.Email.Trim().ToLowerInvariant());
            var candidates=known?
                .Where(p=>p.VerifiedAt<=survey.CreatedAt).Select(p=>p.Id).Distinct().ToArray() ?? [];
            if(candidates.Length==1)
            {
                if(members.Any(p=>p.UserId==candidates[0])) owners[survey.Employee]=candidates[0];
                else if(population.Any(p=>p.UserId==candidates[0]))
                {
                    // Vínculo y empleado publicado se contradicen para un usuario
                    // presente: no clasificarlo como Sin iniciar por perder el cruce.
                    var state=new DigitalPendingState(survey.Finalized,!survey.Finalized,true);
                    Pend(candidates[0],state); foreach(var person in members) Pend(person.UserId,state);
                }
                // Prueba de un dueño fuera del conjunto: no se asigna a otro usuario.
                continue;
            }
            // Correspondencia institucional única anterior, siempre que no haya
            // evidencia estable contradictoria. No sirve para escoger entre cuentas.
            if(members.Length==1 && members[0].EmployeeUnique && (known is null || known.All(p=>p.Id==members[0].UserId)))
            { owners[survey.Employee]=members[0].UserId; continue; }
            foreach(var person in members) Pend(person.UserId,new(survey.Finalized,!survey.Finalized,true));
        }
        // Protección adicional contra varios intentos atribuidos al mismo canónico.
        foreach(var duplicate in owners.GroupBy(p=>p.Value).Where(g=>g.Count()>1).ToArray())
        {
            foreach(var row in duplicate) owners.Remove(row.Key);
            pending[duplicate.Key]=new(true,true,true);
        }
        return new(owners,pending,proofAvailable);
    }
}
