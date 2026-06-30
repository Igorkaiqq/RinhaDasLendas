using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Auth;

public sealed record GetDiscordLinkStatusQuery(Guid UserId) : IRequest<DiscordLinkStatusDto>;
