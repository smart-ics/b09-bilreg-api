namespace Bilreg.Domain.Helpers;

public interface IFactory<out T, out TKey>
{
    T Default { get; }
    TKey Key(string id);
}