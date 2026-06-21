using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IUsuarioService
{
    Task<PaginatedResponseDto<UsuarioResumoDto>> ListAsync(string? search, string? nome, string? email, string? role, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<UsuarioResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UsuarioResponseDto?> UpdateAsync(Guid id, UpdateUsuarioRequestDto request, CancellationToken cancellationToken);
    Task<UsuarioRolesResponseDto?> UpdateRolesAsync(Guid id, UpdateUsuarioRolesRequestDto request, CancellationToken cancellationToken);
    Task<UsuarioResponseDto?> AtivarAsync(Guid id, CancellationToken cancellationToken);
    Task<UsuarioResponseDto?> DesativarAsync(Guid id, CancellationToken cancellationToken);
    Task ResetPasswordAsync(Guid id, ResetUsuarioPasswordRequestDto request, CancellationToken cancellationToken);
    Task<RoleListResponseDto> GetAssignableRolesAsync(CancellationToken cancellationToken);
    Task<PaginatedResponseDto<UsuarioAuditoriaResponseDto>> GetAuditoriaAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken);
}
