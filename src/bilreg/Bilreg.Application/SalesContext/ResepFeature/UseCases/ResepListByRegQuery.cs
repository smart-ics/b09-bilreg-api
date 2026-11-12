using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public record ResepListByRegQuery(string RegId, bool ActiveOnly = true) : IRequest<IEnumerable<ResepListByRegResponse>>;

public record ResepListByRegResponse(
    string ResepId,
    string RegId,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    string LayananId,
    string LayananName,
    string LayananStokId,
    string LayananStokName,
    string UrgenitasId,
    string UrgenitasName,
    string TipeBrgId,
    string TipeBrgName,
    int Iter,
    string Description,
    DateTime TglRencanaPeriksa,
    IReadOnlyList<ResepListObatResponse> ListObat,
    bool IsVoid = false,
    string RingkasanItem = ""
);

public record ResepListObatResponse(
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
    string Route,
    string MealRelation,
    string UddTime,
    string Note,
    IReadOnlyList<ResepListRacikResponse> ListItemRacik,
    bool IsRacik = false
);

public record ResepListRacikResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    decimal Dosis,
    string DosisTxt
);

public class ResepListByRegHandler : IRequestHandler<ResepListByRegQuery, IEnumerable<ResepListByRegResponse>>
{
    private readonly IResepRepo _resepRepo;

    public ResepListByRegHandler(IResepRepo resepRepo)
    {
        _resepRepo = resepRepo;
    }

    public Task<IEnumerable<ResepListByRegResponse>> Handle(ResepListByRegQuery request, CancellationToken cancellationToken)
    {
        var regKey = RegModel.Key(request.RegId);
        var listResep = _resepRepo.ListData(regKey) ?? [];
        if (request.ActiveOnly)
        {
            listResep = listResep.Where(x => !x.IsVoid);
        }
        var response = listResep.Select(GenResponseItem);
        return Task.FromResult(response);
    }

    private static ResepListByRegResponse GenResponseItem(ResepModel resep)
    {
        var reg = resep.Register;
        var dokter = resep.Dokter;
        var layanan = resep.Layanan;
        var layananStok = resep.LayananStok;
        var urgenitas = resep.Urgenitas;
        var tipeBrg = resep.TipeBrg;

        var listObat = resep.ListObat.Select(item =>
        {
            var listItemRacik = item.ListItemRacik.Select(itemRacik => new ResepListRacikResponse(
                itemRacik.NoUrut,
                itemRacik.Brg.BrgId,
                itemRacik.Brg.BrgName,
                itemRacik.Satuan.SatuanId,
                itemRacik.Satuan.SatuanName,
                itemRacik.Qty,
                itemRacik.Dosis,
                itemRacik.DosisTxt
            )).ToList();

            return new ResepListObatResponse(
                item.NoUrut,
                item.Brg.BrgId,
                item.Brg.BrgName,
                item.Satuan.SatuanId,
                item.Satuan.SatuanName,
                item.Qty,
                item.Iter,
                item.Etiket.Signa,
                item.Etiket.Instruction,
                item.Etiket.Frequency,
                item.Etiket.UnitDose,
                item.Etiket.Route,
                item.Etiket.MealRelation,
                item.Etiket.UddTime,
                item.Etiket.Note,
                listItemRacik,
                item.IsRacik
            );
        }).ToList();

        var ringkasan = string.Join(", ", listObat.Select(x => $"{x.BrgName} ({x.Qty} {x.SatuanName})"));

        var result = new ResepListByRegResponse(
            resep.ResepId,
            reg.RegId,
            reg.PasienId,
            reg.PasienName,
            dokter.DokterId,
            dokter.DokterName,
            layanan.LayananId,
            layanan.LayananName,
            layananStok.LayananId,
            layananStok.LayananName,
            urgenitas.UrgenitasId,
            urgenitas.UrgenitasName,
            tipeBrg.TipeBrgId,
            tipeBrg.TipeBrgName,
            resep.Iter,
            resep.Description,
            resep.TglRencanaPeriksa,
            listObat,
            resep.IsVoid,
            ringkasan
        );

        return result;
    }
}