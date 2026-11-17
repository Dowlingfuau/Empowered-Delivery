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
    string? Error);
