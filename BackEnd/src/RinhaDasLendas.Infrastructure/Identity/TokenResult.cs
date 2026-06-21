namespace RinhaDasLendas.Infrastructure.Identity;

public sealed record TokenResult(string AccessToken, string RefreshToken, int ExpiresIn);
