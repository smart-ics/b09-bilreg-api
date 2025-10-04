using FluentValidation.Validators;
using Nuna.Lib.ValidationHelper;
using System.Security.Cryptography.X509Certificates;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModel
{
    private readonly List<AntrianEntryModel> _listEntry;
    public AntrianModel( 
        DateTime antrianDate, 
        TimeSpan startTime, TimeSpan endTime,
        ServicePointType servicePoint,
        IEnumerable<AntrianEntryModel> entries)
    {
        var listEntry = entries.ToList() ?? throw new ArgumentException(nameof(entries));

        AntrianId = Ulid.NewUlid().ToString();
        AntrianDate = antrianDate;
        StartTime = startTime;
        EndTime = endTime;
        _listEntry = listEntry;

    }
    public string AntrianId { get; init; }
    public DateTime AntrianDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public ServicePointType ServicePoint { get; init; }
    public IEnumerable<AntrianEntryModel> ListEntry => _listEntry;


    public static AntrianModel Create(JadwalPraktekType jadwalPraktek)
    {
        return new AntrianModel(DateTime.Now, jadwalPraktek.JamMulai, 
            jadwalPraktek.JamSelesai, ServicePointType.Default, new List<AntrianEntryModel>());

    }

    public static AntrianModel Create(ServicePointType servicePoint)
    {
        var periode = new Periode(DateTime.Now);
        return new AntrianModel(DateTime.Now, periode.Tgl1.TimeOfDay,
            periode.Tgl2.TimeOfDay, servicePoint , new List<AntrianEntryModel>());
    }

    public void AddEntry(PasienTrackerModel pasienTracker)
    {
        var visitor = pasienTracker.Visitor;
        var entry = new AntrianEntryModel(AntrianId, "0", visitor, 
            AntrianStatusEnum.Waiting, DateTime.Now,
            new DateTime(3000,1,1), new DateTime(3000, 1, 1));
        _listEntry.Add(entry);
    }

    public void AddEntry()
    {
        var entry = new AntrianEntryModel(AntrianId, "0", VisitorType.Default, 
            AntrianStatusEnum.Waiting, 
            DateTime.Now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1));
        _listEntry.Add(entry); 
    }




    public static AntrianModel Default => new AntrianModel(
        DateTime.MaxValue, TimeSpan.Zero, TimeSpan.Zero,
       ServicePointType.Default, new List<AntrianEntryModel>()
    );
}