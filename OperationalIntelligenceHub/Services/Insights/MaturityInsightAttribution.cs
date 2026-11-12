using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services;

public sealed record MaturityAssessmentCoordinate(string Lens, string Theme);

public static class MaturityInsightAttribution
{
    public static bool TryResolve(
        AssessmentSession session,
        OperationalInsight insight,
        IReadOnlyList<SignalMapping> mappings,
        IReadOnlyList<RuleEvaluation> evaluations,
        out List<MaturityAssessmentCoordinate> coordinates)
    {
        coordinates = new();
        var layer = insight.Source switch
        {
            "SystemRuleEngine" => "System",
            "BehaviourRuleEngine" => "Behaviour",
            "FlowRuleEngine" => "Flow",
            _ => null
        };
        return layer is not null && ResolveRule(
            session, insight.Name, insight.Intensity, layer, mappings, evaluations,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase), coordinates);
    }

    private static bool ResolveRule(
        AssessmentSession session, string name, string intensity, string layer,
        IReadOnlyList<SignalMapping> mappings, IReadOnlyList<RuleEvaluation> evaluations,
        HashSet<string> visited, List<MaturityAssessmentCoordinate> coordinates)
    {
        var key = $"{layer}:{name}";
        if (!visited.Add(key))
            return false;

        var matches = evaluations.Where(evaluation =>
            evaluation.Result &&
            string.Equals(evaluation.RuleName, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(evaluation.Layer, layer, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(evaluation.Output, intensity, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count != 1)
            return false;

        var groups = matches[0].Clauses.GroupBy(clause => clause.GroupIndex)
            .Where(group => group.Any() && group.All(clause => clause.Matched)).ToList();
        if (groups.Count != 1)
            return false;

        foreach (var clause in groups[0])
        {
            if (layer == "System")
            {
                var sourceLayers = evaluations.Where(evaluation =>
                        evaluation.Result &&
                        string.Equals(evaluation.RuleName, clause.Subject, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(evaluation.Output, clause.Actual, StringComparison.OrdinalIgnoreCase) &&
                        (evaluation.Layer == "Behaviour" || evaluation.Layer == "Flow"))
                    .Select(evaluation => evaluation.Layer).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (sourceLayers.Count != 1 ||
                    !ResolveRule(session, clause.Subject, clause.Actual, sourceLayers[0], mappings, evaluations, visited, coordinates))
                    return false;
            }
            else
            {
                var activeCoordinates = mappings.Where(mapping =>
                        !string.IsNullOrWhiteSpace(mapping.Lens) &&
                        !string.IsNullOrWhiteSpace(mapping.Theme) &&
                        string.Equals(mapping.SignalName, clause.Subject, StringComparison.OrdinalIgnoreCase) &&
                        session.LensAssessments.Any(lens =>
                            string.Equals(lens.Lens, mapping.Lens, StringComparison.OrdinalIgnoreCase) &&
                            lens.Themes.Any(theme => string.Equals(theme.Theme, mapping.Theme, StringComparison.OrdinalIgnoreCase) &&
                                                     theme.Level == mapping.Level)))
                    .Select(mapping => new MaturityAssessmentCoordinate(mapping.Lens!, mapping.Theme!))
                    .Distinct()
                    .ToList();
                if (activeCoordinates.Count == 0)
                    return false;
                coordinates.AddRange(activeCoordinates);
            }
        }
        visited.Remove(key);
        return true;
    }
}
