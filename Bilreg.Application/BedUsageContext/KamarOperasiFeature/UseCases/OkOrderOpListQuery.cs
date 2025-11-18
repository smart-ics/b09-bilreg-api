using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkOrderOpListQuery() : IRequest<IEnumerable<OkOrderOpListResponse>>;

public record OkOrderOpListResponse(string OrderOpId,
    string PasienId, string PasienName,
    string DokterId, string DokterName,
    string TglOperasi, string StatusOrderOp);

public class OkOrderOpListHandler :
    IRequestHandler<OkOrderOpListQuery, IEnumerable<OkOrderOpListResponse>>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";

    public OkOrderOpListHandler(IOrderOpRepo orderOpRepo)
    {
        _orderOpRepo = orderOpRepo;
    }

    public Task<IEnumerable<OkOrderOpListResponse>> Handle(OkOrderOpListQuery request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotEmpty(request.TglOrderYMD);
        Guard.IsTrue(request.TglOrderYMD.IsValidTgl(FORMAT_TGL_YMD));

        //  BUILD
        var periode = new Periode(DateTime.Parse(request.TglOrderYMD, CultureInfo.InvariantCulture));

        var listOrderVw = _orderOpRepo.ListData(periode)?.ToList()
            ?? throw new ArgumentException($"Order Op at {request.TglOrderYMD} not found");

        var listOrderOp = listOrderVw
            .Select(x => _orderOpRepo.LoadEntity(OrderOpModel.Key(x.OrderOpId))
                .GetValueOrDefault())
            .ToList();

        var result = listOrderOp
            .Select(i => new OkOrderOpListResponse(
                OrderOpId: i.OrderOpId,
                RegId: i.Reg.RegId,
                PasienId: i.Pasien.PasienId,
                PasienName: i.Pasien.PasienName,
                DokterId: i.Dokter.PetugasMedisId,
                DokterName: i.Dokter.PetugasMedisName,
                Icd10Id: i.Icd10.Icd10Id,
                Diagnosa: i.Icd10.Icd10Name,
                NamaOperasi: i.NamaOperasi,
                JenisOperasiId: i.JenisOperasi.JenisOperasiId,
                JenisOperasiName: i.JenisOperasi.JenisOperasiName,
                PreferedDate: i.PreferedDate.ToString("yyyy-MM-dd")
                ));
        return Task.FromResult(result);
    }
}
