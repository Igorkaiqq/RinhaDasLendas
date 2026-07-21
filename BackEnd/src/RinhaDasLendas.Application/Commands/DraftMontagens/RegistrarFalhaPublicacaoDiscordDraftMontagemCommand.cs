using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record RegistrarFalhaPublicacaoDiscordDraftMontagemCommand(Guid Id, RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
