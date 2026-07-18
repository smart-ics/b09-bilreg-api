# Taksaka — Plugin Development Guide

How to create, build, and deploy Worker plugins for the Taksaka platform based on the **current** codebase and contracts.

---

## Overview

Workers are .NET class libraries that implement `IWorker`. They ship as DLL files in the host `plugins/` folder. The host must reference only `Taksaka.Abstractions` (and transitively `Taksaka.Core` for types used in signatures).

> **Current limitation:** The host discovers plugin DLLs at startup but does **not** load them or execute `IWorker`. Follow this guide to prepare plugins for when assembly loading and dispatch are implemented.

---

## Folder Structure

### Repository layout (reference implementations)

```text
src/taksaka.backend/
  Taksaka.Abstractions/          ← Worker contract (reference this)
  Taksaka.Core/                  ← Entities, enums, policies (transitive)
  Taksaka.Workers.Integration/
    Workers/
      SampleIntegrationWorker.cs
    Taksaka.Workers.Integration.csproj
  Taksaka.Workers.Projection/
    Workers/
      SampleProjectionWorker.cs
    Taksaka.Workers.Projection.csproj
  Taksaka.Server/
    plugins/                     ← Populated on build (output folder)
```

### New plugin project (recommended)

```text
Taksaka.Workers.MyFeature/
  Workers/
    MyFeatureWorker.cs
  Taksaka.Workers.MyFeature.csproj
```

Deploy output:

```text
{ServerInstall}/plugins/Taksaka.Workers.MyFeature.dll
```

Place DLLs in the **root** of `plugins/` — subfolders are not scanned.

---

## Required Interfaces

### `IWorker`

```csharp
public interface IWorker
{
    WorkerDescriptor Descriptor { get; }
    Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default);
}
```

### `WorkerDescriptor`

| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Unique worker identifier (e.g. `"registration-projection"`) |
| `Category` | `WorkerCategory` | `Projection`, `Integration`, `Business`, `Maintenance` |
| `Policy` | `WorkerExecutionPolicy` | Concurrency, retry, timeout, circuit breaker flag |

### `WorkerResult`

```csharp
WorkerResult.Success("optional message");
WorkerResult.Failure("error description");
```

### `Job` (input)

| Property | Type |
|----------|------|
| `Id` | `Guid` |
| `Payload` | `string` (JSON recommended) |
| `Priority` | `JobPriority` |
| `Status` | `JobStatus` |

---

## Complete Example Worker

Create `Taksaka.Workers.Email/Workers/EmailNotificationWorker.cs`:

```csharp
using System.Text.Json;
using Taksaka.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Core.Policies;

namespace Taksaka.Workers.Email.Workers;

public sealed class EmailNotificationWorker : IWorker
{
    public WorkerDescriptor Descriptor { get; } = new()
    {
        Name = "email-notification",
        Category = WorkerCategory.Business,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 8,
            MaxRetryCount = 3,
            Timeout = TimeSpan.FromMinutes(2),
            CircuitBreakerEnabled = true
        }
    };

    public async Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(job.Payload))
            return WorkerResult.Failure("Payload is required.");

        EmailJobPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EmailJobPayload>(job.Payload);
        }
        catch (JsonException ex)
        {
            return WorkerResult.Failure($"Invalid payload JSON: {ex.Message}");
        }

        if (payload?.To is null)
            return WorkerResult.Failure("Payload.To is required.");

        // Business logic — send email via hospital SMTP/API
        await Task.Delay(100, cancellationToken); // placeholder

        return WorkerResult.Success($"Email queued for {payload.To}");
    }

    private sealed record EmailJobPayload(string To, string Subject, string Body);
}
```

### Project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Taksaka.Abstractions\Taksaka.Abstractions.csproj" />
    <ProjectReference Include="..\Taksaka.Core\Taksaka.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## Registration

### Build-time (current)

Add to `Taksaka.Server.csproj` `CopyPlugins` target:

```xml
<Copy
  SourceFiles="$(MSBuildProjectDirectory)\..\Taksaka.Workers.Email\bin\$(Configuration)\$(TargetFramework)\Taksaka.Workers.Email.dll"
  DestinationFolder="$(PluginsOutputPath)"
  SkipUnchangedFiles="true" />
```

Add project reference with `ReferenceOutputAssembly=false`:

```xml
<ProjectReference Include="..\Taksaka.Workers.Email\Taksaka.Workers.Email.csproj">
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
</ProjectReference>
```

### Runtime (future)

When plugin loading is implemented, registration will likely:

