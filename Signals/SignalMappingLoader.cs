using System.Text.Json;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class SignalMappingLoaderService
    {
        private readonly HttpClient _http;

        // Cache per diagnostic
        private readonly Dictionary<string, List<SignalMapping>> _cache = new();

        public SignalMappingLoaderService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<SignalMapping>> GetMappings(string diagnosticName)
        {
            if (_cache.TryGetValue(diagnosticName, out var cached))
                return cached;
            List<SignalMapping> mappings;
            try
            {
                var json = await _http.GetStringAsync($"data/signals/{diagnosticName}.json");

                mappings = JsonSerializer.Deserialize<List<SignalMapping>>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<SignalMapping>();
            }
            catch
            {
                mappings = new List<SignalMapping>();
            }
            _cache[diagnosticName] = mappings;

            return mappings;
            
        }
    }
}