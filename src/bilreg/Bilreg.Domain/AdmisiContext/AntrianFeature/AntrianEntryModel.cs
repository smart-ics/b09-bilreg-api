using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModel
{
    private static readonly DateTime SentinelAt = new(3000, 1, 1);

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

    public static AntrianEntryModel Create(int noUrut, PersonType visitor, IPasienTrackerKey tracker, string reffId, string reffDesc, DateTime createdAt)
    {
        EnsureBusinessTime(createdAt, nameof(createdAt));

        return new AntrianEntryModel(noUrut, visitor, tracker, AntrianStatusEnum.Waiting, createdAt,
            SentinelAt, SentinelAt, reffId, reffDesc);
    }
    
    public static AntrianEntryModel Default => 
        new AntrianEntryModel(-1, PersonType.Default, PasienTrackerModel.Default, AntrianStatusEnum.Waiting,
            SentinelAt, SentinelAt, SentinelAt, "-", "-");
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

    public void Serve(DateTime servedAt)
    {
        if (AntrianStatus != AntrianStatusEnum.Waiting)
            throw new InvalidOperationException("Only a Waiting queue entry may enter In Service.");

        EnsureBusinessTime(servedAt, nameof(servedAt));
        if (servedAt < CreatedAt)
            throw new ArgumentException("ServedAt shall not precede CreatedAt.", nameof(servedAt));

        ServedAt = servedAt;
        AntrianStatus = AntrianStatusEnum.InService;
    }

    public void Done(DateTime doneAt)
    {
        if (AntrianStatus != AntrianStatusEnum.InService)
            throw new InvalidOperationException("Only an In Service queue entry may become Done.");

        EnsureBusinessTime(doneAt, nameof(doneAt));
        if (doneAt < ServedAt)
            throw new ArgumentException("DoneAt shall not precede ServedAt.", nameof(doneAt));
        
        DoneAt = doneAt;
        AntrianStatus = AntrianStatusEnum.Done;
    }

    public void SetReff(string reffId, string reffDesc)
    {
        ReffId = reffId;
        ReffDesc = reffDesc;
    }

    private static void EnsureBusinessTime(DateTime value, string paramName)
    {
        if (value == default || value == DateTime.MinValue || value == SentinelAt)
            throw new ArgumentException("A valid business time is required.", paramName);
    }
    #endregion

}
