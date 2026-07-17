namespace Taksaka.Abstractions;

public sealed class ResourceDecision
{
    public bool CanExecute { get; init; }

    public string? Reason { get; init; }

    public static ResourceDecision Allow() => new() { CanExecute = true };

    public static ResourceDecision Wait(string reason) =>
        new() { CanExecute = false, Reason = reason };
}
