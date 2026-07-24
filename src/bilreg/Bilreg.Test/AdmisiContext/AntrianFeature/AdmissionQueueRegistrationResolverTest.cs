using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueRegistrationResolverTest
{
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();

    public AdmissionQueueRegistrationResolverTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
    }

    [Fact]
    public void Resolve_WhenAnonymousInService_ThenCreatesRegistrationTrackerAndCompletes()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueue(createdAt);
        var entry = queue.AddEntry(createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        var reg = CreateReg(DateOnly.FromDateTime(createdAt));

        var result = AdmissionQueueRegistrationResolver.ResolveAndComplete(
            _trackerRepo.Object, queue, entry.NoUrut, reg, createdAt.AddMinutes(20));

        result.RequiresConditionalSave.Should().BeTrue();
        result.Entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        result.Entry.Tracker.PasienTrackerId.Should().Be(result.Tracker.PasienTrackerId);
        result.Tracker.ListEvent.Select(x => x.EventName)
            .Should().ContainInOrder("Check In", "Reg-Start", "REGISTER");
        _trackerRepo.Verify(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()), Times.Never);
    }

    [Fact]
    public void Resolve_WhenAlreadyIdentifiedInService_ThenReusesTrackerAndCompletes()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueue(createdAt);
        var reg = CreateReg(DateOnly.FromDateTime(createdAt));
        var tracker = PasienTrackerModel.Create(reg, createdAt.AddMinutes(-10));
        var entry = queue.AddEntry(tracker, createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));

        var result = AdmissionQueueRegistrationResolver.ResolveAndComplete(
            _trackerRepo.Object, queue, entry.NoUrut, reg, createdAt.AddMinutes(20));

        result.RequiresConditionalSave.Should().BeFalse();
        result.Tracker.Should().BeSameAs(tracker);
        result.Entry.AntrianStatus.Should().Be(AntrianStatusEnum.Done);
        tracker.ListEvent.Count(x => x.EventName == "REGISTER" && x.ReffId == reg.RegId)
            .Should().Be(1);
    }

    private AntrianModel CreateQueue(DateTime createdAt) => new(
        "ADM-Q1", DateOnly.FromDateTime(createdAt), TimeOnly.MinValue, TimeOnly.MaxValue,
        "tag", "Loket Admisi", new ServicePointType("ADM", "Loket Admisi"), [], _sequencer.Object);

    private static RegModel CreateReg(DateOnly regDate) => new(
        "RG00000001",
        regDate,
        new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
        AuditInfoType.Default,
        AuditInfoType.Default,
        AuditInfoType.Default,
        JenisRegEnum.RegJalan,
        PasienModel.Default.ToReff(),
        TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(),
        KelasType.Default.ToReff(),
        CaraMasukDkType.Default,
        RujukanType.Default.ToReff(),
        PpaType.Default.ToReff(),
        LayananType.Default.ToReff(),
        KarcisType.Default.ToReff(),
        RegEligibilityType.Default,
        []);
}
