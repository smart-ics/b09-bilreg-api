using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.JaminanSub;

public record GroupJaminanType : IGroupJaminanKey
{
    public GroupJaminanType(string groupJaminanId, string groupJaminanName,
        bool isKaryawan, string keterangan)
    {
        Guard.Against.NullOrWhiteSpace(groupJaminanId, nameof(groupJaminanId));
        Guard.Against.NullOrWhiteSpace(groupJaminanName, nameof(groupJaminanName));
        Guard.Against.Null(keterangan, nameof(keterangan));

        GroupJaminanId = groupJaminanId;
        GroupJaminanName = groupJaminanName;
        IsKaryawan = isKaryawan;
        Keterangan = keterangan;
    }
    
    public string GroupJaminanId { get; init; }
    public string GroupJaminanName { get; init; }
    public bool IsKaryawan { get; init; }
    public string Keterangan { get; init; }
    
    
    public static IGroupJaminanKey Key(string id) => new GroupJaminanType(id, "-", false, "");
    public static GroupJaminanType Default => new("-", "-", false, "");
}

public interface IGroupJaminanKey
{
    string GroupJaminanId {get;}
}