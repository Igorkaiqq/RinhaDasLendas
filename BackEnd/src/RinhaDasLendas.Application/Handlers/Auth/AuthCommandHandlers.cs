using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class RegisterUserCommandHandler(IAuthService authService) : IRequestHandler<RegisterUserCommand, AuthenticatedUserDto>
{
    public Task<AuthenticatedUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request.Request, cancellationToken);
}

public sealed class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, AuthResponseDto>
{
    public Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        => authService.LoginAsync(request.Request, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class RefreshSessionCommandHandler(IAuthService authService) : IRequestHandler<RefreshSessionCommand, AuthResponseDto>
{
    public Task<AuthResponseDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        => authService.RefreshAsync(request.RefreshToken, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class LogoutCommandHandler(IAuthService authService) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        => authService.LogoutAsync(request.RefreshToken, request.UserId, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class ForgotPasswordCommandHandler(IAuthService authService) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseDto>
{
    public Task<ForgotPasswordResponseDto> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => authService.ForgotPasswordAsync(request.Request, cancellationToken);
}

public sealed class ResetPasswordCommandHandler(IAuthService authService) : IRequestHandler<ResetPasswordCommand>
{
    public Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => authService.ResetPasswordAsync(request.Request, cancellationToken);
}

public sealed class ChangePasswordCommandHandler(IAuthService authService) : IRequestHandler<ChangePasswordCommand>
{
    public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        => authService.ChangePasswordAsync(request.UserId, request.Request, cancellationToken);
}

public sealed class UpdateOwnProfileCommandHandler(IAuthService authService) : IRequestHandler<UpdateOwnProfileCommand, AuthenticatedUserDto?>
{
    public Task<AuthenticatedUserDto?> Handle(UpdateOwnProfileCommand request, CancellationToken cancellationToken)
        => authService.UpdateOwnProfileAsync(request.UserId, request.Request, cancellationToken);
}

public sealed class StartDiscordLoginCommandHandler(IAuthService authService) : IRequestHandler<StartDiscordLoginCommand, ExternalAuthStartDto>
{
    public Task<ExternalAuthStartDto> Handle(StartDiscordLoginCommand request, CancellationToken cancellationToken)
        => authService.StartDiscordLoginAsync(cancellationToken);
}

public sealed class StartDiscordLinkCommandHandler(IAuthService authService) : IRequestHandler<StartDiscordLinkCommand, ExternalAuthStartDto>
{
    public Task<ExternalAuthStartDto> Handle(StartDiscordLinkCommand request, CancellationToken cancellationToken)
        => authService.StartDiscordLinkAsync(request.UserId, cancellationToken);
}

public sealed class HandleDiscordCallbackCommandHandler(IAuthService authService) : IRequestHandler<HandleDiscordCallbackCommand, DiscordCallbackResultDto>
{
    public Task<DiscordCallbackResultDto> Handle(HandleDiscordCallbackCommand request, CancellationToken cancellationToken)
        => authService.HandleDiscordCallbackAsync(request.Code, request.State, request.IpAddress, request.UserAgent, cancellationToken);
}

public sealed class UnlinkDiscordCommandHandler(IAuthService authService) : IRequestHandler<UnlinkDiscordCommand>
{
    public Task Handle(UnlinkDiscordCommand request, CancellationToken cancellationToken)
        => authService.UnlinkDiscordAsync(request.UserId, cancellationToken);
}
