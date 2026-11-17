using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public static class MaturityEligibilityPolicy
{
    public const string PolicyId = "BF01-Provisional";
    public const string PolicyVersion = "1.0";

    public static MaturityEligibilityDecision Assess(SemanticEvaluationResult? evaluation, string? error = null)
    {
        var coreSelections = evaluation?.Evidence.Where(x => x.Coordinate is "ProductOwner.Value" or "Squad.Value").ToList()
            ?? new List<SemanticSelection>();
        var core = coreSelections.Select(x => x.EvidenceId).ToList();
        var validCore = evaluation is not null && coreSelections.Count == 2 &&
            coreSelections.Select(x => x.Coordinate).Distinct().Count() == 2 &&
            coreSelections.All(x => x.Status == SemanticEvidenceStatus.Valid && x.Snapshot is not null &&
                evaluation.Definition.CompatibleCatalogueVersions.Contains(x.Snapshot.CatalogueVersion));
        var status = error is not null ? MaturityEligibilityStatus.EvaluationError
            : evaluation?.Outcome switch
            {
                SemanticOutcome.Candidate when validCore && evaluation.Direction is "DirectionA" or "DirectionB"
                    => MaturityEligibilityStatus.EligibleProvisional,
                SemanticOutcome.NoMatch when validCore => MaturityEligibilityStatus.NotEligible,
                _ => MaturityEligibilityStatus.NotAssessed
            };
        var reason = status switch
        {
            MaturityEligibilityStatus.EligibleProvisional => "The valid, compatible core selections match a BF01 direction.",
            MaturityEligibilityStatus.NotEligible => "The valid core selections do not match BF01.",
            MaturityEligibilityStatus.EvaluationError => $"Evaluation failed: {error}",
            _ => "The core relationship could not be assessed from available valid evidence."
        };
        return new(PolicyId, PolicyVersion, status, reason, core);
    }
}
