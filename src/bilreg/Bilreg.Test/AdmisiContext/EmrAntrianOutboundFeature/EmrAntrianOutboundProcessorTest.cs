using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundProcessorTest
{
    private readonly Mock<IEmrAntrianOutboundQueueRepo> _queueRepo = new();
    private readonly Mock<IEmrAntrianOutboundIntegration> _integration = new();
    private readonly EmrAntrianOutboundProcessor _sut;

    public EmrAntrianOutboundProcessorTest()
    {
        _sut = new EmrAntrianOutboundProcessor(_queueRepo.Object, _integration.Object);
    }

    [Fact]
    public void ProcessOne_WhenSendSucceeds_ThenMarksSucceeded()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "BOK000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking,
            EmrAntrianOutboundPayloadBuilder.SerializeBooking(new AddAntrianEmrByBookingCmd(
                "BOK000000001", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 7)));

        SetupLoad(queue);
        _integration
            .Setup(x => x.Send(queue.MessageType, queue.PayloadJson))
            .Returns(new EmrAntrianSendResult(true, null));

        EmrAntrianOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()))
            .Callback<EmrAntrianOutboundQueueModel>(q => savedQueue = q);

        var result = _sut.ProcessOne(queue.QueueId, "worker");

        result.Success.Should().BeTrue();
        savedQueue!.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Succeeded);
    }

    [Fact]
    public void ProcessOne_WhenSendFails_ThenMarksFailed()
    {
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            "REG000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            EmrAntrianOutboundPayloadBuilder.SerializeReg(new AddAntrianEmrByRegCommand(
                "REG000000001", "-", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 7)));

        SetupLoad(queue);
        _integration
            .Setup(x => x.Send(queue.MessageType, queue.PayloadJson))
            .Returns(new EmrAntrianSendResult(false, "EMR addReg failed"));

        EmrAntrianOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()))
            .Callback<EmrAntrianOutboundQueueModel>(q => savedQueue = q);

        var result = _sut.ProcessOne(queue.QueueId, "worker");

        result.Success.Should().BeFalse();
        savedQueue!.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Failed);
        savedQueue.LastError.Should().Contain("EMR addReg failed");
    }

    [Fact]
    public void ProcessOne_WhenAlreadySucceeded_ThenSkipsSend()
    {
        var queue = EmrAntrianOutboundQueueModel.Rehydrate(
            "EAQ000000001",
            "BOK000000001",
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking,
            "{}",
            EmrAntrianOutboundQueueStatusEnum.Succeeded,
            0,
            new DateTime(3000, 1, 1),
            new DateTime(2026, 7, 21, 10, 0, 0),
            "",
            new DateTime(2026, 7, 21, 9, 0, 0));

        SetupLoad(queue);

        var result = _sut.ProcessOne(queue.QueueId, "worker");

        result.Success.Should().BeTrue();
        _integration.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _queueRepo.Verify(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()), Times.Never);
    }

    private void SetupLoad(EmrAntrianOutboundQueueModel queue)
    {
        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<IEmrAntrianOutboundQueueKey>()))
            .Returns((IEmrAntrianOutboundQueueKey key) =>
                key.QueueId == queue.QueueId
                    ? MayBe.From(queue)
                    : MayBe<EmrAntrianOutboundQueueModel>.None);
    }
}
