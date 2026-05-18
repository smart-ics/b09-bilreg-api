using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderWorklistDalTest
{
    private readonly LabOrderDal _headerDal = new(ConnStringHelper.GetTestEnv());
    private readonly LabOrderItemDal _itemDal = new(ConnStringHelper.GetTestEnv());
    private readonly LabOrderWorklistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabOrderDto FakerHeader(
        string orderId = "LBO000000010",
        int labOrderStatus = (int)LabOrderStatusEnum.Ordered,
        string patientName = "Pasien Worklist")
        => new(
            OrderId: orderId,
            OrderNo: "LAB00000010",
            OrderSource: 1,
            LabOrderStatus: labOrderStatus,
            FinancialClearance: 0,
            OwareStatus: 0,
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: patientName,
            BirthDate: new DateTime(1990, 1, 15),
            Gender: "L",
            AgeAtOrder: 36,
            ExecutionRegId: "",
            DeferredReason: "",
            DeferredUntil: new DateTime(3000, 1, 1),
            BillingTindakanId: "",
            BillingLastError: "",
            CollectedDate: new DateTime(3000, 1, 1),
            CollectedUserId: "",
            CollectionNote: "",
            FinancialClearanceDate: new DateTime(3000, 1, 1),
            FinancialClearanceUserId: "",
            FinancialClearanceReason: "",
            ReleasedDate: new DateTime(3000, 1, 1),
            ReleasedUserId: "",
            ReleaseNote: "",
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 5, 18, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1));

    private static LabOrderItemDto FakerItem(string orderId = "LBO000000010")
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

    [Fact]
    public void List_FilterByStatus_ReturnsMatchingProjection()
    {
        using var trans = TransHelper.NewScope();
        _headerDal.Insert(FakerHeader());
        _itemDal.Insert([FakerItem()]);

        var filter = new LabOrderWorklistFilter(
            LabOrderStatus: (int)LabOrderStatusEnum.Ordered,
            SearchTerm: null,
            Date1: null,
            Date2: null);

        var result = _sut.List(filter).ToList();

        result.Should().ContainSingle(x => x.OrderId == "LBO000000010");
        var row = result.Single(x => x.OrderId == "LBO000000010");
        row.OrderNo.Should().Be("LAB00000010");
        row.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        row.PatientName.Should().Be("Pasien Worklist");
        row.ItemCount.Should().Be(1);
        row.TestNames.Should().ContainSingle().Which.Should().Be("Hemoglobin");
    }

    [Fact]
    public void List_SearchTerm_FiltersByPatientName()
    {
        using var trans = TransHelper.NewScope();
        _headerDal.Insert(FakerHeader(orderId: "LBO000000011", patientName: "Unique Alpha Name"));
        _headerDal.Insert(FakerHeader(orderId: "LBO000000012", patientName: "Other Patient"));

        var filter = new LabOrderWorklistFilter(
            LabOrderStatus: null,
            SearchTerm: "Unique Alpha",
            Date1: null,
            Date2: null);

        var result = _sut.List(filter).ToList();

        result.Should().ContainSingle(x => x.OrderId == "LBO000000011");
    }

    [Fact]
    public void List_ExcludesVoidedOrders()
    {
        using var trans = TransHelper.NewScope();
        var voided = FakerHeader(orderId: "LBO000000013") with
        {
            VodUser = "U1",
            VodDate = new DateTime(2026, 5, 18, 12, 0, 0)
        };
        _headerDal.Insert(voided);

        var filter = new LabOrderWorklistFilter(null, null, null, null);
        var result = _sut.List(filter);

        result.Should().NotContain(x => x.OrderId == "LBO000000013");
    }
}
