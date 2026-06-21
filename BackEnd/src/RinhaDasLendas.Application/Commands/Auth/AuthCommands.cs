using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record RegisterUserCommand(RegisterRequestDto Request) : IRequest<AuthenticatedUserDto>;

public sealed record LoginCommand(LoginRequestDto Request, string? IpAddress, string? UserAgent) : IRequest<AuthResponseDto>;

public sealed record RefreshSessionCommand(string RefreshToken, string? IpAddress, string? UserAgent) : IRequest<AuthResponseDto>;

public sealed record LogoutCommand(string? RefreshToken, Guid? UserId, string? IpAddress, string? UserAgent) : IRequest;

public sealed record ForgotPasswordCommand(ForgotPasswordRequestDto Request) : IRequest<ForgotPasswordResponseDto>;

public sealed record ResetPasswordCommand(ResetPasswordRequestDto Request) : IRequest;

public sealed record ChangePasswordCommand(Guid UserId, ChangePasswordRequestDto Request) : IRequest;

public sealed record UpdateOwnProfileCommand(Guid UserId, UpdateOwnProfileRequestDto Request) : IRequest<AuthenticatedUserDto?>;
