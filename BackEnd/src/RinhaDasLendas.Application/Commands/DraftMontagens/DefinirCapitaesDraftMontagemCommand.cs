using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record DefinirCapitaesDraftMontagemCommand(Guid Id, DefinirCapitaesDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
