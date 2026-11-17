using System.Text.Json;
using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public static class SemanticConditionEvaluator
{
    public static SemanticConditionTrace Evaluate(
        JsonElement expression, string conditionId,
        IReadOnlyDictionary<string, SemanticSelection> evidence,
        IReadOnlyDictionary<string, SemanticGroup> groups)
    {
        if (expression.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Condition {conditionId} must be an object.");
        var properties = expression.EnumerateObject().ToList();
        if (properties.Count != 1)
            throw new InvalidOperationException($"Condition {conditionId} must have exactly one operator.");
        var operation = properties[0];
        if (operation.Name is "all" or "any")
        {
            if (operation.Value.ValueKind != JsonValueKind.Array || operation.Value.GetArrayLength() == 0)
                throw new InvalidOperationException($"Condition {conditionId}: {operation.Name} requires a nonempty array.");
            var operands = operation.Value.EnumerateArray()
                .Select((item, index) => Evaluate(item, $"{conditionId}/{index}", evidence, groups)).ToList();
            var result = operation.Name == "all"
                ? operands.Any(x => x.Result == SemanticTruth.False) ? SemanticTruth.False
                    : operands.All(x => x.Result == SemanticTruth.True) ? SemanticTruth.True : SemanticTruth.Unknown
                : operands.Any(x => x.Result == SemanticTruth.True) ? SemanticTruth.True
                    : operands.All(x => x.Result == SemanticTruth.False) ? SemanticTruth.False : SemanticTruth.Unknown;
            return new(conditionId, operation.Name, result, $"{operation.Name} evaluated all {operands.Count} operands.",
                operands.SelectMany(x => x.EvidenceIds).Distinct().ToList(), operands);
        }

        string coordinate;
        Func<SemanticSelection, bool> matches;
        if (operation.Name == "hasGroup")
        {
            if (operation.Value.ValueKind != JsonValueKind.String ||
                !groups.TryGetValue(operation.Value.GetString() ?? "", out var group))
                throw new InvalidOperationException($"Condition {conditionId} references an unknown group.");
            coordinate = group.Coordinate;
            matches = item => group.Levels.Contains(item.Level);
        }
        else if (operation.Name == "hasBehaviour")
        {
            if (operation.Value.ValueKind != JsonValueKind.Object ||
                !operation.Value.TryGetProperty("coordinate", out var coord) || coord.ValueKind != JsonValueKind.String ||
                !operation.Value.TryGetProperty("behaviourIds", out var ids) || ids.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException($"Condition {conditionId}: malformed hasBehaviour.");
            coordinate = coord.GetString() ?? "";
            var allowed = ids.EnumerateArray().Select(x => x.GetString()).ToHashSet(StringComparer.Ordinal);
            matches = item => allowed.Contains(item.Snapshot?.BehaviourId);
        }
        else
            throw new InvalidOperationException($"Condition {conditionId} uses unsupported operator '{operation.Name}'.");

        if (!evidence.TryGetValue(coordinate, out var selection))
            throw new InvalidOperationException($"Condition {conditionId} references undeclared coordinate '{coordinate}'.");
        var value = selection.Status == SemanticEvidenceStatus.Valid
            ? matches(selection) ? SemanticTruth.True : SemanticTruth.False
            : SemanticTruth.Unknown;
        var reason = selection.Status == SemanticEvidenceStatus.Valid
            ? $"Selected behaviour {selection.Snapshot!.BehaviourId} {(value == SemanticTruth.True ? "matches" : "does not match")} {operation.Name}."
            : $"{selection.Status}: {selection.Reason}";
        return new(conditionId, operation.Name, value, reason, new[] { selection.EvidenceId }, Array.Empty<SemanticConditionTrace>());
    }
}
