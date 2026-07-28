using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record ReabrirPresencaDraftMontagemCommand(Guid Id) : IRequest<DraftMontagemResponseDto?>;
