namespace OperationalIntelligenceHub.Models
{
    public class OrgStructure
    {
        public List<Company> Companies { get; set; } = new();
        
        public List<Squad> GetAllSquads()
        {
            return Companies?
                .SelectMany(c => c.BusinessUnits ?? new List<BusinessUnit>())
                .SelectMany(bu => bu.Tribes ?? new List<Tribe>())
                .SelectMany(t => t.Squads ?? new List<Squad>())
                .ToList()
                ?? new List<Squad>();
        }
    }
}