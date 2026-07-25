using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RemoverPresencaManualDraftMontagemCommand(Guid Id, RemoverPresencaManualDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
