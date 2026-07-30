using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record BookingAssistanceIntakeCmd(
    string BookingId,
    string ServicePointId,
    string? FailureCode,
    string KioskId,
    string UserId) : IRequest<BookingAssistanceIntakeResponse>;

public record BookingAssistanceIntakeResponse(
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    bool Existing);

public sealed class BookingAssistanceIntakeHandler
    : IRequestHandler<BookingAssistanceIntakeCmd, BookingAssistanceIntakeResponse>
{
    private readonly IBookingRepo _bookings;
    private readonly IAdmissionServicePointRepo _servicePoints;
    private readonly IAntrianRepo _queues;
    private readonly IAntrianFactory _factory;
    private readonly IBookingAssistanceRepo _assistance;
    private readonly ITglJamProvider _clock;

    public BookingAssistanceIntakeHandler(
        IBookingRepo bookings,
        IAdmissionServicePointRepo servicePoints,
        IAntrianRepo queues,
        IAntrianFactory factory,
        IBookingAssistanceRepo assistance,
        ITglJamProvider clock)
    {
        _bookings = bookings;
        _servicePoints = servicePoints;
        _queues = queues;
        _factory = factory;
        _assistance = assistance;
        _clock = clock;
    }

    public Task<BookingAssistanceIntakeResponse> Handle(
        BookingAssistanceIntakeCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BookingId);
        Guard.Against.NullOrWhiteSpace(request.ServicePointId);
        Guard.Against.NullOrWhiteSpace(request.KioskId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        _bookings.LoadEntity(BookingModel.Key(request.BookingId))
            .GetValueOrThrow($"Booking '{request.BookingId}' not found");

        var existing = _assistance.FindActive(request.BookingId);
        if (existing is not null)
            return Task.FromResult(ToResponse(existing, existing: true));

        var servicePoint = _servicePoints
            .LoadEntity(AdmissionServicePointModel.Key(request.ServicePointId))
            .GetValueOrThrow($"Admission Service Point '{request.ServicePointId}' not found");
        servicePoint.EnsureCanAcceptIntake();

        var at = _clock.Now;
        var businessDate = DateOnly.FromDateTime(at);
        var queue = ResolveOrCreateQueue(servicePoint, businessDate);
        var entry = queue.AddAdmissionEntry(at);
        var correlation = $"BOOKING-ASSISTANCE:{request.BookingId}";

        try
        {
            using var trans = TransHelper.NewScope();
            if (!_assistance.TryCreate(
                    request.BookingId,
                    correlation,
                    request.FailureCode,
                    request.KioskId,
                    request.UserId,
                    at,
                    queue,
                    entry))
                throw new BookingAssistanceRaceException();

            trans.Complete();
        }
        catch (BookingAssistanceRaceException)
        {
            var winner = _assistance.FindActive(request.BookingId)
                ?? throw new AdmissionQueueConcurrencyException(
                    "Booking assistance was created concurrently; reload required.");
            return Task.FromResult(ToResponse(winner, existing: true));
        }

        return Task.FromResult(new BookingAssistanceIntakeResponse(
            queue.AntrianId,
            entry.NoUrut,
            queue.FormatQueueLabel(entry.NoUrut) ?? string.Empty,
            Existing: false));
    }

    private AntrianModel ResolveOrCreateQueue(
        AdmissionServicePointModel servicePoint,
        DateOnly businessDate)
    {
        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);
        var existingView = _queues.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag);

        return existingView is null
            ? _factory.Create(servicePoint, businessDate)
            : _queues.LoadEntity(existingView).Value;
    }

    private static BookingAssistanceIntakeResponse ToResponse(
        BookingAssistanceActive active,
        bool existing) =>
        new(active.AntrianId, active.NoUrut, active.QueueLabel ?? string.Empty, existing);

    private sealed class BookingAssistanceRaceException : Exception;
}
