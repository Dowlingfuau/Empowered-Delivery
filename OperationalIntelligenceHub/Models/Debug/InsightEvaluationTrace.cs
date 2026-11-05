namespace OperationalIntelligenceHub.Models
{
    // What the Insight Engine did with one triggered rule. Triggering, prioritisation and rendering are separate stages.
    public class InsightTraceEntry
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Source { get; set; } = "";
        public string Intensity { get; set; } = "";

        // Configured on the rule.
        public int Priority { get; set; }
        public double ConfidenceWeight { get; set; }
        public double IntensityScore { get; set; }

        // intensity score x priority x confidence weight, exactly as the engine ranks.
        public double Score { get; set; }
        public FormulaTrace? ScoreFormula { get; set; }

        // Stage 1: triggering
        public string? MatchedRuleName { get; set; }
        public string? MatchedCondition { get; set; }
        public string? MatchedResult { get; set; }
        public List<ConditionClause> MatchedClauses { get; set; } = new();
        public bool RuleFound { get; set; }

        // Stage 2: prioritisation
        public bool EligibleForPrioritisation { get; set; } = true;
        public int Rank { get; set; }
        public bool Selected { get; set; }
        public string? NotSelectedReason { get; set; }

        // Stage 3: rendering. Null when the page that hosts the console does not report it.
        public bool? Rendered { get; set; }
        public string? NotRenderedReason { get; set; }
    }

    public class InsightEvaluationTrace
    {
        // Prioritisation keeps this many insights; mirrored from the engine so the trace states the real cut-off.
        public int SelectionLimit { get; set; }

        public int RulesEvaluated { get; set; }
        public int ConditionsEvaluated { get; set; }
        public int RulesTriggered { get; set; }
        public int EligibleForPrioritisation { get; set; }
        public int SelectedForRendering { get; set; }
        public int SuppressedOrExcluded { get; set; }

        public bool RenderStageReported { get; set; }
        public int RenderedCount { get; set; }
        public int SelectedButNotRendered { get; set; }

        // Ordered as the engine ranks them (score, highest first).
        public List<InsightTraceEntry> Entries { get; set; } = new();

        // Called by the hosting page once it knows which insights it actually displayed.
        public void ApplyRendering(IReadOnlyDictionary<string, string?> renderedOrReason)
        {
            RenderStageReported = true;
            foreach (var entry in Entries)
            {
                if (!entry.Selected)
                {
                    entry.Rendered = false;
                    entry.NotRenderedReason = "Not selected during prioritisation.";
                    continue;
                }

                if (renderedOrReason.TryGetValue(entry.Name, out var reason))
                {
                    entry.Rendered = reason == null;
                    entry.NotRenderedReason = reason;
                }
                else
                {
                    entry.Rendered = false;
                    entry.NotRenderedReason = "Selected, but the page produced no placement for it.";
                }
            }

            RenderedCount = Entries.Count(e => e.Rendered == true);
            SelectedButNotRendered = Entries.Count(e => e.Selected && e.Rendered == false);
        }
    }
}
