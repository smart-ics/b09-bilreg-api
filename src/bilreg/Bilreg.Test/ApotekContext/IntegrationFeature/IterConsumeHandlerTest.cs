using System.Text.Json;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class IterConsumeHandlerTest
{
    private readonly InMemoryResepKerjaRepo _resep = new();
    private readonly InMemorySalesOrderRepo _salesOrder = new();
    private readonly FakeIterConsumePort _iter = new();

    [Fact]
    public void Handler_deserializes_enqueue_payload_and_calls_port()
    {
        var payload = JsonSerializer.Serialize(new { SourceResepId = "RS-ITER-1", ConsumeCount = 1 });
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.IterConsume,
            AptIntegrationSourceKindEnum.SalesOrder,
            "ASO000000001",
            "ASO000000001:ITER",
            AptIntegrationDestinationEnum.ResepIter,
            payload);

        var result = CreateHandler().Handle(task);

        result.Success.Should().BeTrue();
        _iter.LastSourceResepId.Should().Be("RS-ITER-1");
        _iter.LastConsumeCount.Should().Be(1);
    }

    [Fact]
    public void ProcessOne_fail_closed_adapter_marks_task_failed()
    {
        var task = CreateTaskWithSalesOrder("RS-FAIL");
        var repo = new Mock<IAptIntegrationTaskRepo>();
        SetupLoad(repo, task);
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var worker = new AptIntegrationWorker(
            repo.Object,
            [new IterConsumeHandler(new FailClosedIterConsumePort(), _resep, _salesOrder)]);

        var result = worker.ProcessOne(task.IntegrationTaskId);

        result.Success.Should().BeFalse();
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Failed);
        task.RetryCount.Should().Be(1);
        task.LastError.Should().Contain("not configured");
    }

    [Fact]
    public void ProcessOne_success_updates_visible_iter_consumed_copy()
    {
        var resep = SeedResepWithIterEntitled(3);
        var order = SeedSalesOrder(resep.ResepKerjaId);
        var task = CreateTaskWithSource(order.SalesOrderId, resep.SourceResepId);

        var repo = new Mock<IAptIntegrationTaskRepo>();
        SetupLoad(repo, task);
        repo.Setup(x => x.ClaimPending(It.IsAny<IAptIntegrationTaskKey>())).Returns(true);

        var worker = new AptIntegrationWorker(
            repo.Object,
            [new IterConsumeHandler(_iter, _resep, _salesOrder)]);

        var result = worker.ProcessOne(task.IntegrationTaskId);

        result.Success.Should().BeTrue();
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Succeeded);
        _resep.Store[resep.ResepKerjaId].IterConsumed.Should().Be(1);
    }

    private IterConsumeHandler CreateHandler() => new(_iter, _resep, _salesOrder);

    private AptIntegrationTaskModel CreateTaskWithSalesOrder(string sourceResepId)
    {
        var resep = SeedResepWithIterEntitled(1);
        var order = SeedSalesOrder(resep.ResepKerjaId);
        return CreateTaskWithSource(order.SalesOrderId, sourceResepId);
    }

    private AptIntegrationTaskModel CreateTaskWithSource(string salesOrderId, string sourceResepId)
    {
        return AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.IterConsume,
            AptIntegrationSourceKindEnum.SalesOrder,
            salesOrderId,
            $"{salesOrderId}:ITER",
            AptIntegrationDestinationEnum.ResepIter,
            JsonSerializer.Serialize(new { SourceResepId = sourceResepId, ConsumeCount = 1 }));
    }

    private ResepKerjaModel SeedResepWithIterEntitled(int iterEntitled)
    {
        var resep = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-ITER-" + Guid.NewGuid().ToString("N")[..6],
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01", 0, iterEntitled,
            [new ResepKerjaItemModel(1, 1, "A", "A", "TAB", "Tab", 10, 0, "3x1", "", "", false)],
            [],
            AuditTrailType.Create("u", DateTime.Now));
        _resep.SaveChanges(resep);
        return resep;
    }

    private SalesOrderModel SeedSalesOrder(string resepKerjaId)
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja,
            resepKerjaId,
            "T1",
            "R1",
            "P1",
            "Pasien",
            PayerPathEnum.GeneralPatientPay,
            PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "A", "A", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        _salesOrder.SaveChanges(order);
        return order;
    }

    private static void SetupLoad(Mock<IAptIntegrationTaskRepo> repo, AptIntegrationTaskModel task)
    {
        repo.Setup(x => x.LoadEntity(It.IsAny<IAptIntegrationTaskKey>()))
            .Returns((IAptIntegrationTaskKey key) =>
                key.IntegrationTaskId == task.IntegrationTaskId
                    ? MayBe.From(task)
                    : MayBe<AptIntegrationTaskModel>.None);
    }

    private sealed class FakeIterConsumePort : IIterConsumePort
    {
        public string LastSourceResepId { get; private set; } = "";
        public int LastConsumeCount { get; private set; }

        public string Consume(string sourceResepId, int consumeCount)
        {
            LastSourceResepId = sourceResepId;
            LastConsumeCount = consumeCount;
            return $"ITER-{sourceResepId}";
        }
    }
}
