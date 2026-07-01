using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.InventoryContext.StokFeature;

public class StokLayerDalTest
{
    private readonly StokLayerDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly DateTime _reffIndate = new(2025,12,16,14,48,50);
    private readonly DateTime _expDate = new(2026, 12, 31);
    private IEnumerable<StokLayerDto> FakerList()
        => new List<StokLayerDto>
        {
            new StokLayerDto(
                StokLayerId: "A",
                BrgId: "B",
                LayananId: "C",
                PurchaseId: "E",
                ReceiveId: "F",
                ExpDate: _expDate,
                BatchNo: "G",
                QtyIn: 1,
                QtySisa: 1,
                Hpp: 1000,
                TrsReffInId: "D",
                TrsReffInDate: _reffIndate,
                BrgName: "H",
                LayananName: "I"
            ),
            new StokLayerDto(
                StokLayerId: "J",
                BrgId: "B",
                LayananId: "C",
                PurchaseId: "L",
                ReceiveId: "M",
                ExpDate: _expDate,
                BatchNo: "N",
                QtyIn: 2,
                QtySisa: 2,
                Hpp: 2000,
                TrsReffInId: "K",
                TrsReffInDate: _reffIndate,
                BrgName: "O",
                LayananName: "P"
            )
        };

    private static IStokKey FakerKey()
        => StokModel.Key("B", "C");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.BrgName)
                .Excluding(x => x.LayananName));
    }
}