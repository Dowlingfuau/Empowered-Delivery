using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;
using System.Text.Json;

namespace OperationalIntelligenceHub.Services.Patterns;

public sealed class MaturitySemanticEvaluator
{
    private readonly MaturitySemanticEvidenceProjector _projector;

    public MaturitySemanticEvaluator(MaturitySemanticEvidenceProjector projector) => _projector = projector;

    public async Task<SemanticEvaluationResult> EvaluateAsync(AssessmentSession session, SemanticPatternDefinition definition)
    {
        EnsureValidated(definition);
        var evidence = await _projector.ProjectAsync(session, definition);
        return Evaluate(definition, evidence);
    }

    public static SemanticEvaluationResult Evaluate(SemanticPatternDefinition definition,
        IReadOnlyDictionary<string, SemanticSelection> evidence)
    {
        EnsureValidated(definition);
        var semantic = definition.SemanticDefinition;
        var expectedCoordinates = semantic.CatalogueEvidence.Core.Concat(semantic.CatalogueEvidence.Supporting)
            .ToDictionary(x => x.Coordinate, StringComparer.Ordinal);
        if (!expectedCoordinates.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(evidence.Keys) ||
            evidence.Any(x => x.Key != x.Value.Coordinate ||
                (x.Value.Status == SemanticEvidenceStatus.Valid &&
                    (x.Value.Snapshot is null ||
                     !semantic.CatalogueEvidence.CompatibleCatalogueVersions.Contains(x.Value.Snapshot.CatalogueVersion) ||
                     !expectedCoordinates[x.Key].Behaviours.Any(b => b.Level == x.Value.Level &&
                         b.BehaviourId == x.Value.Snapshot.BehaviourId && b.Headline == x.Value.Snapshot.Headline)))))
            throw new InvalidOperationException("Evidence projection does not match the validated definition.");
        evidence = evidence.ToDictionary(x => x.Key, x => x.Value with
        {
            Snapshot = MaturitySemanticEvidenceProjector.Copy(x.Value.Snapshot),
            ConflictingSelections = x.Value.ConflictingSelections?.Select(item => item with
            {
                Snapshot = MaturitySemanticEvidenceProjector.Copy(item.Snapshot)
            }).ToList()
        }, StringComparer.Ordinal);
        var groups = semantic.CoreRelationship.GroupDefinitions.ToDictionary(x => x.GroupId, StringComparer.Ordinal);
        var groupResults = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var trace = new List<SemanticStageTrace>();
        foreach (var item in evidence.Values)
        {
            trace.Add(new("Acquisition", item.Coordinate,
                item.ConflictingSelections is not null ? "Ambiguous" : item.Snapshot is null ? "Missing" : "Present",
                item.ConflictingSelections is not null ? "Multiple selections acquired for this coordinate."
                    : item.Snapshot is null ? "No snapshot acquired." : "Saved selection acquired.", new[] { item.EvidenceId }));
            trace.Add(new("Validation", item.Coordinate, item.Status.ToString(), item.Reason, new[] { item.EvidenceId }));
            var membership = item.Status == SemanticEvidenceStatus.Valid
                ? groups.Values.Where(g => g.Coordinate == item.Coordinate && g.Levels.Contains(item.Level))
                    .Select(g => g.GroupId).ToList()
                : new List<string>();
            groupResults.Add(item.Coordinate, membership);
            trace.Add(new("Projection", item.Coordinate, item.Status == SemanticEvidenceStatus.Valid ? "Evaluated" : item.Status.ToString(),
                item.Status == SemanticEvidenceStatus.Valid
                    ? $"Resolved groups: {string.Join(", ", membership)}."
                    : $"Group resolution not possible: {item.Reason}", new[] { item.EvidenceId }));
        }

        var branches = semantic.CoreRelationship.Branches.Select(branch => new SemanticBranchTrace(
            branch.BranchId, branch.Result, branch.Direction,
            SemanticConditionEvaluator.Evaluate(branch.When, $"branch:{branch.BranchId}", evidence, groups),
            branch.Interpretation)).ToList();
        foreach (var branch in branches)
            trace.Add(new("Branch", branch.BranchId, branch.Condition.Result.ToString(),
                branch.Condition.Result == SemanticTruth.True
                    ? branch.Interpretation
                    : $"Branch condition evaluated {branch.Condition.Result}; see its operands for the reasons.",
                branch.Condition.EvidenceIds));

        var coreItems = semantic.CoreRelationship.RequiredCoordinates.Select(x => evidence[x]).ToList();
        var unavailable = coreItems.Any(x => x.Status is SemanticEvidenceStatus.Invalid or SemanticEvidenceStatus.Unavailable or SemanticEvidenceStatus.Incompatible);
        var missing = coreItems.Any(x => x.Status == SemanticEvidenceStatus.Missing);
        SemanticOutcome outcome;
        SemanticBranchTrace? matched = null;
        if (unavailable) outcome = SemanticOutcome.Unavailable;
        else if (missing) outcome = SemanticOutcome.Unknown;
        else
        {
            var matches = branches.Where(x => x.Condition.Result == SemanticTruth.True).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException($"Expected one core branch, found {matches.Count}.");
            matched = matches[0];
            outcome = matched.Result == "PotentialValueTranslationGap" ? SemanticOutcome.Candidate : SemanticOutcome.NoMatch;
        }
        var reason = matched?.Interpretation ?? string.Join("; ", coreItems.Where(x => x.Status != SemanticEvidenceStatus.Valid)
            .Select(x => $"{x.Coordinate}: {x.Status}: {x.Reason}"));
        trace.Add(new("CoreOutcome", definition.RuleId, outcome.ToString(), reason, coreItems.Select(x => x.EvidenceId).ToList()));
        trace.Add(new("Candidate", definition.PatternId, outcome == SemanticOutcome.Candidate ? "Created" : "NotCreated",
            reason, coreItems.Select(x => x.EvidenceId).ToList()));

        var contributions = new List<SemanticContributionTrace>();
        foreach (var profile in semantic.SupportingEvidenceProfiles)
        {
            var item = evidence[profile.Coordinate];
            string status;
            string? classification = null;
            string contributionReason;
            if (outcome != SemanticOutcome.Candidate)
            {
                status = "Skipped";
                contributionReason = $"Core outcome is {outcome}.";
            }
            else if (item.Status != SemanticEvidenceStatus.Valid)
            {
                status = item.Status.ToString();
                contributionReason = item.Reason;
            }
            else
            {
                status = "Evaluated";
                classification = profile.Contributions[matched!.Direction!][item.Level.ToString()];
                contributionReason = profile.Principle;
            }
            var qualification = classification == "Qualifying"
                ? semantic.QualificationRules.Single(x => x.Coordinate == profile.Coordinate &&
                    x.Directions.Contains(matched!.Direction!) && x.BehaviourIds.Contains(item.Snapshot!.BehaviourId))
                : null;
            string? potentialRelevance = null;
            if (classification == "Contextual" && profile.PotentialRelevance.TryGetValue(matched!.Direction!, out var levels))
                levels.TryGetValue(item.Level.ToString(), out potentialRelevance);
            contributions.Add(new SemanticContributionTrace(profile.Coordinate, item.EvidenceId, status, classification, contributionReason)
            {
                PotentialRelevance = potentialRelevance,
                QualificationRuleId = qualification?.RuleId,
                QualificationEffect = qualification?.InterpretiveEffect,
                NarrativeKey = qualification?.NarrativeKey,
                Boundary = qualification?.Boundary
            });
            trace.Add(new("Contribution", profile.Coordinate, classification ?? status, contributionReason, new[] { item.EvidenceId }));
        }

        var contexts = new List<SemanticContextTrace>();
        foreach (var relationship in semantic.ContextRelationships)
        {
            var dependencies = ReferencedCoordinates(relationship.When, groups).Distinct()
                .Select(x => evidence[x].EvidenceId).ToList();
            SemanticConditionTrace? condition = null;
            contexts.Add(new(relationship.RelationshipId, "NotEvaluated",
                relationship.Effect, relationship.Interpretation, dependencies, condition));
            trace.Add(new("ContextRelationship", relationship.RelationshipId, "NotEvaluated",
                "The relationship is pending approval.", dependencies));
        }
        var provenance = new SemanticDefinitionProvenance(definition.PatternId, definition.PatternName, definition.RuleId,
            semantic.DefinitionVersion, definition.Source, definition.Evaluation.Contract,
            definition.ValidatedFingerprint!, semantic.CatalogueEvidence.CompatibleCatalogueVersions.ToList())
        {
            OperationalDimensions = semantic.OperationalDimensions.ToList()
        };
        var assessments = new SemanticAssessmentStates(semantic.ExecutionPolicy["strength"],
            semantic.ExecutionPolicy["reliability"], semantic.ExecutionPolicy["confidence"], semantic.ExecutionPolicy["priority"]);
        return new SemanticEvaluationResult(provenance, evidence.Values.ToList(), groupResults, branches, contributions, contexts,
            outcome, matched?.Direction, matched?.BranchId, assessments, trace)
        {
            GroupDefinitions = semantic.CoreRelationship.GroupDefinitions
                .Select(group => new SemanticGroupDefinitionTrace(group.GroupId, group.Coordinate, group.Levels.ToList())).ToList()
        };
    }

    private static IEnumerable<string> ReferencedCoordinates(JsonElement expression,
        IReadOnlyDictionary<string, SemanticGroup> groups)
    {
        var operation = expression.EnumerateObject().Single();
        if (operation.Name is "all" or "any")
            return operation.Value.EnumerateArray().SelectMany(child => ReferencedCoordinates(child, groups)).ToList();
        return operation.Name == "hasGroup"
            ? new[] { groups[operation.Value.GetString()!].Coordinate }
            : new[] { operation.Value.GetProperty("coordinate").GetString()! };
    }

    private static void EnsureValidated(SemanticPatternDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.ValidatedFingerprint is null ||
            definition.ValidatedFingerprint != MaturitySemanticRuleRepository.Fingerprint(definition))
            throw new InvalidOperationException("The semantic definition has not been validated or was modified after validation. Reload it before evaluation.");
    }
}
