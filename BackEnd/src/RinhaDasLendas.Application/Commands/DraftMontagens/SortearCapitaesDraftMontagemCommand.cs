using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record SortearCapitaesDraftMontagemCommand(Guid Id) : IRequest<DraftMontagemResponseDto?>;
