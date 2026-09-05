using OperationalIntelligenceHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperationalIntelligenceHub.Services
{
    public class AssessmentRepositoryService
    {
        private readonly LocalStorageService _localStorage;
        private const string BacklogHealthKey = "assessments_backlog_health";
        private const string TeamHealthKey = "assessments_team_health";

        // Event for auto-refresh
        public event Func<Task>? OnAssessmentChanged;

        public AssessmentRepositoryService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // ================================
        // BACKLOG HEALTH
        // ================================
        public async Task<List<AssessmentResult>> GetBacklogHealthAsync(Guid? workspaceId = null, Guid? squadId = null)
        {
            var results = await _localStorage.LoadAsync<List<AssessmentResult>>(BacklogHealthKey)
                          ?? new List<AssessmentResult>();

            return results
                .Where(r => (workspaceId == null || r.WorkspaceId == workspaceId) &&
                            (squadId == null || r.SquadId == squadId))
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        public async Task SaveBacklogHealthAsync(AssessmentResult result)
        {
            var results = await _localStorage.LoadAsync<List<AssessmentResult>>(BacklogHealthKey)
                          ?? new List<AssessmentResult>();

            // Replace existing if updating
            results.RemoveAll(r => r.Id == result.Id);
            results.Add(result);

            await _localStorage.SaveAsync(BacklogHealthKey, results);

            if (OnAssessmentChanged != null)
                await OnAssessmentChanged.Invoke();
        }

        public async Task DeleteBacklogHealthAsync(Guid assessmentId)
{
    var results = await _localStorage.LoadAsync<List<AssessmentResult>>(BacklogHealthKey)
                  ?? new List<AssessmentResult>();

    var removed = results.RemoveAll(r => r.Id == assessmentId) > 0;

    if (removed)
    {
        await _localStorage.SaveAsync(BacklogHealthKey, results);
        if (OnAssessmentChanged != null) await OnAssessmentChanged.Invoke();
    }
}
        public async Task DeleteAllBacklogHealthAsync(Guid workspaceId)
{
    var results = await GetBacklogHealthAsync();
    results.RemoveAll(r => r.WorkspaceId == workspaceId);
    await _localStorage.SaveAsync(BacklogHealthKey, results);
    if (OnAssessmentChanged != null) await OnAssessmentChanged.Invoke();
}

        // ================================
        // TEAM HEALTH
        // ================================
        public async Task<List<AssessmentResult>> GetTeamHealthAsync(Guid? workspaceId = null, Guid? squadId = null)
        {
            var results = await _localStorage.LoadAsync<List<AssessmentResult>>(TeamHealthKey)
                          ?? new List<AssessmentResult>();

            return results
                .Where(r => (workspaceId == null || r.WorkspaceId == workspaceId) &&
                            (squadId == null || r.SquadId == squadId))
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        public async Task SaveTeamHealthAsync(AssessmentResult result)
        {
            var results = await _localStorage.LoadAsync<List<AssessmentResult>>(TeamHealthKey)
                          ?? new List<AssessmentResult>();

            results.RemoveAll(r => r.Id == result.Id);
            results.Add(result);

            await _localStorage.SaveAsync(TeamHealthKey, results);

            if (OnAssessmentChanged != null)
                await OnAssessmentChanged.Invoke();
        }

        public async Task DeleteTeamHealthAsync(Guid assessmentId)
        {
            var results = await _localStorage.LoadAsync<List<AssessmentResult>>(TeamHealthKey)
                        ?? new List<AssessmentResult>();

            var removed = results.RemoveAll(r => r.Id == assessmentId) > 0;

            if (removed)
            {
                await _localStorage.SaveAsync(TeamHealthKey, results);
                if (OnAssessmentChanged != null) await OnAssessmentChanged.Invoke();
            }
        }

        public async Task DeleteAllTeamHealthAsync(Guid workspaceId)
        {
            var results = await GetTeamHealthAsync();
            results.RemoveAll(r => r.WorkspaceId == workspaceId);
            await _localStorage.SaveAsync(TeamHealthKey, results);
            if (OnAssessmentChanged != null) await OnAssessmentChanged.Invoke();
        }

        // ================================
        // MATURITY ASSESSMENTS
        // ================================
        private const string MaturityKey = "assessments_maturity";

        public async Task<List<AssessmentSession>> GetMaturitySessionsAsync(Guid? workspaceId = null, Guid? squadId = null)
        {
            var sessions = await _localStorage.LoadAsync<List<AssessmentSession>>(MaturityKey)
                        ?? new List<AssessmentSession>();

            return sessions
                .Where(s => (workspaceId == null || s.WorkspaceId == workspaceId.ToString()) &&
                            (squadId == null || s.SquadId == squadId.ToString()))
                .OrderByDescending(s => s.DateCreated)
                .ToList();
        }
        public async Task<AssessmentSession?> GetMaturitySessionAsync(string sessionId)
        {
            var sessions = await _localStorage.LoadAsync<List<AssessmentSession>>(MaturityKey)
                        ?? new List<AssessmentSession>();

            return sessions.FirstOrDefault(s => s.AssessmentId == sessionId);
        }
        public async Task SaveMaturitySessionAsync(AssessmentSession session)
        {
            var sessions = await _localStorage.LoadAsync<List<AssessmentSession>>(MaturityKey)
                        ?? new List<AssessmentSession>();

            sessions.RemoveAll(s => s.AssessmentId == session.AssessmentId);
            sessions.Add(session);

            await _localStorage.SaveAsync(MaturityKey, sessions);

            if (OnAssessmentChanged != null)
                await OnAssessmentChanged.Invoke();
        }

        public async Task DeleteMaturitySessionAsync(string sessionId)
        {
            var sessions = await _localStorage.LoadAsync<List<AssessmentSession>>(MaturityKey)
                        ?? new List<AssessmentSession>();

            var removed = sessions.RemoveAll(s => s.AssessmentId == sessionId) > 0;

            if (removed)
            {
                await _localStorage.SaveAsync(MaturityKey, sessions);
                if (OnAssessmentChanged != null)
                    await OnAssessmentChanged.Invoke();
            }
        }
    }
}