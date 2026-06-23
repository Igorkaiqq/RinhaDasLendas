using RinhaDasLendas.Application.Validators;

namespace RinhaDasLendas.Application.Dtos;

public sealed record MeuJogadorProfileRequestDto(
    string NomeExibicao,
    string Discord,
    string RiotId,
    string OpGgUrl,
    string DeepLolUrl,
    string Elo,
    string? Divisao,
    IReadOnlyCollection<PreferenciaRotaDto> Preferencias) : IJogadorDadosBasicos;
