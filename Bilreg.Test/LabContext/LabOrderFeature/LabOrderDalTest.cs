using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderDalTest
{
    private readonly LabOrderDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly LabOrderItemDal _itemDal = new(ConnStringHelper.GetTestEnv());

    private static LabOrderDto FakerHeader(string orderId = "LBO000000001")
        => new(
            OrderId: orderId,
            OrderNo: "LAB00000001",
            OrderSource: 1,
            LabOrderStatus: 1,
            FinancialClearance: 0,
            OwareStatus: 0,
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Tes",
            BirthDate: new DateTime(1990, 1, 15),
            Gender: "L",
            AgeAtOrder: 36,
            ExecutionRegId: "",
            DeferredReason: "",
            DeferredUntil: new DateTime(3000, 1, 1),
            BillingTindakanId: "",
            BillingLastError: "",
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 5, 18, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1));

    private static LabOrderItemDto FakerItem(string orderId = "LBO000000001")
        => new(
            orderId,
            1,
            "T1",
            "HB",
            "Hemoglobin",
            "TR1",
            "T-HB",
            "Tarif HB",
            1,
            "Blood",
            1);

    private static ILabOrderKey FakerKey(string orderId = "LBO000000001")
        => LabOrderModel.Key(orderId);

    [Fact]
    public void InsertAndGetData_RoundTripsHeader()
    {
        using var trans = TransHelper.NewScope();
        var dto = FakerHeader();
        _sut.Insert(dto);
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void InsertAndGetData_RoundTripsDeferredFields()
    {
        using var trans = TransHelper.NewScope();
        var until = new DateTime(2026, 5, 20, 8, 0, 0);
        var dto = FakerHeader("LBO000000099") with
        {
            LabOrderStatus = 2,
            DeferredReason = "Puasa 12 jam",
            DeferredUntil = until
        };
        _sut.Insert(dto);
        var actual = _sut.GetData(LabOrderModel.Key("LBO000000099"));
        actual.DeferredReason.Should().Be("Puasa 12 jam");
        actual.DeferredUntil.Should().Be(until);
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        using var trans = TransHelper.NewScope();
        var dto = FakerHeader();
        _sut.Insert(dto);
        var updated = dto with { PatientName = "Updated Name", UpdUser = "U2", UpdDate = DateTime.Now };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.PatientName.Should().Be("Updated Name");
        actual.UpdUser.Should().Be("U2");
    }

    [Fact]
    public void ItemDal_InsertAndListData_RoundTrips()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerHeader());
        _itemDal.Insert([FakerItem()]);
        var items = _itemDal.ListData(FakerKey()).ToList();
        items.Should().HaveCount(1);
        items[0].TestName.Should().Be("Hemoglobin");
    }

    [Fact]
    public void Delete_RemovesHeader()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerHeader());
        _sut.Delete(FakerKey());
        var act = () => _sut.GetData(FakerKey());
        act.Should().Throw<Exception>();
    }
}
