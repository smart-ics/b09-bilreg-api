using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record FormulariumType : IFormulariumKey
{
    #region CREATION

    private FormulariumType(string formulariumId, string formulariumName)
    {
        Guard.Against.NullOrWhiteSpace(formulariumId, nameof(formulariumId));
        Guard.Against.NullOrWhiteSpace(formulariumName, nameof(formulariumName));

        FormulariumId = formulariumId;
        FormulariumName = formulariumName;
    }

    public static FormulariumType Create(string formulariumId, string formulariumName)
        => new(formulariumId, formulariumName);

    public static FormulariumType Default => new("-", "-");

    public static IFormulariumKey Key(string id) => Default with { FormulariumId = id };

    #endregion

    #region PROPERTIES

    public string FormulariumId { get; init; }
    public string FormulariumName { get; init; }

    #endregion
}

public interface IFormulariumKey
{
    string FormulariumId { get; }
}
