namespace OperationalIntelligenceHub.Models
{
    public class Squad
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }
}