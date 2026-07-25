using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class DraftMontagemTests
{
    [Theory]
    [InlineData(DraftMontagemPublicacaoDiscordTipo.Presenca)]
    [InlineData(DraftMontagemPublicacaoDiscordTipo.ChamadaPresenca)]
    public void PublicacaoDePresencaNaoDeveIniciarAposCancelamento(
        DraftMontagemPublicacaoDiscordTipo tipo)
    {
        var agora = new DateTimeOffset(2026, 7, 24, 18, 0, 0, TimeSpan.Zero);
        var montagem = new DraftMontagem(
            "Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.Cancelar("encerrada", Guid.NewGuid());

        var act = () => montagem.IniciarTentativaPublicacaoDiscord(
            tipo, "guild", "canal", Guid.NewGuid(), agora.AddMinutes(5), agora);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceAlreadyClosed);
        montagem.PublicacoesDiscord.Should().BeEmpty();
    }
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

        var presenca = montagem.AdicionarPresencaManual(usuarioId, jogadorId, Guid.NewGuid(), "Adição manual");

        presenca.UsuarioId.Should().Be(usuarioId);
        presenca.JogadorId.Should().Be(jogadorId);
        presenca.OrigemConfirmacao.Should().Be(DraftMontagemPresencaOrigem.Manual);
        presenca.Confirmada.Should().BeTrue();
    }

    [Fact]
    public void Deve_manter_confirmacao_e_cancelamento_repetidos_como_no_op()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var usuarioId = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();
        var primeiraConfirmacao = montagem.ConfirmarPresenca(usuarioId, jogadorId, null, DraftMontagemPresencaOrigem.Web);
        var versaoConfirmada = montagem.VersaoEstado;

        var segundaConfirmacao = montagem.ConfirmarPresenca(usuarioId, jogadorId, null, DraftMontagemPresencaOrigem.Web);

        segundaConfirmacao.Should().BeSameAs(primeiraConfirmacao);
        montagem.Presencas.Should().ContainSingle();
        montagem.VersaoEstado.Should().Be(versaoConfirmada);

        montagem.CancelarPresenca(usuarioId);
        var versaoCancelada = montagem.VersaoEstado;

        var act = () => montagem.CancelarPresenca(usuarioId);

        act.Should().NotThrow();
        montagem.Presencas.Should().ContainSingle().Which.Confirmada.Should().BeFalse();
        montagem.VersaoEstado.Should().Be(versaoCancelada);
    }

    [Fact]
    public void Deve_impedir_presenca_manual_duplicada()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var usuarioId = Guid.NewGuid();
        var jogadorId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(usuarioId, jogadorId, Guid.NewGuid(), "Adição manual");

        var act = () => montagem.AdicionarPresencaManual(usuarioId, jogadorId, Guid.NewGuid(), "Adição manual");

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PlayerAlreadyInQueue);
    }

    [Fact]
    public void Deve_remover_presenca_manual_em_presenca_aberta()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var jogadorId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(Guid.NewGuid(), jogadorId, Guid.NewGuid(), "Adição manual");

        montagem.RemoverPresencaManual(jogadorId);

        montagem.Presencas.Should().ContainSingle().Which.Confirmada.Should().BeFalse();
    }

    [Fact]
    public void Deve_auditar_remocao_manual_de_presenca_com_jogador_alvo()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var jogadorId = Guid.NewGuid();
        var responsavelId = Guid.NewGuid();
        montagem.AdicionarPresencaManual(Guid.NewGuid(), jogadorId, Guid.NewGuid(), "Adição manual");

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
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
        var claimPresenca = Guid.NewGuid();
        var claimTimes = Guid.NewGuid();

        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal-presenca", claimPresenca, agora.AddMinutes(5), agora);
        montagem.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimPresenca, "guild", "canal-presenca", "mensagem-presenca", agora.AddMinutes(1));
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "guild", "canal-draft", claimTimes, agora.AddMinutes(5), agora);
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, claimTimes, "guild", "canal-draft", "MissingPermissions", agora.AddMinutes(1));

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
        var montagem = NovaMontagem();
        var responsavelId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "guild", "canal", claimId, agora.AddMinutes(5), agora);
        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, claimId, "guild", "canal", "MissingPermissions", agora.AddMinutes(1));

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, responsavelId, "permissão corrigida", agora.AddMinutes(2));

        montagem.PublicacoesDiscord.Should().ContainSingle(item =>
            item.Tipo == DraftMontagemPublicacaoDiscordTipo.TimesDefinidos
            && item.Status == DraftMontagemPublicacaoDiscordStatus.Pendente
            && item.UltimoErroCodigo == null);
        montagem.AcoesAdministrativas.Should().ContainSingle(acao =>
            acao.ResponsavelUsuarioId == responsavelId
            && acao.Motivo == "permissão corrigida");
    }

    [Fact]
    public void Deve_conceder_claim_para_publicacao_pendente()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        var expiraEm = agora.AddMinutes(5);

        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, expiraEm, agora);

        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
        publicacao.ClaimId.Should().Be(claimId);
        publicacao.ClaimExpiraEm.Should().Be(expiraEm);
        publicacao.UltimaTentativaEm.Should().Be(agora);
        montagem.DataAtualizacao.Should().Be(agora);
    }

    [Fact]
    public void Deve_rejeitar_segundo_claim_sem_alterar_tentativa_ativa()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        var expiraEm = agora.AddMinutes(5);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, expiraEm, agora);

        var act = () => montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            "outra-guild",
            "outro-canal",
            Guid.NewGuid(),
            agora.AddMinutes(10),
            agora.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationNotPending);
        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
        publicacao.ClaimId.Should().Be(claimId);
        publicacao.ClaimExpiraEm.Should().Be(expiraEm);
        publicacao.GuildId.Should().Be("guild");
        publicacao.ChannelId.Should().Be("canal");
    }

    [Fact]
    public void Deve_concluir_publicacao_com_claim_ativo_e_relogio_explicito()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        montagem.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "canal", "mensagem", agora.AddMinutes(1));

        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Publicada);
        publicacao.MessageId.Should().Be("mensagem");
        publicacao.PublicadaEm.Should().Be(agora.AddMinutes(1));
        publicacao.UltimaTentativaEm.Should().Be(agora.AddMinutes(1));
        publicacao.ClaimExpiraEm.Should().BeNull();
    }

    [Fact]
    public void Deve_registrar_falha_somente_com_claim_ativo()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        montagem.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, claimId, "guild", "canal", "MissingPermissions", agora.AddMinutes(1));

        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Falha);
        publicacao.UltimoErroCodigo.Should().Be("MissingPermissions");
        publicacao.UltimaTentativaEm.Should().Be(agora.AddMinutes(1));
        publicacao.ClaimExpiraEm.Should().BeNull();
    }

    [Fact]
    public void Deve_rejeitar_conclusao_com_claim_divergente_sem_alterar_estado()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        var act = () => montagem.RegistrarPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            Guid.NewGuid(),
            "outra-guild",
            "outro-canal",
            "mensagem",
            agora.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationClaimMismatch);
        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
        publicacao.ClaimId.Should().Be(claimId);
        publicacao.MessageId.Should().BeNull();
        publicacao.GuildId.Should().Be("guild");
        publicacao.ChannelId.Should().Be("canal");
    }

    [Fact]
    public void Deve_marcar_tentativa_expirada_para_reconciliacao()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        var alterada = montagem.MarcarPublicacaoDiscordRequerReconciliacao(DraftMontagemPublicacaoDiscordTipo.Presenca, agora.AddMinutes(5));

        alterada.Should().BeTrue();
        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao);
        publicacao.ClaimId.Should().Be(claimId);
        publicacao.ClaimExpiraEm.Should().BeNull();
        publicacao.UltimaTentativaEm.Should().Be(agora);
    }

    [Fact]
    public void Deve_impedir_novo_claim_quando_publicacao_requer_reconciliacao()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", Guid.NewGuid(), agora.AddMinutes(5), agora);
        montagem.MarcarPublicacaoDiscordRequerReconciliacao(DraftMontagemPublicacaoDiscordTipo.Presenca, agora.AddMinutes(5));

        var act = () => montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            "guild",
            "canal",
            Guid.NewGuid(),
            agora.AddMinutes(11),
            agora.AddMinutes(6));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationNotPending);
        montagem.PublicacoesDiscord.Should().ContainSingle().Which.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Deve_rejeitar_conclusao_no_instante_ou_apos_expiracao(int minutosDepois)
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        var act = () => montagem.RegistrarPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            claimId,
            "guild",
            "canal",
            "mensagem",
            agora.AddMinutes(minutosDepois));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationClaimExpired);
        montagem.PublicacoesDiscord.Should().ContainSingle().Which.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
    }

    [Fact]
    public void Deve_rejeitar_falha_no_instante_da_expiracao()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        var act = () => montagem.RegistrarFalhaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            claimId,
            "guild",
            "canal",
            "Timeout",
            agora.AddMinutes(5));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationClaimExpired);
        montagem.PublicacoesDiscord.Should().ContainSingle().Which.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
    }

    [Fact]
    public void Deve_rejeitar_claim_vazio()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);

        var act = () => montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            "guild",
            "canal",
            Guid.Empty,
            agora.AddMinutes(5),
            agora);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationClaimInvalid);
        montagem.PublicacoesDiscord.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deve_rejeitar_expiracao_de_claim_nao_futura(int minutosDepois)
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);

        var act = () => montagem.IniciarTentativaPublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            "guild",
            "canal",
            Guid.NewGuid(),
            agora.AddMinutes(minutosDepois),
            agora);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationClaimExpirationInvalid);
        montagem.PublicacoesDiscord.Should().BeEmpty();
    }

    [Fact]
    public void Deve_rejeitar_republicacao_em_andamento_sem_limpar_claim()
    {
        var montagem = NovaMontagem();
        var claimId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", claimId, agora.AddMinutes(5), agora);

        var act = () => montagem.SolicitarRepublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            Guid.NewGuid(),
            "nova tentativa",
            agora.AddMinutes(1));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationInProgress);
        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
        publicacao.ClaimId.Should().Be(claimId);
        publicacao.ClaimExpiraEm.Should().Be(agora.AddMinutes(5));
    }

    [Fact]
    public void Deve_exigir_confirmacao_de_ausencia_para_republicar_publicada()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        Publicar(montagem, DraftMontagemPublicacaoDiscordTipo.Presenca, agora);

        var act = () => montagem.SolicitarRepublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            Guid.NewGuid(),
            "mensagem ainda existe",
            agora);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.DiscordPublicationStillPublished);
        montagem.PublicacoesDiscord.Should().ContainSingle().Which.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Publicada);
    }

    [Fact]
    public void Deve_republicar_publicada_apos_confirmacao_administrativa_de_ausencia()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        Publicar(montagem, DraftMontagemPublicacaoDiscordTipo.Presenca, agora);

        montagem.SolicitarRepublicacaoDiscord(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            Guid.NewGuid(),
            "mensagem removida",
            agora,
            confirmarAusenciaPublicacao: true);

        montagem.PublicacoesDiscord.Should().ContainSingle().Which.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
    }

    [Fact]
    public void Deve_republicar_estado_requer_reconciliacao()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        montagem.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "canal", Guid.NewGuid(), agora.AddMinutes(5), agora);
        montagem.MarcarPublicacaoDiscordRequerReconciliacao(DraftMontagemPublicacaoDiscordTipo.Presenca, agora.AddMinutes(5));

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, Guid.NewGuid(), "reconciliada", agora.AddMinutes(6));

        var publicacao = montagem.PublicacoesDiscord.Should().ContainSingle().Subject;
        publicacao.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
        publicacao.ClaimId.Should().BeNull();
    }

    [Fact]
    public void Deve_tratar_republicacao_pendente_como_idempotente()
    {
        var montagem = NovaMontagem();
        var responsavelId = Guid.NewGuid();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);
        Falhar(montagem, DraftMontagemPublicacaoDiscordTipo.Presenca, agora);
        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, responsavelId, "corrigida", agora);
        var versao = montagem.VersaoEstado;
        var auditorias = montagem.AcoesAdministrativas.Count;

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, responsavelId, "corrigida", agora.AddMinutes(1));

        montagem.VersaoEstado.Should().Be(versao);
        montagem.AcoesAdministrativas.Should().HaveCount(auditorias);
    }

    [Fact]
    public void Mutadores_da_publicacao_discord_nao_devem_ser_publicos()
    {
        var mutadores = new[]
        {
            "RegistrarPublicada",
            "RegistrarFalha",
            "IniciarTentativa",
            "MarcarRequerReconciliacao",
            "SolicitarRepublicacao",
        };

        var metodosPublicos = typeof(DraftMontagemPublicacaoDiscord)
            .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name);

        metodosPublicos.Should().NotIntersectWith(mutadores);
    }

    [Fact]
    public void Deve_usar_relogio_explicito_ao_criar_primeira_solicitacao_de_republicacao()
    {
        var montagem = NovaMontagem();
        var agora = new DateTimeOffset(2026, 7, 21, 13, 0, 0, TimeSpan.Zero);

        montagem.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, Guid.NewGuid(), "mensagem ausente", agora);

        montagem.PublicacoesDiscord.Should().ContainSingle().Which.UltimaTentativaEm.Should().Be(agora);
        montagem.DataAtualizacao.Should().Be(agora);
    }

    private static void Publicar(DraftMontagem montagem, DraftMontagemPublicacaoDiscordTipo tipo, DateTimeOffset agora)
    {
        var claimId = Guid.NewGuid();
        montagem.IniciarTentativaPublicacaoDiscord(tipo, "guild", "canal", claimId, agora.AddMinutes(5), agora);
        montagem.RegistrarPublicacaoDiscord(tipo, claimId, "guild", "canal", "mensagem", agora.AddMinutes(1));
    }

    private static void Falhar(DraftMontagem montagem, DraftMontagemPublicacaoDiscordTipo tipo, DateTimeOffset agora)
    {
        var claimId = Guid.NewGuid();
        montagem.IniciarTentativaPublicacaoDiscord(tipo, "guild", "canal", claimId, agora.AddMinutes(5), agora);
        montagem.RegistrarFalhaPublicacaoDiscord(tipo, claimId, "guild", "canal", "Timeout", agora.AddMinutes(1));
    }

    private static DraftMontagem NovaMontagem()
    {
        return new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
    }
}
