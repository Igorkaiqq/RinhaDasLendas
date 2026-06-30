using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Auth;

namespace RinhaDasLendas.Application.Handlers.Auth;

public sealed class GetCurrentUserPermissionsQueryHandler(IAuthService authService) : IRequestHandler<GetCurrentUserPermissionsQuery, UserPermissionsDto>
{
    public Task<UserPermissionsDto> Handle(GetCurrentUserPermissionsQuery request, CancellationToken cancellationToken)
        => authService.GetPermissionsAsync(request.UserId, cancellationToken);
}
