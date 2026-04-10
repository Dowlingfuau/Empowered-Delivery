namespace OperationalIntelligenceHub.Models
{
    public class OperationalInsight
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Description { get; set; } = "";
        public string Intensity { get; set; } = "";
        public double Score { get; set; }
        public string Interpretation { get; set; } = "";
        public List<string> ContributingSignals { get; set; } = new();
        public string Source { get; set; } = "";
        public List<string> SupportingSignals { get; set; } = new();
        public List<InsightReason> Reasons { get; set; } = new();
    }
}