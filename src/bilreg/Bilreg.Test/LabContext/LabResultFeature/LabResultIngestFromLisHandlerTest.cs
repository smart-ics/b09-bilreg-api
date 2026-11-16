using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultIngestFromLisHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabResultDocumentRepo> _resultRepo = new();
    private readonly ILabResultScaffoldService _scaffoldService = new LabResultScaffoldService();
    private readonly LabResultIngestFromLisHandler _sut;

    public LabResultIngestFromLisHandlerTest()
    {
        _sut = new LabResultIngestFromLisHandler(
            _orderRepo.Object,
            _resultRepo.Object,
            _scaffoldService);
    }

    private static LabOrderModel CreateChargedOrderWithComponents(
        string orderId,
        params LabOrderItemComponentModel[] components)
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien Tes", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var resolvedLine = new LabOrderModel.ResolvedOrderLine(item, components);
        var order = LabOrderTestSupport.CreateEmrOrder(
            orderId,
            lines: [resolvedLine],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        return order;
    }

    [Fact]
    public async Task Handle_SuccessfulIngestion_DirectMatchingAndLisFlags()
    {
        // Arrange
        var compHb = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0001",
            componentCode: "HB",
            componentName: "Hemoglobin",
            referenceRangeText: "12-16",
            sequenceNo: 1);
        var compLeu = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0002",
            componentCode: "LEU",
            componentName: "Leukosit",
            referenceRangeText: "4000-10000",
            sequenceNo: 2);
        var compPlt = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0003",
            componentCode: "PLT",
            componentName: "Trombosit",
            referenceRangeText: "150000-450000",
            sequenceNo: 3);

        var order = CreateChargedOrderWithComponents("LAB00000001", compHb, compLeu, compPlt);
        order.CollectSpecimen("PHLEB1", new CollectionInfoType(DateTime.Now, "PHLEB1", ""));

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe<LabResultDocumentModel>.None);

        var validatedAt = new DateTime(2026, 5, 18, 14, 30, 0);
        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-REF-999",
            "Dr. LIS Validator, Sp.PK",
            validatedAt,
            [
                new LabResultIngestItemDto("HB", "10.2", "g/dL", "12-16", "L"),
                new LabResultIngestItemDto("LEU", "14500", "/uL", "4000-10000", "H"),
                new LabResultIngestItemDto("PLT", "25000", "/uL", "150000-450000", "C")
            ],
            CriticalNote: "Peringatan Trombosit Kritis");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.OrderId == order.OrderId &&
            o.LabOrderStatus == LabOrderStatusEnum.Verified)), Times.Once);

        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId &&
            r.ResultStatus == LabResultStatusEnum.Verified &&
            r.ResultSource == LabResultSourceEnum.ExternalLis &&
            r.VerifiedUserId == "Dr. LIS Validator, Sp.PK" &&
            r.VerifiedDate == validatedAt &&
            r.CriticalNote == "Peringatan Trombosit Kritis" &&
            r.Items.Count == 3 &&
            r.Items[0].ComponentCode == "HB" &&
            r.Items[0].NumericValue == 10.2m &&
            r.Items[0].FlagStatus == LabResultFlagEnum.Low &&
            r.Items[1].ComponentCode == "LEU" &&
            r.Items[1].NumericValue == 14500m &&
            r.Items[1].FlagStatus == LabResultFlagEnum.High &&
            r.Items[2].ComponentCode == "PLT" &&
            r.Items[2].NumericValue == 25000m &&
            r.Items[2].FlagStatus == LabResultFlagEnum.Critical)), Times.Once);
    }

    [Theory]
    [InlineData("10.0", "12-16", LabResultFlagEnum.Low)]
    [InlineData("14.0", "12-16", LabResultFlagEnum.Normal)]
    [InlineData("18.0", "12-16", LabResultFlagEnum.High)]
    public async Task Handle_FallbackAutoFlagging_WhenLisFlagIsEmpty(
        string value,
        string refRange,
        LabResultFlagEnum expectedFlag)
    {
        // Arrange
        var comp = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0001",
            componentCode: "HB",
            componentName: "Hemoglobin",
            referenceRangeText: refRange);

        var order = CreateChargedOrderWithComponents("LAB00000002", comp);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe<LabResultDocumentModel>.None);

        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-REF-001",
            "Validator LIS",
            DateTime.Now,
            [
                new LabResultIngestItemDto("HB", value, "g/dL", refRange, Flag: "")
            ]);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.Items.Single().FlagStatus == expectedFlag)), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialResults_FillsOnlySentComponents_LeavesUnsentEmpty()
    {
        // Arrange
        var compHb = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0001",
            componentCode: "HB",
            componentName: "Hemoglobin",
            referenceRangeText: "12-16",
            sequenceNo: 1);
        var compLeu = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0002",
            componentCode: "LEU",
            componentName: "Leukosit",
            referenceRangeText: "4000-10000",
            sequenceNo: 2);
        var compPlt = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0003",
            componentCode: "PLT",
            componentName: "Trombosit",
            referenceRangeText: "150000-450000",
            sequenceNo: 3);

        var order = CreateChargedOrderWithComponents("LAB00000003", compHb, compLeu, compPlt);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe<LabResultDocumentModel>.None);

        // Ingest only Hemoglobin (partial result)
        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-PARTIAL-1",
            "Validator LIS",
            DateTime.Now,
            [
                new LabResultIngestItemDto("HB", "13.8", "g/dL", "12-16", "Normal")
            ]);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Verified)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.ResultStatus == LabResultStatusEnum.Verified &&
            r.Items.Count == 3 &&
            r.Items[0].ComponentCode == "HB" &&
            r.Items[0].NumericValue == 13.8m &&
            r.Items[0].FlagStatus == LabResultFlagEnum.Normal &&
            r.Items[1].ComponentCode == "LEU" &&
            r.Items[1].NumericValue == 0m &&
            r.Items[1].TextValue == "" &&
            r.Items[2].ComponentCode == "PLT" &&
            r.Items[2].NumericValue == 0m &&
            r.Items[2].TextValue == "")), Times.Once);
    }

    [Fact]
    public async Task Handle_TransitionsOrderAndDocument_ToVerified()
    {
        // Arrange
        var comp = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0001",
            componentCode: "HB",
            componentName: "Hemoglobin");

        var order = CreateChargedOrderWithComponents("LAB00000004", comp);
        order.MarkRecorded("ANALIS1");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe<LabResultDocumentModel>.None);

        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-004",
            "Dr. Validator LIS",
            new DateTime(2026, 5, 18, 15, 0, 0),
            [
                new LabResultIngestItemDto("HB", "14.2", "g/dL", "12-16", "Normal")
            ]);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        order.LabOrderStatus.Should().Be(LabOrderStatusEnum.Verified);
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Verified)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r => r.ResultStatus == LabResultStatusEnum.Verified)), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentIngestion_PreservesPreviousValuesAndReplacesExistingDoc()
    {
        // Arrange
        var compHb = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0001",
            componentCode: "HB",
            componentName: "Hemoglobin",
            referenceRangeText: "12-16",
            sequenceNo: 1);
        var compLeu = LabOrderTestSupport.SampleComponent(
            componentId: "MLC0002",
            componentCode: "LEU",
            componentName: "Leukosit",
            referenceRangeText: "4000-10000",
            sequenceNo: 2);

        var order = CreateChargedOrderWithComponents("LAB00000005", compHb, compLeu);
        order.CollectSpecimen("PHLEB1", new CollectionInfoType(DateTime.Now, "PHLEB1", ""));

        // Existing document already has HB filled
        var existingDoc = LabResultDocumentModel.CreateFromLisIngestion(
            order.OrderId,
            [
                new LabResultItemModel(
                    1, "MLC0001", "LTD0001", "Hemoglobin", "HB", "Hemoglobin", 1,
                    LabResultTypeEnum.Numeric, 12.5m, "", "", "", "g/dL", "12-16", true, LabResultFlagEnum.Normal),
                new LabResultItemModel(
                    2, "MLC0002", "LTD0001", "Hemoglobin", "LEU", "Leukosit", 2,
                    LabResultTypeEnum.Numeric, 0m, "", "", "", "/uL", "4000-10000", true, LabResultFlagEnum.Normal)
            ],
            "Previous Validator",
            DateTime.Now.AddMinutes(-30),
            new AuditInfoType("Prev", DateTime.Now.AddMinutes(-30)),
            criticalNote: "Catatan Lama",
            expertiseNote: "Ekspertise Patolog");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(existingDoc));

        // Subsequent ingest sends LEU only
        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-PARTIAL-2",
            "New Validator",
            DateTime.Now,
            [
                new LabResultIngestItemDto("LEU", "8500", "/uL", "4000-10000", "Normal")
            ],
            CriticalNote: "Catatan Baru");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _resultRepo.Verify(x => x.DeleteEntity(existingDoc), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.OrderId == order.OrderId &&
            r.ResultStatus == LabResultStatusEnum.Verified &&
            r.ExpertiseNote == "Ekspertise Patolog" &&
            r.CriticalNote == "Catatan Baru" &&
            r.Items.Count == 2 &&
            r.Items[0].ComponentCode == "HB" &&
            r.Items[0].NumericValue == 12.5m &&
            r.Items[1].ComponentCode == "LEU" &&
            r.Items[1].NumericValue == 8500m)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe<LabOrderModel>.None);

        var cmd = new LabResultIngestFromLisCmd(
            "LAB99999999",
            "LIS-001",
            "Validator",
            DateTime.Now,
            [new LabResultIngestItemDto("HB", "12.0")]);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderInOrderedStatus_ThrowsInvalidOperationException()
    {
        // Arrange: order is Ordered but not yet Charged
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var comp = LabOrderTestSupport.SampleComponent();
        var order = LabOrderTestSupport.CreateEmrOrder(
            "LAB00000006",
            lines: [new LabOrderModel.ResolvedOrderLine(item, [comp])],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var cmd = new LabResultIngestFromLisCmd(
            order.OrderId,
            "LIS-001",
            "Validator",
            DateTime.Now,
            [new LabResultIngestItemDto("HB", "12.0")]);

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ingesti hasil LIS tidak diperbolehkan*");
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }
}
