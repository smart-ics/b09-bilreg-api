namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IAggFactory<out TOut, in TKey>
{
    TOut Load(TKey key);
    TOut? LoadOrNull(TKey key);
}