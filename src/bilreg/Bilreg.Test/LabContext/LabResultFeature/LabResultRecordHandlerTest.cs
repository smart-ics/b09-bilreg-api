using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
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
        _sut = new LabResultRecordHandler(
            _orderRepo.Object,
            _resultRepo.Object,
            new LabResultScaffoldService(), TestTglJamProvider.Instance);
    }

    private static LabOrderModel ChargedLabOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var order = LabOrderTestSupport.CreateEmrOrder(
            "LAB00000001",
            lines: [LabOrderTestSupport.ResolvedLine()],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
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
            [new LabResultRecordValueDto("MLC0001", "13.2")]);

        await _sut.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Recorded)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId
            && r.ResultStatus == LabResultStatusEnum.Recorded
            && r.Items.Single().ComponentId == "MLC0001"
            && r.Items.Single().NumericValue == 13.2m
            && r.CriticalNote == "")), Times.Once);
    }

    [Fact]
    public async Task Handle_FirstRecord_WithCriticalNote_SavesCriticalNoteOnDocument()
    {
        var order = ChargedLabOrder();
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId))
            .Returns(MayBe<LabResultDocumentModel>.None);

        var criticalNote = "Kalium kritis 6.8 mEq/L, konfirmasi ulang ke perawat ruangan";
        var cmd = new LabResultRecordCmd(
            order.OrderId,
            "UR1",
            (int)LabResultSourceEnum.Manual,
            [new LabResultRecordValueDto("MLC0001", "6.8")],
            CriticalNote: criticalNote);

        await _sut.Handle(cmd, CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Recorded)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId
            && r.ResultStatus == LabResultStatusEnum.Recorded
            && r.Items.Single().ComponentId == "MLC0001"
            && r.Items.Single().NumericValue == 6.8m
            && r.CriticalNote == criticalNote)), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentRecord_UpdatesExistingDocumentAndCriticalNote()
    {
        var order = ChargedLabOrder();
        order.MarkRecorded("U1");
        var existingDoc = LabResultDocumentModel.CreateInitial(order.OrderId, new AuditInfoType("U1", DateTime.Now));
        existingDoc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U1");
        existingDoc.SetCriticalNote("Catatan awal");
        existingDoc.MarkRecorded("U1");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId))
            .Returns(MayBe.From(existingDoc));

        var updatedCriticalNote = "Catatan revisi nilai kritis analitik";
        var cmd = new LabResultRecordCmd(
            order.OrderId,
            "UR2",
            (int)LabResultSourceEnum.Manual,
            [new LabResultRecordValueDto("MLC0001", "14.5")],
            CriticalNote: updatedCriticalNote);

        await _sut.Handle(cmd, CancellationToken.None);

        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId
            && r.ResultStatus == LabResultStatusEnum.Recorded
            && r.Items.Single().NumericValue == 14.5m
            && r.CriticalNote == updatedCriticalNote)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_Throws()
    {
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe<LabOrderModel>.None);

        var cmd = new LabResultRecordCmd(
            "MISSING",
            "U1",
            1,
            [new LabResultRecordValueDto("MLC0001", "1")]);

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
