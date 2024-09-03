namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

public class LayananModel(string id, string name) : ILayananKey
{
    public string LayananId { get; protected set; } = id;
    public string LayananName { get; protected set; } = name;
}

public interface ILayananKey
{
    string LayananId { get; }
}