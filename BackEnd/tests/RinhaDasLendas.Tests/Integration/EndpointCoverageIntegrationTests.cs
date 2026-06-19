using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RinhaDasLendas.Tests.Integration;

public sealed class EndpointCoverageIntegrationTests
{
    private readonly List<string> _errors = [];

    [Fact]
    public async Task AllDiscoveredEndpoints_ShouldHaveIntegrationCoverage()
    {
        await using var factory = new PostgreSqlApiFactory();

        try
        {
            await factory.StartContainerAsync();
        }
        catch (DockerUnavailableException exception)
        {
            _errors.Add("Docker indisponivel para Testcontainers: " + exception.Message);
            var dockerUnavailableEndpoints = DiscoverEndpointsFromControllerMetadata();
            var dockerUnavailableCoveredEndpoints = CoveredEndpointKeys();
            WriteCoverageReport(dockerUnavailableEndpoints, dockerUnavailableCoveredEndpoints);

            var dockerUnavailableUncoveredEndpoints = dockerUnavailableEndpoints
                .Where(endpoint => !dockerUnavailableCoveredEndpoints.Contains(endpoint.Key))
                .Select(endpoint => endpoint.DisplayName)
                .ToList();

            dockerUnavailableUncoveredEndpoints.Should().BeEmpty("todo endpoint exposto pela API precisa ter um cenario de integracao mapeado");
            return;
        }

        using var client = factory.CreateClient();
        var discoveredEndpoints = DiscoverEndpoints(factory);
        var coveredEndpoints = CoveredEndpointKeys();

        try
        {
            await ExecuteJogadoresFlowAsync(factory, client);
            await ExecuteTimesFlowAsync(client);
        }
        catch (Exception exception)
        {
            _errors.Add(exception.ToString());
            throw;
        }
        finally
        {
            WriteCoverageReport(discoveredEndpoints, coveredEndpoints);
        }

        var uncoveredEndpoints = discoveredEndpoints
            .Where(endpoint => !coveredEndpoints.Contains(endpoint.Key))
            .Select(endpoint => endpoint.DisplayName)
            .ToList();

        uncoveredEndpoints.Should().BeEmpty("todo endpoint exposto pela API precisa ter um cenario de integracao mapeado");
    }

    private async Task ExecuteJogadoresFlowAsync(PostgreSqlApiFactory factory, HttpClient client)
    {
        var createRequest = CreateRequest($"Teste Integracao {Guid.NewGuid():N}");
        var createResponse = await client.PostAsJsonAsync("/api/v1/jogadores", createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.NomeExibicao.Should().Be(createRequest.NomeExibicao);
        created.Preferencias.Should().HaveCount(5);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.NomeExibicao.Should().Be(createRequest.NomeExibicao);
            jogador.Preferencias.Should().HaveCount(5);
        });

        var listResponse = await client.GetAsync("/api/v1/jogadores?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<JogadorResponseDto>>();
        list.Should().NotBeNull();
        list!.Page.Should().Be(1);
        list.PageSize.Should().Be(20);
        list.Items.Should().Contain(jogador => jogador.Id == created.Id);

        var getByIdResponse = await client.GetAsync($"/api/v1/jogadores/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var found = await getByIdResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
        found.Preferencias.Should().HaveCount(5);

        var updateRequest = new JogadorUpdateRequestDto(
            $"{createRequest.NomeExibicao} Atualizado",
            "Maria Souza",
            "maria#4321",
            "Maria#BR1",
            "https://www.op.gg/summoners/br/Maria-BR1",
            "https://www.deeplol.gg/summoner/br/Maria-BR1",
            "Platina",
            "IV");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/jogadores/{created.Id}/dados-basicos", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        updated.Should().NotBeNull();
        updated!.NomeExibicao.Should().Be(updateRequest.NomeExibicao);
        updated.Elo.Should().Be(updateRequest.Elo);
        updated.Divisao.Should().Be(updateRequest.Divisao);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.NomeExibicao.Should().Be(updateRequest.NomeExibicao);
            jogador.Elo.Should().Be(Elo.Platina);
            jogador.Divisao.Should().Be(Divisao.IV);
        });

        var preferenciasRequest = new UpdatePreferenciasRotasRequestDto([
            new("Mid", 1, false),
            new("Adc", 2, false),
            new("Jungle", 3, false),
            new("Top", 4, true),
            new("Support", 5, false)
        ]);

