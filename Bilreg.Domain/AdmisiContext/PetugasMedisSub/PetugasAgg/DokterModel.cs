using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public class DokterModel : AbstractPetugasModel, IListLayananTugas
{
    public string SmfId { get; private set; }
    public string SmfName { get; private set; }
    
    public IEnumerable<LayananTugasModel> ListLayananTugas { get; private set; }

    public void Add(LayananTugasModel layanan)
    {
        var list = ListLayananTugas.ToList();
        list.Add(layanan);
        ListLayananTugas = list;
    }
    public void Remove(Func<LayananTugasModel, bool> predicate)
    {
        var list = ListLayananTugas.ToList();
        list.RemoveAll(predicate.Invoke);
        ListLayananTugas = list;
    }

    public void Set(SmfModel smf)
    {
        SmfId = smf.SmfId;
        SmfName = smf.SmfName;  
    }

    public DokterModel(string id, string name) : base(id, name)
    {
        ListLayananTugas = new List<LayananTugasModel>();
    }
}

public class DokterModelTest
{
    [Fact]
    public void GivenValidLayanan_WhenAddLayanan_ThenLayananAdded()
    {
        var dokter  = AbstractPetugasModel.Create("1", "Dokter", "Dokter") as DokterModel;
        dokter.Add(new LayananTugasModel("1", "Layanan 1"));
        dokter.Add(new LayananTugasModel("2", "Layanan 2"));
        dokter.Add(new LayananTugasModel("3", "Layanan 3"));
        dokter.ListLayananTugas.Count().Should().Be(3);
    }
}
