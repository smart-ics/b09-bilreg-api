using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
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
        var model = new RegModel(
            "RG00000002",
            new DateOnly(2026, 7, 7),
            new AuditInfoType("user1", new DateTime(2026, 7, 7, 8, 0, 0)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
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
            [],
            new KelasDkType("1", "Kelas DK 1"),
            new BangsalReff("B1", "Bangsal 1"));
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
