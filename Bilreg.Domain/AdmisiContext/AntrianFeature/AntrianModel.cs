using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModel : IAntrianKey
{
    private readonly List<AntrianEntryModel> _listEntry;
    private readonly ISequencer _sequencer;
    
    #region CREATION
    public AntrianModel(string antrianId, DateOnly antrianDate, TimeOnly startTime, TimeOnly endTime,
        string sequenceTag, string antrianDesc, string prefix, IEnumerable<AntrianEntryModel> listEntry, 
        ISequencer sequencer)
    {
        AntrianId = antrianId;
        AntrianDate = antrianDate;
        StartTime = startTime;
        EndTime = endTime;
        SequenceTag = sequenceTag;
        AntrianDescription = antrianDesc;
        Prefix = prefix;
        _listEntry = listEntry.ToList();

        _sequencer = sequencer;

    }
    public static IAntrianKey Key(string id)
    {
        var result = new AntrianModel(id, DateOnly.FromDateTime(DateTime.Now),
            TimeOnly.MinValue, TimeOnly.MinValue, "-", "-", "-",
            new List<AntrianEntryModel>(), null!);
        return result;
    }

    public static AntrianModel Default => new AntrianModel("-", DateOnly.MinValue, 
        TimeOnly.MinValue, TimeOnly.MaxValue, "-", "-", "-", new List<AntrianEntryModel>(), null!);

    #endregion
    
    #region PROPERTIES
    public string AntrianId { get; init; }
    public DateOnly AntrianDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string SequenceTag { get; init; }
    public string AntrianDescription { get; init; }
    public string Prefix { get; init;  }
    public IEnumerable<AntrianEntryModel> ListEntry => _listEntry;
    #endregion

    #region METHODS BEHAVIOR    
    public AntrianEntryModel AddEntry(PasienTrackerModel pasienTracker)
    {
        var visitor = pasienTracker.Person;
        var noUrut = _sequencer.GetNextNoUrut(SequenceTag); 
        
        var entry = AntrianEntryModel.Create(noUrut, visitor, pasienTracker, "-", "-");
        _listEntry.Add(entry);
        return entry;
    }
    public void AddEntry()
    {
        var noUrut = _sequencer.GetNextNoUrut(SequenceTag); 
        var entry = AntrianEntryModel.Create(noUrut, PersonType.Default, PasienTrackerModel.Key("-"), "-", "-");
        _listEntry.Add(entry); 
    }
    public void RemoveEntry(int noUrut)
    {
        var itemRemove = ListEntry
            .FirstOrDefault(x => x.NoUrut == noUrut) ?? 
                AntrianEntryModel.Default;
        _listEntry.Remove(itemRemove);
    }

    public AntrianEntryModel AddEntry(int noUrut, PasienTrackerModel pasienTracker, string reffId, string reffDesc)
    {
        var visitor = pasienTracker.Person;
        var entry = AntrianEntryModel.Create(noUrut, visitor, pasienTracker, reffId, reffDesc);
        _listEntry.Add(entry);
        return entry;
    }
    public static string GenSequenceTag(DateOnly tglAntrian, JadwalPraktekType jadwal)
    {
        Guard.Against.Null(jadwal, nameof(jadwal));
        Guard.Against.Null(tglAntrian, nameof(tglAntrian));
        
        var sequenceTag = $"AN{tglAntrian:yyMMdd}{jadwal.JamMulai:HHmm}_{jadwal.Dokter.PpaId.Replace(' ', '$')}";
        return sequenceTag;
    }
    public static string GenSequenceTag(DateOnly tglAntrian, TimeOnly jamMulai, PpaType dokter)
    {
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(tglAntrian, nameof(tglAntrian));
        Guard.Against.Null(jamMulai, nameof(jamMulai));

        var sequenceTag = $"AN{tglAntrian:yyMMdd}{jamMulai:HHmm}_{dokter.PpaId.Replace(' ', '$')}";
        return sequenceTag;
    }
    public static string GenSequenceTag(DateOnly tglAntrian, TimeOnly jamMulai, ServicePointType servicePoint)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));
        Guard.Against.Null(tglAntrian, nameof(tglAntrian));
        Guard.Against.Null(jamMulai, nameof(jamMulai));

        var sequenceTag = $"AN{tglAntrian:yyMMdd}{jamMulai:HHmm}_{servicePoint.ServicePointCode}";
        return sequenceTag;
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
    TimeOnly StartTime,
    string SequenceTag) : IAntrianKey;


public record AntrianView(string AntrianId, int AntrianStatus, int NoUrut, string PersonName,
    string ReffId, string ReffDesc, DateTime AntrianDate, string SquenceTag, string DokterId,
    string AntrianDescription, TimeOnly StartTime, TimeOnly EndTime);