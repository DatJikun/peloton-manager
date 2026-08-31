using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Peloton.Application;
using Peloton.Content;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class RaceContentTests
{
    private const string ScenarioId = "race-scenario.peloton.prototype-v0";

    [Fact]
    public void ValidFixtureResolvesAndBatchMatchesCanonicalStep()
    {
        JsonRacePrototypeCatalog catalog = new(ContentRoot());
        RaceScenario scenario = catalog.Resolve(ScenarioId);
        PrototypeRaceEngine engine = new();

        RaceResult batch = engine.RunBatch(scenario, 811);
        RaceSession session = engine.CreateSession(scenario, 811);
        while (!session.IsCompleted)
        {
            RaceStepResult step = session.Step();
            if (step.Status == RaceStepStatus.DecisionRequired)
            {
                Peloton.Domain.RaceDecisionRequest request = session.PendingDecision!;
                session.ResolveDecision(new Peloton.Domain.RaceDecisionResolution(
                    request.Id,
                    request.AuthorityId,
                    request.DelegatedDefaultOption));
            }
        }

        Assert.Equal("race-tuning.peloton.prototype-v0", scenario.TuningIdentity);
        Assert.Equal(3, scenario.Definition.Segments.Count);
        Assert.True(scenario.Riders.Count >= 12);
        Assert.Contains(scenario.TacticalPlans, plan => plan.Briefing.Kind == RaceBriefingKind.Chase);
        Assert.Contains(scenario.TacticalPlans, plan => plan.Briefing.Kind == RaceBriefingKind.Protect);
        Assert.Equal(batch.Checksum, session.Result!.Checksum);
        Assert.Equal(batch.FinishOrder, session.Result.FinishOrder);
    }

    [Fact]
    public void ExistingSkeletonScenarioStillResolves()
    {
        WorldRecipe recipe = new JsonScenarioCatalog(ContentRoot()).Resolve("scenario.peloton.skeleton");

        Assert.Equal("scenario.peloton.skeleton", recipe.ContentIdentity.ScenarioId);
        Assert.Equal(3, recipe.Organizations.Count);
        Assert.Equal(12, recipe.Riders.Count);
        Assert.Equal("Adam Wroński", recipe.Manager.Name);
        Assert.DoesNotContain(recipe.Riders, rider => rider.Id == recipe.Manager.Id);
        Assert.All(recipe.Organizations, organization =>
            Assert.Equal(4, recipe.Riders.Count(rider => rider.OrganizationId == organization.Id)));
    }

    [Theory]
    [InlineData("criticalPowerW")]
    [InlineData("wPrimeCapacityJ")]
    [InlineData("bodyMassKg")]
    [InlineData("cdAM2")]
    [InlineData("baseCrr")]
    public void OutOfRangeRiderValueIsRejectedWithStableIssueCode(string field)
    {
        using TemporaryDirectory temp = CopyFixture();
        JsonObject payload = ReadPayload(temp.Path);
        FirstRider(payload)[field] = 0;
        WritePayload(temp.Path, payload);

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("VALUE_OUT_OF_RANGE", error.IssueCode);
        Assert.Contains(field, error.JsonPath, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("rider")]
    [InlineData("team")]
    public void DuplicateDefinitionIdIsRejected(string definitionKind)
    {
        using TemporaryDirectory temp = CopyFixture();
        JsonObject payload = ReadPayload(temp.Path);
        JsonArray definitions = definitionKind == "rider"
            ? Scenario(payload)["riders"]!.AsArray()
            : Scenario(payload)["teams"]!.AsArray();
        definitions.Add(definitions[0]!.DeepClone());
        WritePayload(temp.Path, payload);

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("DEFINITION_ID_DUPLICATE", error.IssueCode);
    }

    [Theory]
    [InlineData("riderTeam")]
    [InlineData("startingRider")]
    [InlineData("commandRider")]
    [InlineData("tacticalSupport")]
    public void MissingReferenceIsRejected(string referenceKind)
    {
        using TemporaryDirectory temp = CopyFixture();
        JsonObject payload = ReadPayload(temp.Path);
        JsonObject scenario = Scenario(payload);
        switch (referenceKind)
        {
            case "riderTeam":
                FirstRider(payload)["teamId"] = "team.missing";
                break;
            case "startingRider":
                scenario["startingOrder"]!.AsArray()[0] = "rider.missing";
                break;
            case "commandRider":
                scenario["commands"]!.AsArray()[0]!["riderId"] = "rider.missing";
                break;
            case "tacticalSupport":
                scenario["tacticalPlans"]!.AsArray()[0]!["supportRiderId"] = "rider.missing";
                break;
            default:
                throw new InvalidOperationException("Unknown test mutation.");
        }

        WritePayload(temp.Path, payload);

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("REFERENCE_MISSING", error.IssueCode);
    }

    [Fact]
    public void ResourcePathCannotEscapePackRoot()
    {
        using TemporaryDirectory temp = new();
        string packRoot = Path.Combine(temp.Path, "unsafe-pack");
        Directory.CreateDirectory(packRoot);
        File.WriteAllText(
            Path.Combine(packRoot, "pack.json"),
            """
            {
              "packId": "peloton.unsafe",
              "packVersion": "1.0.0",
              "contentSchemaVersion": 1,
              "resources": [
                { "kind": "racePrototypeScenarios", "path": "../outside.json" }
              ],
              "dependencies": []
            }
            """);
        File.WriteAllText(Path.Combine(temp.Path, "outside.json"), "{ \"definitions\": [] }");

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("PATH_OUTSIDE_PACK", error.IssueCode);
    }

    [Theory]
    [InlineData("script")]
    [InlineData("commandWatts")]
    public void ExecutableOrDirectWattCommandFieldIsRejected(string forbiddenField)
    {
        using TemporaryDirectory temp = CopyFixture();
        JsonObject payload = ReadPayload(temp.Path);
        if (forbiddenField == "script")
        {
            Scenario(payload)["script"] = "run-arbitrary-code";
        }
        else
        {
            Scenario(payload)["commands"]!.AsArray()[0]!["watts"] = 500;
        }

        WritePayload(temp.Path, payload);

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("JSON_INVALID", error.IssueCode);
    }

    [Fact]
    public void DefinitionAndJsonPropertyOrderDoNotChangeRaceIdentity()
    {
        using TemporaryDirectory firstRoot = CopyFixture();
        using TemporaryDirectory reorderedRoot = CopyFixture();
        JsonObject payload = ReadPayload(reorderedRoot.Path);
        JsonObject scenario = Scenario(payload);
        Reverse(scenario["teams"]!.AsArray());
        Reverse(scenario["riders"]!.AsArray());
        Reverse(scenario["commands"]!.AsArray());
        Reverse(scenario["tacticalPlans"]!.AsArray());
        JsonObject reorderedProperties = Assert.IsType<JsonObject>(ReverseProperties(payload));
        WritePayload(reorderedRoot.Path, reorderedProperties);

        RaceScenario first = new JsonRacePrototypeCatalog(firstRoot.Path).Resolve(ScenarioId);
        RaceScenario second = new JsonRacePrototypeCatalog(reorderedRoot.Path).Resolve(ScenarioId);
        PrototypeRaceEngine engine = new();

        Assert.Equal(first.TuningIdentity, second.TuningIdentity);
        Assert.Equal(engine.RunBatch(first, 912).Checksum, engine.RunBatch(second, 912).Checksum);
    }

    [Fact]
    public void InvalidSiblingDefinitionRejectsPackBeforeRaceAllocation()
    {
        using TemporaryDirectory temp = CopyFixture();
        JsonObject payload = ReadPayload(temp.Path);
        JsonObject invalidSibling = Assert.IsType<JsonObject>(Scenario(payload).DeepClone());
        invalidSibling["id"] = "race-scenario.peloton.invalid-sibling";
        invalidSibling["riders"]!.AsArray()[0]!["criticalPowerW"] = 0;
        payload["definitions"]!.AsArray().Add(invalidSibling);
        WritePayload(temp.Path, payload);

        ContentValidationException error = Assert.Throws<ContentValidationException>(
            () => new JsonRacePrototypeCatalog(temp.Path).Resolve(ScenarioId));

        Assert.Equal("VALUE_OUT_OF_RANGE", error.IssueCode);
    }

    private static JsonObject FirstRider(JsonObject payload)
    {
        return Scenario(payload)["riders"]!.AsArray()[0]!.AsObject();
    }

    private static JsonObject Scenario(JsonObject payload)
    {
        return payload["definitions"]!.AsArray()[0]!.AsObject();
    }

    private static TemporaryDirectory CopyFixture()
    {
        TemporaryDirectory temp = new();
        string source = Path.Combine(ContentRoot(), "peloton.race-prototype");
        string destination = Path.Combine(temp.Path, "peloton.race-prototype");
        Directory.CreateDirectory(destination);
        File.Copy(Path.Combine(source, "pack.json"), Path.Combine(destination, "pack.json"));
        File.Copy(
            Path.Combine(source, "race-prototype.json"),
            Path.Combine(destination, "race-prototype.json"));
        return temp;
    }

    private static JsonObject ReadPayload(string contentRoot)
    {
        string path = Path.Combine(contentRoot, "peloton.race-prototype", "race-prototype.json");
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    private static void WritePayload(string contentRoot, JsonObject payload)
    {
        string path = Path.Combine(contentRoot, "peloton.race-prototype", "race-prototype.json");
        File.WriteAllText(path, payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Reverse(JsonArray values)
    {
        JsonNode?[] items = values.Select(item => item!.DeepClone()).Reverse().ToArray();
        values.Clear();
        foreach (JsonNode? item in items)
        {
            values.Add(item);
        }
    }

    private static JsonNode? ReverseProperties(JsonNode? node)
    {
        if (node is JsonObject sourceObject)
        {
            JsonObject target = new();
            foreach ((string key, JsonNode? value) in sourceObject.Reverse())
            {
                target.Add(key, ReverseProperties(value));
            }

            return target;
        }

        if (node is JsonArray sourceArray)
        {
            return new JsonArray(sourceArray.Select(ReverseProperties).ToArray());
        }

        return node?.DeepClone();
    }

    private static string ContentRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PelotonManager.sln")))
            {
                return Path.Combine(current.FullName, "content");
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
