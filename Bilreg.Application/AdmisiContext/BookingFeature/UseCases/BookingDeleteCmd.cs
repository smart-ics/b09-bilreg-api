using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IDeleteBookingWorkflow _deleteBookingWorkflow;

    public BookingDeleteHandler(IDeleteBookingWorkflow deleteBookingWorkflow)
    {
        _deleteBookingWorkflow = deleteBookingWorkflow;
    }

    //private readonly IBookingRepo _bookingRepo;
    //private readonly IAntrianRepo _antrianRepo;
    //private readonly IPpaRepo _ppaRepo;
    //private readonly IPasienTrackerRepo _paasienTrackerRepo;
    //public BookingDeleteHandler(IBookingRepo bookingRepo,
    //    IAntrianRepo antrianRepo,
    //    IPpaRepo ppaRepo,
    //    IPasienTrackerRepo paasienTrackerRepo)
    //{
    //    _bookingRepo = bookingRepo;
    //    _antrianRepo = antrianRepo;
    //    _ppaRepo = ppaRepo;
    //    _paasienTrackerRepo = paasienTrackerRepo;
    //}

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {
        
        Guard.Against.NullOrWhiteSpace(request.BookingId);
        _deleteBookingWorkflow.Execute(request);
        #region sementara
        //var booking = LoadBookingOrExit(request);
        //if (booking is null)
        //    return Task.CompletedTask;

        //if (booking.Reg.RegId.Trim() != "-")
        //    throw new KeyNotFoundException(
        //        $"Booking sudah registrasi {booking.Reg.RegId}, tidak boleh delete");

        //var dokter = LoadDokterOrThrow(booking);

        //var antrianView = LoadAntrianHeaderOrExit(booking, dokter);
        //if (antrianView is null)
        //    antrianView = new AntrianHeaderView("-", "", new DateOnly(3000, 1, 1), new TimeOnly(0, 0), "");

        //var antrian = _antrianRepo.LoadEntity(antrianView).Value;

        //var entry = antrian.ListEntry
        //    .FirstOrDefault(x => x.NoUrut == booking.NoAntrian);

        //using var trans = TransHelper.NewScope();

        //_bookingRepo.DeleteEntity(booking);
        //if (entry is not null)
        //{
        //    antrian.RemoveEntry(entry.NoUrut);
        //    _antrianRepo.SaveChanges(antrian);

        //    _paasienTrackerRepo.DeleteEntity(
        //        PasienTrackerModel.Key(entry.Tracker.PasienTrackerId));
        //}

        //trans.Complete();
        #endregion
        return Task.CompletedTask;
    }

    #region private-helper
    //private BookingModel? LoadBookingOrExit(BookingDeleteCmd request)
    //{
    //    var opt = _bookingRepo.LoadEntity(request);
    //    return !opt.HasValue ? opt.Value : null;
    //}

    //private PpaType LoadDokterOrThrow(BookingModel booking)
    //{
    //    var key = PpaType.Key(booking.Dokter.PpaId);

    //    return _ppaRepo.LoadEntity(key)
    //        .Match(
    //            onSome: x => x,
    //            onNone: () => throw new KeyNotFoundException(
    //                $"Dokter {key.PpaId} not found")
    //        );
    //}

    //private AntrianHeaderView? LoadAntrianHeaderOrExit(
    //BookingModel booking,
    //PpaType dokter)
    //{
    //    var sequenceTag = AntrianModel.GenSequenceTag(
    //        booking.TglBerobat, dokter);

    //    return _antrianRepo
    //        .ListData(booking.TglBerobat)
    //        .FirstOrDefault(x => x.SequenceTag == sequenceTag);
    //}


    #endregion
}
