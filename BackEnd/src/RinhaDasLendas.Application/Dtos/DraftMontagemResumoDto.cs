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
    string? OrdemEscolhaModo,
    bool PresencaContinuadaManualmente,
    DateTimeOffset? DataRinha,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao,
    bool Arquivado,
    long VersaoEstado);
