using Bilreg.Domain.LabContext.LabOwareFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareOutboundQueueModelTest
{
    [Fact]
    public void CreatePending_SetsInitialState()
    {
        var queue = LabOwareOutboundQueueModel.CreatePending("LBO000000001", "{\"orderNo\":\"LAB1\"}");

        queue.QueueId.Should().StartWith("LOQ");
        queue.OrderId.Should().Be("LBO000000001");
        queue.MessageType.Should().Be(LabOwareOutboundQueueModel.DefaultMessageType);
        queue.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Pending);
        queue.RetryCount.Should().Be(0);
        queue.LastError.Should().BeEmpty();
    }

    [Fact]
    public void MarkSucceeded_FromProcessing_UpdatesStatus()
    {
        var queue = LabOwareOutboundQueueModel.CreatePending("LBO000000001", "{}");
        queue.MarkProcessing();
        queue.MarkSucceeded();

        queue.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Succeeded);
        queue.LastError.Should().BeEmpty();
        queue.ProcessedDate.Year.Should().BeLessThan(3000);
    }

    [Fact]
    public void MarkFailed_FromProcessing_IncrementsRetryCount()
    {
        var queue = LabOwareOutboundQueueModel.CreatePending("LBO000000001", "{}");
        queue.MarkProcessing();
        queue.MarkFailed("timeout");

        queue.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Failed);
        queue.RetryCount.Should().Be(1);
        queue.LastError.Should().Be("timeout");
    }

    [Fact]
    public void AssertCanManualRetry_WhenPending_Throws()
    {
        var queue = LabOwareOutboundQueueModel.CreatePending("LBO000000001", "{}");
        var act = () => queue.AssertCanManualRetry();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkProcessing_FromFailed_Allowed()
    {
        var queue = LabOwareOutboundQueueModel.Rehydrate(
            "LOQ000000001",
            "LBO000000001",
            LabOwareOutboundQueueModel.DefaultMessageType,
            "{}",
            LabOwareQueueStatusEnum.Failed,
            1,
            DateTime.Now,
            DateTime.Now,
            "err",
            DateTime.Now);

        queue.MarkProcessing();
        queue.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Processing);
    }
}
