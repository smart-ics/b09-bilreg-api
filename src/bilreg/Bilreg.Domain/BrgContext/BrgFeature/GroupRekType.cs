using Ardalis.GuardClauses;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Domain.BrgContext.BrgFeature;

public record GroupRekType : IGroupRekKey
{
    #region CREATION
    public GroupRekType(string groupRekId, string groupRekName, GroupRekDkType groupRekDk)
        : this(groupRekId, groupRekName, groupRekDk, string.Empty, CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default, CoaType.Default)
    {
    }

    public GroupRekType(
        string groupRekId,
        string groupRekName,
        GroupRekDkType groupRekDk,
        string rekapCetakId,
        CoaType rekJual,
        CoaType rekJualRajal,
        CoaType rekDisc,
        CoaType rekTax,
        CoaType rekPersediaan)
    {
        Guard.Against.NullOrWhiteSpace(groupRekId, nameof(groupRekId));
        Guard.Against.NullOrWhiteSpace(groupRekName, nameof(groupRekName));
        Guard.Against.Null(groupRekDk, nameof(groupRekDk));
        Guard.Against.Null(rekapCetakId, nameof(rekapCetakId));
        Guard.Against.Null(rekJual, nameof(rekJual));
        Guard.Against.Null(rekJualRajal, nameof(rekJualRajal));
        Guard.Against.Null(rekDisc, nameof(rekDisc));
        Guard.Against.Null(rekTax, nameof(rekTax));
        Guard.Against.Null(rekPersediaan, nameof(rekPersediaan));

        GroupRekId = groupRekId;
        GroupRekName = groupRekName;
        GroupRekDk = groupRekDk;
        RekapCetakId = rekapCetakId;
        RekJual = rekJual;
        RekJualRajal = rekJualRajal;
        RekDisc = rekDisc;
        RekTax = rekTax;
        RekPersediaan = rekPersediaan;
    }

    public static GroupRekType Create(string groupRekId, string groupRekName, GroupRekDkType groupRekDk)
    {
        return new GroupRekType(groupRekId, groupRekName, groupRekDk);
    }

    public static GroupRekType Create(
        string groupRekId,
        string groupRekName,
        GroupRekDkType groupRekDk,
        string rekapCetakId,
        CoaType rekJual,
        CoaType rekJualRajal,
        CoaType rekDisc,
        CoaType rekTax,
        CoaType rekPersediaan)
    {
        return new GroupRekType(groupRekId, groupRekName, groupRekDk, rekapCetakId, rekJual, rekJualRajal, rekDisc, rekTax, rekPersediaan);
    }

    public static GroupRekType Default => new("-", "-", GroupRekDkType.Default);

    public static IGroupRekKey Key(string id) => Default with { GroupRekId = id };
    #endregion

    #region PROPERTIES
    public string GroupRekId { get; init; }
    public string GroupRekName { get; init; }
    public GroupRekDkType GroupRekDk { get; init; }
    public string RekapCetakId { get; init; }
    public CoaType RekJual { get; init; }
    public CoaType RekJualRajal { get; init; }
    public CoaType RekDisc { get; init; }
    public CoaType RekTax { get; init; }
    public CoaType RekPersediaan { get; init; }
    #endregion

    #region BEHAVIOR
    public GroupRekReff ToReff() => new(GroupRekId, GroupRekName);
    #endregion
}

public interface IGroupRekKey
{
    string GroupRekId { get; }
}

public record GroupRekReff(string GroupRekId, string GroupRekName);