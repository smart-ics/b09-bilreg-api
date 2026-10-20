using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class AdmCreateWaitingListHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenActiveWaitingListAlreadyExists_WhenCreateWaitingList_ThenReturnsExistingWaitingListIdIdempotently()
    {
        // Arrange
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1");

        var existingWaitingList = new WaitingListModel(
            "WTL-EXISTING-001",
            WaitingListStatusEnum.Waiting,
            admission.RegId,
            SamplePasienReff(),
            SampleKelas(),
            SampleBangsal(),
            3,
            AuditTrailType.Create("user1", new DateTime(2026, 10, 16)));

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        _waitingListRepoMock
            .Setup(x => x.HasActiveByRegId(admission.RegId))
            .Returns(true);

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId(admission.RegId))
            .Returns(MayBe.From(existingWaitingList));

        var handler = new AdmCreateWaitingListHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        // Act
        var response = await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 3, "user2"),
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.WaitingListId.Should().Be("WTL-EXISTING-001");
        _waitingListRepoMock.Verify(x => x.HasActiveByRegId(admission.RegId), Times.Once);
        _waitingListRepoMock.Verify(x => x.LoadActiveByRegId(admission.RegId), Times.Once);
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
        _wardGatewayMock.Verify(x => x.NotifyHandOver(It.IsAny<WardAccommodationHandOver>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UT02_GivenNoActiveWaitingList_WhenCreateWaitingList_ThenCreatesNewWaitingListAndNotifiesHandOver()
    {
        // Arrange
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        _waitingListRepoMock
            .Setup(x => x.HasActiveByRegId(admission.RegId))
            .Returns(false);

        SetupMasters();

        var handler = new AdmCreateWaitingListHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        // Act
        var response = await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 3, "user2"),
            CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.WaitingListId.Should().NotBeNullOrWhiteSpace();
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Once);
        _wardGatewayMock.Verify(
            x => x.NotifyHandOver(It.Is<WardAccommodationHandOver>(h =>
                h.RegId == admission.RegId &&
                h.BangsalId == "B1" &&
                h.KelasId == "K1" &&
                h.Status == WaitingListStatusEnum.Waiting)),
            Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT03_GivenCancelledAdmission_WhenCreateWaitingList_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelasDk(),
            SampleBangsal(),
            null,
            null,
            "user1")
            .Cancel("user1");

        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == admission.RegId)))
            .Returns(MayBe.From(admission));

        _waitingListRepoMock
            .Setup(x => x.HasActiveByRegId(admission.RegId))
            .Returns(false);

        SetupMasters();

        var handler = new AdmCreateWaitingListHandler(
            _waitingListRepoMock.Object,
            _admissionRepoMock.Object,
            _wardGatewayMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

        // Act
        var act = async () => await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 1, "user2"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*belum dalam status admisi*");
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
        _wardGatewayMock.Verify(x => x.NotifyHandOver(It.IsAny<WardAccommodationHandOver>()), Times.Never);
    }

    private void SetupMasters()
    {
        _wardGatewayMock
            .Setup(x => x.ResolveKelas("K1"))
            .Returns(SampleKelas());

        _wardGatewayMock
            .Setup(x => x.ResolveBangsal("B1"))
            .Returns(SampleBangsal());
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasDkType SampleKelasDk() => new("1", "Kelas DK 1");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
