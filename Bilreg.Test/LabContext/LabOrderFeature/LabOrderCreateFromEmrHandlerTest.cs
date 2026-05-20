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
    private readonly LabOrderCreateFromEmrHandler _sut;

    public LabOrderCreateFromEmrHandlerTest()
    {
        _sequencer.Setup(s => s.GetNextNoUrut("LAB")).Returns(42);
        _sut = new LabOrderCreateFromEmrHandler(_repo.Object, _sequencer.Object);
    }

    private static LabOrderCreateFromEmrCmd ValidCmd() =>
        new(
            UserId: "U1",
            RegId: "REG001",
            PatientId: "MR0001",
            PatientName: "Pasien Tes",
            BirthDateYmd: "1990-05-15",
            Gender: "L",
            Items:
            [
                new LabOrderItemInput(
                    "T1", "HB", "Hemoglobin", "TR1", "T-HB", "Tarif HB",
                    VacutainerTypeEnum.Edta, "Blood", 1)
            ]);

    [Fact]
    public async Task Handle_ValidRequest_SavesAndReturnsOrderIds()
    {
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(ValidCmd(), CancellationToken.None);

        result.OrderId.Should().NotBeNullOrWhiteSpace();
        result.OrderNo.Should().Be("LAB00000042");
        persisted.Should().NotBeNull();
        result.OrderId.Should().Be(persisted!.OrderId);
        persisted.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        persisted.OwareStatus.Should().Be(OwareStatusEnum.Pending);
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
