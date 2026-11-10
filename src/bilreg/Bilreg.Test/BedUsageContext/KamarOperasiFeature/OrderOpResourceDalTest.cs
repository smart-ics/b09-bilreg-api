using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

[Collection("OrderOpResourceDb")]
public class OrderOpResourceDalTest
{
    private readonly OrderOpResourceDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OrderOpResourceDto Faker(
        string id = "01TESTRESOURCE000000000001",
        string orderOpId = "01TESTORDER0000000000001",
        string scheduleOpId = "01TESTSCHED0000000000001")
        => new(
            orderOpResourceId: id,
            orderOpId: orderOpId,
            scheduleOpId: scheduleOpId,
            resourceType: 1,
            category: "BLOOD",
            itemName: "Packed Red Cells (PRC)",
            jumlah: 2,
            satuan: "Kantong",
            keterangan: "Siap sebelum insisi",
            crtUser: "tester",
            crtDate: new DateTime(2026, 11, 10, 8, 0, 0),
            updUser: "tester",
            updDate: new DateTime(2026, 11, 10, 8, 0, 0),
            vodUser: string.Empty,
            vodDate: new DateTime(3000, 1, 1)
        );

    [Fact]
    public void InsertAndGetData_ShouldSucceed_WhenOwnerMatches()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker();

        _sut.Insert(item);

        var actual = _sut.GetData(item.OrderOpResourceId, item.OrderOpId);
        actual.Should().NotBeNull();
        actual!.OrderOpResourceId.Should().Be(item.OrderOpResourceId);
        actual.OrderOpId.Should().Be(item.OrderOpId);
        actual.Category.Should().Be("BLOOD");
        actual.ItemName.Should().Be("Packed Red Cells (PRC)");
        actual.Jumlah.Should().Be(2);
        actual.Satuan.Should().Be("Kantong");
        actual.Keterangan.Should().Be("Siap sebelum insisi");
    }

    [Fact]
    public void GetData_ShouldReturnNull_WhenOrderOpIdMismatch()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker();
        _sut.Insert(item);

        var actual = _sut.GetData(item.OrderOpResourceId, "WRONG_ORDER_OP_ID");
        actual.Should().BeNull();
    }

    [Fact]
    public void ListData_ShouldReturnOnlyResourcesForSpecifiedOrderOpId()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker("01TESTRES00000000000000001", "ORDER_AAA");
        var item2 = Faker("01TESTRES00000000000000002", "ORDER_AAA");
        var itemOther = Faker("01TESTRES00000000000000003", "ORDER_BBB");

        _sut.Insert(item1);
        _sut.Insert(item2);
        _sut.Insert(itemOther);

        var listA = _sut.ListData("ORDER_AAA").ToList();
        listA.Should().HaveCount(2);
        listA.Select(x => x.OrderOpResourceId).Should().Contain(new[] { item1.OrderOpResourceId, item2.OrderOpResourceId });
        listA.Select(x => x.OrderOpResourceId).Should().NotContain(itemOther.OrderOpResourceId);

        var listB = _sut.ListData("ORDER_BBB").ToList();
        listB.Should().HaveCount(1);
        listB.First().OrderOpResourceId.Should().Be(itemOther.OrderOpResourceId);
    }

    [Fact]
    public void Update_ShouldModifyAttributes_WhenOwnerMatches()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker();
        _sut.Insert(item);

        item.Jumlah = 5;
        item.Keterangan = "Updated notes";
        item.Category = "IMPLANT";
        item.ResourceType = 2;
        _sut.Update(item);

        var actual = _sut.GetData(item.OrderOpResourceId, item.OrderOpId);
        actual.Should().NotBeNull();
        actual!.Jumlah.Should().Be(5);
        actual.Keterangan.Should().Be("Updated notes");
        actual.Category.Should().Be("IMPLANT");
        actual.ResourceType.Should().Be(2);
    }

    [Fact]
    public void Update_ShouldThrowException_WhenOwnerMismatch()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker("01TESTRES_UPDATE_MISMATCH", "ORDER_REAL");
        _sut.Insert(item);

        var maliciousItem = Faker("01TESTRES_UPDATE_MISMATCH", "ORDER_ATTACKER");
        maliciousItem.ItemName = "Hacked Name";

        var act = () => _sut.Update(maliciousItem);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*belongs to OrderOpId 'ORDER_REAL', not 'ORDER_ATTACKER'*");
    }

    [Fact]
    public void Delete_ShouldRemoveItem_WhenOwnerMatches()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker();
        _sut.Insert(item);

        _sut.Delete(item.OrderOpResourceId, item.OrderOpId);

        var actual = _sut.GetData(item.OrderOpResourceId, item.OrderOpId);
        actual.Should().BeNull();
    }

    [Fact]
    public void Delete_ShouldThrowException_WhenOwnerMismatch()
    {
        using var trans = TransHelper.NewScope();
        var item = Faker("01TESTRES_DEL_MISMATCH", "ORDER_ACTUAL");
        _sut.Insert(item);

        var act = () => _sut.Delete("01TESTRES_DEL_MISMATCH", "ORDER_WRONG");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*belongs to OrderOpId 'ORDER_ACTUAL', not 'ORDER_WRONG'*");
    }

    [Fact]
    public void DeleteAll_ShouldRemoveAllItems_ForGivenOrderOpId()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker("01TESTRES_DELALL_1", "ORDER_CLEAR");
        var item2 = Faker("01TESTRES_DELALL_2", "ORDER_CLEAR");
        _sut.Insert(item1);
        _sut.Insert(item2);

        _sut.DeleteAll("ORDER_CLEAR");

        var remaining = _sut.ListData("ORDER_CLEAR");
        remaining.Should().BeEmpty();
    }
}
