namespace OperationalIntelligenceHub.Models
{
    public class MaturityTheme
    {
        public string Theme { get; set; } = string.Empty; // e.g., Ownership, Trust, Value
        public int Level { get; set; } = 0; // 1–5
        public string SelectedBehaviourHeadline { get; set; } = string.Empty;
        public string BehaviouralNote { get; set; } = string.Empty;
        public static readonly string [] Themes =
        {
            "Ownership",
            "Trust",
            "Value",
            "Learning",
            "System"
        };

        // Maps lens -> theme -> Level 1-5 descriptors
        public static readonly Dictionary<string, Dictionary<string, string[]>> BehaviouralDescriptors = new()
        {
            { "ScrumMaster", new Dictionary<string, string[]>()
                {
                    { "Ownership", new [] { "Learns by book", "Situational experience", "Extended experience", "Supports value chain", "Teaches org-level" } },
                    { "Trust", new [] { "Focus on learning", "Guides team", "Stimulates responsibility", "Leads team", "Leads org" } },
                    { "Value", new [] { "Operational success", "Connects practice to outcomes", "Continuous improvement team", "Continuous improvement chain", "Continuous improvement org" } },
                    { "Learning", new [] { "Executes operational work", "Guides understanding", "Encourages responsibility", "Stimulates entire team", "Stimulates everyone in org" } },
                    { "System", new [] { "By book", "Experiments", "Improves team", "Improves value chain", "Improves org" } }
                }
            },
            { "ProductOwner", new Dictionary<string, string[]>()
                {
                    { "Ownership", new [] { "Analytical knowledge", "Owns backlog", "Value creation focus", "Mandate to make decisions", "Portfolio planning" } },
                    { "Trust", new [] { "Execute stakeholder plans", "Influences stakeholders", "Inspires team", "Impact all stakeholders", "Customer happiness" } },
                    { "Value", new [] { "Product backlog items", "Sprint goals", "Continuous outcome", "Continuous value", "Continuous value optimisation" } },
                    { "Learning", new [] { "Translates requirements", "Collaborates with stakeholders", "Inspires team collaboration", "Lead value chain", "Lead complex value chains" } },
                    { "System", new [] { "Basic product knowledge", "Manage product backlog", "Overview value process", "Deep knowledge value chain", "Deep portfolio knowledge" } }
                }
            },
            { "Squad", new Dictionary<string, string[]>()
                {
                    { "Ownership", new [] { "Individual tasks", "Shared knowledge", "Common standards", "Responsible outcomes", "Intuitive ownership" } },
                    { "Trust", new [] { "Avoids conflict", "Discover conflicts", "Team open", "Trust & respect", "Trust blindly" } },
                    { "Value", new [] { "Generate output", "Measure success", "Focus on goals", "Deliver high-quality increments", "Deliver value confirmed by users" } },
                    { "Learning", new [] { "Follow process", "Use Scrum values", "Actively ask feedback", "Collaborate with stakeholders", "Participate in sharing learning" } },
                    { "System", new [] { "Looking for stability", "Looking for common understanding", "New insights created", "Challenge/update standards", "Act on shared standards" } }
                }
            },
            { "Leader", new Dictionary<string, string[]>()
                {
                    { "Ownership", new [] { "Control/budget", "Delegate execution", "Co-create plans", "Delegate planning/execution", "Delegate full value chain" } },
                    { "Trust", new [] { "Directive style", "Delegates less critical", "Delegates more important", "Provide advice/facilitate", "Stimulate org & culture" } },
                    { "Value", new [] { "Measured by profit/shareholders", "Targets for teams", "Boundary conditions", "Create environment for self-organising teams", "Facilitate greater goal" } },
                    { "Learning", new [] { "Gives individual targets", "Ensures consensus", "Tracks progress", "Provides vision & mission", "Facilitates growth" } },
                    { "System", new [] { "Plans/rules", "Arranges buy-in", "Sets rules & standards", "Delegates all but critical", "Delegates all decisions" } }
                }
            },
        };

        public string GetDescriptor(string lens)
        {
            if (BehaviouralDescriptors.ContainsKey(lens) &&
                BehaviouralDescriptors[lens].ContainsKey(Theme) &&
                Level >= 1 && Level <= 5)
            {
                return BehaviouralDescriptors[lens][Theme][Level - 1];
            }
            return string.Empty;
        }
    }
}