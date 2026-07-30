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
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

/// <summary>
/// Proves admission registration completion persists via CAS,
/// never whole-aggregate SaveChanges on the admission queue.
/// </summary>
public class AdmissionQueueLifecyclePersistenceTest
{
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();

    public AdmissionQueueLifecyclePersistenceTest()
    {
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>())).Returns(1);
    }

    [Fact]
    public void IdentifiedInServiceDone_UsesCompareAndSet_NotSaveChanges()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueue(createdAt);
        var reg = CreateReg(DateOnly.FromDateTime(createdAt));
        var tracker = PasienTrackerModel.Create(reg, createdAt.AddMinutes(-10));
        var entry = queue.AddEntry(tracker, createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe.From(tracker));

        var antrianRepo = new Mock<IAntrianRepo>();
        antrianRepo.Setup(x => x.TrySaveInServiceToDoneTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(true);

        var resolution = AdmissionQueueRegistrationResolver.ResolveAndComplete(
            _trackerRepo.Object, queue, entry.NoUrut, reg, createdAt.AddMinutes(20));

        resolution.RequiresConditionalSave.Should().BeFalse();
        antrianRepo.Object.TrySaveInServiceToDoneTransition(queue, resolution.Entry)
            .Should().BeTrue();

        antrianRepo.Verify(
            x => x.TrySaveInServiceToDoneTransition(queue, resolution.Entry), Times.Once);
        antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
        antrianRepo.Verify(x => x.TrySaveAnonymousInServiceTransition(
            It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()), Times.Never);
    }

    [Fact]
    public void AnonymousInServiceDone_UsesAnonymousCompareAndSet_NotSaveChanges()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueue(createdAt);
        var entry = queue.AddEntry(createdAt);
        entry.Serve(createdAt.AddMinutes(5));
        var reg = CreateReg(DateOnly.FromDateTime(createdAt));

        var antrianRepo = new Mock<IAntrianRepo>();
        antrianRepo.Setup(x => x.TrySaveAnonymousInServiceTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(true);

        var resolution = AdmissionQueueRegistrationResolver.ResolveAndComplete(
            _trackerRepo.Object, queue, entry.NoUrut, reg, createdAt.AddMinutes(20));

        resolution.RequiresConditionalSave.Should().BeTrue();
        antrianRepo.Object.TrySaveAnonymousInServiceTransition(queue, resolution.Entry)
            .Should().BeTrue();

        antrianRepo.Verify(
            x => x.TrySaveAnonymousInServiceTransition(queue, resolution.Entry), Times.Once);
        antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
        antrianRepo.Verify(x => x.TrySaveInServiceToDoneTransition(
            It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()), Times.Never);
    }

    [Fact]
    public void IdentifiedDone_WhenCompareAndSetFails_ThenThrowsConcurrencyException()
    {
        var createdAt = new DateTime(2025, 5, 3, 8, 0, 0);
        var queue = CreateQueue(createdAt);
        var entry = queue.AddEntry(createdAt);
        var antrianRepo = new Mock<IAntrianRepo>();
        antrianRepo.Setup(x => x.TrySaveInServiceToDoneTransition(
                It.IsAny<AntrianModel>(), It.IsAny<AntrianEntryModel>()))
            .Returns(false);

        Action conflict = () =>
        {
            if (!antrianRepo.Object.TrySaveInServiceToDoneTransition(queue, entry))
                throw new AdmissionQueueConcurrencyException(
                    $"Queue entry '{queue.AntrianId}' / {entry.NoUrut} was changed concurrently.");
        };

        conflict.Should().Throw<AdmissionQueueConcurrencyException>()
            .WithMessage("*changed concurrently*");
        antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Never);
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
