using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class DraftMontagemTests
{
    [Theory]
    [InlineData(15, 5, 3, 0)]
    [InlineData(18, 5, 3, 3)]
    [InlineData(20, 5, 4, 0)]
    public void Deve_calcular_times_e_reservas(int totalJogadores, int tamanhoEquipe, int times, int reservas)
    {
        var resultado = DraftMontagem.CalcularEstrutura(totalJogadores, tamanhoEquipe);

        resultado.QuantidadeTimes.Should().Be(times);
        resultado.QuantidadeReservas.Should().Be(reservas);
    }

    [Fact]
    public void Deve_criar_montagem_com_times_capitaes_e_reservas()
    {
        var jogadores = Enumerable.Range(1, 18).Select(_ => Guid.NewGuid()).ToList();

        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(3).ToList());

        montagem.QuantidadeTimes.Should().Be(3);
        montagem.QuantidadeReservas.Should().Be(3);
        montagem.Times.Should().HaveCount(3);
        montagem.Participantes.Count(participante => participante.Estado == DraftMontagemParticipanteEstado.Reserva).Should().Be(3);
        montagem.Participantes.Count(participante => participante.Capitao).Should().Be(3);
    }

    [Fact]
    public void Deve_impedir_jogador_duplicado_no_layout()
    {
        var jogadores = Enumerable.Range(1, 6).Select(_ => Guid.NewGuid()).ToList();
        var montagem = new DraftMontagem("Rinha", null, 3, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(2).ToList());
        var times = montagem.Times.ToList();

        var act = () => montagem.SalvarLayout(
            [
                new DraftMontagemLayoutTime(times[0].Id, times[0].Nome, jogadores[0], [new DraftMontagemLayoutParticipante(jogadores[0], 1, null)]),
                new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, jogadores[0], [new DraftMontagemLayoutParticipante(jogadores[0], 1, null)])
            ],
            jogadores.Skip(1).Select((jogador, index) => new DraftMontagemLayoutParticipante(jogador, index + 1, null)).ToList(),
            []);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftPlayerAlreadyPicked);
    }

    [Fact]
    public void Deve_sortear_capitaes_sem_mover_jogadores_de_time()
    {
        var jogadores = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(2).ToList());
        var times = montagem.Times.ToList();

        montagem.SalvarLayout(
            [
                new DraftMontagemLayoutTime(times[0].Id, times[0].Nome, jogadores[0], jogadores.Take(5).Select((jogador, index) => new DraftMontagemLayoutParticipante(jogador, index + 1, null)).ToList()),
                new DraftMontagemLayoutTime(times[1].Id, times[1].Nome, jogadores[5], jogadores.Skip(5).Select((jogador, index) => new DraftMontagemLayoutParticipante(jogador, index + 1, null)).ToList())
            ],
            [],
            []);

        montagem.SortearCapitaes();

        foreach (var time in times)
        {
            var membros = montagem.Participantes.Where(participante => participante.TimeId == time.Id).ToList();
            membros.Should().HaveCount(5);
            membros.Should().ContainSingle(participante => participante.Capitao);
            membros.Should().Contain(participante => participante.JogadorId == time.CapitaoId);
        }
    }

    [Fact]
    public void Deve_cancelar_presenca_aberta_expirada()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

        montagem.CancelarPresencaExpirada();

        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
    }

    [Fact]
    public void Deve_impedir_cancelar_presenca_expirada_quando_presenca_ja_foi_encerrada()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        foreach (var _ in Enumerable.Range(1, 10))
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), Guid.NewGuid(), null, DraftMontagemPresencaOrigem.Web);
        }
        montagem.EncerrarPresenca(false, 5);

        var act = () => montagem.CancelarPresencaExpirada();

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceAlreadyClosed);
    }

    [Fact]
    public void Deve_cancelar_montagem_com_presenca_aberta()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var responsavelId = Guid.NewGuid();

        montagem.Cancelar("sem jogadores", responsavelId);

        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.MotivoCancelamento.Should().Be("sem jogadores");
        montagem.AcoesAdministrativas.Should().ContainSingle(acao => acao.ResponsavelUsuarioId == responsavelId && acao.Motivo == "sem jogadores");
    }

    [Fact]
    public void Deve_impedir_cancelar_montagem_finalizada()
    {
        var jogadores = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(2).ToList());
        montagem.Finalizar();

        var act = () => montagem.Cancelar("encerrar");

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DraftClosed);
    }

    [Fact]
    public void Deve_adicionar_presenca_manual_em_presenca_aberta()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var usuarioId = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();

        var presenca = montagem.AdicionarPresencaManual(usuarioId, jogadorId);

        presenca.UsuarioId.Should().Be(usuarioId);
        presenca.JogadorId.Should().Be(jogadorId);
        presenca.OrigemConfirmacao.Should().Be(DraftMontagemPresencaOrigem.Manual);
        presenca.Confirmada.Should().BeTrue();
    }

    [Fact]
    public void Deve_impedir_presenca_manual_duplicada()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var usuarioId = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(usuarioId, jogadorId);

        var act = () => montagem.AdicionarPresencaManual(usuarioId, jogadorId);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PlayerAlreadyInQueue);
    }

    [Fact]
    public void Deve_remover_presenca_manual_em_presenca_aberta()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var jogadorId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(Guid.NewGuid(), jogadorId);

        montagem.RemoverPresencaManual(jogadorId);

        montagem.Presencas.Should().ContainSingle().Which.Confirmada.Should().BeFalse();
    }

    [Fact]
    public void Deve_auditar_remocao_manual_de_presenca_com_jogador_alvo()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var jogadorId = Guid.NewGuid();
        var responsavelId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(Guid.NewGuid(), jogadorId);

        montagem.RemoverPresencaManual(jogadorId, responsavelId, "não poderá jogar");

        montagem.AcoesAdministrativas.Should().ContainSingle(acao =>
            acao.Tipo == "RemocaoPresencaManual"
            && acao.ResponsavelUsuarioId == responsavelId
            && acao.JogadorAlvoId == jogadorId
            && acao.Motivo == "não poderá jogar");
    }

    [Fact]
    public void Deve_registrar_estado_de_publicacao_discord_por_tipo()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

        montagem.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal-presenca", "mensagem-presenca");
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "guild", "canal-draft", "MissingPermissions");

        montagem.PublicacoesDiscord.Should().ContainSingle(item =>
            item.Tipo == DraftMontagemPublicacaoDiscordTipo.Presenca
            && item.Status == DraftMontagemPublicacaoDiscordStatus.Publicada
            && item.MessageId == "mensagem-presenca");
        montagem.PublicacoesDiscord.Should().ContainSingle(item =>
            item.Tipo == DraftMontagemPublicacaoDiscordTipo.TimesDefinidos
            && item.Status == DraftMontagemPublicacaoDiscordStatus.Falha
            && item.UltimoErroCodigo == "MissingPermissions");
    }

    [Fact]
    public void Deve_solicitar_republicacao_discord_com_auditoria()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var responsavelId = Guid.NewGuid();
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "guild", "canal", "MissingPermissions");

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, responsavelId, "permissão corrigida");

        montagem.PublicacoesDiscord.Should().ContainSingle(item =>
            item.Tipo == DraftMontagemPublicacaoDiscordTipo.TimesDefinidos
            && item.Status == DraftMontagemPublicacaoDiscordStatus.Pendente
            && item.UltimoErroCodigo == null);
        montagem.AcoesAdministrativas.Should().ContainSingle(acao =>
            acao.ResponsavelUsuarioId == responsavelId
            && acao.Motivo == "permissão corrigida");
    }

}
