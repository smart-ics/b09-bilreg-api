namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public record AntrianPatternType
{
    private readonly List<AntrianPatternItemType> _listPttrn;
    public AntrianPatternType(string tipe, int max, int rsrvd
        , IEnumerable<AntrianPatternItemType> listPattrn)
    {
        Tipe = tipe;
        Max = max;
        Rsrvd = rsrvd;
        _listPttrn = listPattrn.ToList()?.ToList() ?? [];
    }
    /// <summary>
    ///public AntrianPatternType Create(st)
    /// </summary>

    public static AntrianPatternType Default =>
        new AntrianPatternType(string.Empty, 0, 0, []);
    public string Tipe { get; init; }
    public int Max { get; init; }
    public int Rsrvd { get; init; }
    public List<AntrianPatternItemType> Pttrn => _listPttrn;
}

public record AntrianPatternItemType
{
    public AntrianPatternItemType(string desc, int qty)
    {
        Desc = desc;
        Qty = qty;
    }
    public static AntrianPatternItemType Default
    => new AntrianPatternItemType(string.Empty, 0);
    public string Desc { get; init; }
    public int Qty { get; init; }
}