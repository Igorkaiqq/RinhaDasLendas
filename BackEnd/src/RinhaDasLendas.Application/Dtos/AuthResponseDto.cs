using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record AuthResponseDto(string AccessToken, int ExpiresIn, AuthenticatedUserDto Usuario)
{
    [JsonIgnore]
    public string? RefreshToken { get; init; }
}
