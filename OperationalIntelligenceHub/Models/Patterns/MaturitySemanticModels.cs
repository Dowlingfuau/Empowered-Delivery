using System.Text.Json;
using System.Text.Json.Serialization;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Models.Patterns;

public sealed class SemanticRuleSet
{
    public List<SemanticPatternDefinition> Rules { get; set; } = new();
}

public sealed class SemanticPatternDefinition
{
    public string PatternId { get; set; } = "";
    public string PatternName { get; set; } = "";
    public string RuleId { get; set; } = "";
    public SemanticEvaluationDefinition Evaluation { get; set; } = new();
    public SemanticDefinition SemanticDefinition { get; set; } = new();
    public string Source { get; set; } = "";
    [JsonIgnore]
    internal string? ValidatedFingerprint { get; set; }
}

public sealed class SemanticEvaluationDefinition
{
    public string Contract { get; set; } = "";
}

public sealed class SemanticDefinition
{
    public string DefinitionVersion { get; set; } = "";
    public SemanticCoreRelationship CoreRelationship { get; set; } = new();
    public SemanticCatalogueEvidence CatalogueEvidence { get; set; } = new();
    public List<SemanticSupportingProfile> SupportingEvidenceProfiles { get; set; } = new();
    public List<SemanticContextRelationship> ContextRelationships { get; set; } = new();
    public Dictionary<string, string> ExecutionPolicy { get; set; } = new();
}

public sealed class SemanticCoreRelationship
{
    public List<string> RequiredCoordinates { get; set; } = new();
    public List<SemanticGroup> GroupDefinitions { get; set; } = new();
    public List<SemanticBranch> Branches { get; set; } = new();
}

public sealed class SemanticGroup
{
    public string GroupId { get; set; } = "";
    public string Coordinate { get; set; } = "";
    public List<int> Levels { get; set; } = new();
}

public sealed class SemanticBranch
{
    public string BranchId { get; set; } = "";
    public JsonElement When { get; set; }
    public string? Direction { get; set; }
    public string Result { get; set; } = "";
    public string Interpretation { get; set; } = "";
}

public sealed class SemanticCatalogueEvidence
{
    public List<string> CompatibleCatalogueVersions { get; set; } = new();
    public List<SemanticCatalogueCoordinate> Core { get; set; } = new();
    public List<SemanticCatalogueCoordinate> Supporting { get; set; } = new();
}

public sealed class SemanticCatalogueCoordinate
{
    public string Coordinate { get; set; } = "";
    public List<SemanticCatalogueBehaviour> Behaviours { get; set; } = new();
}

public sealed class SemanticCatalogueBehaviour
{
    public string BehaviourId { get; set; } = "";
    public int Level { get; set; }
    public string Headline { get; set; } = "";
}

public sealed class SemanticSupportingProfile
{
    public string Coordinate { get; set; } = "";
    public string Principle { get; set; } = "";
    public Dictionary<string, Dictionary<string, string>> Contributions { get; set; } = new();
}

public sealed class SemanticContextRelationship
{
    public string RelationshipId { get; set; } = "";
    public JsonElement When { get; set; }
    public string Effect { get; set; } = "";
    public string Interpretation { get; set; } = "";
}

public enum SemanticEvidenceStatus { Valid, Missing, Invalid, Unavailable, Incompatible }
public enum SemanticTruth { True, False, Unknown }
public enum SemanticOutcome { Candidate, NoMatch, Unknown, Unavailable }

public sealed record SemanticConflictingSelection(int Level, string SelectedHeadline, MaturityBehaviourEvidence? Snapshot);

public sealed record SemanticSelection(
    string EvidenceId, string AssessmentId, string Coordinate, string Lens, string Theme,
    int Level, MaturityBehaviourEvidence? Snapshot, SemanticEvidenceStatus Status, string Reason,
    IReadOnlyList<SemanticConflictingSelection>? ConflictingSelections = null);

public sealed record SemanticConditionTrace(
    string ConditionId, string Operation, SemanticTruth Result, string Reason,
    IReadOnlyList<string> EvidenceIds, IReadOnlyList<SemanticConditionTrace> Operands);

public sealed record SemanticBranchTrace(
    string BranchId, string Result, string? Direction, SemanticConditionTrace Condition, string Interpretation);

public sealed record SemanticContributionTrace(
    string Coordinate, string EvidenceId, string Status, string? Classification, string Reason);

public sealed record SemanticContextTrace(
    string RelationshipId, string Status, string Effect, string Interpretation,
    IReadOnlyList<string> EvidenceIds, SemanticConditionTrace? Condition);

public sealed record SemanticStageTrace(
    string Stage, string Subject, string Result, string Reason, IReadOnlyList<string> EvidenceIds);

public sealed record SemanticDefinitionProvenance(
    string PatternId, string PatternName, string RuleId, string DefinitionVersion, string Source,
    string Contract, string DefinitionFingerprint, IReadOnlyList<string> CompatibleCatalogueVersions);

public sealed record SemanticAssessmentStates(string Strength, string Reliability, string Confidence, string Priority);

public sealed record SemanticEvaluationResult(
    SemanticDefinitionProvenance Definition,
    IReadOnlyList<SemanticSelection> Evidence,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Groups,
    IReadOnlyList<SemanticBranchTrace> Branches,
    IReadOnlyList<SemanticContributionTrace> Contributions,
    IReadOnlyList<SemanticContextTrace> ContextRelationships,
    SemanticOutcome Outcome, string? Direction, string? MatchedBranchId,
    SemanticAssessmentStates AssessmentStates, IReadOnlyList<SemanticStageTrace> Trace);
