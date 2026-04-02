using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianRepo : IAntrianRepo
{
    private readonly IAntrianDal _antrianDal;
    private readonly IAntrianEntryDal _antrianEntryDal;
    private readonly ISequencer _sequencer;

    public AntrianRepo(IAntrianDal antrianDal, 
        IAntrianEntryDal antrianEntryDal, 
        ISequencer sequencer)
    {
        _antrianDal = antrianDal;
        _antrianEntryDal = antrianEntryDal;
        _sequencer = sequencer;
    }

    public void SaveChanges(AntrianModel model)
    {
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
        deletedItems.ForEach(x => _antrianEntryDal.Delete(model, x.NoUrut));
        changedItems.ForEach(x => _antrianEntryDal.Update(x));
    }

    public MayBe<AntrianModel> LoadEntity(IAntrianKey key)
    {
        var hdr = _antrianDal.GetData(key);
        var listDtl = _antrianEntryDal.ListData(key)?.ToList() ?? [];
        var listDtlType = listDtl.Select(x => x.ToModel());
        var model = hdr?.ToModel(listDtlType, _sequencer);
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
            DateOnly.FromDateTime(x.AntrianDate),
            TimeOnly.ParseExact(x.StartTime, "HH:mm", CultureInfo.InvariantCulture), x.SequenceTag));
        return result;
    }

    public IEnumerable<AntrianView> ListData(DateTime dateTime)
    {
        var listDto = _antrianDal.ListData(dateTime)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView());
        return result;
    }
    #region HELPER
    private (List<AntrianEntryDto> addedItems, 
        List<AntrianEntryDto> deletedItems, 
        List<AntrianEntryDto> changedItems) 
        CompareCollections(
            List<AntrianEntryDto> listEntryDb, 
            List<AntrianEntryDto> listCurrent)
    {
        // Find deleted items - items that exist in persisted but not in current
        var deletedItems = listEntryDb
            .Where(persisted => listCurrent.All(current => current.NoUrut != persisted.NoUrut))
            .ToList();

        // Find added items - items that exist in current but not in persisted
        var addedItems = listCurrent
            .Where(current => listEntryDb.All(persisted => persisted.NoUrut != current.NoUrut))
            .ToList();

        // Find changed items - items that exist in both but have different properties
        var changedItems = listCurrent
            .Where(current => listEntryDb.Any(persisted => 
                persisted.NoUrut == current.NoUrut && 
                !AreEqual(persisted, current)))
            .ToList();

        return (addedItems, deletedItems, changedItems);
    }

    private static bool AreEqual(AntrianEntryDto persisted, AntrianEntryDto current)
    {
        return persisted.NoUrut == current.NoUrut && 
               persisted.PersonName == current.PersonName && 
               persisted.AntrianStatus == current.AntrianStatus && 
               persisted.CreatedAt == current.CreatedAt && 
               persisted.ServedAt == current.ServedAt && 
               persisted.DoneAt == current.DoneAt &&
               persisted.ReffId == current.ReffId &&
               persisted.ReffDesc == current.ReffDesc;
    }

    
    #endregion

}