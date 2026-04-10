namespace OperationalIntelligenceHub.Models
{
    public class ProgressItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int PercentComplete { get; set; }
        public bool IsComplete { get; set; }
    }
}