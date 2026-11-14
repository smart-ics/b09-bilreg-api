using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedDtoTest
{
    private readonly RegModel _defaultReg = RegModel.Default;
    private readonly LayananType _defaultLayanan = LayananType.Default with { LayananId = "LYN01", LayananName = "Bangsal Anggrek" };
    private readonly BedType _defaultBed = BedType.Default with { BedId = "BED01", BedName = "Bed 01" };
    private readonly TipeKamarType _defaultTipeKamar = TipeKamarType.Default with { TipeKamarId = "TK01", TipeKamarName = "Standard" };
    private readonly KelasType _defaultKelas = KelasType.Default with { KelasId = "KLS01", KelasName = "Kelas 1" };
    private readonly AuditInfoType _defaultAudit = new("USR01", new DateTime(2026, 11, 11, 10, 0, 0));

    [Fact]
    public void FromModel_ShouldMapLayananDkIdToDto_fs_kd_layanan_dk()
    {
        // Arrange
        var model = PakaiBedModel.Create(
            _defaultReg,
            _defaultAudit,
            _defaultLayanan,
            _defaultBed,
            _defaultTipeKamar,
            _defaultKelas,
            150000m,
            layananDkId: "88");

        // Act
        var dto = PakaiBedDto.FromModel(model);

        // Assert
        dto.fs_kd_layanan_dk.Should().Be("88");
    }

    [Fact]
    public void ToModel_ShouldMapDto_fs_kd_layanan_dk_ToModelLayananDkId()
    {
        // Arrange
        var dto = new PakaiBedDto(
            fs_kd_trs: "PKB001",
            fd_tgl_in: "2026-11-11",
            fs_jam_in: "10:00:00",
            fs_kd_petugas: "USR01",
            fd_tgl_out: "3000-01-01",
            fs_jam_out: "00:00:00",
            fs_kd_petugas_out: "",
            fs_kd_reg: "REG01",
            fs_kd_layanan: "LYN01",
            fs_kd_layanan_dk: "88",
            fs_kd_kamar_tipe: "TK01",
            fs_kd_bed: "BED01",
            fn_tarif: 150000m,
            fd_tgl_void: "3000-01-01",
            fs_jam_void: "00:00:00",
            fs_kd_petugas_void: "",
            fd_tgl_entry: "2026-11-11",
            fs_jam_entry: "10:00:00",
            fs_kd_kelas: "KLS01",
            fs_mr: "MR01",
            fs_nm_pasien: "Pasien Test",
            fs_nm_layanan: "Bangsal Anggrek",
            fs_nm_bed: "Bed 01",
            fb_bed_aktif: true,
            fs_nm_kamar_tipe: "Standard",
            fb_kamar_tipe_aktif: true,
            fs_nm_kelas: "Kelas 1");

        // Act
        var model = dto.ToModel();

        // Assert
        model.LayananDkId.Should().Be("88");
    }
}
