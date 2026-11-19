using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundEnqueueServiceTest
{
    private readonly Mock<IEmrAntrianOutboundQueueRepo> _queueRepo = new();
    private readonly EmrAntrianOutboundEnqueueService _sut;

    public EmrAntrianOutboundEnqueueServiceTest()
    {
        _sut = new EmrAntrianOutboundEnqueueService(_queueRepo.Object);
    }

    [Fact]
    public void TryEnqueueAddBooking_WhenNoActiveRow_ThenInsertsPending()
    {
        _queueRepo
            .Setup(x => x.FindActiveBySource("BOK1", EmrAntrianOutboundQueueModel.MessageTypeAddBooking))
            .Returns(MayBe<EmrAntrianOutboundQueueModel>.None);

        EmrAntrianOutboundQueueModel? saved = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()))
            .Callback<EmrAntrianOutboundQueueModel>(q => saved = q);

        var cmd = new AddAntrianEmrByBookingCmd("BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3);
        var queueId = _sut.TryEnqueueAddBooking(cmd, new DateTime(2026, 7, 21, 9, 0, 0));

        queueId.Should().NotBeNullOrWhiteSpace();
        saved!.SourceId.Should().Be("BOK1");
        saved.MessageType.Should().Be(EmrAntrianOutboundQueueModel.MessageTypeAddBooking);
        saved.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
    }

    [Fact]
    public void TryEnqueueAddBooking_WhenActiveRowExists_ThenSkips()
    {
        var existing = EmrAntrianOutboundQueueModel.CreatePending(
            "BOK1",
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking,
            "{}");
        _queueRepo
            .Setup(x => x.FindActiveBySource("BOK1", EmrAntrianOutboundQueueModel.MessageTypeAddBooking))
            .Returns(MayBe.From(existing));

        var cmd = new AddAntrianEmrByBookingCmd("BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3);
        var queueId = _sut.TryEnqueueAddBooking(cmd, DateTime.Now);

        queueId.Should().BeNull();
        _queueRepo.Verify(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()), Times.Never);
    }

    [Fact]
    public void EnqueueOrUpdateAddReg_WhenPendingExists_UpdatesPayloadInPlace()
    {
        var existing = EmrAntrianOutboundQueueModel.CreatePending(
            "REG1",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");
        _queueRepo
            .Setup(x => x.FindPendingOrProcessingBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe.From(existing));

        var cmd = new AddAntrianEmrByRegCommand("REG1", "BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3, "A");
        var queueId = _sut.EnqueueOrUpdateAddReg(cmd, new DateTime(2026, 7, 21, 9, 0, 0));

        queueId.Should().Be(existing.QueueId);
        existing.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
        existing.PayloadJson.Should().Contain("REG1");
        _queueRepo.Verify(x => x.SaveChanges(existing), Times.Once);
    }

    [Fact]
    public void EnqueueOrUpdateAddReg_WhenProcessingExists_ReturnsExistingQueueIdWithoutSaving()
    {
        var existing = EmrAntrianOutboundQueueModel.CreatePending(
            "REG1",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");
        existing.MarkProcessing();
        _queueRepo
            .Setup(x => x.FindPendingOrProcessingBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe.From(existing));

        var cmd = new AddAntrianEmrByRegCommand("REG1", "BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3, "A");
        var queueId = _sut.EnqueueOrUpdateAddReg(cmd, new DateTime(2026, 7, 21, 9, 0, 0));

        queueId.Should().Be(existing.QueueId);
        _queueRepo.Verify(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()), Times.Never);
    }

    [Fact]
    public void EnqueueOrUpdateAddReg_WhenFailedExists_RefreshesPayloadAndResetsToPending()
    {
        var failed = EmrAntrianOutboundQueueModel.CreatePending(
            "REG1",
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            "{\"old\":true}");
        failed.MarkProcessing();
        failed.MarkFailed("timeout", new DateTime(2026, 7, 21, 8, 50, 0));

        _queueRepo
            .Setup(x => x.FindPendingOrProcessingBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe<EmrAntrianOutboundQueueModel>.None);
        _queueRepo
            .Setup(x => x.FindLatestFailedBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe.From(failed));

        var cmd = new AddAntrianEmrByRegCommand("REG1", "BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3, "A");
        var queueId = _sut.EnqueueOrUpdateAddReg(cmd, new DateTime(2026, 7, 21, 9, 0, 0));

        queueId.Should().Be(failed.QueueId);
        failed.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
        failed.LastError.Should().BeEmpty();
        failed.PayloadJson.Should().Contain("REG1");
        _queueRepo.Verify(x => x.SaveChanges(failed), Times.Once);
    }

    [Fact]
    public void EnqueueOrUpdateAddReg_WhenNoPendingProcessingOrFailed_InsertsNewPending()
    {
        _queueRepo
            .Setup(x => x.FindPendingOrProcessingBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe<EmrAntrianOutboundQueueModel>.None);
        _queueRepo
            .Setup(x => x.FindLatestFailedBySource("REG1", EmrAntrianOutboundQueueModel.MessageTypeAddReg))
            .Returns(MayBe<EmrAntrianOutboundQueueModel>.None);

        EmrAntrianOutboundQueueModel? saved = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<EmrAntrianOutboundQueueModel>()))
            .Callback<EmrAntrianOutboundQueueModel>(q => saved = q);

        var cmd = new AddAntrianEmrByRegCommand("REG1", "BOK1", "PAS1", "ANI", "LY1", "DR1", "2026-07-21", "08:00", 3, "A");
        var queueId = _sut.EnqueueOrUpdateAddReg(cmd, new DateTime(2026, 7, 21, 9, 0, 0));

        queueId.Should().NotBeNullOrWhiteSpace();
        saved.Should().NotBeNull();
        saved!.QueueId.Should().Be(queueId);
        saved.SourceId.Should().Be("REG1");
        saved.MessageType.Should().Be(EmrAntrianOutboundQueueModel.MessageTypeAddReg);
        saved.QueueStatus.Should().Be(EmrAntrianOutboundQueueStatusEnum.Pending);
    }
}
