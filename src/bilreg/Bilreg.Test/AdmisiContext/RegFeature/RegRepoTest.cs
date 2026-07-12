using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegRepoTest
{
    private readonly Mock<IRegDal> _regDal = new();
    private readonly Mock<IRegJaminanDal> _regJaminanDal = new();
    private readonly Mock<IRegKomponenDal> _regKomponenDal = new();
    private readonly Mock<IRegHistoryDokterDal> _regHistoryDokterDal = new();
    private readonly Mock<IKelasRepo> _kelasRepo = new();
    private readonly Mock<IBangsalRepo> _bangsalRepo = new();

    private RegRepo CreateSut() =>
        new(
            _regDal.Object,
            _regJaminanDal.Object,
            _regKomponenDal.Object,
            _regHistoryDokterDal.Object,
            _kelasRepo.Object,
            _bangsalRepo.Object);

    [Fact]
    public void GivenLegacyRegInap_WhenLoadEntity_ThenKelasDkAndBangsalAreEnriched()
    {
        var dto = CreateRegDto(JenisRegEnum.RegInap);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns(dto);
        _regJaminanDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns(new RegJaminanDto(dto.fs_kd_reg, "-", "-", "-"));
        _regKomponenDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns([]);
        _kelasRepo
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS1")))
            .Returns(MayBe.From(new KelasType("KLS1", "Kelas 1", true, new KelasDkType("1", "Kelas DK 1"))));
        _bangsalRepo
            .Setup(x => x.ListData(It.Is<ILayananKey>(l => l.LayananId == "LYN1")))
            .Returns([new BangsalType("B1", "Bangsal 1", RoomCatType.Default, new LayananReff("LYN1", "Layanan 1"))]);

        var result = CreateSut().LoadEntity(RegModel.Key(dto.fs_kd_reg));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: reg =>
            {
                reg.KelasDk.KelasDkId.Should().Be("1");
                reg.Bangsal.BangsalId.Should().Be("B1");
            },
            onNone: () => Assert.Fail("Expected RegModel"));
    }

    [Fact]
    public void GivenLegacyRegInapWithMultipleBangsalForLayanan_WhenLoadEntity_ThenThrows()
    {
        var dto = CreateRegDto(JenisRegEnum.RegInap);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns(dto);
        _kelasRepo
            .Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(new KelasType("KLS1", "Kelas 1", true, new KelasDkType("1", "Kelas DK 1"))));
        _bangsalRepo
            .Setup(x => x.ListData(It.IsAny<ILayananKey>()))
            .Returns([
                new BangsalType("B1", "Bangsal 1", RoomCatType.Default, new LayananReff("LYN1", "Layanan 1")),
                new BangsalType("B2", "Bangsal 2", RoomCatType.Default, new LayananReff("LYN1", "Layanan 1"))
            ]);

        var act = () => CreateSut().LoadEntity(RegModel.Key(dto.fs_kd_reg));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*lebih dari satu Bangsal*");
    }

    [Fact]
    public void GivenRegWithSyncFields_WhenSaveChanges_ThenRegDtoStillUsesLegacyRegistrationFields()
    {
        RegDto? inserted = null;
        var model = CreateRegModel(JenisRegEnum.RegInap, "RG00000002", []);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((RegDto)null!);
        _regDal.Setup(x => x.Insert(It.IsAny<RegDto>()))
            .Callback<RegDto>(dto => inserted = dto);
        _regKomponenDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns([]);

        CreateSut().SaveChanges(model);

        inserted.Should().NotBeNull();
        inserted!.fs_kd_reg.Should().Be("RG00000002");
        inserted.fs_kd_kelas.Should().Be("KLS1");
        inserted.fs_kd_layanan.Should().Be("LYN1");
    }

    [Fact]
    public void GivenRawatInapRegistration_WhenPersisted_ThenDoesNotCreateRegistrasi2()
    {
        var leftover = new RegKomponenDto(
            "RGINAP0001", "KP1", 10000m, 0m, "DR1", "Komponen 1", "Dokter 1");
        var model = CreateRegModel(
            JenisRegEnum.RegInap,
            "RGINAP0001",
            [
                new RegKomponenType(
                    new KomponenReff("KP1", "Komponen 1"),
                    new PpaReff("DR1", "Dokter 1"),
                    10000m,
                    0m)
            ]);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((RegDto)null!);
        // Leftover rows must not be re-inserted for inpatient, even if DAL still has them.
        _regKomponenDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns([leftover]);

        CreateSut().SaveChanges(model);

        _regKomponenDal.Verify(x => x.Delete(It.Is<IRegKey>(k => k.RegId == "RGINAP0001")), Times.Once);
        _regKomponenDal.Verify(x => x.Insert(It.IsAny<IEnumerable<RegKomponenDto>>()), Times.Never);
        _regJaminanDal.Verify(x => x.Insert(It.IsAny<RegJaminanDto>()), Times.Once);
    }

    [Fact]
    public void GivenRawatJalanRegistration_WhenPersisted_ThenPreservesKomponenFromDal()
    {
        var existing = new[]
        {
            new RegKomponenDto("RGJALAN001", "KP1", 10000m, 0m, "DR1", "Komponen 1", "Dokter 1"),
            new RegKomponenDto("RGJALAN001", "KP2", 5000m, 0m, "DR1", "Komponen 2", "Dokter 1"),
        };
        var model = CreateRegModel(JenisRegEnum.RegJalan, "RGJALAN001", []);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((RegDto)null!);
        _regKomponenDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns(existing);

        CreateSut().SaveChanges(model);

        _regKomponenDal.Verify(x => x.Delete(It.Is<IRegKey>(k => k.RegId == "RGJALAN001")), Times.Once);
        _regKomponenDal.Verify(
            x => x.Insert(It.Is<IEnumerable<RegKomponenDto>>(list =>
                list.Count() == 2
                && list.Any(i => i.fs_kd_detil_tarif == "KP1")
                && list.Any(i => i.fs_kd_detil_tarif == "KP2"))),
            Times.Once);
    }

    [Fact]
    public void GivenIgdRegistration_WhenPersisted_ThenPreservesKomponenFromDal()
    {
        var existing = new[]
        {
            new RegKomponenDto("RGIGD00001", "KP1", 15000m, 0m, "DR1", "Komponen 1", "Dokter 1"),
        };
        var model = CreateRegModel(JenisRegEnum.Darurat, "RGIGD00001", []);
        _regDal.Setup(x => x.GetData(It.IsAny<IRegKey>())).Returns((RegDto)null!);
        _regKomponenDal.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns(existing);

        CreateSut().SaveChanges(model);

        _regKomponenDal.Verify(x => x.Delete(It.Is<IRegKey>(k => k.RegId == "RGIGD00001")), Times.Once);
        _regKomponenDal.Verify(
            x => x.Insert(It.Is<IEnumerable<RegKomponenDto>>(list =>
                list.Count() == 1 && list.Single().fs_kd_detil_tarif == "KP1")),
            Times.Once);
    }

    private static RegModel CreateRegModel(
        JenisRegEnum jenisReg,
        string regId,
        IEnumerable<RegKomponenType> listKomponen) =>
        new(
            regId,
            new DateOnly(2026, 7, 7),
            new AuditInfoType("user1", new DateTime(2026, 7, 7, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("KLS1", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("LYN1", "Layanan 1"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            listKomponen,
            new KelasDkType("1", "Kelas DK 1"),
            new BangsalReff("B1", "Bangsal 1"));

    private static RegDto CreateRegDto(JenisRegEnum jenisReg) =>
        new(
            "RG00000001",
            "2026-07-07",
            "08:00:00",
            "user1",
            "3000-01-01",
            "00:00:00",
            "",
            "3000-01-01",
            "00:00:00",
            "",
            "3000-01-01",
            "00:00:00",
            "",
            jenisReg.ToNumberString(),
            "P0001",
            "00000",
            "KLS1",
            "-",
            "-",
            "DR1",
            "LYN1",
            "KR1",
            "-",
            "-",
            "-",
            "2026-07-07 08:00:00",
            "3000-01-01 00:00:00",
            "Pasien Test",
            "1990-01-01",
            "L",
            "Bayar Sendiri",
            "Kelas 1",
            "-",
            "-",
            "Dokter 1",
            "Layanan 1",
            "Karcis 1");
}
