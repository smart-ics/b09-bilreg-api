using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionRegistrationOrchestratorTest
{
    [Fact]
    public async Task GivenOpnameRequest_WhenProcessed_ThenCreatesAdmissionRegAndRegAktifWithSharedId()
    {
        var opname = OpnameRequestModel.Create(PasienModel.Default.ToReff(),
            PpaType.Default.ToReff(), DateTime.Today.AddDays(1), "test", "user1");
        var admissionRepo = new Mock<IAdmissionRepo>();
        var opnameRepo = new Mock<IOpnameRequestRepo>();
        var reservationRepo = new Mock<IReservationRepo>();
        var regRepo = new Mock<IRegRepo>();
        var regAktifRepo = new Mock<IRegAktifRepo>();
        opnameRepo.Setup(x => x.LoadEntity(It.IsAny<IOpnameRequestKey>())).Returns(MayBe.From(opname));
        admissionRepo.Setup(x => x.ListData(It.IsAny<AdmissionListFilter>())).Returns([]);
        regAktifRepo.Setup(x => x.IsPasienAktif(It.IsAny<IPasienKey>())).Returns(false);

        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(PasienModel.Default));
        var tipeJaminanRepo = new Mock<ITipeJaminanRepo>();
        tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(TipeJaminanType.BayarSendiri));
        var caraMasukRepo = new Mock<ICaraMasukDkRepo>();
        caraMasukRepo.Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>()))
            .Returns(MayBe.From(CaraMasukDkType.DatangSendiri));
        var rujukanRepo = new Mock<IRujukanRepo>();
        rujukanRepo.Setup(x => x.LoadEntity(It.IsAny<IRujukanKey>()))
            .Returns(MayBe.From(RujukanType.Default));
        var ppaRepo = new Mock<IPpaRepo>();
        ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(PpaType.Default));
        var (layanan, karcis) = InpatientVisit();
        var layananRepo = new Mock<ILayananRepo>();
        layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        var karcisRepo = new Mock<IKarcisRepo>();
        karcisRepo.Setup(x => x.LoadEntity(It.IsAny<IKarcisKey>())).Returns(MayBe.From(karcis));

        var ward = new Mock<IWardAccommodationGateway>();
        ward.Setup(x => x.ResolveKelasDk("1")).Returns(new KelasDkType("1", "Kelas 1"));
        ward.Setup(x => x.ResolveBangsalForCareClass("B1", "1"))
            .Returns(new BangsalReff("B1", "Bangsal 1"));
        var factory = new RegFactory(Mock.Of<ISequencerManual>(),
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        AdmissionModel? savedAdmission = null;
        RegModel? savedReg = null;
        RegAktifModel? savedRegAktif = null;
        OpnameRequestModel? savedOpname = null;
        admissionRepo.Setup(x => x.SaveChanges(It.IsAny<AdmissionModel>()))
            .Callback<AdmissionModel>(x => savedAdmission = x);
        regRepo.Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(x => savedReg = x);
        regAktifRepo.Setup(x => x.SaveChanges(It.IsAny<RegAktifModel>()))
            .Callback<RegAktifModel>(x => savedRegAktif = x);
        opnameRepo.Setup(x => x.SaveChanges(It.IsAny<OpnameRequestModel>()))
            .Callback<OpnameRequestModel>(x => savedOpname = x);

        var sut = new AdmissionRegistrationOrchestrator(
            admissionRepo.Object, opnameRepo.Object, reservationRepo.Object, ward.Object,
            pasienRepo.Object, tipeJaminanRepo.Object, Mock.Of<IPolisRepo>(),
            caraMasukRepo.Object, rujukanRepo.Object, ppaRepo.Object, layananRepo.Object,
            karcisRepo.Object, factory, regRepo.Object, regAktifRepo.Object, Mock.Of<IAuditRepo>());
        var command = new AdmProcessOpnameRequestCmd(opname.OpnameRequestId, "1", "B1", "user2",
            new AdmissionRegistrationData("00000", "1", "-", "-", "RI1", "KRI", "PESERTA1"));

        var response = await sut.ProcessOpnameRequest(command, CancellationToken.None);

        savedAdmission.Should().NotBeNull();
        savedReg.Should().NotBeNull();
        savedRegAktif.Should().NotBeNull();
        savedAdmission!.AdmissionSource.Should().Be(AdmissionSourceEnum.Admission);
        savedReg!.JenisReg.Should().Be(JenisRegEnum.RegInap);
        savedReg.RegId.Should().Be(savedAdmission.RegId).And.Be(response.RegId);
        savedRegAktif!.RegId.Should().Be(response.RegId);
        savedOpname!.FulfilledRegId.Should().Be(response.RegId);
    }

    private static (LayananType, KarcisType) InpatientVisit()
    {
        var layanan = LayananType.Default with
        {
            LayananId = "RI1", LayananName = "Rawat Inap", InstalasiDk = InstalasiDkType.RawatInap
        };
        var karcis = new KarcisType("KRI", "Karcis Inap", true,
            InstalasiDkType.RawatInap, RekapCetakType.Default.ToReff(),
            TarifType.Default.ToReff(), [], [layanan.ToReff()]);
        return (layanan, karcis);
    }
}
