using Xunit;

namespace Bilreg.Test.ApotekContext.Support;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApotekApiCollection : ICollectionFixture<ApotekApiWebApplicationFactory>
{
    public const string Name = "ApotekApi";
}
