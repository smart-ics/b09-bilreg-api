using System.Reflection;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    private static AdmissionModel CreateAdmitted() =>
        AdmissionModel.Admit(SamplePasien(), SampleKelas(), SampleBangsal(), null, null, "user1");

    [Fact]
    public void DT_AD_01_GivenAdmit_WhenLifecycle_ThenReachesCompleted()
    {
        var admitted = CreateAdmitted();

        admitted.Should().BeAssignableTo<IRegKey>();
        admitted.RegId.Should().StartWith("RG");
        admitted.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);

        var updated = admitted.Update(new KelasReff("K2", "Kelas 2"), SampleBangsal(), "user1");
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

        var act = () => completed.Update(SampleKelas(), SampleBangsal(), "user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak diperbolehkan*");
    }

    [Fact]
    public void GivenAdmitted_WhenCancel_ThenCancelled()
    {
        var cancelled = CreateAdmitted().Cancel("user1");

        cancelled.AdmissionStatus.Should().Be(AdmissionStatusEnum.Cancelled);
    }
}
