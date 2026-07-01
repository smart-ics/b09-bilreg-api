namespace Farinv.Domain.SalesContext.TelaahFeature;

public class ClinicalReview
{
    private readonly Dictionary<ClinicalCheckEnum, bool?> _items;

    #region CREATION
    public ClinicalReview(string indikasi, string alergi)
    {
        Indikasi = indikasi;
        Alergi = alergi;

        _items = Enum
            .GetValues<ClinicalCheckEnum>()
            .ToDictionary(x => x, _ => (bool?)null);
    }
    #endregion

    #region PROPERTIES
    public string Indikasi { get; private set; }
    public string Alergi { get; private set; }
    public bool IsComplete => _items.Values.All(x => x.HasValue);

    public bool HasIssue => _items.Values.Any(x => x == false);
    #endregion

    #region BEHAVIOR
    public void UpdateDiagnosa(string indikasi)
    {
        Indikasi = indikasi;
    }

    public void UpdateAlergi(string alergi)
    {
        Alergi = alergi;
    }
    public void SetAppropriate(ClinicalCheckEnum item, bool isAppropriate)
    {
        _items[item] = isAppropriate;
    }
    #endregion
}

public enum ClinicalCheckEnum
{
    Indication,
    Allergy
}
