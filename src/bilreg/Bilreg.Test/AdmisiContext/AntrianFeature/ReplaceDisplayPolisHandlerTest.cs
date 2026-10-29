using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class ReplaceDisplayPolisHandlerTest
{
    private readonly Mock<IAdmissionQueueDisplayRepo> _displayRepoMock = new();
    private readonly Mock<IClinicQueueOperationDal> _clinicDalMock = new();
    private readonly Mock<ITglJamProvider> _clockMock = new();

    private readonly DateTime _now = new(2026, 10, 28, 11, 0, 0);

    public ReplaceDisplayPolisHandlerTest()
    {
        _clockMock.Setup(c => c.Now).Returns(_now);
    }

    [Fact]
    public async Task Handle_WhenDisplayNotFound_ThrowsDisplayNotFoundException()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        var sut = new ReplaceDisplayPolisHandler(_displayRepoMock.Object, _clinicDalMock.Object, _clockMock.Object);

        var cmd = new ReplaceDisplayPolisCmd("DISP-99", new[] { new AdmissionDisplayPoliDtoResponse("POLI-A", 1) }, "1", "user1");
        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayNotFound);
    }

    [Fact]
    public async Task Handle_WhenRowVersionMismatch_ThrowsConcurrencyException()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-CLI", "Display Poli", "Lt 1", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "user1", _now,
            new[] { new AdmissionDisplayPoliMapping("POLI-A", 1) });

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI")))
            .Returns(MayBe.From(display));

        var sut = new ReplaceDisplayPolisHandler(_displayRepoMock.Object, _clinicDalMock.Object, _clockMock.Object);

        // Stale row version (display is at version 1, passing version 99)
        var cmd = new ReplaceDisplayPolisCmd("DISP-CLI", new[] { new AdmissionDisplayPoliDtoResponse("POLI-B", 1) }, "99", "user1");
        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.Concurrency);
    }

    [Fact]
    public async Task Handle_WhenActiveClinicDisplayAndPolisEmpty_ThrowsDisplayMappingRequired()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-CLI", "Display Poli", "Lt 1", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "user1", _now,
            new[] { new AdmissionDisplayPoliMapping("POLI-A", 1) });

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI")))
            .Returns(MayBe.From(display));

        var sut = new ReplaceDisplayPolisHandler(_displayRepoMock.Object, _clinicDalMock.Object, _clockMock.Object);

        var cmd = new ReplaceDisplayPolisCmd("DISP-CLI", Array.Empty<AdmissionDisplayPoliDtoResponse>(), "1", "user1");
        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesRepoAndDalAndReturnsDto()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-CLI", "Display Poli", "Lt 1", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "user1", _now,
            new[] { new AdmissionDisplayPoliMapping("POLI-A", 1) });

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI")))
            .Returns(MayBe.From(display));

        AdmissionQueueDisplayModel? savedModel = null;
        _displayRepoMock.Setup(r => r.SaveChanges(It.IsAny<AdmissionQueueDisplayModel>()))
            .Callback<AdmissionQueueDisplayModel>(m => savedModel = m);

        var sut = new ReplaceDisplayPolisHandler(_displayRepoMock.Object, _clinicDalMock.Object, _clockMock.Object);

        var newPolis = new[]
        {
            new AdmissionDisplayPoliDtoResponse("POLI-B", 2),
            new AdmissionDisplayPoliDtoResponse("POLI-C", 1)
        };
        var cmd = new ReplaceDisplayPolisCmd("DISP-CLI", newPolis, "1", "admin1");
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.DisplayId.Should().Be("DISP-CLI");
        result.RowVersion.Should().Be("2");
        result.Polis.Should().NotBeNull();
        result.Polis!.Select(p => p.PoliId).Should().Equal("POLI-C", "POLI-B");

        savedModel.Should().NotBeNull();
        savedModel!.RowVersion.Should().Be(2);
        savedModel.UpdatedBy.Should().Be("admin1");

        _clinicDalMock.Verify(d => d.ReplaceDisplayPolis(
            "DISP-CLI",
            It.Is<IEnumerable<string>>(p => p.SequenceEqual(new[] { "POLI-C", "POLI-B" }))),
            Times.Once);
    }
}
