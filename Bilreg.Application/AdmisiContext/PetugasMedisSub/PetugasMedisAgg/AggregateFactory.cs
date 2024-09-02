namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IFactoryLoadOrNull<out TOut, in TKey>
{
    TOut? LoadOrNull(TKey key);
}

public interface IFactoryLoad<out TOut, in TKey>
{
    TOut Load(TKey key);
}
public abstract class AggFactory<TOut, TKey> 
    : IFactoryLoad<TOut, TKey>, IFactoryLoadOrNull<TOut, TKey>
{
    public TOut? LoadOrNull(TKey key)
    {
        var aggregate = default(TOut);
        try
        {
            aggregate = LoadAggregate(key);
        }
        catch (KeyNotFoundException ex)
        {
        }

        return aggregate;
    }

    public TOut Load(TKey key)
    {
        var aggregate = LoadAggregate(key);
        return aggregate;
    }
    protected abstract TOut LoadAggregate(TKey key);
}