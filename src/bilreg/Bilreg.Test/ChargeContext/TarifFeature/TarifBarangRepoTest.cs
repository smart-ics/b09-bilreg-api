using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifBarangRepoTest
{
    private readonly Mock<ITarifBarangDal> _dalMock = new();
    private readonly TarifBarangRepo _sut;

    public TarifBarangRepoTest()
    {
        _sut = new TarifBarangRepo(_dalMock.Object);
    }

    [Fact]
    public void ListData_GivenKey_ReturnsMappedDomainModels()
    {
        var key = TarifType.Key("TRF01");
        var dtos = new List<TarifBarangDto>
        {
            new("TRF01", 1m, "B01", "Barang 1", "S01", "Pcs", 2m),
            new("TRF01", 2m, "B02", "Barang 2", "S02", "Box", 1m),
        };
        _dalMock.Setup(x => x.ListData(key)).Returns(dtos);

        var actual = _sut.ListData(key).ToList();

        actual.Should().HaveCount(2);
        actual[0].Should().BeEquivalentTo(new TarifBarangType("TRF01", 1, "B01", "Barang 1", "S01", "Pcs", 2m));
        actual[1].Should().BeEquivalentTo(new TarifBarangType("TRF01", 2, "B02", "Barang 2", "S02", "Box", 1m));
    }

    [Fact]
    public void ListData_WhenDalReturnsNull_ReturnsEmptyList()
    {
        var key = TarifType.Key("TRF_EMPTY");
        _dalMock.Setup(x => x.ListData(key)).Returns((IEnumerable<TarifBarangDto>?)null!);

        var actual = _sut.ListData(key);

        actual.Should().BeEmpty();
    }
}
