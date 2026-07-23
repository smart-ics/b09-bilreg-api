using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class BookingAssistanceIntakeTest
{
    [Fact]
    public async Task Fallback_CreatesOneEntryWithDerivedCorrelationAndLabel()
    {
        var f=Fixture(); f.Assistance.Setup(x=>x.FindActive("B1")).Returns((BookingAssistanceActive?)null);
        f.Assistance.Setup(x=>x.TryCreate("B1","BOOKING-ASSISTANCE:B1","FAIL","K1","u",
            TestTglJamProvider.Instance.Now,It.IsAny<AntrianModel>(),It.IsAny<AntrianEntryModel>())).Returns(true);
        var result=await f.Sut.Handle(new("B1","ADM","FAIL","K1","u"),default);
        result.Existing.Should().BeFalse();result.QueueLabel.Should().Be("A0007");f.Assistance.VerifyAll();
    }

    [Fact]
    public async Task Retry_ReturnsExistingWithoutAllocatingOrCreating()
    {
        var f=Fixture();f.Assistance.Setup(x=>x.FindActive("B1")).Returns(new BookingAssistanceActive("B1","Q",7,"A0007"));
        var result=await f.Sut.Handle(new("B1","ADM",null,"K1","u"),default);
        result.Existing.Should().BeTrue();result.QueueLabel.Should().Be("A0007");
        f.Sequencer.Verify(x=>x.GetNextNoUrut(It.IsAny<string>(),It.IsAny<int>()),Times.Never);
        f.Assistance.Verify(x=>x.TryCreate(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string?>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<AntrianModel>(),It.IsAny<AntrianEntryModel>()),Times.Never);
    }

    [Fact]
    public async Task ConcurrentLoser_ReloadsWinnerAndReturnsSameEntry()
    {
        var f=Fixture();f.Assistance.SetupSequence(x=>x.FindActive("B1"))
            .Returns((BookingAssistanceActive?)null).Returns(new BookingAssistanceActive("B1","WIN",8,"A0008"));
        f.Assistance.Setup(x=>x.TryCreate(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string?>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<AntrianModel>(),It.IsAny<AntrianEntryModel>())).Returns(false);
        var result=await f.Sut.Handle(new("B1","ADM",null,"K2","u"),default);
        result.Existing.Should().BeTrue();result.AntrianId.Should().Be("WIN");result.QueueLabel.Should().Be("A0008");
        result.Existing.Should().BeTrue("TryCreate=false must not claim a new assistance entry");
    }

    [Fact]
    public async Task InactiveServicePoint_RejectsBeforeAllocateOrCreate()
    {
        var bookings=new Mock<IBookingRepo>();
        bookings.Setup(x=>x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(BookingModel.Default));
        var points=new Mock<IAdmissionServicePointRepo>();
        var retired=AdmissionServicePointModel.Create("ADM","Admission","A").Retire();
        points.Setup(x=>x.LoadEntity(It.IsAny<IAdmissionServicePointKey>())).Returns(MayBe.From(retired));
        var queues=new Mock<IAntrianRepo>();
        var seq=new Mock<ISequencer>();
        var factory=new AntrianFactory(seq.Object);
        var assistance=new Mock<IBookingAssistanceRepo>();
        assistance.Setup(x=>x.FindActive("B1")).Returns((BookingAssistanceActive?)null);
        var sut=new BookingAssistanceIntakeHandler(bookings.Object,points.Object,queues.Object,
            factory,assistance.Object,TestTglJamProvider.Instance);

        var act=()=>sut.Handle(new("B1","ADM","FAIL","K1","u"),default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*retired*");
        seq.Verify(x=>x.GetNextNoUrut(It.IsAny<string>(),It.IsAny<int>()),Times.Never);
        assistance.Verify(x=>x.TryCreate(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string?>(),
            It.IsAny<string>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<AntrianModel>(),
            It.IsAny<AntrianEntryModel>()),Times.Never);
    }

    [Fact]
    public async Task SequenceExhausted_PropagatesWithoutCreatingAssistance()
    {
        var bookings=new Mock<IBookingRepo>();
        bookings.Setup(x=>x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(BookingModel.Default));
        var points=new Mock<IAdmissionServicePointRepo>();
        var point=AdmissionServicePointModel.Create("ADM","Admission","A");
        points.Setup(x=>x.LoadEntity(It.IsAny<IAdmissionServicePointKey>())).Returns(MayBe.From(point));
        var queues=new Mock<IAntrianRepo>();
        queues.Setup(x=>x.ListData(It.IsAny<DateOnly>())).Returns([]);
        var seq=new Mock<ISequencer>();
        seq.Setup(x=>x.GetNextNoUrut(It.IsAny<string>(),9999))
            .Throws(new SequenceExhaustedException("tag",9999));
        var factory=new AntrianFactory(seq.Object);
        var assistance=new Mock<IBookingAssistanceRepo>();
        assistance.Setup(x=>x.FindActive("B1")).Returns((BookingAssistanceActive?)null);
        var sut=new BookingAssistanceIntakeHandler(bookings.Object,points.Object,queues.Object,
            factory,assistance.Object,TestTglJamProvider.Instance);

        var act=()=>sut.Handle(new("B1","ADM","FAIL","K1","u"),default);

        await act.Should().ThrowAsync<SequenceExhaustedException>();
        assistance.Verify(x=>x.TryCreate(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string?>(),
            It.IsAny<string>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<AntrianModel>(),
            It.IsAny<AntrianEntryModel>()),Times.Never);
    }

    private static TestFixture Fixture()
    {
        var bookings=new Mock<IBookingRepo>();bookings.Setup(x=>x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(BookingModel.Default));
        var points=new Mock<IAdmissionServicePointRepo>();var point=AdmissionServicePointModel.Create("ADM","Admission","A");
        points.Setup(x=>x.LoadEntity(It.IsAny<IAdmissionServicePointKey>())).Returns(MayBe.From(point));
        var queues=new Mock<IAntrianRepo>();queues.Setup(x=>x.ListData(It.IsAny<DateOnly>())).Returns([]);
        var seq=new Mock<ISequencer>();seq.Setup(x=>x.GetNextNoUrut(It.IsAny<string>(),9999)).Returns(7);
        var factory=new AntrianFactory(seq.Object);var assistance=new Mock<IBookingAssistanceRepo>();
        return new(new(bookings.Object,points.Object,queues.Object,factory,assistance.Object,TestTglJamProvider.Instance),assistance,seq);
    }
    private sealed record TestFixture(BookingAssistanceIntakeHandler Sut,Mock<IBookingAssistanceRepo> Assistance,Mock<ISequencer> Sequencer);
}
