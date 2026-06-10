using FluentAssertions;
using RinhaDasLendas.Application;
using RinhaDasLendas.Domain;
using RinhaDasLendas.Domain.Constants;
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

        assemblyNames.Should().OnlyContain(name => name != null);
    }

    [Fact]
    public void DomainConstants_ShouldBeCentralizedInConstantsNamespace()
    {
        typeof(MessageCodes).Namespace.Should().Be("RinhaDasLendas.Domain.Constants");
        typeof(ValidationConstants).Namespace.Should().Be("RinhaDasLendas.Domain.Constants");
        ValidationConstants.RequiredRoutePreferencesCount.Should().Be(5);
        ValidationConstants.MaxRoutePriority.Should().Be(5);
    }
}

