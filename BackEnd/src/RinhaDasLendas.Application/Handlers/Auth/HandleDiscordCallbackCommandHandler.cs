using MediatR;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class HandleDiscordCallbackCommandHandler(IAuthService authService) : IRequestHandler<HandleDiscordCallbackCommand, DiscordCallbackResultDto>
{
    public Task<DiscordCallbackResultDto> Handle(HandleDiscordCallbackCommand request, CancellationToken cancellationToken)
        => authService.HandleDiscordCallbackAsync(request.Code, request.State, request.IpAddress, request.UserAgent, cancellationToken);
}
