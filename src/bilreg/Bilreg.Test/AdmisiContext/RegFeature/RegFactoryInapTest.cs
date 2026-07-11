using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegFactoryInapTest
{
    private readonly Mock<ISequencerManual> _sequencer = new();

    [Fact]
    public void GivenAdmission_WhenCreateRegInap_ThenUsesAdmissionIdentityAndPlacement()
    {
        var admission = AdmissionModel.Admit(
            PasienModel.Default.ToReff(),
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal 1"),
            null, null, "user1");
        var (layanan, karcis) = InpatientVisit();
        var factory = new RegFactory(_sequencer.Object,
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        var reg = factory.CreateRegInapFromAdmission(
            admission, PasienModel.Default, TipeJaminanType.BayarSendiri,
            PolisModel.Default, CaraMasukDkType.DatangSendiri, RujukanType.Default,
            PpaType.Default, layanan, karcis, "PESERTA1");

        reg.RegId.Should().Be(admission.RegId);
        reg.JenisReg.Should().Be(JenisRegEnum.RegInap);
        reg.Kelas.Should().Be(KelasType.Default.ToReff());
        reg.KelasDk.Should().Be(admission.KelasDk);
        reg.Bangsal.Should().Be(admission.Bangsal);
        reg.Layanan.Should().Be(layanan.ToReff());
        _sequencer.VerifyNoOtherCalls();
    }

    [Fact]
    public void GivenNonInpatientService_WhenCreateRegInap_ThenThrows()
    {
        var admission = AdmissionModel.Admit(PasienModel.Default.ToReff(),
            new KelasDkType("1", "Kelas 1"), new BangsalReff("B1", "Bangsal 1"),
            null, null, "user1");
        var (layanan, karcis) = InpatientVisit();
        layanan = layanan with { InstalasiDk = InstalasiDkType.RawatJalan };
        var factory = new RegFactory(_sequencer.Object,
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        var act = () => factory.CreateRegInapFromAdmission(
            admission, PasienModel.Default, TipeJaminanType.BayarSendiri,
            PolisModel.Default, CaraMasukDkType.DatangSendiri, RujukanType.Default,
            PpaType.Default, layanan, karcis, "PESERTA1");

        act.Should().Throw<ArgumentException>().WithMessage("*bukan instalasi rawat inap*");
    }

    private static (LayananType, KarcisType) InpatientVisit()
    {
        var layanan = LayananType.Default with
        {
            LayananId = "RI1",
            LayananName = "Rawat Inap",
            InstalasiDk = InstalasiDkType.RawatInap
        };
        var karcis = new KarcisType("KRI", "Karcis Inap", true,
            InstalasiDkType.RawatInap, RekapCetakType.Default.ToReff(),
            TarifType.Default.ToReff(), [], [layanan.ToReff()]);
        return (layanan, karcis);
    }
}
