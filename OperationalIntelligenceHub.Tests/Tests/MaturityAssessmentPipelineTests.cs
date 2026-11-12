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
using Microsoft.JSInterop;

public class MaturityAssessmentPipelineTests
{
    private readonly ITestOutputHelper _output;

    public MaturityAssessmentPipelineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DraftTransfer_PlansOnlyRecordedSelectionsAgainstMatchingNames()
    {
        var draft = new AssessmentSession
        {
            LensAssessments = new()
            {
                new() { Lens = "Squad", Themes = new()
                {
                    new() { Theme = "Ownership", Level = 3, BehaviouralNote = "draft evidence" },
                    new() { Theme = "Trust" }
                } }
            }
        };
        var target = new AssessmentSession
        {
            LensAssessments = new()
            {
                new() { Lens = "SQUAD", Themes = new()
                {
                    new() { Theme = "Trust", Level = 4, BehaviouralNote = "keep this" },
                    new() { Theme = "OWNERSHIP", Level = 1, BehaviouralNote = "old evidence" }
                } }
            }
        };

        var update = Assert.Single(MaturityDraftTransfer.Plan(draft, target));

        Assert.Equal(new MaturityDraftUpdate("SQUAD", "OWNERSHIP", 3, "draft evidence"), update);
        Assert.Equal("keep this", target.LensAssessments[0].Themes[0].BehaviouralNote);
        Assert.Equal("old evidence", target.LensAssessments[0].Themes[1].BehaviouralNote);
    }

