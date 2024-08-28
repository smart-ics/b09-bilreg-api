namespace Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

public class LayananModel : ILayananKey
{
    public string LayananId { get; private set; }
    public string LayananName { get; private set; }
}

public interface ILayananKey
{
    string LayananId { get; }
}