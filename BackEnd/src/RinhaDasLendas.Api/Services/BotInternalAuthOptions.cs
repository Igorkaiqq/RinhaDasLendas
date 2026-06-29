namespace RinhaDasLendas.Api.Services;

public sealed class BotInternalAuthOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
{
    public const string SchemeName = "BotInternal";
    public const string HeaderName = "X-Rinha-Internal-Token";
    public string Token { get; set; } = string.Empty;
    public IReadOnlyCollection<string> ValidTokens { get; set; } = [];
}
