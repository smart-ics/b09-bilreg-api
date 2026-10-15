using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderRepoTest
{
    private readonly Mock<ILabOrderDal> _orderDal = new();
    private readonly Mock<ILabOrderItemDal> _itemDal = new();
    private readonly Mock<ILabOrderItemComponentDal> _itemComponentDal = new();
    private readonly LabOrderRepo _sut;

    public LabOrderRepoTest()
    {
        _sut = new LabOrderRepo(_orderDal.Object, _itemDal.Object, _itemComponentDal.Object);
    }

    private static LabOrderModel CreateTestModel() =>
        LabOrderTestSupport.CreateEmrOrder(audit: new AuditInfoType("U1", DateTime.Now));

    private static LabOrderDto CreateTestDto(LabOrderModel model)
        => LabOrderDto.FromModel(model);

    [Fact]
    public void SaveChanges_NewEntity_CallsInsertAndReplacesItems()
    {
        var model = CreateTestModel();
        _orderDal.Setup(x => x.GetData(model)).Returns((LabOrderDto)null!);

        _sut.SaveChanges(model);

        _orderDal.Verify(x => x.Insert(It.IsAny<LabOrderDto>()), Times.Once);
        _orderDal.Verify(x => x.Update(It.IsAny<LabOrderDto>()), Times.Never);
        _itemDal.Verify(x => x.Delete(model), Times.Once);
        _itemDal.Verify(x => x.Insert(It.Is<IEnumerable<LabOrderItemDto>>(l => l.Count() == 1)), Times.Once);
        _itemComponentDal.Verify(x => x.Delete(model), Times.Once);
        _itemComponentDal.Verify(x => x.Insert(It.IsAny<IEnumerable<LabOrderItemComponentDto>>()), Times.Once);
    }

    [Fact]
    public void SaveChanges_ExistingEntity_CallsUpdate()
    {
        var model = CreateTestModel();
        _orderDal.Setup(x => x.GetData(model)).Returns(CreateTestDto(model));

        _sut.SaveChanges(model);

        _orderDal.Verify(x => x.Update(It.IsAny<LabOrderDto>()), Times.Once);
        _orderDal.Verify(x => x.Insert(It.IsAny<LabOrderDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsAggregateWithItems()
    {
        var model = CreateTestModel();
        var dto = CreateTestDto(model);
        var itemDto = LabOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.GetData(It.IsAny<ILabOrderKey>())).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([itemDto]);
        _itemComponentDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([]);

        var result = _sut.LoadEntity(model);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.Items.Should().HaveCount(1);
                m.OrderId.Should().Be(model.OrderId);
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadEntity_WhenMissing_ReturnsNone()
    {
        var key = LabOrderModel.Key("MISSING");
        _orderDal.Setup(x => x.GetData(key)).Returns((LabOrderDto)null!);

        var result = _sut.LoadEntity(key);

        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListByRegId_ReturnsOrdersMatchingRegId()
    {
        var model = CreateTestModel();
        var dto = CreateTestDto(model);
        var itemDto = LabOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.ListByRegId("REG001")).Returns([dto]);
        _itemDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([itemDto]);
        _itemComponentDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([]);

        var results = _sut.ListByRegId("REG001").ToList();

        results.Should().HaveCount(1);
        results[0].OrderId.Should().Be(model.OrderId);
        results[0].Patient.RegId.Should().Be("REG001");
    }

    [Fact]
    public void SaveChanges_MapsClinicalContextAndUrgencyToDto()
    {
        var model = LabOrderModel.CreateFromEmr(
            emrOrderId: "EMR-TEST-01",
            snapshot: LabOrderTestSupport.EmrSnapshot(),
            lines: [LabOrderTestSupport.ResolvedLine()],
            orderNo: "LAB-TEST-01",
            audit: new AuditInfoType("U1", DateTime.Now),
            dokterPerujukId: "DOC99",
            dokterPerujukName: "dr. Spesialis",
            layananPerujukId: "LAY99",
            layananPerujukName: "Klinik Jantung",
            clinicalNote: "Nyeri dada",
            urgency: LabOrderUrgencyEnum.Cito);

        _orderDal.Setup(x => x.GetData(model)).Returns((LabOrderDto)null!);

        LabOrderDto? captured = null;
        _orderDal.Setup(x => x.Insert(It.IsAny<LabOrderDto>()))
            .Callback<LabOrderDto>(d => captured = d);

        _sut.SaveChanges(model);

        captured.Should().NotBeNull();
        captured!.DokterPerujukId.Should().Be("DOC99");
        captured.DokterPerujukName.Should().Be("dr. Spesialis");
        captured.LayananPerujukId.Should().Be("LAY99");
        captured.LayananPerujukName.Should().Be("Klinik Jantung");
        captured.ClinicalNote.Should().Be("Nyeri dada");
        captured.Urgency.Should().Be(1);
    }

    [Fact]
    public void LoadEntity_MapsClinicalContextAndUrgencyToModel()
    {
        var model = LabOrderModel.CreateFromEmr(
            emrOrderId: "EMR-TEST-02",
            snapshot: LabOrderTestSupport.EmrSnapshot(),
            lines: [LabOrderTestSupport.ResolvedLine()],
            orderNo: "LAB-TEST-02",
            audit: new AuditInfoType("U1", DateTime.Now),
            dokterPerujukId: "DOC88",
            dokterPerujukName: "dr. Bedah",
            layananPerujukId: "LAY88",
            layananPerujukName: "Poli Bedah",
            clinicalNote: "Pre-op",
            urgency: LabOrderUrgencyEnum.Cito);

        var dto = LabOrderDto.FromModel(model);
        var itemDto = LabOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.GetData(It.IsAny<ILabOrderKey>())).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([itemDto]);
        _itemComponentDal.Setup(x => x.ListData(It.IsAny<ILabOrderKey>())).Returns([]);

        var result = _sut.LoadEntity(model);

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.DokterPerujukId.Should().Be("DOC88");
                m.DokterPerujukName.Should().Be("dr. Bedah");
                m.LayananPerujukId.Should().Be("LAY88");
                m.LayananPerujukName.Should().Be("Poli Bedah");
                m.ClinicalNote.Should().Be("Pre-op");
                m.Urgency.Should().Be(LabOrderUrgencyEnum.Cito);
                m.IsCito.Should().BeTrue();
                m.Items.First().Urgency.Should().Be(LabOrderUrgencyEnum.Cito);
                m.Items.First().IsCito.Should().BeTrue();
            },
            onNone: () => Assert.Fail("Expected Some"));
    }
}
