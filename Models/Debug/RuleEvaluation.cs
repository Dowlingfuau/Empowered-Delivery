namespace OperationalIntelligenceHub.Models
{
    public class RuleEvaluation
    {
        public string RuleName { get; set; } = "";

        public string Condition { get; set; } = "";

        public bool Result { get; set; }

        public string? Output { get; set; }
    }
}