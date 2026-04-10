namespace OperationalIntelligenceHub.Models
{
    public class Tribe
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public List<Squad> Squads { get; set; } = new();
    }
}