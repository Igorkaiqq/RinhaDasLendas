using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class UpdateUsuarioRolesCommandHandler(IUsuarioService usuarioService) : IRequestHandler<UpdateUsuarioRolesCommand, UsuarioRolesResponseDto?>
{
    public Task<UsuarioRolesResponseDto?> Handle(UpdateUsuarioRolesCommand request, CancellationToken cancellationToken)
        => usuarioService.UpdateRolesAsync(request.UsuarioId, request.Request, cancellationToken);
}
