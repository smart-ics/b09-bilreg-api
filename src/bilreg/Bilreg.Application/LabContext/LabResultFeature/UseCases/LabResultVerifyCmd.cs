using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabResultFeature.UseCases;

public record LabResultVerifyCmd(
    string OrderId,
    string VerifiedUserId,
    DateTime VerifiedDate,
    string ExpertiseNote = "")
    : IRequest;

public class LabResultVerifyHandler : IRequestHandler<LabResultVerifyCmd>
{
    private static readonly DateTime EmptyDateSentinel = new(3000, 1, 1);

    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabResultDocumentRepo _labResultDocumentRepo;
    private readonly ICurrentUserContext _currentUser;

    public LabResultVerifyHandler(
        ILabOrderRepo labOrderRepo,
        ILabResultDocumentRepo labResultDocumentRepo,
        ICurrentUserContext currentUser)
    {
        _labOrderRepo = labOrderRepo;
        _labResultDocumentRepo = labResultDocumentRepo;
        _currentUser = currentUser;
    }

    public LabResultVerifyHandler(
        ILabOrderRepo labOrderRepo,
        ILabResultDocumentRepo labResultDocumentRepo)
        : this(labOrderRepo, labResultDocumentRepo, null!)
    {
    }

    public Task Handle(LabResultVerifyCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.VerifiedUserId, nameof(request.VerifiedUserId));

        if (request.VerifiedDate == default || request.VerifiedDate.Date == EmptyDateSentinel.Date)
            throw new ArgumentException("VerifiedDate wajib diisi dengan tanggal valid.", nameof(request.VerifiedDate));

        EnsureVerifierAuthorization();

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(request.OrderId))
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Recorded)
            throw new InvalidOperationException(
                $"LabOrder '{request.OrderId}' berstatus {order.LabOrderStatus}; verifikasi hanya diperbolehkan saat status Recorded.");

        var result = _labResultDocumentRepo.LoadByOrderId(request.OrderId)
            .GetValueOrThrow($"LabResultDocument untuk order '{request.OrderId}' tidak ditemukan.");

        if (result.ResultStatus == LabResultStatusEnum.Verified)
            throw new InvalidOperationException(
                $"Hasil untuk order '{request.OrderId}' sudah diverifikasi.");

        result.Verify(request.VerifiedUserId, request.VerifiedDate, request.ExpertiseNote);
        order.MarkVerified(request.VerifiedUserId, request.VerifiedDate);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        _labResultDocumentRepo.SaveChanges(result);
        trans.Complete();

        return Task.CompletedTask;
    }

    private void EnsureVerifierAuthorization()
    {
        var roles = _currentUser?.GetRoles();
        if (roles is null || !roles.Any(IsAuthorizedRoleOrPermission))
        {
            throw new UnauthorizedAccessException(
                "Verifikasi hasil laboratorium hanya dapat dilakukan oleh Dokter Spesialis Patologi Klinik (doktersppk) atau pemilik izin 'lab.result.verify'.");
        }
    }

    private static bool IsAuthorizedRoleOrPermission(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        var normalized = role.Trim();
        return normalized.Equals("doktersppk", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("dokter_sppk", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("sp.pk", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("sppk", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("lab.result.verify", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}
