namespace OperationalIntelligenceHub.Models
{
    public class SignalDefinition
    {
        public string Name { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Type { get; set; } = "Behaviour";

        public string NormalisationRule { get; set; } = "clamp";
    }
}