using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class AtivarUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<AtivarUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(AtivarUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.AtivarAsync(request.UsuarioId, cancellationToken);
}
