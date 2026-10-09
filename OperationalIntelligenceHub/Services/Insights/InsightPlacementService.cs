using System.Text.Json;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    // Decides WHERE an insight belongs in a diagnostic. It never generates, scores, filters or reorders insights.
    public class InsightPlacementService
    {
        private const string ConfigPath = "data/insights/diagnostic_anchors.json";

        private readonly HttpClient _http;
        private readonly RuleRepositoryService _rules;

        // diagnostic -> (signal identifier, canonical or alias) -> anchor
        private Dictionary<string, Dictionary<string, (string AnchorId, string Canonical)>> _signalIndex = new();
        private Dictionary<string, Dictionary<string, string>> _labels = new();

        public InsightPlacementService(HttpClient http, RuleRepositoryService rules)
        {
            _http = http;
            _rules = rules;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await _http.GetStringAsync(ConfigPath);
                var config = JsonSerializer.Deserialize<InsightAnchorConfig>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new InsightAnchorConfig();

                Build(config);
            }
            catch (Exception ex)
            {
                // Without configuration every insight resolves to the overall area.
                Console.WriteLine($">>> InsightPlacement: anchor config unavailable ({ex.Message})");
                _signalIndex = new();
                _labels = new();
            }
        }

        private void Build(InsightAnchorConfig config)
        {
            var index = new Dictionary<string, Dictionary<string, (string, string)>>();
            var labels = new Dictionary<string, Dictionary<string, string>>();

            foreach (var (diagnostic, set) in config.Diagnostics)
            {
                var signals = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
                var anchorLabels = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var anchor in set.Anchors)
                {
                    if (string.IsNullOrWhiteSpace(anchor.Id) || string.IsNullOrWhiteSpace(anchor.CanonicalSignal))
                    {
                        Console.WriteLine($">>> InsightPlacement: ignoring incomplete anchor in '{diagnostic}'");
                        continue;
                    }

                    anchorLabels[anchor.Id] = anchor.Label;

                    foreach (var identifier in new[] { anchor.CanonicalSignal }.Concat(anchor.Aliases))
                    {
                        // An identifier that maps to two areas is ambiguous; the first definition wins and the clash is reported.
                        if (signals.TryGetValue(identifier, out var existing) && existing.Item1 != anchor.Id)
                        {
                            Console.WriteLine($">>> InsightPlacement: '{identifier}' is mapped to both '{existing.Item1}' and '{anchor.Id}' in '{diagnostic}'; keeping '{existing.Item1}'");
                            continue;
                        }

                        signals[identifier] = (anchor.Id, anchor.CanonicalSignal);
                    }
                }

                index[diagnostic] = signals;
                labels[diagnostic] = anchorLabels;
            }

            _signalIndex = index;
            _labels = labels;
        }

        public string? GetAnchorLabel(string diagnosticId, string anchorId) =>
            _labels.TryGetValue(diagnosticId, out var labels) && labels.TryGetValue(anchorId, out var label)
                ? label
                : null;

        // Output follows the input order exactly: the engine's priority order is preserved.
        public IReadOnlyList<InsightPlacement> Resolve(string diagnosticId, IEnumerable<OperationalInsight> insights)
        {
            _signalIndex.TryGetValue(diagnosticId, out var index);

            return insights.Select(insight => ResolveOne(insight, index)).ToList();
        }

        private InsightPlacement ResolveOne(
            OperationalInsight insight,
            Dictionary<string, (string AnchorId, string Canonical)>? index)
        {
            var references = insight.ContributingSignals
                .Select(id => index != null && index.TryGetValue(id, out var hit)
                    ? new InsightSignalReference(id, hit.Canonical, hit.AnchorId)
                    : new InsightSignalReference(id, null, null))
                .ToList();

            var category = FindRule(insight.Name)?.Category;

            InsightPlacement Place(string? anchorId, InsightPlacementReason reason) => new()
            {
                Insight = insight,
                AnchorId = anchorId,
                Reason = reason,
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                Signals = references
            };

            if (insight.Source == "SystemRuleEngine")
                return Place(null, InsightPlacementReason.SystemLevel);

            if (references.Count == 0)
                return Place(null, InsightPlacementReason.NoContributingSignals);

            if (references.Any(r => r.AnchorId == null))
                return Place(null, InsightPlacementReason.UnmappedSignal);

            var anchors = references.Select(r => r.AnchorId!).Distinct().ToList();

            return anchors.Count == 1
                ? Place(anchors[0], InsightPlacementReason.Anchored)
                : Place(null, InsightPlacementReason.Compound);
        }

        private DerivedSignalRule? FindRule(string name) =>
            _rules.BehaviourRules.FirstOrDefault(r => r.Name == name)
            ?? _rules.FlowRules.FirstOrDefault(r => r.Name == name)
            ?? _rules.SystemRules.FirstOrDefault(r => r.Name == name);
    }
}