1. Load each `*.dll` from `PluginLoader:PluginsPath`
2. Find concrete `IWorker` types
3. Register in worker catalog (not necessarily ASP.NET Core DI for scoped dependencies)

**Do not** reference `Taksaka.Engine` or `Taksaka.Server` from worker projects.

---

## Configuration

Workers should read configuration via:

- **Job payload** — per-job parameters (preferred for job-specific data)
- **Environment variables** — deployment secrets
- **Future:** `IConfiguration` injected when host supports worker DI scope

No worker-specific `appsettings` section exists today.

---

## Dependency Injection

Workers are **plain classes** today — no constructor injection from host.

| Approach | When to use |
|----------|-------------|
| Parameterless constructor + static/config | Current state |
| Factory registered in host | Future |
| `IServiceProvider` scope per job | Future recommended pattern |

Keep workers **stateless** between executions (architecture rule).

---

## Testing

### Unit test example

```csharp
[Fact]
public async Task ExecuteAsync_ValidPayload_ReturnsSuccess()
{
    var worker = new EmailNotificationWorker();
    var job = new Job
    {
        Payload = """{"To":"user@hospital.id","Subject":"Test","Body":"Hi"}"""
    };

    var result = await worker.ExecuteAsync(job);

    Assert.True(result.IsSuccess);
}
```

Reference: `Taksaka.Core.Tests`, `Taksaka.Engine.Tests`.

Run:

```powershell
dotnet test src\taksaka.backend\Taksaka.sln
```

---

## Packaging

1. `dotnet build -c Release` worker project.
2. Collect `bin/Release/net8.0/Taksaka.Workers.*.dll`.
3. Include NuGet dependency DLLs if worker uses packages not already in host.
4. Copy to server `plugins/`.
5. Restart server (required until hot-reload exists).

---

## Deployment

1. Stop Taksaka service.
2. Backup existing `plugins/` folder.
3. Copy new DLL(s).
4. Start service.
5. Verify log: `Discovered plugin assembly: ...Taksaka.Workers.Email.dll`

---

## Versioning

| Practice | Recommendation |
|----------|----------------|
| Assembly version | Match release tag (e.g. `1.2.0`) |
| Worker name | Stable `Descriptor.Name` — do not rename without migration |
| Breaking payload | Version field inside JSON payload |
| Side-by-side DLLs | Avoid duplicate worker names in one folder |

Document worker changelog per hospital deployment.

---

## Best Practices

1. **Single responsibility** — one worker per job type.
2. **Idempotent execution** — safe on retry.
3. **Return `WorkerResult.Failure`** for business errors; throw only for unexpected faults (when engine handles exceptions).
4. **Parse payload defensively** — validate before external I/O.
5. **Respect `CancellationToken`** — pass to HTTP/SQL calls.
6. **No scheduling** — never enqueue follow-up jobs from worker (use API/scheduler).
7. **No retry logic** — engine owns retry.
8. **Short transactions** — commit before slow external calls when possible.
9. **Structured logging** — use `ILogger` when host injects it (future).
10. **Target `net8.0`** — match host.

---

## Common Mistakes

| Mistake | Consequence |
|---------|-------------|
| Reference `Taksaka.Engine` | Circular dependency; violates plugin isolation |
| Put DLL in `plugins/SubFolder/` | Not discovered (top directory only) |
| Target `net8.0` | Load failure when loading is enabled |
| Stateful instance fields | Race conditions under concurrency |
| Long blocking calls without cancellation | Timeouts cannot abort (when enforced) |
| Same `Descriptor.Name` in two DLLs | Ambiguous dispatch (future) |
| Expecting DI today | Null/services unavailable |
| Implement retry in worker | Double retry with engine policy |

---

## Reference Implementations

### Sample Integration Worker

```12:24:src/taksaka.backend/Taksaka.Workers.Integration/Workers/SampleIntegrationWorker.cs
        Name = "sample-integration",
        Category = WorkerCategory.Integration,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 2,
            MaxRetryCount = 5,
            Timeout = TimeSpan.FromMinutes(5)
        }
    };

    public Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkerResult.Success("Sample integration worker completed."));
```

### Sample Projection Worker

```12:23:src/taksaka.backend/Taksaka.Workers.Projection/Workers/SampleProjectionWorker.cs
        Name = "sample-projection",
        Category = WorkerCategory.Projection,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 4,
            MaxRetryCount = null,
            Timeout = null
        }
    };

    public Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkerResult.Success("Sample projection worker completed."));
```

---

## Related Documents

- [01-deployment-guide.md](01-deployment-guide.md)
- [05-architecture-runtime.md](05-architecture-runtime.md)
- [09-administrator-guide.md](09-administrator-guide.md)
