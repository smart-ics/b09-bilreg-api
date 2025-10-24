using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Helpers;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public class AntrianModel : IAntrianKey
{
    private readonly List<AntrianEntryModel> _listEntry;
    private readonly ISequencer _sequencer;
    
    #region CREATION
    public AntrianModel(string antrianId, DateOnly antrianDate, TimeOnly startTime, TimeOnly endTime,
        string sequenceTag, string antrianDesc, IEnumerable<AntrianEntryModel> listEntry, 
        ISequencer sequencer)
    {
        AntrianId = antrianId;
        AntrianDate = antrianDate;
        StartTime = startTime;
        EndTime = endTime;
        SequenceTag = sequenceTag;
        AntrianDescription = antrianDesc;
        _listEntry = listEntry.ToList();

        _sequencer = sequencer;

    }
    public static IAntrianKey Key(string id)
    {
        var result = new AntrianModel(id, DateOnly.FromDateTime(DateTime.Now),
            TimeOnly.MinValue, TimeOnly.MinValue, "-", "-",
            new List<AntrianEntryModel>(), null!);
        return result;
    }

    #endregion
    
    #region PROPERTIES
    public string AntrianId { get; init; }
    public DateOnly AntrianDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string SequenceTag { get; init; }
    public string AntrianDescription { get; init; }
    public IEnumerable<AntrianEntryModel> ListEntry => _listEntry;
    #endregion

    #region METHODS BEHAVIOR    
    public AntrianEntryModel AddEntry(PasienTrackerModel pasienTracker)
    {
        var visitor = pasienTracker.Person;
        var noUrut = _sequencer.GetNextNoUrut(SequenceTag); 
        
        var entry = AntrianEntryModel.Create(noUrut, visitor);
        _listEntry.Add(entry);
        return entry;
    }
    public void AddEntry()
    {
        var noUrut = _sequencer.GetNextNoUrut(SequenceTag); 
        var entry = AntrianEntryModel.Create(noUrut, PersonType.Default);
        _listEntry.Add(entry); 
    }
    public static string GenSequenceTag(DateOnly tglAntrian, JadwalPraktekType jadwal)
    {
        Guard.Against.Null(jadwal, nameof(jadwal));
        Guard.Against.Null(tglAntrian, nameof(tglAntrian));
        
        var sequenceTag = $"AN{tglAntrian:yyMMdd}_{jadwal.Dokter.PetugasMedisId.Replace(' ', '$')}";
        return sequenceTag;
    }
    public static string GenSequenceTag(DateOnly tglAntrian, ServicePointType servicePoint)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));
        Guard.Against.Null(tglAntrian, nameof(tglAntrian));
        
        var sequenceTag = $"AN{tglAntrian:yyMMdd}_{servicePoint.ServicePointCode}";
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
