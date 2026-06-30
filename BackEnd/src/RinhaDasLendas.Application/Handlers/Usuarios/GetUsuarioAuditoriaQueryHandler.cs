using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class GetUsuarioAuditoriaQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetUsuarioAuditoriaQuery, PaginatedResponseDto<UsuarioAuditoriaResponseDto>>
{
    public Task<PaginatedResponseDto<UsuarioAuditoriaResponseDto>> Handle(GetUsuarioAuditoriaQuery request, CancellationToken cancellationToken)
        => usuarioService.GetAuditoriaAsync(request.UsuarioId, request.Page, request.PageSize, cancellationToken);
}
