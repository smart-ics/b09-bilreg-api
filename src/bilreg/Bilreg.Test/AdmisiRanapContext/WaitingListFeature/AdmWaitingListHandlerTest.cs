using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class AdmWaitingListHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();

    [Fact]
    public async Task UT01_GivenCancelledAdmission_WhenCreateWaitingList_ThenThrows()
    {
        var admission = AdmissionModel.Admit(
            SamplePasienReff(),
            SampleKelas(),
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
            _kelasRepoMock.Object,
            _bangsalRepoMock.Object);

        var act = async () => await handler.Handle(
            new AdmCreateWaitingListCmd(admission.RegId, "K1", "B1", 1, "user2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*belum dalam status admisi*");
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
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

        var handler = new AdmCloseWaitingListHandler(_waitingListRepoMock.Object);
        await handler.Handle(
            new AdmCloseWaitingListCmd(waitingList.WaitingListId, "user2"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Closed);
        _admissionRepoMock.Verify(x => x.SaveChanges(It.IsAny<AdmissionModel>()), Times.Never);
    }

    private void SetupMasters()
    {
        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K1")))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1", KelasName = "Kelas 1" }));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B1")))
            .Returns(MayBe.From(new BangsalType(
                "B1",
                "Bangsal A",
                RoomCatType.Default,
                new Bilreg.Domain.AdmisiContext.LayananFeature.LayananReff("-", "-"))));
    }

    private static PasienReff SamplePasienReff() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");
}
