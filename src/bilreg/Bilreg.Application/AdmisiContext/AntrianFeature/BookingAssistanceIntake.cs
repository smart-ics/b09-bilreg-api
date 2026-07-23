using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record BookingAssistanceIntakeCmd(string BookingId,string ServicePointId,
    string? FailureCode,string KioskId,string UserId):IRequest<BookingAssistanceIntakeResponse>;
public record BookingAssistanceIntakeResponse(string AntrianId,int NoUrut,string QueueLabel,bool Existing);
public record BookingAssistanceActive(string BookingId,string AntrianId,int NoUrut,string? QueueLabel);
public interface IBookingAssistanceRepo
{
    BookingAssistanceActive? FindActive(string bookingId);
    BookingAssistanceActive? FindActiveByEntry(string antrianId,int noUrut);
    bool TryCreate(string bookingId,string correlation,string? failureCode,string kioskId,string userId,
        DateTime at,AntrianModel queue,AntrianEntryModel entry);
}

public sealed class BookingAssistanceIntakeHandler:IRequestHandler<BookingAssistanceIntakeCmd,BookingAssistanceIntakeResponse>
{
    private readonly IBookingRepo _bookings; private readonly IAdmissionServicePointRepo _points;
    private readonly IAntrianRepo _queues; private readonly IAntrianFactory _factory;
    private readonly IBookingAssistanceRepo _assistance; private readonly ITglJamProvider _clock;
    public BookingAssistanceIntakeHandler(IBookingRepo bookings,IAdmissionServicePointRepo points,
      IAntrianRepo queues,IAntrianFactory factory,IBookingAssistanceRepo assistance,ITglJamProvider clock)=>
      (_bookings,_points,_queues,_factory,_assistance,_clock)=(bookings,points,queues,factory,assistance,clock);
    public Task<BookingAssistanceIntakeResponse> Handle(BookingAssistanceIntakeCmd r,CancellationToken ct)
    {
      if(string.IsNullOrWhiteSpace(r.BookingId))throw new ArgumentException("BookingId is required.");
      _bookings.LoadEntity(BookingModel.Key(r.BookingId)).GetValueOrThrow($"Booking '{r.BookingId}' not found");
      var found=_assistance.FindActive(r.BookingId); if(found is not null)return Task.FromResult(ToResponse(found,true));
      var point=_points.LoadEntity(AdmissionServicePointModel.Key(r.ServicePointId)).GetValueOrThrow($"Admission Service Point '{r.ServicePointId}' not found"); point.EnsureCanAcceptIntake();
      var at=_clock.Now;var date=DateOnly.FromDateTime(at);var reff=new ServicePointType(point.ServicePointId,point.DisplayName);
      var tag=AntrianModel.GenSequenceTag(date,TimeOnly.MinValue,reff);var view=_queues.ListData(date).FirstOrDefault(x=>x.SequenceTag==tag);
      var queue=view is null?_factory.Create(point,date):_queues.LoadEntity(view).Value;
      var entry=queue.AddAdmissionEntry(at);var correlation=$"BOOKING-ASSISTANCE:{r.BookingId}";
      try { using var t=TransHelper.NewScope();
        if(!_assistance.TryCreate(r.BookingId,correlation,r.FailureCode,r.KioskId,r.UserId,at,queue,entry))
          throw new BookingAssistanceRaceException(); t.Complete(); }
      catch(BookingAssistanceRaceException)
      { found=_assistance.FindActive(r.BookingId)??throw new AdmissionQueueConcurrencyException("Booking assistance was created concurrently; reload required.");
        return Task.FromResult(ToResponse(found,true)); }
      return Task.FromResult(new BookingAssistanceIntakeResponse(queue.AntrianId,entry.NoUrut,
        queue.FormatQueueLabel(entry.NoUrut)??string.Empty,false));
    }
    private static BookingAssistanceIntakeResponse ToResponse(BookingAssistanceActive x,bool existing)=>
      new(x.AntrianId,x.NoUrut,x.QueueLabel??string.Empty,existing);
    private sealed class BookingAssistanceRaceException:Exception { }
}
