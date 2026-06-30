using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class StartDiscordLoginCommandHandler(IAuthService authService) : IRequestHandler<StartDiscordLoginCommand, ExternalAuthStartDto>
{
    public Task<ExternalAuthStartDto> Handle(StartDiscordLoginCommand request, CancellationToken cancellationToken)
        => authService.StartDiscordLoginAsync(cancellationToken);
}
