namespace OperationalIntelligenceHub.Models
{
    public class AssessmentResult
    {
        public string ToolName { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public Guid Id { get; set; } = Guid.NewGuid();

    // Squad filtering
        public Guid WorkspaceId { get; set; }
        public Guid SquadId { get; set; }

    // Frozen display
        public string WorkspaceName { get; set; } = "";
        public string SquadName { get; set; } = "";

    // Maturity (4 Lenses)
        public int? MaturityScore { get; set; }
        public int? SquadMaturityScore { get; set; }
        public int? ScrumMasterMaturityScore { get; set; }
        public int? PoMaturityScore { get; set; }
        public int? LeaderMaturityScore { get; set; }

    // Team Health tool
        // Dimension Scores
        public int? ClarityScore { get; set; }
        public int? SafetyScore { get; set; }
        public int? FlowScore { get; set; }
        public int? CollaborationScore { get; set; }
        public int? FocusScore { get; set; }
        public int? StakeholderScore { get; set; }
        public int? ImprovementScore { get; set; }
        public int? MoraleScore { get; set; }

        // Notes per dimension
        public string ClarityNotes { get; set; } = "";
        public string SafetyNotes { get; set; } = "";
        public string FlowNotes { get; set; } = "";
        public string CollaborationNotes { get; set; } = "";
        public string FocusNotes { get; set; } = "";
        public string StakeholderNotes { get; set; } = "";
        public string ImprovementNotes { get; set; } = "";
        public string MoraleNotes { get; set; } = "";

        // Overall average score
        public int? TeamHealthScore { get; set; }

    // Backlog Health tool (FULL breakdown)
        // Dimension Scores
        public int? BacklogSize { get; set; }
        public int? BacklogAge { get; set; }
        public int? BacklogVolatility { get; set; }
        public int? BacklogPriority { get; set; }
        public int? BacklogPredictability { get; set; }

        // Calculated overall
        public int? BacklogHealthScore { get; set; }

    // Guidance
        public string GrowthText { get; set; } = "";
        public string CoachingAdvice { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
