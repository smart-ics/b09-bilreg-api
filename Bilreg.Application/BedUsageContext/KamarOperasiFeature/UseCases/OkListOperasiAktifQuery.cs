using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListOperasiAktifQuery : IRequest<IEnumerable<OkListOperasiAktifResponse>>;

public record OkListOperasiAktifResponse(string OrderOpId,
    string PasienId, string PasienName,
    string DokterId, string DokterName,
    string NamaOperasi, string PreferedDate, int EstimasiDurasi, 
    string UrgencyLevel, string StatusOrderOp);

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
        //  BUILD
        var listActiveOrderOp = _opCaseRepo.ListActiveOpCase();
        var result = listActiveOrderOp
            .Select(x => new OkListOperasiAktifResponse(x.OrderOpId,
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                x.Dokter.PpaId,
                x.Dokter.PpaName,
                x.NamaOperasi,
                x.PreferedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.EstimasiDurasi,
                x.UrgencyLevel.ToString(),
                x.OpCaseState.ToString()))
            .ToList();

        return Task.FromResult(result.AsEnumerable());
    }
}
