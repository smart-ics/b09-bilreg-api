using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public interface IKotaWriter: INunaWriterWithReturn<KotaType>
{
    public void Delete(IKotaKey key);
}

public class KotaWriter : IKotaWriter
{
    private readonly IKotaDal _kotaDal;

    public KotaWriter(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public KotaType Save(KotaType type)
    {
        var kotaDb = _kotaDal.GetData(type);
        if (kotaDb is null)
            _kotaDal.Insert(type);
        else
            _kotaDal.Update(type);
        return type;
    }

    public void Delete(IKotaKey key)
    {
        _kotaDal.Delete(key);
    }
}
