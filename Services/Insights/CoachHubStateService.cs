using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class CoachHubStateService
    {
        private readonly LocalStorageService _localStorage;
        private const string StorageKey = "coachhub_state";

        private Dictionary<Guid, CoachHubWorkspaceState> _workspaceStates = new();

        public CoachHubStateService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            _workspaceStates =
                await _localStorage.LoadAsync<Dictionary<Guid, CoachHubWorkspaceState>>(StorageKey)
                ?? new Dictionary<Guid, CoachHubWorkspaceState>();
        }

        private async Task SaveAsync()
        {
            await _localStorage.SaveAsync(StorageKey, _workspaceStates);
        }

        private CoachHubWorkspaceState GetOrCreateWorkspaceState(Guid workspaceId)
        {
            if (!_workspaceStates.ContainsKey(workspaceId))
                _workspaceStates[workspaceId] = new CoachHubWorkspaceState();

            return _workspaceStates[workspaceId];
        }

        // ================================
        // BACKLOG INCLUDE
        // ================================
        public async Task<HashSet<Guid>> GetBacklogIncludedAsync(Guid workspaceId, List<AssessmentResult> allAssessments)
        {
            var state = GetOrCreateWorkspaceState(workspaceId);

            // Default: include all
            if (!state.BacklogIncluded.Any())
            {
                state.BacklogIncluded = allAssessments
                    .Where(a => a.WorkspaceId == workspaceId)
                    .Select(a => a.Id)
                    .ToHashSet();

                await SaveAsync();
            }

            return state.BacklogIncluded;
        }

        public async Task ToggleBacklogAsync(Guid workspaceId, Guid assessmentId)
        {
            var state = GetOrCreateWorkspaceState(workspaceId);

            if (state.BacklogIncluded.Contains(assessmentId))
                state.BacklogIncluded.Remove(assessmentId);
            else
                state.BacklogIncluded.Add(assessmentId);

            await SaveAsync();
        }

        // ================================
        // TEAM INCLUDE
        // ================================
        public async Task<HashSet<Guid>> GetTeamIncludedAsync(Guid workspaceId, List<AssessmentResult> allAssessments)
        {
            var state = GetOrCreateWorkspaceState(workspaceId);

            if (!state.TeamIncluded.Any())
            {
                state.TeamIncluded = allAssessments
                    .Where(a => a.WorkspaceId == workspaceId)
                    .Select(a => a.Id)
                    .ToHashSet();

                await SaveAsync();
            }

            return state.TeamIncluded;
        }

        public async Task ToggleTeamAsync(Guid workspaceId, Guid assessmentId)
        {
            var state = GetOrCreateWorkspaceState(workspaceId);

            if (state.TeamIncluded.Contains(assessmentId))
                state.TeamIncluded.Remove(assessmentId);
            else
                state.TeamIncluded.Add(assessmentId);

            await SaveAsync();
        }

        // ================================
        // CLEANUP AFTER DELETE
        // ================================
        public async Task RemoveAssessmentAsync(Guid workspaceId, Guid assessmentId)
        {
            if (_workspaceStates.TryGetValue(workspaceId, out var state))
            {
                state.BacklogIncluded.Remove(assessmentId);
                state.TeamIncluded.Remove(assessmentId);
                await SaveAsync();
            }
        }
    }

    public class CoachHubWorkspaceState
    {
        public HashSet<Guid> BacklogIncluded { get; set; } = new();
        public HashSet<Guid> TeamIncluded { get; set; } = new();
    }
}