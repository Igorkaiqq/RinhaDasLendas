using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Events.Competitive;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Architecture;

public sealed class CompetitiveDomainContractsArchitectureTests
{
    [Fact]
    public void CompetitiveDomainEvents_ShouldMatchFundamentalCatalog()
    {
        var eventTypes = typeof(ICompetitiveDomainEvent).Assembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(ICompetitiveDomainEvent).Namespace)
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => typeof(ICompetitiveDomainEvent).IsAssignableFrom(type))
            .ToArray();

        eventTypes.Select(type => type.Name).Should().BeEquivalentTo(
            nameof(TemporadaCriada),
            nameof(TemporadaAtivada),
            nameof(TemporadaEncerrada),
            nameof(CompeticaoCriada),
            nameof(RodadaCriada),
            nameof(RodadasReordenadas),
            nameof(RegrasCompeticaoPublicadas),
            nameof(EventoCriado),
            nameof(SerieCriada),
            nameof(SerieAdicionadaAoEvento),
            nameof(SerieIniciada),
            nameof(PartidaAdicionada),
            nameof(PicksPartidaRegistrados),
            nameof(PartidaConfirmada),
            nameof(PartidaMarcadaComoRemake),
            nameof(ResultadoSerieConfirmado),
            nameof(SerieCancelada),
            nameof(SerieAnulada),
            nameof(FatoCompetitivoCorrigido));
        eventTypes.Should().HaveCount(19);

        eventTypes.Should().OnlyContain(type => type.IsSealed);
        eventTypes.Should().OnlyContain(type => IsRecord(type));
        eventTypes.SelectMany(type => type.GetProperties())
            .Should().NotContain(property => property.Name.Contains("Message", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Payload", StringComparison.OrdinalIgnoreCase));

        var metadataProperties = typeof(ICompetitiveDomainEvent).GetProperties();
        metadataProperties.Select(property => property.Name).Should().BeEquivalentTo(
            "EventoDominioId",
            "AgregadoId",
            "VersaoAgregado",
            "OcorridoEm",
            "CorrelationId",
            "CausationId",
            "AtorUsuarioId");
        metadataProperties.Should().OnlyContain(property => property.SetMethod == null);

        eventTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.SetMethod is not null)
            .Should().OnlyContain(property => property.SetMethod!
                .ReturnParameter
                .GetRequiredCustomModifiers()
                .Contains(typeof(IsExternalInit)));

        typeof(SerieCriada).GetProperty("EventoId")!.PropertyType.Should().Be(typeof(Guid?));
    }

    [Fact]
    public void CompetitiveRepositories_ShouldBeDomainOwnedAsyncInterfaces()
    {
        Type[] repositoryTypes =
        [
            typeof(ICalendarioCompetitivoRepository),
            typeof(ICompeticaoRepository),
            typeof(IEventoCompetitivoRepository),
            typeof(ISerieRepository),
            typeof(ICompetitiveAuditRepository),
            typeof(IIdempotencyRepository)
        ];

        repositoryTypes.Should().OnlyContain(type => type.IsInterface);
        repositoryTypes.Should().OnlyContain(type => type.Namespace == "RinhaDasLendas.Domain.Repositories");
        repositoryTypes.Should().OnlyContain(type => type.GetMethods().Length > 0);
        repositoryTypes.SelectMany(type => type.GetMethods())
            .Should().OnlyContain(method => typeof(Task).IsAssignableFrom(method.ReturnType));
        repositoryTypes.SelectMany(type => type.GetMethods())
            .Should().OnlyContain(method => HasCancellationTokenLast(method));
        repositoryTypes.SelectMany(type => type.GetMethods())
            .Should().NotContain(method => method.Name.Contains("SaveChanges", StringComparison.Ordinal)
                || method.Name.Contains("Commit", StringComparison.Ordinal));

        var unitOfWorkType = typeof(ICalendarioCompetitivoRepository).Assembly
            .GetType("RinhaDasLendas.Domain.Repositories.ICompetitiveUnitOfWork");
        unitOfWorkType.Should().NotBeNull();
        unitOfWorkType!.IsInterface.Should().BeTrue();
        unitOfWorkType.GetMethods().Should().ContainSingle();
        var saveChangesMethod = unitOfWorkType.GetMethods().Single();
        saveChangesMethod.Name.Should().Be("SaveChangesAsync");
        saveChangesMethod.ReturnType.Should().Be(typeof(Task));
        HasCancellationTokenLast(saveChangesMethod).Should().BeTrue();

        typeof(ICalendarioCompetitivoRepository).GetMethod("GetWithSeasonsAsync").Should().NotBeNull();
        typeof(ICalendarioCompetitivoRepository).GetMethod("GetActiveSeasonAsync").Should().NotBeNull();
        typeof(ICalendarioCompetitivoRepository).GetMethod("ExistsOverlappingSeasonAsync").Should().NotBeNull();
        typeof(ICompeticaoRepository).GetMethod("GetWithRoundsAndRulesAsync").Should().NotBeNull();
        typeof(ICompeticaoRepository).GetMethod("ExistsCodeAsync").Should().NotBeNull();
        typeof(IEventoCompetitivoRepository).GetMethod("GetWithTeamsAsync").Should().NotBeNull();
        typeof(IEventoCompetitivoRepository).GetMethods().Select(method => method.Name)
            .Should().BeEquivalentTo("GetWithTeamsAsync", "AddAsync");
        typeof(ISerieRepository).GetMethod("GetAggregateAsync").Should().NotBeNull();
        typeof(ISerieRepository).GetMethod("GetAggregateByPartidaIdAsync").Should().NotBeNull();
        typeof(ISerieRepository).GetMethod("ExistsForDraftAsync").Should().NotBeNull();
        typeof(ICompetitiveAuditRepository).GetMethod("ListByResourceAsync").Should().NotBeNull();
        typeof(IIdempotencyRepository).GetMethod("FindAsync").Should().NotBeNull();
        typeof(IIdempotencyRepository).GetMethod("DeleteExpiredAsync").Should().NotBeNull();

        AssertOptionalSeasonStateFilter(typeof(ICalendarioCompetitivoRepository).GetMethod("ListSeasonsAsync")!);
        AssertOptionalSeasonStateFilter(typeof(ICalendarioCompetitivoRepository).GetMethod("CountSeasonsAsync")!);

        repositoryTypes.Append(unitOfWorkType).SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
            .Should().OnlyContain(type => IsAllowedContractType(type));
    }

    [Fact]
    public void ContractTypeInspection_ShouldRecursivelyValidateContainersArraysAndByRefs()
    {
        IsAllowedContractType(typeof(Task<IReadOnlyCollection<Season>>)).Should().BeTrue();
        IsAllowedContractType(typeof(Task<Season?>)).Should().BeTrue();
        IsAllowedContractType(typeof(Dictionary<string, Season>)).Should().BeTrue();
        IsAllowedContractType(typeof(Guid[])).Should().BeTrue();
        IsAllowedContractType(typeof(Guid).MakeByRefType()).Should().BeTrue();

        IsAllowedContractType(typeof(AndConstraint<Season>)).Should().BeFalse();
        IsAllowedContractType(typeof(Task<IReadOnlyCollection<FluentAssertions.Execution.AssertionScope>>)).Should().BeFalse();
        IsAllowedContractType(typeof(Dictionary<string, FluentAssertions.Execution.AssertionScope>)).Should().BeFalse();
        IsAllowedContractType(typeof(FluentAssertions.Execution.AssertionScope[])).Should().BeFalse();
        IsAllowedContractType(typeof(FluentAssertions.Execution.AssertionScope).MakeByRefType()).Should().BeFalse();
        IsAllowedContractType(typeof(Microsoft.EntityFrameworkCore.DbSet<Season>)).Should().BeFalse();
        IsAllowedContractType(typeof(RinhaDasLendas.Domainish.DomainishContractWrapper<Season>)).Should().BeFalse();
        IsAllowedContractType(typeof(Guid).MakePointerType()).Should().BeFalse();
    }

    [Fact]
    public void CompetitiveHandlers_ShouldNotOwnEntityFrameworkContextsOrTransactions()
    {
        var applicationAssembly = typeof(IIdempotencyService).Assembly;
        var competitiveNamespaces = new[]
        {
            ".Handlers.Seasons",
            ".Handlers.Competicoes",
            ".Handlers.Eventos",
            ".Handlers.Series",
            ".Handlers.Partidas",
        };
        var handlers = applicationAssembly.GetTypes()
            .Where(type => competitiveNamespaces.Any(suffix =>
                type.Namespace?.EndsWith(suffix, StringComparison.Ordinal) == true))
            .ToArray();

        applicationAssembly.GetReferencedAssemblies().Select(reference => reference.Name)
            .Should().NotContain(reference => reference == "Microsoft.EntityFrameworkCore" || reference == "Npgsql",
                "Application handlers must leave DbContext and transaction ownership to Infrastructure");
        handlers.SelectMany(type => type.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Should().NotContain(type =>
                type.Name.Contains("DbContext", StringComparison.Ordinal)
                || type.Name.Contains("DatabaseFacade", StringComparison.Ordinal)
                || type.Name.Contains("DbContextTransaction", StringComparison.Ordinal)
                || type.Name.Contains("DbConnection", StringComparison.Ordinal)
                || type.Name.Contains("DbTransaction", StringComparison.Ordinal));
    }

    private static bool IsAllowedContractType(Type type)
    {
        if (type.IsPointer)
        {
            return false;
        }

        if (type.IsArray || type.IsByRef)
        {
            return IsAllowedContractType(type.GetElementType()!);
        }

        if (!IsDomainOrSystemType(type.IsGenericType ? type.GetGenericTypeDefinition() : type))
        {
            return false;
        }

        return !type.IsGenericType || type.GetGenericArguments().All(IsAllowedContractType);
    }

    private static bool IsDomainOrSystemType(Type type)
    {
        var typeNamespace = type.Namespace;
        return typeNamespace == "System"
            || typeNamespace?.StartsWith("System.", StringComparison.Ordinal) is true
            || typeNamespace == "RinhaDasLendas.Domain"
            || typeNamespace?.StartsWith("RinhaDasLendas.Domain.", StringComparison.Ordinal) is true;
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;

    private static bool HasCancellationTokenLast(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length > 0 && parameters[^1].ParameterType == typeof(CancellationToken);
    }

    private static void AssertOptionalSeasonStateFilter(MethodInfo method)
    {
        method.GetParameters().Should().ContainSingle(parameter =>
            parameter.Name == "estado" && parameter.ParameterType == typeof(SeasonEstado?));
    }

}
