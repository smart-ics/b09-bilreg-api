using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.ReservationFeature;

public class ReservationRepo : IReservationRepo
{
    private readonly IReservationDal _dal;

    public ReservationRepo(IReservationDal dal) => _dal = dal;

    public void SaveChanges(ReservationModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(ReservationDto.FromModel(model)),
                onNone: () => _dal.Insert(ReservationDto.FromModel(model)));
    }

    public MayBe<ReservationModel> LoadEntity(IReservationKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null)
            return MayBe<ReservationModel>.None;
        return MayBe.From(dto.ToModel());
    }

    public IEnumerable<ReservationModel> ListData(ReservationListFilter filter)
    {
        var listDto = _dal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel()).ToList();
    }
}
