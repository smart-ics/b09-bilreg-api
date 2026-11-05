using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class MergeBillingDomainServiceTests
{
    private const string SourceRegId = "REG-SRC-001";
    private const string TargetRegId = "REG-TGT-001";

    private readonly Mock<IProjectionRegenerationDomainService> _projectionService = new();
    private readonly Mock<IRegRelationPolicy> _regRelationPolicy = new();

    private MergeBillingDomainService CreateSut() =>
        new(_projectionService.Object, _regRelationPolicy.Object);

    [Fact]
    public void Constructor_GivenNullProjectionService_ThrowsArgumentNullException()
    {
        var act = () => new MergeBillingDomainService(null!, _regRelationPolicy.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("projectionService");
    }

    [Fact]
    public void Constructor_GivenNullRegRelationPolicy_ThrowsArgumentNullException()
    {
        var act = () => new MergeBillingDomainService(_projectionService.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("regRelationPolicy");
    }

    [Fact]
    public void Execute_GivenUnrelatedRegistrations_ThrowsInvalidOperationException()
    {
        // Arrange
        _regRelationPolicy
            .Setup(p => p.IsRelated(SourceRegId, TargetRegId))
            .Returns(false);

        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-01", SourceRegId, 50_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-02", TargetRegId, 30_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(TargetRegId, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-001", SourceRegId, TargetRegId);

        var sut = CreateSut();

        // Act
        var act = () => sut.Execute(mergeRequest, source, target);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Registrasi sumber dan tujuan tidak memiliki keterkaitan relasi yang sah.");

        mergeRequest.Status.Should().Be(MergeRequestStatusEnum.Pending);
        source.ListTrsBill.Should().ContainSingle();
        target.ListTrsBill.Should().ContainSingle();
        _projectionService.Verify(p => p.ClearProjection(It.IsAny<TataRekeningModel>()), Times.Never);
    }

    [Fact]
    public void Execute_GivenLegitimateRelation_SuccessfullyExecutesMerge()
    {
        // Arrange
        _regRelationPolicy
            .Setup(p => p.IsRelated(SourceRegId, TargetRegId))
            .Returns(true);

        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-01", SourceRegId, 50_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-02", TargetRegId, 30_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(TargetRegId, targetBill);
        var mergeRequest = MergeRequestModel.Create("MR-001", SourceRegId, TargetRegId);

        var sut = CreateSut();

        // Act
        var result = sut.Execute(mergeRequest, source, target);

        // Assert
        result.Should().NotBeNull();
        result.MergeRequest.Status.Should().Be(MergeRequestStatusEnum.Executed);
        source.ListTrsBill.Should().BeEmpty();
        target.ListTrsBill.Should().HaveCount(2);
        target.ListTrsBill.Should().Contain(b => b.TrsBillingId == "BILL-01");
        target.ListTrsBill.Single(b => b.TrsBillingId == "BILL-01").Reg.RegId.Should().Be(TargetRegId);
        _projectionService.Verify(p => p.ClearProjection(target), Times.Once);
        _regRelationPolicy.Verify(p => p.IsRelated(SourceRegId, TargetRegId), Times.Once);
    }

    [Fact]
    public void Execute_GivenNullArguments_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId);
        var target = TataRekeningTestDataBuilder.HydrateClosed(TargetRegId);
        var request = MergeRequestModel.Create("MR-001", SourceRegId, TargetRegId);

        var act1 = () => sut.Execute(null!, source, target);
        var act2 = () => sut.Execute(request, null!, target);
        var act3 = () => sut.Execute(request, source, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
    }
}
