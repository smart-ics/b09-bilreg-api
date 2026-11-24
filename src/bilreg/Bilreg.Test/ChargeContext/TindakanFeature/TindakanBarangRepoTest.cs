using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanBarangRepoTest
{
    private readonly Mock<ITindakanBarangDal> _dalMock = new();
    private readonly TindakanBarangRepo _sut;

    public TindakanBarangRepoTest()
    {
        _sut = new TindakanBarangRepo(_dalMock.Object);
    }

    [Fact]
    public void Insert_SingleModel_CallsDalInsertWithDto()
    {
        var model = new TindakanBarangType(
            "TB01", "TDK01", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, 50000m, "PK01", "TB01", false);

        _sut.Insert(model, "USER1");

        _dalMock.Verify(x => x.Insert(It.Is<TindakanBarangDto>(d =>
            d.TindakanBarangId == "TB01" &&
            d.TindakanId == "TDK01" &&
            d.CRTUSR == "USER1" &&
            d.IsChargeBarang == true)), Times.Once);
    }

    [Fact]
    public void Insert_MultipleModels_CallsDalInsertWithDtoList()
    {
        var models = new List<TindakanBarangType>
        {
            new("TB01", "TDK01", 1, "BRG01", "PCS", 2m, "PCS", 2m, 2m, 0m, true, 50000m),
            new("TB02", "TDK01", 2, "BRG02", "PCS", 1m, "PCS", 1m, 1m, 0m, false, 0m)
        };

        _sut.Insert(models, "USER2");

        _dalMock.Verify(x => x.Insert(It.Is<IEnumerable<TindakanBarangDto>>(l =>
            l.Count() == 2)), Times.Once);
    }

    [Fact]
    public void ListData_GivenFilter_ReturnsMappedDomainModels()
    {
        var filter = TindakanModel.Key("TDK01");
        var dtos = new List<TindakanBarangDto>
        {
            new() { TindakanBarangId = "TB01", TindakanId = "TDK01", NoUrut = 1, BrgId = "BRG01", SatuanBomId = "PCS", QtyBom = 2m, SatuanKecilId = "PCS", QtyKecilRequested = 2m, QtyKecilDeducted = 2m, QtyKecilPending = 0m, IsChargeBarang = true, HargaJualTotal = 50000m, PakaiBrgId = "PK01", TrsBillingId = "TB01", IsVoid = false }
        };
        _dalMock.Setup(x => x.ListData(filter)).Returns(dtos);

        var actual = _sut.ListData(filter).ToList();

        actual.Should().HaveCount(1);
        actual[0].TindakanBarangId.Should().Be("TB01");
        actual[0].TindakanId.Should().Be("TDK01");
        actual[0].IsChargeBarang.Should().BeTrue();
    }

    [Fact]
    public void GetData_GivenId_ReturnsMappedDomainModel()
    {
        var dto = new TindakanBarangDto
        {
            TindakanBarangId = "TB01", TindakanId = "TDK01", NoUrut = 1, BrgId = "BRG01",
            SatuanBomId = "PCS", QtyBom = 2m, SatuanKecilId = "PCS", QtyKecilRequested = 2m,
            QtyKecilDeducted = 2m, QtyKecilPending = 0m, IsChargeBarang = true,
            HargaJualTotal = 50000m, PakaiBrgId = "PK01", TrsBillingId = "TB01", IsVoid = false
        };
        _dalMock.Setup(x => x.GetData("TB01")).Returns(dto);

        var actual = _sut.GetData("TB01");

        actual.Should().NotBeNull();
        actual!.TindakanBarangId.Should().Be("TB01");
    }

    [Fact]
    public void MarkVoid_CallsDalMarkVoid()
    {
        _sut.MarkVoid("TDK01", "USER3");

        _dalMock.Verify(x => x.MarkVoid("TDK01", "USER3"), Times.Once);
    }
}
