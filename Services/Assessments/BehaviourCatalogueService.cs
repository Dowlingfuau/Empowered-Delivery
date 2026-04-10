using System.Text.Json;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class BehaviourCatalogueService
    {
        private readonly HttpClient _http;
        private Dictionary<string, Dictionary<string, List<BehaviourOption>>> _catalogue
            = new();

        public BehaviourCatalogueService(HttpClient http)
        {
            _http = http;
        }

        public async Task InitializeAsync()
        {
            try
            {
            var json = await _http.GetStringAsync("data/diagnostics/maturity_behaviours.json");

            _catalogue = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BehaviourOption>>>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();
            }
            catch
            {
                _catalogue = new();
            }
        }   

        public List<BehaviourOption> GetBehaviours(string lens, string theme)
        {
            if (_catalogue.ContainsKey(lens) &&
                _catalogue[lens].ContainsKey(theme))
            {
                return _catalogue[lens][theme];
            }

            return new List<BehaviourOption>();
        }
    }

    public class BehaviourOption
    {
        public int Level { get; set; }

        public string Headline { get; set; } = "";

        public List<string> Signals { get; set; } = new();

        public List<string> Examples { get; set; } = new();
    }
    
}