using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public class KarcisLayananModel(string id, string layananId, string layananName) 
    : IKarcisKey, ILayananKey
{
    public string KarcisId { get; protected set; } = id;
    public string LayananId { get; protected set; } = layananId;
    public string LayananName { get; protected set; } = layananName;
    public void SetKarcisId(string id) => KarcisId = id;
}