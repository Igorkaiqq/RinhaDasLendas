using MediatR;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;

namespace RinhaDasLendas.Application.Handlers.Usuarios;

public sealed class GetUsuariosQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetUsuariosQuery, PaginatedResponseDto<UsuarioResumoDto>>
{
    public Task<PaginatedResponseDto<UsuarioResumoDto>> Handle(GetUsuariosQuery request, CancellationToken cancellationToken)
        => usuarioService.ListAsync(request.Search, request.Nome, request.Email, request.Role, request.Status, request.Page, request.PageSize, cancellationToken);
}
