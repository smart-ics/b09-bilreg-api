using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.DepositFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositDtoTest
{
    [Fact]
    public void FromModel_MapsModelProperties_AndDefaultsCashierFields()
    {
        // Arrange
        var audit = new AuditTrailType(
            new AuditInfoType("USER01", "2026-10-22", "10:00:00"),
            new AuditInfoType("USER02", "2026-10-22", "11:00:00"),
            new AuditInfoType("", "3000-01-01", "00:00:00"));

        var model = new DepositModel(
            depositId: "DP0001",
            depositDate: new DateTime(2026, 10, 22, 10, 0, 0),
            reg: new RegReff("REG001", "MR001", "PASIEN 1"),
            layanan: new LayananReff("LAY01", "RAWAT INAP"),
            ketarangan: "Uang muka kamar",
            nilaiDeposit: 500_000m,
            audit: audit);

        // Act
        var dto = DepositDto.FromModel(model);

        // Assert
        dto.fs_kd_trs.Should().Be("DP0001");
        dto.fs_kd_reg.Should().Be("REG001");
        dto.fs_kd_layanan.Should().Be("LAY01");
        dto.fs_keterangan.Should().Be("Uang muka kamar");
        dto.fn_nilai_deposit.Should().Be(500_000m);
        dto.fn_total_deposit.Should().Be(500_000m);
        dto.fs_kd_petugas.Should().Be("USER01");
        dto.fn_kas.Should().Be(0m);
        dto.fn_bank.Should().Be(0m);
        dto.fn_kartu_kredit.Should().Be(0m);
        dto.fs_kd_petugas_kasir.Should().BeEmpty();
        dto.fs_terima_dari.Should().BeEmpty();
        dto.fs_no_kwitansi.Should().BeEmpty();
    }

    [Fact]
    public void ToModel_MapsDtoToDomainModel()
    {
        // Arrange
        var dto = new DepositDto(
            fs_kd_trs: "DP0002",
            fd_tgl_trs: "2026-10-22",
            fs_jam_trs: "10:30:00",
            fd_tgl_jam_trs: "2026-10-22 10:30:00",
            fs_kd_petugas: "KASIR01",
            fs_kd_reg: "REG002",
            fs_kd_layanan: "LAY02",
            fs_keterangan: "Titipan deposit",
            fd_tgl_void: "3000-01-01",
            fs_jam_void: "00:00:00",
            fs_kd_petugas_void: "",
            fs_kd_trs_gen: "",
            fs_kd_trs_deposit_khusus: "",
            fn_nilai_deposit: 1_000_000m,
            fn_total_deposit: 1_000_000m,
            crttgl: "2026-10-22",
            crtjam: "10:30:00",
            crtusr: "KASIR01",
            updtgl: "2026-10-22",
            updjam: "10:30:00",
            updusr: "KASIR01",
            fs_mr: "MR002",
            fs_nm_pasien: "PASIEN TEST",
            fs_nm_layanan: "RAWAT INAP MAWAR",
            fs_no_kwitansi: "KW0001",
            fn_kas: 1_000_000m,
            fn_bank: 0m,
            fn_kartu_kredit: 0m,
            fs_kd_petugas_kasir: "KASIR01",
            fs_terima_dari: "Keluarga Pasien");

        // Act
        var model = dto.ToModel();

        // Assert
        model.DepositId.Should().Be("DP0002");
        model.Reg.RegId.Should().Be("REG002");
        model.Reg.PasienId.Should().Be("MR002");
        model.Reg.PasienName.Should().Be("PASIEN TEST");
        model.Layanan.LayananId.Should().Be("LAY02");
        model.Layanan.LayananName.Should().Be("RAWAT INAP MAWAR");
        model.Keterangan.Should().Be("Titipan deposit");
        model.NilaiDeposit.Should().Be(1_000_000m);
        model.Audit.Created.UserId.Should().Be("KASIR01");
        model.Audit.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void DepositSaldoDto_InstantiatesWithCorrectValues()
    {
        // Arrange & Act
        var saldoDto = new DepositSaldoDto(
            RegId: "REG003",
            TotalSetoran: 2_500_000m,
            TotalTerpakai: 1_000_000m,
            SaldoAktif: 1_500_000m);

        // Assert
        saldoDto.RegId.Should().Be("REG003");
        saldoDto.TotalSetoran.Should().Be(2_500_000m);
        saldoDto.TotalTerpakai.Should().Be(1_000_000m);
        saldoDto.SaldoAktif.Should().Be(1_500_000m);
    }

    [Fact]
    public void DepositSaldoDto_ToModel_MapsCorrectly()
    {
        // Arrange
        var saldoDto = new DepositSaldoDto(
            RegId: "REG003",
            TotalSetoran: 2_500_000m,
            TotalTerpakai: 1_000_000m,
            SaldoAktif: 1_500_000m);

        // Act
        var model = saldoDto.ToModel();

        // Assert
        model.RegId.Should().Be("REG003");
        model.TotalSetoran.Should().Be(2_500_000m);
        model.TotalTerpakai.Should().Be(1_000_000m);
        model.SaldoAktif.Should().Be(1_500_000m);
    }

    [Fact]
    public void DepositSaldoDto_FromModel_MapsCorrectly()
    {
        // Arrange
        var model = new DepositSaldoModel("REG004", 3_000_000m, 500_000m, 2_500_000m);

        // Act
        var dto = DepositSaldoDto.FromModel(model);

        // Assert
        dto.RegId.Should().Be("REG004");
        dto.TotalSetoran.Should().Be(3_000_000m);
        dto.TotalTerpakai.Should().Be(500_000m);
        dto.SaldoAktif.Should().Be(2_500_000m);
    }
}
