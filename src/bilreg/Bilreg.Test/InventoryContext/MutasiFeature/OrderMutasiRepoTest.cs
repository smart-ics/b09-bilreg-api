using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

[Collection("OrderMutasiDatabaseTests")]
public class OrderMutasiRepoTest
{
    private readonly OrderMutasiRepo _sut;

    public OrderMutasiRepoTest()
    {
        OrderMutasiSchemaFixture.EnsureSchema();
        _sut = new(
            new OrderMutasiDal(ConnStringHelper.GetTestEnv()),
            new OrderMutasiItemDal(ConnStringHelper.GetTestEnv()));
    }

    private static OrderMutasiModel FakerData(
        string id = "",
        string layananOrderId = "L001",
        OrderMutasiPriorityEnum priority = OrderMutasiPriorityEnum.Cito,
        OrderMutasiStateEnum state = OrderMutasiStateEnum.Submitted,
        string orderNote = "Kebutuhan Cito ICU")
    {
        var finalId = string.IsNullOrWhiteSpace(id) || id == "-" ? Ulid.NewUlid().ToString() : id;
        var brg = new BrgReff("B001", "Obat A");
        var satuan = new SatuanType("S001", "Buah");
        var item = new OrderMutasiItemModel(1, brg, 10m, satuan, 15m, 4m);
        var approval = new ApprovalType("U_APP", new DateTime(2025, 12, 26, 11, 0, 0));
        var rejection = new ApprovalType("-", new DateTime(3000, 1, 1));
        var audit = AuditTrailType.Create("U001", new DateTime(2025, 12, 26, 9, 0, 0));

        return new OrderMutasiModel(
            finalId,
            new DateTime(2025, 12, 26, 10, 0, 0),
            state,
            new LayananReff(layananOrderId, "Rawat Inap"),
            new LayananReff("L002", "Gudang Farmasi"),
            approval,
            rejection,
            orderNote,
            audit,
            [item],
            priority
        );
    }

    private static IOrderMutasiKey FakerKey(string id)
        => OrderMutasiModel.Key(id);

    [Fact]
    public void SaveChanges_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var model = FakerData();
        _sut.SaveChanges(model);
    }

    [Fact]
    public void SaveChanges_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var model = FakerData();
        _sut.SaveChanges(model); // insert

        model.UpdateNote("Updated Note");
        _sut.SaveChanges(model); // update

        var loaded = _sut.LoadEntity(model);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.OrderNote.Should().Be("Updated Note");
    }

    [Fact]
    public void DeleteEntityTest()
    {
        using var trans = TransHelper.NewScope();
        var model = FakerData();
        _sut.SaveChanges(model);

        _sut.DeleteEntity(model);

        var loaded = _sut.LoadEntity(model);
        loaded.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntityTest()
    {
        using var trans = TransHelper.NewScope();

        // Arrange
        var model = FakerData();
        _sut.SaveChanges(model);

        // Act
        var actual = _sut.LoadEntity(model);

        // Assert
        actual.HasValue.Should().BeTrue();
        actual.Value.OrderMutasiId.Should().Be(model.OrderMutasiId);
        actual.Value.Priority.Should().Be(OrderMutasiPriorityEnum.Cito);
        actual.Value.OrderNote.Should().Be("Kebutuhan Cito ICU");
        actual.Value.ListItem.Should().HaveCount(1);

        var item = actual.Value.ListItem.First();
        item.Brg.BrgId.Should().Be("B001");
        item.StokCurrent.Should().Be(15m);
        item.Qty.Should().Be(10m);
        item.QtyTerpenuhi.Should().Be(4m);
        item.SisaBackorder.Should().Be(6m);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();

        // Arrange
        var model = FakerData();
        _sut.SaveChanges(model);

        var filter = new Periode(new DateTime(2025, 12, 26));

        // Act
        var actual = _sut.ListData(filter).ToList();

        // Assert
        actual.Should().NotBeEmpty();
        var match = actual.FirstOrDefault(x => x.OrderMutasiId == model.OrderMutasiId);
        match.Should().NotBeNull();
        match!.Priority.Should().Be(OrderMutasiPriorityEnum.Cito);
        match.OrderNote.Should().Be("Kebutuhan Cito ICU");
    }

    [Fact]
    public void ListData_ByLayananOrder_Test()
    {
        using var trans = TransHelper.NewScope();

        var model1 = FakerData(layananOrderId: "L001");
        var model2 = FakerData(layananOrderId: "L999");
        _sut.SaveChanges(model1);
        _sut.SaveChanges(model2);

        var filter = new Periode(new DateTime(2025, 12, 26));
        var actual = _sut.ListData(new LayananReff("L001", "Rawat Inap"), filter).ToList();

        actual.Should().Contain(x => x.OrderMutasiId == model1.OrderMutasiId);
        actual.Should().NotContain(x => x.OrderMutasiId == model2.OrderMutasiId);
    }

    [Fact]
    public void ListApprovedStateTest()
    {
        using var trans = TransHelper.NewScope();

        var approved = FakerData(state: OrderMutasiStateEnum.Approved);
        var draft = FakerData(state: OrderMutasiStateEnum.Draft);
        _sut.SaveChanges(approved);
        _sut.SaveChanges(draft);

        var filter = new Periode(new DateTime(2025, 12, 26));
        var actual = _sut.ListApprovedState(filter).ToList();

        actual.Should().Contain(x => x.OrderMutasiId == approved.OrderMutasiId);
        actual.Should().NotContain(x => x.OrderMutasiId == draft.OrderMutasiId);
    }
}
