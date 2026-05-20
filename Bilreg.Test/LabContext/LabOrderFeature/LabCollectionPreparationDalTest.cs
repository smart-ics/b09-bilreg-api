using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabCollectionPreparationDalTest
{
    private readonly LabOrderDal _headerDal = new(ConnStringHelper.GetTestEnv());
    private readonly LabOrderItemDal _itemDal = new(ConnStringHelper.GetTestEnv());
    private readonly LabCollectionPreparationDal _sut = new(ConnStringHelper.GetTestEnv());

    private const string OrderId = "LBO000000020";

    private static LabOrderDto ChargedHeader(string orderId = OrderId)
        => new(
            OrderId: orderId,
            OrderNo: "LAB00000020",
            OrderSource: 1,
            LabOrderStatus: (int)LabOrderStatusEnum.Charged,
            OwareStatus: 0,
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Prep",
            BirthDate: new DateTime(1990, 1, 15),
            Gender: "L",
            AgeAtOrder: 36,
            ExecutionRegId: "",
            DeferredReason: "",
            DeferredUntil: new DateTime(3000, 1, 1),
            BillingTindakanId: "TDK1",
            BillingLastError: "",
            CollectedDate: new DateTime(3000, 1, 1),
            CollectedUserId: "",
            CollectionNote: "",
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
            CrtDate: new DateTime(2026, 5, 18, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1));

    private static LabOrderItemDto Item(
        string orderId,
        int itemNo,
        string testCode,
        string testName,
        int tubeType,
        string specimenType,
        int requiredTubeCount)
        => new(
            orderId,
            itemNo,
            $"T{itemNo}",
            testCode,
            testName,
            "TR1",
            "TC",
            "Tarif",
            tubeType,
            specimenType,
            requiredTubeCount);

    [Fact]
    public void Get_ReturnsTestsAndVacutainerGroups()
    {
        using var trans = TransHelper.NewScope();
        _headerDal.Insert(ChargedHeader());
        _itemDal.Insert(
        [
            Item(OrderId, 1, "CBC", "CBC", (int)VacutainerTypeEnum.Edta, "Blood", 1),
            Item(OrderId, 2, "HBA1C", "HbA1c", (int)VacutainerTypeEnum.Edta, "Blood", 1),
            Item(OrderId, 3, "SGOT", "SGOT", (int)VacutainerTypeEnum.Serum, "Blood", 1)
        ]);

        var view = _sut.Get(OrderId);

        view.Should().NotBeNull();
        view!.OrderId.Should().Be(OrderId);
        view.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Charged);
        view.Tests.Should().HaveCount(3);
        view.VacutainerGroups.Should().HaveCount(2);
        var edta = view.VacutainerGroups.Single(g => g.TubeType == (int)VacutainerTypeEnum.Edta);
        edta.SpecimenType.Should().Be("Blood");
        edta.TubeCount.Should().Be(1);
        var serum = view.VacutainerGroups.Single(g => g.TubeType == (int)VacutainerTypeEnum.Serum);
        serum.TubeCount.Should().Be(1);
    }

    [Fact]
    public void Get_WhenMissing_ReturnsNull()
    {
        var view = _sut.Get("LBO000000099999");
        view.Should().BeNull();
    }
}
