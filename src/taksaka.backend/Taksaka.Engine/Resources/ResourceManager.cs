using System.Collections.Concurrent;
using Taksaka.Abstractions;
using Taksaka.Core.Entities;

namespace Taksaka.Engine.Resources;

public sealed class ResourceManager(IWorkerRegistry workerRegistry) : IResourceManager
{
    private readonly ConcurrentDictionary<string, WorkerConcurrencyState> _workerStates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, string> _activeJobs = new();

    public Task<ResourceDecision> TryAcquireAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(job.WorkerName))
        {
            return Task.FromResult(ResourceDecision.Wait("Job has no worker name."));
        }

        if (!workerRegistry.TryGetWorker(job.WorkerName, out var worker) || worker is null)
        {
            return Task.FromResult(ResourceDecision.Wait($"Worker '{job.WorkerName}' is not registered."));
        }

        var maxConcurrency = Math.Max(1, worker.Descriptor.Policy.MaxConcurrency);
        var state = _workerStates.GetOrAdd(job.WorkerName, _ => new WorkerConcurrencyState(maxConcurrency));

        if (state.ActiveCount >= state.MaxConcurrency)
        {
            return Task.FromResult(ResourceDecision.Wait(
                $"Worker '{job.WorkerName}' has reached max concurrency ({state.MaxConcurrency})."));
        }

        state.Increment();
        _activeJobs[job.Id] = job.WorkerName;
        return Task.FromResult(ResourceDecision.Allow());
    }

    public void Release(Job job)
    {
        if (!_activeJobs.TryRemove(job.Id, out var workerName))
        {
            return;
        }

        if (_workerStates.TryGetValue(workerName, out var state))
        {
            state.Decrement();
        }
    }

    private sealed class WorkerConcurrencyState(int maxConcurrency)
    {
        private int _activeCount;

        public int MaxConcurrency { get; } = maxConcurrency;

        public int ActiveCount => Volatile.Read(ref _activeCount);

        public void Increment() => Interlocked.Increment(ref _activeCount);

        public void Decrement()
        {
            var value = Interlocked.Decrement(ref _activeCount);
            if (value < 0)
            {
                Interlocked.Exchange(ref _activeCount, 0);
            }
        }
    }
}
