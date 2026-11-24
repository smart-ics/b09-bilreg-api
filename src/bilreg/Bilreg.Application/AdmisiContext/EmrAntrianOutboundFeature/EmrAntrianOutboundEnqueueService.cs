using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundEnqueueService
{
    private readonly IEmrAntrianOutboundQueueRepo _queueRepo;

    public EmrAntrianOutboundEnqueueService(IEmrAntrianOutboundQueueRepo queueRepo)
    {
        _queueRepo = queueRepo;
    }

    /// <summary>
    /// Enqueue Booking Queue Number Assigned delivery to EMR. Returns QueueId or null when an active row already exists.
    /// </summary>
    public string? TryEnqueueAddBooking(AddAntrianEmrByBookingCmd cmd, DateTime createdAt)
    {
        if (_queueRepo.FindActiveBySource(cmd.BookingId, EmrAntrianOutboundQueueModel.MessageTypeAddBooking).HasValue)
            return null;

        var payloadJson = EmrAntrianOutboundPayloadBuilder.SerializeBooking(cmd);
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            cmd.BookingId,
            EmrAntrianOutboundQueueModel.MessageTypeAddBooking,
            payloadJson,
            createdAt);
        _queueRepo.SaveChanges(queue);
        return queue.QueueId;
    }

    /// <summary>
    /// Enqueue registration queue publication to EMR. Returns QueueId or null when an active row already exists.
    /// </summary>
    public string? TryEnqueueAddReg(AddAntrianEmrByRegCommand cmd, DateTime createdAt)
    {
        if (_queueRepo.FindActiveBySource(cmd.RegId, EmrAntrianOutboundQueueModel.MessageTypeAddReg).HasValue)
            return null;

        var payloadJson = EmrAntrianOutboundPayloadBuilder.SerializeReg(cmd);
        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            cmd.RegId,
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            payloadJson,
            createdAt);
        _queueRepo.SaveChanges(queue);
        return queue.QueueId;
    }

    /// <summary>
    /// Enqueue or update registration queue publication to EMR.
    /// Pending -> updates payload in place; Processing -> returns existing QueueId;
    /// Failed -> refreshes payload and resets to Pending; Succeeded/none -> inserts new Pending row.
    /// </summary>
    public string EnqueueOrUpdateAddReg(AddAntrianEmrByRegCommand cmd, DateTime createdAt)
    {
        var payloadJson = EmrAntrianOutboundPayloadBuilder.SerializeReg(cmd);

        var active = _queueRepo.FindPendingOrProcessingBySource(cmd.RegId, EmrAntrianOutboundQueueModel.MessageTypeAddReg);
        if (active.HasValue)
        {
            var existing = active.Value;
            if (existing.QueueStatus == EmrAntrianOutboundQueueStatusEnum.Pending)
            {
                existing.RefreshPayload(payloadJson);
                _queueRepo.SaveChanges(existing);
            }

            return existing.QueueId;
        }

        var failed = _queueRepo.FindLatestFailedBySource(cmd.RegId, EmrAntrianOutboundQueueModel.MessageTypeAddReg);
        if (failed.HasValue)
        {
            var existingFailed = failed.Value;
            existingFailed.RefreshPayload(payloadJson);
            _queueRepo.SaveChanges(existingFailed);
            return existingFailed.QueueId;
        }

        var queue = EmrAntrianOutboundQueueModel.CreatePending(
            cmd.RegId,
            EmrAntrianOutboundQueueModel.MessageTypeAddReg,
            payloadJson,
            createdAt);
        _queueRepo.SaveChanges(queue);
        return queue.QueueId;
    }
}
