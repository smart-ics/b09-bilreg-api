using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class QueueCommandTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemoryJualBebasRepo _jb = new();
    private readonly InMemoryQueueMappingRepo _map = new();
    private readonly InMemoryQueueCloseRepo _close = new();
    private readonly InMemoryIntegrationTaskRepo _tasks = new();
    private readonly FakeTrackerPort _tracker = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Map_two_independent_demands_to_one_queue()
    {
        var resepId = SeedResep().ResepKerjaId;
        var jualBebasId = SeedJualBebas().JualBebasId;
        var mapHandler = CreateMapHandler();

        await mapHandler.Handle(new QueueMapCmd(
            "mapper", QueueDemandKindEnum.ResepKerja, resepId, "Q-SHARED", 3, "TRK1", QueueMappingMethodEnum.Manual), default);
        await mapHandler.Handle(new QueueMapCmd(
            "mapper", QueueDemandKindEnum.JualBebas, jualBebasId, "Q-SHARED", 3, "TRK1", QueueMappingMethodEnum.Tracker), default);

        _map.Store.Should().HaveCount(2);
        _map.ListByQueue("Q-SHARED", 3).Should().HaveCount(2);
        _map.ListByQueue("Q-SHARED", 3).Select(x => x.DemandId).Should().BeEquivalentTo([resepId, jualBebasId]);
    }

    [Fact]
    public async Task Map_correction_updates_existing_row_in_place()
    {
        var resepId = SeedResep().ResepKerjaId;
        var mapHandler = CreateMapHandler();

        await mapHandler.Handle(new QueueMapCmd(
            "mapper-a", QueueDemandKindEnum.ResepKerja, resepId, "Q-OLD", 1, "TRK-OLD", QueueMappingMethodEnum.Manual), default);
        await mapHandler.Handle(new QueueMapCmd(
            "mapper-b", QueueDemandKindEnum.ResepKerja, resepId, "Q-NEW", 2, "TRK-NEW", QueueMappingMethodEnum.Manual), default);

        _map.Store.Should().HaveCount(1);
        var stored = _map.LoadEntity(QueueMappingModel.Key(QueueDemandKindEnum.ResepKerja, resepId)).Value;
        stored.AntrianId.Should().Be("Q-NEW");
        stored.NoUrut.Should().Be(2);
        stored.PasienTrackerId.Should().Be("TRK-NEW");
        stored.MappedBy.Should().Be("mapper-b");
    }

    [Fact]
    public async Task Map_does_not_invoke_tracker_port()
    {
        var resepId = SeedResep().ResepKerjaId;
        _tracker.Status = AntrianStatusEnum.Waiting;

        await CreateMapHandler().Handle(new QueueMapCmd(
            "mapper", QueueDemandKindEnum.ResepKerja, resepId, "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        _tracker.ServeCount.Should().Be(0);
        _tracker.DoneCount.Should().Be(0);
        _tracker.Status.Should().Be(AntrianStatusEnum.Waiting);
    }

    [Fact]
    public async Task Map_rejects_unknown_demand_without_creating_mapping()
    {
        var act = () => CreateMapHandler().Handle(new QueueMapCmd(
            "mapper", QueueDemandKindEnum.ResepKerja, "RK-MISSING", "Q1", 1, "TRK1", QueueMappingMethodEnum.Manual), default);

        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
        _map.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Close_from_waiting_persists_fact_and_enqueues_tracker_withdrawn()
    {
        _tracker.Status = AntrianStatusEnum.Waiting;

        var response = await CreateCloseHandler().Handle(new QueueCloseCmd("closer", "Q1", 1, "patient left"), default);

        response.QueueCloseId.Should().StartWith(QueueCloseModel.IdPrefix);
        _close.Store.Should().ContainSingle();
        _close.Store.Values.Single().Reason.Should().Be("patient left");
        _close.Store.Values.Single().StaffId.Should().Be("closer");

        _tasks.Store.Should().ContainSingle();
        var task = _tasks.Store.Values.Single();
        task.TaskType.Should().Be(AptIntegrationTaskTypeEnum.TrackerWithdrawn);
        task.SourceKind.Should().Be(AptIntegrationSourceKindEnum.QueueClose);
        task.SourceId.Should().Be(response.QueueCloseId);
        task.IdempotencyKey.Should().Be($"{response.QueueCloseId}:WDN");
        task.Destination.Should().Be(AptIntegrationDestinationEnum.Tracker);
        task.PayloadJson.Should().Contain("patient left");
    }

    [Fact]
    public async Task Close_rejects_in_service_without_persisting_fact_or_task()
    {
        _tracker.Status = AntrianStatusEnum.InService;

        var act = () => CreateCloseHandler().Handle(new QueueCloseCmd("closer", "Q1", 1, "no show"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only from Waiting*");
        _close.Store.Should().BeEmpty();
        _tasks.Store.Should().BeEmpty();
    }

    [Fact]
    public async Task Close_rejects_blank_reason_and_already_closed_queue()
    {
        _tracker.Status = AntrianStatusEnum.Waiting;

        var blankReason = () => CreateCloseHandler().Handle(new QueueCloseCmd("closer", "Q1", 1, "  "), default);
        await blankReason.Should().ThrowAsync<ArgumentException>().WithParameterName("Reason");

        await CreateCloseHandler().Handle(new QueueCloseCmd("closer", "Q1", 1, "left"), default);

        var secondClose = () => CreateCloseHandler().Handle(new QueueCloseCmd("closer", "Q1", 1, "duplicate"), default);
        await secondClose.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already closed*");
        _tasks.Store.Should().ContainSingle();
    }

    private QueueMapHandler CreateMapHandler()
        => new(_map, _resep, _jb, _auth);

    private QueueCloseHandler CreateCloseHandler()
        => new(_close, _tracker, _tasks, _auth);

    private ResepKerjaModel SeedResep()
    {
        var resep = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-" + Guid.NewGuid().ToString("N")[..6],
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 0, [new ResepKerjaItemModel(1, 1, "A", "A", "TAB", "Tab", 10m, 0, "3x1", "", "", false)],
            [], AuditTrailType.Create("u", DateTime.Now));
        _resep.SaveChanges(resep);
        return resep;
    }

    private JualBebasModel SeedJualBebas()
    {
        var model = JualBebasModel.Accept(
            "R1", "P1", "Pasien", "acceptor", DateTime.Now,
            [new JualBebasItemModel(1, "A", "A", "TAB", 2, "3x1")]);
        _jb.SaveChanges(model);
        return model;
    }
}
