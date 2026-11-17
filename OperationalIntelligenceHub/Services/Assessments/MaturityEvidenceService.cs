using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services;

public sealed class MaturityEvidenceService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, VersionData> _versions = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MaturityEvidenceService(HttpClient http) => _http = http;

    public string CurrentVersion { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var manifest = await _http.GetStringAsync("data/maturity/current.json");
        var version = JsonSerializer.Deserialize<VersionManifest>(manifest, JsonOptions)?.CurrentVersion;
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("The current maturity catalogue version is missing.");

        var archived = await LoadVersionAsync(version);
        var currentCatalogue = await _http.GetStringAsync("data/diagnostics/maturity_behaviours.json");
        var currentMapping = await _http.GetStringAsync("data/signals/maturity.json");
        if (!JsonNode.DeepEquals(JsonNode.Parse(currentCatalogue), JsonNode.Parse(archived.CatalogueJson)) ||
            !JsonNode.DeepEquals(JsonNode.Parse(currentMapping), JsonNode.Parse(archived.MappingJson)))
            throw new InvalidOperationException("The current maturity catalogue or mapping differs from its retained version. Create a new version before changing it.");
        CurrentVersion = version;
    }

    public async Task<MaturityBehaviourEvidence> CaptureAsync(string lens, string theme, BehaviourOption option)
    {
        if (string.IsNullOrWhiteSpace(CurrentVersion))
            throw new InvalidOperationException("The maturity catalogue version has not been loaded.");
        var data = await LoadVersionAsync(CurrentVersion);
        var mapping = FindMapping(data, lens, theme, option.Level);
        var archivedOption = FindOption(data, lens, theme, option.Level);
        if (!SameContent(option, archivedOption))
            throw new InvalidOperationException($"The displayed behaviour for {lens} / {theme} / level {option.Level} does not match the retained catalogue.");

        return new MaturityBehaviourEvidence
        {
            BehaviourId = mapping.BehaviourId!,
            CatalogueVersion = CurrentVersion,
            Headline = option.Headline,
            Signals = option.Signals.ToList(),
            Examples = option.Examples.ToList()
        };
    }

    public async Task<SignalMapping> ResolveAsync(string lens, MaturityTheme theme)
    {
        var evidence = theme.SelectedBehaviour;
        if (theme.Level < 1 || theme.Level > 5 || evidence is null ||
            string.IsNullOrWhiteSpace(evidence.BehaviourId) ||
            string.IsNullOrWhiteSpace(evidence.CatalogueVersion) ||
            evidence.Signals is null || evidence.Examples is null)
            throw new InvalidOperationException($"The selection for {lens} / {theme.Theme} lacks valid historical provenance. Legacy assessments cannot be reopened safely.");

        var data = await LoadVersionAsync(evidence.CatalogueVersion);
        var mapping = FindMapping(data, lens, theme.Theme, theme.Level);
        var option = FindOption(data, lens, theme.Theme, theme.Level);
        if (!string.Equals(mapping.BehaviourId, evidence.BehaviourId, StringComparison.Ordinal) ||
            !string.Equals(option.Headline, evidence.Headline, StringComparison.Ordinal) ||
            !option.Signals.SequenceEqual(evidence.Signals) ||
            !option.Examples.SequenceEqual(evidence.Examples) ||
            !string.Equals(theme.SelectedBehaviourHeadline, evidence.Headline, StringComparison.Ordinal))
            throw new InvalidOperationException($"The saved evidence for {lens} / {theme.Theme} is inconsistent with catalogue version {evidence.CatalogueVersion}.");
        return mapping;
    }

    public async Task<List<SignalMapping>> ResolveSessionAsync(AssessmentSession session)
    {
        var mappings = new List<SignalMapping>();
        foreach (var lens in session.LensAssessments)
            foreach (var theme in lens.Themes.Where(item => item.Level > 0))
                mappings.Add(await ResolveAsync(lens.Lens, theme));
        return mappings;
    }

    private async Task<VersionData> LoadVersionAsync(string version)
    {
        if (!Regex.IsMatch(version, @"\A[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z"))
            throw new InvalidOperationException("The maturity catalogue version is invalid.");
        if (_versions.TryGetValue(version, out var cached))
            return cached;

        var root = $"data/maturity/versions/{version}";
        string catalogueJson;
        string mappingJson;
        try
        {
            catalogueJson = await _http.GetStringAsync($"{root}/maturity_behaviours.json");
            mappingJson = await _http.GetStringAsync($"{root}/maturity.json");
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException($"Maturity catalogue or mapping version {version} is unavailable.", exception);
        }
        var catalogue = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BehaviourOption>>>>(catalogueJson, JsonOptions)
            ?? throw new InvalidOperationException($"Maturity catalogue version {version} is empty.");
        var mappings = JsonSerializer.Deserialize<List<SignalMapping>>(mappingJson, JsonOptions)
            ?? throw new InvalidOperationException($"Maturity mapping version {version} is empty.");
        var data = new VersionData(catalogueJson, mappingJson, catalogue, mappings);
        _versions[version] = data;
        return data;
    }

    private static SignalMapping FindMapping(VersionData data, string lens, string theme, int level)
    {
        var matches = data.Mappings.Where(mapping =>
            string.Equals(mapping.Lens, lens, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(mapping.Theme, theme, StringComparison.OrdinalIgnoreCase) &&
            mapping.Level == level).ToList();
        if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].BehaviourId) ||
            string.IsNullOrWhiteSpace(matches[0].SignalName) || string.IsNullOrWhiteSpace(matches[0].Domain))
            throw new InvalidOperationException($"The retained mapping for {lens} / {theme} / level {level} is missing or ambiguous.");
        return matches[0];
    }

    private static BehaviourOption FindOption(VersionData data, string lens, string theme, int level)
    {
        var lensEntry = data.Catalogue.FirstOrDefault(item => string.Equals(item.Key, lens, StringComparison.OrdinalIgnoreCase));
        var themeEntry = lensEntry.Value?.FirstOrDefault(item => string.Equals(item.Key, theme, StringComparison.OrdinalIgnoreCase));
        var matches = themeEntry?.Value?.Where(option => option.Level == level).ToList();
        if (matches is null || matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].Headline) ||
            matches[0].Signals is null || matches[0].Examples is null)
            throw new InvalidOperationException($"The retained behaviour for {lens} / {theme} / level {level} is missing or ambiguous.");
        return matches[0];
    }

    private static bool SameContent(BehaviourOption left, BehaviourOption right) =>
        string.Equals(left.Headline, right.Headline, StringComparison.Ordinal) &&
        left.Signals.SequenceEqual(right.Signals) && left.Examples.SequenceEqual(right.Examples);

    private sealed class VersionManifest
    {
        public string CurrentVersion { get; set; } = string.Empty;
    }

    private sealed record VersionData(
        string CatalogueJson,
        string MappingJson,
        Dictionary<string, Dictionary<string, List<BehaviourOption>>> Catalogue,
        List<SignalMapping> Mappings);
}
