using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record CancelarPresencaDraftMontagemCommand(Guid Id, CancelarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
