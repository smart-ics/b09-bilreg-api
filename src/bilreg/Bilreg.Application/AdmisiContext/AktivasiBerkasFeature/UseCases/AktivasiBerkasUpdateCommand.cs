using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;

/// <summary>
/// Command MediatR untuk memperbarui transaksi aktivasi berkas yang sudah ada.
/// </summary>
public record AktivasiBerkasUpdateCommand(
    string Id,
    string TglJam,
    string ReqUserrId,
    string TglAktif,
    string AktifasiUserrId,
    string LayananId,
    IEnumerable<AktivasiBerkasRegisterRefDto> ListRegister
) : IRequest<AktivasiBerkasMutationResult>;

public class AktivasiBerkasUpdateHandler : IRequestHandler<AktivasiBerkasUpdateCommand, AktivasiBerkasMutationResult>
{
    private readonly IAktivasiBerkasEmrGateway _gateway;
    private readonly IRegRepo? _regRepo;

    public AktivasiBerkasUpdateHandler(IAktivasiBerkasEmrGateway gateway, IRegRepo? regRepo = null)
    {
        _gateway = gateway;
        _regRepo = regRepo;
    }

    public async Task<AktivasiBerkasMutationResult> Handle(
        AktivasiBerkasUpdateCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return AktivasiBerkasMutationResult.Fail("Data permintaan aktivasi berkas tidak boleh kosong.");

        if (string.IsNullOrWhiteSpace(request.Id))
            return AktivasiBerkasMutationResult.Fail("ID transaksi aktivasi berkas wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.TglAktif))
            return AktivasiBerkasMutationResult.Fail("Tanggal aktif aktivasi berkas wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.ReqUserrId))
            return AktivasiBerkasMutationResult.Fail("User pemohon (ReqUserrId) wajib diisi.");

        if (string.IsNullOrWhiteSpace(request.LayananId))
            return AktivasiBerkasMutationResult.Fail("Layanan tujuan aktivasi berkas wajib diisi.");

        var registerList = request.ListRegister?.ToList() ?? [];
        if (registerList.Count == 0)
            return AktivasiBerkasMutationResult.Fail("Daftar register aktivasi berkas tidak boleh kosong.");

        foreach (var item in registerList)
        {
            if (string.IsNullOrWhiteSpace(item.RegId))
                return AktivasiBerkasMutationResult.Fail("Nomor register tidak boleh kosong.");

            if (item.IsVoid)
                return AktivasiBerkasMutationResult.Fail($"Register {item.RegId} berstatus VOID dan tidak dapat diaktivasi.");

            if (_regRepo is not null)
            {
                var regEntity = _regRepo.LoadEntity(RegModel.Key(item.RegId));
                var isLocalVoid = regEntity.Match(
                    r => r.RegVoidAudit != AuditInfoType.Default &&
                         r.RegVoidAudit.Timestamp != new DateTime(3000, 1, 1) &&
                         r.RegVoidAudit.Timestamp != DateTime.MinValue,
                    () => false);

                if (isLocalVoid)
                    return AktivasiBerkasMutationResult.Fail($"Register {item.RegId} sudah dibatalkan (VOID) dan tidak dapat diaktivasi.");
            }
        }

        var updateDto = new AktivasiBerkasUpdateDto
        {
            TglJam = request.TglJam,
            ReqUserrId = request.ReqUserrId,
            TglAktif = request.TglAktif,
            AktifasiUserrId = request.AktifasiUserrId,
            LayananId = request.LayananId,
            ListRegister = registerList
        };

        return await _gateway.Update(request.Id, updateDto, cancellationToken);
    }
}
