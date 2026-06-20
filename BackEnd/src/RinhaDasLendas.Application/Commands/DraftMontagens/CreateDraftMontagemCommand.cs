using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record CreateDraftMontagemCommand(CreateDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto>;
