using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class KasirShiftRepoTest
{
    private readonly Mock<IKasirShiftDal> _kasirShiftDalMock;
    private readonly KasirShiftRepo _sut;

    public KasirShiftRepoTest()
    {
        _kasirShiftDalMock = new Mock<IKasirShiftDal>();
        _sut = new KasirShiftRepo(_kasirShiftDalMock.Object);
    }

    private static KasirShiftModel CreateFakeModel(
        string shiftId = "SH12345678",
        string kasirId = "KASIR01",
        string status = KasirConst.StatusShiftBuka,
        decimal saldoAwal = 500_000m)
    {
        var audit = new AuditTrailType(
            new AuditInfoType("USER01", "2026-11-07", "07:00:00"),
            new AuditInfoType("USER01", "2026-11-07", "07:00:00"),
            new AuditInfoType(string.Empty, "3000-01-01", "00:00:00"));

        return new KasirShiftModel(
            shiftId: shiftId,
            tglShift: "2026-11-07",
            kodeShift: "1",
            kasirId: kasirId,
            loket: "LOKET-1",
            tglJamBuka: "2026-11-07 07:00:00",
            saldoAwal: saldoAwal,
            tglJamTutup: status == KasirConst.StatusShiftTutup ? "2026-11-07 14:00:00" : "3000-01-01 00:00:00",
            saldoAkhirFisik: status == KasirConst.StatusShiftTutup ? 800_000m : 0,
            totalTerimaSistem: status == KasirConst.StatusShiftTutup ? 350_000m : 0,
            totalKeluarSistem: status == KasirConst.StatusShiftTutup ? 50_000m : 0,
            statusShift: status,
            audit: audit);
    }

    private static KasirShiftDto CreateFakeDto(
        string shiftId = "SH12345678",
        string kasirId = "KASIR01",
        string status = KasirConst.StatusShiftBuka,
        decimal saldoAwal = 500_000m)
    {
        return new KasirShiftDto(
            ShiftId: shiftId,
            TglShift: "2026-11-07",
            KodeShift: "1",
            KasirId: kasirId,
            Loket: "LOKET-1",
            TglJamBuka: "2026-11-07 07:00:00",
            SaldoAwal: saldoAwal,
            TglJamTutup: status == KasirConst.StatusShiftTutup ? "2026-11-07 14:00:00" : "3000-01-01 00:00:00",
            SaldoAkhirFisik: status == KasirConst.StatusShiftTutup ? 800_000m : 0,
            TotalTerimaSistem: status == KasirConst.StatusShiftTutup ? 350_000m : 0,
            TotalKeluarSistem: status == KasirConst.StatusShiftTutup ? 50_000m : 0,
            StatusShift: status,
            crttgl: "2026-11-07",
            crtjam: "07:00:00",
            crtusr: "USER01",
            updtgl: "2026-11-07",
            updjam: "07:00:00",
            updusr: "USER01");
    }

    [Fact]
    public void GivenNewShift_WhenSaveChanges_ThenCallInsert()
    {
        // Arrange
        var model = CreateFakeModel();
        _kasirShiftDalMock
            .Setup(x => x.GetData(It.Is<IKasirShiftKey>(k => k.ShiftId == model.ShiftId)))
            .Returns((KasirShiftDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _kasirShiftDalMock.Verify(x => x.Insert(It.Is<KasirShiftDto>(d => d.ShiftId == model.ShiftId)), Times.Once);
        _kasirShiftDalMock.Verify(x => x.Update(It.IsAny<KasirShiftDto>()), Times.Never);
    }

    [Fact]
    public void GivenExistingShift_WhenSaveChanges_ThenCallUpdate()
    {
        // Arrange
        var model = CreateFakeModel();
        var existingDto = CreateFakeDto();
        _kasirShiftDalMock
            .Setup(x => x.GetData(It.Is<IKasirShiftKey>(k => k.ShiftId == model.ShiftId)))
            .Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _kasirShiftDalMock.Verify(x => x.Insert(It.IsAny<KasirShiftDto>()), Times.Never);
        _kasirShiftDalMock.Verify(x => x.Update(It.Is<KasirShiftDto>(d => d.ShiftId == model.ShiftId)), Times.Once);
    }

    [Fact]
    public void GivenModel_WhenSave_ThenCallSaveChanges()
    {
        // Arrange
        var model = CreateFakeModel();
        _kasirShiftDalMock
            .Setup(x => x.GetData(It.IsAny<IKasirShiftKey>()))
            .Returns((KasirShiftDto)null!);

        // Act
        _sut.Save(model);

        // Assert
        _kasirShiftDalMock.Verify(x => x.Insert(It.Is<KasirShiftDto>(d => d.ShiftId == model.ShiftId)), Times.Once);
    }

    [Fact]
    public void GivenExistingKey_WhenLoadEntity_ThenReturnMayBeModel()
    {
        // Arrange
        var key = new KasirShiftKey("SH12345678");
        var dto = CreateFakeDto("SH12345678");

        _kasirShiftDalMock
            .Setup(x => x.GetData(key))
            .Returns(dto);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.ShiftId.Should().Be("SH12345678");
        result.Value.SaldoAwal.Should().Be(500_000m);
        result.Value.StatusShift.Should().Be(KasirConst.StatusShiftBuka);
    }

    [Fact]
    public void GivenNonExistingKey_WhenLoadEntity_ThenReturnMayBeNone()
    {
        // Arrange
        var key = new KasirShiftKey("SH_NOT_FOUND");
        _kasirShiftDalMock
            .Setup(x => x.GetData(key))
            .Returns((KasirShiftDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenExistingKey_WhenGetKey_ThenReturnModel()
    {
        // Arrange
        var key = new KasirShiftKey("SH12345678");
        var dto = CreateFakeDto("SH12345678");

        _kasirShiftDalMock
            .Setup(x => x.GetData(key))
            .Returns(dto);

        // Act
        var result = _sut.GetKey(key);

        // Assert
        result.Should().NotBeNull();
        result!.ShiftId.Should().Be("SH12345678");
    }

    [Fact]
    public void GivenNonExistingKey_WhenGetKey_ThenReturnNull()
    {
        // Arrange
        var key = new KasirShiftKey("SH_NOT_FOUND");
        _kasirShiftDalMock
            .Setup(x => x.GetData(key))
            .Returns((KasirShiftDto)null!);

        // Act
        var result = _sut.GetKey(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenExistingStringId_WhenGetKey_ThenReturnModel()
    {
        // Arrange
        var shiftId = "SH12345678";
        var dto = CreateFakeDto(shiftId);

        _kasirShiftDalMock
            .Setup(x => x.GetData(It.Is<IKasirShiftKey>(k => k.ShiftId == shiftId)))
            .Returns(dto);

        // Act
        var result = _sut.GetKey(shiftId);

        // Assert
        result.Should().NotBeNull();
        result!.ShiftId.Should().Be(shiftId);
    }

    [Fact]
    public void GivenActiveShiftExists_WhenGetActiveShift_ThenReturnModel()
    {
        // Arrange
        var kasirId = "KASIR01";
        var dto = CreateFakeDto("SH12345678", kasirId: kasirId);

        _kasirShiftDalMock
            .Setup(x => x.GetActiveShift(kasirId))
            .Returns(dto);

        // Act
        var result = _sut.GetActiveShift(kasirId);

        // Assert
        result.Should().NotBeNull();
        result!.KasirId.Should().Be(kasirId);
        result.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void GivenNoActiveShift_WhenGetActiveShift_ThenReturnNull()
    {
        // Arrange
        var kasirId = "KASIR_INACTIVE";
        _kasirShiftDalMock
            .Setup(x => x.GetActiveShift(kasirId))
            .Returns((KasirShiftDto)null!);

        // Act
        var result = _sut.GetActiveShift(kasirId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GivenTglShift_WhenListByTgl_ThenReturnMappedModels()
    {
        // Arrange
        var tglShift = "2026-11-07";
        var dto = CreateFakeDto("SH12345678");

        _kasirShiftDalMock
            .Setup(x => x.ListByTgl(tglShift))
            .Returns(new[] { dto });

        // Act
        var result = _sut.ListByTgl(tglShift).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].ShiftId.Should().Be("SH12345678");
        result[0].TglShift.Should().Be(tglShift);
    }
}
