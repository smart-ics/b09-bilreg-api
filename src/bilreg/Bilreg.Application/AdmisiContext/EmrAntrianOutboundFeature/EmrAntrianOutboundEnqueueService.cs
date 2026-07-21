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
}
