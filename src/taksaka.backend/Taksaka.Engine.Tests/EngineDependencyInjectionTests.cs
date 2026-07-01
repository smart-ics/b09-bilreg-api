using Microsoft.Extensions.DependencyInjection;
using Taksaka.Abstractions;
using Taksaka.Engine;

namespace Taksaka.Engine.Tests;

public sealed class EngineDependencyInjectionTests
{
    [Fact]
    public void AddTaksakaEngine_RegistersScheduler()
    {
        var services = new ServiceCollection();
        services.AddTaksakaEngine();

        using var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IScheduler>();

        Assert.NotNull(scheduler);
    }
}
