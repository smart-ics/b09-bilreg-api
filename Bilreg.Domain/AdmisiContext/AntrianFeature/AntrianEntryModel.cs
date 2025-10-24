using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModel
{
    #region CREATION
    public AntrianEntryModel(int noUrut, 
        PersonType visitor, AntrianStatusEnum status, 
        DateTime createdAt, DateTime servedAt, DateTime doneAt)
    {
        NoUrut = noUrut;
        Visitor = visitor;
        AntrianStatus = status;
        CreatedAt = createdAt;
        ServedAt = servedAt;
        DoneAt = doneAt;
    }

    public static AntrianEntryModel Create(int noUrut, PersonType visitor)
    {
        var newEntry = new AntrianEntryModel(noUrut, visitor, AntrianStatusEnum.Waiting, DateTime.Now,
            new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
        return newEntry;
    }
    
    public static AntrianEntryModel Default => 
        new AntrianEntryModel(-1, PersonType.Default, AntrianStatusEnum.Waiting,
            DateTime.Now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
    #endregion
    
    #region PROPERTIES
    public int NoUrut { get; private set; }
    public PersonType Visitor { get; private set; }
    public AntrianStatusEnum AntrianStatus { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ServedAt { get; private set; }
    public DateTime DoneAt { get; private set; }
    
    #endregion
    
    #region METHOD BEHAVIOUR
    public void AssignPasien(PasienTrackerModel pasienTracker)
    {
        Guard.Against.Null(pasienTracker, nameof(pasienTracker));
        var visitor = pasienTracker.Person;
        Visitor = visitor;
    }

    public void Serve()
    {
        ServedAt = DateTime.Now;
        AntrianStatus = AntrianStatusEnum.InService;
    }

    public void Done()
    { 
        if (ServedAt == new DateTime(3000,1,1))
            throw new ArgumentException("Pasien belum dilayani");

        var now = DateTime.Now;
        if (ServedAt >= now)
            throw new ArgumentException("Pasien belum dilayani");
        
        DoneAt = now;
        AntrianStatus = AntrianStatusEnum.Done;
    }
    #endregion

}

