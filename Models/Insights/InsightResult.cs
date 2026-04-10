namespace OperationalIntelligenceHub.Models
{
    public class InsightResult
    {
        public string InsightId { get; set; } = "";

        public double Priority { get; set; }

        public double Confidence { get; set; }

        // WHAT
        public string Headline { get; set; } = "";

        // SO WHAT
        public string? Context { get; set; }

        // NOW WHAT (used in panel / MRI)
        public string? Action { get; set; }

        // Deep reasoning (debug + MRI)
        public string? Reason { get; set; }

        // Optional tags for UI routing
        public List<string> Tags { get; set; } = new();
    }
}