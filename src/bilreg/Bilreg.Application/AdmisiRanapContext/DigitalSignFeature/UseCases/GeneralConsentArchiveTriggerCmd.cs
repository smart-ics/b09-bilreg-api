using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;

public record GeneralConsentArchiveTriggerCmd(
    string? SigningRequestId = null,
    string? RegId = null,
    string? DokumenId = null,
    int BatchSize = 50,
    string TriggerType = "MANUAL",
    string UserId = "SYSTEM") : IRequest<GeneralConsentArchiveBatchResult>;

public class GeneralConsentArchiveTriggerHandler
    : IRequestHandler<GeneralConsentArchiveTriggerCmd, GeneralConsentArchiveBatchResult>
{
    private readonly GeneralConsentArchiveWorker _worker;

    public GeneralConsentArchiveTriggerHandler(GeneralConsentArchiveWorker worker)
    {
        _worker = worker;
    }

    public async Task<GeneralConsentArchiveBatchResult> Handle(
        GeneralConsentArchiveTriggerCmd request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SigningRequestId))
        {
            var singleRes = await _worker.ProcessOneAsync(
                request.SigningRequestId.Trim(),
                request.TriggerType,
                request.UserId,
                cancellationToken);

            return new GeneralConsentArchiveBatchResult(
                ProcessedCount: 1,
                SucceededCount: singleRes.Success && !singleRes.IsAlreadyArchived ? 1 : 0,
                FailedCount: singleRes.Success ? 0 : 1,
                SkippedCount: singleRes.IsAlreadyArchived ? 1 : 0,
                Details: [singleRes]);
        }

        if (!string.IsNullOrWhiteSpace(request.RegId) && !string.IsNullOrWhiteSpace(request.DokumenId))
        {
            var singleRes = await _worker.ProcessCorrelationAsync(
                request.RegId.Trim(),
                request.DokumenId.Trim(),
                request.TriggerType,
                request.UserId,
                cancellationToken);

            return new GeneralConsentArchiveBatchResult(
                ProcessedCount: 1,
                SucceededCount: singleRes.Success && !singleRes.IsAlreadyArchived ? 1 : 0,
                FailedCount: singleRes.Success ? 0 : 1,
                SkippedCount: singleRes.IsAlreadyArchived ? 1 : 0,
                Details: [singleRes]);
        }

        return await _worker.ProcessBatchAsync(
            request.BatchSize > 0 ? request.BatchSize : 50,
            request.TriggerType,
            request.UserId,
            cancellationToken);
    }
}
