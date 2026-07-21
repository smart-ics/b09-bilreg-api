using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedAlokasiRepoTest
{
    private static readonly DateTime At = new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IPakaiBedAlokasiDal> _header = new();
    private readonly Mock<IPakaiBedTransisiDal> _transitions = new();
    private readonly Mock<IPakaiBedKoreksiDal> _corrections = new();
    private readonly Mock<IPakaiBedDal> _legacy = new();

    [Fact]
    public void SaveChanges_NewActiveAllocation_InsertsCompositeRows()
    {
        var allocation = Active();
        var legacy = Legacy(allocation);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        Sut().SaveChanges(allocation, legacy);

        _legacy.Verify(x => x.Insert(It.Is<PakaiBedDto>(d => d.fs_kd_trs == allocation.PakaiBedId)), Times.Once);
        _header.Verify(x => x.Insert(It.Is<PakaiBedAlokasiDto>(d => d.PakaiBedStatus == (int)PakaiBedStatusEnum.Active)), Times.Once);
        _transitions.Verify(x => x.Insert(It.Is<IEnumerable<PakaiBedTransisiDto>>(d => d.Count() == 2)), Times.Once);
    }

    [Fact]
    public void SaveChanges_ProposedAllocation_RejectsPersistence()
    {
        var proposed = Proposed();
        var legacy = Legacy(proposed with { StartedAt = At, AssignedBy = "USR-2" });

        var act = () => Sut().SaveChanges(proposed, legacy);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("INTEGRITY_CONFLICT");
        _header.VerifyNoOtherCalls();
        _legacy.VerifyNoOtherCalls();
    }

    [Fact]
    public void SaveChanges_StaleVersion_ThrowsConcurrencyConflict()
    {
        var allocation = Active();
        var legacy = Legacy(allocation);
        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(PakaiBedAlokasiDto.FromModel(allocation) with { Version = 4 });

        var act = () => Sut().SaveChanges(allocation, legacy);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("CONCURRENCY_CONFLICT");
        _legacy.Verify(x => x.Insert(It.IsAny<PakaiBedDto>()), Times.Never);
    }

    private PakaiBedAlokasiRepo Sut() =>
        new(_header.Object, _transitions.Object, _corrections.Object, _legacy.Object);

    private static PakaiBedAlokasiModel Proposed() => PakaiBedAlokasiModel.Propose(
        "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
        PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
        "USR-1", At, At.AddMinutes(1), "Proposal");

    private static PakaiBedAlokasiModel Active() => Proposed().Assign(
        "USR-2", At.AddMinutes(5), At.AddMinutes(6), "Assignment", "EVIDENCE-1");

    private static PakaiBedModel Legacy(PakaiBedAlokasiModel allocation) => new(
        allocation.PakaiBedId,
        new PeriodePakaiBedType(
            new AuditInfoType(allocation.AssignedBy, allocation.StartedAt!.Value), AuditInfoType.Default),
        new RegReff(allocation.RegId, allocation.PasienId, "Patient"),
        new LayananReff("LAY-1", "Ward"),
        new BedReff(allocation.BedId, "Bed", true),
        new TipeKamarReff("TYPE-1", "Type", true),
        new KelasReff("CLS-1", "Class"), 100,
        AuditTrailType.Default);
}
