using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record DesativarUsuarioCommand(Guid UsuarioId) : IRequest<UsuarioResponseDto?>;
