using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.StokFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.InventoryContext.StokFeature;

public class StokBukuDalTest
{
    private readonly StokBukuDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly DateTime _dateTime1;
    private readonly DateTime _dateTime2;
    private readonly DateTime _dateTime3;

    public StokBukuDalTest()
    {
        _dateTime1 = new DateTime(2025,12,2);
        _dateTime2 = new DateTime(2025,12,3);
        _dateTime3 = new DateTime(2025,12,4);
    }
    
    private IEnumerable<StokBukuDto> FakerList()
        => new List<StokBukuDto>
        {
            new StokBukuDto(
                StokBukuId: "A",
                StokLayerId: "B",
                NoUrut: 1,
                BrgId: "C",
                LayananId: "D",
                PurchaseId: "E",
                ReceiveId: "F",
                ExpDate: _dateTime1,
                BatchNo: "G",
                QtyIn: 1,
                QtyOut: 0,
                Hpp: 1000,
                TrsReffId: "H",
                TrsReffDate: _dateTime2,
                UseCase: "I",
                EntryDate: _dateTime2
            ),
            new StokBukuDto(
                StokBukuId: "J",
                StokLayerId: "B",
                NoUrut: 2,
                BrgId: "C",
                LayananId: "D",
                PurchaseId: "K",
                ReceiveId: "L",
                ExpDate: _dateTime1.AddDays(1),
                BatchNo: "M",
                QtyIn: 0,
                QtyOut: 1,
                Hpp: 1000,
                TrsReffId: "N",
                TrsReffDate: _dateTime2.AddDays(1),
                UseCase: "O",
                EntryDate: _dateTime3.AddDays(1)
            )
        };

    private static IStokLayerKey FakerKey()
        => StokLayerModel.Key("B");

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
        actual.Should().BeEquivalentTo(FakerList());
    }
}