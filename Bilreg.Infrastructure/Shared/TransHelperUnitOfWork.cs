using Bilreg.Application.Shared;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.Shared;

public sealed class TransHelperUnitOfWork : IUnitOfWork
{
    public IUnitOfWorkScope Begin() => new TransHelperUnitOfWorkScope();
}

internal sealed class TransHelperUnitOfWorkScope : IUnitOfWorkScope
{
    private readonly dynamic _scope = TransHelper.NewScope();

    public void Complete() => _scope.Complete();

    public void Dispose() => _scope.Dispose();
}
