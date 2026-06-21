using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record SalvarLayoutDraftMontagemCommand(Guid Id, SalvarLayoutDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
