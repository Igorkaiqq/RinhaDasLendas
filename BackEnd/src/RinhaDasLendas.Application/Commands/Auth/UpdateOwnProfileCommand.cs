using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Auth;

public sealed record UpdateOwnProfileCommand(Guid UserId, UpdateOwnProfileRequestDto Request) : IRequest<AuthenticatedUserDto?>;
