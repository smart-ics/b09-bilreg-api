using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningPhase4ApplicationTest
{
    private readonly Mock<ITransferReceivableService> _transferReceivableService = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();

    [Fact]
    public async Task P4_01_GivenTransferReceivableFails_WhenMergeBilling_ThenDoesNotCompleteTransaction()
    {
        var phase3 = new TataRekeningPhase3ApplicationTestHarness();
        _transferReceivableService
            .Setup(s => s.Transfer(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Transfer failed"));

        var handler = phase3.CreateMergeBillingHandler(
            _transferReceivableService.Object,
            _auditRepo.Object);

        var act = () => handler.Handle(new MergeBillingCommand("MR-MERGE", "UserId"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        phase3.UnitOfWorkScope.Verify(s => s.Complete(), Times.Never);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<Bilreg.Domain.Shared.AuditLogFeature.AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task P4_02_GivenSuccessfulMerge_WhenMergeBilling_ThenWritesAudit()
    {
        var phase3 = new TataRekeningPhase3ApplicationTestHarness();
        var handler = phase3.CreateMergeBillingHandler(
            _transferReceivableService.Object,
            _auditRepo.Object);

        await handler.Handle(new MergeBillingCommand("MR-MERGE", "UserId"), CancellationToken.None);

        _transferReceivableService.Verify(
            s => s.Transfer(TataRekeningPhase3ApplicationTestHarness.SourceRegId, TataRekeningPhase3ApplicationTestHarness.RegId),
            Times.Once);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<Bilreg.Domain.Shared.AuditLogFeature.AuditLog>()), Times.Once);
    }
}
