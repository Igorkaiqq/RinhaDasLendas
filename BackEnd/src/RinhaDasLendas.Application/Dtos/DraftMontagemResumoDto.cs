using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemResumoDto(
    Guid Id,
    string Nome,
    string Status,
    string Modo,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordGuildId,
    string? DiscordPresenceMessageId,
    string? OrdemEscolhaModo,
    bool PresencaContinuadaManualmente,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao);
