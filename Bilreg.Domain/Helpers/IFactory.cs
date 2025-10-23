namespace Bilreg.Domain.Helpers;

public interface IFactory<out T>
{
    T Default { get; }
}