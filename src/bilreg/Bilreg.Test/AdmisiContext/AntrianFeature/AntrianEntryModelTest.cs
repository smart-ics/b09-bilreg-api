using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianEntryModelTest
{
    private static readonly DateTime CreatedAt = new(2025, 8, 3, 6, 51, 0);
    private static readonly DateTime ServedAt = new(2025, 8, 3, 7, 0, 0);
    private static readonly DateTime DoneAt = new(2025, 8, 3, 7, 30, 0);

    [Fact]
    public void T01_GivenWaiting_WhenDone_ThrowException()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        var actual = () => entry.Done(DoneAt);
        actual.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void T02_GivenServed_WhenDone_ThenSuccess()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);
        var actual = () => entry.Done(DoneAt);
        actual.Should().NotThrow();
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        entry.DoneAt.Should().Be(DoneAt);
    }

    [Fact]
    public void T03_GivenCreateWithoutBusinessTime_ThenThrows()
    {
        var act = () => AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", default);
        act.Should().Throw<ArgumentException>().WithParameterName("createdAt");
    }

    [Fact]
    public void T04_GivenWaiting_WhenServe_ThenInService()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.InService);
        entry.ServedAt.Should().Be(ServedAt);
    }

    [Fact]
    public void T05_GivenDone_WhenServe_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);
        entry.Done(DoneAt);

        var act = () => entry.Serve(DoneAt.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void T06_GivenServeBeforeCreated_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        var act = () => entry.Serve(CreatedAt.AddMinutes(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("servedAt");
    }

    [Fact]
    public void T07_GivenServeEqualCreated_ThenSucceeds()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        var act = () => entry.Serve(CreatedAt);
        act.Should().NotThrow();
    }

    [Fact]
    public void T08_GivenDoneEqualServed_ThenSucceeds()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);
        var act = () => entry.Done(ServedAt);
        act.Should().NotThrow();
    }

    [Fact]
    public void T09_GivenDoneBeforeServed_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);
        var act = () => entry.Done(ServedAt.AddMinutes(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("doneAt");
    }

    [Fact]
    public void T10_GivenServeWithDefaultTime_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        var act = () => entry.Serve(default);
        act.Should().Throw<ArgumentException>().WithParameterName("servedAt");
    }

    [Fact]
    public void T11_GivenInService_WhenCancelRegistration_ThenReturnsToWaiting()
    {
        var entry = AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);
        entry.Serve(ServedAt);

        entry.CancelRegistration();

        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);
        entry.ServedAt.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void T12_GivenWaiting_WhenCancelRegistration_ThenThrows()
    {
        var entry = AntrianEntryModel.Create(
            1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B", CreatedAt);

        var act = entry.CancelRegistration;

        act.Should().Throw<InvalidOperationException>();
    }
}
