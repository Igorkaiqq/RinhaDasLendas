using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record ResetUsuarioPasswordCommand(Guid UsuarioId, ResetUsuarioPasswordRequestDto Request) : IRequest;
