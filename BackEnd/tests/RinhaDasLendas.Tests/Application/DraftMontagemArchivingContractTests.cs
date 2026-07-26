using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Api.Controllers;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemArchivingContractTests
{
    [Fact]
    public void Application_DeveExporDtosEValidatorsDeArquivamento()
    {
        var assembly = typeof(RinhaDasLendas.Application.ApplicationAssemblyReference).Assembly;

        assembly.GetType("RinhaDasLendas.Application.Dtos.ArquivarDraftMontagemRequestDto").Should().NotBeNull();
        assembly.GetType("RinhaDasLendas.Application.Dtos.RestaurarDraftMontagemRequestDto").Should().NotBeNull();
        assembly.GetType("RinhaDasLendas.Application.Dtos.DraftMontagemArquivamentoDto").Should().NotBeNull();
        assembly.GetType("RinhaDasLendas.Application.Validators.ArquivarDraftMontagemValidator").Should().NotBeNull();
        assembly.GetType("RinhaDasLendas.Application.Validators.RestaurarDraftMontagemValidator").Should().NotBeNull();
    }

    [Fact]
    public void ContratosPublicos_DevemExporArquivadoEVersaoEstado()
    {
        var assembly = typeof(RinhaDasLendas.Application.ApplicationAssemblyReference).Assembly;
        foreach (var typeName in new[] { "DraftMontagemResponseDto", "DraftMontagemResumoDto", "DraftMontagemDiscordOperationalDto" })
        {
            var type = assembly.GetType($"RinhaDasLendas.Application.Dtos.{typeName}");
            type.Should().NotBeNull();
            type!.GetProperty("Arquivado").Should().NotBeNull();
            type.GetProperty("VersaoEstado").Should().NotBeNull();
        }
    }

    [Fact]
    public void CodigosDeMensagemEPermissao_DevemExistir()
    {
        typeof(MessageCodes).GetField("ArchiveReasonRequired")!.GetRawConstantValue().Should().Be("MV101");
        typeof(MessageCodes).GetField("ArchiveReasonMaxLength")!.GetRawConstantValue().Should().Be("MV102");
        typeof(MessageCodes).GetField("DraftStateConflict")!.GetRawConstantValue().Should().Be("MV103");
        typeof(MessageCodes).GetField("DraftStateVersionInvalid")!.GetRawConstantValue().Should().Be("MV104");
        typeof(AuthPermissions).GetField("CanArchiveDrafts")!.GetRawConstantValue().Should().Be("CanArchiveDrafts");
    }

    [Fact]
    public void ApplicationEApi_DevemExporFluxosCompletosDeArquivamento()
    {
        var application = typeof(RinhaDasLendas.Application.ApplicationAssemblyReference).Assembly;
        foreach (var typeName in new[]
        {
            "Commands.DraftMontagens.ArquivarDraftMontagemCommand",
            "Commands.DraftMontagens.RestaurarDraftMontagemCommand",
            "Commands.DraftMontagens.RepublicarCancelamentoDraftArquivadoCommand",
            "Queries.DraftMontagens.GetDraftMontagemArquivamentoQuery",
            "Handlers.DraftMontagens.ArquivarDraftMontagemCommandHandler",
            "Handlers.DraftMontagens.RestaurarDraftMontagemCommandHandler",
            "Handlers.DraftMontagens.RepublicarCancelamentoDraftArquivadoCommandHandler",
            "Handlers.DraftMontagens.GetDraftMontagemArquivamentoQueryHandler",
        })
        {
            application.GetType($"RinhaDasLendas.Application.{typeName}").Should().NotBeNull();
        }

        foreach (var action in new[] { "Archive", "Restore", "GetArchiving", "RepublishArchivedCancellation" })
        {
            typeof(DraftMontagensController).GetMethod(action).Should().NotBeNull();
        }
    }

    [Fact]
    public void RepositorioENotifier_DevemSepararAcessoNormalDoAdministrativo()
    {
        typeof(IDraftMontagemRepository).GetMethod("GetByIdIncludingArchivedAsync").Should().NotBeNull();
        typeof(IDraftMontagemRepository).GetMethod("ReloadByIdIncludingArchivedAsync").Should().NotBeNull();
        typeof(IDraftMontagemRepository).GetMethod("ListAsync")!.GetParameters().Should().Contain(parameter => parameter.Name == "includeArchived");
        typeof(IDraftMontagemRepository).GetMethod("CountAsync")!.GetParameters().Should().Contain(parameter => parameter.Name == "includeArchived");
        typeof(IDraftMontagemRealtimeNotifier).GetMethod("ArchivedAsync").Should().NotBeNull();
    }

    [Fact]
    public void ProjecaoDeModerador_NaoDeveExporAcoesDeArquivamento()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.Arquivar("motivo", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var dto = DraftMontagemAdminResponseDto.FromEntity(montagem);

        dto.AcoesAdministrativas.Should().NotContain(acao =>
            new[] { "Arquivamento", "Restauracao", "CancelamentoPorArquivamento" }.Contains(acao.Tipo));
    }

    [Fact]
    public void ProjecaoDeModerador_NaoDeveExporMotivoDeCancelamentoOriginadoPorArquivamentoAposRestauracao()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.Arquivar("motivo sigiloso", Guid.NewGuid(), DateTimeOffset.UtcNow);
        montagem.Restaurar(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

        var dto = DraftMontagemAdminResponseDto.FromEntity(montagem);

        dto.MotivoCancelamento.Should().BeNull();
    }

    [Fact]
    public void ProjecaoDeModerador_DevePreservarMotivoDeCancelamentoOperacional()
    {
        var montagem = new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.Cancelar("motivo operacional", Guid.NewGuid());

        DraftMontagemAdminResponseDto.FromEntity(montagem).MotivoCancelamento.Should().Be("motivo operacional");
    }
}
