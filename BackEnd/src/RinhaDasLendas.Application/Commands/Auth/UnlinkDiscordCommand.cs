using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record UnlinkDiscordCommand(Guid UserId) : IRequest;
