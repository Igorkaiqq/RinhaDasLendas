using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record DefinirOrdemEscolhaDraftMontagemCommand(Guid Id, DefinirOrdemEscolhaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
