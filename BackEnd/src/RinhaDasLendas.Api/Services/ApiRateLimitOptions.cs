namespace RinhaDasLendas.Api.Services;

internal sealed class ApiRateLimitOptions
{
    internal const string SectionName = "RateLimiting:Api";

    public int PermitLimit { get; init; } = 120;

    public int WindowSeconds { get; init; } = 60;
}
