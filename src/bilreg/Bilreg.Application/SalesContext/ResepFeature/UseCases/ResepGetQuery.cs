using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public record ResepGetQuery(string ResepId): IRequest<ResepGetResponse>;

public record ResepGetResponse(
    string ResepId,
    string RegId,
    string PasienId,
    string PasienName,
    decimal BodyWeight,
    decimal BodyHeight,
    decimal LingkarPinggang,
    string DokterId,
    string DokterName,
    string LayananId,
    string LayananName,
    string UrgenitasId,
    string UrgenitasName,
    string TipeBrgId,
    string TipeBrgName,
    int Iter,
    string Description,
    IReadOnlyList<ResepGetListItemObatResponse> ListObat
);

public record ResepGetListItemObatResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    int Iter,
    string Signa,
    string Instruction,
    int Frequency,
    decimal UnitDose,
    string Note,
    IReadOnlyList<ResepGetListItemRacikResponse> ListItemRacik
);

public record ResepGetListItemRacikResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    decimal Dosis,
    string DosisTxt
);

public class ResepGetHandler: IRequestHandler<ResepGetQuery, ResepGetResponse>
{
    private readonly IResepRepo _resepRepo;

    public ResepGetHandler(IResepRepo resepRepo)
    {
        _resepRepo = resepRepo;
    }

    public Task<ResepGetResponse> Handle(ResepGetQuery request, CancellationToken cancellationToken)
    {
        var resepKey = ResepModel.Key(request.ResepId);
        var resep = _resepRepo.LoadEntity(resepKey)
            .GetValueOrThrow($"Resep with Id: {resepKey.ResepId} not found.");

        var response = GenResponse(resep);
        return Task.FromResult(response);
    }

    private static ResepGetResponse GenResponse(ResepModel resep)
    {
        var listObat = resep.ListObat.Select(item =>
        {
            var listItemRacik = new List<ResepGetListItemRacikResponse>();
            if (item.ListItemRacik.Any())
                item.ListItemRacik.ForEach(itemRacik =>
                {
                    var obatRacik = new ResepGetListItemRacikResponse(
                        itemRacik.NoUrut, itemRacik.Brg.BrgId, itemRacik.Brg.BrgName, itemRacik.Satuan.SatuanId,
                        itemRacik.Satuan.SatuanName, itemRacik.Qty, itemRacik.Dosis, itemRacik.DosisTxt);
                    listItemRacik.Add(obatRacik);
                });

            var obat = new ResepGetListItemObatResponse(item.NoUrut, item.Brg.BrgId, item.Brg.BrgName, item.Satuan.SatuanId,
                item.Satuan.SatuanName, item.Qty, item.Iter, item.Etiket.Signa, item.Etiket.Instruction, item.Etiket.Frequency,
                item.Etiket.UnitDose, item.Etiket.Note, listItemRacik);

            return obat;
        }).ToList();

        var reg = resep.Register;
        var bodyMetric = resep.BodyMetric;
        var dokter = resep.Dokter;
        var layanan = resep.Layanan;
        var urgenitas = resep.Urgenitas;
        var tipeBrg = resep.TipeBrg;

        var response = new ResepGetResponse(
            resep.ResepId, reg.RegId, reg.PasienId, reg.PasienName, bodyMetric.BodyWeight, bodyMetric.BodyHeight,
            bodyMetric.LingkarPinggang, dokter.DokterId, dokter.DokterName, layanan.LayananId, layanan.LayananName,
            urgenitas.UrgenitasId, urgenitas.UrgenitasName, tipeBrg.TipeBrgId, tipeBrg.TipeBrgName, resep.Iter, resep.Description,
            listObat
        );

        return response;
    }
}