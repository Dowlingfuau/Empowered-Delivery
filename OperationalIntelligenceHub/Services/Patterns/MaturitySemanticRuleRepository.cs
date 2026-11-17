using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public sealed class MaturitySemanticRuleRepository
{
    public const string DefaultSource = "data/rules/maturity_pattern_rules_BF01_revised.json";
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;

    public MaturitySemanticRuleRepository(HttpClient http) => _http = http;

    public async Task<SemanticPatternDefinition> LoadAsync(string patternId, string source = DefaultSource)
    {
        var json = await _http.GetStringAsync(source);
        SemanticRuleSet set;
        try
        {
            using var document = JsonDocument.Parse(json);
            CheckDuplicateProperties(document.RootElement, "$");
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("rules", out var rulesElement) ||
                rulesElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException($"Invalid semantic definition JSON at '{source}': rules must be an array.");
            set = JsonSerializer.Deserialize<SemanticRuleSet>(json, Options)
                ?? throw new InvalidOperationException($"Definition source '{source}' is empty.");
            var rawRules = rulesElement.EnumerateArray()
                .Where(x => x.TryGetProperty("patternId", out var id) && id.GetString() == patternId).ToList();
            if (rawRules.Count == 1)
            {
                var raw = rawRules[0];
                CheckNoNulls(raw.GetProperty("evaluation"), "evaluation");
                var semantic = raw.GetProperty("semanticDefinition");
                if (semantic.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException($"Invalid semantic definition JSON at '{source}': semanticDefinition must be an object.");
                foreach (var name in new[] { "operationalDimensions", "coreRelationship", "catalogueEvidence", "supportingEvidenceProfiles", "qualificationRules", "contextRelationships", "executionPolicy" })
                    CheckNoNulls(semantic.GetProperty(name), name);
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Invalid semantic definition JSON at '{source}': {exception.Message}", exception);
        }
        catch (KeyNotFoundException exception)
        {
            throw new InvalidOperationException($"Invalid semantic definition JSON at '{source}': missing required field.", exception);
        }
        if (set.Rules.Any(x => string.IsNullOrWhiteSpace(x.RuleId)) ||
            set.Rules.Select(x => x.RuleId).Distinct(StringComparer.Ordinal).Count() != set.Rules.Count)
            throw new InvalidOperationException($"Invalid semantic definition source '{source}': missing or duplicate rule IDs.");
        var matches = set.Rules.Where(x => x.PatternId == patternId).ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException($"Expected one definition for '{patternId}' in '{source}', found {matches.Count}.");
        var definition = matches[0];
        definition.Source = source;
        await ValidateAsync(definition);
        definition.ValidatedFingerprint = Fingerprint(definition);
        return definition;
    }

    internal static string Fingerprint(SemanticPatternDefinition definition) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition))));

    private static void CheckDuplicateProperties(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidOperationException($"Invalid semantic definition JSON: duplicate property '{path}.{property.Name}'.");
                CheckDuplicateProperties(property.Value, $"{path}.{property.Name}");
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) CheckDuplicateProperties(child, $"{path}[]");
    }

    private static void CheckNoNulls(JsonElement element, string path)
    {
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw new InvalidOperationException($"Invalid semantic definition JSON: '{path}' cannot be null.");
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) CheckNoNulls(property.Value, $"{path}.{property.Name}");
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) CheckNoNulls(child, $"{path}[]");
    }

    private async Task ValidateAsync(SemanticPatternDefinition rule)
    {
        void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"Invalid semantic definition {rule.PatternId}: {message}");
        }
        Require(rule.Evaluation.Contract == "maturity-semantic-v2", $"Unsupported contract '{rule.Evaluation.Contract}'.");
        Require(!string.IsNullOrWhiteSpace(rule.PatternId) && !string.IsNullOrWhiteSpace(rule.PatternName) &&
            !string.IsNullOrWhiteSpace(rule.RuleId) && !string.IsNullOrWhiteSpace(rule.SemanticDefinition.DefinitionVersion), "Missing identity or version.");
        var definition = rule.SemanticDefinition;
        Require(definition.OperationalDimensions.Count > 0 &&
            definition.OperationalDimensions.All(x => x is "Clarity" or "Performance" or "Leadership" or "Culture" or "Foundation") &&
            definition.OperationalDimensions.Distinct(StringComparer.Ordinal).Count() == definition.OperationalDimensions.Count,
            "Missing, duplicate or unrecognised operational dimensions.");
        var catalogue = definition.CatalogueEvidence;
        Require(catalogue.CompatibleCatalogueVersions.Count > 0 &&
            catalogue.CompatibleCatalogueVersions.Distinct().Count() == catalogue.CompatibleCatalogueVersions.Count, "Missing or duplicate compatible versions.");
        var all = catalogue.Core.Concat(catalogue.Supporting).ToList();
        Require(all.Count > 0 && all.Select(x => x.Coordinate).Distinct(StringComparer.Ordinal).Count() == all.Count,
            "Missing or duplicate coordinates.");
        Require(definition.CoreRelationship.RequiredCoordinates.Count > 0 &&
            definition.CoreRelationship.RequiredCoordinates.Distinct().Count() == definition.CoreRelationship.RequiredCoordinates.Count &&
            definition.CoreRelationship.RequiredCoordinates.ToHashSet().SetEquals(catalogue.Core.Select(x => x.Coordinate)),
            "Core coordinates must match catalogue evidence.");
        foreach (var coordinate in all)
        {
            Require(Regex.IsMatch(coordinate.Coordinate, @"^[A-Za-z]+\.[A-Za-z]+$"), $"Invalid coordinate '{coordinate.Coordinate}'.");
            Require(coordinate.Behaviours.Count == 5 && coordinate.Behaviours.Select(x => x.Level).Order().SequenceEqual(Enumerable.Range(1, 5)) &&
                coordinate.Behaviours.Select(x => x.BehaviourId).Distinct().Count() == 5,
                $"Coordinate {coordinate.Coordinate} must have one distinct behaviour for each level.");
        }
        Require(all.SelectMany(x => x.Behaviours).Select(x => x.BehaviourId).Distinct().Count() == all.Count * 5,
            "Duplicate behaviour IDs.");

        foreach (var version in catalogue.CompatibleCatalogueVersions)
        {
            Require(Regex.IsMatch(version, @"\A[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z"), $"Invalid version '{version}'.");
            var root = $"data/maturity/versions/{version}";
            var mapping = JsonSerializer.Deserialize<List<SignalMapping>>(await _http.GetStringAsync($"{root}/maturity.json"), Options)
                ?? throw new InvalidOperationException($"Catalogue mapping '{version}' is empty.");
            var entries = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BehaviourOption>>>>(
                await _http.GetStringAsync($"{root}/maturity_behaviours.json"), Options)
                ?? throw new InvalidOperationException($"Catalogue '{version}' is empty.");
            foreach (var coordinate in all)
            {
                var parts = coordinate.Coordinate.Split('.');
                Require(entries.TryGetValue(parts[0], out var themes) && themes.TryGetValue(parts[1], out _),
                    $"Coordinate {coordinate.Coordinate} is absent from {version}.");
                var options = entries[parts[0]][parts[1]];
                foreach (var behaviour in coordinate.Behaviours)
                {
                    var mappings = mapping.Where(x => x.Lens == parts[0] && x.Theme == parts[1] && x.Level == behaviour.Level).ToList();
                    var optionsAtLevel = options.Where(x => x.Level == behaviour.Level).ToList();
                    Require(mappings.Count == 1 && mappings[0].BehaviourId == behaviour.BehaviourId &&
                        optionsAtLevel.Count == 1 && optionsAtLevel[0].Headline == behaviour.Headline,
                        $"Behaviour {behaviour.BehaviourId} does not match retained catalogue and mapping {version}.");
                }
            }
        }

        var core = definition.CoreRelationship;
        Require(core.GroupDefinitions.Select(x => x.GroupId).Distinct().Count() == core.GroupDefinitions.Count &&
            core.GroupDefinitions.All(x => !string.IsNullOrWhiteSpace(x.GroupId) &&
                core.RequiredCoordinates.Contains(x.Coordinate) && x.Levels.Count > 0 &&
                x.Levels.Distinct().Count() == x.Levels.Count && x.Levels.All(n => n is >= 1 and <= 5)),
            "Invalid, duplicate or ambiguous groups.");
        var groups = core.GroupDefinitions.ToDictionary(x => x.GroupId, StringComparer.Ordinal);
        var coordinates = all.ToDictionary(x => x.Coordinate, StringComparer.Ordinal);
        Require(core.Branches.Count > 0 && core.Branches.Select(x => x.BranchId).Distinct().Count() == core.Branches.Count &&
            core.Branches.All(x => !string.IsNullOrWhiteSpace(x.BranchId)), "Missing or duplicate branches.");
        foreach (var branch in core.Branches)
        {
            ValidateExpression(branch.When, groups, coordinates, Require);
            var possible = PossibleAssignments(branch.When, groups, coordinates);
            Require(possible.Count > 0 && possible.All(x => x.Keys.All(core.RequiredCoordinates.Contains)),
                $"Branch {branch.BranchId} is impossible or refers to a non-core coordinate.");
            Require(branch.Result is "PotentialValueTranslationGap" or "DoesNotTriggerBF01" or "ChallengesBF01",
                $"Unknown branch result '{branch.Result}'.");
            Require(branch.Result == "PotentialValueTranslationGap"
                ? branch.Direction is "DirectionA" or "DirectionB" : branch.Direction is null,
                $"Invalid direction for branch {branch.BranchId}.");
        }
        Require(core.RequiredCoordinates.Count == 2, "This contract requires exactly two core coordinates.");
        var coverage = core.Branches.ToDictionary(x => x.BranchId, _ => 0);
        foreach (var a in catalogue.Core[0].Behaviours)
        foreach (var b in catalogue.Core[1].Behaviours)
        {
            var test = new Dictionary<string, SemanticSelection>
            {
                [catalogue.Core[0].Coordinate] = TestSelection(catalogue.Core[0].Coordinate, a),
                [catalogue.Core[1].Coordinate] = TestSelection(catalogue.Core[1].Coordinate, b)
            };
            var matched = core.Branches.Where(x => SemanticConditionEvaluator.Evaluate(x.When, x.BranchId, test, groups).Result == SemanticTruth.True).ToList();
            Require(matched.Count == 1, $"Core branches overlap or do not cover {a.BehaviourId} / {b.BehaviourId}.");
            coverage[matched[0].BranchId]++;
        }
        Require(coverage.Values.All(x => x > 0), "A core branch is impossible.");

        Require(definition.SupportingEvidenceProfiles.Select(x => x.Coordinate).Distinct().Count() == definition.SupportingEvidenceProfiles.Count &&
            definition.SupportingEvidenceProfiles.Select(x => x.Coordinate).ToHashSet().SetEquals(catalogue.Supporting.Select(x => x.Coordinate)),
            "Supporting profiles do not match supporting coordinates.");
        foreach (var profile in definition.SupportingEvidenceProfiles)
        {
            Require(profile.Contributions.Keys.ToHashSet().SetEquals(new[] { "DirectionA", "DirectionB" }), $"Missing directions for {profile.Coordinate}.");
            foreach (var pair in profile.Contributions)
                Require(pair.Value.Keys.ToHashSet().SetEquals(new[] { "1", "2", "3", "4", "5" }) &&
                    pair.Value.Values.All(x => x is "Supporting" or "Challenging" or "Qualifying" or "Contextual" or "Neutral"),
                    $"Invalid levels or classifications for {profile.Coordinate}/{pair.Key}.");
            foreach (var (direction, levels) in profile.PotentialRelevance)
                Require(profile.Contributions.ContainsKey(direction) && levels.All(x =>
                    profile.Contributions[direction].TryGetValue(x.Key, out var classification) &&
                    classification == "Contextual" && !string.IsNullOrWhiteSpace(x.Value)),
                    $"Invalid potential relevance for {profile.Coordinate}/{direction}.");
        }
        Require(definition.QualificationRules.Select(x => x.RuleId).Distinct().Count() == definition.QualificationRules.Count,
            "Duplicate qualification rule IDs.");
        foreach (var qualification in definition.QualificationRules)
        {
            Require(!string.IsNullOrWhiteSpace(qualification.RuleId) && coordinates.ContainsKey(qualification.Coordinate) &&
                catalogue.Supporting.Any(x => x.Coordinate == qualification.Coordinate) &&
                qualification.BehaviourIds.Count > 0 && qualification.BehaviourIds.Distinct().Count() == qualification.BehaviourIds.Count &&
                qualification.BehaviourIds.All(id => coordinates[qualification.Coordinate].Behaviours.Any(x => x.BehaviourId == id)) &&
                qualification.Directions.Count > 0 && qualification.Directions.Distinct().Count() == qualification.Directions.Count &&
                qualification.Directions.All(x => x is "DirectionA" or "DirectionB") &&
                !string.IsNullOrWhiteSpace(qualification.InterpretiveEffect) && !string.IsNullOrWhiteSpace(qualification.NarrativeKey) &&
                !string.IsNullOrWhiteSpace(qualification.Boundary), $"Invalid qualification rule '{qualification.RuleId}'.");
        }
        foreach (var profile in definition.SupportingEvidenceProfiles)
        foreach (var (direction, levels) in profile.Contributions)
        foreach (var (level, classification) in levels)
        {
            var behaviour = coordinates[profile.Coordinate].Behaviours.Single(x => x.Level.ToString() == level);
            var matchingRules = definition.QualificationRules.Count(x => x.Coordinate == profile.Coordinate &&
                x.Directions.Contains(direction) && x.BehaviourIds.Contains(behaviour.BehaviourId));
            Require(matchingRules == (classification == "Qualifying" ? 1 : 0),
                $"Qualification rules do not match {profile.Coordinate}/{direction}/{level}.");
        }
        Require(definition.ContextRelationships.Select(x => x.RelationshipId).Distinct().Count() == definition.ContextRelationships.Count &&
            definition.ContextRelationships.All(x => !string.IsNullOrWhiteSpace(x.RelationshipId) &&
                x.Effect == "ContextOnly" && x.Status == "PendingApproval"),
            "Invalid or duplicate contextual relationships.");
        foreach (var relation in definition.ContextRelationships)
        {
            ValidateExpression(relation.When, groups, coordinates, Require);
            Require(PossibleAssignments(relation.When, groups, coordinates).Count > 0,
                $"Context relationship {relation.RelationshipId} is impossible.");
        }
        var expectedPolicy = new Dictionary<string, string>
        {
            ["evidenceSource"] = "SelectedBehaviours", ["contextEvaluation"] = "PendingApproval",
            ["contextEffect"] = "NotApplied", ["missingCore"] = "Unknown",
            ["unavailableCore"] = "Unavailable", ["nonMatchingCore"] = "NoMatch", ["matchedCore"] = "Candidate",
            ["neutralPresentation"] = "TraceOnly", ["strength"] = "NotAssessed", ["reliability"] = "NotAssessed",
            ["confidence"] = "NotAssessed", ["priority"] = "NotAssessed"
        };
        Require(expectedPolicy.All(x => definition.ExecutionPolicy.TryGetValue(x.Key, out var value) && value == x.Value),
            "Missing or unsupported execution policy.");
    }

    private static List<Dictionary<string, HashSet<int>>> PossibleAssignments(JsonElement expression,
        IReadOnlyDictionary<string, SemanticGroup> groups,
        IReadOnlyDictionary<string, SemanticCatalogueCoordinate> coordinates)
    {
        var op = expression.EnumerateObject().Single();
        if (op.Name == "hasGroup")
        {
            var group = groups[op.Value.GetString()!];
            return new() { new() { [group.Coordinate] = group.Levels.ToHashSet() } };
        }
        if (op.Name == "hasBehaviour")
        {
            var coordinate = op.Value.GetProperty("coordinate").GetString()!;
            var ids = op.Value.GetProperty("behaviourIds").EnumerateArray().Select(x => x.GetString()).ToHashSet();
            return new() { new() { [coordinate] = coordinates[coordinate].Behaviours
                .Where(x => ids.Contains(x.BehaviourId)).Select(x => x.Level).ToHashSet() } };
        }
        if (op.Name == "any")
            return op.Value.EnumerateArray().SelectMany(x => PossibleAssignments(x, groups, coordinates)).ToList();
        var combinations = new List<Dictionary<string, HashSet<int>>> { new() };
        foreach (var child in op.Value.EnumerateArray())
        {
            var next = new List<Dictionary<string, HashSet<int>>>();
            foreach (var left in combinations)
            foreach (var right in PossibleAssignments(child, groups, coordinates))
            {
                var merged = left.ToDictionary(x => x.Key, x => x.Value.ToHashSet());
                foreach (var (coordinate, levels) in right)
                {
                    if (merged.TryGetValue(coordinate, out var existing)) existing.IntersectWith(levels);
                    else merged.Add(coordinate, levels.ToHashSet());
                }
                if (merged.Values.All(x => x.Count > 0)) next.Add(merged);
            }
            combinations = next;
        }
        return combinations;
    }

    private static void ValidateExpression(JsonElement expression, IReadOnlyDictionary<string, SemanticGroup> groups,
        IReadOnlyDictionary<string, SemanticCatalogueCoordinate> coordinates, Action<bool, string> require)
    {
        require(expression.ValueKind == JsonValueKind.Object, "Expression must be an object.");
        var properties = expression.EnumerateObject().ToList();
        require(properties.Count == 1, "Expression must contain exactly one operator.");
        var op = properties[0];
        if (op.Name is "all" or "any")
        {
            require(op.Value.ValueKind == JsonValueKind.Array && op.Value.GetArrayLength() > 0, $"{op.Name} requires operands.");
            foreach (var child in op.Value.EnumerateArray()) ValidateExpression(child, groups, coordinates, require);
        }
        else if (op.Name == "hasGroup")
            require(op.Value.ValueKind == JsonValueKind.String && groups.ContainsKey(op.Value.GetString() ?? ""), "Unknown group.");
        else if (op.Name == "hasBehaviour")
        {
            require(op.Value.ValueKind == JsonValueKind.Object && op.Value.EnumerateObject().Count() == 2 &&
                op.Value.TryGetProperty("coordinate", out _) && op.Value.TryGetProperty("behaviourIds", out _), "Malformed hasBehaviour.");
            var coordinate = op.Value.GetProperty("coordinate");
            var ids = op.Value.GetProperty("behaviourIds");
            require(coordinate.ValueKind == JsonValueKind.String && coordinates.ContainsKey(coordinate.GetString() ?? "") &&
                ids.ValueKind == JsonValueKind.Array && ids.GetArrayLength() > 0, "Invalid hasBehaviour coordinate or IDs.");
            var known = coordinates[coordinate.GetString()!].Behaviours.Select(x => x.BehaviourId).ToHashSet();
            var selected = ids.EnumerateArray().ToList();
            require(selected.All(x => x.ValueKind == JsonValueKind.String && known.Contains(x.GetString()!)) &&
                selected.Select(x => x.GetString()).Distinct().Count() == selected.Count,
                $"Unknown, duplicate or wrong-coordinate behaviour in {coordinate.GetString()}.");
        }
        else require(false, $"Unsupported operator '{op.Name}'.");
    }

    private static SemanticSelection TestSelection(string coordinate, SemanticCatalogueBehaviour behaviour) =>
        new(coordinate, "validation", coordinate, coordinate.Split('.')[0], coordinate.Split('.')[1], behaviour.Level,
            new MaturityBehaviourEvidence { BehaviourId = behaviour.BehaviourId }, SemanticEvidenceStatus.Valid, "validation");
}
