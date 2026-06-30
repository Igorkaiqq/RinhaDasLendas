using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Auth;

public sealed record GetCurrentUserPermissionsQuery(Guid UserId) : IRequest<UserPermissionsDto>;
