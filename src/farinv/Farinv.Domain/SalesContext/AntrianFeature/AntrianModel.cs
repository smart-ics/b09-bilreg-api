using Farinv.Domain.SalesContext.PenjualanFeature;

namespace Farinv.Domain.SalesContext.AntrianFeature;

public class AntrianModel: IAntrianKey
{
    private readonly List<AntrianEntryModel> _listEntry;

    #region CREATION
    public AntrianModel(
        string antrianId, DateOnly antrianDate, int servicePoint, string antrianDesc, 
        IEnumerable<AntrianEntryModel> listEntry)
    {
        AntrianId = antrianId;
        AntrianDate = antrianDate;
        ServicePoint = servicePoint;
        AntrianDescription = antrianDesc;

        _listEntry = [.. listEntry];
    }

    public static AntrianModel Create(
        DateOnly antrianDate, int servicePoint, string antrianDesc)
    {
        var newId = Ulid.NewUlid().ToString();
        return new AntrianModel(newId, antrianDate, servicePoint, antrianDesc, []);
    }

    public static IAntrianKey Key(string id)
    {
        return new AntrianModel(id, DateOnly.FromDateTime(DateTime.Now), 0, "-", []);
    }

    public static AntrianModel Default => new("-", DateOnly.MinValue, 0, "-", []);
    #endregion

    #region PROPERTIES
    public string AntrianId { get; init; }
    public DateOnly AntrianDate { get; init; }

    public int ServicePoint { get; init; }
    public string AntrianDescription { get; init; }

    public IReadOnlyCollection<AntrianEntryModel> ListEntry => _listEntry.AsReadOnly();
    #endregion

    #region BEHAVIOR
    public void AddEntry(int noAntrian)
    {
        EnsureSlotAvailable(noAntrian);
        var entry = AntrianEntryModel.Create(noAntrian, RegType.Default.ToReff(), "-", "-");
        _listEntry.Add(entry);
    }

    public void AddEntry(int noAntrian, RegReff reg)
    {
        EnsureSlotAvailable(noAntrian);
        var entry = AntrianEntryModel.Create(noAntrian, reg, "-", "-");
        _listEntry.Add(entry);
    }

    public AntrianEntryModel AddEntryByTracker(
        int noAntrian, RegReff reg, string pasienTrackerId, DateTime takenAt)
    {
        PharmacyTrackerIdentity.EnsureRealTrackerId(pasienTrackerId);

        var existing = FindActiveEntryByTracker(pasienTrackerId);
        if (existing is not null)
            return existing;

        EnsureSlotAvailable(noAntrian);
        var entry = AntrianEntryModel.CreateIdentified(noAntrian, reg, pasienTrackerId, takenAt);
        _listEntry.Add(entry);
        return entry;
    }

    public void RemoveEntry(int noAntrian)
    {
        var itemToRemove = ListEntry.FirstOrDefault(x => x.NoAntrian == noAntrian) 
            ?? throw new InvalidOperationException("NoAntrian not found.");
        if (itemToRemove.AntrianStatus != AntrianStatusEnum.Taken)
            throw new InvalidOperationException("Only Taken slot can be removed.");

        _listEntry.Remove(itemToRemove);
    }

    public void AssignSlot(int noAntrian, RegReff reg)
    {
        var entry = GetEntry(noAntrian);
        entry.Assign(reg);
    }

    public void PrepareSlot(int noAntrian)
    {
        var entry = GetEntry(noAntrian);
        entry.Prepare();
    }

    public void DeliverSlot(int noAntrian)
    {
        var entry = GetEntry(noAntrian);
        entry.Deliver();
    }

    public void ConfirmPharmacySale(int noAntrian, string penjualanId, DateTime servedAt)
    {
        var entry = GetEntry(noAntrian);
        entry.ConfirmSale(penjualanId, servedAt);
    }

    public void CancelSlot(int noAntrian)
    {
        var entry = GetEntry(noAntrian);
        entry.Cancel();
    }

    public void UpdateSlotReff(int noAntrian, string reffId, string reffDesc)
    {
        var entry = GetEntry(noAntrian);

        if (entry.AntrianStatus != AntrianStatusEnum.Taken)
            throw new InvalidOperationException("Reff can only be set when slot is Taken.");

        entry.SetReff(reffId, reffDesc);
    }

    public AntrianEntryModel? FindActiveEntryByTracker(string pasienTrackerId)
    {
        if (!PharmacyTrackerIdentity.IsRealTrackerId(pasienTrackerId))
            return null;

        return _listEntry.FirstOrDefault(x => x.IsActiveForTracker(pasienTrackerId));
    }

    public AntrianEntryModel GetActiveEntryByTracker(string pasienTrackerId)
        => FindActiveEntryByTracker(pasienTrackerId)
           ?? throw new KeyNotFoundException(
               $"Active pharmacy queue entry for tracker '{pasienTrackerId}' not found.");

    private AntrianEntryModel GetEntry(int noAntrian)
    {
        var entry = _listEntry.FirstOrDefault(x => x.NoAntrian == noAntrian);
        return entry is null ? throw new InvalidOperationException($"Slot {noAntrian} not found.") : entry;
    }

    private void EnsureSlotAvailable(int noAntrian)
    {
        if (_listEntry.Any(x => x.NoAntrian == noAntrian))
            throw new InvalidOperationException($"Slot {noAntrian} already taken.");
    }
    #endregion
}

public interface IAntrianKey
{
    string AntrianId { get; }
}

public record AntrianHeaderView(
    string AntrianId,
    string AntrianDescription,
    DateOnly AntrianDate,
    int ServicePoint) : IAntrianKey;


public record AntrianView(string AntrianId, int NoAntrian, AntrianStatusEnum AntrianStatus, 
    string RegId, string PasienId, string PasienName, string ReffId, string ReffDesc, 
    string PasienTrackerId,
    DateTime AntrianDate, int ServicePoint, string AntrianDescription);

public record PharmacyQueueEntryResponse(
    string AntrianId,
    int NoAntrian,
    string PasienTrackerId);
