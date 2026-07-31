using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionQueueAssociateBookingCmd(string AntrianId, int NoUrut, string LoketKey,
    byte[] ExpectedRowVersion, string BookingId, string UserId) : IRequest<AdmissionQueueOperationResponse>;

public sealed class AdmissionQueueAssociateBookingHandler : IRequestHandler<AdmissionQueueAssociateBookingCmd, AdmissionQueueOperationResponse>
{
    private readonly IAntrianRepo _queues; private readonly IBookingRepo _bookings;
    private readonly IPpaRepo _ppas; private readonly IPasienTrackerRepo _trackers;
    private readonly IAdmissionQueueOperationRepo _operations; private readonly IAdmissionServicePointResolver _points;
    private readonly IAdmissionQueueRefreshPublisher _publisher;
    public AdmissionQueueAssociateBookingHandler(IAntrianRepo queues, IBookingRepo bookings, IPpaRepo ppas,
        IPasienTrackerRepo trackers, IAdmissionQueueOperationRepo operations, IAdmissionServicePointResolver points,
        IAdmissionQueueRefreshPublisher publisher)
    { _queues=queues; _bookings=bookings; _ppas=ppas; _trackers=trackers; _operations=operations; _points=points; _publisher=publisher; }
    public async Task<AdmissionQueueOperationResponse> Handle(AdmissionQueueAssociateBookingCmd request, CancellationToken ct)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId); Guard.Against.NullOrWhiteSpace(request.LoketKey);
        Guard.Against.NullOrWhiteSpace(request.BookingId); Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrEmpty(request.ExpectedRowVersion); if (request.NoUrut <= 0) throw new ArgumentOutOfRangeException(nameof(request.NoUrut));
        if (!_operations.HasInServiceClaim(request.AntrianId, request.NoUrut, request.LoketKey, request.ExpectedRowVersion))
            throw new AdmissionQueueConcurrencyException("Admission queue claim is stale or not In Service.");
        var booking = _bookings.LoadEntity(BookingModel.Key(request.BookingId)).GetValueOrThrow($"Booking '{request.BookingId}' not found");
        var tracker = BookingTrackerResolver.Resolve(_queues, _ppas, _trackers, booking).Tracker;
        var queue = _queues.LoadEntity(AntrianModel.Key(request.AntrianId)).GetValueOrThrow($"Admission queue '{request.AntrianId}' not found");
        _points.EnsureAdmissionQueue(queue);
        var entry = queue.ListEntry.First(x => x.NoUrut == request.NoUrut);
        if (PasienTrackerStableIdentity.IsRealTrackerId(entry.Tracker.PasienTrackerId))
        {
            if (entry.Tracker.PasienTrackerId != tracker.PasienTrackerId) throw new InvalidOperationException("Queue entry belongs to a different Tracker.");
            return new(request.AntrianId, request.NoUrut, "InService");
        }
        AdmissionQueueIdentify.RequireAnonymousInServiceEntry(queue, request.NoUrut);
        using var trans = TransHelper.NewScope();
        AdmissionQueueIdentify.IdentifyExistingTrackerAndRecordEvidence(queue, entry, tracker);
        if (!_queues.TrySaveAnonymousInServiceTransition(queue, entry)) throw new AdmissionQueueConcurrencyException("Queue entry changed concurrently.");
        _trackers.SaveChanges(tracker); trans.Complete();
        await _publisher.PublishAsync(request.LoketKey, ct);
        return new(request.AntrianId, request.NoUrut, "InService");
    }
}
