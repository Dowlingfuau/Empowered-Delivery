using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services;

public sealed record EvidenceTraceEntry(int Depth, string Description);

public class SignalTraceService
{
    public List<TraceInput> GetUserInputs(
        IEnumerable<SignalTrace>? traces, IEnumerable<TraceInput>? additionalInputs = null)
    {
        return (traces ?? Enumerable.Empty<SignalTrace>())
            .SelectMany(trace => trace.Inputs)
            .Concat(additionalInputs ?? Enumerable.Empty<TraceInput>())
            .Where(IsUserInput)
            .DistinctBy(input => $"{input.Source}\u001f{input.Name}", StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public SignalTrace? FindSignalTrace(IEnumerable<SignalTrace>? traces, string name)
    {
        var matches = (traces ?? Enumerable.Empty<SignalTrace>())
            .Where(trace => string.Equals(trace.SignalName, name, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    public List<EvidenceTraceEntry> TraceInsight(
        InsightTraceEntry insight, IReadOnlyList<RuleEvaluation> evaluations, IReadOnlyList<SignalTrace> traces)
    {
        var result = new List<EvidenceTraceEntry>();
        var layer = insight.Source switch
        {
            "BehaviourRuleEngine" => "Behaviour",
            "FlowRuleEngine" => "Flow",
            "SystemRuleEngine" => "System",
            _ => null
        };
        if (layer == null)
            result.Add(new(0, $"Evidence path unavailable: unknown source '{insight.Source}'."));
        else
            TraceRule(insight.Name, insight.Intensity, layer, 0, evaluations, traces, new(), result);
        return result;
    }

    private void TraceRule(
        string name, string output, string layer, int depth, IReadOnlyList<RuleEvaluation> evaluations,
        IReadOnlyList<SignalTrace> traces, HashSet<string> visited, List<EvidenceTraceEntry> result)
    {
        var key = $"{layer}\u001f{name}\u001f{output}";
        if (!visited.Add(key))
        {
            result.Add(new(depth, $"Evidence path unavailable: cycle at {name}."));
            return;
        }

        var matches = evaluations.Where(e => e.Result &&
            string.Equals(e.RuleName, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Layer, layer, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Output, output, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count != 1)
        {
            result.Add(new(depth, $"Evidence path unavailable for {name}: expected one matching rule evaluation, found {matches.Count}."));
            visited.Remove(key);
            return;
        }

        var evaluation = matches[0];
        result.Add(new(depth, $"{layer} rule {name}: {evaluation.Condition} → {output}"));
        var groups = evaluation.Clauses.GroupBy(c => c.GroupIndex)
            .Where(group => group.Any() && group.All(clause => clause.Matched)).ToList();
        if (groups.Count != 1)
        {
            result.Add(new(depth + 1, "Matched condition evidence is unavailable or ambiguous."));
            visited.Remove(key);
            return;
        }

        foreach (var clause in groups[0])
        {
            result.Add(new(depth + 1, $"{clause.Subject}: {clause.Actual} {clause.Operator} {clause.Threshold}"));
            if (layer == "System")
            {
                var layers = evaluations.Where(e => e.Result &&
                    string.Equals(e.RuleName, clause.Subject, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Output, clause.Actual, StringComparison.OrdinalIgnoreCase) &&
                    (e.Layer == "Behaviour" || e.Layer == "Flow"))
                    .Select(e => e.Layer).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (layers.Count == 1)
                    TraceRule(clause.Subject, clause.Actual, layers[0], depth + 2, evaluations, traces, visited, result);
                else
                    result.Add(new(depth + 2, "Source rule evidence is unavailable or ambiguous."));
            }
            else
            {
                var matchingTraces = traces.Where(t =>
                    string.Equals(t.SignalName, clause.Subject, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingTraces.Count != 1)
                {
                    result.Add(new(depth + 2, "Source signal trace is unavailable or ambiguous."));
                    continue;
                }
                var trace = matchingTraces[0];
                if (!string.IsNullOrWhiteSpace(trace.BehaviourId))
                    result.Add(new(depth + 2, $"Behaviour ID: {trace.BehaviourId}"));
                foreach (var input in trace.Inputs.Where(IsUserInput))
                    result.Add(new(depth + 2, $"{input.Name}: {FormatValue(input.Value)}"));
                foreach (var step in trace.CalculationSteps)
                {
                    if (step.Formula is { } formula)
                    {
                        var expression = formula.Expression ?? $"{formula.Numerator} / {formula.Denominator}";
                        result.Add(new(depth + 2, $"{step.StepName}: {formula.Symbolic} ({expression}) = {formula.Result}{(string.IsNullOrWhiteSpace(formula.Unit) ? "" : $" {formula.Unit}")}"));
                    }
                    else
                        result.Add(new(depth + 2, $"{step.StepName}: {FormatValue(step.Value)}"));
                }
            }
        }
        visited.Remove(key);
    }

    private static string FormatValue(object? value) =>
        value is System.Collections.IEnumerable values && value is not string
            ? string.Join(", ", values.Cast<object?>().Select(item => item?.ToString() ?? "not provided"))
            : value?.ToString() ?? "not provided";

    private static bool IsUserInput(TraceInput input)
    {
        if (input.Kind != TraceInputKind.Unspecified)
            return input.Kind == TraceInputKind.UserInput;

        var name = input.Name.ToLowerInvariant();
        var source = (input.Source ?? "").ToLowerInvariant();
        if (source == "calculation" || name.Contains("average") || name.Contains("variance") ||
            name.Contains("standarddeviation") || name.Contains("coefficientofvariation") || name.Contains("cv"))
            return false;
        if (source == "throughputhistory" && input.Value is System.Collections.IEnumerable && input.Value is not string)
            return true;
        if (source == "backloginput" || source == "input" || source == "throughputhistory")
            return true;
        return string.IsNullOrWhiteSpace(source) &&
            input.Value is int or long or double or float or decimal;
    }
}
