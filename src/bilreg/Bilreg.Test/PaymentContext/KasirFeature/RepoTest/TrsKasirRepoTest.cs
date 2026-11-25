using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class TrsKasirRepoTest
{
    private readonly Mock<ITrsKasirDal> _trsKasirDalMock;
    private readonly TrsKasirRepo _sut;

    public TrsKasirRepoTest()
    {
        _trsKasirDalMock = new Mock<ITrsKasirDal>();
        _sut = new TrsKasirRepo(_trsKasirDalMock.Object);
    }

    private static TrsKasirModel CreateFakeModel(
        string trsKasirId = "DX12345678",
        string shiftId = "SH12345678",
        string kasirId = "KASIR01",
        string regId = "REG01",
        decimal totalNilai = 150_000m)
    {
        var payments = new List<TrsKasirPaymentType>
        {
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarTunai,
                nilaiBayar: 200_000m,
                nilaiKembalian: 50_000m,
                noUrut: 1,
                trsKasirId: trsKasirId)
        };

        var audit = new AuditTrailType(
            new AuditInfoType("USER01", "2026-11-07", "10:00:00"),
            new AuditInfoType("USER01", "2026-11-07", "10:00:00"),
            new AuditInfoType(string.Empty, "3000-01-01", "00:00:00"));

        return new TrsKasirModel(
            trsKasirId: trsKasirId,
            tglJamTrs: new DateTime(2026, 11, 7, 10, 0, 0),
            jenisTrs: KasirConst.JenisTrsTerima,
            shiftId: shiftId,
            kasirId: kasirId,
            regId: regId,
            pasienId: "MR01",
            pasienName: "Pasien Test",
            orderRef: "BIL01",
            keterangan: "Pembayaran Kasir Test",
            totalNilai: totalNilai,
            statusTrs: KasirConst.StatusTrsBerhasil,
            payments: payments,
            audit: audit);
    }

    private static TrsKasirDto CreateFakeDto(
        string trsKasirId = "DX12345678",
        string shiftId = "SH12345678",
        string kasirId = "KASIR01",
        string regId = "REG01",
        decimal totalNilai = 150_000m)
    {
        return new TrsKasirDto(
            TrsKasirId: trsKasirId,
            TglTrs: "2026-11-07",
            JamTrs: "10:00:00",
            TglJamTrs: "2026-11-07 10:00:00",
            JenisTrs: KasirConst.JenisTrsTerima,
            ShiftId: shiftId,
            KasirId: kasirId,
            RegId: regId,
            PasienId: "MR01",
            PasienName: "Pasien Test",
            OrderRef: "BIL01",
            Keterangan: "Pembayaran Kasir Test",
            TotalNilai: totalNilai,
            StatusTrs: KasirConst.StatusTrsBerhasil,
            TglVoid: "3000-01-01",
            JamVoid: "00:00:00",
            PetugasVoid: string.Empty,
            AlasanVoid: string.Empty,
            crttgl: "2026-11-07",
            crtjam: "10:00:00",
            crtusr: "USER01",
            updtgl: "2026-11-07",
            updjam: "10:00:00",
            updusr: "USER01");
    }

    private static TrsKasirPaymentDto CreateFakePaymentDto(
        string trsKasirId = "DX12345678",
        int noUrut = 1,
        string metodeBayar = KasirConst.MetodeBayarTunai,
        decimal nilaiBayar = 200_000m,
        decimal nilaiKembalian = 50_000m)
    {
        return new TrsKasirPaymentDto(
            PaymentRowId: "ROW01",
            TrsKasirId: trsKasirId,
            NoUrut: noUrut,
            MetodeBayar: metodeBayar,
            NilaiBayar: nilaiBayar,
            NilaiKembalian: nilaiKembalian,
            NomorKartu: string.Empty,
            NomorReferensi: string.Empty,
            NamaBank: string.Empty);
    }

    [Fact]
    public void GivenNewTransaction_WhenSaveChanges_ThenCallInsertAndPayments()
    {
        // Arrange
        var model = CreateFakeModel();
        _trsKasirDalMock
            .Setup(x => x.GetData(It.Is<ITrsKasirKey>(k => k.TrsKasirId == model.TrsKasirId)))
            .Returns((TrsKasirDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _trsKasirDalMock.Verify(x => x.Insert(It.Is<TrsKasirDto>(d => d.TrsKasirId == model.TrsKasirId)), Times.Once);
        _trsKasirDalMock.Verify(x => x.Update(It.IsAny<TrsKasirDto>()), Times.Never);
        _trsKasirDalMock.Verify(x => x.DeletePayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == model.TrsKasirId)), Times.Once);
        _trsKasirDalMock.Verify(x => x.InsertPayments(It.Is<IEnumerable<TrsKasirPaymentDto>>(list => list.Count() == 1)), Times.Once);
    }

    [Fact]
    public void GivenExistingTransaction_WhenSaveChanges_ThenCallUpdateAndDeleteAndInsertPayments()
    {
        // Arrange
        var model = CreateFakeModel();
        var existingDto = CreateFakeDto();
        _trsKasirDalMock
            .Setup(x => x.GetData(It.Is<ITrsKasirKey>(k => k.TrsKasirId == model.TrsKasirId)))
            .Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _trsKasirDalMock.Verify(x => x.Insert(It.IsAny<TrsKasirDto>()), Times.Never);
        _trsKasirDalMock.Verify(x => x.Update(It.Is<TrsKasirDto>(d => d.TrsKasirId == model.TrsKasirId)), Times.Once);
        _trsKasirDalMock.Verify(x => x.DeletePayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == model.TrsKasirId)), Times.Once);
        _trsKasirDalMock.Verify(x => x.InsertPayments(It.Is<IEnumerable<TrsKasirPaymentDto>>(list => list.Count() == 1)), Times.Once);
    }

    [Fact]
    public void GivenModel_WhenSave_ThenCallSaveChanges()
    {
        // Arrange
        var model = CreateFakeModel();
        _trsKasirDalMock
            .Setup(x => x.GetData(It.IsAny<ITrsKasirKey>()))
            .Returns((TrsKasirDto)null!);

        // Act
        _sut.Save(model);

        // Assert
        _trsKasirDalMock.Verify(x => x.Insert(It.Is<TrsKasirDto>(d => d.TrsKasirId == model.TrsKasirId)), Times.Once);
    }

    [Fact]
    public void GivenExistingKey_WhenLoadEntity_ThenReturnMayBeWithPayments()
    {
        // Arrange
        var key = new TrsKasirKey("DX12345678");
        var dto = CreateFakeDto("DX12345678");
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.GetData(key))
            .Returns(dto);
        _trsKasirDalMock
            .Setup(x => x.ListPayments(key))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.TrsKasirId.Should().Be("DX12345678");
        result.Value.TotalNilai.Should().Be(150_000m);
        result.Value.Payments.Should().HaveCount(1);
        result.Value.Payments[0].NilaiBayar.Should().Be(200_000m);
        result.Value.Payments[0].NilaiKembalian.Should().Be(50_000m);
    }

    [Fact]
    public void GivenNonExistingKey_WhenLoadEntity_ThenReturnMayBeNone()
    {
        // Arrange
        var key = new TrsKasirKey("DX_NOT_FOUND");
        _trsKasirDalMock
            .Setup(x => x.GetData(key))
            .Returns((TrsKasirDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenExistingKey_WhenGetKey_ThenReturnModel()
    {
        // Arrange
        var key = new TrsKasirKey("DX12345678");
        var dto = CreateFakeDto("DX12345678");
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.GetData(key))
            .Returns(dto);
        _trsKasirDalMock
            .Setup(x => x.ListPayments(key))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.GetKey(key);

        // Assert
        result.Should().NotBeNull();
        result!.TrsKasirId.Should().Be("DX12345678");
    }

    [Fact]
    public void GivenNonExistingKey_WhenGetKey_ThenReturnNull()
    {
        // Arrange
        var key = new TrsKasirKey("DX_NOT_FOUND");
        _trsKasirDalMock
            .Setup(x => x.GetData(key))
            .Returns((TrsKasirDto)null!);

        // Act
        var result = _sut.GetKey(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExistingStringId_WhenGetKey_ThenReturnModel()
    {
        // Arrange
        var trsKasirId = "DX12345678";
        var dto = CreateFakeDto(trsKasirId);
        var paymentDto = CreateFakePaymentDto(trsKasirId);

        _trsKasirDalMock
            .Setup(x => x.GetData(It.Is<ITrsKasirKey>(k => k.TrsKasirId == trsKasirId)))
            .Returns(dto);
        _trsKasirDalMock
            .Setup(x => x.ListPayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == trsKasirId)))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.GetKey(trsKasirId);

        // Assert
        result.Should().NotBeNull();
        result!.TrsKasirId.Should().Be(trsKasirId);
    }

    [Fact]
    public void GivenShiftId_WhenListByShiftKey_ThenReturnMappedModels()
    {
        // Arrange
        var shiftKey = new KasirShiftKey("SH12345678");
        var dto = CreateFakeDto("DX12345678", shiftId: "SH12345678");
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.ListByShift("SH12345678"))
            .Returns(new[] { dto });
        _trsKasirDalMock
            .Setup(x => x.ListPayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == "DX12345678")))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.ListByShift(shiftKey).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TrsKasirId.Should().Be("DX12345678");
        result[0].ShiftId.Should().Be("SH12345678");
    }

    [Fact]
    public void GivenShiftId_WhenListByShiftString_ThenReturnMappedModels()
    {
        // Arrange
        var shiftId = "SH12345678";
        var dto = CreateFakeDto("DX12345678", shiftId: shiftId);
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.ListByShift(shiftId))
            .Returns(new[] { dto });
        _trsKasirDalMock
            .Setup(x => x.ListPayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == "DX12345678")))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.ListByShift(shiftId).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TrsKasirId.Should().Be("DX12345678");
    }

    [Fact]
    public void GivenRegId_WhenListByReg_ThenReturnMappedModels()
    {
        // Arrange
        var regId = "REG01";
        var dto = CreateFakeDto("DX12345678", regId: regId);
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.ListByReg(regId))
            .Returns(new[] { dto });
        _trsKasirDalMock
            .Setup(x => x.ListPayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == "DX12345678")))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.ListByReg(regId).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].RegId.Should().Be(regId);
    }

    [Fact]
    public void GivenTglTrs_WhenListByTgl_ThenReturnMappedModels()
    {
        // Arrange
        var tglTrs = "2026-11-07";
        var dto = CreateFakeDto("DX12345678");
        var paymentDto = CreateFakePaymentDto("DX12345678");

        _trsKasirDalMock
            .Setup(x => x.ListByTgl(tglTrs))
            .Returns(new[] { dto });
        _trsKasirDalMock
            .Setup(x => x.ListPayments(It.Is<ITrsKasirKey>(k => k.TrsKasirId == "DX12345678")))
            .Returns(new[] { paymentDto });

        // Act
        var result = _sut.ListByTgl(tglTrs).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TglTrs.Should().Be(tglTrs);
    }
}
