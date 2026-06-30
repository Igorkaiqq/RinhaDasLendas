using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class LogoutCommandHandler(IAuthService authService) : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        => authService.LogoutAsync(request.RefreshToken, request.UserId, request.IpAddress, request.UserAgent, cancellationToken);
}
