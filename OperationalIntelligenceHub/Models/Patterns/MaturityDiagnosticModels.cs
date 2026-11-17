namespace OperationalIntelligenceHub.Models.Patterns;

public enum MaturityEligibilityStatus { EligibleProvisional, NotEligible, NotAssessed, EvaluationError }

public sealed record MaturityEligibilityDecision(
    string PolicyId, string PolicyVersion, MaturityEligibilityStatus Status, string Reason,
    IReadOnlyList<string> EvidenceIds);

public sealed record MaturityInterpretationProfile(
    IReadOnlyList<SemanticContributionTrace> Contributions,
    IReadOnlyList<SemanticContextTrace> ContextRelationships,
    IReadOnlyList<string> UnavailableEvidenceIds,
    string AlternativeExplanationsStatus);

public sealed record MaturityNarrativeQualification(
    string RuleId, string EvidenceId, string Coordinate, string Text, string Boundary);

public sealed record MaturityDiagnosticFinding(
    string FindingId, string PatternId, string Direction, string NarrativeSource, string NarrativeVersion,
    string NarrativeFingerprint, string Observed, string Suspected, string Meaning, string Action,
    string Limitation, IReadOnlyList<MaturityNarrativeQualification> Qualifications,
    IReadOnlyList<SemanticSelection> Evidence)
{
    public IReadOnlyList<string> OperationalDimensions { get; init; } = Array.Empty<string>();
}
