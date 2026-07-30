namespace RinhaDasLendas.Application.Security;

public sealed record CompetitiveAuthorizationContext(
    string Capability,
    string Operation,
    string ResourceType,
    string? SerieType,
    string? SeasonState,
    bool HasStartedSeries,
    bool HasRequiredJustification);
