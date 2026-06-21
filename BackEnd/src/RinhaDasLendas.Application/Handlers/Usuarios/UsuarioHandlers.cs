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

public sealed class GetUsuarioByIdQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetUsuarioByIdQuery, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(GetUsuarioByIdQuery request, CancellationToken cancellationToken)
        => usuarioService.GetByIdAsync(request.UsuarioId, cancellationToken);
}

public sealed class UpdateUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<UpdateUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.UpdateAsync(request.UsuarioId, request.Request, cancellationToken);
}

public sealed class UpdateUsuarioRolesCommandHandler(IUsuarioService usuarioService) : IRequestHandler<UpdateUsuarioRolesCommand, UsuarioRolesResponseDto?>
{
    public Task<UsuarioRolesResponseDto?> Handle(UpdateUsuarioRolesCommand request, CancellationToken cancellationToken)
        => usuarioService.UpdateRolesAsync(request.UsuarioId, request.Request, cancellationToken);
}

public sealed class AtivarUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<AtivarUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(AtivarUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.AtivarAsync(request.UsuarioId, cancellationToken);
}

public sealed class DesativarUsuarioCommandHandler(IUsuarioService usuarioService) : IRequestHandler<DesativarUsuarioCommand, UsuarioResponseDto?>
{
    public Task<UsuarioResponseDto?> Handle(DesativarUsuarioCommand request, CancellationToken cancellationToken)
        => usuarioService.DesativarAsync(request.UsuarioId, cancellationToken);
}

public sealed class ResetUsuarioPasswordCommandHandler(IUsuarioService usuarioService) : IRequestHandler<ResetUsuarioPasswordCommand>
{
    public Task Handle(ResetUsuarioPasswordCommand request, CancellationToken cancellationToken)
        => usuarioService.ResetPasswordAsync(request.UsuarioId, request.Request, cancellationToken);
}

public sealed class GetAssignableRolesQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetAssignableRolesQuery, RoleListResponseDto>
{
    public Task<RoleListResponseDto> Handle(GetAssignableRolesQuery request, CancellationToken cancellationToken)
        => usuarioService.GetAssignableRolesAsync(cancellationToken);
}

public sealed class GetUsuarioAuditoriaQueryHandler(IUsuarioService usuarioService) : IRequestHandler<GetUsuarioAuditoriaQuery, PaginatedResponseDto<UsuarioAuditoriaResponseDto>>
{
    public Task<PaginatedResponseDto<UsuarioAuditoriaResponseDto>> Handle(GetUsuarioAuditoriaQuery request, CancellationToken cancellationToken)
        => usuarioService.GetAuditoriaAsync(request.UsuarioId, request.Page, request.PageSize, cancellationToken);
}
