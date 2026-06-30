using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class GetAssignableRolesQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetAssignableRolesQuery, RoleListResponseDto>
{
    public Task<RoleListResponseDto> Handle(GetAssignableRolesQuery request, CancellationToken cancellationToken)
        => usuarioService.GetAssignableRolesAsync(cancellationToken);
}
