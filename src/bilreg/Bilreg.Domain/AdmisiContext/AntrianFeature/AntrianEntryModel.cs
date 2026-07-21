using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModel
{
    #region CREATION
    public AntrianEntryModel(int noUrut, 
        PersonType visitor, IPasienTrackerKey tracker,
        AntrianStatusEnum status, 
        DateTime createdAt, DateTime servedAt, DateTime doneAt,
        string reffId, string reffDesc)
    {
        NoUrut = noUrut;
        Visitor = visitor;
        Tracker = tracker;
        AntrianStatus = status;
        CreatedAt = createdAt;
        ServedAt = servedAt;
        DoneAt = doneAt;
        ReffId = reffId;
        ReffDesc = reffDesc;
    }

    public static AntrianEntryModel Create(int noUrut, PersonType visitor, IPasienTrackerKey tracker, string reffId, string reffDesc, DateTime createdAt = default)
    {
        var newEntry = new AntrianEntryModel(noUrut, visitor, tracker, AntrianStatusEnum.Waiting, createdAt,
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), reffId, reffDesc);
        return newEntry;
    }
    
    public static AntrianEntryModel Default => 
        new AntrianEntryModel(-1, PersonType.Default, PasienTrackerModel.Default, AntrianStatusEnum.Waiting,
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), "-", "-");
    #endregion
    
    #region PROPERTIES
    public int NoUrut { get; private set; }
    public PersonType Visitor { get; private set; }
    public IPasienTrackerKey Tracker { get; private set; }
    public AntrianStatusEnum AntrianStatus { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ServedAt { get; private set; }
    public DateTime DoneAt { get; private set; }
    public string ReffId { get; private set; }
    public string ReffDesc { get; private set; }
    
    #endregion
    
    #region METHOD BEHAVIOUR
    public void AssignPasien(PasienTrackerModel pasienTracker)
    {
        Guard.Against.Null(pasienTracker, nameof(pasienTracker));
        if (!IsRealTrackerId(pasienTracker.PasienTrackerId))
            throw new ArgumentException("PasienTrackerId is required to identify a queue entry.", nameof(pasienTracker));
        if (IsRealTrackerId(Tracker.PasienTrackerId))
            throw new InvalidOperationException("Queue entry is already identified.");

        Visitor = pasienTracker.Person;
        Tracker = pasienTracker;
    }

    private static bool IsRealTrackerId(string? trackerId)
        => trackerId is not null
           && trackerId.Trim() != ""
           && trackerId != "-";

    public void Serve(DateTime servedAt = default)
    {
        ServedAt = servedAt;
        AntrianStatus = AntrianStatusEnum.InService;
    }

    public void Done(DateTime doneAt = default)
    { 
        if (ServedAt == new DateTime(3000,1,1))
            throw new ArgumentException("Pasien belum dilayani");

        if (ServedAt >= doneAt)
            throw new ArgumentException("Pasien belum dilayani");
        
        DoneAt = doneAt;
        AntrianStatus = AntrianStatusEnum.Done;
    }

    public void SetReff(string reffId, string reffDesc)
    {
        ReffId = reffId;
        ReffDesc = reffDesc;
    }
    #endregion

}

