using System.Text.Json;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class SignalRegistryService
    {
        private readonly HttpClient _http;
        private Dictionary<string, SignalDefinition> _signals = new();

        public SignalRegistryService(HttpClient http)
        {
            _http = http;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await _http.GetStringAsync("data/signals/signal_registry.json?v=1");

                Console.WriteLine($">>> Registry JSON length: {json.Length}");

                var list = JsonSerializer.Deserialize<List<SignalDefinition>>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SignalDefinition>();
                
                Console.WriteLine($">>> Registry loaded: {list.Count} signals");

                _signals = list.ToDictionary(s => s.Name, s => s);
            }
            catch
            {
                _signals = new();
            }
        }

        public SignalDefinition? Get(string signalName)
        {
            return _signals.TryGetValue(signalName, out var def)
                ? def
                : null;
        }
    }
}