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
    public void SaveChanges_NewActiveAllocation_WithDualClassAndTarif_ProjectsBillingKelasAndTarifToLegacyTable()
    {
        var allocation = Active(physicalKelasId: "VIP", billingKelasId: "KL3", tarif: 250000);
        var legacy = Legacy(allocation);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        Sut().SaveChanges(allocation, legacy);

        _legacy.Verify(x => x.Insert(It.Is<PakaiBedDto>(d =>
            d.fs_kd_trs == allocation.PakaiBedId &&
            d.fs_kd_kelas == "KL3" &&
            d.fn_tarif == 250000)), Times.Once);

        _header.Verify(x => x.Insert(It.Is<PakaiBedAlokasiDto>(d =>
            d.BillingKelasId == "KL3" &&
            d.PhysicalKelasId == "VIP" &&
            d.Tarif == 250000)), Times.Once);
    }

    [Fact]
    public void SaveChanges_ReleasedAllocation_SynchronizesExitTimeToBothTables()
    {
        var active = Active(physicalKelasId: "VIP", billingKelasId: "KL3", tarif: 250000);
        var outTime = At.AddDays(2);
        var released = active.Release(outTime, "Pulang Sembuh", "USR-OUT");
        var legacy = Legacy(active);

        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(PakaiBedAlokasiDto.FromModel(active));
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns(LegacyDto(active));
        _alokasiDalUpdateConditionalReturns(1);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(active.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        Sut().SaveChanges(released, legacy);

        _legacy.Verify(x => x.Update(It.Is<PakaiBedDto>(d =>
            d.fs_kd_trs == released.PakaiBedId &&
            d.fd_tgl_out == outTime.ToString("yyyy-MM-dd") &&
            d.fs_jam_out == outTime.ToString("HH:mm:ss") &&
            d.fs_kd_petugas_out == "USR-OUT" &&
            d.fs_kd_kelas == "KL3" &&
            d.fn_tarif == 250000)), Times.Once);

        _header.Verify(x => x.UpdateConditional(
            It.Is<PakaiBedAlokasiDto>(d => d.PakaiBedStatus == (int)PakaiBedStatusEnum.Released),
            active.Version), Times.Once);

        _transitions.Verify(x => x.Insert(It.Is<IEnumerable<PakaiBedTransisiDto>>(list =>
            list.Any(t => t.PakaiBedTransition == (int)PakaiBedTransisiEnum.Released))), Times.Once);
    }

    [Fact]
    public void SaveChanges_CancelledAllocation_SynchronizesVoidToBothTables()
    {
        var active = Active(physicalKelasId: "VIP", billingKelasId: "KL3", tarif: 250000);
        var voidTime = At.AddDays(1);
        var cancelled = active.Void(voidTime, "Batal Masuk", "USR-VOID");
        var legacy = Legacy(active);

        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(PakaiBedAlokasiDto.FromModel(active));
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns(LegacyDto(active));
        _alokasiDalUpdateConditionalReturns(1);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(active.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        Sut().SaveChanges(cancelled, legacy);

        _legacy.Verify(x => x.Update(It.Is<PakaiBedDto>(d =>
            d.fs_kd_trs == cancelled.PakaiBedId &&
            d.fd_tgl_void == voidTime.ToString("yyyy-MM-dd") &&
            d.fs_jam_void == voidTime.ToString("HH:mm:ss") &&
            d.fs_kd_petugas_void == "USR-VOID" &&
            d.fs_kd_kelas == "KL3" &&
            d.fn_tarif == 250000)), Times.Once);

        _header.Verify(x => x.UpdateConditional(
            It.Is<PakaiBedAlokasiDto>(d => d.PakaiBedStatus == (int)PakaiBedStatusEnum.Cancelled),
            active.Version), Times.Once);

        _transitions.Verify(x => x.Insert(It.Is<IEnumerable<PakaiBedTransisiDto>>(list =>
            list.Any(t => t.PakaiBedTransition == (int)PakaiBedTransisiEnum.Cancelled))), Times.Once);
    }

    [Fact]
    public void SaveChanges_SingleParameterOverload_LoadsExistingLegacyAndProjectsUpdates()
    {
        var active = Active("VIP", "KL3", 250000);
        var outTime = At.AddDays(2);
        var released = active.Release(outTime, "Pulang Sembuh", "USR-OUT");

        _legacy.Setup(x => x.GetData(It.Is<IPakaiBed>(k => k.PakaiBedId == released.PakaiBedId)))
            .Returns(LegacyDto(active));
        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(PakaiBedAlokasiDto.FromModel(active));
        _alokasiDalUpdateConditionalReturns(1);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(active.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        Sut().SaveChanges(released);

        _legacy.Verify(x => x.Update(It.Is<PakaiBedDto>(d =>
            d.fs_kd_trs == released.PakaiBedId &&
            d.fd_tgl_out == outTime.ToString("yyyy-MM-dd") &&
            d.fs_jam_out == outTime.ToString("HH:mm:ss") &&
            d.fs_kd_petugas_out == "USR-OUT")), Times.Once);

        _header.Verify(x => x.UpdateConditional(
            It.Is<PakaiBedAlokasiDto>(d => d.PakaiBedStatus == (int)PakaiBedStatusEnum.Released),
            active.Version), Times.Once);
    }

    [Fact]
    public void SaveChanges_SingleParameterOverload_ThrowsIntegrityWhenLegacyNotFound()
    {
        var released = Active().Release(At.AddDays(1), "Checkout", "USR-OUT");
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns((PakaiBedDto?)null);

        var act = () => Sut().SaveChanges(released);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("INTEGRITY_CONFLICT");
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

    [Fact]
    public void SaveChanges_ConditionalUpdateFails_ThrowsConcurrencyConflict()
    {
        var active = Active();
        var released = active.Release(At.AddDays(1), "Checkout", "USR-OUT");
        var legacy = Legacy(active);

        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(PakaiBedAlokasiDto.FromModel(active));
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns(LegacyDto(active));
        _alokasiDalUpdateConditionalReturns(0); // concurrency conflict
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(active.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        var act = () => Sut().SaveChanges(released, legacy);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public void LoadEntity_ForReleasedAllocation_LoadsWithoutFalsePositiveValidation()
    {
        var active = Active("VIP", "KL3", 250000);
        var releaseTime = At.AddDays(2);
        var released = active.Release(releaseTime, "Pulang Sembuh", "USR-OUT");

        var headerDto = PakaiBedAlokasiDto.FromModel(released);
        var legacyDto = LegacyDto(released) with
        {
            fd_tgl_out = releaseTime.ToString("yyyy-MM-dd"),
            fs_jam_out = releaseTime.ToString("HH:mm:ss"),
            fs_kd_petugas_out = "USR-OUT"
        };

        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(headerDto);
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns(legacyDto);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(released.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        var result = Sut().LoadEntity(released);

        result.HasValue.Should().BeTrue();
        result.Value.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Released);
    }

    [Fact]
    public void LoadEntity_ForCancelledAllocation_LoadsWithoutFalsePositiveValidation()
    {
        var active = Active("VIP", "KL3", 250000);
        var voidTime = At.AddDays(1);
        var cancelled = active.Void(voidTime, "Batal Rawat", "USR-VOID");

        var headerDto = PakaiBedAlokasiDto.FromModel(cancelled);
        var legacyDto = LegacyDto(cancelled) with
        {
            fd_tgl_void = voidTime.ToString("yyyy-MM-dd"),
            fs_jam_void = voidTime.ToString("HH:mm:ss"),
            fs_kd_petugas_void = "USR-VOID"
        };

        _header.Setup(x => x.GetData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(headerDto);
        _legacy.Setup(x => x.GetData(It.IsAny<IPakaiBed>()))
            .Returns(legacyDto);
        _transitions.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(cancelled.ListTransition.Select(PakaiBedTransisiDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns([]);

        var result = Sut().LoadEntity(cancelled);

        result.HasValue.Should().BeTrue();
        result.Value.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Cancelled);
    }

    [Fact]
    public void ValidateComposite_ActiveAllocationWithVoidedLegacy_ThrowsIntegrityConflict()
    {
        var active = Active();
        var voidedLegacy = Legacy(active);
        voidedLegacy.Void("USR-VOID", At.AddHours(1));

        var act = () => Sut().SaveChanges(active, voidedLegacy);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("INTEGRITY_CONFLICT");
    }

    [Fact]
    public void ValidateComposite_ActiveAllocationWithLegacyAlreadyOut_ThrowsIntegrityConflict()
    {
        var active = Active();
        var outLegacy = new PakaiBedModel(
            active.PakaiBedId,
            new PeriodePakaiBedType(
                new AuditInfoType(active.AssignedBy, active.StartedAt!.Value),
                new AuditInfoType("USR-OUT", At.AddHours(2))),
            new RegReff(active.RegId, active.PasienId, "Patient"),
            new LayananReff("LAY-1", "Ward"),
            new BedReff(active.BedId, "Bed", true),
            new TipeKamarReff("TYPE-1", "Type", true),
            new KelasReff("CLS-1", "Class"), 100,
            AuditTrailType.Default);

        var act = () => Sut().SaveChanges(active, outLegacy);

        act.Should().Throw<PakaiBedPersistenceException>()
            .Which.Code.Should().Be("INTEGRITY_CONFLICT");
    }

    private PakaiBedAlokasiRepo Sut() =>
        new(_header.Object, _transitions.Object, _corrections.Object, _legacy.Object);

    private void _alokasiDalUpdateConditionalReturns(int count)
    {
        _header.Setup(x => x.UpdateConditional(It.IsAny<PakaiBedAlokasiDto>(), It.IsAny<int>()))
            .Returns(count);
    }

    private static PakaiBedAlokasiModel Proposed(
        string physicalKelasId = "",
        string billingKelasId = "",
        decimal tarif = 0) => PakaiBedAlokasiModel.Propose(
        "REG-1", "P-1", "BG", "KMR-1", "BED-1", "WTL-1", "REQ-1",
        PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
        "USR-1", At, At.AddMinutes(1), "Proposal",
        physicalKelasId: physicalKelasId,
        billingKelasId: billingKelasId,
        tarif: tarif);

    private static PakaiBedAlokasiModel Active(
        string physicalKelasId = "",
        string billingKelasId = "",
        decimal tarif = 0) => Proposed(physicalKelasId, billingKelasId, tarif).Assign(
        "USR-2", At.AddMinutes(5), At.AddMinutes(6), "Assignment", "EVIDENCE-1");

    private static PakaiBedModel Legacy(PakaiBedAlokasiModel allocation) => new(
        allocation.PakaiBedId,
        new PeriodePakaiBedType(
            new AuditInfoType(allocation.AssignedBy, allocation.StartedAt!.Value), AuditInfoType.Default),
        new RegReff(allocation.RegId, allocation.PasienId, "Patient"),
        new LayananReff("LAY-1", "Ward"),
        new BedReff(allocation.BedId, "Bed", true),
        new TipeKamarReff("TYPE-1", "Type", true),
        new KelasReff(string.IsNullOrWhiteSpace(allocation.BillingKelasId) ? "CLS-1" : allocation.BillingKelasId, "Class"),
        allocation.Tarif > 0 ? allocation.Tarif : 100,
        AuditTrailType.Default);

    private static PakaiBedDto LegacyDto(PakaiBedAlokasiModel allocation, string kelas = "CLS-1", decimal tarif = 100) => new(
        fs_kd_trs: allocation.PakaiBedId,
        fd_tgl_in: allocation.StartedAt!.Value.ToString("yyyy-MM-dd"),
        fs_jam_in: allocation.StartedAt!.Value.ToString("HH:mm:ss"),
        fs_kd_petugas: allocation.AssignedBy,
        fd_tgl_out: "3000-01-01",
        fs_jam_out: "00:00:00",
        fs_kd_petugas_out: "",
        fs_kd_reg: allocation.RegId,
        fs_kd_layanan: "LAY-1",
        fs_kd_layanan_dk: "",
        fs_kd_kamar_tipe: "TYPE-1",
        fs_kd_bed: allocation.BedId,
        fn_tarif: allocation.Tarif > 0 ? allocation.Tarif : tarif,
        fd_tgl_void: "3000-01-01",
        fs_jam_void: "00:00:00",
        fs_kd_petugas_void: "",
        fd_tgl_entry: "2026-07-17",
        fs_jam_entry: "08:00:00",
        fs_kd_kelas: string.IsNullOrWhiteSpace(allocation.BillingKelasId) ? kelas : allocation.BillingKelasId,
        fs_mr: allocation.PasienId,
        fs_nm_pasien: "Patient",
        fs_nm_layanan: "Ward",
        fs_nm_bed: "Bed",
        fb_bed_aktif: true,
        fs_nm_kamar_tipe: "Type",
        fb_kamar_tipe_aktif: true,
        fs_nm_kelas: "Class");
}
