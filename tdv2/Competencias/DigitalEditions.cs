using System.Text.Json.Nodes;
namespace Tdv2.Competencias;

public sealed record DigitalEditionNotice(string Previous, string Current, string[] RemovedTools, string[] ReviewFields);
public static class DigitalEditions
{
    public static bool RequiresUpdate(DigitalInstrument current, DigitalSurvey? survey) => survey is { CompletedAt: null, ReopenedAt: null } && survey.InstrumentVersion != current.Version;
    public static DigitalEditionNotice? Notice(DigitalInstrument current, DigitalSurvey? survey)
    {
        if (!RequiresUpdate(current, survey)) return null;
        var old = new DigitalInstrument(survey!.InstrumentJson);
        var profile = JsonNode.Parse(survey.ProfileJson)!.AsObject();
        var options = current.Resource["profile"]!.AsArray();
        var review = options.Where(f => profile[f!["id"]!.ToString()] is { } value &&
            (value is JsonArray a ? a.Any(v => !f["options"]!.AsArray().Any(o => JsonNode.DeepEquals(o, v))) : !f["options"]!.AsArray().Any(o => JsonNode.DeepEquals(o, value))))
            .Select(f => f!["label"]!.ToString() + ": " + profile[f["id"]!.ToString()]!.ToJsonString()).ToArray();
        return new(old.Version, current.Version, old.Blocks.SelectMany(b => b.Tools).Where(t => !current.Blocks.SelectMany(b => b.Tools).Any(n => n.Id == t.Id)).Select(t => t.N).ToArray(), review);
    }
    public static void Upgrade(DigitalInstrument current, DigitalSurvey survey, DateTimeOffset now)
    {
        if (survey.CompletedAt is not null) throw new Tdv2.Domain.DomainProblem(409, "Una encuesta finalizada no cambia de instrumento.");
        if (!current.Resource["supersedes"]!.AsArray().Any(v => v!.ToString() == survey.InstrumentVersion))
            throw new Tdv2.Domain.DomainProblem(409, "Esta edición requiere una revisión de compatibilidad antes de actualizar el borrador.");
        var previous = new JsonObject { ["instrument"] = JsonNode.Parse(survey.InstrumentJson), ["profile"] = JsonNode.Parse(survey.ProfileJson),
            ["answers"] = JsonNode.Parse(survey.AnswersJson), ["version"] = survey.Version, ["updatedAt"] = now };
        var profile = JsonNode.Parse(survey.ProfileJson)!.AsObject();
        foreach (var field in current.Resource["profile"]!.AsArray())
        {
            var key = field!["id"]!.ToString(); var options = field["options"]!.AsArray();
            if (profile[key] is JsonArray a) profile[key] = new JsonArray(a.Where(v => options.Any(o => JsonNode.DeepEquals(o, v))).Select(v => v?.DeepClone()).ToArray());
            else if (profile[key] is { } value && !options.Any(o => JsonNode.DeepEquals(o, value))) profile.Remove(key);
        }
        var answers = JsonNode.Parse(survey.AnswersJson)!.AsObject();
        var keys = current.Blocks.SelectMany(b => b.Tools).Select(t => t.Id).ToHashSet();
        foreach (var key in answers.Select(p => p.Key).Where(k => !keys.Contains(k)).ToArray()) answers.Remove(key);
        var snapshot = current.Resource.DeepClone().AsObject(); snapshot["previousDraft"] = previous;
        survey.ProfileJson = profile.ToJsonString(); survey.AnswersJson = answers.ToJsonString(); survey.InstrumentJson = snapshot.ToJsonString();
        survey.InstrumentVersion = current.Version; survey.UpdatedAt = now; survey.Version++;
    }
}
