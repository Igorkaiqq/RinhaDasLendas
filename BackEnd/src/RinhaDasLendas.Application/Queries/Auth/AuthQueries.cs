using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Auth;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<AuthenticatedUserDto?>;

public sealed record GetCurrentUserPermissionsQuery(Guid UserId) : IRequest<UserPermissionsDto>;

public sealed record GetDiscordLinkStatusQuery(Guid UserId) : IRequest<DiscordLinkStatusDto>;
