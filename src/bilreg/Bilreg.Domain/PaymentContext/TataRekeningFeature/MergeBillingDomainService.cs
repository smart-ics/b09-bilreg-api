using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed class MergeBillingDomainService : IMergeBillingDomainService
{
    private readonly IProjectionRegenerationDomainService _projectionService;

    public MergeBillingDomainService(IProjectionRegenerationDomainService projectionService)
    {
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
    }

    public MergeBillingResult Execute(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening)
    {
        ArgumentNullException.ThrowIfNull(mergeRequest);
        ArgumentNullException.ThrowIfNull(sourceTataRekening);
        ArgumentNullException.ThrowIfNull(targetTataRekening);

        ValidateMergeRequest(mergeRequest, sourceTataRekening, targetTataRekening);
        ValidateSource(sourceTataRekening);
        ValidateTarget(targetTataRekening);

        var releasedBills = sourceTataRekening.ReleaseBillingSetForMerge();
        var targetReg = new RegReff(targetTataRekening.RegId, "-", "-");
        targetTataRekening.AcceptMergedBillingSet(releasedBills, targetReg);

        _projectionService.ClearProjection(targetTataRekening);

        mergeRequest.Execute();

        return new MergeBillingResult(mergeRequest, sourceTataRekening, targetTataRekening);
    }

    public MergeBillingResult Execute(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening,
        RegModel sourceReg,
        RegModel targetReg)
    {
        ArgumentNullException.ThrowIfNull(mergeRequest);
        ArgumentNullException.ThrowIfNull(sourceTataRekening);
        ArgumentNullException.ThrowIfNull(targetTataRekening);
        ArgumentNullException.ThrowIfNull(sourceReg);
        ArgumentNullException.ThrowIfNull(targetReg);

        ValidatePatientIdentity(sourceReg, targetReg);
        ValidateMergeRequest(mergeRequest, sourceTataRekening, targetTataRekening);
        ValidateSource(sourceTataRekening);
        ValidateTarget(targetTataRekening);

        var releasedBills = sourceTataRekening.ReleaseBillingSetForMerge();
        var targetRegReff = new RegReff(targetTataRekening.RegId, "-", "-");
        targetTataRekening.AcceptMergedBillingSet(releasedBills, targetRegReff);

        _projectionService.ClearProjection(targetTataRekening);

        mergeRequest.Execute();

        return new MergeBillingResult(mergeRequest, sourceTataRekening, targetTataRekening);
    }

    public void ValidatePatientIdentity(RegModel sourceReg, RegModel targetReg)
    {
        ArgumentNullException.ThrowIfNull(sourceReg);
        ArgumentNullException.ThrowIfNull(targetReg);

        var sourceNoRm = sourceReg.Pasien?.PasienId;
        var targetNoRm = targetReg.Pasien?.PasienId;

        ValidatePatientIdentity(sourceNoRm, targetNoRm);
    }

    public void ValidatePatientIdentity(string? sourceNoRm, string? targetNoRm)
    {
        if (string.IsNullOrWhiteSpace(sourceNoRm) ||
            string.IsNullOrWhiteSpace(targetNoRm) ||
            !string.Equals(targetNoRm.Trim(), sourceNoRm.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainViolationException(
                $"Inkonsistensi integritas pasien: Target NoRM '{targetNoRm}' " +
                $"tidak cocok dengan Source NoRM '{sourceNoRm}'. Auto-Merge dibatalkan.");
        }
    }

    public void ValidateAutoMergeIntegrity(
        RegModel sourceReg,
        RegModel targetReg,
        TataRekeningModel? sourceTataRekening = null,
        TataRekeningModel? targetTataRekening = null)
    {
        ValidatePatientIdentity(sourceReg, targetReg);

        if (sourceTataRekening is not null)
        {
            if (sourceTataRekening.Status == TataRekeningStatusEnum.Lunas)
                throw new DomainViolationException("Registrasi sumber tidak boleh berstatus LUNAS.");

            if (sourceTataRekening.Status == TataRekeningStatusEnum.Finalized)
                throw new DomainViolationException("Registrasi sumber tidak dapat di-merge karena sudah FINALIZED.");
        }

        if (targetTataRekening is not null)
        {
            if (targetTataRekening.Status == TataRekeningStatusEnum.Finalized)
                throw new DomainViolationException("Registrasi tujuan telah berstatus FINALIZED.");

            if (targetTataRekening.Status == TataRekeningStatusEnum.Lunas)
                throw new DomainViolationException("Registrasi tujuan tidak boleh berstatus LUNAS.");
        }
    }

    private static void ValidateMergeRequest(
        MergeRequestModel mergeRequest,
        TataRekeningModel sourceTataRekening,
        TataRekeningModel targetTataRekening)
    {
        if (mergeRequest.Status != MergeRequestStatusEnum.Pending)
            throw new InvalidOperationException(
                $"Merge Billing hanya dapat dilakukan terhadap Merge Request berstatus Pending (status: {mergeRequest.Status}).");

        if (!string.Equals(mergeRequest.SourceRegId, sourceTataRekening.RegId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Source Registrasi pada Merge Request tidak sesuai dengan Tata Rekening sumber.");

        if (string.IsNullOrWhiteSpace(mergeRequest.TargetRegId))
            throw new InvalidOperationException(
                "Merge Request memerlukan Target Registrasi sebelum Merge Billing dieksekusi.");

        if (!string.Equals(mergeRequest.TargetRegId, targetTataRekening.RegId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Target Registrasi pada Merge Request tidak sesuai dengan Tata Rekening tujuan.");
    }

    private static void ValidateSource(TataRekeningModel sourceTataRekening)
    {
        if (sourceTataRekening.Status == TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Registrasi sumber tidak boleh berstatus LUNAS.");

        if (sourceTataRekening.Status == TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "Registrasi sumber tidak dapat di-merge karena sudah FINALIZED.");
    }

    private static void ValidateTarget(TataRekeningModel targetTataRekening)
    {
        if (targetTataRekening.Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Merge Billing hanya dapat dilakukan apabila Billing Registrasi tujuan berstatus CLOSED.");

        if (targetTataRekening.Status == TataRekeningStatusEnum.Finalized)
            throw new InvalidOperationException(
                "Registrasi tujuan telah berstatus FINALIZED.");

        if (targetTataRekening.Status == TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Registrasi tujuan tidak boleh berstatus LUNAS.");
    }
}
