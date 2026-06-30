namespace RinhaDasLendas.Api.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : context.TraceIdentifier;

        context.Response.Headers[HeaderName] = correlationId;
        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>().BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        }))
        {
            await next(context);
        }
    }
}
