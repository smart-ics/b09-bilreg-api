using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapWithRegResolverTest
{
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IGetGrupJaminanJetliService> _getGrupJaminanJetliService = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly AntrianMapWithRegResolver _sut;

    private readonly JadwalPraktekType _jadwal;
    private readonly DateOnly _tgl;

    public AntrianMapWithRegResolverTest()
    {
        _tgl = new DateOnly(2025, 10, 24);
        _jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "JADWAL01",
            Dokter = new PpaReff("DOK01", "dr. Budi"),
            Layanan = new LayananReff("LAY01", "Poli Umum"),
            Hari = _tgl.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 10,
            AntrianPattern = AntrianPatternType.Default
        };

        _featureResolver.Setup(x => x.UseResolver).Returns(false);
        _sut = new AntrianMapWithRegResolver(
            _antrianMapRepo.Object,
            _getGrupJaminanJetliService.Object,
            _featureResolver.Object);
    }

    [Fact]
    public void Resolve_FirstTimeRegistration_AllocatesNewFreeSlot()
    {
        // Arrange
        var antrianMap = AntrianMapModel.CreateFromJadwal(_jadwal, _tgl);
        _antrianMapRepo
            .Setup(x => x.Find(_jadwal, _tgl))
            .Returns(MayBe.From(antrianMap));
        _getGrupJaminanJetliService
            .Setup(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()))
            .Returns(new GetGrupJaminanJetliResponse(TipeJaminanType.Default.ToReff(), "NON-JKN", "UMUM"));

        var reg = CreateReg("RG00000001", "Pasien A", "P001");

        // Act
        var result = _sut.Resolve(_jadwal, _tgl, reg);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var (map, allocatedSlot) = result.Value;
        allocatedSlot.Should().NotBeNull();
        allocatedSlot.NoUrut.Should().Be(1);
        allocatedSlot.ReffId.Should().Be("RG00000001");
        allocatedSlot.PasienName.Should().Be("Pasien A");
        allocatedSlot.PasienId.Should().Be("P001");
        allocatedSlot.IsTerpakai.Should().BeTrue();

        map.ListMap.Count(x => x.IsTerpakai).Should().Be(1);
        map.ListMap.First(x => x.NoUrut == 2).IsTerpakai.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenAlreadyRegisteredWithSameRegId_ReusesExistingSlot_AndDoesNotAllocateNewSlot()
    {
        // Arrange
        var antrianMap = AntrianMapModel.CreateFromJadwal(_jadwal, _tgl);
        _antrianMapRepo
            .Setup(x => x.Find(_jadwal, _tgl))
            .Returns(MayBe.From(antrianMap));
        _getGrupJaminanJetliService
            .Setup(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()))
            .Returns(new GetGrupJaminanJetliResponse(TipeJaminanType.Default.ToReff(), "NON-JKN", "UMUM"));

        var reg = CreateReg("RG00000001", "Pasien A", "P001");

        // First resolution: allocates slot 1
        var firstResult = _sut.Resolve(_jadwal, _tgl, reg);
        firstResult.IsSuccess.Should().BeTrue();
        firstResult.Value.Item2.NoUrut.Should().Be(1);

        // Act - Second resolution with the exact same RegId (e.g. retry / duplicate registration flow)
        var updatedReg = CreateReg("RG00000001", "Pasien A Mutated", "P001");
        var secondResult = _sut.Resolve(_jadwal, _tgl, updatedReg);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        var (map, reusedSlot) = secondResult.Value;
        reusedSlot.Should().NotBeNull();
        reusedSlot.NoUrut.Should().Be(1); // Same slot reused!
        reusedSlot.ReffId.Should().Be("RG00000001");
        reusedSlot.PasienName.Should().Be("Pasien A Mutated");
        reusedSlot.IsTerpakai.Should().BeTrue();

        // Ensure no new slot was taken from quota
        map.ListMap.Count(x => x.IsTerpakai).Should().Be(1);
        map.ListMap.First(x => x.NoUrut == 2).IsTerpakai.Should().BeFalse();
        map.ListMap.First(x => x.NoUrut == 2).ReffId.Trim().Should().BeEmpty();

        // Idempotency guard should have returned before calling Jaminan service on second resolution
        _getGrupJaminanJetliService.Verify(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()), Times.Once);
    }

    [Fact]
    public void Resolve_WhenExistingSlotAlreadyPresentInMap_ReturnsExistingSlotImmediately()
    {
        // Arrange: Map where slot 3 is already assigned to RG00000002
        var antrianMap = AntrianMapModel.CreateFromJadwal(_jadwal, _tgl);
        var slot3 = antrianMap.ListMap.First(x => x.NoUrut == 3);
        slot3.SetPasien("Existing Patient", "P002", "RG00000002");

        _antrianMapRepo
            .Setup(x => x.Find(_jadwal, _tgl))
            .Returns(MayBe.From(antrianMap));

        var reg = CreateReg("RG00000002", "Existing Patient Updated", "P002");

        // Act
        var result = _sut.Resolve(_jadwal, _tgl, reg);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Item2.NoUrut.Should().Be(3);
        result.Value.Item2.ReffId.Should().Be("RG00000002");
        result.Value.Item2.PasienName.Should().Be("Existing Patient Updated");
        result.Value.Item1.ListMap.Count(x => x.IsTerpakai).Should().Be(1);

        _getGrupJaminanJetliService.Verify(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()), Times.Never);
    }

    [Fact]
    public void Resolve_WhenSlot1FreeInMapButOccupiedInSession_SkipsSlot1AcrossFlagStrictTimeAndAuto()
    {
        // Arrange: Flag UMUM at 1..2, Strict Time 08:00 at 3, AUTO at 4
        var detils = new[]
        {
            new AntrianMapDetilModel(1, "", "", "", "UMUM", false),
            new AntrianMapDetilModel(2, "", "", "", "UMUM", false),
            new AntrianMapDetilModel(3, "", "", "", "08:00", false),
            new AntrianMapDetilModel(4, "", "", "", "AUTO", false)
        };
        var antrianMap = new AntrianMapModel(
            "MAP01", _jadwal.JadwalPraktekId, _jadwal.Dokter, _jadwal.Layanan,
            _tgl, _jadwal.JamMulai, _jadwal.JamMulai, _jadwal.AntrianPattern, 4, detils);

        _antrianMapRepo.Setup(x => x.Find(_jadwal, _tgl)).Returns(MayBe.From(antrianMap));
        _getGrupJaminanJetliService
            .Setup(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()))
            .Returns(new GetGrupJaminanJetliResponse(TipeJaminanType.Default.ToReff(), "NON-JKN", "UMUM"));

        // Slot 1 is occupied in BILRG_AntrianEntry (e.g. HiDok booking), even though free in map
        var r1 = _sut.Resolve(_jadwal, _tgl, CreateReg("RG01", "P1", "P01"), [1]);
        r1.Value.Item2.NoUrut.Should().Be(2);

        // Now slot 2 is taken; slot 3 (strict time) is occupied in session -> falls through to slot 4 (AUTO)
        var r2 = _sut.Resolve(_jadwal, _tgl, CreateReg("RG02", "P2", "P02"), [1, 3]);
        r2.Value.Item2.NoUrut.Should().Be(4);

        // Now all seeded slots (1..4) are taken or occupied, and 5 is also occupied in session -> overflow AddAuto picks 6
        var r3 = _sut.Resolve(_jadwal, _tgl, CreateReg("RG03", "P3", "P03"), [1, 3, 5]);
        r3.Value.Item2.NoUrut.Should().Be(6);
    }

    [Fact]
    public void Resolve_WhenExistingSlotCollidesWithOccupiedSession_DoesNotReuseCollidingSlot()
    {
        var antrianMap = AntrianMapModel.CreateFromJadwal(_jadwal, _tgl);
        antrianMap.ListMap.First(x => x.NoUrut == 1).SetPasien("Old", "P01", "RG00000001");

        _antrianMapRepo.Setup(x => x.Find(_jadwal, _tgl)).Returns(MayBe.From(antrianMap));
        _getGrupJaminanJetliService
            .Setup(x => x.Execute(It.IsAny<GetGrupJaminanJetliRequest>()))
            .Returns(new GetGrupJaminanJetliResponse(TipeJaminanType.Default.ToReff(), "NON-JKN", "UMUM"));

        var result = _sut.Resolve(_jadwal, _tgl, CreateReg("RG00000001", "Old", "P01"), [1]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Item2.NoUrut.Should().Be(2);
    }

    private static RegModel CreateReg(string regId, string pasienName, string pasienId) => new(
        regId,
        new DateOnly(2025, 10, 24),
        new AuditInfoType("tester", new DateTime(2025, 10, 24, 8, 0, 0)),
        AuditInfoType.Default,
        AuditInfoType.Default,
        AuditInfoType.Default,
        JenisRegEnum.RegJalan,
        new PasienReff(pasienId, pasienName, new DateOnly(1990, 1, 1), "L"),
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
