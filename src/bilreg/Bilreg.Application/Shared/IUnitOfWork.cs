namespace Bilreg.Application.Shared;

public interface IUnitOfWork
{
    IUnitOfWorkScope Begin();
}

public interface IUnitOfWorkScope : IDisposable
{
    void Complete();
}
