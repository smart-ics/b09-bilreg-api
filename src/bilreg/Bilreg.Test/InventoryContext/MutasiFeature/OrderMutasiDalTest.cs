using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

[Collection("OrderMutasiDatabaseTests")]
public class OrderMutasiDalTest
{
    private readonly OrderMutasiDal _sut;

    public OrderMutasiDalTest()
    {
        OrderMutasiSchemaFixture.EnsureSchema();
        _sut = new(ConnStringHelper.GetTestEnv());
    }

    private static OrderMutasiDto FakerData(
        string id = "OM001",
        OrderMutasiStateEnum state = OrderMutasiStateEnum.Submitted,
        OrderMutasiPriorityEnum priority = OrderMutasiPriorityEnum.Cito,
        string layananOrderId = "L001",
        string orderNote = "Order Cito stok darurat")
    {
        return new OrderMutasiDto(
            OrderMutasiId: id,
            OrderMutasiDate: new DateTime(2025, 12, 26, 10, 0, 0),
            State: state,
            Priority: priority,
            LayananOrderId: layananOrderId,
            LayananOrderName: "Rawat Inap",
            LayananTujuanId: "L002",
            LayananTujuanName: "Gudang Farmasi",
            ApprovalUserId: "U_APPROVE",
            ApprovalDate: new DateTime(2025, 12, 26, 11, 0, 0),
            RejectionUserId: "-",
            RejectionDate: new DateTime(3000, 1, 1),
            OrderNote: orderNote,
            CrtUser: "U001",
            CrtDate: new DateTime(2025, 12, 26, 9, 0, 0),
            UpdUser: "U001",
            UpdDate: new DateTime(2025, 12, 26, 10, 0, 0),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1)
        );
    }

    private static IOrderMutasiKey FakerKey(string id = "OM001")
        => OrderMutasiModel.Key(id);

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
        _sut.Update(data with
        {
            State = OrderMutasiStateEnum.Approved,
            Priority = OrderMutasiPriorityEnum.Rutin,
            OrderNote = "Updated Note"
        });

        var actual = _sut.GetData(FakerKey());
        actual.State.Should().Be(OrderMutasiStateEnum.Approved);
        actual.Priority.Should().Be(OrderMutasiPriorityEnum.Rutin);
        actual.OrderNote.Should().Be("Updated Note");
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerData());
        _sut.Delete(FakerKey());

        var actual = _sut.GetData(FakerKey());
        actual.Should().BeNull();
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
        actual.Priority.Should().Be(expected.Priority);
        actual.OrderNote.Should().Be(expected.OrderNote);
    }

    [Fact]
    public void ListData_Periode_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = FakerData();
        _sut.Insert(expected);

        var filter = new Periode(new DateTime(2025, 12, 26));
        var list = _sut.ListData(filter).ToList();

        list.Should().Contain(x => x.OrderMutasiId == expected.OrderMutasiId && x.Priority == expected.Priority);
    }

    [Fact]
    public void ListDraftStateTest()
    {
        using var trans = TransHelper.NewScope();
        var draft = FakerData("OM_DRAFT", OrderMutasiStateEnum.Draft);
        var submitted = FakerData("OM_SUBMIT", OrderMutasiStateEnum.Submitted);
        _sut.Insert(draft);
        _sut.Insert(submitted);

        var list = _sut.ListDraftState().ToList();

        list.Should().Contain(x => x.OrderMutasiId == "OM_DRAFT");
        list.Should().NotContain(x => x.OrderMutasiId == "OM_SUBMIT");
    }

    [Fact]
    public void ListApprovedState_ShouldInclude_Approved_And_PartiallyFulfilled()
    {
        using var trans = TransHelper.NewScope();
        var approved = FakerData("OM_APP", OrderMutasiStateEnum.Approved);
        var partiallyFulfilled = FakerData("OM_PART", OrderMutasiStateEnum.PartiallyFulfilled);
        var draft = FakerData("OM_DRF2", OrderMutasiStateEnum.Draft);
        _sut.Insert(approved);
        _sut.Insert(partiallyFulfilled);
        _sut.Insert(draft);

        var filter = new Periode(new DateTime(2025, 12, 26));
        var list = _sut.ListApprovedState(filter).ToList();

        list.Should().Contain(x => x.OrderMutasiId == "OM_APP");
        list.Should().Contain(x => x.OrderMutasiId == "OM_PART");
        list.Should().NotContain(x => x.OrderMutasiId == "OM_DRF2");
    }

    [Fact]
    public void ListData_ByLayananOrder_Test()
    {
        using var trans = TransHelper.NewScope();
        var order1 = FakerData("OM_L1", layananOrderId: "L001");
        var order2 = FakerData("OM_L2", layananOrderId: "L999");
        _sut.Insert(order1);
        _sut.Insert(order2);

        var filter = new Periode(new DateTime(2025, 12, 26));
        var list = _sut.ListData(new LayananReff("L001", "Rawat Inap"), filter).ToList();

        list.Should().Contain(x => x.OrderMutasiId == "OM_L1");
        list.Should().NotContain(x => x.OrderMutasiId == "OM_L2");
    }
}
