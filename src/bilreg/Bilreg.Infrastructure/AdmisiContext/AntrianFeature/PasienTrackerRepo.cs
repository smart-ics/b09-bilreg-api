using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class PasienTrackerRepo : IPasienTrackerRepo
{
    private readonly IPasienTrackerDal _pasienTrackerdal;
    private readonly IPasienTrackerEventDal _pasienTrackerEventDal;

    public PasienTrackerRepo(IPasienTrackerDal pasienTrackerdal, 
        IPasienTrackerEventDal pasienTrackerEventDal)
    {
        _pasienTrackerdal = pasienTrackerdal;
        _pasienTrackerEventDal = pasienTrackerEventDal;
    }

    public void SaveChanges(PasienTrackerModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _pasienTrackerdal.Update(PasienTrackerDto.FromModel(model)),
                onNone: () => _pasienTrackerdal.Insert(PasienTrackerDto.FromModel(model))
            );

        // Append-only: insert events whose NoUrut is not yet persisted (BR-TRK-017).
        var persistedNoUrut = (_pasienTrackerEventDal.ListData(model) ?? [])
            .Select(x => x.NoUrut)
            .ToHashSet();
        var addedItems = model.ListEvent
            .Where(x => !persistedNoUrut.Contains(x.NoUrut))
            .Select(x => PasienTrackerEventDto.FromModel(model.PasienTrackerId, x))
            .ToList();

        addedItems.ForEach(x => _pasienTrackerEventDal.Insert(x));
    }

    public MayBe<PasienTrackerModel> LoadEntity(IPasienTrackerKey key)
    {
        var hdr = _pasienTrackerdal.GetData(key);
        var listDtl = _pasienTrackerEventDal.ListData(key)?.ToList() ?? [];
        var listDtlType = listDtl.Select(x => x.ToModel());
        var model = hdr?.ToModel(listDtlType);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IPasienTrackerKey key)
    {
        // Tracker evidence is append-only (BR-TRK-017). Cancellation/reschedule must
        // append evidence via SaveChanges; physical deletion of journey history is not supported.
        throw new NotSupportedException(
            "PasienTracker evidence is append-only. Append cancellation or reschedule evidence instead of deleting the tracker.");
    }

    public IEnumerable<PasienTrackerView> ListData(Periode visitDate, DateOnly tglLahir)
    {
        var listData = _pasienTrackerdal.ListData(visitDate)?.ToList() ?? [];
        var tglLahirDt = tglLahir.ToDateTime(TimeOnly.MinValue);
        var listTglLahir = listData
            .Where(x => x.TglLahir == tglLahirDt)
            .Select(x => new PasienTrackerView(
                x.PasienTrackerId, 
                new PersonType(x.PersonName, DateOnly.FromDateTime(x.TglLahir)),
                DateOnly.FromDateTime(x.VisitDate),
                DateOnly.FromDateTime(x.StartPeriod),
                DateOnly.FromDateTime(x.LastPeriod)));
        return listTglLahir;
    }
}
