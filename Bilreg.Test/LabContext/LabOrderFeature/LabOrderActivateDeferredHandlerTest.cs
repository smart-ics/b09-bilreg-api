using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderActivateDeferredHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ILabRegIntegration> _regIntegration = new();
    private readonly LabOrderActivateDeferredHandler _sut;

    public LabOrderActivateDeferredHandlerTest()
    {
        _sut = new LabOrderActivateDeferredHandler(_repo.Object, _regIntegration.Object);
    }

    private static LabOrderModel DeferredOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");
        return order;
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRegIntegrationAndSaves()
    {
        var order = DeferredOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _regIntegration
            .Setup(i => i.CreateExecutionRegistration(It.IsAny<LabRegExecutionRequest>()))
            .Returns("REG-FAKE-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderActivateDeferredCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.ExecutionRegId.Should().Be("REG-FAKE-0001");
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        persisted.ExecutionRegId.Should().Be("REG-FAKE-0001");
        persisted.DeferredInfo.IsEmpty.Should().BeTrue();
        _regIntegration.Verify(
            i => i.CreateExecutionRegistration(It.Is<LabRegExecutionRequest>(r =>
                r.OrderId == order.OrderId && r.UserId == "U3")),
            Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }
}
