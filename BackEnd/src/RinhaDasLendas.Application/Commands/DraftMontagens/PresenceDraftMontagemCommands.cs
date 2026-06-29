using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record ConfirmarPresencaDraftMontagemCommand(Guid Id, ConfirmarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

public sealed record CancelarPresencaDraftMontagemCommand(Guid Id, CancelarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

public sealed record EncerrarPresencaDraftMontagemCommand(Guid Id, EncerrarPresencaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

public sealed record DefinirCapitaesDraftMontagemCommand(Guid Id, DefinirCapitaesDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

public sealed record DefinirOrdemEscolhaDraftMontagemCommand(Guid Id, DefinirOrdemEscolhaDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;

public sealed record RegistrarPublicacaoDiscordDraftMontagemCommand(Guid Id, RegistrarPublicacaoDiscordDraftMontagemRequestDto Request) : IRequest<DraftMontagemResponseDto?>;
