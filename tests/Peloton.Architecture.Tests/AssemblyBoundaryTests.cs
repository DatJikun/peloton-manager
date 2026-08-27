using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Persistence;
using Peloton.Rules;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Architecture.Tests;

public sealed class AssemblyBoundaryTests
{
    private static readonly string[] ForbiddenSpecialTeamTypeNames =
    {
        string.Concat("Player", "Team"),
        string.Concat("Is", "Human", "Team"),
        string.Concat("Human", "Team"),
    };

    private static readonly Assembly[] GameplayAssemblies =
    {
        typeof(DomainAssembly).Assembly,
        typeof(RulesAssembly).Assembly,
        typeof(SimulationAssembly).Assembly,
        typeof(ApplicationAssembly).Assembly,
        typeof(PersistenceAssembly).Assembly,
        typeof(ContentAssembly).Assembly,
        typeof(InfrastructureAssembly).Assembly,
    };

    [Fact]
    public void GameplayAssembliesContainNoSpecialTeamType()
    {
        IEnumerable<string> forbidden = GameplayAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.Name)
            .Where(name => ForbiddenSpecialTeamTypeNames.Contains(name, StringComparer.Ordinal));

        Assert.Empty(forbidden);
    }

    [Fact]
    public void HeadlessAssembliesDoNotReferenceGodot()
    {
        Assembly[] headlessAssemblies =
        {
            typeof(DomainAssembly).Assembly,
            typeof(RulesAssembly).Assembly,
            typeof(SimulationAssembly).Assembly,
            typeof(PersistenceAssembly).Assembly,
        };

        foreach (Assembly assembly in headlessAssemblies)
        {
            Assert.DoesNotContain(
                assembly.GetReferencedAssemblies(),
                reference => reference.Name?.Contains("Godot", StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    [Fact]
    public void GameplayAssembliesContainNoStubRaceEngineType()
    {
        IEnumerable<Type> forbidden = GameplayAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name == "StubRaceEngine");

        Assert.Empty(forbidden);
    }
}
