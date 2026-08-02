using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Api.Middleware;

public sealed class IdempotencyMiddleware(
    IIdempotencyService service,
    ICurrentUser currentUser,
    IOptions<IdempotencyMiddlewareOptions> options) : IMiddleware
{
    private const string KeyHeader = "Idempotency-Key";
    private const string ReplayedHeader = "Idempotency-Replayed";
    private readonly IdempotencyMiddlewareOptions _options = ValidateOptions(options.Value);

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requirement = context.GetEndpoint()?.Metadata.GetMetadata<RequireIdempotencyAttribute>();
        if (requirement is null)
        {
            await next(context);
            return;
        }

        if (currentUser.UserId is not Guid actorId || actorId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var key = context.Request.Headers[KeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(key)
            || key.Length is < 8 or > 100
            || key.Any(char.IsControl))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (context.GetEndpoint() is not RouteEndpoint
            || !context.Request.Path.HasValue)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var body = await ReadBoundedRequestAsync(context.Request, context.RequestAborted);
        var request = new IdempotencyRequest(
            actorId,
            context.Request.Method.ToUpperInvariant(),
            NormalizeResourcePath(context.Request.PathBase.Add(context.Request.Path).Value!),
            key,
            ComputeCanonicalHash(body, context.Request.ContentType));
        var initialResponse = ResponseSnapshot.Capture(context.Response);
        var realBody = context.Response.Body;

        IdempotencyExecutionResult execution;
        try
        {
            execution = await service.ExecuteAsync(
                request,
                cancellationToken => CaptureAsync(
                    context,
                    next,
                    requirement.ResourceType,
                    cancellationToken),
                context.RequestAborted);
        }
        catch
        {
            context.Response.Body = realBody;
            initialResponse.Restore(context.Response);
            throw;
        }

        context.Response.Body = realBody;
        if (execution.Replayed)
        {
            ApplyStoredMetadata(context.Response, execution.Result);
            context.Response.Headers[ReplayedHeader] = "true";
        }

        await realBody.WriteAsync(execution.Result.Content, context.RequestAborted);
    }

    private static string NormalizeResourcePath(string path) =>
        (path.Length > 1 ? path.TrimEnd('/') : path).ToLowerInvariant();

    private async Task<IdempotencyResult> CaptureAsync(
        HttpContext context,
        RequestDelegate next,
        RecursoCompetitivoTipo resourceType,
        CancellationToken cancellationToken)
    {
        var previousBody = context.Response.Body;
        await using var captured = new BoundedWriteStream(_options.MaxResponseBodyBytes);
        context.Response.Body = captured;
        try
        {
            await next(context);
            if (context.Response.ContentLength > _options.MaxResponseBodyBytes)
            {
                throw new DomainException(MessageCodes.ValidationError);
            }

            var content = captured.ToArray();
            var metadata = new Dictionary<MetadadoResultadoOperacao, object?>();
            AddIfPresent(metadata, MetadadoResultadoOperacao.TipoConteudo, context.Response.ContentType);
            AddIfPresent(metadata, MetadadoResultadoOperacao.Localizacao, context.Response.Headers.Location.ToString());
            AddIfPresent(metadata, MetadadoResultadoOperacao.VersaoRecurso, context.Response.Headers.ETag.ToString());
            return new IdempotencyResult(
                context.Response.StatusCode,
                resourceType,
                ReadResourceId(content),
                content,
                metadata);
        }
        finally
        {
            context.Response.Body = previousBody;
        }
    }

    private async Task<byte[]> ReadBoundedRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > _options.MaxRequestBodyBytes)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        request.EnableBuffering();
        await using var buffer = new BoundedWriteStream(_options.MaxRequestBodyBytes);
        try
        {
            await request.Body.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static string ComputeCanonicalHash(byte[] body, string? contentType)
    {
        if (body.Length == 0)
        {
            return Convert.ToHexStringLower(SHA512.HashData(body));
        }

        if (contentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Convert.ToHexStringLower(SHA512.HashData(body));
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteCanonical(writer, document.RootElement);
            }
            return Convert.ToHexStringLower(SHA512.HashData(stream.ToArray()));
        }
        catch (JsonException)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static Guid? ReadResourceId(byte[] content)
    {
        if (content.Length == 0)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("id", out var id)
                && id.TryGetGuid(out var resourceId)
                    ? resourceId
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyStoredMetadata(HttpResponse response, IdempotencyResult stored)
    {
        response.StatusCode = stored.StatusCode;
        response.ContentType = ReadMetadata(stored, MetadadoResultadoOperacao.TipoConteudo);
        response.Headers.Remove("Location");
        response.Headers.Remove("ETag");
        if (ReadMetadata(stored, MetadadoResultadoOperacao.Localizacao) is { } location)
        {
            response.Headers.Location = location;
        }
        if (ReadMetadata(stored, MetadadoResultadoOperacao.VersaoRecurso) is { } etag)
        {
            response.Headers.ETag = etag;
        }
    }

    private static string? ReadMetadata(IdempotencyResult result, MetadadoResultadoOperacao key) =>
        result.Metadata.TryGetValue(key, out var value) ? value as string : null;

    private static void AddIfPresent(
        IDictionary<MetadadoResultadoOperacao, object?> metadata,
        MetadadoResultadoOperacao key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata.Add(key, value);
        }
    }

    private static IdempotencyMiddlewareOptions ValidateOptions(IdempotencyMiddlewareOptions value)
    {
        if (value.MaxRequestBodyBytes <= 0
            || value.MaxResponseBodyBytes <= 0
            || value.MaxResponseBodyBytes > ResultadoOperacaoIdempotente.TamanhoMaximoConteudo)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        return value;
    }

    private sealed record ResponseSnapshot(
        int StatusCode,
        string? ContentType,
        IReadOnlyDictionary<string, string[]> Headers)
    {
        internal static ResponseSnapshot Capture(HttpResponse response) => new(
            response.StatusCode,
            response.ContentType,
            response.Headers.ToDictionary(
                header => header.Key,
                header => header.Value.Select(value => value ?? string.Empty).ToArray(),
                StringComparer.OrdinalIgnoreCase));

        internal void Restore(HttpResponse response)
        {
            response.Headers.Clear();
            foreach (var (name, values) in Headers)
            {
                response.Headers[name] = values;
            }
            response.StatusCode = StatusCode;
            response.ContentType = ContentType;
        }
    }
}