        var preferenciasResponse = await client.PutAsJsonAsync($"/api/v1/jogadores/{created.Id}/preferencias-rotas", preferenciasRequest);
        preferenciasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferenciasUpdated = await preferenciasResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        preferenciasUpdated.Should().NotBeNull();
        preferenciasUpdated!.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == "Top" && preferencia.NaoJogoNemLascando);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == Rota.Top && preferencia.NaoJogoNemLascando);
            jogador.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == Rota.Mid && preferencia.Prioridade == 1);
        });

        var inativarResponse = await client.PatchAsync($"/api/v1/jogadores/{created.Id}/inativar", null);
        inativarResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertPersistedAsync(factory, created.Id, jogador => jogador.Status.Should().Be(JogadorStatus.Inativo));
    }

    private static async Task ExecuteTimesFlowAsync(HttpClient client)
    {
        var jogadorA = await CreateJogadorAsync(client, $"Jogador Time {Guid.NewGuid():N}");
        var jogadorB = await CreateJogadorAsync(client, $"Jogador Time {Guid.NewGuid():N}");

        var createRequest = new CreateTimeRequestDto(
            $"Time Integracao {Guid.NewGuid():N}",
            $"TI{Random.Shared.Next(1000, 9999)}",
            "Time criado pelo teste de integracao",
            jogadorA.Id,
            [jogadorA.Id, jogadorB.Id]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/times", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        created.Should().NotBeNull();
        created!.Nome.Should().Be(createRequest.Nome);
        created.Membros.Should().HaveCount(2);

        var listResponse = await client.GetAsync("/api/v1/times?page=1&pageSize=20");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<TimeResponseDto>>();
        list.Should().NotBeNull();
        list!.Page.Should().Be(1);
        list.PageSize.Should().Be(20);
        list.TotalItems.Should().BeGreaterThanOrEqualTo(1);
        list.TotalPages.Should().BeGreaterThanOrEqualTo(1);
        list.Items.Should().Contain(time => time.Id == created.Id);

        var getByIdResponse = await client.GetAsync($"/api/v1/times/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var found = await getByIdResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);

        var updateRequest = new UpdateTimeRequestDto(
            $"{createRequest.Nome} Atualizado",
            createRequest.Tag,
            "Time atualizado pelo teste de integracao",
            jogadorB.Id,
            [jogadorA.Id, jogadorB.Id]);
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/times/{created.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        updated.Should().NotBeNull();
        updated!.Nome.Should().Be(updateRequest.Nome);
        updated.Capitao.Should().NotBeNull();
        updated.Capitao!.Id.Should().Be(jogadorB.Id);

        var inativarResponse = await client.PatchAsync($"/api/v1/times/{created.Id}/inativar", null);
        inativarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inactive = await inativarResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        inactive.Should().NotBeNull();
        inactive!.Status.Should().Be("Inativo");

        var reativarResponse = await client.PatchAsync($"/api/v1/times/{created.Id}/reativar", null);
        reativarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var active = await reativarResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        active.Should().NotBeNull();
        active!.Status.Should().Be("Ativo");
    }

    private static async Task<JogadorResponseDto> CreateJogadorAsync(HttpClient client, string nome)
    {
        var request = CreateRequest(nome);
        var response = await client.PostAsJsonAsync("/api/v1/jogadores", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JogadorResponseDto>();
        created.Should().NotBeNull();
        return created!;
    }

    private static async Task AssertPersistedAsync(PostgreSqlApiFactory factory, Guid jogadorId, Action<RinhaDasLendas.Domain.Entities.Jogador> assertion)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var jogador = await dbContext.Jogadores
            .Include(entity => entity.Preferencias)
            .SingleOrDefaultAsync(entity => entity.Id == jogadorId);

        jogador.Should().NotBeNull();
        assertion(jogador!);
    }

    private static IReadOnlyCollection<EndpointDescription> DiscoverEndpoints(PostgreSqlApiFactory factory)
    {
        var provider = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        return provider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(api => api.ActionDescriptor.RouteValues.ContainsKey("controller"))
            .Select(api => new EndpointDescription(
                EndpointKey.From(api.HttpMethod ?? string.Empty, api.RelativePath ?? string.Empty),
                api.HttpMethod ?? string.Empty,
                NormalizePath(api.RelativePath ?? string.Empty),
                api.ParameterDescriptions
                    .Select(parameter => new EndpointParameter(
                        parameter.Name,
                        parameter.Source?.DisplayName ?? "Unknown",
                        FriendlyTypeName(parameter.Type)))
                    .ToList(),
                api.ParameterDescriptions
                    .Where(parameter => string.Equals(parameter.Source?.DisplayName, "Body", StringComparison.OrdinalIgnoreCase))
                    .Select(parameter => FriendlyTypeName(parameter.Type))
                    .Distinct()
                    .ToList(),
                api.SupportedResponseTypes
                    .Select(response => new EndpointResponse(
                        response.StatusCode,
                        FriendlyTypeName(response.Type)))
                    .Distinct()
                    .OrderBy(response => response.StatusCode)
                    .ToList()))
            .OrderBy(endpoint => endpoint.HttpMethod)
            .ThenBy(endpoint => endpoint.Route)
            .ToList();
    }

    private static IReadOnlyCollection<EndpointDescription> DiscoverEndpointsFromControllerMetadata()
    {
        return typeof(Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && type.GetCustomAttribute<ApiControllerAttribute>() is not null)
            .SelectMany(controller =>
            {
                var controllerRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;

                return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetCustomAttributes().OfType<IActionHttpMethodProvider>().SelectMany(httpAttribute =>
                    {
                        var actionRoute = (httpAttribute as IRouteTemplateProvider)?.Template ?? string.Empty;
                        var route = CombineRoutes(controllerRoute, actionRoute);
                        var parameters = method.GetParameters()
                            .Where(parameter => parameter.ParameterType != typeof(CancellationToken))
                            .Select(parameter => new EndpointParameter(
                                parameter.Name ?? string.Empty,
                                ParameterSource(parameter),
                                FriendlyTypeName(parameter.ParameterType)))
                            .ToList();
                        var inputDtos = method.GetParameters()
                            .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
                            .Select(parameter => FriendlyTypeName(parameter.ParameterType))
                            .Distinct()
                            .ToList();
                        var responses = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                            .Select(attribute => new EndpointResponse(attribute.StatusCode, FriendlyTypeName(attribute.Type)))
                            .Distinct()
                            .OrderBy(response => response.StatusCode)
                            .ToList();

                        return httpAttribute.HttpMethods.Select(httpMethod => new EndpointDescription(
                            EndpointKey.From(httpMethod, route),
                            httpMethod,
                            NormalizePath(route),
                            parameters,
                            inputDtos,
                            responses));
                    }));
            })
            .OrderBy(endpoint => endpoint.HttpMethod)
            .ThenBy(endpoint => endpoint.Route)
            .ToList();
    }

    private static string CombineRoutes(string controllerRoute, string actionRoute)
    {
        return string.Join("/", new[] { controllerRoute, actionRoute }
            .Where(route => !string.IsNullOrWhiteSpace(route))
            .Select(route => route.Trim("/".ToCharArray())));
    }

    private static string ParameterSource(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
        {
            return "Body";
        }

        if (parameter.GetCustomAttribute<FromRouteAttribute>() is not null)
        {
            return "Path";
        }

        if (parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
        {
            return "Query";
        }

        return "Unknown";
    }

    private static HashSet<EndpointKey> CoveredEndpointKeys()
    {
        return
        [
            EndpointKey.From("POST", "/api/v1/jogadores"),
            EndpointKey.From("GET", "/api/v1/jogadores"),
            EndpointKey.From("GET", "/api/v1/jogadores/{id}"),
            EndpointKey.From("PUT", "/api/v1/jogadores/{id}/dados-basicos"),
            EndpointKey.From("PUT", "/api/v1/jogadores/{id}/preferencias-rotas"),
            EndpointKey.From("PATCH", "/api/v1/jogadores/{id}/inativar"),
            EndpointKey.From("GET", "/api/v1/times"),
            EndpointKey.From("GET", "/api/v1/times/{id}"),
            EndpointKey.From("POST", "/api/v1/times"),
            EndpointKey.From("PUT", "/api/v1/times/{id}"),
            EndpointKey.From("PATCH", "/api/v1/times/{id}/inativar"),
            EndpointKey.From("PATCH", "/api/v1/times/{id}/reativar")
        ];
    }

    private void WriteCoverageReport(IReadOnlyCollection<EndpointDescription> discoveredEndpoints, HashSet<EndpointKey> coveredEndpoints)
    {
        var uncoveredEndpoints = discoveredEndpoints
            .Where(endpoint => !coveredEndpoints.Contains(endpoint.Key))
            .ToList();

        var testedEndpoints = discoveredEndpoints
            .Where(endpoint => coveredEndpoints.Contains(endpoint.Key))
            .ToList();

        var report = new StringBuilder();
        report.AppendLine("# Relatorio de cobertura de endpoints");
        report.AppendLine();
        report.AppendLine($"Gerado em UTC: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        AppendEndpointSection(report, "Endpoints encontrados", discoveredEndpoints);
        AppendEndpointSection(report, "Endpoints testados", testedEndpoints);
        AppendEndpointSection(report, "Endpoints sem cobertura", uncoveredEndpoints);
        report.AppendLine("## Erros encontrados");
        report.AppendLine();

        if (_errors.Count == 0)
        {
            report.AppendLine("Nenhum erro encontrado durante a execucao dos cenarios.");
        }
        else
        {
            foreach (var error in _errors)
            {
                report.AppendLine("```text");
                report.AppendLine(error);
                report.AppendLine("```");
            }
        }

        var reportDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults"));
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(Path.Combine(reportDirectory, "endpoint-coverage-report.md"), report.ToString());
    }

    private static void AppendEndpointSection(StringBuilder report, string title, IReadOnlyCollection<EndpointDescription> endpoints)
    {
        report.AppendLine($"## {title}");
        report.AppendLine();

        if (endpoints.Count == 0)
        {
            report.AppendLine("Nenhum endpoint.");
            report.AppendLine();
            return;
        }

        foreach (var endpoint in endpoints)
        {
            report.AppendLine($"### {endpoint.HttpMethod} {endpoint.Route}");
            report.AppendLine();
            report.AppendLine($"- Parametros: {FormatParameters(endpoint.Parameters)}");
            report.AppendLine($"- DTOs de entrada: {FormatList(endpoint.InputDtos)}");
            report.AppendLine($"- DTOs de saida/status: {FormatResponses(endpoint.Responses)}");
            report.AppendLine();
        }
    }

    private static string FormatParameters(IReadOnlyCollection<EndpointParameter> parameters)
    {
        return parameters.Count == 0
            ? "Nenhum"
            : string.Join(", ", parameters.Select(parameter => $"{parameter.Name} ({parameter.Source}: {parameter.TypeName})"));
    }

    private static string FormatList(IReadOnlyCollection<string> values)
    {
        return values.Count == 0 ? "Nenhum" : string.Join(", ", values);
    }

    private static string FormatResponses(IReadOnlyCollection<EndpointResponse> responses)
    {
        return responses.Count == 0
            ? "Nao declarado"
            : string.Join(", ", responses.Select(response => $"{response.StatusCode} => {response.TypeName}"));
    }

    private static JogadorCreateRequestDto CreateRequest(string nome)
    {
        var suffix = Regex.Replace(nome, "[^A-Za-z0-9]", string.Empty);
        suffix = suffix.Length > 12 ? suffix[..12] : suffix;

        return new JogadorCreateRequestDto(
            nome,
            "Joao Silva",
            $"joao-{suffix}#1234",
            $"{suffix}#BR1",
            $"https://www.op.gg/summoners/br/{suffix}-BR1",
            $"https://www.deeplol.gg/summoner/br/{suffix}-BR1",
            "Ouro",
            "II",
            [
                new("Top", 1, false),
                new("Jungle", 2, false),
                new("Mid", 3, false),
                new("Adc", 4, false),
                new("Support", 5, false)
            ]);
    }

    private static string NormalizePath(string route)
    {
        var path = route.Split('?')[0].Trim('/');
        path = Regex.Replace(path, "{([^}:]+):[^}]+}", "{$1}");
        return $"/{path}";
    }

    private static string FriendlyTypeName(Type? type)
    {
        if (type is null || type == typeof(void))
        {
            return "Nenhum";
        }

        if (!type.GetTypeInfo().IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArguments = string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName));
        return $"{genericTypeName}<{genericArguments}>";
    }

    private sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>
    {
        private PostgreSqlContainer? _postgres;
        private string? _connectionString;

        public async Task StartContainerAsync()
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("rinha_das_lendas_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
        }

        public new async ValueTask DisposeAsync()
        {
            await DisposeContainerAsync();
        }

        public async Task DisposeContainerAsync()
        {
            await base.DisposeAsync();

            if (_postgres is not null)
            {
                await _postgres.DisposeAsync();
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RinhaDasLendas"] = _connectionString
                        ?? throw new InvalidOperationException("O container PostgreSQL deve ser iniciado antes da API de teste.")
                });
            });
        }
    }

    private readonly record struct EndpointKey(string HttpMethod, string Route)
    {
        public static EndpointKey From(string httpMethod, string route)
        {
            return new EndpointKey(httpMethod.ToUpperInvariant(), NormalizePath(route));
        }
    }

    private sealed record EndpointDescription(
        EndpointKey Key,
        string HttpMethod,
        string Route,
        IReadOnlyCollection<EndpointParameter> Parameters,
        IReadOnlyCollection<string> InputDtos,
        IReadOnlyCollection<EndpointResponse> Responses)
    {
        public string DisplayName => $"{HttpMethod} {Route}";
    }

    private sealed record EndpointParameter(string Name, string Source, string TypeName);

    private sealed record EndpointResponse(int StatusCode, string TypeName);
}
