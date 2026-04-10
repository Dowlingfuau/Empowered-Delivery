namespace OperationalIntelligenceHub.Models
{
    public class ToolDefinition
    {
        public required string Name { get; set; }
        public required string Route { get; set; }
        public required string Category { get; set; }
        public required string Icon { get; set; }

        public bool ShowInSidebar { get; set; } = true;
    }
}