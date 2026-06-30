using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class DesativarUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<DesativarUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(DesativarUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.DesativarAsync(request.UsuarioId, cancellationToken);
}
