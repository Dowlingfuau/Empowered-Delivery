using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public sealed class MaturityNarrativeComposer
{
    public const string Source = "data/rules/maturity_bf01_narrative_v1.json";
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public MaturityNarrativeComposer(HttpClient http) => _http = http;

    public async Task<NarrativeContent> LoadAsync(SemanticPatternDefinition definition)
    {
        var json = await _http.GetStringAsync(Source);
        var content = JsonSerializer.Deserialize<NarrativeContent>(json, Options)
            ?? throw new InvalidOperationException("BF01 narrative content is empty.");
        if (content.PatternId != definition.PatternId || string.IsNullOrWhiteSpace(content.Version) ||
            string.IsNullOrWhiteSpace(content.Limitation) || content.Directions is null || content.Qualifications is null)
            throw new InvalidOperationException("BF01 narrative identity, version, or required content is missing.");
        foreach (var direction in new[] { "DirectionA", "DirectionB" })
        {
            if (!content.Directions.TryGetValue(direction, out var text) || text is null ||
                string.IsNullOrWhiteSpace(text.Observed) || string.IsNullOrWhiteSpace(text.Suspected) ||
                string.IsNullOrWhiteSpace(text.Meaning) || string.IsNullOrWhiteSpace(text.Action) ||
                !text.Observed.Contains("{ProductOwner}", StringComparison.Ordinal) ||
                !text.Observed.Contains("{Squad}", StringComparison.Ordinal))
                throw new InvalidOperationException($"BF01 narrative for {direction} is incomplete.");
        }
        foreach (var rule in definition.SemanticDefinition.QualificationRules)
        foreach (var direction in rule.Directions)
        {
            if (!content.Qualifications.TryGetValue(rule.NarrativeKey, out var entries) || entries is null ||
                !entries.TryGetValue(direction, out var text) || string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException($"Narrative guidance is missing for qualification {rule.RuleId}/{direction}.");
        }
        content.Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return content;
    }

    public MaturityDiagnosticFinding Compose(SemanticEvaluationResult evaluation,
        MaturityInterpretationProfile profile, NarrativeContent content)
    {
        if (evaluation.Outcome != SemanticOutcome.Candidate || evaluation.Direction is null)
            throw new InvalidOperationException("A BF01 candidate is required for narrative composition.");
        var direction = evaluation.Direction;
        var text = content.Directions[direction];
        var owner = evaluation.Evidence.Single(x => x.Coordinate == "ProductOwner.Value");
        var squad = evaluation.Evidence.Single(x => x.Coordinate == "Squad.Value");
        if (owner.Status != SemanticEvidenceStatus.Valid || squad.Status != SemanticEvidenceStatus.Valid ||
            owner.Snapshot is null || squad.Snapshot is null)
            throw new InvalidOperationException("BF01 core evidence is unavailable for narrative composition.");
        var observed = text.Observed.Replace("{ProductOwner}", owner.Snapshot.Headline, StringComparison.Ordinal)
            .Replace("{Squad}", squad.Snapshot.Headline, StringComparison.Ordinal);
        var qualifications = profile.Contributions.Where(x => x.Classification == "Qualifying").Select(x =>
        {
            if (x.QualificationRuleId is null || x.NarrativeKey is null || x.Boundary is null ||
                !content.Qualifications.TryGetValue(x.NarrativeKey, out var entries) ||
                !entries.TryGetValue(direction, out var guidance))
                throw new InvalidOperationException($"Narrative guidance is unavailable for {x.Coordinate}.");
            return new MaturityNarrativeQualification(x.QualificationRuleId, x.EvidenceId, x.Coordinate, guidance, x.Boundary);
        }).ToList();
        return new(Guid.NewGuid().ToString(), evaluation.Definition.PatternId, direction, Source,
            content.Version, content.Fingerprint, observed, text.Suspected, text.Meaning, text.Action,
            content.Limitation, qualifications, evaluation.Evidence)
        {
            OperationalDimensions = evaluation.Definition.OperationalDimensions.ToList()
        };
    }

    public sealed class NarrativeContent
    {
        public string PatternId { get; set; } = "";
        public string Version { get; set; } = "";
        public Dictionary<string, NarrativeText> Directions { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Qualifications { get; set; } = new();
        public string Limitation { get; set; } = "";
        [System.Text.Json.Serialization.JsonIgnore]
        public string Fingerprint { get; set; } = "";
    }

    public sealed class NarrativeText
    {
        public string Observed { get; set; } = "";
        public string Suspected { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string Action { get; set; } = "";
    }
}
