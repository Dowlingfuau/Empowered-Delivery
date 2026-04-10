namespace OperationalIntelligenceHub.Models
{
    public class BusinessUnit
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public List<Tribe> Tribes { get; set; } = new();
    }
}