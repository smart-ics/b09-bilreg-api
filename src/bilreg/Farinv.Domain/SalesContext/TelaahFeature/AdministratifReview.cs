namespace Farinv.Domain.SalesContext.TelaahFeature;

public class AdministratifReview
{
    private readonly Dictionary<AdministratifCheckEnum, bool?> _items;

    #region CREATION
    public AdministratifReview()
    {
        _items = Enum
            .GetValues<AdministratifCheckEnum>()
            .ToDictionary(x => x, _ => (bool?)null);
    }
    #endregion

    #region PROPERTIES
    public bool IsComplete => _items.Values.All(x => x.HasValue);
    public bool IsFullyCompliant => IsComplete && 
        _items.Values.All(x => x == true);
    public bool HasIssue => _items.Values.Any(x => x == false);
    #endregion

    #region BEHAVIOR
    public void SetAppropriate(AdministratifCheckEnum item, bool isAppropriate)
    {
        _items[item] = isAppropriate;
    }
    #endregion
}

public enum AdministratifCheckEnum
{
    Dokter,
    Pasien,
    TanggalResep,
    CareUnit
}
