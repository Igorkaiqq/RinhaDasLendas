using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record FinalizarDraftMontagemCommand(Guid Id) : IRequest<DraftMontagemResponseDto?>;
