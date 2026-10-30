using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class DisplayBootConfigAndSnapshotHandlerTest
{
    private readonly Mock<IAdmissionQueueDisplayRepo> _displayRepoMock = new();
    private readonly Mock<IAdmissionQueueOperationalProjection> _projectionMock = new();
    private readonly Mock<IClinicQueueOperationDal> _clinicDalMock = new();
    private readonly Mock<IAntrianRepo> _antrianRepoMock = new();

    private readonly DateTime _now = new(2026, 10, 28, 10, 0, 0);

    [Fact]
    public async Task GetDisplayBootConfigHandler_AdmissionLayout_ReturnsLoketIdsAndNullPoliIds()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-ADM", "Admission Display", "Lobby", true, true, 5000, "default", "",
            new[] { new AdmissionDisplayLoketMapping("LOKET-01", 0), new AdmissionDisplayLoketMapping("LOKET-02", 1) },
            "user1", _now);

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-ADM")))
            .Returns(MayBe.From(display));

        var sut = new GetDisplayBootConfigHandler(_displayRepoMock.Object, _clinicDalMock.Object);
        var response = await sut.Handle(new GetDisplayBootConfigQry("DISP-ADM"), CancellationToken.None);

        response.DeviceId.Should().Be("DISP-ADM");
        response.LoketIds.Should().Equal("LOKET-01", "LOKET-02");
        response.PoliIds.Should().BeNull();
        response.LayoutKey.Should().Be("default");
    }

    [Fact]
    public async Task GetDisplayBootConfigHandler_ClinicLayout_WithPolisInModel_ReturnsPoliIds()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-CLI", "Clinic Display", "Lantai 2", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "user1", _now,
            new[] { new AdmissionDisplayPoliMapping("POLI-INT", 0), new AdmissionDisplayPoliMapping("POLI-ANAK", 1) });

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI")))
            .Returns(MayBe.From(display));

        var sut = new GetDisplayBootConfigHandler(_displayRepoMock.Object, _clinicDalMock.Object);
        var response = await sut.Handle(new GetDisplayBootConfigQry("DISP-CLI"), CancellationToken.None);

        response.DeviceId.Should().Be("DISP-CLI");
        response.PoliIds.Should().Equal("POLI-INT", "POLI-ANAK");
        response.LayoutKey.Should().Be("clinic");
    }

    [Fact]
    public async Task GetDisplayBootConfigHandler_ClinicLayout_LoadsPolisFromDalWhenEmptyInModel()
    {
        var display = AdmissionQueueDisplayModel.Load(
            "DISP-CLI-LEGACY", "Clinic Display", "Lantai 2", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), 1, "user1", _now, "user1", _now,
            Array.Empty<AdmissionDisplayPoliMapping>());

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI-LEGACY")))
            .Returns(MayBe.From(display));

        _clinicDalMock.Setup(d => d.GetDisplayPolis("DISP-CLI-LEGACY"))
            .Returns(new List<AdmissionDisplayPoliDto>
            {
                new() { DisplayId = "DISP-CLI-LEGACY", PoliId = "POLI-BEDAH", SortOrder = 0 }
            });

        var sut = new GetDisplayBootConfigHandler(_displayRepoMock.Object, _clinicDalMock.Object);
        var response = await sut.Handle(new GetDisplayBootConfigQry("DISP-CLI-LEGACY"), CancellationToken.None);

        response.DeviceId.Should().Be("DISP-CLI-LEGACY");
        response.PoliIds.Should().Equal("POLI-BEDAH");
    }

    [Fact]
    public async Task GetDisplayBootConfigHandler_ThrowsWhenNotFoundOrInactive()
    {
        _displayRepoMock.Setup(r => r.LoadEntity(It.IsAny<IAdmissionQueueDisplayKey>()))
            .Returns(MayBe<AdmissionQueueDisplayModel>.None);

        var sut = new GetDisplayBootConfigHandler(_displayRepoMock.Object, _clinicDalMock.Object);
        var notFound = () => sut.Handle(new GetDisplayBootConfigQry("NONEXISTENT"), CancellationToken.None);
        await notFound.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayNotFound);

        var inactiveDisplay = AdmissionQueueDisplayModel.Create(
            "DISP-INACTIVE", "Inactive", "Lobby", false, true, 5000, "default", "",
            new[] { new AdmissionDisplayLoketMapping("LOKET-01", 0) }, "user1", _now);
        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-INACTIVE")))
            .Returns(MayBe.From(inactiveDisplay));

        var inactive = () => sut.Handle(new GetDisplayBootConfigQry("DISP-INACTIVE"), CancellationToken.None);
        await inactive.Should().ThrowAsync<AdmissionQueueConfigurationException>()
            .Where(e => e.Code == AdmissionQueueConfigurationErrorCodes.DisplayInactive);
    }

    [Fact]
    public async Task GetPublicDisplaySnapshotHandler_AdmissionLayout_ReturnsProjectionItems()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-ADM", "Admission Display", "Lobby", true, true, 5000, "default", "",
            new[] { new AdmissionDisplayLoketMapping("LOKET-01", 0) }, "user1", _now);

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-ADM")))
            .Returns(MayBe.From(display));

        var projectionItems = new List<CurrentLoketDisplayItem>
        {
            new("LOKET-01", "Q-01", 1, "A001", "SP-01", AdmissionQueueClaimState.InService, 1, _now, _now, Array.Empty<byte>()),
            new("LOKET-02", "Q-02", 2, "A002", "SP-01", AdmissionQueueClaimState.InService, 1, _now, _now, Array.Empty<byte>())
        };
        _projectionMock.Setup(p => p.ListCurrentLoket(null)).Returns(projectionItems);

        var sut = new GetPublicDisplaySnapshotHandler(
            _displayRepoMock.Object, _projectionMock.Object, _clinicDalMock.Object, _antrianRepoMock.Object);

        var result = await sut.Handle(new GetPublicDisplaySnapshotQry("DISP-ADM"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].LoketKey.Should().Be("LOKET-01");
        result[0].QueueLabel.Should().Be("A001");
    }

    [Fact]
    public async Task GetPublicDisplaySnapshotHandler_ClinicLayout_ReturnsActiveCallsAsDisplayItems()
    {
        var display = AdmissionQueueDisplayModel.Create(
            "DISP-CLI", "Clinic Display", "Lt 2", true, true, 5000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "user1", _now,
            new[] { new AdmissionDisplayPoliMapping("POLI-INT", 0) });

        _displayRepoMock.Setup(r => r.LoadEntity(It.Is<IAdmissionQueueDisplayKey>(k => k.DisplayId == "DISP-CLI")))
            .Returns(MayBe.From(display));

        var callDto = new ClinicQueueCallDto
        {
            PoliId = "POLI-INT",
            DokterId = "DOC-01",
            RuangId = "R-101",
            AntrianId = "QUE-CLI-01",
            NoUrut = 12,
            CalledAt = _now,
            CallCount = 3,
            CallStatus = ClinicQueueCallStatus.Active
        };

        _clinicDalMock.Setup(d => d.GetActiveCallsByPoliIds(It.Is<IEnumerable<string>>(p => p.Contains("POLI-INT"))))
            .Returns(new List<ClinicQueueCallDto> { callDto });

        var queue = new AntrianModel("QUE-CLI-01", DateOnly.FromDateTime(_now), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "AN", "Desc", ServicePointType.Default, Array.Empty<AntrianEntryModel>(), null!, queuePrefixSnapshot: "B");

        _antrianRepoMock.Setup(r => r.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "QUE-CLI-01")))
            .Returns(MayBe.From(queue));

        var sut = new GetPublicDisplaySnapshotHandler(
            _displayRepoMock.Object, _projectionMock.Object, _clinicDalMock.Object, _antrianRepoMock.Object);

        var result = await sut.Handle(new GetPublicDisplaySnapshotQry("DISP-CLI"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].LoketKey.Should().Be("R-101");
        result[0].AntrianId.Should().Be("QUE-CLI-01");
        result[0].NoUrut.Should().Be(12);
        result[0].QueueLabel.Should().Be("B0012");
        result[0].ServicePointId.Should().Be("POLI-INT");
        result[0].AnnouncementVersion.Should().Be(3);
    }
}
