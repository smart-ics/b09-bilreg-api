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
        string patientName = "Pasien Worklist",
        int orderSource = 1,
        int urgency = 0,
        DateTime? crtDate = null)
        => new(
            OrderId: orderId,
            EmrOrderId: "",
            OrderNo: orderId == "LBO000000010" ? "LAB00000010" : "LAB" + orderId,
            OrderSource: orderSource,
            LabOrderStatus: labOrderStatus,
            LastBillingReleaseStatus: 0,
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
            LastBillingReleaseCheckAt: new DateTime(3000, 1, 1),
            LastBillingReleaseCheckUserId: "",
            LastBillingReleaseMessage: "",
            ReleasedDate: new DateTime(3000, 1, 1),
            ReleasedUserId: "",
            ReleaseNote: "",
            CancelledReason: "",
            CancelledDate: new DateTime(3000, 1, 1),
            CancelledUserId: "",
            TerminationReason: "",
            TerminationDate: new DateTime(3000, 1, 1),
            TerminationUserId: "",
            CrtUser: "U1",
            CrtDate: crtDate ?? new DateTime(2026, 5, 18, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            Urgency: urgency);

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

    [Fact]
    public void List_Sorting_PrioritizesCitoThenCrtDateDesc()
    {
        using var trans = TransHelper.NewScope();
        var orderNormalOld = FakerHeader(orderId: "LBO_SORT_1", patientName: "SortTest Normal Old", urgency: 0, crtDate: new DateTime(2026, 5, 18, 8, 0, 0));
        var orderCitoOld = FakerHeader(orderId: "LBO_SORT_2", patientName: "SortTest Cito Old", urgency: 1, crtDate: new DateTime(2026, 5, 18, 9, 0, 0));
        var orderNormalNew = FakerHeader(orderId: "LBO_SORT_3", patientName: "SortTest Normal New", urgency: 0, crtDate: new DateTime(2026, 5, 18, 11, 0, 0));
        var orderCitoNew = FakerHeader(orderId: "LBO_SORT_4", patientName: "SortTest Cito New", urgency: 1, crtDate: new DateTime(2026, 5, 18, 10, 0, 0));

        _headerDal.Insert(orderNormalOld);
        _headerDal.Insert(orderCitoOld);
        _headerDal.Insert(orderNormalNew);
        _headerDal.Insert(orderCitoNew);

        var filter = new LabOrderWorklistFilter(
            LabOrderStatus: null,
            SearchTerm: "SortTest",
            Date1: null,
            Date2: null);

        var result = _sut.List(filter).ToList();

        result.Should().HaveCount(4);
        result.Select(x => x.OrderId).Should().Equal("LBO_SORT_4", "LBO_SORT_2", "LBO_SORT_3", "LBO_SORT_1");
    }

    [Fact]
    public void List_FilterByOrderSource_ReturnsMatchingOrders()
    {
        using var trans = TransHelper.NewScope();
        var orderRajal = FakerHeader(orderId: "LBO_SRC_1", patientName: "SourceTest Rajal", orderSource: 1);
        var orderRanap = FakerHeader(orderId: "LBO_SRC_2", patientName: "SourceTest Ranap", orderSource: 2);
        var orderIgd = FakerHeader(orderId: "LBO_SRC_3", patientName: "SourceTest Igd", orderSource: 3);

        _headerDal.Insert(orderRajal);
        _headerDal.Insert(orderRanap);
        _headerDal.Insert(orderIgd);

        var filterRajal = new LabOrderWorklistFilter(
            LabOrderStatus: null,
            SearchTerm: "SourceTest",
            Date1: null,
            Date2: null,
            OrderSource: 1);
        var resultRajal = _sut.List(filterRajal).ToList();
        resultRajal.Should().ContainSingle(x => x.OrderId == "LBO_SRC_1");

        var filterRanap = new LabOrderWorklistFilter(
            LabOrderStatus: null,
            SearchTerm: "SourceTest",
            Date1: null,
            Date2: null,
            OrderSource: 2);
        var resultRanap = _sut.List(filterRanap).ToList();
        resultRanap.Should().ContainSingle(x => x.OrderId == "LBO_SRC_2");

        var filterAll = new LabOrderWorklistFilter(
            LabOrderStatus: null,
            SearchTerm: "SourceTest",
            Date1: null,
            Date2: null,
            OrderSource: null);
        var resultAll = _sut.List(filterAll).ToList();
        resultAll.Should().HaveCount(3);
    }
}
