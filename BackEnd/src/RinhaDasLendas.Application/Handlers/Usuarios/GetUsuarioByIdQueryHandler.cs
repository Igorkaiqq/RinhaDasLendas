using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class GetUsuarioByIdQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetUsuarioByIdQuery, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(GetUsuarioByIdQuery request, CancellationToken cancellationToken)
        => usuarioService.GetByIdAsync(request.UsuarioId, cancellationToken);
}
