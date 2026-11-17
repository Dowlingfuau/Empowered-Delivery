using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;
using OperationalIntelligenceHub.Services;
using OperationalIntelligenceHub.Services.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OperationalIntelligenceHub.Components.Debug;
using OperationalIntelligenceHub.Components.Domain;

public class MaturitySemanticEvaluatorTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "OperationalIntelligenceHub", "wwwroot")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class DataHandler : HttpMessageHandler
    {
        public Dictionary<string, string?> Overrides { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            string? contents;
            if (!Overrides.TryGetValue(path, out contents))
            {
                var filename = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
                contents = File.Exists(filename) ? File.ReadAllText(filename) : null;
            }
            return Task.FromResult(contents is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(contents, Encoding.UTF8, "application/json") });
        }
    }

    private static (HttpClient Http, DataHandler Handler) CreateHttp()
    {
        var handler = new DataHandler();
        return (new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, handler);
    }

    private static async Task<(SemanticPatternDefinition Definition, MaturitySemanticEvaluator Evaluator)> CreateEngine(HttpClient http)
    {
        var definition = await new MaturitySemanticRuleRepository(http).LoadAsync("BF01");
        var evaluator = new MaturitySemanticEvaluator(new MaturitySemanticEvidenceProjector(new MaturityEvidenceService(http)));
        return (definition, evaluator);
    }

    private static MaturityTheme Selection(string lens, string theme, int level)
    {
        var file = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", "data", "maturity", "versions", "v1", "maturity_behaviours.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var option = document.RootElement.GetProperty(lens).GetProperty(theme).EnumerateArray()
            .Select(x => JsonSerializer.Deserialize<BehaviourOption>(x.GetRawText(), Options)!)
            .Single(x => x.Level == level);
        return new MaturityTheme
        {
            Theme = theme,
            Level = level,
            SelectedBehaviourHeadline = option.Headline,
            SelectedBehaviour = new MaturityBehaviourEvidence
            {
                BehaviourId = $"{lens}_{theme}_L{level}",
                CatalogueVersion = "v1",
                Headline = option.Headline,
                Signals = option.Signals.ToList(),
                Examples = option.Examples.ToList()
            }
        };
    }

    private static AssessmentSession Session(params (string Lens, string Theme, int Level)[] choices) => new()
    {
        AssessmentId = "assessment-1",
        LensAssessments = choices.GroupBy(x => x.Lens).Select(group => new MaturityLensAssessment
        {
            Lens = group.Key,
            Themes = group.Select(x => Selection(x.Lens, x.Theme, x.Level)).ToList()
        }).ToList()
    };

    public static IEnumerable<object[]> CoreCases()
    {
        for (var po = 1; po <= 5; po++)
        for (var squad = 1; squad <= 5; squad++)
        {
            var direction = po >= 3 && squad <= 2 ? "DirectionA" : po <= 2 && squad >= 3 ? "DirectionB" : null;
            var branch = direction == "DirectionA" ? "DirectionA_ProductOwnerAhead" :
                direction == "DirectionB" ? "DirectionB_SquadAhead" :
                po <= 2 ? "SharedLowOrientation" : "SharedValueOrientation";
            yield return new object[] { po, squad, direction!, branch };
        }
    }

    [Theory]
    [MemberData(nameof(CoreCases))]
    public async Task CorePairs_ReturnExactOutcomeDirectionAndTrace(int po, int squad, string? direction, string branch)
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", po), ("Squad", "Value", squad)), definition);
        Assert.Equal(direction is null ? SemanticOutcome.NoMatch : SemanticOutcome.Candidate, result.Outcome);
        Assert.Equal(direction, result.Direction);
        Assert.Equal(branch, result.MatchedBranchId);
        Assert.Single(result.Branches.Where(x => x.Condition.Result == SemanticTruth.True));
        Assert.All(result.Trace.Where(x => x.Stage == "Branch" && x.Result == "False"),
            x => Assert.Contains("condition evaluated False", x.Reason));
        Assert.Equal("v1", result.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Snapshot!.CatalogueVersion);
        Assert.Equal($"Squad_Value_L{squad}", result.Evidence.Single(x => x.Coordinate == "Squad.Value").Snapshot!.BehaviourId);
        Assert.Contains(result.Trace, x => x.Stage == "Candidate" && x.Result == (direction is null ? "NotCreated" : "Created"));
    }

    public static IEnumerable<object[]> ProfileCases()
    {
        var profiles = new Dictionary<string, (string A, string B)>
        {
            ["ScrumMaster.Value"] = ("XXQQQ", "XXQQQ"),
            ["Leader.Value"] = ("XXQQQ", "XXQQQ"),
            ["ProductOwner.Ownership"] = ("XXXXX", "XXXXX"),
            ["Squad.Ownership"] = ("XXXXX", "XXXXX"),
            ["Leader.Ownership"] = ("XXXXX", "XXXXX"),
            ["Leader.Trust"] = ("XXXXX", "XXXXX"),
            ["ScrumMaster.Ownership"] = ("XXXXX", "XXXXX"),
            ["ScrumMaster.Trust"] = ("XXXXX", "XXXXX")
        };
        foreach (var (coordinate, profilesByDirection) in profiles)
        foreach (var direction in new[] { "DirectionA", "DirectionB" })
        for (var level = 1; level <= 5; level++)
        {
            var letter = (direction == "DirectionA" ? profilesByDirection.A : profilesByDirection.B)[level - 1];
            var expected = letter == 'Q' ? "Qualifying" : "Contextual";
            yield return new object[] { coordinate, direction, level, expected };
        }
    }

    [Theory]
    [MemberData(nameof(ProfileCases))]
    public async Task SupportingProfiles_UseConfiguredDirectionAndLevel(string coordinate, string direction, int level, string expected)
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var parts = coordinate.Split('.');
        var session = Session(("ProductOwner", "Value", direction == "DirectionA" ? 3 : 1),
            ("Squad", "Value", direction == "DirectionA" ? 1 : 3), (parts[0], parts[1], level));
        var result = await evaluator.EvaluateAsync(session, definition);
        var contribution = result.Contributions.Single(x => x.Coordinate == coordinate);
        Assert.Equal(SemanticOutcome.Candidate, result.Outcome);
        Assert.Equal("Evaluated", contribution.Status);
        Assert.Equal(expected, contribution.Classification);
        Assert.Contains(result.Trace, x => x.Stage == "Contribution" && x.Subject == coordinate && x.EvidenceIds.Contains(contribution.EvidenceId));
    }

    [Fact]
    public async Task MissingCoreAndSupportingOnly_DoNotProduceCandidate()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("Leader", "Trust", 1)), definition);
        Assert.Equal(SemanticOutcome.Unknown, result.Outcome);
        Assert.All(result.Contributions, x => Assert.Equal("Skipped", x.Status));
        Assert.All(result.ContextRelationships, x => Assert.Equal("NotEvaluated", x.Status));
        Assert.All(result.Evidence.Where(x => x.Coordinate is "ProductOwner.Value" or "Squad.Value"),
            x => Assert.Equal(SemanticEvidenceStatus.Missing, x.Status));
    }

    [Theory]
    [InlineData("ProductOwner")]
    [InlineData("Squad")]
    public async Task OneMissingCore_IsUnknownNotFalse(string missingLens)
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var lens = missingLens == "ProductOwner" ? "Squad" : "ProductOwner";
        var result = await evaluator.EvaluateAsync(Session((lens, "Value", 3)), definition);
        Assert.Equal(SemanticOutcome.Unknown, result.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Missing, result.Evidence.Single(x => x.Lens == missingLens && x.Theme == "Value").Status);
        Assert.Contains(result.Branches, x => x.Condition.Result == SemanticTruth.Unknown);
        Assert.Contains(result.Trace, x => x.Stage == "CoreOutcome" && x.Reason.Contains(missingLens));
    }

    [Fact]
    public async Task MatchAndNonMatch_ExposeDefinitionAndEvidenceLinkedExplanation()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        foreach (var (squad, outcome, branch) in new[]
        {
            (1, SemanticOutcome.Candidate, "DirectionA_ProductOwnerAhead"),
            (3, SemanticOutcome.NoMatch, "SharedValueOrientation")
        })
        {
            var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 3), ("Squad", "Value", squad)), definition);
            Assert.Equal(outcome, result.Outcome);
            Assert.Equal("BF01", result.Definition.PatternId);
            Assert.Equal("2.0", result.Definition.DefinitionVersion);
            Assert.Equal(MaturitySemanticRuleRepository.DefaultSource, result.Definition.Source);
            Assert.Equal("maturity-semantic-v2", result.Definition.Contract);
            Assert.False(string.IsNullOrWhiteSpace(result.Definition.DefinitionFingerprint));
            Assert.Equal("v1", Assert.Single(result.Definition.CompatibleCatalogueVersions));
            var matched = result.Branches.Single(x => x.BranchId == branch);
            Assert.Equal(SemanticTruth.True, matched.Condition.Result);
            Assert.Equal(2, matched.Condition.EvidenceIds.Count);
            Assert.All(matched.Condition.EvidenceIds, id => Assert.Contains(result.Evidence, x => x.EvidenceId == id));
            Assert.Contains(result.Trace, x => x.Stage == "CoreOutcome" && x.Result == outcome.ToString() && !string.IsNullOrWhiteSpace(x.Reason));
        }
    }

    [Fact]
    public async Task DuplicateCoordinate_IsInvalidAndPreservesReason()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        session.LensAssessments[0].Themes.Add(Selection("ProductOwner", "Value", 4));
        var result = await evaluator.EvaluateAsync(session, definition);
        var evidence = result.Evidence.Single(x => x.Coordinate == "ProductOwner.Value");
        Assert.Equal(SemanticOutcome.Unavailable, result.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Invalid, evidence.Status);
        Assert.Contains("Duplicate", evidence.Reason);
        Assert.Equal(2, evidence.ConflictingSelections!.Count);
        Assert.Contains(result.Trace, x => x.Stage == "Acquisition" && x.Subject == "ProductOwner.Value" && x.Result == "Ambiguous");
    }

    [Fact]
    public async Task ContextualEvidenceAndPendingRelationships_DoNotChangeCoreOutcome()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1),
            ("Leader", "Ownership", 4), ("Leader", "Trust", 1));
        var result = await evaluator.EvaluateAsync(session, definition);
        Assert.Equal(SemanticOutcome.Candidate, result.Outcome);
        Assert.Contains(result.Contributions, x => x.Coordinate == "Leader.Ownership" && x.Classification == "Contextual");
        Assert.Contains(result.Contributions, x => x.Coordinate == "Leader.Trust" && x.Classification == "Contextual" && x.PotentialRelevance != null);
        var relationship = result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust");
        Assert.Equal("NotEvaluated", relationship.Status);
        Assert.Equal("ContextOnly", relationship.Effect);
        Assert.Null(relationship.Condition);
        Assert.Equal("NotAssessed", result.AssessmentStates.Confidence);
        Assert.Equal("NotAssessed", result.AssessmentStates.Priority);
        Assert.Contains(result.Trace, x => x.Stage == "Candidate" && x.Result == "Created");
    }

    [Fact]
    public async Task ContextualAndMissingEvidence_RemainDistinct()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 1), ("Squad", "Value", 3),
            ("ScrumMaster", "Ownership", 3)), definition);
        Assert.Equal("Contextual", result.Contributions.Single(x => x.Coordinate == "ScrumMaster.Ownership").Classification);
        var missing = result.Contributions.Single(x => x.Coordinate == "Leader.Trust");
        Assert.Equal("Missing", missing.Status);
        Assert.Null(missing.Classification);
        Assert.Equal("NotEvaluated", result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust").Status);
    }

    [Fact]
    public async Task ContextualRelationship_RemainsUnevaluatedEvenWhenEvidenceIsPresent()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1),
            ("Leader", "Ownership", 1), ("Leader", "Trust", 1)), definition);
        var relationship = result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust");
        Assert.Equal("NotEvaluated", relationship.Status);
        Assert.Null(relationship.Condition);
        Assert.Equal(2, relationship.EvidenceIds.Count);
    }

    [Theory]
    [InlineData("DelegationWithDirectiveTrust")]
    [InlineData("OwnershipWithProcessCompliance")]
    [InlineData("ValueIntentWithCentralDirection")]
    [InlineData("ValueImprovementWithOperationalDirection")]
    public async Task ContextRelationships_HandleMatchFalseAndMissingWithoutChangingCore(string id)
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var inputs = id switch
        {
            "DelegationWithDirectiveTrust" => new[] { ("Leader", "Ownership", 4), ("Leader", "Trust", 1) },
            "OwnershipWithProcessCompliance" => new[] { ("ScrumMaster", "Ownership", 3), ("ScrumMaster", "Trust", 1) },
            "ValueIntentWithCentralDirection" => new[] { ("Leader", "Value", 4), ("Leader", "Ownership", 1), ("Leader", "Trust", 1) },
            _ => new[] { ("ScrumMaster", "Value", 3), ("ScrumMaster", "Ownership", 1), ("ScrumMaster", "Trust", 1) }
        };
        var core = new[] { ("ProductOwner", "Value", 3), ("Squad", "Value", 1) };
        var matching = await evaluator.EvaluateAsync(Session(core.Concat(inputs).ToArray()), definition);
        var missing = await evaluator.EvaluateAsync(Session(core.Concat(inputs.Skip(1)).ToArray()), definition);
        var wrong = inputs.ToArray();
        wrong[0] = (wrong[0].Item1, wrong[0].Item2, id == "OwnershipWithProcessCompliance" ? 1 : 2);
        var nonmatching = await evaluator.EvaluateAsync(Session(core.Concat(wrong).ToArray()), definition);
        Assert.All(new[] { matching, missing, nonmatching }, x => Assert.Equal(SemanticOutcome.Candidate, x.Outcome));
        Assert.All(new[] { matching, missing, nonmatching }, x =>
            Assert.Equal("NotEvaluated", x.ContextRelationships.Single(r => r.RelationshipId == id).Status));
        Assert.Equal("ContextOnly", matching.ContextRelationships.Single(x => x.RelationshipId == id).Effect);
    }

    [Fact]
    public async Task IncompatibleInvalidAndUnavailableSelections_AreNotNonMatches()
    {
        var (http, handler) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var po = session.LensAssessments[0].Themes[0];
        po.SelectedBehaviour!.CatalogueVersion = "v2";
        var incompatible = await evaluator.EvaluateAsync(session, definition);
        Assert.Equal(SemanticOutcome.Unavailable, incompatible.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Incompatible, incompatible.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Status);
        po.SelectedBehaviour.CatalogueVersion = "v1";
        po.SelectedBehaviour.BehaviourId = "wrong";
        var invalid = await evaluator.EvaluateAsync(session, definition);
        Assert.Equal(SemanticOutcome.Unavailable, invalid.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Invalid, invalid.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Status);
        po.SelectedBehaviour.BehaviourId = "ProductOwner_Value_L3";
        handler.Overrides["data/maturity/versions/v1/maturity.json"] = null;
        var freshEvaluator = new MaturitySemanticEvaluator(new MaturitySemanticEvidenceProjector(new MaturityEvidenceService(http)));
        var unavailable = await freshEvaluator.EvaluateAsync(session, definition);
        Assert.Equal(SemanticOutcome.Unavailable, unavailable.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Unavailable, unavailable.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Status);
    }

    [Fact]
    public async Task Evaluations_AreDeterministicAndDoNotMutateOrShareAssessmentEvidence()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var firstSession = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var before = JsonSerializer.Serialize(firstSession);
        var first = await evaluator.EvaluateAsync(firstSession, definition);
        var unrelated = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 1), ("Squad", "Value", 1)), definition);
        var again = await evaluator.EvaluateAsync(firstSession, definition);
        Assert.Equal(before, JsonSerializer.Serialize(firstSession));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(again));
        Assert.Equal(SemanticOutcome.NoMatch, unrelated.Outcome);
        first.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Snapshot!.Signals[0] = "changed";
        Assert.Equal(before, JsonSerializer.Serialize(firstSession));
    }

    public static IEnumerable<object[]> TruthCases()
    {
        foreach (var op in new[] { "all", "any" })
        foreach (var a in Enum.GetValues<SemanticTruth>())
        foreach (var b in Enum.GetValues<SemanticTruth>())
        {
            var expected = op == "all"
                ? a == SemanticTruth.False || b == SemanticTruth.False ? SemanticTruth.False
                    : a == SemanticTruth.True && b == SemanticTruth.True ? SemanticTruth.True : SemanticTruth.Unknown
                : a == SemanticTruth.True || b == SemanticTruth.True ? SemanticTruth.True
                    : a == SemanticTruth.False && b == SemanticTruth.False ? SemanticTruth.False : SemanticTruth.Unknown;
            yield return new object[] { op, a, b, expected };
        }
    }

    [Theory]
    [MemberData(nameof(TruthCases))]
    public void LogicalOperators_RespectThreeValuedSemanticsAndTraceAllOperands(string op, SemanticTruth a, SemanticTruth b, SemanticTruth expected)
    {
        using var json = JsonDocument.Parse($"{{\"{op}\":[{{\"hasGroup\":\"a\"}},{{\"hasGroup\":\"b\"}}]}}");
        var groups = new Dictionary<string, SemanticGroup>
        {
            ["a"] = new() { GroupId = "a", Coordinate = "A.Value", Levels = new() { 1 } },
            ["b"] = new() { GroupId = "b", Coordinate = "B.Value", Levels = new() { 1 } }
        };
        SemanticSelection Item(string coordinate, SemanticTruth truth) => new(coordinate, "test", coordinate,
            coordinate.Split('.')[0], "Value", truth == SemanticTruth.False ? 2 : 1,
            new MaturityBehaviourEvidence { BehaviourId = coordinate }, truth == SemanticTruth.Unknown ? SemanticEvidenceStatus.Missing : SemanticEvidenceStatus.Valid, "test");
        var evidence = new Dictionary<string, SemanticSelection> { ["A.Value"] = Item("A.Value", a), ["B.Value"] = Item("B.Value", b) };
        var trace = SemanticConditionEvaluator.Evaluate(json.RootElement, "test", evidence, groups);
        Assert.Equal(expected, trace.Result);
        Assert.Equal(2, trace.Operands.Count);
        Assert.Equal(b, trace.Operands[1].Result);
    }

    [Theory]
    [InlineData(SemanticEvidenceStatus.Valid, "Squad_Value_L1", SemanticTruth.True)]
    [InlineData(SemanticEvidenceStatus.Valid, "Squad_Value_L2", SemanticTruth.False)]
    [InlineData(SemanticEvidenceStatus.Missing, null, SemanticTruth.Unknown)]
    [InlineData(SemanticEvidenceStatus.Unavailable, "Squad_Value_L1", SemanticTruth.Unknown)]
    [InlineData(SemanticEvidenceStatus.Incompatible, "Squad_Value_L1", SemanticTruth.Unknown)]
    public void HasBehaviour_PreservesUnknownReasons(SemanticEvidenceStatus status, string? id, SemanticTruth expected)
    {
        using var json = JsonDocument.Parse("{\"hasBehaviour\":{\"coordinate\":\"Squad.Value\",\"behaviourIds\":[\"Squad_Value_L1\"]}}");
        var selection = new SemanticSelection("e1", "s1", "Squad.Value", "Squad", "Value", 1,
            id is null ? null : new MaturityBehaviourEvidence { BehaviourId = id }, status, "reason");
        var trace = SemanticConditionEvaluator.Evaluate(json.RootElement, "condition", new Dictionary<string, SemanticSelection>
        {
            ["Squad.Value"] = selection
        }, new Dictionary<string, SemanticGroup>());
        Assert.Equal(expected, trace.Result);
        Assert.Equal("e1", Assert.Single(trace.EvidenceIds));
        if (status != SemanticEvidenceStatus.Valid) Assert.Contains(status.ToString(), trace.Reason);
    }

    [Theory]
    [InlineData("unknownOperator")]
    [InlineData("duplicateGroup")]
    [InlineData("unknownGroup")]
    [InlineData("missingProfileLevel")]
    [InlineData("wrongCoordinate")]
    [InlineData("overlappingBranch")]
    [InlineData("incompleteBranch")]
    [InlineData("missingDirection")]
    [InlineData("malformedExpression")]
    [InlineData("impossibleContext")]
    [InlineData("nullProfiles")]
    [InlineData("missingQualification")]
    [InlineData("trustQualification")]
    [InlineData("activeContext")]
    [InlineData("unsupportedContract")]
    public async Task InvalidDefinition_IsRejectedWithActionableError(string mutation)
    {
        var (http, handler) = CreateHttp();
        var path = MaturitySemanticRuleRepository.DefaultSource;
        var file = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
        var root = JsonNode.Parse(File.ReadAllText(file))!;
        var semantic = root["rules"]![0]!["semanticDefinition"]!;
        var groups = semantic["coreRelationship"]!["groupDefinitions"]!.AsArray();
        var branches = semantic["coreRelationship"]!["branches"]!.AsArray();
        switch (mutation)
        {
            case "unknownOperator": branches[0]!["when"] = JsonNode.Parse("{\"not\":true}"); break;
            case "duplicateGroup": groups[1]!["groupId"] = groups[0]!["groupId"]!.GetValue<string>(); break;
            case "unknownGroup": branches[0]!["when"]!["all"]![0]!["hasGroup"] = "missing"; break;
            case "missingProfileLevel": semantic["supportingEvidenceProfiles"]![0]!["contributions"]!["DirectionA"]!.AsObject().Remove("1"); break;
            case "wrongCoordinate": semantic["contextRelationships"]![0]!["when"]!["all"]![0]!["hasBehaviour"]!["behaviourIds"]![0] = "Squad_Value_L1"; break;
            case "overlappingBranch": branches[1]!["when"] = branches[0]!["when"]!.DeepClone(); break;
            case "incompleteBranch": branches.RemoveAt(0); break;
            case "missingDirection": branches[0]!.AsObject().Remove("direction"); break;
            case "malformedExpression": branches[0]!["when"] = JsonNode.Parse("{\"all\":[]}"); break;
            case "impossibleContext":
                semantic["contextRelationships"]![0]!["when"] = JsonNode.Parse(
                    "{\"all\":[{\"hasBehaviour\":{\"coordinate\":\"Leader.Trust\",\"behaviourIds\":[\"Leader_Trust_L1\"]}},{\"hasBehaviour\":{\"coordinate\":\"Leader.Trust\",\"behaviourIds\":[\"Leader_Trust_L4\"]}}]}");
                break;
            case "nullProfiles": semantic["supportingEvidenceProfiles"] = null; break;
            case "missingQualification": semantic["qualificationRules"]!.AsArray().RemoveAt(0); break;
            case "trustQualification":
                semantic["supportingEvidenceProfiles"]!.AsArray().First(x => x!["coordinate"]!.GetValue<string>() == "Leader.Trust")!
                    ["contributions"]!["DirectionA"]!["1"] = "Qualifying";
                break;
            case "activeContext": semantic["contextRelationships"]![0]!["status"] = "Approved"; break;
            case "unsupportedContract": root["rules"]![0]!["evaluation"]!["contract"] = "unknown"; break;
        }

        handler.Overrides[path] = root.ToJsonString();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new MaturitySemanticRuleRepository(http).LoadAsync("BF01"));
        Assert.Contains("Invalid semantic definition", exception.Message);
    }

    [Fact]
    public async Task ProjectedEvidence_CanBeEvaluatedIndependentlyOfSessionAcquisition()
    {
        var (http, _) = CreateHttp();
        var (definition, _) = await CreateEngine(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var projection = await new MaturitySemanticEvidenceProjector(new MaturityEvidenceService(http)).ProjectAsync(session, definition);
        var result = MaturitySemanticEvaluator.Evaluate(definition, projection);
        Assert.Equal(SemanticOutcome.Candidate, result.Outcome);
        Assert.Equal("DirectionA", result.Direction);
        projection["ProductOwner.Value"].Snapshot!.Signals[0] = "changed";
        Assert.NotEqual("changed", result.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Snapshot!.Signals[0]);
    }

    [Fact]
    public async Task ModifiedAfterValidation_IsRejectedBeforeEvidenceEvaluation()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        definition.SemanticDefinition.CoreRelationship.Branches[0].Direction = "DirectionB";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1)), definition));
        Assert.Contains("modified after validation", exception.Message);
    }

    [Fact]
    public async Task ChangedRuleWithSameVersion_HasDifferentProvenanceAndChangesConfiguredResult()
    {
        var (http, handler) = CreateHttp();
        var (original, evaluator) = await CreateEngine(http);
        var path = MaturitySemanticRuleRepository.DefaultSource;
        var file = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
        var root = JsonNode.Parse(File.ReadAllText(file))!;
        root["rules"]![0]!["semanticDefinition"]!["supportingEvidenceProfiles"]![0]!["principle"] = "Updated explanatory principle.";
        handler.Overrides[path] = root.ToJsonString();
        var revised = await new MaturitySemanticRuleRepository(http).LoadAsync("BF01");
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1), ("ScrumMaster", "Value", 1));
        var before = await evaluator.EvaluateAsync(session, original);
        var after = await evaluator.EvaluateAsync(session, revised);
        Assert.Equal(before.Definition.DefinitionVersion, after.Definition.DefinitionVersion);
        Assert.NotEqual(before.Definition.DefinitionFingerprint, after.Definition.DefinitionFingerprint);
        Assert.NotEqual(before.Contributions.Single(x => x.Coordinate == "ScrumMaster.Value").Reason,
            after.Contributions.Single(x => x.Coordinate == "ScrumMaster.Value").Reason);
    }

    [Theory]
    [InlineData(3, 1, SemanticOutcome.Candidate, "EligibleProvisional")]
    [InlineData(1, 3, SemanticOutcome.Candidate, "EligibleProvisional")]
    [InlineData(1, 1, SemanticOutcome.NoMatch, "NotEligible")]
    [InlineData(3, 3, SemanticOutcome.NoMatch, "NotEligible")]
    public async Task SemanticRun_PreservesEvaluationAndCreatesOnlyEligibleFindings(
        int po, int squad, SemanticOutcome outcome, string eligibility)
    {
        var (http, _) = CreateHttp();
        var service = CreateRunService(http);
        var run = await service.EvaluateAsync(Session(("ProductOwner", "Value", po), ("Squad", "Value", squad)));
        Assert.Null(run.Error);
        Assert.Equal(outcome, run.Evaluation!.Outcome);
        Assert.Equal(eligibility, run.Eligibility);
        Assert.Equal(outcome == SemanticOutcome.Candidate, run.Finding is not null);
        Assert.NotNull(run.Interpretation);
        Assert.NotEmpty(run.RunId);
        Assert.NotEmpty(run.AssessmentFingerprint);
        var branch = run.Evaluation.Branches.Single(x => x.Condition.Result == SemanticTruth.True);
        Assert.All(branch.Condition.EvidenceIds, id => Assert.Contains(run.Evaluation.Evidence, evidence => evidence.EvidenceId == id));
        Assert.Contains("hasGroup", branch.Condition.Operands[0].Expression);
        Assert.Contains(run.Evaluation.GroupDefinitions, x => x.GroupId == "ValueOrientation" && x.Coordinate == "ProductOwner.Value");
    }

    [Fact]
    public async Task SemanticRun_DistinguishesUnknownAndUnavailableEvidence()
    {
        var (http, _) = CreateHttp();
        var service = CreateRunService(http);
        var unknown = await service.EvaluateAsync(Session(("ProductOwner", "Value", 3)));
        Assert.Equal(SemanticOutcome.Unknown, unknown.Evaluation!.Outcome);
        Assert.Equal("NotAssessed", unknown.Eligibility);
        Assert.Null(unknown.Finding);
        Assert.Equal(SemanticEvidenceStatus.Missing, unknown.Evaluation.Evidence.Single(x => x.Coordinate == "Squad.Value").Status);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        session.LensAssessments[0].Themes[0].SelectedBehaviour!.CatalogueVersion = "v2";
        var unavailable = await service.EvaluateAsync(session);
        Assert.Equal(SemanticOutcome.Unavailable, unavailable.Evaluation!.Outcome);
        Assert.Equal(SemanticEvidenceStatus.Incompatible, unavailable.Evaluation.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Status);
        Assert.Equal("NotAssessed", unavailable.Eligibility);
        Assert.Null(unavailable.Finding);
    }

    [Fact]
    public async Task SemanticRun_ReusesUnchangedSnapshotButReevaluatesChangesToEvidenceAndDefinition()
    {
        var (http, handler) = CreateHttp();
        var service = CreateRunService(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var first = await service.EvaluateAsync(session);
        var same = await service.EvaluateAsync(session, first);
        Assert.Same(first, same);
        session.LensAssessments[1].Themes[0] = Selection("Squad", "Value", 3);
        var changed = await service.EvaluateAsync(session, same);
        Assert.NotEqual(first.RunId, changed.RunId);
        Assert.NotEqual(first.AssessmentFingerprint, changed.AssessmentFingerprint);
        Assert.Equal(SemanticOutcome.NoMatch, changed.Evaluation!.Outcome);

        var path = MaturitySemanticRuleRepository.DefaultSource;
        var file = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
        var root = JsonNode.Parse(File.ReadAllText(file))!;
        root["rules"]![0]!["semanticDefinition"]!["supportingEvidenceProfiles"]![0]!["principle"] = "Updated explanatory principle.";
        handler.Overrides[path] = root.ToJsonString();
        var changedRule = await service.EvaluateAsync(session, changed);
        Assert.NotEqual(changed.RunId, changedRule.RunId);
        Assert.NotEqual(changed.Evaluation.Definition.DefinitionFingerprint, changedRule.Evaluation!.Definition.DefinitionFingerprint);
    }

    [Fact]
    public async Task SemanticRun_ReportsDefinitionFailureAndRetriesWithoutReusingOldResult()
    {
        var (http, handler) = CreateHttp();
        var service = CreateRunService(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var original = await service.EvaluateAsync(session);
        handler.Overrides[MaturitySemanticRuleRepository.DefaultSource] = null;
        var failure = await service.EvaluateAsync(session, original);
        Assert.Null(failure.Evaluation);
        Assert.Equal("DefinitionLoading", failure.FailedStage);
        Assert.False(string.IsNullOrWhiteSpace(failure.Error));
        Assert.Equal("EvaluationError", failure.Eligibility);
        handler.Overrides.Remove(MaturitySemanticRuleRepository.DefaultSource);
        var retry = await service.EvaluateAsync(session, failure);
        Assert.Null(retry.Error);
        Assert.Equal(SemanticOutcome.Candidate, retry.Evaluation!.Outcome);
        Assert.NotEqual(original.RunId, retry.RunId);
    }

    [Theory]
    [InlineData(3, 1, "DirectionA")]
    [InlineData(1, 3, "DirectionB")]
    public async Task EligibleFinding_UsesOriginalEvidenceAndOnlyApprovedQualifications(int po, int squad, string direction)
    {
        var (http, _) = CreateHttp();
        var run = await CreateRunService(http).EvaluateAsync(Session(
            ("ProductOwner", "Value", po), ("Squad", "Value", squad),
            ("ScrumMaster", "Value", 3), ("Leader", "Value", 5),
            ("ScrumMaster", "Trust", 3), ("Leader", "Trust", 1),
            ("ProductOwner", "Ownership", 2)));
        Assert.Null(run.Error);
        Assert.Equal("EligibleProvisional", run.Eligibility);
        var finding = Assert.IsType<MaturityDiagnosticFinding>(run.Finding);
        Assert.Equal(direction, finding.Direction);
        Assert.Equal("Performance", Assert.Single(finding.OperationalDimensions));
        Assert.Contains(run.Evaluation!.Evidence.Single(x => x.Coordinate == "ProductOwner.Value").Snapshot!.Headline, finding.Observed);
        Assert.Contains(run.Evaluation.Evidence.Single(x => x.Coordinate == "Squad.Value").Snapshot!.Headline, finding.Observed);
        Assert.Equal(2, finding.Qualifications.Count);
        Assert.All(finding.Qualifications, x =>
        {
            Assert.EndsWith(".Value", x.Coordinate);
            Assert.False(string.IsNullOrWhiteSpace(x.Boundary));
            Assert.Contains(finding.Evidence, evidence => evidence.EvidenceId == x.EvidenceId);
        });
        Assert.All(run.Evaluation.Contributions.Where(x => x.Coordinate.EndsWith(".Trust")),
            x => Assert.Equal("Contextual", x.Classification));
        Assert.All(run.Evaluation.ContextRelationships, x => Assert.Equal("NotEvaluated", x.Status));
        Assert.NotEmpty(run.Interpretation!.UnavailableEvidenceIds);
        Assert.Equal("NotAssessed", run.Evaluation.AssessmentStates.Confidence);
        Assert.Equal("NotAssessed", run.Evaluation.AssessmentStates.Priority);
    }

    [Fact]
    public async Task SemanticRun_NarrativeFailurePreservesEligibilityAndRetryRecomposes()
    {
        var (http, handler) = CreateHttp();
        var service = CreateRunService(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        handler.Overrides[MaturityNarrativeComposer.Source] = null;
        var failure = await service.EvaluateAsync(session);
        Assert.Equal("NarrativeComposition", failure.FailedStage);
        Assert.NotNull(failure.Error);
        Assert.Equal("EligibleProvisional", failure.Eligibility);
        Assert.Equal(SemanticOutcome.Candidate, failure.Evaluation!.Outcome);
        Assert.NotNull(failure.Interpretation);
        Assert.Null(failure.Finding);
        handler.Overrides.Remove(MaturityNarrativeComposer.Source);
        var retry = await service.EvaluateAsync(session, failure);
        Assert.Null(retry.Error);
        Assert.NotNull(retry.Finding);
        Assert.Same(failure.Evaluation, retry.Evaluation);
    }

    [Fact]
    public async Task SemanticRun_ChangedNarrativeRecomposesWithoutReevaluatingEvidence()
    {
        var (http, handler) = CreateHttp();
        var service = CreateRunService(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var first = await service.EvaluateAsync(session);
        var path = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot",
            MaturityNarrativeComposer.Source.Replace('/', Path.DirectorySeparatorChar));
        var narrative = JsonNode.Parse(File.ReadAllText(path))!;
        narrative["directions"]!["DirectionA"]!["action"] = "Updated action.";
        handler.Overrides[MaturityNarrativeComposer.Source] = narrative.ToJsonString();
        var changed = await service.EvaluateAsync(session, first);
        Assert.Null(changed.Error);
        Assert.Same(first.Evaluation, changed.Evaluation);
        Assert.NotEqual(first.Finding!.NarrativeFingerprint, changed.Finding!.NarrativeFingerprint);
        Assert.Equal("Updated action.", changed.Finding.Action);
    }

    [Fact]
    public async Task SemanticRun_PolicyVersionChangeRecomposes()
    {
        var (http, _) = CreateHttp();
        var service = CreateRunService(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1));
        var original = await service.EvaluateAsync(session);
        var stale = original with { EligibilityDecision = original.EligibilityDecision! with { PolicyVersion = "old" } };
        var updated = await service.EvaluateAsync(session, stale);
        Assert.NotEqual(original.RunId, updated.RunId);
        Assert.Equal(MaturityEligibilityPolicy.PolicyVersion, updated.EligibilityDecision!.PolicyVersion);
        Assert.Same(original.Evaluation, updated.Evaluation);
        Assert.NotNull(updated.Finding);
    }

    [Fact]
    public async Task OperationalDimensionAssociations_RetainMultipleConfiguredDimensions()
    {
        var (http, handler) = CreateHttp();
        var path = MaturitySemanticRuleRepository.DefaultSource;
        var filename = Path.Combine(Root, "OperationalIntelligenceHub", "wwwroot", path.Replace('/', Path.DirectorySeparatorChar));
        var definition = JsonNode.Parse(File.ReadAllText(filename))!;
        definition["rules"]![0]!["semanticDefinition"]!["operationalDimensions"]!.AsArray().Add("Clarity");
        handler.Overrides[path] = definition.ToJsonString();
        var run = await CreateRunService(http).EvaluateAsync(Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1)));
        Assert.Null(run.Error);
        Assert.Equal(new[] { "Performance", "Clarity" }, run.Finding!.OperationalDimensions);
    }

    [Fact]
    public async Task FindingView_RendersFourPartNarrativeAndEvidence()
    {
        var (http, _) = CreateHttp();
        var run = await CreateRunService(http).EvaluateAsync(Session(
            ("ProductOwner", "Value", 3), ("Squad", "Value", 1), ("Leader", "Value", 3)));
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Finding"] = run.Finding, ["Profile"] = run.Interpretation
            });
            return (await renderer.RenderComponentAsync<MaturityDiagnosticFindingView>(parameters)).ToHtmlString();
        });
        Assert.Contains("What we observed", html);
        Assert.Contains("What we suspect", html);
        Assert.Contains("Why it may matter", html);
        Assert.Contains("What could be done", html);
        Assert.Contains("ProductOwner_Value_L3", html);
        Assert.Contains("Boundary:", html);
        Assert.Contains("missing or unavailable", html);
    }

    [Fact]
    public async Task SemanticTrace_RendersRecordedEvidenceConditionsAndEligibility()
    {
        var (http, _) = CreateHttp();
        var run = await CreateRunService(http).EvaluateAsync(Session(
            ("ProductOwner", "Value", 3), ("Squad", "Value", 1), ("Leader", "Trust", 1)));
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { ["Run"] = run });
            return (await renderer.RenderComponentAsync<MaturitySemanticTrace>(parameters)).ToHtmlString();
        });
        Assert.Contains(run.RunId, html);
        Assert.Contains("ProductOwner_Value_L3", html);
        Assert.Contains("DirectionA_ProductOwnerAhead", html);
        Assert.Contains("hasGroup", html);
        Assert.Contains("Leader.Trust", html);
        Assert.Contains("Contextual", html);
        Assert.Contains("Missing", html);
        Assert.Contains("NotAssessed", html);
        Assert.Contains("Provisional diagnostic finding and narrative", html);
    }

    [Fact]
    public async Task DiagnosticsConsole_ShowsSemanticFailureOnlyWhenRequested()
    {
        var (http, handler) = CreateHttp();
        handler.Overrides[MaturitySemanticRuleRepository.DefaultSource] = null;
        var run = await CreateRunService(http).EvaluateAsync(Session(("ProductOwner", "Value", 3)));
        var services = new ServiceCollection().AddLogging().AddSingleton<SignalTraceService>().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        async Task<string> Render(bool show) => await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["ShowMaturitySemantic"] = show,
                ["MaturitySemanticRun"] = run
            });
            return (await renderer.RenderComponentAsync<DiagnosticsConsole>(parameters)).ToHtmlString();
        });
        var maturity = await Render(true);
        Assert.Contains("Maturity pattern detection", maturity);
        Assert.Contains("DefinitionLoading", maturity);
        Assert.Contains("EvaluationError", maturity);
        Assert.DoesNotContain("Outcome: NoMatch", maturity);
        Assert.DoesNotContain("Maturity pattern detection", await Render(false));
    }

    private static MaturitySemanticRunService CreateRunService(HttpClient http) =>
        new(new MaturitySemanticRuleRepository(http),
            new MaturitySemanticEvaluator(new MaturitySemanticEvidenceProjector(new MaturityEvidenceService(http))),
            new MaturityNarrativeComposer(http),
            NullLogger<MaturitySemanticRunService>.Instance);
}
