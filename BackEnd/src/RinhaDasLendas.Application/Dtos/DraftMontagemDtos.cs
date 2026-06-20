using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record CreateDraftMontagemRequestDto(
    string Nome,
    string? Observacoes,
    int TamanhoEquipe,
    bool SortearCapitaes,
    IReadOnlyCollection<Guid> CapitaesIds,
    IReadOnlyCollection<Guid> JogadoresIds);

public sealed record SalvarLayoutDraftMontagemRequestDto(
    IReadOnlyCollection<DraftMontagemLayoutTimeDto> Times,
    IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Livres,
    IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Reservas);

public sealed record DraftMontagemLayoutTimeDto(Guid TimeId, string Nome, Guid? CapitaoId, IReadOnlyCollection<DraftMontagemLayoutParticipanteDto> Jogadores);

public sealed record DraftMontagemLayoutParticipanteDto(Guid JogadorId, int Ordem, string? RotaContextual);

public sealed record CancelarDraftMontagemRequestDto(string? Motivo);

public sealed record DraftMontagemResumoDto(
    Guid Id,
    string Nome,
    string Status,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao);

public sealed record DraftMontagemResponseDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    string CriterioCapitaes,
    IReadOnlyCollection<DraftMontagemTimeResponseDto> Times,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Livres,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Reservas,
    string? MotivoCancelamento,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemResponseDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        return new DraftMontagemResponseDto(
            montagem.Id,
            montagem.Nome,
            montagem.Observacoes,
            montagem.Status.ToString(),
            montagem.TamanhoEquipe,
            montagem.QuantidadeTimes,
            montagem.QuantidadeReservas,
            montagem.CriterioCapitaes.ToString(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => DraftMontagemTimeResponseDto.FromEntity(time, participantes)).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Livre).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            montagem.MotivoCancelamento,
            montagem.DataCadastro,
            montagem.DataAtualizacao);
    }
}

public sealed record DraftMontagemTimeResponseDto(
    Guid Id,
    string Nome,
    int Ordem,
    string Cor,
    Guid? CapitaoId,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Jogadores)
{
    public static DraftMontagemTimeResponseDto FromEntity(DraftMontagemTime time, IReadOnlyCollection<DraftMontagemParticipante> participantes)
    {
        return new DraftMontagemTimeResponseDto(
            time.Id,
            time.Nome,
            time.Ordem,
            time.Cor,
            time.CapitaoId,
            participantes.Where(participante => participante.TimeId == time.Id).OrderByDescending(participante => participante.Capitao).ThenBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList());
    }
}

public sealed record DraftMontagemParticipanteResponseDto(
    Guid JogadorId,
    string NomeExibicao,
    string? Discord,
    string? RiotId,
    string? OpGgUrl,
    string? DeepLolUrl,
    string? Elo,
    string? Divisao,
    string Status,
    IReadOnlyCollection<PreferenciaRotaDto> Preferencias,
    string Estado,
    bool Capitao,
    string? RotaContextual,
    int Ordem,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemParticipanteResponseDto FromEntity(DraftMontagemParticipante participante)
    {
        var jogador = participante.Jogador;
        return new DraftMontagemParticipanteResponseDto(
            participante.JogadorId,
            jogador?.NomeExibicao ?? string.Empty,
            jogador?.Discord,
            jogador?.RiotId,
            jogador?.OpGgUrl,
            jogador?.DeepLolUrl,
            jogador?.Elo.ToDisplayName(),
            jogador?.Divisao?.ToDisplayName(),
            jogador?.Status.ToString() ?? string.Empty,
            jogador?.Preferencias.OrderBy(preferencia => preferencia.Prioridade).Select(preferencia => new PreferenciaRotaDto(preferencia.Rota.ToString(), preferencia.Prioridade, preferencia.NaoJogoNemLascando)).ToList() ?? [],
            participante.Estado.ToString(),
            participante.Capitao,
            participante.RotaContextual?.ToString(),
            participante.Ordem,
            jogador?.DataCadastro ?? participante.DataCadastro,
            jogador?.DataAtualizacao ?? participante.DataAtualizacao);
    }
}
