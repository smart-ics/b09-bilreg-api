using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class BookingRepo : IBookingRepo
{
    private readonly IBookingDal _bookingDal;

    public BookingRepo(IBookingDal bookingDal)
    {
        _bookingDal = bookingDal;
    }

    public void SaveChanges(BookingModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _bookingDal.Update(BookingDto.FromModel(model)),
                onNone: () => _bookingDal.Insert(BookingDto.FromModel(model))
            );
    }

    public MayBe<BookingModel> LoadEntity(IBookingKey key)
    {
        var result = _bookingDal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public void DeleteEntity(IBookingKey key)
        => _bookingDal.Delete(key);

    public IEnumerable<BookingModel> ListData(Periode periode)
    {
        var listDto = _bookingDal.ListData(periode)?.ToList()  ?? [];
        var models = listDto.Select(x => x.ToModel()) ?? [];
        return models;
    }

    public IEnumerable<BookingModel> ListDataTglBerobat(Periode periode)
    {
        var listDto = _bookingDal.ListPerTglBerobat(periode)?.ToList() ?? [];
        var models = listDto?.Select(x => x.ToModel()) ?? [];
        return models;
    }

    public MayBe<BookingModel> LoadEntity(string key)
    {
        var booking = _bookingDal.GetData(key);
        return booking is null ?
            MayBe<BookingModel>.None :
            LoadEntity(BookingModel.Key(booking.BookingId));
    }
}
