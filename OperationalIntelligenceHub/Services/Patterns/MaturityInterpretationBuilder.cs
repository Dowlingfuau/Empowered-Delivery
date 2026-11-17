using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public static class MaturityInterpretationBuilder
{
    public static MaturityInterpretationProfile Build(SemanticEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        var unavailable = evaluation.Contributions.Where(x => x.Status is "Missing" or "Invalid" or "Unavailable" or "Incompatible")
            .Select(x => x.EvidenceId).ToList();
        return new(evaluation.Contributions, evaluation.ContextRelationships, unavailable, "NotAssessed");
    }
}
