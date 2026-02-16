using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.UseCase;

public record RegGetReg(string RegId) : IRequest<RegGetRegResponse>, IRegKey;
public record RegGetRegResponse(string RegId, string RegDate, string RegDateOut,
    string PasienId, string PasienName, string TipeJaminanName,
    string LayananName, string KelasName, string JenisReg, string JenisRegString);

public class RegGetRegHandler : IRequestHandler<RegGetReg, RegGetRegResponse>
{
    private readonly IRegRepo _regRepo;
    public RegGetRegHandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }
    public Task<RegGetRegResponse> Handle(RegGetReg request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");

        var result = new RegGetRegResponse(
            reg.RegId,
            reg.RegMasukAudit.Timestamp.ToString(DateFormatEnum.YMD_HMS),
            reg.RegKeluarAudit.Timestamp.ToString(DateFormatEnum.YMD_HMS),
            reg.Pasien.PasienId,
            reg.Pasien.PasienName,
            reg.TipeJaminan.TipeJaminanName,
            reg.Layanan.LayananName,
            reg.Kelas.KelasName,
            ((int)reg.JenisReg).ToString(),
            reg.JenisReg.ToString()
            );

        return Task.FromResult(result);
    }
}
