using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class BookingRepo : IBookingRepo
{
    private readonly IBookingDal _bookingDal;
    private readonly IBookingExternalDal _bookingExtDal;
    public BookingRepo(IBookingDal bookingDal, 
        IBookingExternalDal bookingExtDal)
    {
        _bookingDal = bookingDal;
        _bookingExtDal = bookingExtDal;
    }

    public void SaveChanges(BookingModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _bookingDal.Update(BookingDto.FromModel(model)),
                onNone: () => _bookingDal.Insert(BookingDto.FromModel(model))
            );
        

        var bookExt = _bookingExtDal.GetData(model);
        var extDto = BookingExternalDto.FromModel(model.BookingId, model.ExtAppReff);
        if (bookExt is null)
            _bookingExtDal.Insert(extDto);
        else
            _bookingExtDal.Update(extDto);

    }

    public MayBe<BookingModel> LoadEntity(IBookingKey key)
    {
        var booking = _bookingDal.GetData(key);
        
        var bookExt = _bookingExtDal.GetData(key)  
            ?? new BookingExternalDto(key.BookingId, "", "", "");

        var model = booking?.ToModel(bookExt);
        
        return MayBe.From(model!);
    }

    public void DeleteEntity(IBookingKey key)
    {
        _bookingDal.Delete(key);
        _bookingExtDal.Delete(key);
    }

    public IEnumerable<BookingView> ListData(Periode periode)
    {
        var listDto = _bookingDal.ListData(periode)?.ToList()  ?? [];
        var models = listDto.Select(x => x.ToView()) ?? [];
        return models;
    }

    public IEnumerable<BookingView> ListDataTglBerobat(Periode periode)
    {
        var listDto = _bookingDal.ListPerTglBerobat(periode)?.ToList() ?? [];
        var models = listDto?.Select(x => x.ToView()) ?? [];
        return models;
    }

    public MayBe<BookingModel> LoadEntity(string key)
    {
        var booking = _bookingExtDal.GetData(key);
        return booking is null ?
            MayBe<BookingModel>.None :
            LoadEntity(BookingModel.Key(booking.BookingId));
    }
}
