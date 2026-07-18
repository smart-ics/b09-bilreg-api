namespace Taksaka.Abstractions;

public sealed class WorkerResult
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public static WorkerResult Success(string? message = null) =>
        new() { IsSuccess = true, Message = message };

    public static WorkerResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}
