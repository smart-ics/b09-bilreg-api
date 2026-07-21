using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.Shared;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.Shared.BusinessDateFeature;

public class TglJamProviderTest
{
    private static readonly DateTime SqlNow = new(2026, 7, 20, 10, 15, 30, 250);

    [Fact]
    public void SystemMode_ReturnsSqlServerTime()
    {
        var clock = new CountingSqlServerClock(SqlNow);
        var provider = CreateProvider(clock, BusinessDateMode.System);

        provider.Now.Should().BeCloseTo(SqlNow, TimeSpan.FromMilliseconds(100));
        provider.IsSimulation.Should().BeFalse();
    }

    [Fact]
    public void FixedMode_ReplacesOnlyDateAndPreservesTimeOfDay()
    {
        var clock = new CountingSqlServerClock(SqlNow);
        var fixedDate = new DateOnly(2025, 5, 3);
        var provider = CreateProvider(clock, BusinessDateMode.Fixed, fixedDate);

        var actual = provider.Now;

        DateOnly.FromDateTime(actual).Should().Be(fixedDate);
        actual.TimeOfDay.Should().BeCloseTo(SqlNow.TimeOfDay, TimeSpan.FromMilliseconds(100));
        provider.IsSimulation.Should().BeTrue();
    }

    [Fact]
    public void Now_ContinuesRunning()
    {
        var provider = CreateProvider(
            new CountingSqlServerClock(SqlNow),
            BusinessDateMode.Fixed,
            new DateOnly(2025, 5, 3));
        var first = provider.Now;

        Thread.Sleep(30);

        provider.Now.Should().BeAfter(first);
    }

    [Fact]
    public void BusinessAndSystemTime_InitializeSqlClockOnlyOncePerProviderScope()
    {
        var clock = new CountingSqlServerClock(SqlNow);
        var provider = CreateProvider(
            clock,
            BusinessDateMode.Fixed,
            new DateOnly(2025, 5, 3));

        _ = provider.Now;
        _ = provider.SystemNow;
        _ = provider.BusinessNow;

        clock.CallCount.Should().Be(1);
    }

    [Fact]
    public void FixedBusinessDate_FlowsFromApplicationClockIntoAdmissionDomain()
    {
        var provider = CreateProvider(
            new CountingSqlServerClock(SqlNow),
            BusinessDateMode.Fixed,
            new DateOnly(2025, 5, 3));

        // This is the Application-layer boundary: capture once, then pass explicitly.
        var admittedAt = provider.Now;
        var admission = AdmissionModel.Admit(
            new PasienReff("PSN-1", "Patient", new DateOnly(1990, 1, 1), "L"),
            KelasDkType.Default,
            new BangsalReff("BGS-1", "Ward"),
            null,
            null,
            "tester",
            admittedAt);

        admission.AdmissionDate.Should().Be(admittedAt);
        DateOnly.FromDateTime(admission.AdmissionDate).Should().Be(new DateOnly(2025, 5, 3));
        admission.AdmissionDate.TimeOfDay.Should().BeCloseTo(SqlNow.TimeOfDay, TimeSpan.FromMilliseconds(100));
        admission.AuditTrail.Created.Timestamp.Should().Be(admittedAt);
    }

    private static TglJamProvider CreateProvider(
        ISqlServerClock clock,
        BusinessDateMode mode,
        DateOnly? fixedDate = null) =>
        new(clock, Options.Create(new BusinessDateOptions
        {
            Mode = mode,
            FixedDate = fixedDate
        }));

    private sealed class CountingSqlServerClock(DateTime value) : ISqlServerClock
    {
        public int CallCount { get; private set; }

        public DateTime GetDate()
        {
            CallCount++;
            return value;
        }
    }
}
