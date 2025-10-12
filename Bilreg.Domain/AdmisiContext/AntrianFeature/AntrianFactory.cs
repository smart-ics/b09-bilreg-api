using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;


namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAntrianFactory
{
    AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek);
    AntrianModel Create(ServicePointType servicePoint);
    AntrianModel Default { get; }
    IAntrianKey Key(string id);
}

public class AntrianFactory : IAntrianFactory
{
    private readonly IAntrianSequencer _antrianSequencer;

    public AntrianFactory(IAntrianSequencer antrianSequencer)
    {
        _antrianSequencer = antrianSequencer;
    }

    public AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek)
    {
        Guard.Against.Null(jadwalPraktek, nameof(jadwalPraktek));

        if (antrianDate.DayOfWeek != jadwalPraktek.Hari)
            throw new ArgumentException($"{antrianDate:dd-MM-yyyy} bukan hari ({jadwalPraktek.Hari.ToString()}).",
                nameof(antrianDate));
        var newId = Ulid.NewUlid().ToString();
        var sequenceTag = AntrianModel.GenSequenceTag(antrianDate, jadwalPraktek);
        var antrianDesc = $"Praktek Dokter {jadwalPraktek.Dokter.PetugasMedisId}";
        
        var result = new AntrianModel(newId, antrianDate, jadwalPraktek.JamMulai, 
            jadwalPraktek.JamSelesai,sequenceTag, antrianDesc, 
            new List<AntrianEntryModel>(), _antrianSequencer);
        return result;
    }

    public AntrianModel Create(ServicePointType servicePoint)
    {
        Guard.Against.Null(servicePoint, nameof(servicePoint));

        var newId = Ulid.NewUlid().ToString();
        var antrianDate = DateOnly.FromDateTime(DateTime.Now);
        var mulai = TimeOnly.MinValue;
        var selesai = TimeOnly.MaxValue;
        var sequenceTag = AntrianModel.GenSequenceTag(antrianDate, servicePoint);
        
        return new AntrianModel(newId, antrianDate, mulai, selesai, 
            sequenceTag, servicePoint.ServicePointName, new List<AntrianEntryModel>(), 
            _antrianSequencer);
    }
    
    public AntrianModel Default => new AntrianModel(
        "-", DateOnly.FromDateTime(new DateTime(3000, 1, 1)), TimeOnly.MinValue, TimeOnly.MinValue,
        "-","-", new List<AntrianEntryModel>(), _antrianSequencer);

    public IAntrianKey Key(string id)
    {
        var result = new AntrianModel(id, DateOnly.FromDateTime(DateTime.Now),
            TimeOnly.MinValue, TimeOnly.MinValue, "-", "-",
            new List<AntrianEntryModel>(), _antrianSequencer);
        return result;
    }

}