namespace OperationalIntelligenceHub.Models
{
    public class InsightReason
{
    public string SignalName { get; set; } = "";
    public double SignalValue { get; set; }
    public string Condition { get; set; } = "";
    public string Result { get; set; } = "";
    public double ContributionScore { get; set; }
}
}