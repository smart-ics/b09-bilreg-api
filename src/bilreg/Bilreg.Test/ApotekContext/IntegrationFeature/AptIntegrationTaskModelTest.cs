using Bilreg.Domain.ApotekContext.IntegrationFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskModelTest
{
    [Fact]
    public void CreatePending_SetsInitialState()
    {
        var task = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.TrackerServedAt,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP000000001",
            "ADP000000001:START",
            AptIntegrationDestinationEnum.Tracker,
            "{}");

        task.IntegrationTaskId.Should().StartWith("AIT");
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Pending);
        task.RetryCount.Should().Be(0);
        task.IdempotencyKey.Should().Be("ADP000000001:START");
        task.CorrelationId.Should().BeEmpty();
    }

    [Fact]
    public void ClaimPending_FromPending_MovesToProcessing()
    {
        var task = NewPending();
        task.ClaimPending();
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Processing);
    }

    [Fact]
    public void MarkSucceeded_FromProcessing_StoresCorrelation()
    {
        var task = NewPending();
        task.ClaimPending();
        task.MarkSucceeded("Q-1");
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Succeeded);
        task.CorrelationId.Should().Be("Q-1");
        task.LastError.Should().BeEmpty();
    }

    [Fact]
    public void MarkFailed_IncrementsRetry_ThenDeadAfterMax()
    {
        var task = AptIntegrationTaskModel.Rehydrate(
            "AIT000000001",
            AptIntegrationTaskTypeEnum.StockReserve,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP1",
            "ADP1:I1:RESERVE",
            AptIntegrationDestinationEnum.StockLedger,
            "{}",
            AptIntegrationTaskStatusEnum.Processing,
            AptIntegrationTaskModel.MaxRetries - 1,
            "",
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            "",
            DateTime.Now);

        task.MarkFailed("timeout");
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Dead);
        task.RetryCount.Should().Be(AptIntegrationTaskModel.MaxRetries);
        task.LastError.Should().Be("timeout");
    }

    [Fact]
    public void PrepareRetry_FromFailed_ReturnsToPending()
    {
        var task = NewPending();
        task.ClaimPending();
        task.MarkFailed("err");
        task.PrepareRetry();
        task.TaskStatus.Should().Be(AptIntegrationTaskStatusEnum.Pending);
    }

    [Fact]
    public void ClaimPending_FromSucceeded_Throws()
    {
        var task = NewPending();
        task.ClaimPending();
        task.MarkSucceeded("x");
        var act = () => task.ClaimPending();
        act.Should().Throw<InvalidOperationException>();
    }

    private static AptIntegrationTaskModel NewPending()
        => AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.BillingCharge,
            AptIntegrationSourceKindEnum.Invoice,
            "ASI000000001",
            "ASI000000001:CHARGE",
            AptIntegrationDestinationEnum.TataRekening,
            "{\"invoiceId\":\"ASI000000001\"}");
}
