using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModel : IAntrianKey
{
    private readonly List<AntrianEntryModel> _listEntry;
    
    #region CREATION
    public AntrianModel(string antrianId, DateOnly antrianDate, TimeSpan startTime, TimeSpan endTime,
        ServicePointType servicePoint, IEnumerable<AntrianEntryModel> listEntry)
    {
        AntrianId = antrianId;
        AntrianDate = antrianDate;
        StartTime = startTime;
        EndTime = endTime;
        ServicePoint = servicePoint;
        _listEntry = listEntry.ToList();

    }
    public static AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek)
    {
        Guard.Against.Null(jadwalPraktek, nameof(jadwalPraktek));
        if (antrianDate.DayOfWeek != jadwalPraktek.Hari)
            throw new ArgumentException($"{antrianDate:dd-MM-yyyy} bukan hari ({jadwalPraktek.Hari.ToString()}).",
                nameof(antrianDate));
        
        var newId = Ulid.NewUlid().ToString();
        var result = new AntrianModel(newId, antrianDate, jadwalPraktek.JamMulai, 
            jadwalPraktek.JamSelesai, ServicePointType.Default, new List<AntrianEntryModel>());
        return result;
    }
    
    public static AntrianModel Create(ServicePointType servicePoint)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));
        if (servicePoint.Status != ServicePointStatusEnum.Opened)
            throw new ArgumentException($"{servicePoint.ServicePointName} belum buka.", nameof(servicePoint));
        
        var newId = Ulid.NewUlid().ToString();
        
        var antrianDate = DateOnly.FromDateTime(DateTime.Now);
        var mulai = TimeSpan.MinValue;
        var selesai = TimeSpan.MaxValue;
        
        return new AntrianModel(newId, antrianDate, mulai, selesai, 
            servicePoint , new List<AntrianEntryModel>());
    }

    public static AntrianModel Default => new AntrianModel(
        "-", DateOnly.FromDateTime(new DateTime(3000, 1, 1)), TimeSpan.Zero, TimeSpan.Zero,
        ServicePointType.Default, new List<AntrianEntryModel>());

    public static IAntrianKey Key(string id)
    {
        var result = new AntrianModel(id, DateOnly.FromDateTime(DateTime.Now),
            TimeSpan.MinValue, TimeSpan.MinValue, ServicePointType.Default,
            new List<AntrianEntryModel>());
        return result;
    }
    #endregion
    
    #region PROPERTIES
    public string AntrianId { get; init; }
    public DateOnly AntrianDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public IEnumerable<AntrianEntryModel> ListEntry => _listEntry;
    #endregion

    #region METHODS BEHAVIOR    
    public void AddEntry(PasienTrackerModel pasienTracker)
    {
        var visitor = pasienTracker.Visitor;
        var noUrut = _listEntry.Count != 0 
            ? _listEntry.Max(x => x.NoUrut) + 1 
            : 1; 
        
        var entry = new AntrianEntryModel(noUrut, visitor, 
            AntrianStatusEnum.Waiting, DateTime.Now,
            new DateTime(3000,1,1), new DateTime(3000, 1, 1));
        _listEntry.Add(entry);
    }
    public void AddEntry()
    {
        var noUrut = _listEntry.Max(x => x.NoUrut) + 1;
        var entry = new AntrianEntryModel(noUrut, VisitorType.Default, 
            AntrianStatusEnum.Waiting, 
            DateTime.Now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
        _listEntry.Add(entry); 
    }
    #endregion
}

public interface IAntrianKey
{
    string AntrianId { get; }
}