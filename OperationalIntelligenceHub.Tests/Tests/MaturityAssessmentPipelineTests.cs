using Xunit;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Text;
using System;
using System.Linq;

public class MaturityAssessmentPipelineTests
{
    private readonly ITestOutputHelper _output;

    public MaturityAssessmentPipelineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Simple HTTP handler that returns minimal mapping + registry JSON for tests
    private class FakeHttpHandler : HttpMessageHandler
    {
                private readonly string _mappingsJson = @"[
    {
        ""behaviourId"": ""Squad_Ownership_L1"",
        ""lens"": ""Squad"",
        ""theme"": ""Ownership"",
        ""level"": 1,
        ""signalName"": ""TeamOwnershipLevel"",
        ""domain"": ""Capability""
    }
]";

        private readonly string _registryJson = @"[
  { ""name"": ""TeamOwnershipLevel"", ""domain"": ""Capability"", ""type"": ""Behaviour"", ""normalisationRule"": ""value/5"" }
]";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;
            var content = "[]";

            if (url.Contains("maturity.json"))
                content = _mappingsJson;
            else if (url.Contains("signal_registry.json"))
                content = _registryJson;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task BuildSignalsAndDerived_ShouldProduce_LowTeamOwnershipInsight_FromSession()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var mappingLoader = new SignalMappingLoaderService(http);
        var registry = new SignalRegistryService(http);
        await registry.InitializeAsync();

        var normaliser = new SignalNormaliserService();
        var maturityBuilder = new MaturitySignalBuilderService(normaliser, mappingLoader, registry);

        var repo = new TestRuleRepositoryService();

        // Add behaviour rule targeting TeamOwnershipLevel (maturity signal)
        repo.AddBehaviourRule(new DerivedSignalRule
        {
            Name = "LowTeamOwnership",
            Domain = "Capability",
            Priority = 8,
            Confidence_Weight = 0.9,
            Rules = new List<RuleCondition>
            {
                new RuleCondition { Condition = "TeamOwnershipLevel < 0.4", Result = "High" }
            },
            Interpretation = "Team ownership is low"
        });

        var derived = new DerivedSignalEngine(repo);
        var system = new SystemRuleEngine(repo);
        var insight = new OperationalInsightEngine(repo);

        var pipeline = new SignalPipelineService(
            maturityBuilder,
            derived,
            system,
            insight,
            new HealthAggregationService()
        );

        var session = new AssessmentSession
        {
            WorkspaceId = Guid.NewGuid().ToString(),
            SquadId = Guid.NewGuid().ToString(),
            LensAssessments = new List<MaturityLensAssessment>
            {
                new MaturityLensAssessment
                {
                    Lens = "Squad",
                    Themes = new List<MaturityTheme>
                    {
                        new MaturityTheme { Theme = "Ownership", Level = 1 }
                    }
                }
            }
        };

        // Act
        var (signals, behaviour, systemSignals, insights, evaluations) =
            await pipeline.BuildSignalsAndDerived(session);

        foreach (var s in signals) _output.WriteLine($"SIGNAL: {s.Name} ({s.NormalisedValue:0.00})");
        foreach (var b in behaviour) _output.WriteLine($"BEHAVIOUR: {b.Name} ({b.Intensity})");
        foreach (var i in insights) _output.WriteLine($"INSIGHT: {i.Name} ({i.Score})");

        // Assert
        Assert.Contains(signals, s => s.Name == "TeamOwnershipLevel");
        Assert.Contains(behaviour, b => b.Name == "LowTeamOwnership");
        Assert.Contains(insights, i => i.Name == "LowTeamOwnership");
    }
}
