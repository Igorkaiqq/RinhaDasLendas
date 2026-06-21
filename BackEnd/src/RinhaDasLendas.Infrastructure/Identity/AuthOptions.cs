namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class AuthOptions
{
    public JwtOptions Jwt { get; set; } = new();
    public CookieOptions Cookie { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "RinhaDasLendas";
    public string Audience { get; set; } = "RinhaDasLendas.FrontEnd";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}

public sealed class CookieOptions
{
    public string RefreshTokenName { get; set; } = "rinha_refresh_token";
}
