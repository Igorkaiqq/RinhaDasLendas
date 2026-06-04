using FluentAssertions;
using RinhaDasLendas.Application;
using RinhaDasLendas.Domain;
using RinhaDasLendas.Infrastructure;

namespace RinhaDasLendas.Tests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void InitialLayerAssemblies_ShouldBeLoadable()
    {
        var assemblyNames = new[]
        {
            typeof(ApplicationAssemblyReference).Assembly.GetName().Name,
            typeof(DomainAssemblyReference).Assembly.GetName().Name,
            typeof(InfrastructureAssemblyReference).Assembly.GetName().Name
        };

        assemblyNames.Should().OnlyContain(name => name is not null);
    }
}

