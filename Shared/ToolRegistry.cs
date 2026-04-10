using OperationalIntelligenceHub.Models;
public static class ToolRegistry
{
    public static List<ToolDefinition> Tools = new()
    {
        new ToolDefinition
        {
            Name = "Backlog Health Monitor",
            Route = "/hub/backlog-health",
            Category = "Diagnostics",
            Icon = "📋"
        },
        new ToolDefinition
        {
            Name = "Maturity Assessment",
            Route = "/hub/maturity-assessment",
            Category = "Diagnostics",
            Icon = "👥"
        },
        new ToolDefinition
        {
            Name = "Team Health Check",
            Route = "/hub/team-health-check",
            Category = "Diagnostics",
            Icon = "💬"
        }
    };
}