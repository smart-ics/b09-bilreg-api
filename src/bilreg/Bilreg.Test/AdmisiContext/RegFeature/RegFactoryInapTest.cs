using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
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
        var layanan = InpatientLayanan();
        var factory = new RegFactory(_sequencer.Object,
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        var reg = factory.CreateRegInapFromAdmission(
            admission, PasienModel.Default, TipeJaminanType.BayarSendiri,
            PolisModel.Default, CaraMasukDkType.DatangSendiri, RujukanType.Default,
            PpaType.Default, layanan, "PESERTA1");

        reg.RegId.Should().Be(admission.RegId);
        reg.JenisReg.Should().Be(JenisRegEnum.RegInap);
        reg.Kelas.Should().Be(KelasType.Default.ToReff());
        reg.KelasDk.Should().Be(admission.KelasDk);
        reg.Bangsal.Should().Be(admission.Bangsal);
        reg.Layanan.Should().Be(layanan.ToReff());
        reg.Karcis.Should().Be(KarcisType.Default.ToReff());
        reg.ListKomponen.Should().BeEmpty();
        _sequencer.VerifyNoOtherCalls();
    }

    [Fact]
    public void GivenInpatientVisit_WhenCreateRegInap_ThenLeavesKomponenEmptyAndKarcisDefault()
    {
        var admission = AdmissionModel.Admit(
            PasienModel.Default.ToReff(),
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal 1"),
            null, null, "user1");
        var layanan = InpatientLayanan();
        var factory = new RegFactory(_sequencer.Object,
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        var reg = factory.CreateRegInapFromAdmission(
            admission, PasienModel.Default, TipeJaminanType.BayarSendiri,
            PolisModel.Default, CaraMasukDkType.DatangSendiri, RujukanType.Default,
            PpaType.Default, layanan, "PESERTA1");

        reg.ListKomponen.Should().BeEmpty();
        reg.Karcis.KarcisId.Should().Be("-");
    }

    [Fact]
    public void GivenNonInpatientService_WhenCreateRegInap_ThenThrows()
    {
        var admission = AdmissionModel.Admit(PasienModel.Default.ToReff(),
            new KelasDkType("1", "Kelas 1"), new BangsalReff("B1", "Bangsal 1"),
            null, null, "user1");
        var layanan = InpatientLayanan() with { InstalasiDk = InstalasiDkType.RawatJalan };
        var factory = new RegFactory(_sequencer.Object,
            Mock.Of<IGetKelasRajalService>(), Mock.Of<IGetKelasRadarService>());

        var act = () => factory.CreateRegInapFromAdmission(
            admission, PasienModel.Default, TipeJaminanType.BayarSendiri,
            PolisModel.Default, CaraMasukDkType.DatangSendiri, RujukanType.Default,
            PpaType.Default, layanan, "PESERTA1");

        act.Should().Throw<ArgumentException>().WithMessage("*bukan instalasi rawat inap*");
    }

    private static LayananType InpatientLayanan() =>
        LayananType.Default with
        {
            LayananId = "RI1",
            LayananName = "Rawat Inap",
            InstalasiDk = InstalasiDkType.RawatInap
        };
}
