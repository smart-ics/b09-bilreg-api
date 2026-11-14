using System.Globalization;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListOperasiAktifQuery(
    string? Status = null,
    string? TglAwal = null,
    string? TglAkhir = null,
    int? Limit = null) : IRequest<IEnumerable<OkListOperasiAktifResponse>>;

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
    private readonly IOpCaseRepo _opCaseRepo;

    public OkListOperasiAktifHandler(IOpCaseRepo opCaseRepo)
    {
        _opCaseRepo = opCaseRepo;
    }

    public Task<IEnumerable<OkListOperasiAktifResponse>> Handle(OkListOperasiAktifQuery request, 
        CancellationToken cancellationToken)
    {
        var opCaseState = ResolveOpCaseState(request.Status);
        var tglAwal = DateTime.TryParse(request.TglAwal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1) ? (DateTime?)d1 : null;
        var tglAkhir = DateTime.TryParse(request.TglAkhir, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2) ? (DateTime?)d2 : null;

        var filter = new OpCaseAktifFilter(
            OpCaseState: opCaseState.HasValue ? (int)opCaseState.Value : null,
            TglAwal: tglAwal,
            TglAkhir: tglAkhir,
            Limit: request.Limit);

        var listActiveOrderOp = _opCaseRepo.ListActiveOpCase(filter)?.ToList() ?? [];

        var result = listActiveOrderOp.Select(x =>
        {
            var klasifikasiStr = Enum.IsDefined(typeof(KlasifikasiOperasiEnum), x.KlasifikasiOperasi)
                ? ((KlasifikasiOperasiEnum)x.KlasifikasiOperasi).ToString()
                : KlasifikasiOperasiEnum.Kecil.ToString();

            var urgencyStr = x.UrgencyLevel == UrgencyLevelEnum.Cito
                ? UrgencyLevelEnum.Cito.ToString()
                : UrgencyLevelEnum.Elective.ToString();

            return new OkListOperasiAktifResponse(
                x.OrderOpId,
                x.Pasien.PasienId,
                x.Pasien.PasienName,
                x.DokterPengorderId,
                x.DokterPengorderName,
                x.DokterOperatorId,
                x.DokterOperatorName,
                x.NamaOperasi,
                x.ProsedurTambahan,
                klasifikasiStr,
                x.PreferedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.EstimasiDurasi,
                urgencyStr,
                x.OpCaseState.ToString());
        }).ToList();

        return Task.FromResult(result.AsEnumerable());
    }

    private static OpCaseStateEnum? ResolveOpCaseState(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var trimmed = status.Trim();
        if (string.Equals(trimmed, "Requested", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
            return OpCaseStateEnum.Requested;

        if (string.Equals(trimmed, "Scheduled", StringComparison.OrdinalIgnoreCase) || trimmed == "2")
            return OpCaseStateEnum.Scheduled;

        if (Enum.TryParse<OpCaseStateEnum>(trimmed, true, out var parsedEnum))
            return parsedEnum;

        return null;
    }
}
