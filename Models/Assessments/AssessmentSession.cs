using System;
using System.Collections.Generic;
using System.Linq;

namespace OperationalIntelligenceHub.Models
{
    public class AssessmentSession
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string WorkspaceId { get; set; } = string.Empty;
        public string SquadId { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string SessionName { get; set; } = "";
        public string Status { get; set; } = "Not Started"; // Draft / Complete
        public List<MaturityLensAssessment> LensAssessments { get; set; } = new List<MaturityLensAssessment>();

        public bool IsComplete => LensAssessments.All(l => l.IsComplete);
        public int PercentComplete
        {
            get
            {
                if (LensAssessments == null || LensAssessments.Count == 0)
                    return 0;

                var totalThemes = LensAssessments.Count * 5;

                if (totalThemes == 0)
                    return 0;

                var completedThemes = LensAssessments
                    .Sum(l => l.Themes.Count(t => t.Level > 0));

                return completedThemes * 100 / totalThemes;
            }
        }
    }
}