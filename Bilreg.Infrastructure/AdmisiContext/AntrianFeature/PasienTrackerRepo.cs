using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;

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

        var listEventDb = _pasienTrackerEventDal.ListData(model)?.ToList() ?? [];
        var listCurrent = model.ListEvent
            .Select(x => PasienTrackerEventDto.FromModel(model.PasienTrackerId, x)).ToList();
        var (addedItems, deletedItems, changedItems) = CompareCollections(listEventDb, listCurrent);

        addedItems.ForEach(x => _pasienTrackerEventDal.Insert(x));
        deletedItems.ForEach(x => _pasienTrackerEventDal.Delete(x.PasienTrackerId, x.NoUrut));
        changedItems.ForEach(x => _pasienTrackerEventDal.Update(x));
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
        throw new NotImplementedException();
    }

    #region HELPER
    private (List<PasienTrackerEventDto> addedItems, 
        List<PasienTrackerEventDto> deletedItems, 
        List<PasienTrackerEventDto> changedItems) 
        CompareCollections(
            List<PasienTrackerEventDto> persistedItemList, 
            List<PasienTrackerEventDto> currentItemList)
    {
        // Find deleted items - items that exist in persisted but not in current
        var deletedItems = persistedItemList
            .Where(persisted => currentItemList.All(current => current.NoUrut != persisted.NoUrut))
            .ToList();

        // Find added items - items that exist in current but not in persisted
        var addedItems = currentItemList
            .Where(current => persistedItemList.All(persisted => persisted.NoUrut != current.NoUrut))
            .ToList();

        // Find changed items - items that exist in both but have different properties
        var changedItems = currentItemList
            .Where(current => persistedItemList.Any(persisted => 
                persisted.NoUrut == current.NoUrut && 
                !AreEqual(persisted, current)))
            .ToList();

        return (addedItems, deletedItems, changedItems);
    }

    private static bool AreEqual(PasienTrackerEventDto a, PasienTrackerEventDto b)
    {
        return a.NoUrut == b.NoUrut &&
               a.EventName == b.EventName &&
               a.EventDate == b.EventDate &&
               a.ReffId == b.ReffId;
    }
    #endregion
}

