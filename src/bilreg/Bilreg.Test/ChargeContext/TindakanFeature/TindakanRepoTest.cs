using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanRepoTest
{
    private readonly Mock<ITindakanDal> _tindakanDalMock;
    private readonly Mock<ITindakanKomponenDal> _tindakanKompDalMock;
    private readonly TindakanRepo _sut;

    public TindakanRepoTest()
    {
        _tindakanDalMock = new Mock<ITindakanDal>();
        _tindakanKompDalMock = new Mock<ITindakanKomponenDal>();
        _sut = new TindakanRepo(_tindakanDalMock.Object, _tindakanKompDalMock.Object);
    }

    private static TindakanModel CreateTestModel(string pakaiBedId = "BED-01", int qty = 3)
    {
        var regReff = new RegReff("REG01", "PAS01", "Pasien A");
        var layananReff = new LayananReff("LAY01", "Layanan A");
        var kelasReff = new KelasReff("K1", "Kelas 1");
        var tipeTarifReff = new TipeTarifReff("TT", "Umum");
        var tarifReff = new TarifReff("TRF01", "Tarif Tindakan");
        var komponen = new TindakanKomponenWithoutPpaType(
            new KomponenReff("K1", "Jasa Sarana"), 1, 50000, qty, 50000 * qty);
        var auditTrail = AuditTrailType.Create("USR01", new DateTime(2026, 10, 20));

        return new TindakanModel(
            "TDK001", new DateTime(2026, 10, 20), "ORD001",
            regReff, layananReff, kelasReff, tipeTarifReff, tarifReff,
            new[] { komponen }, auditTrail, pakaiBedId, qty);
    }

    private static TindakanDto CreateTestDto(string pakaiBedId = "BED-01", int qty = 3)
        => new(
            "TDK001", new DateTime(2026, 10, 20), "ORD001",
            "REG01", "PAS01", "Pasien A",
            "LAY01", "Layanan A",
            "K1", "Kelas 1",
            "TT", "Umum",
            "TRF01", "Tarif Tindakan", 150000,
            "USR01", new DateTime(2026, 10, 20),
            "", new DateTime(3000, 1, 1),
            "", new DateTime(3000, 1, 1),
            pakaiBedId, qty);

    [Fact]
    public void SaveChanges_GivenNewEntity_InsertsTindakanAndKomponen()
    {
        // Arrange
        var model = CreateTestModel();
        _tindakanDalMock.Setup(x => x.GetData(It.IsAny<ITindakanKey>())).Returns((TindakanDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _tindakanDalMock.Verify(x => x.Insert(It.Is<TindakanDto>(d => d.PakaiBedId == "BED-01" && d.Qty == 3)), Times.Once);
        _tindakanDalMock.Verify(x => x.Update(It.IsAny<TindakanDto>()), Times.Never);
        _tindakanKompDalMock.Verify(x => x.Delete(model), Times.Once);
        _tindakanKompDalMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanKomponenDto>>(list =>
            list.Any(k => k.Qty == 3 && k.SubTotal == 150000))), Times.Once);
    }

    [Fact]
    public void SaveChanges_GivenExistingEntity_UpdatesTindakanAndKomponen()
    {
        // Arrange
        var model = CreateTestModel("BED-02", 5);
        _tindakanDalMock.Setup(x => x.GetData(It.IsAny<ITindakanKey>())).Returns(CreateTestDto());

        // Act
        _sut.SaveChanges(model);

        // Assert
        _tindakanDalMock.Verify(x => x.Update(It.Is<TindakanDto>(d => d.PakaiBedId == "BED-02" && d.Qty == 5)), Times.Once);
        _tindakanDalMock.Verify(x => x.Insert(It.IsAny<TindakanDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_GivenExistingKey_ReturnsTindakanModelWithPakaiBedIdAndQty()
    {
        // Arrange
        var dto = CreateTestDto("BED-01", 3);
        var kompDto = new TindakanKomponenDto("TDK001", 1, "K1", "Jasa Sarana", "", "", 3, 50000, 150000);
        _tindakanDalMock.Setup(x => x.GetData(It.IsAny<ITindakanKey>())).Returns(dto);
        _tindakanKompDalMock.Setup(x => x.ListData(It.IsAny<ITindakanKey>())).Returns(new[] { kompDto });

        // Act
        var result = _sut.LoadEntity(TindakanModel.Key("TDK001"));

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.PakaiBedId.Should().Be("BED-01");
        model.Qty.Should().Be(3);
        model.ListKomponen.Should().HaveCount(1);
        model.ListKomponen.First().Qty.Should().Be(3);
        model.ListKomponen.First().SubTotal.Should().Be(150000);
    }

    [Fact]
    public void ListDataTdkJual_MapsToViewWithPakaiBedIdAndQty()
    {
        // Arrange
        var regKey = RegModel.Key("REG01");
        var jualDto = new TindakanJualDto(
            "TDK001", new DateTime(2026, 10, 20), "ORD001",
            "REG01", "PAS01", "Pasien A",
            "LAY01", "Layanan A",
            "TRF01", "Tarif Tindakan",
            3, 150000, "TINDAKAN",
            "USR01", new DateTime(2026, 10, 20),
            "", new DateTime(3000, 1, 1),
            "", new DateTime(3000, 1, 1),
            "BED-01");
        _tindakanDalMock.Setup(x => x.ListTdkJual(regKey)).Returns(new[] { jualDto });

        // Act
        var list = _sut.ListDataTdkJual(regKey).ToList();

        // Assert
        list.Should().HaveCount(1);
        list[0].Qty.Should().Be(3);
        list[0].PakaiBedId.Should().Be("BED-01");
    }
}
