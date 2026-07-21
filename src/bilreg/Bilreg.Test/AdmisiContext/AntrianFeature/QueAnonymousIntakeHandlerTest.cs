using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class QueAnonymousIntakeHandlerTest
{
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<ISequencer> _sequencer = new();

    public QueAnonymousIntakeHandlerTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(3);
    }

    [Fact]
    public async Task Intake_WhenNoSession_ThenCreatesAnonymousEntryOnly()
    {
        var businessDate = DateOnly.FromDateTime(TestTglJamProvider.Instance.Now);
        var servicePoint = new ServicePointType("ADM01", "Loket Admisi");
        var queue = new AntrianFactory(_sequencer.Object).Create(servicePoint, businessDate);

        _antrianRepo.Setup(x => x.ListData(businessDate)).Returns([]);
        _antrianFactory
            .Setup(x => x.Create(It.IsAny<ServicePointType>(), businessDate))
            .Returns(queue);

        AntrianModel? saved = null;
        _antrianRepo
            .Setup(x => x.SaveChanges(It.IsAny<AntrianModel>()))
            .Callback<AntrianModel>(m => saved = m);

        var sut = new QueAnonymousIntakeHandler(
            _antrianRepo.Object, _antrianFactory.Object, TestTglJamProvider.Instance);

        var result = await sut.Handle(
            new QueAnonymousIntakeCmd("ADM01", "Loket Admisi"),
            CancellationToken.None);

        result.AntrianId.Should().Be(queue.AntrianId);
        result.NoUrut.Should().Be(3);
        result.CreatedAt.Should().Be(TestTglJamProvider.Instance.Now);
        saved.Should().NotBeNull();
        saved!.ListEntry.Should().ContainSingle(e =>
            e.NoUrut == 3 && e.Tracker.PasienTrackerId == "-");
        _antrianFactory.Verify(
            x => x.Create(It.IsAny<ServicePointType>(), businessDate), Times.Once);
    }
}
