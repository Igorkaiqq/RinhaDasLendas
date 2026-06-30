using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record StartDiscordLinkCommand(Guid UserId) : IRequest<ExternalAuthStartDto>;
