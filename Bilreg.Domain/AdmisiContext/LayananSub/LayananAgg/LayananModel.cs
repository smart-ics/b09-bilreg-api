namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

public class LayananModel(string id, string name) : ILayananKey
{
    public string LayananId { get; private set; } = id;
    public string LayananName { get; private set; } = name;
}

public interface ILayananKey
{
    string LayananId { get; }
}