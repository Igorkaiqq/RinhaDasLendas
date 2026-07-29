using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record SelecionarModoDraftMontagemCommand(
    Guid Id,
    SelecionarModoDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