    [Fact]
    public void DraftTransfer_RejectsMissingTargetBeforeAnyUpdates()
    {
        var draft = new AssessmentSession
        {
            LensAssessments = new()
            {
                new() { Lens = "Squad", Themes = new() { new() { Theme = "Ownership", Level = 2 } } },
                new() { Lens = "Leader", Themes = new() { new() { Theme = "Trust", Level = 4 } } }
            }
        };
        var target = new AssessmentSession
        {
            LensAssessments = new()
            {
                new() { Lens = "Squad", Themes = new() { new() { Theme = "Ownership", Level = 1 } } }
            }
        };

        Assert.Throws<InvalidOperationException>(() => MaturityDraftTransfer.Plan(draft, target));
        Assert.Equal(1, target.LensAssessments[0].Themes[0].Level);
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

    [Fact]
    public void MaturityAttribution_UsesOnlyTheMatchedOrGroup()
    {
        var session = AttributionSession();
        var insight = new OperationalInsight { Name = "Example", Intensity = "High", Source = "BehaviourRuleEngine" };
        var evaluations = new List<RuleEvaluation>
        {
            new()
            {
                RuleName = "Example", Layer = "Behaviour", Result = true, Output = "High",
                Clauses = new List<ConditionClause>
                {
                    new() { Subject = "TeamOwnership", Matched = false, GroupIndex = 0 },
                    new() { Subject = "LeaderTrust", Matched = true, GroupIndex = 1 }
                }
            }
        };

        var resolved = MaturityInsightAttribution.TryResolve(session, insight, AttributionMappings(), evaluations, out var coordinates);

        Assert.True(resolved);
        Assert.Equal(new MaturityAssessmentCoordinate("Leader", "Trust"), Assert.Single(coordinates));
    }

    [Fact]
    public void MaturityAttribution_FollowsMatchedSystemEvidenceToRecordedBehaviours()
    {
        var session = AttributionSession();
        var insight = new OperationalInsight { Name = "Friction", Intensity = "High", Source = "SystemRuleEngine" };
        var evaluations = new List<RuleEvaluation>
        {
            MatchedRule("Friction", "System", "High",
                new ConditionClause { Subject = "TeamRule", Actual = "High", Matched = true },
                new ConditionClause { Subject = "TrustRule", Actual = "Low", Matched = true }),
            MatchedRule("TeamRule", "Behaviour", "High", new ConditionClause { Subject = "TeamOwnership", Matched = true }),
            MatchedRule("TrustRule", "Behaviour", "Low", new ConditionClause { Subject = "LeaderTrust", Matched = true })
        };

        var resolved = MaturityInsightAttribution.TryResolve(session, insight, AttributionMappings(), evaluations, out var coordinates);

        Assert.True(resolved);
        Assert.Contains(new MaturityAssessmentCoordinate("Squad", "Ownership"), coordinates);
        Assert.Contains(new MaturityAssessmentCoordinate("Leader", "Trust"), coordinates);
        Assert.Equal(2, coordinates.Count);
    }

    [Fact]
    public void MaturityAttribution_RejectsMissingOrAmbiguousEvidence()
    {
        var insight = new OperationalInsight { Name = "Example", Intensity = "High", Source = "BehaviourRuleEngine" };
        var match = MatchedRule("Example", "Behaviour", "High", new ConditionClause { Subject = "Unknown", Matched = true });

        Assert.False(MaturityInsightAttribution.TryResolve(AttributionSession(), insight, AttributionMappings(), new[] { match }, out _));
        match.Clauses[0].Subject = "TeamOwnership";
        Assert.False(MaturityInsightAttribution.TryResolve(AttributionSession(), insight, AttributionMappings(), new[] { match, match }, out _));
    }

    [Fact]
    public async Task SelectionUpdate_SavesTheSuppliedNoteAndNotifiesAssessmentSubscribers()
    {
        var http = new HttpClient(new FakeHttpHandler()) { BaseAddress = new Uri("http://localhost/") };
        var registry = new SignalRegistryService(http);
        await registry.InitializeAsync();
        var rules = new TestRuleRepositoryService();
        var pipeline = new SignalPipelineService(
            new MaturitySignalBuilderService(new SignalNormaliserService(), new SignalMappingLoaderService(http), registry),
            new DerivedSignalEngine(rules),
            new SystemRuleEngine(rules),
            new OperationalInsightEngine(rules),
            new HealthAggregationService());
        var repository = new AssessmentRepositoryService(new LocalStorageService(new MemoryJsRuntime()));
        var session = new AssessmentSession
        {
            AssessmentId = Guid.NewGuid().ToString(),
            WorkspaceId = Guid.NewGuid().ToString(),
            SquadId = Guid.NewGuid().ToString(),
            LensAssessments = new List<MaturityLensAssessment>
            {
                new() { Lens = "Squad", Themes = new List<MaturityTheme> { new() { Theme = "Ownership", BehaviouralNote = "old note" } } }
            }
        };
        await repository.SaveMaturitySessionAsync(session);
        var notifications = 0;
        repository.OnAssessmentChanged += () => { notifications++; return Task.CompletedTask; };

        await new SessionService(repository, pipeline).UpdateThemeScoreAsync(
            session.AssessmentId, "Squad", "Ownership", 1, "catalogue signals and examples");

        var saved = await repository.GetMaturitySessionAsync(session.AssessmentId);
        Assert.NotNull(saved);
        var theme = Assert.Single(Assert.Single(saved.LensAssessments).Themes);
        Assert.Equal(1, theme.Level);
        Assert.Equal("catalogue signals and examples", theme.BehaviouralNote);
        Assert.Equal(1, notifications);
    }

    private sealed class MemoryJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string> values = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            if (identifier == "localStorage.setItem")
            {
                values[key] = (string)args[1]!;
                return ValueTask.FromResult(default(TValue)!);
            }
            if (identifier == "localStorage.getItem")
            {
                values.TryGetValue(key, out var value);
                return ValueTask.FromResult((TValue)(object?)value!);
            }
            throw new InvalidOperationException($"Unexpected JS call: {identifier}");
        }
    }

    private static AssessmentSession AttributionSession() => new()
    {
        LensAssessments = new List<MaturityLensAssessment>
        {
            new() { Lens = "Squad", Themes = new List<MaturityTheme> { new() { Theme = "Ownership", Level = 3 } } },
            new() { Lens = "Leader", Themes = new List<MaturityTheme> { new() { Theme = "Trust", Level = 2 } } }
        }
    };

    private static List<SignalMapping> AttributionMappings() => new()
    {
        new() { SignalName = "TeamOwnership", Lens = "Squad", Theme = "Ownership", Level = 3 },
        new() { SignalName = "LeaderTrust", Lens = "Leader", Theme = "Trust", Level = 2 }
    };

    private static RuleEvaluation MatchedRule(string name, string layer, string output, params ConditionClause[] clauses) => new()
    {
        RuleName = name,
        Layer = layer,
        Result = true,
        Output = output,
        Clauses = clauses.ToList()
    };
}
