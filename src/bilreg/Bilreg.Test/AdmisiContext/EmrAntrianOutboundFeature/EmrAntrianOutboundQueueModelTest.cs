using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundQueueModelTest
{
    [Fact]
    public void CreatePending_ThenMarkSucceeded_SetsStatusAndClearsError()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "BOK000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking,
            "{\"bookingId\":\"BOK000000001\"}");

        queue.SourceId.Should().Be("BOK000000001");
        queue.MessageType.Should().Be(EmrAntrianOutboundQueueModel.MessageTypeAddBooking);
        queue.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);

        queue.MarkProcessing();
        queue.MarkSucceeded(new DateTime(2026, 7, 21, 10, 0, 0));

        queue.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Succeeded);
        queue.LastError.Should().BeEmpty();
    }

    [Fact]
    public void CreatePending_ThenMarkFailed_TruncatesErrorAndIncrementsRetry()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{}");

        queue.MarkProcessing();
        queue.MarkFailed(new string('x', 250), new DateTime(2026, 7, 21, 10, 0, 0));

        queue.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Failed);
        queue.RetryCount.Should().Be(1);
        queue.LastError.Should().HaveLength(200);
    }

    [Fact]
    public void AssertCanManualRetry_WhenPending_Throws()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{}");

        var act = () => queue.AssertCanManualRetry();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RefreshPayload_WhenPending_UpdatesPayloadAndKeepsPending()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");

        queue.RefreshPayload("{\"new\":true}");

        queue.PayloadJson.Should().Be("{\"new\":true}");
        queue.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
        queue.LastError.Should().BeEmpty();
    }

    [Fact]
    public void RefreshPayload_WhenFailed_ResetsToPendingAndClearsError()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");
        queue.MarkProcessing();
        queue.MarkFailed("timeout", new DateTime(2026, 7, 21, 10, 0, 0));

        queue.RefreshPayload("{\"new\":true}");

        queue.PayloadJson.Should().Be("{\"new\":true}");
        queue.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
        queue.LastError.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmrAntrianOutboundQueueStatusEnum.Processing)]
    [InlineData(EmrAntrianOutboundQueueStatusEnum.Succeeded)]
    public void RefreshPayload_WhenProcessingOrSucceeded_Throws(EmrAntrianOutboundQueueStatusEnum status)
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");
        queue.MarkProcessing();
        if (status == EmrAntrianOutboundQueueStatusEnum.Succeeded)
            queue.MarkSucceeded(new DateTime(2026, 7, 21, 10, 0, 0));

        var act = () => queue.RefreshPayload("{\"new\":true}");
        act.Should().Throw<InvalidOperationException>();
    }
}
