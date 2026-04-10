namespace OperationalIntelligenceHub.Models
{
    // BEHAVIOUR DETECTED FROM SIGNALS
    public class DerivedSignal
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public double Score { get; set; }
        public string Source { get; set; } = "";
        public string Intensity { get; set; } = "";
        
        public Guid WorkspaceId { get; set; }
        public Guid SquadId { get; set; }

        public List<string> ContributingSignals { get; set; } = new();

        public List<InsightReason> Reasons { get; set; } = new();
    }
}