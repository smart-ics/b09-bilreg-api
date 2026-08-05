using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.StandardFeature;

public record FornasType : IFornasKey
{
    #region CREATION
    public FornasType( 
        string fornasId, string fornasName,
        string kelasTerapi, string kelasTerapi1,
        string kelasTerapi2, string kelasTerapi3,
        string namaObat, string sediaan,
        string kekuatan, string satuan,
        string maksPeresepan, string restriksiKelasTerapi,
        string restriksiObat, string restriksiSediaan)
    {
        FornasId = fornasId;
        FornasName = fornasName;
        KelasTerapi = kelasTerapi;
        KelasTerapi1 = kelasTerapi1;
        KelasTerapi2 = kelasTerapi2;
        KelasTerapi3 = kelasTerapi3;
        NamaObat = namaObat;
        Sediaan = sediaan;
        Kekuatan = kekuatan;
        Satuan = satuan;
        MaksPeresepan = maksPeresepan;
        RestriksiKelasTerapi = restriksiKelasTerapi;
        RestriksiObat = restriksiObat;
        RestriksiSediaan = restriksiSediaan;
    }

    public static FornasType Default => new( fornasId: "-", fornasName: "-",
        kelasTerapi: "-", kelasTerapi1: "-", kelasTerapi2: "-", kelasTerapi3: "-",
        namaObat: "-", sediaan: "-", kekuatan: "-", satuan: "-",
        maksPeresepan: "-", restriksiKelasTerapi: "-", restriksiObat: "-", restriksiSediaan: "-");

    public static IFornasKey Key(string id) => Default with { FornasId = id };
    #endregion

    #region PROPERTIES
    public string FornasId { get; init; }
    public string FornasName { get; init; }
    public string KelasTerapi { get; init; }
    public string KelasTerapi1 { get; init; }
    public string KelasTerapi2 { get; init; }
    public string KelasTerapi3 { get; init; }
    public string NamaObat { get; init; }
    public string Sediaan { get; init; }
    public string Kekuatan { get; init; }
    public string Satuan { get; init; }
    public string MaksPeresepan { get; init; }
    public string RestriksiKelasTerapi { get; init; }
    public string RestriksiObat { get; init; }
    public string RestriksiSediaan { get; init; }
    #endregion

    #region BEHAVIOUR
    public FornasReff ToReff() => new(FornasId, FornasName);
    #endregion
}

public interface IFornasKey
{
    string FornasId { get; }
}

public record FornasReff(string FornasId, string FornasName);
