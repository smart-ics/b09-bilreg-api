using System.Reflection;
using NetArchTest.Rules;

namespace Taksaka.Architecture.Tests;

public sealed class ArchitectureTests
{
  private static readonly Assembly AbstractionsAssembly = typeof(Taksaka.Abstractions.IWorker).Assembly;
  private static readonly Assembly InfrastructureAssembly = typeof(Taksaka.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions).Assembly;
  private static readonly Assembly EngineAssembly = typeof(Taksaka.Engine.EngineServiceCollectionExtensions).Assembly;
  private static readonly Assembly HostingAssembly = typeof(Taksaka.Hosting.DependencyInjection.HostingServiceCollectionExtensions).Assembly;
  private static readonly Assembly ServerAssembly = typeof(Taksaka.Server.DependencyInjection.ServerServiceCollectionExtensions).Assembly;
  private static readonly Assembly MaintenanceAssembly = typeof(Taksaka.Workers.Maintenance.Worker.AntrianConsistencyRepairWorker).Assembly;
  private static readonly Assembly IntegrationAssembly = typeof(Taksaka.Workers.Integration.Workers.SampleIntegrationWorker).Assembly;
  private static readonly Assembly ProjectionAssembly = typeof(Taksaka.Workers.Projection.Workers.SampleProjectionWorker).Assembly;

  [Fact]
  public void PlatformProjects_MustNotReferenceWorkerAssemblies()
  {
    var platformAssemblies = new[]
    {
      AbstractionsAssembly,
      InfrastructureAssembly,
      EngineAssembly,
      HostingAssembly,
      ServerAssembly
    };

    foreach (var assembly in platformAssemblies)
    {
      var result = Types.InAssembly(assembly)
        .ShouldNot()
        .HaveDependencyOnAny(
          MaintenanceAssembly.GetName().Name!,
          IntegrationAssembly.GetName().Name!,
          ProjectionAssembly.GetName().Name!)
        .GetResult();

      Assert.True(result.IsSuccessful, $"{assembly.GetName().Name}: {FormatFailures(result)}");
    }
  }

  [Fact]
  public void Abstractions_MustNotContainWorkerSpecificNamespaces()
  {
    var result = Types.InAssembly(AbstractionsAssembly)
      .ShouldNot()
      .ResideInNamespace("Taksaka.Abstractions.Maintenance")
      .Or()
      .HaveNameMatching("AntrianConsistency.*")
      .GetResult();

    Assert.True(result.IsSuccessful, FormatFailures(result));
  }

  [Fact]
  public void WorkerPlugins_MustOnlyReferenceAbstractionsAndCore()
  {
    var workerAssemblies = new[] { MaintenanceAssembly, IntegrationAssembly, ProjectionAssembly };

    foreach (var assembly in workerAssemblies)
    {
      var references = assembly.GetReferencedAssemblies()
        .Select(reference => reference.Name)
        .Where(name => name is not null && name.StartsWith("Taksaka.", StringComparison.Ordinal))
        .ToList();

      Assert.All(references, name =>
        Assert.True(
          name is "Taksaka.Abstractions" or "Taksaka.Core",
          $"{assembly.GetName().Name} must not reference {name}"));
    }
  }

  [Fact]
  public void Hosting_MustNotReferenceDataAccessPackages()
  {
    var references = HostingAssembly.GetReferencedAssemblies()
      .Select(reference => reference.Name)
      .ToList();

    Assert.DoesNotContain("Dapper", references);
    Assert.DoesNotContain("Microsoft.Data.SqlClient", references);
  }

  private static string FormatFailures(TestResult result) =>
    string.Join(", ", result.FailingTypes?.Select(type => type.FullName) ?? []);
}
