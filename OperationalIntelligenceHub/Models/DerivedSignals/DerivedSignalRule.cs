using System.Text.Json.Serialization;

namespace OperationalIntelligenceHub.Models
{
    // RULE DEFINITION
    public class DerivedSignalRule
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";

        // Authored in the rule JSON ("category"); read for presentation only.
        public string Category { get; set; } = "";

        public List<string> SourceSignals { get; set; } = new();
        public List<RuleCondition> Rules { get; set; } = new();

        public string Interpretation { get; set; } = "";

        public int Priority { get; set; }

        [JsonPropertyName("confidence_weight")]
        public double Confidence_Weight { get; set; }
    }
    public class RuleCondition
    {
        public string Condition { get; set; } = "";
        public string Result { get; set; } = "";
    }
}