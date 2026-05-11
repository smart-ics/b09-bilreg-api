using Bilreg.Domain.IgdContext.IgdVisitFeature;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public record IgdVisitEventDto(
    string IgdVisitId,
    int NoEvent,
    string EventKind,
    DateTime EventDateTime,
    string UserId,
    string Notes)
{
    public static IgdVisitEventDto FromModel(string igdVisitId, IgdVisitEventType evt)
        => new(
            IgdVisitId: igdVisitId,
            NoEvent: evt.NoEvent,
            EventKind: evt.EventKind.ToCode(),
            EventDateTime: evt.EventDateTime,
            UserId: evt.UserId,
            Notes: evt.Notes);

    public IgdVisitEventType ToModel()
        => new(
            NoEvent: NoEvent,
            EventKind: EventKind.ToIgdEventEnum(),
            EventDateTime: EventDateTime,
            UserId: UserId,
            Notes: Notes);
}
