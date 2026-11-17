using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;
using OperationalIntelligenceHub.Services;
using OperationalIntelligenceHub.Services.Patterns;

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
            ["ScrumMaster.Value"] = ("SQSSS", "SQSSS"),
            ["Leader.Value"] = ("SSCCC", "QQSSS"),
            ["Leader.Ownership"] = ("SSQCC", "QQSSS"),
            ["Leader.Trust"] = ("SSQCC", "QQSSS"),
            ["ScrumMaster.Ownership"] = ("SQCCC", "QQNNQ"),
            ["ScrumMaster.Trust"] = ("SQCCC", "QQNQQ")
        };
        foreach (var (coordinate, profilesByDirection) in profiles)
        foreach (var direction in new[] { "DirectionA", "DirectionB" })
        for (var level = 1; level <= 5; level++)
        {
            var letter = (direction == "DirectionA" ? profilesByDirection.A : profilesByDirection.B)[level - 1];
            var expected = letter switch { 'S' => "Supporting", 'Q' => "Qualifying", 'C' => "Challenging", _ => "Neutral" };
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
        Assert.All(result.ContextRelationships, x => Assert.Equal("Skipped", x.Status));
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
            Assert.Equal("1.0-draft", result.Definition.DefinitionVersion);
            Assert.Equal(MaturitySemanticRuleRepository.DefaultSource, result.Definition.Source);
            Assert.Equal("maturity-semantic-v1-draft", result.Definition.Contract);
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
    public async Task MixedSupportingAndChallengingAndContext_DoNotChangeCoreOutcome()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1),
            ("Leader", "Ownership", 4), ("Leader", "Trust", 1));
        var result = await evaluator.EvaluateAsync(session, definition);
        Assert.Equal(SemanticOutcome.Candidate, result.Outcome);
        Assert.Contains(result.Contributions, x => x.Coordinate == "Leader.Ownership" && x.Classification == "Challenging");
        Assert.Contains(result.Contributions, x => x.Coordinate == "Leader.Trust" && x.Classification == "Supporting");
        var relationship = result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust");
        Assert.Equal("True", relationship.Status);
        Assert.Equal("ContextOnly", relationship.Effect);
        Assert.Equal(2, relationship.Condition!.EvidenceIds.Count);
        Assert.Equal("NotAssessed", result.AssessmentStates.Confidence);
        Assert.Equal("NotAssessed", result.AssessmentStates.Priority);
        Assert.Contains(result.Trace, x => x.Stage == "Candidate" && x.Result == "Created");
    }

    [Fact]
    public async Task NeutralAndMissingEvidence_RemainDistinct()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 1), ("Squad", "Value", 3),
            ("ScrumMaster", "Ownership", 3)), definition);
        Assert.Equal("Neutral", result.Contributions.Single(x => x.Coordinate == "ScrumMaster.Ownership").Classification);
        var missing = result.Contributions.Single(x => x.Coordinate == "Leader.Trust");
        Assert.Equal("Missing", missing.Status);
        Assert.Null(missing.Classification);
        Assert.Equal("Unknown", result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust").Status);
    }

    [Fact]
    public async Task ContextualRelationship_EvaluatesFalseAndTracesEveryOperand()
    {
        var (http, _) = CreateHttp();
        var (definition, evaluator) = await CreateEngine(http);
        var result = await evaluator.EvaluateAsync(Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1),
            ("Leader", "Ownership", 1), ("Leader", "Trust", 1)), definition);
        var condition = result.ContextRelationships.Single(x => x.RelationshipId == "DelegationWithDirectiveTrust").Condition!;
        Assert.Equal(SemanticTruth.False, condition.Result);
        Assert.Equal(2, condition.Operands.Count);
        Assert.Equal(SemanticTruth.True, condition.Operands[1].Result);
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
        Assert.Equal("True", matching.ContextRelationships.Single(x => x.RelationshipId == id).Status);
        Assert.Equal("Unknown", missing.ContextRelationships.Single(x => x.RelationshipId == id).Status);
        Assert.Equal("False", nonmatching.ContextRelationships.Single(x => x.RelationshipId == id).Status);
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
        root["rules"]![0]!["semanticDefinition"]!["supportingEvidenceProfiles"]![0]!["contributions"]!["DirectionA"]!["1"] = "Qualifying";
        handler.Overrides[path] = root.ToJsonString();
        var revised = await new MaturitySemanticRuleRepository(http).LoadAsync("BF01");
        var session = Session(("ProductOwner", "Value", 3), ("Squad", "Value", 1), ("ScrumMaster", "Value", 1));
        var before = await evaluator.EvaluateAsync(session, original);
        var after = await evaluator.EvaluateAsync(session, revised);
        Assert.Equal(before.Definition.DefinitionVersion, after.Definition.DefinitionVersion);
        Assert.NotEqual(before.Definition.DefinitionFingerprint, after.Definition.DefinitionFingerprint);
        Assert.Equal("Supporting", before.Contributions.Single(x => x.Coordinate == "ScrumMaster.Value").Classification);
        Assert.Equal("Qualifying", after.Contributions.Single(x => x.Coordinate == "ScrumMaster.Value").Classification);
    }
}
