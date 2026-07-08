using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.OpnameRequestFeature;

public class OpnameRequestModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static PpaReff SampleDokter() => new("D001", "Dr. Test");

    private static OpnameRequestModel CreateRequested() =>
        OpnameRequestModel.Create(SamplePasien(), SampleDokter(), "Catatan klinis", "user1");

    [Fact]
    public void DT_OR_01_GivenRequested_WhenFulfillOrCancel_ThenValidTransitions()
    {
        var request = CreateRequested();

        var fulfilled = request.Fulfill("RG00000001", "user1");
        fulfilled.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Fulfilled);
        fulfilled.FulfilledRegId.Should().Be("RG00000001");

        var cancelled = CreateRequested().Cancel("user1");
        cancelled.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Cancelled);
    }

    [Fact]
    public void DT_OR_01_GivenFulfilled_WhenCancel_ThenThrows()
    {
        var request = CreateRequested().Fulfill("RG00000001", "user1");

        var act = () => request.Cancel("user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Requested*");
    }

    [Fact]
    public void DT_OR_02_GivenCancelled_WhenFulfill_ThenThrows()
    {
        var request = CreateRequested().Cancel("user1");

        var act = () => request.Fulfill("RG00000001", "user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void DT_OR_01_GivenCreate_WhenCalled_ThenRequestedWithOpnId()
    {
        var request = CreateRequested();

        request.OpnameRequestStatus.Should().Be(OpnameRequestStatusEnum.Requested);
        request.OpnameRequestId.Should().StartWith("OPN");
        request.FulfilledRegId.Should().Be("-");
    }

    [Fact]
    public void GivenFulfilled_WhenFulfillAgain_ThenThrows()
    {
        var request = CreateRequested().Fulfill("RG00000001", "user1");

        var act = () => request.Fulfill("RG00000002", "user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dipenuhi*");
    }
}
