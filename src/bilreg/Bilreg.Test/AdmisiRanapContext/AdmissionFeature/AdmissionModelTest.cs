using System.Reflection;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasDkType SampleKelasDk() => new("1", "Kelas DK 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    private static AdmissionModel CreateAdmitted() =>
        AdmissionModel.Admit(SamplePasien(), SampleKelasDk(), SampleBangsal(), null, null, "user1");

    [Fact]
    public void DT_AD_01_GivenAdmit_WhenLifecycle_ThenReachesCompleted()
    {
        var admitted = CreateAdmitted();

        admitted.Should().BeAssignableTo<IRegKey>();
        admitted.RegId.Should().StartWith("RG");
        admitted.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
        admitted.AdmissionSource.Should().Be(AdmissionSourceEnum.Admission);

        var updated = admitted.Update(new KelasDkType("2", "Kelas DK 2"), SampleBangsal(), "user1");
        updated.AdmissionStatus.Should().Be(AdmissionStatusEnum.Updated);

        var waiting = updated.MarkWaiting("user1");
        waiting.AdmissionStatus.Should().Be(AdmissionStatusEnum.Waiting);

        var completed = waiting.Complete("user1");
        completed.AdmissionStatus.Should().Be(AdmissionStatusEnum.Completed);
    }

    [Fact]
    public void DT_AD_02_GivenAdmissionModel_WhenInspectMembers_ThenNoRoomOrBedAllocation()
    {
        var forbidden = new[] { "Bed", "Kamar", "Room", "Allocate" };
        var members = typeof(AdmissionModel)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(m => m.Name)
            .ToList();

        foreach (var token in forbidden)
        {
            members.Where(m => m.Contains(token, StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty(because: $"Admission must not expose {token} allocation concerns");
        }
    }

    [Fact]
    public void GivenCompleted_WhenUpdate_ThenThrows()
    {
        var completed = CreateAdmitted().Complete("user1");

        var act = () => completed.Update(SampleKelasDk(), SampleBangsal(), "user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak diperbolehkan*");
    }

    [Fact]
    public void GivenAdmitted_WhenCancel_ThenCancelled()
    {
        var cancelled = CreateAdmitted().Cancel("user1");

        cancelled.AdmissionStatus.Should().Be(AdmissionStatusEnum.Cancelled);
        cancelled.AdmissionSource.Should().Be(AdmissionSourceEnum.Admission);
    }

    [Fact]
    public void GivenLegacyRegInap_WhenCreateFromLegacyRegistration_ThenUsesExistingRegIdAndLegacySource()
    {
        var reg = CreateRegInap("RGLEGACY1");

        var admission = AdmissionModel.CreateFromLegacyRegistration(
            reg,
            "legacy-sync");

        admission.RegId.Should().Be("RGLEGACY1");
        admission.Pasien.Should().Be(reg.Pasien);
        admission.OpnameRequestId.Should().Be("-");
        admission.ReservationId.Should().Be("-");
        admission.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
        admission.AdmissionSource.Should().Be(AdmissionSourceEnum.Legacy);
    }

    [Fact]
    public void GivenLegacyRegJalan_WhenCreateFromLegacyRegistration_ThenThrows()
    {
        var reg = CreateRegJalan("RGRAJAL1");

        var act = () => AdmissionModel.CreateFromLegacyRegistration(
            reg,
            "legacy-sync");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RegInap*");
    }

    [Fact]
    public void GivenLegacyRegInapWithoutKelasDk_WhenCreateFromLegacyRegistration_ThenThrows()
    {
        var reg = CreateReg("RGNOKLS1", JenisRegEnum.RegInap, KelasDkType.Default, SampleBangsal());

        var act = () => AdmissionModel.CreateFromLegacyRegistration(reg, "legacy-sync");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*KelasDk*");
    }

    [Fact]
    public void GivenLegacyRegInapWithoutBangsal_WhenCreateFromLegacyRegistration_ThenThrows()
    {
        var reg = CreateReg("RGNOBGS1", JenisRegEnum.RegInap, SampleKelasDk(), new BangsalReff("-", "-"));

        var act = () => AdmissionModel.CreateFromLegacyRegistration(reg, "legacy-sync");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bangsal*");
    }

    private static RegModel CreateRegInap(string regId) =>
        CreateReg(regId, JenisRegEnum.RegInap, SampleKelasDk(), SampleBangsal());

    private static RegModel CreateRegJalan(string regId) =>
        CreateReg(regId, JenisRegEnum.RegJalan, SampleKelasDk(), SampleBangsal());

    private static RegModel CreateReg(
        string regId,
        JenisRegEnum jenisReg,
        KelasDkType kelasDk,
        BangsalReff bangsal) =>
        new(
            regId,
            new DateOnly(2026, 7, 7),
            new AuditInfoType("legacy", new DateTime(2026, 7, 7)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            SamplePasien(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            kelasDk,
            bangsal);
}
