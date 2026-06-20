using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record CancelarDraftMontagemCommand(Guid Id, CancelarDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
