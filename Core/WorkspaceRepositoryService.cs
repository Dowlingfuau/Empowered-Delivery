using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class WorkspaceRepositoryService
    {
        private readonly LocalStorageService _localStorage;
        private const string WorkspaceKey = "org_workspaces";
        private const string ActiveWorkspaceKey = "active_workspace";

        private List<OrgWorkspace> _workspaces = new();

        public event Func<Task>? OnWorkspaceChanged;

        public Guid? ActiveWorkspaceId { get; private set; }

        public OrgWorkspace? ActiveWorkspace => 
            _workspaces.FirstOrDefault(w => w.Id == ActiveWorkspaceId);

        public WorkspaceRepositoryService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

       public async Task InitializeAsync()
    {
        _workspaces = await _localStorage.LoadAsync<List<OrgWorkspace>>(WorkspaceKey) ?? new List<OrgWorkspace>();
        ActiveWorkspaceId = await _localStorage.LoadAsync<Guid?>(ActiveWorkspaceKey);

        // Signal to all components that data is loaded
        OnWorkspaceChanged?.Invoke();
    }

        // ===== Workspace Methods =====
        public Task<List<OrgWorkspace>> GetWorkspacesAsync() => Task.FromResult(_workspaces);

        public async Task AddWorkspaceAsync(string? name = null)
        {
            var ws = new OrgWorkspace { Name = name ?? $"Workspace {_workspaces.Count + 1}" };
            _workspaces.Add(ws);
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task DeleteWorkspaceAsync(OrgWorkspace workspace)
        {
            _workspaces.Remove(workspace);
            if (ActiveWorkspaceId == workspace.Id)
                ActiveWorkspaceId = null;
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task SetActiveWorkspaceAsync(Guid? workspaceId)
        {
            if (ActiveWorkspaceId != workspaceId)
            {
                ActiveWorkspaceId = workspaceId;
                await SaveAsync();
                OnWorkspaceChanged?.Invoke();
            }
        }

        // ===== Hierarchy Methods =====
        public async Task AddCompanyAsync(OrgWorkspace workspace, string? name = null)
        {
            workspace.Structure.Companies.Add(new Company { Name = name ?? "New Company" });
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task DeleteCompanyAsync(OrgWorkspace workspace, Company company)
        {
            workspace.Structure.Companies.Remove(company);
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task AddBusinessUnitAsync(Company company, string? name = null)
        {
            company.BusinessUnits.Add(new BusinessUnit { Name = name ?? "New BU" });
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task DeleteBusinessUnitAsync(Company company, BusinessUnit bu)
        {
            company.BusinessUnits.Remove(bu);
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task AddTribeAsync(BusinessUnit bu, string? name = null)
        {
            bu.Tribes.Add(new Tribe { Name = name ?? "New Tribe" });
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task DeleteTribeAsync(BusinessUnit bu, Tribe tribe)
        {
            bu.Tribes.Remove(tribe);
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task AddSquadAsync(Tribe tribe, string? name = null)
        {
            tribe.Squads.Add(new Squad { Name = name ?? "New Squad" });
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        public async Task DeleteSquadAsync(Tribe tribe, Squad squad)
        {
            tribe.Squads.Remove(squad);
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }

        // ===== Persistence =====
        // ===== Public Save Method =====
        public async Task SaveWorkspacesAsync()
        {
            await SaveAsync();
            OnWorkspaceChanged?.Invoke();
        }
        private async Task SaveAsync()
        {
            await _localStorage.SaveAsync(WorkspaceKey, _workspaces);
            await _localStorage.SaveAsync(ActiveWorkspaceKey, ActiveWorkspaceId);
        }
        public string GetWorkspaceName(Guid workspaceId)
        {
            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            return ws?.Name ?? "Unknown Workspace";
        }

        public string GetSquadName(Guid workspaceId, Guid? squadId)
        {
            if (squadId == null)
                return "Unknown Squad";

            var ws = _workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (ws?.Structure == null)
            {
                return "Workspace not found";
            }

            var squad = ws.Structure.Companies
                .SelectMany(c => c.BusinessUnits)
                .SelectMany(bu => bu.Tribes)
                .SelectMany(t => t.Squads)
                .FirstOrDefault(s => s.Id == squadId.Value);

            if (squad == null)
            {
                return "Squad Not Found";
            }

            return squad.Name;
        } 
    }
}