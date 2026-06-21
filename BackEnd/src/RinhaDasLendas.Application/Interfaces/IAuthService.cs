using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IAuthService
{
    Task<AuthenticatedUserDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<AuthResponseDto> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task LogoutAsync(string? refreshToken, Guid? userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken);
    Task<AuthenticatedUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<AuthenticatedUserDto?> UpdateOwnProfileAsync(Guid userId, UpdateOwnProfileRequestDto request, CancellationToken cancellationToken);
    Task<UserPermissionsDto> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<DiscordLinkStatusDto> GetDiscordStatusAsync(Guid userId, CancellationToken cancellationToken);
}
