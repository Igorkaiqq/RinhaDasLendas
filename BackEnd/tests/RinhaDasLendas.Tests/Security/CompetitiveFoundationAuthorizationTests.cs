using System.Security.Claims;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Infrastructure.Services;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Security;

public sealed class CompetitiveFoundationAuthorizationTests
{
    private const string SuperAdmin = "SuperAdmin";
    private const string Presidente = "Presidente";
    private const string VicePresidente = "VicePresidente";
    private const string Admin = "Admin";
    private const string Moderador = "Moderador";
    private const string Capitao = "Capitão";
    private const string Jogador = "Jogador";

    private const string CanManageSeasons = "CanManageSeasons";
    private const string CanManageCompetitions = "CanManageCompetitions";
    private const string CanManageMatches = "CanManageMatches";
    private const string CanFinalizeMatches = "CanFinalizeMatches";
    private const string CanViewCompetitiveAudit = "CanViewCompetitiveAudit";

    private static readonly CompetitiveAuthorizationApiFactory ApiFactory = new();

    [Fact]
    public void CompetitiveRolesAndCapabilities_ShouldExposeStableConstants()
    {
        AuthRoles.Presidente.Should().Be(Presidente);
        AuthRoles.VicePresidente.Should().Be(VicePresidente);
        AuthRoles.Levels.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            [SuperAdmin] = 700,
            [Presidente] = 600,
            [VicePresidente] = 500,
            [Admin] = 400,
            [Moderador] = 300,
            [Capitao] = 200,
            [Jogador] = 100,
        });

        AuthPermissions.CanManageSeasons.Should().Be(CanManageSeasons);
        AuthPermissions.CanManageCompetitions.Should().Be(CanManageCompetitions);
        AuthPermissions.CanManageMatches.Should().Be(CanManageMatches);
        AuthPermissions.CanFinalizeMatches.Should().Be(CanFinalizeMatches);
        AuthPermissions.CanViewCompetitiveAudit.Should().Be(CanViewCompetitiveAudit);
        AuthPermissions.CompetitiveCapabilities.Should().Equal(
            CanManageSeasons,
            CanManageCompetitions,
            CanManageMatches,
            CanFinalizeMatches,
            CanViewCompetitiveAudit);
        AuthPermissions.CompetitiveBaseRoleGrants.Should().BeEquivalentTo(
            new Dictionary<string, IReadOnlySet<string>>
            {
                [SuperAdmin] = new HashSet<string> { CanManageSeasons, CanManageMatches, CanFinalizeMatches, CanViewCompetitiveAudit },
                [Presidente] = new HashSet<string> { CanManageSeasons, CanManageCompetitions, CanManageMatches, CanFinalizeMatches, CanViewCompetitiveAudit },
                [VicePresidente] = new HashSet<string>(),
                [Admin] = new HashSet<string> { CanManageCompetitions, CanManageMatches, CanFinalizeMatches, CanViewCompetitiveAudit },
                [Moderador] = new HashSet<string> { CanManageMatches, CanFinalizeMatches },
                [Capitao] = new HashSet<string>(),
                [Jogador] = new HashSet<string>(),
            });
    }

    [Fact]
    public void CompetitiveRoles_ShouldHaveUniqueStableSeedIdsInEfModel()
    {
        var expectedIds = new Dictionary<string, Guid>
        {
            [SuperAdmin] = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            [Admin] = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            [Moderador] = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            [Capitao] = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            [Jogador] = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            [Presidente] = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            [VicePresidente] = Guid.Parse("10000000-0000-0000-0000-000000000007"),
        };
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test")
            .Options;
        using var context = new RinhaDasLendasDbContext(options);

        var roleEntity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ApplicationRole))!;
        var seededIds = roleEntity.GetSeedData()
            .ToDictionary(seed => (string)seed[nameof(ApplicationRole.Name)]!, seed => (Guid)seed[nameof(ApplicationRole.Id)]!);

        seededIds.Should().BeEquivalentTo(expectedIds);
        seededIds.Values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CurrentActor_ShouldReadOnlyAnAuthenticatedPrincipal()
    {
        var actorId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                new Claim(ClaimTypes.Role, Presidente),
            ], "Test")),
        };

        var currentUser = new RinhaDasLendas.Api.Services.CurrentUser(
            new HttpContextAccessor { HttpContext = context });
        var actor = new CurrentActor(currentUser);

        actor.UserId.Should().Be(actorId);
        actor.Roles.Should().Equal(Presidente);

        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, SuperAdmin),
        ]));

        actor.UserId.Should().BeNull();
        actor.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedSuperAdminIdentity_ShouldNotElevateAuthenticatedPlayer()
    {
        var principal = new ClaimsPrincipal([
            new ClaimsIdentity([
                new Claim(ClaimTypes.Role, Jogador),
            ], "Test"),
            new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, SuperAdmin),
            ]),
        ]);
        var currentUser = CreateCurrentUser(principal);
        var service = new CompetitiveAuthorizationService(new CurrentActor(currentUser));
        var seasonAction = CreateContext(CanManageSeasons, "CapabilityCheck", "Global");

        currentUser.UserId.Should().BeNull();
        currentUser.Roles.Should().Equal(Jogador);
        (await service.AuthorizeAsync(seasonAction, CancellationToken.None)).Should().BeFalse();
        (await service.GetAllowedActionsAsync([seasonAction], CancellationToken.None)).Should().BeEmpty();

        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var policyResult = await authorization.AuthorizeAsync(
            principal,
            seasonAction,
            CanManageSeasons);
        policyResult.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleAuthenticatedIdentities_ShouldPreserveRoleUnion()
    {
        var principal = new ClaimsPrincipal([
            new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, SuperAdmin),
            ], "Primary"),
            new ClaimsIdentity([
                new Claim(ClaimTypes.Role, Admin),
            ], "Secondary"),
        ]);
        var currentUser = CreateCurrentUser(principal);
        var service = new CompetitiveAuthorizationService(new CurrentActor(currentUser));
        var competitionAction = CreateContext(
            CanManageCompetitions,
            "ConfigurarCompeticao",
            "Competicao",
            seasonState: "Ativa");

        currentUser.Roles.Should().BeEquivalentTo(SuperAdmin, Admin);
        (await service.AuthorizeAsync(competitionAction, CancellationToken.None)).Should().BeTrue();

        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var policyResult = await authorization.AuthorizeAsync(
            principal,
            competitionAction,
            CanManageCompetitions);
        policyResult.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnauthenticatedMalformedAndRolelessPrincipals_ShouldFailEveryCompetitivePolicy()
    {
        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principals = new[]
        {
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, SuperAdmin),
            ])),
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
                new Claim(ClaimTypes.Role, SuperAdmin),
            ], "Test")),
            CreatePrincipal("UnknownRole"),
            CreatePrincipal(),
        };

        foreach (var principal in principals)
        {
            foreach (var capability in AuthPermissions.CompetitiveCapabilities)
            {
                var result = await authorization.AuthorizeAsync(
                    principal,
                    CreateContext(capability, "CapabilityCheck", "Global"),
                    capability);
                result.Succeeded.Should().BeFalse();
            }
        }
    }

    [Fact]
    public async Task GetAllowedActions_ShouldUseOnlyTheInjectedActorRoles()
    {
        var actor = new StubCurrentActor(Jogador);
        var service = new CompetitiveAuthorizationService(actor);
        var actions = new[]
        {
            CreateContext(CanManageMatches, "CorrecaoTecnica", "Partida", hasRequiredJustification: true),
            CreateContext(CanManageSeasons, "CapabilityCheck", "Global"),
        };

        var allowed = await service.GetAllowedActionsAsync(actions, CancellationToken.None);

        allowed.Should().BeEmpty();
    }

    [Theory]
    [InlineData(SuperAdmin, Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Ativa")]
    [InlineData(SuperAdmin, Moderador, CanManageMatches, "OperacaoNormal", "Serie", "Amistoso", null)]
    [InlineData(Presidente, VicePresidente, CanManageCompetitions, "CapabilityCheck", "Global", null, null)]
    public async Task MultipleRoles_ShouldUseUnionSemantics(
        string firstRole,
        string secondRole,
        string capability,
        string operation,
        string resourceType,
        string? serieType,
        string? seasonState)
    {
        var context = CreateContext(capability, operation, resourceType, serieType, seasonState);
        var service = new CompetitiveAuthorizationService(new StubCurrentActor(firstRole, secondRole));

        (await service.AuthorizeAsync(context, CancellationToken.None)).Should().BeTrue();

        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var policyResult = await authorization.AuthorizeAsync(
            CreatePrincipal(firstRole, secondRole),
            context,
            capability);
        policyResult.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void CompetitiveInfrastructure_ShouldShareOneScopedUnitOfWorkAndDbContext()
    {
        using var scope = ApiFactory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<RinhaDasLendasDbContext>();
        var concreteUnitOfWork = services.GetRequiredService<CompetitiveUnitOfWork>();
        var calendarRepository = services.GetRequiredService<ICalendarioCompetitivoRepository>();
        var competitionRepository = services.GetRequiredService<ICompeticaoRepository>();
        var seriesRepository = services.GetRequiredService<ISerieRepository>();
        var auditRepository = services.GetRequiredService<ICompetitiveAuditRepository>();
        var idempotencyRepository = services.GetRequiredService<IIdempotencyRepository>();
        var idempotencyService = services.GetRequiredService<IIdempotencyService>();

        services.GetRequiredService<ICompetitiveUnitOfWork>().Should().BeSameAs(concreteUnitOfWork);
        services.GetRequiredService<ICalendarioCompetitivoRepository>().Should().BeSameAs(calendarRepository);
        services.GetRequiredService<ICompeticaoRepository>().Should().BeSameAs(competitionRepository);
        services.GetRequiredService<ISerieRepository>().Should().BeSameAs(seriesRepository);
        services.GetRequiredService<ICompetitiveAuditRepository>().Should().BeSameAs(auditRepository);
        services.GetRequiredService<IIdempotencyRepository>().Should().BeSameAs(idempotencyRepository);
        idempotencyService.Should().BeOfType<IdempotencyService>();
        calendarRepository.Should().BeOfType<CalendarioCompetitivoRepository>();
        competitionRepository.Should().BeOfType<CompeticaoRepository>();
        seriesRepository.Should().BeOfType<SerieRepository>();
        auditRepository.Should().BeOfType<CompetitiveAuditRepository>();
        idempotencyRepository.Should().BeOfType<IdempotencyRepository>();
        services.GetRequiredService<ICurrentActor>().Should().BeOfType<CurrentActor>();

        ResolveDbContext(concreteUnitOfWork).Should().BeSameAs(dbContext);
        ResolveDbContext(calendarRepository).Should().BeSameAs(dbContext);
        ResolveDbContext(competitionRepository).Should().BeSameAs(dbContext);
        ResolveDbContext(seriesRepository).Should().BeSameAs(dbContext);
        ResolveDbContext(auditRepository).Should().BeSameAs(dbContext);
        ResolveDbContext(idempotencyRepository).Should().BeSameAs(dbContext);
        ResolveDbContext(idempotencyService).Should().BeSameAs(dbContext);
        ResolveDependency<CompetitiveUnitOfWork>(idempotencyService).Should().BeSameAs(concreteUnitOfWork);
        ResolveDependency<IIdempotencyRepository>(idempotencyService).Should().BeSameAs(idempotencyRepository);

        using var secondScope = ApiFactory.Services.CreateScope();
        var secondServices = secondScope.ServiceProvider;
        secondServices.GetRequiredService<RinhaDasLendasDbContext>().Should().NotBeSameAs(dbContext);
        secondServices.GetRequiredService<CompetitiveUnitOfWork>().Should().NotBeSameAs(concreteUnitOfWork);
        secondServices.GetRequiredService<ICalendarioCompetitivoRepository>().Should().NotBeSameAs(calendarRepository);
        secondServices.GetRequiredService<ICompeticaoRepository>().Should().NotBeSameAs(competitionRepository);
        secondServices.GetRequiredService<ISerieRepository>().Should().NotBeSameAs(seriesRepository);
        secondServices.GetRequiredService<ICompetitiveAuditRepository>().Should().NotBeSameAs(auditRepository);
        secondServices.GetRequiredService<IIdempotencyRepository>().Should().NotBeSameAs(idempotencyRepository);
    }

    [Theory]
    [MemberData(nameof(BaseCapabilityMatrix))]
    public async Task BaseCapabilityMatrix_ShouldMatchExplicitGovernance(
        string role,
        string capability,
        bool expected)
    {
        var context = CreateContext(capability, "CapabilityCheck", "Global");
        var service = new CompetitiveAuthorizationService(new StubCurrentActor(role));
        var actual = await service.AuthorizeAsync(context, CancellationToken.None);

        actual.Should().Be(expected, $"{role} must have an explicit decision for {capability}");
    }

    [Theory]
    [MemberData(nameof(BaseCapabilityMatrix))]
    public async Task BaseCapabilityPolicies_ShouldMatchExplicitPrincipals(
        string role,
        string capability,
        bool expected)
    {
        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(
            CreatePrincipal(role),
            CreateContext(capability, "CapabilityCheck", "Global"),
            capability);

        result.Succeeded.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(ResourceConditionMatrix))]
    public async Task ResourceConditions_ShouldBeEnforcedByBackend(
        string role,
        string capability,
        string operation,
        string resourceType,
        string? serieType,
        string? seasonState,
        bool hasStartedSeries,
        bool hasRequiredJustification,
        bool expected)
    {
        var context = CreateContext(
            capability,
            operation,
            resourceType,
            serieType,
            seasonState,
            hasStartedSeries,
            hasRequiredJustification);
        var service = new CompetitiveAuthorizationService(new StubCurrentActor(role));
        var actual = await service.AuthorizeAsync(context, CancellationToken.None);

        actual.Should().Be(expected,
            $"{role}/{capability} must enforce {resourceType}/{serieType}/{seasonState}/{operation}, " +
            $"hasStartedSeries={hasStartedSeries} and hasRequiredJustification={hasRequiredJustification}");
    }

    [Theory]
    [MemberData(nameof(ResourceConditionMatrix))]
    public async Task ResourceConditionPolicies_ShouldEvaluateThePassedPrincipal(
        string role,
        string capability,
        string operation,
        string resourceType,
        string? serieType,
        string? seasonState,
        bool hasStartedSeries,
        bool hasRequiredJustification,
        bool expected)
    {
        using var scope = ApiFactory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var context = CreateContext(
            capability,
            operation,
            resourceType,
            serieType,
            seasonState,
            hasStartedSeries,
            hasRequiredJustification);

        var result = await authorization.AuthorizeAsync(CreatePrincipal(role), context, capability);

        result.Succeeded.Should().Be(expected);
    }

    public static TheoryData<string, string, bool> BaseCapabilityMatrix => new()
    {
        { SuperAdmin, CanManageSeasons, true },
        { Presidente, CanManageSeasons, true },
        { VicePresidente, CanManageSeasons, false },
        { Admin, CanManageSeasons, false },
        { Moderador, CanManageSeasons, false },
        { Capitao, CanManageSeasons, false },
        { Jogador, CanManageSeasons, false },

        { SuperAdmin, CanManageCompetitions, false },
        { Presidente, CanManageCompetitions, true },
        { VicePresidente, CanManageCompetitions, false },
        { Admin, CanManageCompetitions, true },
        { Moderador, CanManageCompetitions, false },
        { Capitao, CanManageCompetitions, false },
        { Jogador, CanManageCompetitions, false },

        { SuperAdmin, CanManageMatches, true },
        { Presidente, CanManageMatches, true },
        { VicePresidente, CanManageMatches, false },
        { Admin, CanManageMatches, true },
        { Moderador, CanManageMatches, true },
        { Capitao, CanManageMatches, false },
        { Jogador, CanManageMatches, false },

        { SuperAdmin, CanFinalizeMatches, true },
        { Presidente, CanFinalizeMatches, true },
        { VicePresidente, CanFinalizeMatches, false },
        { Admin, CanFinalizeMatches, true },
        { Moderador, CanFinalizeMatches, true },
        { Capitao, CanFinalizeMatches, false },
        { Jogador, CanFinalizeMatches, false },

        { SuperAdmin, CanViewCompetitiveAudit, true },
        { Presidente, CanViewCompetitiveAudit, true },
        { VicePresidente, CanViewCompetitiveAudit, false },
        { Admin, CanViewCompetitiveAudit, true },
        { Moderador, CanViewCompetitiveAudit, false },
        { Capitao, CanViewCompetitiveAudit, false },
        { Jogador, CanViewCompetitiveAudit, false },
    };

    public static TheoryData<string, string, string, string, string?, string?, bool, bool, bool> ResourceConditionMatrix => new()
    {
        { SuperAdmin, CanManageMatches, "OperacaoNormal", "Partida", null, null, false, false, false },
        { SuperAdmin, CanManageMatches, "CorrecaoTecnica", "Partida", null, null, false, true, true },
        { SuperAdmin, CanManageMatches, "CorrecaoTecnica", "Partida", null, null, false, false, false },
        { SuperAdmin, CanManageMatches, "AnulacaoTecnica", "Partida", null, null, false, true, true },
        { SuperAdmin, CanManageMatches, "AnulacaoTecnica", "Partida", null, null, false, false, false },
        { SuperAdmin, CanFinalizeMatches, "FinalizacaoNormal", "Partida", null, null, false, false, false },
        { SuperAdmin, CanFinalizeMatches, "CorrecaoTecnica", "Partida", null, null, false, true, true },
        { SuperAdmin, CanFinalizeMatches, "CorrecaoTecnica", "Partida", null, null, false, false, false },
        { SuperAdmin, CanFinalizeMatches, "AnulacaoTecnica", "Partida", null, null, false, true, true },
        { SuperAdmin, CanFinalizeMatches, "AnulacaoTecnica", "Partida", null, null, false, false, false },

        { Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Planejada", false, false, true },
        { Admin, CanManageCompetitions, "ConfigurarRodada", "Rodada", null, "Planejada", false, false, true },
        { Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Ativa", false, false, true },
        { Admin, CanManageCompetitions, "ConfigurarRodada", "Rodada", null, "Ativa", false, false, true },
        { Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Encerrada", false, false, false },
        { Admin, CanManageCompetitions, "ConfigurarRodada", "Rodada", null, "Encerrada", false, false, false },
        { Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Planejada", true, false, false },
        { Admin, CanManageCompetitions, "ConfigurarRodada", "Rodada", null, "Planejada", true, false, false },
        { Admin, CanManageCompetitions, "ConfigurarCompeticao", "Competicao", null, "Ativa", true, false, false },
        { Admin, CanManageCompetitions, "ConfigurarRodada", "Rodada", null, "Ativa", true, false, false },
        { Admin, CanManageCompetitions, "Publicar", "Regra", null, null, false, false, false },
        { Admin, CanManageCompetitions, "CriarOuAlterar", "Evento", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Serie", null, null, false, false, true },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Partida", null, null, false, false, true },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Calendario", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Season", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Competicao", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Rodada", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Regra", null, null, false, false, false },
        { Admin, CanViewCompetitiveAudit, "Consultar", "Evento", null, null, false, false, false },

        { Moderador, CanManageMatches, "OperacaoNormal", "Serie", "DiariaTemporaria", null, false, false, true },
        { Moderador, CanManageMatches, "OperacaoNormal", "Serie", "Amistoso", null, false, false, true },
        { Moderador, CanManageMatches, "OperacaoNormal", "Serie", "ConfrontoOficial", null, false, false, false },
        { Moderador, CanManageMatches, "OperacaoNormal", "Evento", null, null, false, false, false },
        { Moderador, CanManageMatches, "CorrecaoTecnica", "Partida", null, null, false, true, false },
        { Moderador, CanManageMatches, "AnulacaoTecnica", "Partida", null, null, false, true, false },
        { Moderador, CanFinalizeMatches, "FinalizacaoNormal", "Serie", "DiariaTemporaria", null, false, false, true },
        { Moderador, CanFinalizeMatches, "FinalizacaoNormal", "Serie", "Amistoso", null, false, false, true },
        { Moderador, CanFinalizeMatches, "FinalizacaoNormal", "Serie", "ConfrontoOficial", null, false, false, false },
        { Moderador, CanFinalizeMatches, "FinalizacaoNormal", "Evento", null, null, false, false, false },
        { Moderador, CanFinalizeMatches, "CorrecaoTecnica", "Partida", null, null, false, true, false },
        { Moderador, CanFinalizeMatches, "AnulacaoTecnica", "Partida", null, null, false, true, false },
    };

    private static CompetitiveAuthorizationContext CreateContext(
        string capability,
        string operation,
        string resourceType,
        string? serieType = null,
        string? seasonState = null,
        bool hasStartedSeries = false,
        bool hasRequiredJustification = false) =>
        new(
            capability,
            operation,
            resourceType,
            serieType,
            seasonState,
            hasStartedSeries,
            hasRequiredJustification);

    private static ClaimsPrincipal CreatePrincipal(params string[] roles) =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }
                .Concat(roles.Select(role => new Claim(ClaimTypes.Role, role))),
            "Test"));

    private static RinhaDasLendas.Api.Services.CurrentUser CreateCurrentUser(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext { User = principal };
        return new RinhaDasLendas.Api.Services.CurrentUser(
            new HttpContextAccessor { HttpContext = context });
    }

    private sealed class StubCurrentActor(params string[] roles) : ICurrentActor
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class CompetitiveAuthorizationApiFactory : SecurityApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Testing");
        }
    }

    private static RinhaDasLendasDbContext ResolveDbContext(object service) =>
        ResolveDependency<RinhaDasLendasDbContext>(service);

    private static T ResolveDependency<T>(object service) where T : class =>
        service.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => typeof(T).IsAssignableFrom(field.FieldType))
            .Select(field => field.GetValue(service))
            .OfType<T>()
            .Single();
}
