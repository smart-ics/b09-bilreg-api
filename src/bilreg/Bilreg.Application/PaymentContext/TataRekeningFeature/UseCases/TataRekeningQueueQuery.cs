using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record TataRekeningQueueQuery(int? JenisReg = null) : IRequest<IEnumerable<TataRekeningQueueResponse>>;

public record TataRekeningQueueResponse(
    string RegId,
    string RegDate,
    string PasienId,
    string PasienName,
    string TglLahir,
    string Gender,
    string JenisReg,
    string JenisRegString,
    string LayananId,
    string LayananName,
    string DokterId,
    string DokterName,
    string TipeJaminanId,
    string TipeJaminanName,
    int StatusTataRekening);

public class TataRekeningQueueHandler : IRequestHandler<TataRekeningQueueQuery, IEnumerable<TataRekeningQueueResponse>>
{
    private readonly IBilrgTataRekeningDal _tataRekeningDal;

    public TataRekeningQueueHandler(IBilrgTataRekeningDal tataRekeningDal)
    {
        _tataRekeningDal = tataRekeningDal;
    }

    public Task<IEnumerable<TataRekeningQueueResponse>> Handle(
        TataRekeningQueueQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = _tataRekeningDal.ListDataQueue(request.JenisReg) ?? [];
        var response = dtos.Select(MapToResponse).ToList();
        return Task.FromResult<IEnumerable<TataRekeningQueueResponse>>(response);
    }

    private static TataRekeningQueueResponse MapToResponse(TataRekeningQueueDto dto)
    {
        return new TataRekeningQueueResponse(
            RegId: dto.RegId,
            RegDate: dto.RegDate.ToString("yyyy-MM-dd"),
            PasienId: dto.PasienId,
            PasienName: dto.PasienName,
            TglLahir: FormatDate(dto.TglLahir),
            Gender: dto.Gender,
            JenisReg: dto.JenisReg,
            JenisRegString: ToJenisRegString(dto.JenisReg),
            LayananId: dto.LayananId,
            LayananName: dto.LayananName,
            DokterId: dto.DokterId,
            DokterName: dto.DokterName,
            TipeJaminanId: dto.TipeJaminanId,
            TipeJaminanName: dto.TipeJaminanName,
            StatusTataRekening: dto.StatusTataRekening);
    }

    private static string FormatDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return "";
        if (DateTime.TryParse(dateStr, out var dt)) return dt.ToString("yyyy-MM-dd");
        return dateStr;
    }

    private static string ToJenisRegString(string? jenisReg) => jenisReg switch
    {
        "0" or nameof(JenisRegEnum.RegJalan) => "Rawat Jalan",
        "1" or nameof(JenisRegEnum.RegInap) => "Rawat Inap",
        "3" or nameof(JenisRegEnum.Darurat) => "Rawat Darurat",
        _ => "-"
    };
}
