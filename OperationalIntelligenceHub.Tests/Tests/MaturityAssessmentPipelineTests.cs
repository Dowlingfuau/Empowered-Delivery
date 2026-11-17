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
                    new() { Theme = "Ownership", Level = 3, BehaviouralNote = "draft evidence",
                        SelectedBehaviour = Evidence("Squad_Ownership_L3", "original") },
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

        Assert.Equal("SQUAD", update.Lens);
        Assert.Equal("OWNERSHIP", update.Theme);
        Assert.Equal(3, update.Level);
        Assert.Equal("Squad_Ownership_L3", update.Evidence.BehaviourId);
        Assert.Equal("original", update.Evidence.Headline);
        Assert.Equal("v1", update.Evidence.CatalogueVersion);
        Assert.Equal("signal", Assert.Single(update.Evidence.Signals));
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
                new() { Lens = "Squad", Themes = new() { new() { Theme = "Ownership", Level = 2, SelectedBehaviour = Evidence("id1", "one") } } },
                new() { Lens = "Leader", Themes = new() { new() { Theme = "Trust", Level = 4, SelectedBehaviour = Evidence("id2", "two") } } }
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

    private static MaturityBehaviourEvidence Evidence(string id, string headline) => new()
    {
        BehaviourId = id,
        CatalogueVersion = "v1",
        Headline = headline,
        Signals = new() { "signal" },
        Examples = new() { "example" }
    };

    private class FakeHttpHandler : HttpMessageHandler
    {
        public string CurrentVersion { get; set; } = "v1";
        public bool CurrentMismatch { get; set; }

        private readonly string _mappingsJson = @"[
    {
        ""behaviourId"": ""Squad_Ownership_L1"",
        ""lens"": ""Squad"",
        ""theme"": ""Ownership"",
        ""level"": 1,
        ""signalName"": ""TeamOwnershipLevel"",
        ""domain"": ""Capability""
    },
    {
        ""behaviourId"": ""Squad_Ownership_L2"",
        ""lens"": ""Squad"",
        ""theme"": ""Ownership"",
        ""level"": 2,
        ""signalName"": ""TeamOwnershipLevel"",
        ""domain"": ""Capability""
    }
]";

        private readonly string _catalogueJson = @"{
  ""Squad"": { ""Ownership"": [
    { ""level"": 1, ""headline"": ""original"", ""signals"": [""signal""], ""examples"": [""example""] },
    { ""level"": 2, ""headline"": ""second"", ""signals"": [""signal""], ""examples"": [""example""] }
  ] }
}";

        private readonly string _registryJson = @"[
  { ""name"": ""TeamOwnershipLevel"", ""domain"": ""Capability"", ""type"": ""Behaviour"", ""normalisationRule"": ""value/5"" }
]";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;
            var content = "[]";
            if (url.Contains("/versions/") && !url.Contains("/versions/v1/") && !url.Contains("/versions/v2/"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            if (url.Contains("current.json"))
                content = $@"{{ ""currentVersion"": ""{CurrentVersion}"" }}";
            else if (url.Contains("maturity_behaviours.json"))
                content = url.Contains("/versions/v2/") || (!url.Contains("/versions/") && (CurrentVersion == "v2" || CurrentMismatch))
                    ? _catalogueJson.Replace("original", "revised")
                    : _catalogueJson;
            else if (url.Contains("maturity.json"))
                content = url.Contains("/versions/v2/") || (!url.Contains("/versions/") && CurrentVersion == "v2")
                    ? _mappingsJson.Replace("TeamOwnershipLevel", "RevisedOwnershipLevel")
                    : _mappingsJson;
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
    public async Task HistoricalSelection_RetainsContentAndUsesOriginalMappingAfterVersionChange()
    {
        var handler = new FakeHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var originalService = new MaturityEvidenceService(http);
        await originalService.InitializeAsync();
        var original = new BehaviourOption { Level = 1, Headline = "original", Signals = new() { "signal" }, Examples = new() { "example" } };
        var captured = await originalService.CaptureAsync("Squad", "Ownership", original);
        original.Signals[0] = "changed after capture";
        var theme = new MaturityTheme { Theme = "Ownership", Level = 1, SelectedBehaviourHeadline = captured.Headline, SelectedBehaviour = captured };
        var session = new AssessmentSession { LensAssessments = new() { new() { Lens = "Squad", Themes = new() { theme } } } };
        session.WorkspaceId = Guid.NewGuid().ToString();
        session.SquadId = Guid.NewGuid().ToString();
        var repository = new AssessmentRepositoryService(new LocalStorageService(new MemoryJsRuntime()));
        session.AssessmentId = Guid.NewGuid().ToString();
        await repository.SaveMaturitySessionAsync(session);

        handler.CurrentVersion = "v2";
        var newService = new MaturityEvidenceService(http);
        await newService.InitializeAsync();
        var reopened = await repository.GetMaturitySessionAsync(session.AssessmentId);
        Assert.NotNull(reopened);
        var saved = Assert.Single(Assert.Single(reopened.LensAssessments).Themes);
        var mapping = Assert.Single(await newService.ResolveSessionAsync(reopened));
        Assert.Equal("v1", saved.SelectedBehaviour!.CatalogueVersion);
        Assert.Equal("original", saved.SelectedBehaviour.Headline);
        Assert.Equal("signal", Assert.Single(saved.SelectedBehaviour.Signals));
        Assert.Equal("TeamOwnershipLevel", mapping.SignalName);
        Assert.Equal("Squad_Ownership_L1", mapping.BehaviourId);
        var registry = new SignalRegistryService(http);
        await registry.InitializeAsync();
        var builder = new MaturitySignalBuilderService(new SignalNormaliserService(), newService, registry);
        var (signals, traces) = await builder.BuildSignalsWithTrace(reopened);
        Assert.Equal("TeamOwnershipLevel", Assert.Single(signals).Name);
        Assert.Equal("Squad_Ownership_L1", Assert.Single(traces).BehaviourId);
        var revised = await newService.CaptureAsync("Squad", "Ownership",
            new BehaviourOption { Level = 1, Headline = "revised", Signals = new() { "signal" }, Examples = new() { "example" } });
        Assert.Equal("v2", revised.CatalogueVersion);
        Assert.Equal("revised", revised.Headline);
    }

    [Fact]
    public async Task HistoricalSelection_RejectsMissingOrInconsistentProvenance()
    {
        var http = new HttpClient(new FakeHttpHandler()) { BaseAddress = new Uri("http://localhost/") };
        var service = new MaturityEvidenceService(http);
        var theme = new MaturityTheme { Theme = "Ownership", Level = 1 };
        var legacy = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync("Squad", theme));
        Assert.Contains("provenance", legacy.Message);

        theme.SelectedBehaviour = Evidence("Squad_Ownership_L1", "original");
        theme.SelectedBehaviourHeadline = "original";
        theme.SelectedBehaviour.CatalogueVersion = "missing";
        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync("Squad", theme));
        Assert.Contains("version missing is unavailable", missing.Message);

        theme.SelectedBehaviour.CatalogueVersion = "v1";
        theme.SelectedBehaviour.BehaviourId = "wrong";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync("Squad", theme));
        theme.SelectedBehaviour.BehaviourId = "Squad_Ownership_L1";
        theme.SelectedBehaviour.Signals[0] = "altered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync("Squad", theme));
    }

    [Fact]
    public async Task CurrentDataMustMatchRetainedVersion()
    {
        var handler = new FakeHttpHandler { CurrentMismatch = true };
        var service = new MaturityEvidenceService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
        Assert.Contains("differs from its retained version", error.Message);
    }

    [Fact]
    public async Task DraftTransfer_PreservesTheVersionAndContentOriginallySelected()
    {
        var handler = new FakeHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var originalService = new MaturityEvidenceService(http);
        await originalService.InitializeAsync();
        var captured = await originalService.CaptureAsync("Squad", "Ownership",
            new BehaviourOption { Level = 1, Headline = "original", Signals = new() { "signal" }, Examples = new() { "example" } });
        var draft = new AssessmentSession
        {
            LensAssessments = new() { new() { Lens = "Squad", Themes = new()
            {
                new() { Theme = "Ownership", Level = 1, SelectedBehaviourHeadline = captured.Headline, SelectedBehaviour = captured }
            } } }
        };
        var target = new AssessmentSession
        {
            AssessmentId = Guid.NewGuid().ToString(),
            WorkspaceId = Guid.NewGuid().ToString(),
            SquadId = Guid.NewGuid().ToString(),
            LensAssessments = new() { new() { Lens = "Squad", Themes = new() { new() { Theme = "Ownership" } } } }
        };
        var repository = new AssessmentRepositoryService(new LocalStorageService(new MemoryJsRuntime()));
        await repository.SaveMaturitySessionAsync(target);

        handler.CurrentVersion = "v2";
        var evidenceService = new MaturityEvidenceService(http);
        await evidenceService.InitializeAsync();
        var registry = new SignalRegistryService(http);
        await registry.InitializeAsync();
        var rules = new TestRuleRepositoryService();
        var pipeline = new SignalPipelineService(
            new MaturitySignalBuilderService(new SignalNormaliserService(), evidenceService, registry),
            new DerivedSignalEngine(rules), new SystemRuleEngine(rules),
            new OperationalInsightEngine(rules), new HealthAggregationService());
        var update = Assert.Single(MaturityDraftTransfer.Plan(draft, target));
        await evidenceService.ResolveSessionAsync(draft);
        await new SessionService(repository, pipeline, evidenceService).UpdateThemeScoreAsync(
            target.AssessmentId, update.Lens, update.Theme, update.Level, update.Evidence);

        var saved = await repository.GetMaturitySessionAsync(target.AssessmentId);
        var selection = Assert.Single(Assert.Single(saved!.LensAssessments).Themes);
        Assert.Equal(1, selection.Level);
        Assert.Equal("v1", selection.SelectedBehaviour!.CatalogueVersion);
        Assert.Equal("Squad_Ownership_L1", selection.SelectedBehaviour.BehaviourId);
        Assert.Equal("original", selection.SelectedBehaviourHeadline);
        Assert.Equal("signal", Assert.Single(selection.SelectedBehaviour.Signals));
        Assert.Equal("example", Assert.Single(selection.SelectedBehaviour.Examples));
    }

    [Fact]
    public async Task BuildSignalsAndDerived_ShouldProduce_LowTeamOwnershipInsight_FromSession()
    {
        // Arrange
        var handler = new FakeHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var evidence = new MaturityEvidenceService(http);
        var registry = new SignalRegistryService(http);
        await registry.InitializeAsync();

        var normaliser = new SignalNormaliserService();
        var maturityBuilder = new MaturitySignalBuilderService(normaliser, evidence, registry);

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
                        new MaturityTheme { Theme = "Ownership", Level = 1, BehaviouralNote = "private note",
                            SelectedBehaviourHeadline = "original", SelectedBehaviour = Evidence("Squad_Ownership_L1", "original") }
                    }
                }
            }
        };

        // Act
        var (signals, behaviour, systemSignals, insights, evaluations, traces, insightEvaluation) =
            await pipeline.BuildSignalsAndDerivedDetailed(session);

        foreach (var s in signals) _output.WriteLine($"SIGNAL: {s.Name} ({s.NormalisedValue:0.00})");
        foreach (var b in behaviour) _output.WriteLine($"BEHAVIOUR: {b.Name} ({b.Intensity})");
        foreach (var i in insights) _output.WriteLine($"INSIGHT: {i.Name} ({i.Score})");

        // Assert
        Assert.Contains(signals, s => s.Name == "TeamOwnershipLevel");
        Assert.Contains(behaviour, b => b.Name == "LowTeamOwnership");
        Assert.Contains(insights, i => i.Name == "LowTeamOwnership");
        var trace = Assert.Single(traces);
        Assert.Equal("Squad_Ownership_L1", trace.BehaviourId);
        var input = Assert.Single(trace.Inputs);
        Assert.Equal(TraceInputKind.UserInput, input.Kind);
        Assert.Equal(1, input.Value);
        Assert.DoesNotContain("private note", string.Join(" ", trace.Inputs.Select(i => i.Value)));
        var path = new SignalTraceService().TraceInsight(Assert.Single(insightEvaluation.Entries), evaluations, traces);
        Assert.Contains(path, item => item.Description.Contains("Squad_Ownership_L1"));
        Assert.Contains(path, item => item.Description.Contains("Squad · Ownership · behaviour level: 1"));
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
        var evidenceService = new MaturityEvidenceService(http);
        var pipeline = new SignalPipelineService(
            new MaturitySignalBuilderService(new SignalNormaliserService(), evidenceService, registry),
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

        await new SessionService(repository, pipeline, evidenceService).UpdateThemeScoreAsync(
            session.AssessmentId, "Squad", "Ownership", 1, Evidence("Squad_Ownership_L1", "original"));

        var saved = await repository.GetMaturitySessionAsync(session.AssessmentId);
        Assert.NotNull(saved);
        var theme = Assert.Single(Assert.Single(saved.LensAssessments).Themes);
        Assert.Equal(1, theme.Level);
        Assert.Equal(string.Join(Environment.NewLine, "signal", "example"), theme.BehaviouralNote);
        Assert.Equal("original", theme.SelectedBehaviourHeadline);
        Assert.Equal("Squad_Ownership_L1", theme.SelectedBehaviour!.BehaviourId);
        Assert.Equal("v1", theme.SelectedBehaviour.CatalogueVersion);
        Assert.Equal(1, notifications);

        await new SessionService(repository, pipeline, evidenceService).UpdateThemeScoreAsync(
            session.AssessmentId, "Squad", "Ownership", 2, Evidence("Squad_Ownership_L2", "second"));
        saved = await repository.GetMaturitySessionAsync(session.AssessmentId);
        theme = Assert.Single(Assert.Single(saved!.LensAssessments).Themes);
        Assert.Equal(2, theme.Level);
        Assert.Equal("second", theme.SelectedBehaviourHeadline);
        Assert.Equal("Squad_Ownership_L2", theme.SelectedBehaviour!.BehaviourId);
        Assert.Equal(2, notifications);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SessionService(repository, pipeline, evidenceService).UpdateThemeScoreAsync(
                session.AssessmentId, "Squad", "Ownership", 1, Evidence("wrong", "original")));
        saved = await repository.GetMaturitySessionAsync(session.AssessmentId);
        theme = Assert.Single(Assert.Single(saved!.LensAssessments).Themes);
        Assert.Equal(2, theme.Level);
        Assert.Equal("Squad_Ownership_L2", theme.SelectedBehaviour!.BehaviourId);
        Assert.Equal(2, notifications);
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
