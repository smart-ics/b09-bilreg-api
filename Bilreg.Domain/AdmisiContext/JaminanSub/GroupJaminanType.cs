using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.DemografiFeature;
using FluentAssertions;
using Xunit;

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
    public GroupJaminanReff ToReff() => new(GroupJaminanId, GroupJaminanName);
    
    
    public static IGroupJaminanKey Key(string id) => new GroupJaminanType(id, "-", false, "");
    public static GroupJaminanType Default => new("-", "-", false, "");
}

public interface IGroupJaminanKey
{
    string GroupJaminanId {get;}
}

public record GroupJaminanReff(string GroupJaminanId, string GroupJaminanName);

public class GroupJaminanTypeTest
{
    [Fact]
    public void UT1_GivenValidArgument_WhenConstruct_ThenSuccess()
    {
        var sut = new GroupJaminanType("1", "2", false, "");
        sut.GroupJaminanId.Should().Be("1");
        sut.GroupJaminanName.Should().Be("2");
    }
    [Fact]
    public void UT2_WhenDefault_ThenSuccess()
    {
        var sut = GroupJaminanType.Default;
        sut.GroupJaminanId.Should().Be("-");
        sut.GroupJaminanName.Should().Be("-");
    }
}