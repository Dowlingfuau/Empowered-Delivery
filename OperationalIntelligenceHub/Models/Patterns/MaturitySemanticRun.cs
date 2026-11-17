namespace OperationalIntelligenceHub.Models.Patterns;

public sealed record MaturitySemanticRun(
    string RunId,
    string AssessmentId,
    string AssessmentFingerprint,
    string PatternId,
    string DefinitionSource,
    SemanticEvaluationResult? Evaluation,
    string Eligibility,
    string EligibilityReason,
    string? FailedStage,
    string? Error)
{
    public MaturityEligibilityDecision? EligibilityDecision { get; init; }
    public MaturityInterpretationProfile? Interpretation { get; init; }
    public MaturityDiagnosticFinding? Finding { get; init; }
    public string PresentationStatus { get; init; } = "NotReported";
    public string PresentationReason { get; init; } = "Presentation has not been reported.";
}
