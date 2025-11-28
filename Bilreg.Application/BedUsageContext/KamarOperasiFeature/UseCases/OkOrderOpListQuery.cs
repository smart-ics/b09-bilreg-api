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
    string PreferedDate, string UrgencyLevel, string StatusOrderOp);

public class OkOrderOpListHandler :
    IRequestHandler<OkOrderOpListQuery, IEnumerable<OkOrderOpListResponse>>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkOrderOpListHandler(IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<IEnumerable<OkOrderOpListResponse>> Handle(OkOrderOpListQuery request, CancellationToken cancellationToken)
    {
        //  BUILD
        var listActiveOrderOp = _opCaseRepo.ListActiveOpCase();

        var listOrderVw = _orderOpRepo.ListData(periode)?.ToList()
            ?? throw new ArgumentException($"Order Op at {request.TglOrderYMD} not found");

        var listOrderOp = listOrderVw
            .Select(x => _orderOpRepo.LoadEntity(OrderOpModel.Key(x.OrderOpId))
                .GetValueOrDefault())
            .ToList();

        var result = listOrderOp
            .Select(i => new OkOrderOpListResponse(
                OrderOpId: i.OrderOpId,
                PasienId: i.Pasien.PasienId,
                PasienName: i.Pasien.PasienName,
                DokterId: i.Dokter.PpaId,
                DokterName: i.Dokter.PpaName,
                Icd10Id: i.Icd10.Icd10Id,
                Diagnosa: i.Icd10.Icd10Name,
                NamaOperasi: i.NamaOperasi,
                JenisOperasiId: i.JenisOperasi.JenisOperasiId,
                JenisOperasiName: i.JenisOperasi.JenisOperasiName,
                PreferedDate: i.PreferedDate.ToString("yyyy-MM-dd")
                DokterId: i.Dokter.PetugasMedisId,
                DokterName: i.Dokter.PetugasMedisName,
                PreferedDate: i.PreferedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                UrgencyLevel: i.UrgencyLevel.ToString(),
                StatusOrderOp: i.OrderOpState.ToString()
                ));
        return Task.FromResult(result);
    }
}
