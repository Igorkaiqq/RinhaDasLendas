using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.AgendamentosPresenca;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Models;

namespace RinhaDasLendas.Tests.AgendamentosPresenca;

public sealed class AgendamentoPresencaHandlersTests
{
    private static readonly Guid Responsavel = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Agora = new(2026, 7, 24, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 7, 24);
    private readonly Mock<IAgendamentoPresencaRepository> _repository = new();
    private readonly Mock<IValidator<SaveAgendamentoPresencaRequestDto>> _validator = new();
    private readonly IAgendamentoPresencaTimeZone _timeZone = new TestTimeZone();
    private readonly ISystemClock _clock = new TestClock(Agora);
    private AgendamentoPresenca? _addedAgenda;

    public AgendamentoPresencaHandlersTests()
    {
        _validator.Setup(item => item.ValidateAsync(It.IsAny<SaveAgendamentoPresencaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repository.Setup(item => item.AddAsync(It.IsAny<AgendamentoPresenca>(), It.IsAny<CancellationToken>()))
            .Callback<AgendamentoPresenca, CancellationToken>((agenda, _) => _addedAgenda = agenda)
            .Returns(Task.CompletedTask);
        _repository.Setup(item => item.GetSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _addedAgenda?.Id == id
                ? new AgendamentoPresencaListItem(_addedAgenda, null)
                : null);
        _repository.Setup(item => item.GetLatestOccurrenceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OcorrenciaAgendamentoPresenca?)null);
    }

    [Theory]
    [InlineData("17:59", "2026-07-23")]
    [InlineData("18:00", "2026-07-23")]
    [InlineData("18:01", "2026-07-24")]
    public async Task Create_ShouldInitializeMarkerAtApprovedTemporalBoundary(string localTime, string expectedDate)
    {
        var now = new DateTimeOffset(DateOnly.Parse("2026-07-24").ToDateTime(TimeOnly.Parse(localTime)), TimeSpan.FromHours(-3));
        var handler = new CreateAgendamentoPresencaCommandHandler(
            _repository.Object, _validator.Object, new TestTimeZone(), new TestClock(now));

        var result = await handler.Handle(new CreateAgendamentoPresencaCommand(Valid(), Responsavel), CancellationToken.None);

        result.Should().NotBeNull();
        _repository.Verify(item => item.AddAsync(
            It.Is<AgendamentoPresenca>(agenda => agenda.UltimaDataAvaliada == DateOnly.Parse(expectedDate)
                && agenda.CriadoPorUsuarioId == Responsavel
                && agenda.Historicos.Single().ResponsavelUsuarioId == Responsavel),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePauseReactivateAndArchive_ShouldUseTrustedCommandAuthorAndPersist()
    {
        var agenda = CreateAgenda();
        SetupSummary(agenda);
        _repository.SetupSequence(item => item.GetByIdAsync(agenda.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agenda)
            .ReturnsAsync(agenda)
            .ReturnsAsync(agenda)
            .ReturnsAsync(agenda);

        var updated = await new UpdateAgendamentoPresencaCommandHandler(_repository.Object, _validator.Object, _clock)
            .Handle(new UpdateAgendamentoPresencaCommand(agenda.Id, Valid() with { Nome = "Agenda editada" }, Responsavel), CancellationToken.None);
        var paused = await new PausarAgendamentoPresencaCommandHandler(_repository.Object, _clock)
            .Handle(new PausarAgendamentoPresencaCommand(agenda.Id, Responsavel), CancellationToken.None);
        var reactivated = await new ReativarAgendamentoPresencaCommandHandler(_repository.Object, _timeZone, _clock)
            .Handle(new ReativarAgendamentoPresencaCommand(agenda.Id, Responsavel), CancellationToken.None);
        var archived = await new ArquivarAgendamentoPresencaCommandHandler(_repository.Object, _clock)
            .Handle(new ArquivarAgendamentoPresencaCommand(agenda.Id, Responsavel), CancellationToken.None);

        updated!.Nome.Should().Be("Agenda editada");
        paused!.Status.Should().Be(AgendamentoPresencaStatus.Pausado);
        reactivated!.Status.Should().Be(AgendamentoPresencaStatus.Ativo);
        archived.Should().BeTrue();
        agenda.Historicos.Skip(1).Should().OnlyContain(item => item.ResponsavelUsuarioId == Responsavel);
        _repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task Reactivate_ShouldNeverMoveMarkerBackwards()
    {
        var agenda = CreateAgenda(ultimaDataAvaliada: Hoje.AddDays(2));
        SetupSummary(agenda);
        agenda.Pausar(Responsavel, Agora.AddMinutes(-1));
        _repository.Setup(item => item.GetByIdAsync(agenda.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(agenda);

        await new ReativarAgendamentoPresencaCommandHandler(_repository.Object, _timeZone, _clock)
            .Handle(new ReativarAgendamentoPresencaCommand(agenda.Id, Responsavel), CancellationToken.None);

        agenda.UltimaDataAvaliada.Should().Be(Hoje.AddDays(2));
        agenda.AtivadoEm.Should().Be(Agora);
    }

    [Theory]
    [InlineData("17:59", "2026-07-23")]
    [InlineData("18:00", "2026-07-23")]
    [InlineData("18:01", "2026-07-24")]
    public async Task Reactivate_ShouldApplyApprovedTemporalBoundary(string localTime, string expectedDate)
    {
        var now = new DateTimeOffset(DateOnly.Parse("2026-07-24").ToDateTime(TimeOnly.Parse(localTime)), TimeSpan.FromHours(-3));
        var agenda = CreateAgenda(ultimaDataAvaliada: Hoje.AddDays(-2));
        SetupSummary(agenda);
        agenda.Pausar(Responsavel, now.AddMinutes(-1));
        _repository.Setup(item => item.GetByIdAsync(agenda.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(agenda);

        await new ReativarAgendamentoPresencaCommandHandler(_repository.Object, _timeZone, new TestClock(now))
            .Handle(new ReativarAgendamentoPresencaCommand(agenda.Id, Responsavel), CancellationToken.None);

        agenda.UltimaDataAvaliada.Should().Be(DateOnly.Parse(expectedDate));
        agenda.AtivadoEm.Should().Be(now);
    }

    [Fact]
    public async Task Mutations_ShouldReturnMissingForAbsentSchedule()
    {
        _repository.Setup(item => item.GetByIdAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgendamentoPresenca?)null);
        var id = Guid.NewGuid();

        var update = await new UpdateAgendamentoPresencaCommandHandler(_repository.Object, _validator.Object, _clock)
            .Handle(new UpdateAgendamentoPresencaCommand(id, Valid(), Responsavel), CancellationToken.None);
        var pause = await new PausarAgendamentoPresencaCommandHandler(_repository.Object, _clock)
            .Handle(new PausarAgendamentoPresencaCommand(id, Responsavel), CancellationToken.None);
        var reactivate = await new ReativarAgendamentoPresencaCommandHandler(_repository.Object, _timeZone, _clock)
            .Handle(new ReativarAgendamentoPresencaCommand(id, Responsavel), CancellationToken.None);
        var archive = await new ArquivarAgendamentoPresencaCommandHandler(_repository.Object, _clock)
            .Handle(new ArquivarAgendamentoPresencaCommand(id, Responsavel), CancellationToken.None);

        update.Should().BeNull();
        pause.Should().BeNull();
        reactivate.Should().BeNull();
        archive.Should().BeFalse();
        _repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_ShouldIncludePausedAndReturnStablePaginationMetadata()
    {
        var first = CreateAgenda("Agenda A");
        var paused = CreateAgenda("Agenda B");
        paused.Pausar(Responsavel, Agora);
        _repository.Setup(item => item.CountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _repository.Setup(item => item.ListAsync(true, 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoPresencaListItem(first, new DateTimeOffset(2026, 7, 31, 21, 0, 0, TimeSpan.Zero)),
                new AgendamentoPresencaListItem(paused, null),
            ]);
        _repository.Setup(item => item.ListLatestOccurrencesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, OcorrenciaAgendamentoPresenca>());

        var result = await new ListAgendamentosPresencaQueryHandler(_repository.Object)
            .Handle(new ListAgendamentosPresencaQuery(2, 2), CancellationToken.None);

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalItems.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.Items.Select(item => item.Id).Should().Equal(first.Id, paused.Id);
        result.Items.Last().ProximaExecucaoEm.Should().BeNull();
        _repository.Verify(item => item.ListLatestOccurrencesAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(item => item.ListOccurrencesAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Detail_ShouldReturnSafeSummaryAndLocalizedNextExecution()
    {
        var agenda = CreateAgenda(dias: [DiaSemanaIso.Sexta]);
        _repository.Setup(item => item.GetSummaryAsync(agenda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaListItem(
                agenda,
                new DateTimeOffset(2026, 7, 31, 21, 0, 0, TimeSpan.Zero)));
        _repository.Setup(item => item.GetLatestOccurrenceAsync(agenda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OcorrenciaAgendamentoPresenca?)null);

        var result = await new GetAgendamentoPresencaQueryHandler(_repository.Object)
            .Handle(new GetAgendamentoPresencaQuery(agenda.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProximaExecucaoEm.Should().Be(new DateTimeOffset(2026, 7, 31, 21, 0, 0, TimeSpan.Zero));
        typeof(AgendamentoPresencaSummaryDto).GetProperties().Select(item => item.Name).Should().NotContain([
            "ClaimId", "ClaimExpiresAt", "CriadoPorUsuarioId", "ResponsavelUsuarioId", "DiscordGuildId", "ChannelId", "Token"]);
    }

    [Fact]
    public async Task Occurrences_ShouldCheckScheduleAndReturnPagedSafeProjection()
    {
        var agenda = CreateAgenda();
        var occurrence = OcorrenciaAgendamentoPresenca.Bloqueada(
            agenda.Id, Hoje, Agora.AddHours(-1), Agora.AddHours(1), "MV098", Agora);
        _repository.Setup(item => item.ExistsAsync(agenda.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(item => item.CountOccurrencesAsync(agenda.Id, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _repository.Setup(item => item.ListOccurrencesAsync(agenda.Id, 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([occurrence]);

        var result = await new ListOcorrenciasAgendamentoPresencaQueryHandler(_repository.Object)
            .Handle(new ListOcorrenciasAgendamentoPresencaQuery(agenda.Id, 2, 2), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalItems.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.Items.Single().MessageCode.Should().Be("MV098");
        typeof(OcorrenciaAgendamentoPresencaSummaryDto).GetProperties().Select(item => item.Name).Should().NotContain([
            "ClaimId", "ClaimExpiresAt", "UltimaTentativaEm", "DiscordGuildId", "ChannelId", "MessageId", "Token"]);
        _repository.Verify(item => item.ExistsAsync(agenda.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(item => item.GetByIdAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SaveAgendamentoPresencaRequestDto Valid() => new(
        "Agenda semanal", null, [DiaSemanaIso.Sexta], new TimeOnly(18, 0), new TimeOnly(20, 0));

    private static AgendamentoPresenca CreateAgenda(
        string nome = "Agenda semanal",
        DateOnly? ultimaDataAvaliada = null,
        IReadOnlyCollection<DiaSemanaIso>? dias = null) => new(
            nome, null, new TimeOnly(18, 0), new TimeOnly(20, 0), dias ?? [DiaSemanaIso.Sexta],
            ultimaDataAvaliada ?? Hoje, Responsavel, Agora.AddDays(-1));

    private void SetupSummary(AgendamentoPresenca agenda)
    {
        _repository.Setup(item => item.GetSummaryAsync(agenda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaListItem(agenda, null));
    }

    private sealed class TestClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class TestTimeZone : IAgendamentoPresencaTimeZone
    {
        public DateOnly GetLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(TimeSpan.FromHours(-3)).DateTime);

        public DateTimeOffset ToUtc(DateOnly date, TimeOnly time) => new(date.ToDateTime(time).AddHours(3), TimeSpan.Zero);
    }
}
