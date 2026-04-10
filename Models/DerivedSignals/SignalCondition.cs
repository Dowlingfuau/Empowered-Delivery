namespace OperationalIntelligenceHub.Models
{
    // INDIVIDUAL RULE CONDITION
    public class SignalCondition
    {
        public string Signal { get; set; } = "";

        public string Operator { get; set; } = "";

        public double Value { get; set; }
    }
}