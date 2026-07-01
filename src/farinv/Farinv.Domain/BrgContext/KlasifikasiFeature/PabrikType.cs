using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record PabrikType : IPabrikKey
{
    #region CREATION
    public PabrikType(
        string pabrikId,
        string pabrikName)
    {
        PabrikId = pabrikId;
        PabrikName = pabrikName;
    }

    public static PabrikType Create(
        string pabrikId,
        string pabrikName)
    {
        Guard.Against.NullOrWhiteSpace(pabrikId, nameof(pabrikId));
        Guard.Against.NullOrWhiteSpace(pabrikName, nameof(pabrikName));
        return new PabrikType(
            pabrikId,
            pabrikName);
    }

    public static PabrikType Default => new("-", "-");

    public static IPabrikKey Key(string id) => Default with { PabrikId = id };
    #endregion

    #region PROPERTIES
    public string PabrikId { get; init; }
    public string PabrikName { get; init; }
    public decimal KoefFormula { get; init; }
    #endregion

    #region BEHAVIOUR
    public PabrikReff ToReff() => new(PabrikId, PabrikName);
    #endregion
}

public interface IPabrikKey
{
    string PabrikId { get; }
}

public record PabrikReff(string PabrikId, string PabrikName);