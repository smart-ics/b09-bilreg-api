namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianEntryModel
{
    public AntrianEntryModel() { }
    public AntrianEntryModel(string antrianId, string noUrut, 
        VisitorType visitor, AntrianStatusEnum status, 
        DateTime createdAt, DateTime servedAt, DateTime doneAt)
    {
        AntrianId = antrianId;
        NoUrut = noUrut;
        Visitor = visitor;
        Status = status;
        CreatedAt = createdAt;
        ServedAt = servedAt;
        DoneAt = doneAt;
    }

    public string AntrianId { get; init; }
    public string NoUrut { get; set; }
    public VisitorType Visitor { get; private set; }
    public AntrianStatusEnum Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ServedAt { get; private set; }
    public DateTime DoneAt { get; private set; }

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

    public static AntrianEntryModel Deafult => new()
    {
        AntrianId = "-",
        NoUrut = "-",
        Visitor = VisitorType.Default,
        Status = AntrianStatusEnum.Waiting,
        CreatedAt = DateTime.Now,
        ServedAt = DateTime.Now,
        DoneAt = DateTime.Now
    };
}