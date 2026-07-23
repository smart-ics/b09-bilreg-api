using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.Shared.Helpers;


namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAntrianFactory : INunaFactory<AntrianModel>
{
    AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek);
    AntrianModel Create(DateOnly antrianDate, JadwalPraktekEffective effective);
    AntrianModel Create(ServicePointType servicePoint, DateOnly businessDate);
    AntrianModel Create(AdmissionServicePointModel servicePoint, DateOnly businessDate);
}

public class AntrianFactory : IAntrianFactory
{
    private readonly ISequencer _antrianSequencer;

    public AntrianFactory(ISequencer antrianSequencer)
    {
        _antrianSequencer = antrianSequencer;
    }

    public AntrianModel Create(DateOnly antrianDate, JadwalPraktekEffective effective)
    {
        Guard.Against.Null(effective, nameof(effective));

        if (antrianDate != effective.TglPraktek)
            throw new ArgumentException(
                $"{antrianDate:dd-MM-yyyy} tidak sesuai tanggal praktek ({effective.TglPraktek:dd-MM-yyyy}).",
                nameof(antrianDate));

        var newId = Ulid.NewUlid().ToString();
        var sequenceTag = AntrianModel.GenSequenceTag(antrianDate, effective);
        var antrianDesc = $"Praktek Dokter {effective.Dokter.PpaId}";
        var servicePoint = new ServicePointType(
            effective.Dokter.PpaId.Replace(' ', '$'),
            antrianDesc);

        return new AntrianModel(newId, antrianDate, effective.JamMulai,
            effective.JamSelesai, sequenceTag, antrianDesc, servicePoint,
            new List<AntrianEntryModel>(), _antrianSequencer);
    }

    public AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek)
    {
        Guard.Against.Null(jadwalPraktek, nameof(jadwalPraktek));

        if (antrianDate.DayOfWeek != jadwalPraktek.Hari)
            throw new ArgumentException($"{antrianDate:dd-MM-yyyy} bukan hari ({jadwalPraktek.Hari.ToString()}).",
                nameof(antrianDate));
        var newId = Ulid.NewUlid().ToString();
        var sequenceTag = AntrianModel.GenSequenceTag(antrianDate, jadwalPraktek);
        var antrianDesc = $"Praktek Dokter {jadwalPraktek.Dokter.PpaId}";
        var servicePoint = new ServicePointType(
            jadwalPraktek.Dokter.PpaId.Replace(' ', '$'),
            antrianDesc);
        
        var result = new AntrianModel(newId, antrianDate, jadwalPraktek.JamMulai, 
            jadwalPraktek.JamSelesai, sequenceTag, antrianDesc, servicePoint,
            new List<AntrianEntryModel>(), _antrianSequencer);
        return result;
    }

    public AntrianModel Create(ServicePointType servicePoint, DateOnly businessDate)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));

        var newId = Ulid.NewUlid().ToString();
        var antrianDate = businessDate;
        var mulai = TimeOnly.MinValue;
        var selesai = TimeOnly.MaxValue;
        var sequenceTag = AntrianModel.GenSequenceTag(antrianDate, mulai, servicePoint);
        
        return new AntrianModel(newId, antrianDate, mulai, selesai, 
            sequenceTag, servicePoint.ServicePointName, servicePoint,
            new List<AntrianEntryModel>(), 
            _antrianSequencer);
    }

    public AntrianModel Create(AdmissionServicePointModel servicePoint, DateOnly businessDate)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));
        servicePoint.EnsureCanAcceptIntake();
        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var queue = Create(reference, businessDate);
        return new AntrianModel(queue.AntrianId, queue.AntrianDate, queue.StartTime, queue.EndTime,
            queue.SequenceTag, queue.AntrianDescription, queue.ServicePoint, queue.ListEntry,
            _antrianSequencer, servicePoint.QueuePrefix);
    }

    public AntrianModel Load(string antrianId, DateOnly antrianDate, TimeOnly startTime, TimeOnly endTime,
        string sequenceTag, string antrianDesc, ServicePointType servicePoint,
        IEnumerable<AntrianEntryModel> listEntry)
    {
        return new AntrianModel(antrianId, antrianDate, startTime, endTime, 
            sequenceTag, antrianDesc, servicePoint, listEntry, _antrianSequencer);
    }
    
    public AntrianModel Default => new AntrianModel(
        "-", DateOnly.FromDateTime(new DateTime(3000, 1, 1)), TimeOnly.MinValue, TimeOnly.MinValue,
        "-", "-", ServicePointType.Default, new List<AntrianEntryModel>(), _antrianSequencer);
}
