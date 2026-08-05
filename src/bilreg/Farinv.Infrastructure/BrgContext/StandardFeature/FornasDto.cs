using Farinv.Domain.BrgContext.StandardFeature;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public record FornasDto(string FornasId, string FornasName,
    string KelasTerapi, string KelasTerapi1, string KelasTerapi2, string KelasTerapi3,
    string NamaObat, string Sediaan, string Kekuatan, string Satuan, string MaksPeresepan,
    string RestriksiKelasTerapi, string RestriksiObat, string RestriksiSediaan)
{
    public static FornasDto FromModel(FornasType model)
    {
        return new FornasDto( model.FornasId, model.FornasName,
            model.KelasTerapi, model.KelasTerapi1, model.KelasTerapi2, model.KelasTerapi3,
            model.NamaObat, model.Sediaan, model.Kekuatan, model.Satuan,
            model.MaksPeresepan, model.RestriksiKelasTerapi, model.RestriksiObat, model.RestriksiSediaan);
    }

    public FornasType ToModel()
    {
        return new FornasType( FornasId, FornasName,
            KelasTerapi, KelasTerapi1, KelasTerapi2, KelasTerapi3,
            NamaObat, Sediaan, Kekuatan, Satuan,
            MaksPeresepan, RestriksiKelasTerapi, RestriksiObat, RestriksiSediaan);
    }
}
