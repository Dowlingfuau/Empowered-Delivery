using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public sealed class MaturitySemanticEvidenceProjector
{
    private readonly MaturityEvidenceService _evidence;

    public MaturitySemanticEvidenceProjector(MaturityEvidenceService evidence) => _evidence = evidence;

    public async Task<IReadOnlyDictionary<string, SemanticSelection>> ProjectAsync(
        AssessmentSession session, SemanticPatternDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(session);
        var result = new Dictionary<string, SemanticSelection>(StringComparer.Ordinal);
        var coordinates = definition.SemanticDefinition.CatalogueEvidence.Core
            .Concat(definition.SemanticDefinition.CatalogueEvidence.Supporting);
        foreach (var item in coordinates)
        {
            var parts = item.Coordinate.Split('.');
            var matchingLenses = session.LensAssessments
                .Where(x => string.Equals(x.Lens, parts[0], StringComparison.OrdinalIgnoreCase)).ToList();
            var matches = matchingLenses.SelectMany(x => x.Themes)
                .Where(x => string.Equals(x.Theme, parts[1], StringComparison.OrdinalIgnoreCase)).ToList();
            var id = $"{session.AssessmentId}:{item.Coordinate}";
            SemanticSelection Entry(MaturityTheme? theme, SemanticEvidenceStatus status, string reason) =>
                new(id, session.AssessmentId, item.Coordinate, parts[0], parts[1], theme?.Level ?? 0,
                    Copy(theme?.SelectedBehaviour), status, reason);

            if (matchingLenses.Count > 1 || matches.Count > 1)
                result.Add(item.Coordinate, Entry(null, SemanticEvidenceStatus.Invalid, "Duplicate assessment coordinate.") with
                {
                    ConflictingSelections = matches.Select(x =>
                        new SemanticConflictingSelection(x.Level, x.SelectedBehaviourHeadline, Copy(x.SelectedBehaviour))).ToList()
                });
            else if (matches.Count == 0 || (matches[0].Level == 0 && matches[0].SelectedBehaviour is null))
                result.Add(item.Coordinate, Entry(matches.FirstOrDefault(), SemanticEvidenceStatus.Missing, "No behaviour selected."));
            else
            {
                var theme = matches[0];
                var version = theme.SelectedBehaviour?.CatalogueVersion;
                if (!string.IsNullOrWhiteSpace(version) &&
                    !definition.SemanticDefinition.CatalogueEvidence.CompatibleCatalogueVersions.Contains(version, StringComparer.Ordinal))
                    result.Add(item.Coordinate, Entry(theme, SemanticEvidenceStatus.Incompatible, $"Catalogue version '{version}' is not compatible."));
                else
                {
                    try
                    {
                        await _evidence.ResolveAsync(parts[0], theme);
                        result.Add(item.Coordinate, Entry(theme, SemanticEvidenceStatus.Valid, "Snapshot validated against retained catalogue and mapping."));
                    }
                    catch (InvalidOperationException exception)
                    {
                        var status = exception.InnerException is HttpRequestException
                            ? SemanticEvidenceStatus.Unavailable : SemanticEvidenceStatus.Invalid;
                        result.Add(item.Coordinate, Entry(theme, status, exception.Message));
                    }
                }
            }
        }
        return result;
    }

    internal static MaturityBehaviourEvidence? Copy(MaturityBehaviourEvidence? evidence) =>
        evidence is null ? null : new MaturityBehaviourEvidence
        {
            BehaviourId = evidence.BehaviourId,
            CatalogueVersion = evidence.CatalogueVersion,
            Headline = evidence.Headline,
            Signals = evidence.Signals is null ? null! : evidence.Signals.ToList(),
            Examples = evidence.Examples is null ? null! : evidence.Examples.ToList()
        };
}
