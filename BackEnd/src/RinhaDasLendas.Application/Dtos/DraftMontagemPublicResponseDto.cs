using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemPublicResponseDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    string Modo,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    string CriterioCapitaes,
    Guid? TurnoAtualTimeId,
    Guid? TurnoAtualCapitaoId,
    int? TurnoSequencia,
    DateTimeOffset? TurnoIniciadoEm,
    DateTimeOffset? TurnoExpiraEm,
    int DuracaoTurnoSegundos,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? OrdemEscolhaModo,
    bool PresencaContinuadaManualmente,
    IReadOnlyCollection<DraftMontagemPresencaPublicResponseDto> Presencas,
    IReadOnlyCollection<DraftMontagemTimeResponseDto> Times,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Livres,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Reservas,
    IReadOnlyCollection<DraftMontagemEscolhaResponseDto> Escolhas,
    IReadOnlyCollection<DraftMontagemSubstituicaoPublicResponseDto> Substituicoes,
    IReadOnlyCollection<DraftMontagemPublicacaoDiscordPublicResponseDto> PublicacoesDiscord,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemPublicResponseDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        return new DraftMontagemPublicResponseDto(
            montagem.Id,
            montagem.Nome,
            montagem.Observacoes,
            montagem.Status.ToString(),
            montagem.Modo.ToString(),
            montagem.TamanhoEquipe,
            montagem.QuantidadeTimes,
            montagem.QuantidadeReservas,
            montagem.CriterioCapitaes.ToString(),
            montagem.TurnoAtualTimeId,
            montagem.TurnoAtualCapitaoId,
            montagem.TurnoSequencia,
            montagem.TurnoIniciadoEm,
            montagem.TurnoExpiraEm,
            montagem.DuracaoTurnoSegundos,
            montagem.HorarioEncerramentoPresenca,
            montagem.OrdemEscolhaModo?.ToString(),
            montagem.PresencaContinuadaManualmente,
            montagem.Presencas.OrderBy(presenca => presenca.OrdemFinal ?? presenca.OrdemManual ?? presenca.OrdemConfirmacao).Select(DraftMontagemPresencaPublicResponseDto.FromEntity).ToList(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => DraftMontagemTimeResponseDto.FromEntity(time, participantes)).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Livre).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            montagem.Escolhas.OrderBy(escolha => escolha.Sequencia).Select(DraftMontagemEscolhaResponseDto.FromEntity).ToList(),
            montagem.Substituicoes.OrderBy(substituicao => substituicao.RegistradoEm).Select(DraftMontagemSubstituicaoPublicResponseDto.FromEntity).ToList(),
            montagem.PublicacoesDiscord.OrderBy(publicacao => publicacao.Tipo).Select(DraftMontagemPublicacaoDiscordPublicResponseDto.FromEntity).ToList(),
            montagem.DataCadastro,
            montagem.DataAtualizacao);
    }
}

public sealed record DraftMontagemPresencaPublicResponseDto(
    Guid Id,
    Guid UsuarioId,
    Guid JogadorId,
    string NomeExibicao,
    string OrigemConfirmacao,
    string Status,
    DateTimeOffset ConfirmadoEm,
    DateTimeOffset? CanceladoEm,
    int OrdemConfirmacao,
    int? OrdemManual,
    int? OrdemFinal)
{
    public static DraftMontagemPresencaPublicResponseDto FromEntity(DraftMontagemPresenca presenca)
    {
        return new DraftMontagemPresencaPublicResponseDto(
            presenca.Id,
            presenca.UsuarioId,
            presenca.JogadorId,
            presenca.Jogador?.NomeExibicao ?? string.Empty,
            presenca.OrigemConfirmacao.ToString(),
            presenca.Status.ToString(),
            presenca.ConfirmadoEm,
            presenca.CanceladoEm,
            presenca.OrdemConfirmacao,
            presenca.OrdemManual,
            presenca.OrdemFinal);
    }
}

public sealed record DraftMontagemSubstituicaoPublicResponseDto(
    Guid TimeId,
    Guid JogadorSaiuId,
    Guid ReservaEntrouId,
    string? JogadorSaiuNome,
    string? ReservaEntrouNome,
    DateTimeOffset RegistradoEm)
{
    public static DraftMontagemSubstituicaoPublicResponseDto FromEntity(DraftMontagemSubstituicao substituicao)
    {
        return new DraftMontagemSubstituicaoPublicResponseDto(
            substituicao.TimeId,
            substituicao.JogadorSaiuId,
            substituicao.ReservaEntrouId,
            substituicao.JogadorSaiu?.NomeExibicao,
            substituicao.ReservaEntrou?.NomeExibicao,
            substituicao.RegistradoEm);
    }
}

public sealed record DraftMontagemPublicacaoDiscordPublicResponseDto(
    string Tipo,
    string Status,
    DateTimeOffset? PublicadaEm,
    DateTimeOffset UltimaTentativaEm)
{
    public static DraftMontagemPublicacaoDiscordPublicResponseDto FromEntity(DraftMontagemPublicacaoDiscord publicacao)
    {
        return new DraftMontagemPublicacaoDiscordPublicResponseDto(
            publicacao.Tipo.ToString(),
            publicacao.Status.ToString(),
            publicacao.PublicadaEm,
            publicacao.UltimaTentativaEm);
    }
}
