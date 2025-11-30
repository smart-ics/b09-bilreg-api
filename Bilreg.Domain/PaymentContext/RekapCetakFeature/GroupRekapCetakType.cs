using Ardalis.GuardClauses;

namespace Bilreg.Domain.PaymentContext.RekapCetakSub;

public record GroupRekapCetakType : IGroupRekapCetakKey
{
    public GroupRekapCetakType(string groupRekapCetakId, string groupRekapCetakName)
    {
        Guard.Against.NullOrWhiteSpace(groupRekapCetakId, nameof(groupRekapCetakId));
        Guard.Against.NullOrWhiteSpace(groupRekapCetakName, nameof(groupRekapCetakName));

        GroupRekapCetakId = groupRekapCetakId;
        GroupRekapCetakName = groupRekapCetakName;
    }
    
    public string GroupRekapCetakId { get; init; }
    public string GroupRekapCetakName { get; init; }
    
    public static GroupRekapCetakType Default => new("-", "-");
    public static IGroupRekapCetakKey Key(string id) => Default with { GroupRekapCetakId = id };
}

public interface IGroupRekapCetakKey
{
    string GroupRekapCetakId {get;}
}