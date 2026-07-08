using System.Reflection;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class WaitingListModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal A");

    private static WaitingListModel CreateWaiting() =>
        WaitingListModel.Create(
            "RG00000001",
            AdmissionStatusEnum.Admitted,
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            priority: 1,
            "user1");

    [Fact]
    public void DT_WL_01_GivenCancelledAdmissionStatus_WhenCreate_ThenThrows()
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            AdmissionStatusEnum.Cancelled,
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            1,
            "user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*belum dalam status admisi*");
    }

    [Fact]
    public void DT_WL_01_GivenCompletedAdmissionStatus_WhenCreate_ThenThrows()
    {
        var act = () => WaitingListModel.Create(
            "RG00000001",
            AdmissionStatusEnum.Completed,
            SamplePasien(),
            SampleKelas(),
            SampleBangsal(),
            1,
            "user1");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DT_WL_02_GivenStatuses_WhenIsActive_ThenExpected()
    {
        var waiting = CreateWaiting();
        waiting.IsActive.Should().BeTrue();

        var accepted = waiting.Accept("user1");
        accepted.IsActive.Should().BeTrue();

        var closed = accepted.Close("user1");
        closed.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DT_WL_03_GivenWaitingList_WhenAcceptOrClose_ThenNoAdmissionModelParameter()
    {
        var acceptMethod = typeof(WaitingListModel).GetMethod(nameof(WaitingListModel.Accept));
        var closeMethod = typeof(WaitingListModel).GetMethod(nameof(WaitingListModel.Close));

        acceptMethod!.GetParameters().Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(string));
        closeMethod!.GetParameters().Should().ContainSingle()
            .Which.ParameterType.Should().Be(typeof(string));

        typeof(WaitingListModel).GetMethod(nameof(WaitingListModel.Create))!
            .GetParameters()
            .Should().NotContain(p => p.ParameterType == typeof(AdmissionModel));
    }

    [Fact]
    public void GivenWaiting_WhenUpdate_ThenPreservesWaitingStatus()
    {
        var updated = CreateWaiting().Update(
            2,
            new KelasReff("K2", "Kelas 2"),
            new BangsalReff("B2", "Bangsal B"),
            "user1");

        updated.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        updated.Priority.Should().Be(2);
    }

    [Fact]
    public void GivenWaiting_WhenCloseWithoutAccept_ThenThrows()
    {
        var act = () => CreateWaiting().Close("user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Accepted*");
    }
}
