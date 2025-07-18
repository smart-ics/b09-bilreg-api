using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KabupatenType : IKabupatenKey
{
    public KabupatenType(string kabupatenId, string kabupatenName, PropinsiType propinsi)
    {
        Guard.Against.NullOrWhiteSpace(kabupatenId, nameof(kabupatenId));
        Guard.Against.NullOrWhiteSpace(kabupatenName, nameof(kabupatenName));
        Guard.Against.Null(propinsi, nameof(propinsi));

        KabupatenId = kabupatenId;
        KabupatenName = kabupatenName;
        Propinsi = propinsi;
    }

    public string KabupatenId { get; init; }
    public string KabupatenName { get; init; }
    public PropinsiType Propinsi { get; init; }

    public static IKabupatenKey Key(string id) => new KabupatenType(id, "-", PropinsiType.Default);
    public static KabupatenType Default => new("-", "-", PropinsiType.Default);
}

public interface IKabupatenKey
{
    string KabupatenId {get;}
}