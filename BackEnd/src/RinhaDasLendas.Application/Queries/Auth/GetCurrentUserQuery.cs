using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Auth;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<AuthenticatedUserDto?>;
