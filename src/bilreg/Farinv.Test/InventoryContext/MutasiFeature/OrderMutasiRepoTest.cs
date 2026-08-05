using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;
using Farinv.Infrastructure.Helpers;
using Farinv.Infrastructure.InventoryContext.MutasiFeature;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Test.InventoryContext.MutasiFeature;

public class OrderMutasiRepoTest
{
    private readonly OrderMutasiRepo _sut = new(
        new OrderMutasiDal(ConnStringHelper.GetTestEnv()),
        new OrderMutasiItemDal(ConnStringHelper.GetTestEnv()));

    private OrderMutasiModel FakerData()
    {
        var brg = new BrgReff("B001", "Obat A");
        var satuan = new SatuanType("S001", "Buah");
        var item = new OrderMutasiItemModel(1, brg, 10, satuan);
        var approval = new ApprovalType("-", new DateTime(3000, 1, 1));
        var rejection = new ApprovalType("-", new DateTime(3000, 1, 1));
        var audit = AuditTrailType.Create("U001", new DateTime(2025, 12, 26));

        return new OrderMutasiModel(
            "-", // new
            new DateTime(2025, 12, 26),
            OrderMutasiStateEnum.Submitted,
            new LayananReff("L001", "Rawat Inap"),
            new LayananReff("L002", "Rawat Jalan"),
            approval, rejection, "Note",
            audit,
            new List<OrderMutasiItemModel> { item }
        );
    }

    private static IOrderMutasiKey FakerKey()
        => OrderMutasiModel.Key("OM001");

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
        _sut.SaveChanges(model); // insert dulu
        model.UpdateNote("Updated Note");
        _sut.SaveChanges(model); // update
    }

    [Fact]
    public void DeleteEntityTest()
    {
        using var trans = TransHelper.NewScope();
        var model = FakerData();
        _sut.SaveChanges(model); // insert dulu
        _sut.DeleteEntity(FakerKey());
    }

    [Fact]
    public void LoadEntityTest()
    {
        using var trans = TransHelper.NewScope();

        // Arrange
        var model = FakerData();
        _sut.SaveChanges(model);

        // Ambil ID yang BENAR
        var headers = _sut.ListData(new Periode(new DateTime(2025, 12, 26)));
        headers.Should().NotBeEmpty(); // safety
        var id = headers.First().OrderMutasiId;

        // Act
        var actual = _sut.LoadEntity(OrderMutasiModel.Key(id));

        // Assert
        actual.HasValue.Should().BeTrue();
        actual.Value.OrderMutasiId.Should().Be(id);
        actual.Value.ListItem.Should().HaveCount(1);
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
        actual.Should().HaveCountGreaterThan(0);

        actual.First().OrderMutasiId
            .Should().NotBeNullOrWhiteSpace();
    }

}