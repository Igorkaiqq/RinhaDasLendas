using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Api.Middleware;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Domain.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RinhaDasLendas.Api.OpenApi;

public sealed class CompetitiveHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = "/" + (context.ApiDescription.RelativePath ?? string.Empty);
        if (!path.StartsWith("/api/v1/temporadas", StringComparison.Ordinal)
            && !path.StartsWith("/api/v1/competicoes", StringComparison.Ordinal))
        {
            return;
        }

        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<RequireIdempotencyAttribute>().Any())
        {
            AddRequestHeader(operation, "Idempotency-Key");
        }

        if (RequiresCalendarPrecondition(path))
        {
            AddRequestHeader(operation, "If-Match-Calendar");
        }
        else if (RequiresResourcePrecondition(context.ApiDescription.HttpMethod, path))
        {
            AddRequestHeader(operation, "If-Match");
        }

        foreach (var response in (operation.Responses ?? []).Where(item => item.Key.StartsWith('2')))
        {
            if (ReturnsETag(context.ApiDescription.HttpMethod, path))
            {
                AddResponseHeader(response.Value, "ETag");
            }

            if (response.Key == "201")
            {
                AddResponseHeader(response.Value, "Location");
            }
        }
    }

    private static void AddRequestHeader(OpenApiOperation operation, string name)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });
    }

    private static void AddResponseHeader(IOpenApiResponse response, string name)
    {
        if (response is not OpenApiResponse concreteResponse)
        {
            return;
        }

        concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
        concreteResponse.Headers[name] = new OpenApiHeader
        {
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        };
    }

    private static bool RequiresCalendarPrecondition(string path) =>
        path.EndsWith("/aberturas", StringComparison.Ordinal)
        || path.EndsWith("/encerramentos", StringComparison.Ordinal);

    private static bool RequiresResourcePrecondition(string? method, string path) =>
        string.Equals(method, "PATCH", StringComparison.Ordinal)
        || string.Equals(method, "POST", StringComparison.Ordinal)
            && (path.EndsWith("/rodadas", StringComparison.Ordinal)
                || path.EndsWith("/ordenacoes-rodadas", StringComparison.Ordinal)
                || path.EndsWith("/regras-publicadas", StringComparison.Ordinal));

    private static bool ReturnsETag(string? method, string path) =>
        path != "/api/v1/competicoes"
        && !(string.Equals(method, "GET", StringComparison.Ordinal)
            && (path.EndsWith("/competicoes", StringComparison.Ordinal)
                || path.EndsWith("/rodadas", StringComparison.Ordinal)));
}

public sealed class AuthorizedOperationSecurityFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];
        if (actionAttributes.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var authorization = actionAttributes.Concat(controllerAttributes).OfType<IAuthorizeData>().ToArray();
        if (!authorization.Any(AllowsBearer))
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null!, null!)] = [],
        });
    }

    private static bool AllowsBearer(IAuthorizeData authorization)
    {
        if (!string.IsNullOrWhiteSpace(authorization.AuthenticationSchemes))
        {
            return authorization.AuthenticationSchemes.Split(',', StringSplitOptions.TrimEntries)
                .Contains(ApiAuthenticationDefaults.SchemeName, StringComparer.Ordinal);
        }

        return !string.Equals(
            authorization.Policy,
            AuthPermissions.CanUseDiscordBotApi,
            StringComparison.Ordinal);
    }
}

public sealed class BearerSecurityReferenceDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var operation in document.Paths.Values
            .SelectMany(path => path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>()))
        {
            if (operation.Security is not { Count: > 0 })
            {
                continue;
            }

            operation.Security.Clear();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, null!)] = [],
            });
        }
    }
}

public sealed class CompetitiveRequestSchemaFilter : ISchemaFilter
{
    private static readonly IReadOnlyDictionary<Type, string[]> RequestTypes =
        new Dictionary<Type, string[]>
        {
            [typeof(CreateSeasonRequestDto)] = ["nome", "ano", "ordemNoAno", "dataInicio", "dataFimExclusiva"],
            [typeof(UpdateSeasonRequestDto)] = [],
            [typeof(CreateCompetitionRequestDto)] = ["nome", "codigo", "circuitoDiario"],
            [typeof(UpdateCompetitionRequestDto)] = [],
            [typeof(CreateRoundRequestDto)] = ["nome", "ordem"],
            [typeof(ReorderRoundsRequestDto)] = ["rodadaIds"],
            [typeof(PublishRulesRequestDto)] = ["formato", "modoDraft"],
        };

    public void Apply(IOpenApiSchema target, SchemaFilterContext context)
    {
        if (target is not OpenApiSchema schema
            || !RequestTypes.TryGetValue(context.Type, out var required))
        {
            return;
        }

        schema.AdditionalPropertiesAllowed = false;
        schema.Required ??= new HashSet<string>();
        schema.Required.Clear();
        foreach (var property in required)
        {
            schema.Required.Add(property);
        }

        schema.MinProperties = context.Type == typeof(UpdateSeasonRequestDto)
            || context.Type == typeof(UpdateCompetitionRequestDto)
            ? 1
            : null;

        foreach (var property in context.Type.GetProperties())
        {
            if (!property.PropertyType.IsGenericType
                || property.PropertyType.GetGenericTypeDefinition() != typeof(Optional<>))
            {
                continue;
            }

            var optionalType = property.PropertyType.GetGenericArguments()[0];
            var wireType = Nullable.GetUnderlyingType(optionalType) ?? optionalType;
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[propertyName] = context.SchemaGenerator.GenerateSchema(
                wireType,
                context.SchemaRepository);
        }

        foreach (var propertySchema in schema.Properties?.Values.OfType<OpenApiSchema>()
            ?? Enumerable.Empty<OpenApiSchema>())
        {
            if (propertySchema.Type.HasValue)
            {
                propertySchema.Type &= ~JsonSchemaType.Null;
            }
        }
    }
}
