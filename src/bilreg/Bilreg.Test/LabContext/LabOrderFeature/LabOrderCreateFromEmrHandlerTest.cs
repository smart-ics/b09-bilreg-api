using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCreateFromEmrHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly Mock<ILabTestResolutionService> _resolution = new();
    private readonly LabOrderCreateFromEmrHandler _sut;

    public LabOrderCreateFromEmrHandlerTest()
    {
        _sequencer.Setup(s => s.GetNextNoUrut("LAB")).Returns(42);
        _resolution
            .Setup(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()))
            .Returns([new ResolvedLabOrderLine(LabOrderTestSupport.SampleItem(), [])]);
        _sut = new LabOrderCreateFromEmrHandler(_repo.Object, _sequencer.Object, _resolution.Object, TestTglJamProvider.Instance);
    }

    private static LabOrderCreateFromEmrCmd ValidCmd() =>
        new(
            UserId: "U1",
            EmrOrderId: "EMR-ORDER-001",
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Tes",
            BirthDateYmd: "1990-05-15",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TR1", "Tarif HB")]);

    [Fact]
    public async Task Handle_ValidRequest_SavesAndReturnsEmrAck()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmd(), CancellationToken.None);

        result.EmrOrderId.Should().Be("EMR-ORDER-001");
        result.OrderNo.Should().Be("LAB00000042");
        result.LabOrderStatus.Should().Be((int)LabOrderStatusEnum.Ordered);
        persisted.Should().NotBeNull();
        persisted!.EmrOrderId.Should().Be("EMR-ORDER-001");
        persisted.Items.Should().HaveCount(1);
        persisted.Items.First().TestDefinitionId.Should().Be("LTD0001");
        _resolution.Verify(r => r.ResolveByTarifItems(It.IsAny<IEnumerable<LabOrderTarifItemInput>>()), Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyRegId_Throws()
    {
        var cmd = ValidCmd() with { RegId = "" };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyItems_Throws()
    {
        var cmd = ValidCmd() with { Items = [] };
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
