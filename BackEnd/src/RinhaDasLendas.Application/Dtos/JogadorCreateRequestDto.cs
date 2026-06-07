using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Application.Dtos;

public sealed record JogadorCreateRequestDto(
    string NomeExibicao,
    string? NomeReal,
    string? Discord,
    string? RiotId,
    string? OpGgUrl,
    string? DeepLolUrl,
    string? Elo,
    string? Divisao,
    IReadOnlyCollection<PreferenciaRotaDto> Preferencias) : IJogadorDadosBasicos;
