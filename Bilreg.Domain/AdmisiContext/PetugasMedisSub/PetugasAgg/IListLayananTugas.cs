namespace Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;

public interface IListLayananTugas
{
    IEnumerable<LayananTugasModel> ListLayananTugas { get;}
    void Add(LayananTugasModel layanan);
    void Remove(Func<LayananTugasModel, bool> predicate);
}

public class LayananTugasModel
{
    public LayananTugasModel(string id, string name)
    {
        LayananId = id;
        LayananName = name;
        IsUtama = false;
    }
    public void SetUtama() => IsUtama = true;
    public void UnsetUtama() => IsUtama = false;
    public string LayananId { get; private set; }
    public string LayananName { get; private set; }
    public bool IsUtama { get; private set; }
}