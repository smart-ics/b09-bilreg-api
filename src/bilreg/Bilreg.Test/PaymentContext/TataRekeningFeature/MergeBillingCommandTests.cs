using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class MergeBillingCommandTests
{
    private const string SourceRegId = "REG-SRC-001";
    private const string TargetRegId = "REG-TGT-001";
    private const string MergeRequestId = "MR-TEST-001";
    private const string UserId = "user-123";

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<IMergeRequestRepo> _mergeRequestRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<ITransferReceivableService> _transferReceivableService = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly ICurrentUserContext _currentUser = new FixedCurrentUserContext(UserId);
    private readonly Mock<IRegRelationPolicy> _regRelationPolicy = new();
    private readonly IProjectionRegenerationDomainService _projectionService = new ProjectionRegenerationDomainService();

    public MergeBillingCommandTests()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    private MergeBillingHandler CreateHandler()
    {
        var domainService = new MergeBillingDomainService(_projectionService, _regRelationPolicy.Object);
        return new MergeBillingHandler(
            _tataRekeningRepo.Object,
            _mergeRequestRepo.Object,
            _trsBillingRepo.Object,
            domainService,
            _transferReceivableService.Object,
            _auditRepo.Object,
            _unitOfWork.Object,
            _currentUser,
            TestTglJamProvider.Instance);
    }

    [Fact]
    public async Task Handle_GivenUnrelatedRegistrations_ThrowsInvalidOperationException_AndDoesNotPersist()
    {
        // Arrange
        _regRelationPolicy
            .Setup(p => p.IsRelated(SourceRegId, TargetRegId))
            .Returns(false);

        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-SRC", SourceRegId, 45_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-TGT", TargetRegId, 25_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(TargetRegId, targetBill);
        var mergeRequest = MergeRequestModel.Create(MergeRequestId, SourceRegId, TargetRegId);

        _mergeRequestRepo
            .Setup(r => r.LoadEntity(It.Is<IMergeRequestKey>(k => k.MergeRequestId == MergeRequestId)))
            .Returns(MayBe.From(mergeRequest));
        _tataRekeningRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == SourceRegId)))
            .Returns(MayBe.From(source));
        _tataRekeningRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == TargetRegId)))
            .Returns(MayBe.From(target));

        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(new MergeBillingCommand(MergeRequestId, UserId), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Registrasi sumber dan tujuan tidak memiliki keterkaitan relasi yang sah.");

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _mergeRequestRepo.Verify(r => r.SaveChanges(It.IsAny<MergeRequestModel>()), Times.Never);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.IsAny<TrsBillType>()), Times.Never);
        _transferReceivableService.Verify(s => s.Transfer(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Never);
    }

    [Fact]
    public async Task Handle_GivenLegitimateRelation_SuccessfullyExecutes_AndPersistsMergePermanently()
    {
        // Arrange
        _regRelationPolicy
            .Setup(p => p.IsRelated(SourceRegId, TargetRegId))
            .Returns(true);

        var sourceBill = TataRekeningTestDataBuilder.CreateBill("BILL-SRC", SourceRegId, 45_000m);
        var targetBill = TataRekeningTestDataBuilder.CreateBill("BILL-TGT", TargetRegId, 25_000m);
        var source = TataRekeningTestDataBuilder.HydrateClosed(SourceRegId, sourceBill);
        var target = TataRekeningTestDataBuilder.HydrateClosed(TargetRegId, targetBill);
        var mergeRequest = MergeRequestModel.Create(MergeRequestId, SourceRegId, TargetRegId);

        _mergeRequestRepo
            .Setup(r => r.LoadEntity(It.Is<IMergeRequestKey>(k => k.MergeRequestId == MergeRequestId)))
            .Returns(MayBe.From(mergeRequest));
        _tataRekeningRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == SourceRegId)))
            .Returns(MayBe.From(source));
        _tataRekeningRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == TargetRegId)))
            .Returns(MayBe.From(target));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new MergeBillingCommand(MergeRequestId, UserId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MergeRequest.Status.Should().Be(MergeRequestStatusEnum.Executed);
        result.TargetSummary.RegId.Should().Be(TargetRegId);
        source.ListTrsBill.Should().BeEmpty();
        target.ListTrsBill.Should().HaveCount(2);

        _tataRekeningRepo.Verify(r => r.SaveChanges(source), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(target), Times.Once);
        _mergeRequestRepo.Verify(r => r.SaveChanges(It.Is<MergeRequestModel>(m =>
            m.MergeRequestId == MergeRequestId &&
            m.Status == MergeRequestStatusEnum.Executed)), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(It.Is<TrsBillType>(b => b.TrsBillingId == "BILL-SRC")), Times.Once);
        _transferReceivableService.Verify(s => s.Transfer(SourceRegId, TargetRegId), Times.Once);
        _auditRepo.Verify(r => r.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "TATA_REKENING_MERGE_BILLING" &&
            a.EntityName == nameof(MergeRequestModel) &&
            a.EntityId == MergeRequestId)), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }
}
