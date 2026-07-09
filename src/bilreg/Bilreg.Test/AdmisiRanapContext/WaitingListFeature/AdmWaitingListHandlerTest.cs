using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
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
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class AdmWaitingListHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IWardAccommodationGateway> _wardGatewayMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    [Fact]
    public async Task UT01_GivenCancelledAdmission_WhenCreateWaitingList_ThenThrows()
    {
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
            _auditRepoMock.Object);

        var act = async () => await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 1, "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*belum dalam status admisi*");
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
        _wardGatewayMock.Verify(x => x.NotifyHandOver(It.IsAny<WardAccommodationHandOver>()), Times.Never);
    }

    [Fact]
    public async Task UT02_GivenAcceptedWaitingList_WhenClose_ThenDoesNotSaveAdmission()
    {
        var waitingList = WaitingListModel.Create(
            "RG00000001",
            AdmissionStatusEnum.Admitted,
            SamplePasienReff(),
            SampleKelas(),
            SampleBangsal(),
            1,
            "user1")
            .Accept("user1");

        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == waitingList.WaitingListId)))
            .Returns(MayBe.From(waitingList));

        WaitingListModel? saved = null;
        _waitingListRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => saved = m);

        var handler = new AdmCloseWaitingListHandler(
            _waitingListRepoMock.Object,
            _auditRepoMock.Object);
        await handler.Handle(
            new AdmCloseWaitingListCmd(waitingList.WaitingListId, "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Closed);
        _admissionRepoMock.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UT03_GivenAdmittedAdmission_WhenCreateWaitingList_ThenNotifiesWardHandOver()
    {
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
            _auditRepoMock.Object);

        await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 1, "user2"),
            CancellationToken.None);

        _wardGatewayMock.Verify(
            x => x.NotifyHandOver(It.Is<WardAccommodationHandOver>(h =>
                h.RegId == admission.RegId &&
                h.BangsalId == "B1" &&
                h.KelasId == "K1" &&
                h.Status == WaitingListStatusEnum.Waiting)),
            Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    private void SetupMasters()
    {
        _wardGatewayMock
            .Setup(x => x.ResolveKelas("K1"))
            .Returns(new KelasReff("K1", "Kelas 1"));

        _wardGatewayMock
            .Setup(x => x.ResolveBangsal("B1"))
            .Returns(new BangsalReff("B1", "Bangsal A"));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasDkType SampleKelasDk() => new("1", "Kelas DK 1");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
