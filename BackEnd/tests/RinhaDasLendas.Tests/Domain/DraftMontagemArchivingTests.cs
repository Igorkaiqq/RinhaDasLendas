using System.Reflection;
using FluentAssertions;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Domain;

public sealed class DraftMontagemArchivingTests
{
    [Fact]
    public void DraftMontagem_DeveExporContratoDeArquivamento()
    {
        var type = typeof(DraftMontagem);

        type.GetProperty("Arquivado").Should().NotBeNull();
        type.GetProperty("ArquivadoEm").Should().NotBeNull();
        type.GetProperty("ArquivadoPorUsuarioId").Should().NotBeNull();
        type.GetProperty("MotivoArquivamento").Should().NotBeNull();
        type.GetMethod("Arquivar", [typeof(string), typeof(Guid), typeof(DateTimeOffset)]).Should().NotBeNull();
        type.GetMethod("Restaurar", [typeof(Guid), typeof(DateTimeOffset)]).Should().NotBeNull();
    }

    [Fact]
    public void PublicacaoDiscord_DeveConterCancelamento()
    {
        Enum.GetNames<DraftMontagemPublicacaoDiscordTipo>().Should().Contain("Cancelamento");
    }

    [Fact]
    public void AcaoAdministrativa_DeveAceitarTimestampDeterministico()
    {
        typeof(DraftMontagemAcaoAdministrativa).GetConstructor([
            typeof(string), typeof(Guid), typeof(string), typeof(Guid?), typeof(DateTimeOffset)
        ]).Should().NotBeNull();
    }

    [Theory]
    [InlineData(DraftMontagemStatus.PresencaAberta, true)]
    [InlineData(DraftMontagemStatus.PresencaEncerrada, true)]
    [InlineData(DraftMontagemStatus.CapitaesDefinidos, true)]
    [InlineData(DraftMontagemStatus.OrdemDefinida, true)]
    [InlineData(DraftMontagemStatus.Aberta, true)]
    [InlineData(DraftMontagemStatus.Finalizada, false)]
    [InlineData(DraftMontagemStatus.Cancelada, false)]
    public void Arquivar_DevePreservarTerminalEOuCancelarAtivo(DraftMontagemStatus status, bool deveCancelar)
    {
        var montagem = CriarNoStatus(status);
        var agora = new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
        var responsavel = Guid.NewGuid();

        Invocar(montagem, "Arquivar", "  motivo administrativo  ", responsavel, agora);

        Obter<bool>(montagem, "Arquivado").Should().BeTrue();
        Obter<string>(montagem, "MotivoArquivamento").Should().Be("motivo administrativo");
        Obter<DateTimeOffset?>(montagem, "ArquivadoEm").Should().Be(agora);
        montagem.Status.Should().Be(deveCancelar ? DraftMontagemStatus.Cancelada : status);
        montagem.AcoesAdministrativas.Should().ContainSingle(acao => acao.Tipo == "Arquivamento");
        montagem.AcoesAdministrativas.Count(acao => acao.Tipo == "CancelamentoPorArquivamento").Should().Be(deveCancelar ? 1 : 0);
        montagem.AcoesAdministrativas.Where(acao => acao.Tipo is "Arquivamento" or "CancelamentoPorArquivamento")
            .Should().OnlyContain(acao => acao.Motivo == "motivo administrativo" && acao.RegistradoEm == agora);
        montagem.PublicacoesDiscord.Count(publicacao => publicacao.Tipo.ToString() == "Cancelamento").Should().Be(deveCancelar ? 1 : 0);
    }

    [Fact]
    public void ArquivarERestaurar_RepetidosDevemSerIdempotentesEPreservarHistorico()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var agora = new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
        var responsavel = Guid.NewGuid();

        Invocar(montagem, "Arquivar", "primeiro", responsavel, agora);
        var versaoArquivada = montagem.VersaoEstado;
        Invocar(montagem, "Arquivar", "segundo", Guid.NewGuid(), agora.AddMinutes(1));

        montagem.VersaoEstado.Should().Be(versaoArquivada);
        Obter<string>(montagem, "MotivoArquivamento").Should().Be("primeiro");
        montagem.AcoesAdministrativas.Should().HaveCount(2);

        Invocar(montagem, "Restaurar", responsavel, agora.AddMinutes(2));
        var versaoRestaurada = montagem.VersaoEstado;
        Invocar(montagem, "Restaurar", responsavel, agora.AddMinutes(3));

        Obter<bool>(montagem, "Arquivado").Should().BeFalse();
        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.VersaoEstado.Should().Be(versaoRestaurada);
        montagem.AcoesAdministrativas.Should().ContainSingle(acao => acao.Tipo == "Restauracao");
    }

    [Theory]
    [InlineData("   ", MessageCodes.ArchiveReasonRequired)]
    [InlineData(null, MessageCodes.ArchiveReasonRequired)]
    public void Arquivar_DeveRejeitarMotivoVazioSemAlterarEstado(string? motivo, string codigo)
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

        var act = () => montagem.Arquivar(motivo!, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(codigo);
        montagem.Arquivado.Should().BeFalse();
        montagem.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
    }

    [Fact]
    public void Arquivar_DeveRejeitarMotivoComMaisDe500CaracteresSemAlterarEstado()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

        var act = () => montagem.Arquivar(new string('x', 501), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.ArchiveReasonMaxLength);
        montagem.Arquivado.Should().BeFalse();
    }

    [Fact]
    public void ArquivarERestaurar_DevemPreservarColecoesDoDraft()
    {
        var jogadores = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(2).ToArray());
        var contagens = (montagem.Participantes.Count, montagem.Times.Count, montagem.Escolhas.Count, montagem.Substituicoes.Count);

        montagem.Arquivar("motivo", Guid.NewGuid(), DateTimeOffset.UtcNow);
        montagem.Restaurar(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

        (montagem.Participantes.Count, montagem.Times.Count, montagem.Escolhas.Count, montagem.Substituicoes.Count)
            .Should().Be(contagens);
    }

    private static DraftMontagem CriarNoStatus(DraftMontagemStatus status)
    {
        if (status == DraftMontagemStatus.PresencaAberta)
        {
            return new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        }

        var jogadores = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, jogadores, jogadores.Take(2).ToArray());
        if (status == DraftMontagemStatus.Finalizada)
        {
            montagem.Finalizar();
        }
        else if (status == DraftMontagemStatus.Cancelada)
        {
            montagem.Cancelar("cancelado");
        }
        else
        {
            typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.Status))!.SetValue(montagem, status);
        }

        return montagem;
    }

    private static void Invocar(object target, string method, params object[] arguments)
    {
        target.GetType().GetMethod(method, arguments.Select(argument => argument.GetType()).ToArray())
            .Should().NotBeNull().And.Subject!.Invoke(target, arguments);
    }

    private static T Obter<T>(object target, string property)
    {
        return (T)target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.GetValue(target)!;
    }
}
