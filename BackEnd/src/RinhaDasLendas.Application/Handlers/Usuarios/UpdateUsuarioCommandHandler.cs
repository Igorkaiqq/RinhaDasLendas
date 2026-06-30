using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class UpdateUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<UpdateUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.UpdateAsync(request.UsuarioId, request.Request, cancellationToken);
}
