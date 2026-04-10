namespace OperationalIntelligenceHub.Models
{
    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<BusinessUnit> BusinessUnits { get; set; } = new();
    }
}