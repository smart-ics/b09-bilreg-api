namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModel
{
    #region CREATION
    public AntrianEntryModel(int noUrut, 
        VisitorType visitor, AntrianStatusEnum status, 
        DateTime createdAt, DateTime servedAt, DateTime doneAt)
    {
        NoUrut = noUrut;
        Visitor = visitor;
        Status = status;
        CreatedAt = createdAt;
        ServedAt = servedAt;
        DoneAt = doneAt;
    }

    public static AntrianEntryModel Default => 
        new AntrianEntryModel(-1, VisitorType.Default, AntrianStatusEnum.Waiting,
            DateTime.Now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
    #endregion
    
    #region PROPERTIES
    public int NoUrut { get; private set; }
    public VisitorType Visitor { get; private set; }
    public AntrianStatusEnum Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ServedAt { get; private set; }
    public DateTime DoneAt { get; private set; }
    #endregion
    
    #region METHOD BEHAVIOUR
    public void AssignPasien(PasienTrackerModel pasienTracker)
    {
        var visitor = pasienTracker.Visitor;
        Visitor = visitor;
    }

    public void Serve()
    {
        ServedAt = DateTime.Now;
        Status = AntrianStatusEnum.InService;
    }

    public void Done()
    { 
        DoneAt = DateTime.Now;
        Status = AntrianStatusEnum.Done;
    }
    #endregion

}