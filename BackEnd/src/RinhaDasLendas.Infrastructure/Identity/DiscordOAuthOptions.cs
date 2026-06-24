namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class DiscordOAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scopes { get; set; } = "identify email";
    public string AuthorizationUrl { get; set; } = "https://discord.com/oauth2/authorize";
    public string TokenUrl { get; set; } = "https://discord.com/api/oauth2/token";
    public string RevocationUrl { get; set; } = "https://discord.com/api/oauth2/token/revoke";
    public string UserInfoUrl { get; set; } = "https://discord.com/api/users/@me";
    public string FrontendSuccessUrl { get; set; } = "http://localhost:5173/configuracoes?discord=success";
    public string FrontendErrorUrl { get; set; } = "http://localhost:5173/configuracoes?discord=error";
    public string FrontendLoginErrorUrl { get; set; } = "http://localhost:5173/login?discord=error";
    public int StateExpirationMinutes { get; set; } = 10;
}
