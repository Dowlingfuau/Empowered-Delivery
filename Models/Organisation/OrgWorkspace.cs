namespace OperationalIntelligenceHub.Models
{
    public class OrgWorkspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public OrgStructure Structure { get; set; } = new();
        public List<AssessmentResult> AssessmentResults { get; set; } = new();
    }
}