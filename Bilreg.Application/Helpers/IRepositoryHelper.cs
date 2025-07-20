using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.Helpers;


public interface ISaveChange<in T>
{
    void SaveChanges(T model);
}

public interface ILoadEntity<TOut, in TIn>
{
    MayBe<TOut> LoadEntity(TIn key);
}

public interface IDeleteEntity<in T>
{
    void DeleteEntity(T key);
}