using Farinv.Application.SalesContext.AntrianFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public class AntrianRepo : IAntrianRepo
{
    private readonly IAntrianDal _antrianDal;
    private readonly IAntrianEntryDal _antrianEntryDal;

    public AntrianRepo(IAntrianDal antrianDal,
        IAntrianEntryDal antrianEntryDal)
    {
        _antrianDal = antrianDal;
        _antrianEntryDal = antrianEntryDal;
    }

    public void SaveChanges(AntrianModel model)
    {
        using var trans = TransHelper.NewScope();

        LoadEntity(model)
            .Match(
                onSome: _ => _antrianDal.Update(AntrianDto.FromModel(model)),
                onNone: () => _antrianDal.Insert(AntrianDto.FromModel(model))
            );

        var listEntryDb = _antrianEntryDal.ListData(model)?.ToList() ?? [];
        var listCurrent = model.ListEntry
            .Select(x => AntrianEntryDto.FromModel(model.AntrianId, x)).ToList();
        var (addedItems, deletedItems, changedItems) = CompareCollections(listEntryDb, listCurrent);

        addedItems.ForEach(x => _antrianEntryDal.Insert(x));
        deletedItems.ForEach(x => _antrianEntryDal.Delete(model, x.NoAntrian));
        changedItems.ForEach(x => _antrianEntryDal.Update(x));

        trans.Complete();
    }

    public MayBe<AntrianModel> LoadEntity(IAntrianKey key)
    {
        var hdr = _antrianDal.GetData(key);
        var listDtl = _antrianEntryDal.ListData(key)?.ToList() ?? [];
        var listDtlType = listDtl.Select(x => x.ToModel());
        var model = hdr?.ToModel(listDtlType);
        return MayBe.From(model!);
    }

    public void DeleteEntity(IAntrianKey key)
    {
        _antrianDal.Delete(key);
        _antrianEntryDal.Delete(key);
    }

    public IEnumerable<AntrianHeaderView> ListData(DateOnly filter)
    {
        var periode = new Periode(filter.ToDateTime(TimeOnly.MinValue));
        var listDto = _antrianDal.ListData(periode)?.ToList() ?? [];
        var result = listDto.Select(x => new AntrianHeaderView(x.AntrianId, x.AntrianDescription,
            DateOnly.FromDateTime(x.AntrianDate), x.ServicePoint));
        return result;
    }

    public IEnumerable<AntrianView> ListData(DateTime dateTime)
    {
        var listDto = _antrianDal.ListData(dateTime)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView());
        return result;
    }

    public AntrianModel LoadOrCreate(int servicePoint)
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        var listAntrian = ListData(date) ?? [];
        var antrianView = listAntrian
            .FirstOrDefault(x => x.ServicePoint == servicePoint && x.AntrianDate == date);

        if (antrianView is not null)
        {
            var maybe = LoadEntity(antrianView);
            return maybe.Value;
        }

        return AntrianModel.Create(date, servicePoint, $"Antrian Apotek {servicePoint}");
    }

    #region HELPER
    private static (List<AntrianEntryDto> addedItems,
        List<AntrianEntryDto> deletedItems,
        List<AntrianEntryDto> changedItems)
        CompareCollections(
            List<AntrianEntryDto> listEntryDb,
            List<AntrianEntryDto> listCurrent)
    {
        // Find deleted items - items that exist in persisted but not in current
        var deletedItems = listEntryDb
            .Where(persisted => listCurrent.All(current => current.NoAntrian != persisted.NoAntrian))
            .ToList();

        // Find added items - items that exist in current but not in persisted
        var addedItems = listCurrent
            .Where(current => listEntryDb.All(persisted => persisted.NoAntrian != current.NoAntrian))
            .ToList();

        // Find changed items - items that exist in both but have different properties
        var changedItems = listCurrent
            .Where(current => listEntryDb.Any(persisted =>
                persisted.NoAntrian == current.NoAntrian &&
                !AreEqual(persisted, current)))
            .ToList();

        return (addedItems, deletedItems, changedItems);
    }

    private static bool AreEqual(AntrianEntryDto persisted, AntrianEntryDto current)
    {
        return persisted.NoAntrian == current.NoAntrian &&
               persisted.AntrianStatus == current.AntrianStatus &&
               persisted.TakenAt == current.TakenAt &&
               persisted.AssignedAt == current.AssignedAt &&
               persisted.PreparedAt == current.PreparedAt &&
               persisted.DeliveredAt == current.DeliveredAt &&
               persisted.CancelAt == current.CancelAt &&
               persisted.RegId == current.RegId &&
               persisted.ReffId == current.ReffId &&
               persisted.ReffDesc == current.ReffDesc;
    }    
    #endregion
}
