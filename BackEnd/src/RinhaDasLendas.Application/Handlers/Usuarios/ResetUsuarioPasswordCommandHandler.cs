using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class ResetUsuarioPasswordCommandHandler(IUsuarioService usuarioService) : IRequestHandler<ResetUsuarioPasswordCommand>
{
    public Task Handle(ResetUsuarioPasswordCommand request, CancellationToken cancellationToken)
        => usuarioService.ResetPasswordAsync(request.UsuarioId, request.Request, cancellationToken);
}
