namespace OperationalIntelligenceHub.Models
{
    // One comparison inside a rule condition, e.g. WorkHorizon > 5 with the actual value that was tested.
    public class ConditionClause
    {
        public string Subject { get; set; } = "";
        public string Operator { get; set; } = "";
        public string Threshold { get; set; } = "";
        public string Actual { get; set; } = "";
        public bool Matched { get; set; }

        // Position of the OR group this clause belongs to.
        public int GroupIndex { get; set; }

        public string? Note { get; set; }
    }

    public class RuleEvaluation
    {
        public string RuleName { get; set; } = "";

        public string Condition { get; set; } = "";

        public bool Result { get; set; }

        public string? Output { get; set; }

        // Behaviour, Flow or System. Empty when the evaluation did not come from an engine.
        public string Layer { get; set; } = "";

        // The comparisons that were actually evaluated. Evaluation stops at the first failing clause in a group.
        public List<ConditionClause> Clauses { get; set; } = new();
    }
}