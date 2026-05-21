using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultRecordHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabResultDocumentRepo> _resultRepo = new();
    private readonly LabResultRecordHandler _sut;

    public LabResultRecordHandlerTest()
    {
        _sut = new LabResultRecordHandler(_orderRepo.Object, _resultRepo.Object);
    }

    private static LabOrderModel ChargedLabOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        return order;
    }

    [Fact]
    public async Task Handle_FirstRecord_CreatesDocumentAndSavesBoth()
    {
        var order = ChargedLabOrder();
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId))
            .Returns(MayBe<LabResultDocumentModel>.None);

        var cmd = new LabResultRecordCmd(
            order.OrderId,
            "UR1",
            (int)LabResultSourceEnum.Manual,
            [
                new LabResultRecordItemDto(
                    "T1", "HB", "", "", (int)LabResultTypeEnum.Numeric, 13m, null, null, null, "g/dL", "12-16")
            ]);

        await _sut.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Recorded)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId
            && r.ResultStatus == LabResultStatusEnum.Recorded)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_Throws()
    {
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe<LabOrderModel>.None);

        var cmd = new LabResultRecordCmd("MISSING", "U1", 1, [new LabResultRecordItemDto("T1", "N", "", "", 1, 1m, null, null, null, "", "")]);

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
