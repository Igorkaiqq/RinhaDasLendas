using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class StartDiscordLinkCommandHandler(IAuthService authService) : IRequestHandler<StartDiscordLinkCommand, ExternalAuthStartDto>
{
    public Task<ExternalAuthStartDto> Handle(StartDiscordLinkCommand request, CancellationToken cancellationToken)
        => authService.StartDiscordLinkAsync(request.UserId, cancellationToken);
}
