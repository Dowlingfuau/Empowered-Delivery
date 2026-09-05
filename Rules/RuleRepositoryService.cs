using System.Text.Json;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class RuleRepositoryService
    {
        private readonly HttpClient _http;

        public List<DerivedSignalRule> BehaviourRules { get; private set; } = new();

        public List<DerivedSignalRule> FlowRules { get; private set; } = new();

        public List<DerivedSignalRule> SystemRules { get; private set; } = new();

        public RuleRepositoryService(HttpClient http)
        {
            _http = http;
        }
        public async Task InitializeAsync()
        {
            // Behaviour rules
            var json = await _http.GetStringAsync("data/rules/behaviour_rules.json");

            var ruleSet = JsonSerializer.Deserialize<DerivedSignalRuleSet>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            BehaviourRules = ruleSet?.Derived_Signal_Rules ?? new();

            // Flow rules
            var flowJson = await _http.GetStringAsync("data/rules/flow_rules_v3.json");

            var flowSet = JsonSerializer.Deserialize<DerivedSignalRuleSet>(
                flowJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            FlowRules = flowSet?.Derived_Signal_Rules ?? new();

            Console.WriteLine($">>> Flow rules loaded: {FlowRules.Count}");
            Console.WriteLine($">>> RuleRepo instance: {this.GetHashCode()}");

            // System rules
            var systemJson = await _http.GetStringAsync("data/rules/system_rules.json");

            var systemSet = JsonSerializer.Deserialize<DerivedSignalRuleSet>(
                systemJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            SystemRules = systemSet?.Derived_Signal_Rules ?? new();
        }
    }
}