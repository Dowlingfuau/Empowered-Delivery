using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
    public class OperationalInsightEngine
    {
        private readonly RuleRepositoryService _ruleRepo;
        public OperationalInsightEngine(RuleRepositoryService ruleRepo)
        {
            _ruleRepo = ruleRepo;
        }
        private const int SelectionLimit = 5;

        public List<OperationalInsight> GenerateInsights(
            List<DerivedSignal> behaviourSignals,
            List<DerivedSignal> systemSignals) =>
            GenerateInsightsWithTrace(behaviourSignals, systemSignals, null).insights;

        // Same result as GenerateInsights, plus a record of triggering and prioritisation.
        // Rendering is reported later by the hosting page through InsightEvaluationTrace.ApplyRendering.
        public (List<OperationalInsight> insights, InsightEvaluationTrace trace) GenerateInsightsWithTrace(
            List<DerivedSignal> behaviourSignals,
            List<DerivedSignal> systemSignals,
            IReadOnlyList<RuleEvaluation>? evaluations)
        {
            var insights = new List<OperationalInsight>();

            var allSignals = behaviourSignals.Concat(systemSignals).ToList();

            foreach (var signal in allSignals)
            {
                var rule = FindRule(signal.Name);

                insights.Add(new OperationalInsight
                {
                    Name = signal.Name,
                    Intensity = signal.Intensity,
                    Interpretation = string.IsNullOrWhiteSpace(signal.Interpretation)
                        ? rule?.Interpretation ?? ""
                        : signal.Interpretation,
                    ContributingSignals = signal.ContributingSignals ?? new List<string>(),
                    Source = signal.Source,
                    Score = CalculateScore(signal.Name, signal.Intensity),
                    Reasons = signal.Reasons ?? new List<InsightReason>()
                });
            }

            var ranked = insights.OrderByDescending(i => i.Score).ToList();
            var selected = ranked.Take(SelectionLimit).ToList();

            var trace = BuildTrace(allSignals, ranked, selected, evaluations, behaviourSignals.Count + systemSignals.Count);

            return (selected, trace);
        }

        private InsightEvaluationTrace BuildTrace(
            List<DerivedSignal> allSignals,
            List<OperationalInsight> ranked,
            List<OperationalInsight> selected,
            IReadOnlyList<RuleEvaluation>? evaluations,
            int triggeredCount)
        {
            var trace = new InsightEvaluationTrace
            {
                SelectionLimit = SelectionLimit,
                RulesEvaluated =
                    (_ruleRepo.BehaviourRules?.Count ?? 0) +
                    (_ruleRepo.FlowRules?.Count ?? 0) +
                    (_ruleRepo.SystemRules?.Count ?? 0),
                ConditionsEvaluated = evaluations?.Count ?? 0,
                RulesTriggered = triggeredCount,
                EligibleForPrioritisation = ranked.Count,
                SelectedForRendering = selected.Count,
                SuppressedOrExcluded = ranked.Count - selected.Count
            };

            for (int i = 0; i < ranked.Count; i++)
            {
                var insight = ranked[i];
                var signal = allSignals.First(s => s.Name == insight.Name);
                var rule = FindRule(insight.Name);
                var isSelected = i < SelectionLimit;

                var intensityScore = IntensityScore(insight.Intensity);
                var priority = rule?.Priority ?? 0;
                var confidence = rule?.Confidence_Weight ?? 0;

                // First evaluation of this rule that matched and produced the signal's intensity.
                var match = evaluations?.FirstOrDefault(e =>
                    e.RuleName == insight.Name && e.Result && e.Output == insight.Intensity);

                trace.Entries.Add(new InsightTraceEntry
                {
                    Name = insight.Name,
                    Category = rule?.Category ?? "",
                    Domain = rule?.Domain ?? signal.Domain,
                    Source = insight.Source,
                    Intensity = insight.Intensity,
                    Priority = priority,
                    ConfidenceWeight = confidence,
                    IntensityScore = intensityScore,
                    Score = insight.Score,
                    RuleFound = rule != null,
                    ScoreFormula = FormulaTrace.Expr(
                        "IntensityScore × Priority × ConfidenceWeight",
                        $"{TraceNumber.Format(intensityScore)} × {priority} × {TraceNumber.Format(confidence)}",
                        insight.Score),
                    MatchedRuleName = match?.RuleName,
                    MatchedCondition = match?.Condition,
                    MatchedResult = match?.Output,
                    MatchedClauses = match?.Clauses ?? new List<ConditionClause>(),
                    EligibleForPrioritisation = true,
                    Rank = i + 1,
                    Selected = isSelected,
                    NotSelectedReason = isSelected
                        ? null
                        : $"Ranked {i + 1} of {ranked.Count} by score; only the top {SelectionLimit} are kept."
                });
            }

            return trace;
        }

        private static double IntensityScore(string intensity) => intensity switch
        {
            "High" => 1.0,
            "Moderate" => 0.6,
            "Low" => 0.2,
            _ => 0
        };

        private DerivedSignalRule? FindRule(string name)
        {
            var behaviourRule = _ruleRepo.BehaviourRules
                .FirstOrDefault(r => r.Name == name);

            if (behaviourRule != null)
                return behaviourRule;
            
            var flowRule = _ruleRepo.FlowRules?
                .FirstOrDefault(r => r.Name == name);

            if (flowRule != null)
                return flowRule;

            return _ruleRepo.SystemRules
                .FirstOrDefault(r => r.Name == name);
        }
        private double CalculateScore(string name, string intensity)
        {
            var rule = FindRule(name);

            if (rule == null)
                return 0;

            // Map intensity → numeric
            var intensityScore = IntensityScore(intensity);

            var priority = GetPriority(name);
            var confidence = GetConfidence(name);

            return intensityScore * priority * confidence;
        }
        private double GetPriority(string name)
        {
            var rule = FindRule(name);
            return rule?.Priority ?? 0;
        }

        private double GetConfidence(string name)
        {
            var rule = FindRule(name);
            return rule?.Confidence_Weight ?? 0;
        }
    }
}