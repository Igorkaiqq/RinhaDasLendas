using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class UnlinkDiscordCommandHandler(IAuthService authService) : IRequestHandler<UnlinkDiscordCommand>
{
    public Task Handle(UnlinkDiscordCommand request, CancellationToken cancellationToken)
        => authService.UnlinkDiscordAsync(request.UserId, cancellationToken);
}
