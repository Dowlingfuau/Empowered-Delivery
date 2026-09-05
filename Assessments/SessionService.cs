using OperationalIntelligenceHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OperationalIntelligenceHub.Services
{
    public class SessionService
    {
        private readonly AssessmentRepositoryService _repository;
        private readonly SignalPipelineService _signalPipeline;        
        public SessionService( AssessmentRepositoryService repository, SignalPipelineService signalPipeline)
        {
            _repository = repository;
            _signalPipeline = signalPipeline;
        }
        // Create a session
        public async Task<AssessmentSession> CreateSessionAsync(string workspaceId, string squadId, DateTime date)
        {
            var lenses = new[]
                {
                    "Squad",
                    "ScrumMaster",
                    "ProductOwner",
                    "Leader"
                };
                var lensAssessments = new List<MaturityLensAssessment>();
                foreach (var lens in lenses)
                {
                    var assessment = new MaturityLensAssessment { Lens = lens};
                    assessment.InitializeThemes();
                    lensAssessments.Add(assessment);
                }
                var session = new AssessmentSession
                {
                    AssessmentId = Guid.NewGuid().ToString(),
                    WorkspaceId = workspaceId,
                    SquadId = squadId,
                    DateCreated = DateTime.Now,
                    LensAssessments = lensAssessments
                };

            await _repository.SaveMaturitySessionAsync(session);
            return session;
        }
        // Add a lens assessment to a session
        public async Task<MaturityLensAssessment> AddLensAssessmentAsync(string sessionId, string lens)
        {
            var session = await _repository.GetMaturitySessionAsync(sessionId);
            if (session == null) throw new Exception($"Session {sessionId} not found.");

            if (session.LensAssessments.Any(l => l.Lens == lens))
                throw new Exception($"Lens {lens} already exists in this session.");

            var lensAssessment = new MaturityLensAssessment { Lens = lens };
            lensAssessment.InitializeThemes();

            session.LensAssessments.Add(lensAssessment);
            await _repository.SaveMaturitySessionAsync(session); // persist the change

            return lensAssessment;
        }

        // Update theme score
        public async Task UpdateThemeScoreAsync(string sessionId, string lens, string theme, int level, string? note = null)
        {
            var session = await _repository.GetMaturitySessionAsync(sessionId);
            if (session == null) throw new Exception($"Session {sessionId} not found.");

            var lensAssessment = session.LensAssessments.FirstOrDefault(l => l.Lens == lens);
            if (lensAssessment == null) throw new Exception($"Lens {lens} not found in session {sessionId}.");

            var themeScore = lensAssessment.Themes.FirstOrDefault(t => t.Theme == theme);
            if (themeScore == null) throw new Exception($"Theme {theme} not found in lens {lens}.");

            themeScore.Level = level;
            themeScore.BehaviouralNote = note ?? string.Empty;

            await _repository.SaveMaturitySessionAsync(session); // persist update
            await _signalPipeline.DebugSignals(session);
        }

        public async Task<AssessmentSession?> GetSession(string sessionId)
        {
            var session = await _repository.GetMaturitySessionAsync(sessionId);
            if (session == null) return null;
            var requiredLenses = new[]
            {
                "Squad",
                "ScrumMaster",
                "ProductOwner",
                "Leader"
            };
            foreach (var lens in requiredLenses)
            {
                if (!session.LensAssessments.Any(l => l.Lens == lens))
                {
                    var lensAssessment = new MaturityLensAssessment { Lens = lens };
                    lensAssessment.InitializeThemes();
                    session.LensAssessments.Add(lensAssessment);
                }
            }
        await _repository.SaveMaturitySessionAsync(session);
        return session;
        }

        public async Task<List<AssessmentSession>> ListSessions(string workspaceId, string squadId)
        {
            Guid? wId = Guid.TryParse(workspaceId, out var w) ? w : null;
            Guid? sId = Guid.TryParse(squadId, out var s) ? s : null;
            return await _repository.GetMaturitySessionsAsync(wId, sId);
        }

        public async Task<bool> IsSessionComplete(string sessionId)
        {
            var session = await _repository.GetMaturitySessionAsync(sessionId);
            return session?.IsComplete ?? false;
        }
    }
}
