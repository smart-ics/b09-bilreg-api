using System.Globalization;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListOperasiAktifQuery : IRequest<IEnumerable<OkListOperasiAktifResponse>>;

public record OkListOperasiAktifResponse(
    string OrderOpId,
    string PasienId,
    string PasienName,
    string DokterPengorderId,
    string DokterPengorderName,
    string DokterOperatorId,
    string DokterOperatorName,
    string NamaOperasi,
    string ProsedurTambahan,
    string KlasifikasiOperasi,
    string PreferedDate,
    int EstimasiDurasi,
    string UrgencyLevel,
    string StatusOrderOp)
{
    public string DokterId => !string.IsNullOrWhiteSpace(DokterOperatorId) && DokterOperatorId != "-" ? DokterOperatorId : DokterPengorderId;
    public string DokterName => !string.IsNullOrWhiteSpace(DokterOperatorName) && DokterOperatorName != "-" ? DokterOperatorName : DokterPengorderName;
}

public class OkListOperasiAktifHandler :
    IRequestHandler<OkListOperasiAktifQuery, IEnumerable<OkListOperasiAktifResponse>>
{
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;

    public OkListOperasiAktifHandler(IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo)
    {
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
    }

    public Task<IEnumerable<OkListOperasiAktifResponse>> Handle(OkListOperasiAktifQuery request, 
        CancellationToken cancellationToken)
    {
        var listActiveOrderOp = _opCaseRepo.ListActiveOpCase()?.ToList() ?? [];
        var result = new List<OkListOperasiAktifResponse>();

        foreach (var x in listActiveOrderOp)
        {
            var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(x.OrderOpId))
                .Match(onSome: m => m, onNone: () => null as OrderOpModel);

            if (orderOp != null)
            {
                result.Add(new OkListOperasiAktifResponse(
                    x.OrderOpId,
                    x.Pasien.PasienId,
                    x.Pasien.PasienName,
                    orderOp.DokterPengorder.PpaId,
                    orderOp.DokterPengorder.PpaName,
                    orderOp.DokterOperator.PpaId,
                    orderOp.DokterOperator.PpaName,
                    orderOp.NamaOperasi,
                    orderOp.ProsedurTambahan,
                    orderOp.KlasifikasiOperasi.ToString(),
                    x.PreferedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    orderOp.EstimasiDurasiInMinutes,
                    orderOp.UrgencyLevel.ToString(),
                    x.OpCaseState.ToString()));
            }
            else
            {
                result.Add(new OkListOperasiAktifResponse(
                    x.OrderOpId,
                    x.Pasien.PasienId,
                    x.Pasien.PasienName,
                    x.Dokter.PpaId,
                    x.Dokter.PpaName,
                    string.Empty,
                    string.Empty,
                    x.NamaOperasi,
                    string.Empty,
                    KlasifikasiOperasiEnum.Kecil.ToString(),
                    x.PreferedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    x.EstimasiDurasi,
                    x.UrgencyLevel.ToString(),
                    x.OpCaseState.ToString()));
            }
        }

        return Task.FromResult(result.AsEnumerable());
    }
}
