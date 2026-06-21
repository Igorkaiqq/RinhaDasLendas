using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Auth;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class GetCurrentUserQueryHandler(IAuthService authService) : IRequestHandler<GetCurrentUserQuery, AuthenticatedUserDto?>
{
    public Task<AuthenticatedUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        => authService.GetCurrentUserAsync(request.UserId, cancellationToken);
}

public sealed class GetCurrentUserPermissionsQueryHandler(IAuthService authService) : IRequestHandler<GetCurrentUserPermissionsQuery, UserPermissionsDto>
{
    public Task<UserPermissionsDto> Handle(GetCurrentUserPermissionsQuery request, CancellationToken cancellationToken)
        => authService.GetPermissionsAsync(request.UserId, cancellationToken);
}

public sealed class GetDiscordLinkStatusQueryHandler(IAuthService authService) : IRequestHandler<GetDiscordLinkStatusQuery, DiscordLinkStatusDto>
{
    public Task<DiscordLinkStatusDto> Handle(GetDiscordLinkStatusQuery request, CancellationToken cancellationToken)
        => authService.GetDiscordStatusAsync(request.UserId, cancellationToken);
}
