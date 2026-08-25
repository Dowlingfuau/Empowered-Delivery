using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
public static class PlayLibraryService
{
    public static List<Play> Plays = new()
    {
        new Play
        {
            Title = "Backlog Pruning Workshop",
            Description = "Review and simplify backlog items to restore prioritisation clarity.",
            Difficulty = "Easy",
            Duration = "60–90 minutes",
            Category = "Backlog Practices",
            Route = "/plays/backlog-pruning"
        },

        new Play
        {
            Title = "Reduce Work in Progress",
            Description = "Experiment with limiting work intake to improve delivery flow.",
            Difficulty = "Easy",
            Duration = "2 sprints",
            Category = "Flow Experiments",
            Route = "/plays/reduce-wip"
        },
        new Play
        {
            Title = "Disruptive Brainstorming",
            Description = "Spark creative thinking and generate fresh ideas.",
            Difficulty = "Hard",
            Duration = "60 minutes",
            Category = "Flow Experiments",
            Route = "/plays/disruptive-brainstorming"
        },
        new Play
        {
            Title = "Team Culture of Trust",
            Description = "Surface team norms and turn trust conversations into concrete actions.",
            Difficulty = "Medium",
            Duration = "60-90 minutes",
            Category = "Team Health",
            Route = "/plays/team-culture-of-trust"
        }
    };
}
}
