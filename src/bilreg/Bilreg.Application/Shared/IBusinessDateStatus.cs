namespace Bilreg.Application.Shared;

public interface IBusinessDateStatus
{
    string Mode { get; }
    DateOnly? FixedDate { get; }
    bool IsSimulation { get; }
    DateTime BusinessNow { get; }
    DateTime SystemNow { get; }
}
