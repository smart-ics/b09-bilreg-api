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
}
