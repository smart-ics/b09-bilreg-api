using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.InventoryContext.MutasiFeature;

public class OrderMutasiDalTest
{
    private readonly OrderMutasiDal _sut = new(ConnStringHelper.GetTestEnv());

    private OrderMutasiDto FakerData()
    {
        return new OrderMutasiDto(
            OrderMutasiId: "OM001",
            OrderMutasiDate: new DateTime(2025, 12, 26, 10, 0, 0),
            State: OrderMutasiStateEnum.Submitted,
            LayananOrderId: "L001",
            LayananOrderName: "Rawat Inap",
            LayananTujuanId: "L002",
            LayananTujuanName: "Rawat Jalan",
            ApprovalUserId: "-",
            ApprovalDate: new DateTime(3000, 1, 1),
            RejectionUserId: "-",
            RejectionDate: new DateTime(3000, 1, 1),
            OrderNote: "Order untuk stok baru",
            CrtUser: "U001",
            CrtDate: new DateTime(2025, 12, 26, 9, 0, 0),
            UpdUser: "U001",
            UpdDate: new DateTime(2025, 12, 26, 10, 0, 0),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1)
        );
    }

    private static IOrderMutasiKey FakerKey()
        => OrderMutasiModel.Key("OM001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var data = FakerData();
        _sut.Insert(data);
        _sut.Update(data with { State = OrderMutasiStateEnum.Approved });
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var actual = _sut.GetData(FakerKey());

        actual.Should().NotBeNull();
        actual.OrderMutasiId.Should().Be(expected.OrderMutasiId);
        actual.LayananOrderId.Should().Be(expected.LayananOrderId);
        actual.State.Should().Be(expected.State);
    }
}
