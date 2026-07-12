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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenWaitingOrAccepted_WhenCancel_ThenVoidsWithReasonAndTimestamp(bool accepted)
    {
        var waitingList = CreateWaiting();
        if (accepted)
            waitingList = waitingList.Accept("user1");
        var timestamp = new DateTime(2026, 7, 12, 10, 30, 0);

        var cancelled = waitingList.Cancel("void-user", "Pasien membatalkan rencana rawat inap", timestamp);

        cancelled.WaitingListStatus.Should().Be(WaitingListStatusEnum.Cancelled);
        cancelled.IsActive.Should().BeFalse();
        cancelled.AuditTrail.Voided.Should().Be(new Bilreg.Domain.Shared.Helpers.CommonValueObjects.AuditInfoType("void-user", timestamp));
    }

    [Fact]
    public void GivenClosedOrCancelled_WhenCancel_ThenThrows()
    {
        var closed = CreateWaiting().Accept("user1").Close("user1");
        var cancelled = CreateWaiting().Cancel("void-user", "reason", new DateTime(2026, 7, 12));

        Action cancelClosed = () => closed.Cancel("void-user", "reason", new DateTime(2026, 7, 12));
        Action cancelAgain = () => cancelled.Cancel("void-user", "reason", new DateTime(2026, 7, 12));

        cancelClosed.Should().Throw<InvalidOperationException>();
        cancelAgain.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenBlankReason_WhenCancel_ThenThrows(string reason)
    {
        var act = () => CreateWaiting().Cancel("void-user", reason, new DateTime(2026, 7, 12));

        act.Should().Throw<ArgumentException>();
    }
}
