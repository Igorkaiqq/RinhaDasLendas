using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record AdicionarPresencaManualDraftMontagemCommand(Guid Id, AdicionarPresencaManualDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
