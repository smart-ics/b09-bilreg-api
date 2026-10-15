using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderGetHandlerTest
{
    private const string OrderId = "LBO09A8QKSFR";
    private const string EmrOrderId = "EMR-20261015-001";
    private const string OrderNo = "LAB20261015001";
    private static readonly DateTime FixedNow = new(2026, 10, 15, 14, 0, 0);

    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderGetHandler _sut;

    public LabOrderGetHandlerTest()
    {
        _sut = new LabOrderGetHandler(_repo.Object);
    }

    private static LabOrderModel CreateSampleOrder(
        LabOrderUrgencyEnum orderUrgency = LabOrderUrgencyEnum.Cito,
        LabOrderUrgencyEnum itemUrgency = LabOrderUrgencyEnum.Cito)
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR001", "Budi Santoso", new DateTime(1985, 5, 20), "L", 41);

        var item = LabOrderItemModel.Create(
            testDefinitionId: "DEF-CKMB",
            labTestCode: "CKMB",
            labTestName: "Creatine Kinase-MB",
            tarifId: "TRF-CKMB",
            tarifCode: "CKMB01",
            tarifName: "Pemeriksaan CK-MB",
            tubeType: VacutainerTypeEnum.Edta,
            specimenType: "Serum",
            requiredTubeCount: 1,
            urgency: itemUrgency);

        var component = LabOrderTestSupport.SampleComponent(
            componentId: "CMP-CKMB-VAL",
            componentCode: "CKMB_VAL",
            componentName: "Kadar CK-MB",
            resultType: LabResultTypeEnum.Numeric,
            unit: "U/L",
            referenceRangeText: "< 25",
            sequenceNo: 1,
            isMandatory: true);

        var line = new LabOrderModel.ResolvedOrderLine(item, [component]);
        var audit = new AuditInfoType("DOKTER01", FixedNow);

        return LabOrderModel.CreateFromEmr(
            emrOrderId: EmrOrderId,
            snapshot: snapshot,
            lines: [line],
            orderNo: OrderNo,
            audit: audit,
            dokterPerujukId: "DOK001",
            dokterPerujukName: "dr. Andi, Sp.PK",
            layananPerujukId: "LAY001",
            layananPerujukName: "Instalasi Gawat Darurat",
            clinicalNote: "Nyeri dada akut tipikal 2 jam",
            urgency: orderUrgency);
    }

    [Fact]
    public async Task Handle_GivenEmptyOrderId_ThrowsArgumentException()
    {
        var act = async () => await _sut.Handle(new LabOrderGetQuery(""), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_GivenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == OrderId)))
            .Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(new LabOrderGetQuery(OrderId), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*LabOrder '{OrderId}' not found*");
    }

    [Fact]
    public async Task Handle_GivenCitoOrder_ReturnsFullDetailWithClinicalContextAndUrgency()
    {
        // Arrange
        var order = CreateSampleOrder(LabOrderUrgencyEnum.Cito, LabOrderUrgencyEnum.Cito);
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        // Act
        var result = await _sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.OrderId);
        result.EmrOrderId.Should().Be(EmrOrderId);
        result.OrderNo.Should().Be(OrderNo);

        // Clinical context & urgency assertions on header
        result.DokterPerujukId.Should().Be("DOK001");
        result.DokterPerujukName.Should().Be("dr. Andi, Sp.PK");
        result.LayananPerujukId.Should().Be("LAY001");
        result.LayananPerujukName.Should().Be("Instalasi Gawat Darurat");
        result.ClinicalNote.Should().Be("Nyeri dada akut tipikal 2 jam");
        result.Urgency.Should().Be((int)LabOrderUrgencyEnum.Cito);
        result.IsCito.Should().BeTrue();

        // Item-level urgency assertions
        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.TestDefinitionId.Should().Be("DEF-CKMB");
        item.LabTestName.Should().Be("Creatine Kinase-MB");
        item.Urgency.Should().Be((int)LabOrderUrgencyEnum.Cito);
        item.IsCito.Should().BeTrue();

        // Component assertions
        result.ItemComponents.Should().HaveCount(1);
        var component = result.ItemComponents.First();
        component.ComponentCode.Should().Be("CKMB_VAL");
        component.Unit.Should().Be("U/L");
    }

    [Fact]
    public async Task Handle_GivenRutinOrder_ReturnsRutinUrgencyAndIsCitoFalse()
    {
        // Arrange
        var order = CreateSampleOrder(LabOrderUrgencyEnum.Rutin, LabOrderUrgencyEnum.Rutin);
        _repo.Setup(r => r.LoadEntity(It.Is<ILabOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));

        // Act
        var result = await _sut.Handle(new LabOrderGetQuery(order.OrderId), CancellationToken.None);

        // Assert
        result.Urgency.Should().Be((int)LabOrderUrgencyEnum.Rutin);
        result.IsCito.Should().BeFalse();

        var item = result.Items.Single();
        item.Urgency.Should().Be((int)LabOrderUrgencyEnum.Rutin);
        item.IsCito.Should().BeFalse();
    }
}
