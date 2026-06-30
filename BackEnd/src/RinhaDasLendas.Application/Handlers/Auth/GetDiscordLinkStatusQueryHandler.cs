using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Auth;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class GetDiscordLinkStatusQueryHandler(IAuthService authService) : IRequestHandler<GetDiscordLinkStatusQuery, DiscordLinkStatusDto>
{
    public Task<DiscordLinkStatusDto> Handle(GetDiscordLinkStatusQuery request, CancellationToken cancellationToken)
        => authService.GetDiscordStatusAsync(request.UserId, cancellationToken);
}
