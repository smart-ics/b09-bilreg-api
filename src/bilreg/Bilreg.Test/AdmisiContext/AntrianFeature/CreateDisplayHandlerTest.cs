using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class CreateDisplayHandlerTest
{
    private readonly Mock<IAdmissionQueueDisplayRepo> _displayRepoMock = new();
    private readonly Mock<IClinicQueueOperationDal> _clinicDalMock = new();
    private readonly Mock<ITglJamProvider> _clockMock = new();

    private readonly DateTime _now = new(2026, 10, 28, 10, 0, 0);

    public CreateDisplayHandlerTest()
    {
        _clockMock.Setup(c => c.Now).Returns(_now);
    }

    [Fact]
    public async Task Handle_ActiveClinicDisplay_WithPolis_SucceedsAndPersistsPolis()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        AdmissionQueueDisplayModel? savedModel = null;
        _displayRepoMock.Setup(r => r.SaveChanges(It.IsAny<AdmissionQueueDisplayModel>()))
            .Callback<AdmissionQueueDisplayModel>(m => savedModel = m);

        var sut = new CreateDisplayHandler(_displayRepoMock.Object, _clockMock.Object, _clinicDalMock.Object);

        var polis = new List<AdmissionDisplayPoliDtoResponse>
        {
            new("POLI-INT", 1),
            new("POLI-ANAK", 2)
        };

        var cmd = new CreateDisplayCmd(
            "DISP-CLI", "Clinic Display", "Lt 2", true, true, 5000, "clinic", "notes",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(), "admin", polis);

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.DisplayId.Should().Be("DISP-CLI");
        response.DisplayName.Should().Be("Clinic Display");
        response.LayoutKey.Should().Be("clinic");
        response.Active.Should().BeTrue();
        response.Polis.Select(p => p.PoliId).Should().Equal("POLI-INT", "POLI-ANAK");

        savedModel.Should().NotBeNull();
        savedModel!.Polis.Select(p => p.PoliId).Should().Equal("POLI-INT", "POLI-ANAK");
        _displayRepoMock.Verify(r => r.SaveChanges(It.IsAny<AdmissionQueueDisplayModel>()), Times.Once);

        _clinicDalMock.Verify(d => d.ReplaceDisplayPolis(
            "DISP-CLI",
            It.Is<IEnumerable<string>>(p => p.SequenceEqual(new[] { "POLI-INT", "POLI-ANAK" }))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveClinicDisplay_WithoutPolis_ThrowsDisplayMappingRequired()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        var sut = new CreateDisplayHandler(_displayRepoMock.Object, _clockMock.Object, _clinicDalMock.Object);

        var cmd = new CreateDisplayCmd(
            "DISP-CLI", "Clinic Display", "Lt 2", true, true, 5000, "clinic", "notes",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(), "admin", null);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired);
    }

    [Fact]
    public async Task Handle_ActiveAdmissionDisplay_WithoutLokets_ThrowsDisplayMappingRequired()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        var sut = new CreateDisplayHandler(_displayRepoMock.Object, _clockMock.Object, _clinicDalMock.Object);

        var cmd = new CreateDisplayCmd(
            "DISP-ADM", "Admission Display", "Lobby", true, true, 5000, "default", "notes",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(), "admin", null);

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired);
    }

    [Fact]
    public async Task Handle_InactiveClinicDisplay_WithoutPolis_Succeeds()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        var sut = new CreateDisplayHandler(_displayRepoMock.Object, _clockMock.Object, _clinicDalMock.Object);

        var cmd = new CreateDisplayCmd(
            "DISP-CLI", "Clinic Display", "Lt 2", false, true, 5000, "clinic", "notes",
            Array.Empty<AdmissionDisplayLoketDtoResponse>(), "admin", null);

        var response = await sut.Handle(cmd, CancellationToken.None);

        response.DisplayId.Should().Be("DISP-CLI");
        response.Active.Should().BeFalse();
        response.Polis.Should().BeEmpty();
        _displayRepoMock.Verify(r => r.SaveChanges(It.IsAny<AdmissionQueueDisplayModel>()), Times.Once);
    }
}
