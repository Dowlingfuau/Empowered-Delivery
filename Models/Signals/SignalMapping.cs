namespace OperationalIntelligenceHub.Models
{
    public class SignalMapping
    {
        public string? BehaviourId { get; set; }

        // Matching criteria (flexible)
        public string? Lens { get; set; }
        public string? Theme { get; set; }
        public int? Level { get; set; }

        // Output signal
        public string SignalName { get; set; } = "";
        public string Domain { get; set; } = "";
        public string? NormalisationRule { get; set; }
        
        // TEMP default
        public string Type { get; set; } = "Behaviour";
    }
}