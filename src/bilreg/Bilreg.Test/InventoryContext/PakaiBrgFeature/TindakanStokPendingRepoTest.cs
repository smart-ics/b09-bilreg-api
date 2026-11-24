using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class TindakanStokPendingRepoTest
{
    private readonly Mock<ITindakanStokPendingDal> _dalMock = new();
    private readonly TindakanStokPendingRepo _sut;

    public TindakanStokPendingRepoTest()
    {
        _sut = new TindakanStokPendingRepo(_dalMock.Object);
    }

    [Fact]
    public void Insert_SingleModel_CallsDalInsertWithDto()
    {
        var model = new TindakanStokPendingType(
            "PND01", "TDK01", "TB01", 1, "REG01", "LY01", "BRG01", "PCS", 5m);

        _sut.Insert(model, "USER1");

        _dalMock.Verify(x => x.Insert(It.Is<TindakanStokPendingDto>(d =>
            d.PendingId == "PND01" &&
            d.TindakanId == "TDK01" &&
            d.CrtUser == "USER1" &&
            d.QtyPending == 5m)), Times.Once);
    }

    [Fact]
    public void Insert_MultipleModels_CallsDalInsertWithDtoList()
    {
        var models = new List<TindakanStokPendingType>
        {
            new("PND01", "TDK01", "TB01", 1, "REG01", "LY01", "BRG01", "PCS", 5m),
            new("PND02", "TDK01", "TB02", 2, "REG01", "LY01", "BRG02", "PCS", 3m)
        };

        _sut.Insert(models, "USER2");

        _dalMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanStokPendingDto>>(l =>
            l.Count() == 2)), Times.Once);
    }

    [Fact]
    public void ListByTindakanId_GivenId_ReturnsMappedDomainModels()
    {
        var dtos = new List<TindakanStokPendingDto>
        {
            new() { PendingId = "PND01", TindakanId = "TDK01", TindakanBarangId = "TB01", NoUrut = 1, RegId = "REG01", LayananId = "LY01", BrgId = "BRG01", SatuanKecilId = "PCS", QtyPending = 5m }
        };
        _dalMock.Setup(x => x.ListByTindakanId("TDK01")).Returns(dtos);

        var actual = _sut.ListByTindakanId("TDK01").ToList();

        actual.Should().HaveCount(1);
        actual[0].PendingId.Should().Be("PND01");
        actual[0].QtyPending.Should().Be(5m);
    }

    [Fact]
    public void ListFifoAktif_GivenBrgAndLayanan_ReturnsMappedDomainModels()
    {
        var dtos = new List<TindakanStokPendingDto>
        {
            new() { PendingId = "PND01", TindakanId = "TDK01", TindakanBarangId = "TB01", NoUrut = 1, RegId = "REG01", LayananId = "LY01", BrgId = "BRG01", SatuanKecilId = "PCS", QtyPending = 5m }
        };
        _dalMock.Setup(x => x.ListFifoAktif("BRG01", "LY01")).Returns(dtos);

        var actual = _sut.ListFifoAktif("BRG01", "LY01").ToList();

        actual.Should().HaveCount(1);
        actual[0].PendingId.Should().Be("PND01");
    }

    [Fact]
    public void GetData_GivenId_ReturnsMappedDomainModel()
    {
        var dto = new TindakanStokPendingDto
        {
            PendingId = "PND01", TindakanId = "TDK01", TindakanBarangId = "TB01", NoUrut = 1,
            RegId = "REG01", LayananId = "LY01", BrgId = "BRG01", SatuanKecilId = "PCS",
            QtyPending = 5m
        };
        _dalMock.Setup(x => x.GetData("PND01")).Returns(dto);

        var actual = _sut.GetData("PND01");

        actual.Should().NotBeNull();
        actual!.PendingId.Should().Be("PND01");
    }

    [Fact]
    public void MarkSettled_CallsDalMarkSettled()
    {
        var settledDate = new DateTime(2026, 11, 12, 10, 0, 0);
        _sut.MarkSettled("PND01", "PK01", 5m, settledDate, "USER1");

        _dalMock.Verify(x => x.MarkSettled("PND01", "PK01", 5m, settledDate, "USER1"), Times.Once);
    }

    [Fact]
    public void MarkVoid_CallsDalMarkVoid()
    {
        var voidDate = new DateTime(2026, 11, 12, 11, 0, 0);
        _sut.MarkVoid("PND01", voidDate, "USER2");

        _dalMock.Verify(x => x.MarkVoid("PND01", voidDate, "USER2"), Times.Once);
    }

    [Fact]
    public void MarkVoidByTindakanId_CallsDalMarkVoidByTindakanId()
    {
        var voidDate = new DateTime(2026, 11, 12, 11, 0, 0);
        _sut.MarkVoidByTindakanId("TDK01", voidDate, "USER3");

        _dalMock.Verify(x => x.MarkVoidByTindakanId("TDK01", voidDate, "USER3"), Times.Once);
    }
}
