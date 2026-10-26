using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.DepositFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositRepoTest
{
    private readonly Mock<IDepositDal> _depositDalMock;
    private readonly DepositRepo _sut;

    public DepositRepoTest()
    {
        _depositDalMock = new Mock<IDepositDal>();
        _sut = new DepositRepo(_depositDalMock.Object);
    }

    private static DepositModel CreateFakeModel(string depositId = "DP01", string regId = "REG01")
    {
        return new DepositModel(
            depositId: depositId,
            depositDate: new DateTime(2026, 10, 22, 10, 0, 0),
            reg: new RegReff(regId, "MR01", "Pasien A"),
            layanan: new LayananReff("LAY01", "Layanan A"),
            ketarangan: "Deposit Test",
            nilaiDeposit: 500_000m,
            audit: new AuditTrailType(
                new AuditInfoType("USER01", DateTime.Now),
                new AuditInfoType("USER01", DateTime.Now),
                new AuditInfoType(string.Empty, new DateTime(3000, 1, 1))));
    }

    private static DepositDto CreateFakeDto(string depositId = "DP01", string regId = "REG01")
    {
        return new DepositDto(
            fs_kd_trs: depositId,
            fd_tgl_trs: "2026-10-22",
            fs_jam_trs: "10:00:00",
            fd_tgl_jam_trs: "2026-10-22 10:00:00",
            fs_kd_petugas: "USER01",
            fs_kd_reg: regId,
            fs_kd_layanan: "LAY01",
            fs_keterangan: "Deposit Test",
            fd_tgl_void: "3000-01-01",
            fs_jam_void: "00:00:00",
            fs_kd_petugas_void: string.Empty,
            fs_kd_trs_gen: string.Empty,
            fs_kd_trs_deposit_khusus: string.Empty,
            fn_nilai_deposit: 500_000m,
            fn_total_deposit: 500_000m,
            crttgl: "2026-10-22",
            crtjam: "10:00:00",
            crtusr: "USER01",
            updtgl: "2026-10-22",
            updjam: "10:00:00",
            updusr: "USER01",
            fs_mr: "MR01",
            fs_nm_pasien: "Pasien A",
            fs_nm_layanan: "Layanan A",
            fs_no_kwitansi: "KW-01",
            fn_kas: 500_000m,
            fn_bank: 0,
            fn_kartu_kredit: 0,
            fs_kd_petugas_kasir: "KASIR01",
            fs_terima_dari: "Pasien A");
    }

    [Fact]
    public void GivenNewDeposit_WhenSaveChanges_ThenCallInsert()
    {
        // Arrange
        var model = CreateFakeModel();
        _depositDalMock
            .Setup(x => x.GetData(It.Is<IDepositId>(k => k.DepositId == model.DepositId)))
            .Returns((DepositDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _depositDalMock.Verify(x => x.Insert(It.Is<DepositDto>(d => d.fs_kd_trs == model.DepositId)), Times.Once);
        _depositDalMock.Verify(x => x.Update(It.IsAny<DepositDto>()), Times.Never);
    }

    [Fact]
    public void GivenExistingDeposit_WhenSaveChanges_ThenCallUpdate()
    {
        // Arrange
        var model = CreateFakeModel();
        var existingDto = CreateFakeDto();
        _depositDalMock
            .Setup(x => x.GetData(It.Is<IDepositId>(k => k.DepositId == model.DepositId)))
            .Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _depositDalMock.Verify(x => x.Update(It.Is<DepositDto>(d => d.fs_kd_trs == model.DepositId)), Times.Once);
        _depositDalMock.Verify(x => x.Insert(It.IsAny<DepositDto>()), Times.Never);
    }

    [Fact]
    public void GivenExistingDeposit_WhenLoadEntity_ThenReturnModel()
    {
        // Arrange
        var existingDto = CreateFakeDto();
        var keyMock = new Mock<IDepositId>();
        keyMock.Setup(k => k.DepositId).Returns("DP01");

        _depositDalMock
            .Setup(x => x.GetData(keyMock.Object))
            .Returns(existingDto);

        // Act
        var result = _sut.LoadEntity(keyMock.Object);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.DepositId.Should().Be("DP01");
        result.Value.Reg.RegId.Should().Be("REG01");
        result.Value.NilaiDeposit.Should().Be(500_000m);
    }

    [Fact]
    public void GivenNonExistingDeposit_WhenLoadEntity_ThenReturnNone()
    {
        // Arrange
        var keyMock = new Mock<IDepositId>();
        keyMock.Setup(k => k.DepositId).Returns("DP99");

        _depositDalMock
            .Setup(x => x.GetData(keyMock.Object))
            .Returns((DepositDto)null!);

        // Act
        var result = _sut.LoadEntity(keyMock.Object);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenRegKey_WhenListData_ThenReturnMappedModels()
    {
        // Arrange
        var regKeyMock = new Mock<IRegKey>();
        regKeyMock.Setup(k => k.RegId).Returns("REG01");

        var dtoList = new List<DepositDto>
        {
            CreateFakeDto("DP01", "REG01"),
            CreateFakeDto("DP02", "REG01")
        };

        _depositDalMock
            .Setup(x => x.ListData(regKeyMock.Object))
            .Returns(dtoList);

        // Act
        var result = _sut.ListData(regKeyMock.Object).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].DepositId.Should().Be("DP01");
        result[1].DepositId.Should().Be("DP02");
    }

    [Fact]
    public void GivenRegKey_WhenGetSaldoFound_ThenReturnDepositSaldoModel()
    {
        // Arrange
        var regKeyMock = new Mock<IRegKey>();
        regKeyMock.Setup(k => k.RegId).Returns("REG01");

        var saldoDto = new DepositSaldoDto(
            RegId: "REG01",
            TotalSetoran: 2_000_000m,
            TotalTerpakai: 750_000m,
            SaldoAktif: 1_250_000m);

        _depositDalMock
            .Setup(x => x.GetSaldo(regKeyMock.Object))
            .Returns(saldoDto);

        // Act
        var result = _sut.GetSaldo(regKeyMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG01");
        result.TotalSetoran.Should().Be(2_000_000m);
        result.TotalTerpakai.Should().Be(750_000m);
        result.SaldoAktif.Should().Be(1_250_000m);
    }

    [Fact]
    public void GivenRegKey_WhenGetSaldoNull_ThenReturnZeroSaldoModel()
    {
        // Arrange
        var regKeyMock = new Mock<IRegKey>();
        regKeyMock.Setup(k => k.RegId).Returns("REG99");

        _depositDalMock
            .Setup(x => x.GetSaldo(regKeyMock.Object))
            .Returns((DepositSaldoDto)null!);

        // Act
        var result = _sut.GetSaldo(regKeyMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG99");
        result.TotalSetoran.Should().Be(0);
        result.TotalTerpakai.Should().Be(0);
        result.SaldoAktif.Should().Be(0);
    }
}
