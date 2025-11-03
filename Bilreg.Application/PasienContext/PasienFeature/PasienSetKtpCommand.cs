using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetKtpCommand(string PasienId, string PasienName, string Nik, 
    string AlamtaKtp1, string AlamatKtp2, string AlamatKtp3, string GolDarah,
    string Gender, string TempatLahir, string TglLahir, string Agama, string StatusKawin,
    string KotaKtp, string KodePosKtp, string Rt, string Rw, string KelurahanKtpId,
    bool IsForceUpdate) : IRequest<PasienSetKtpResponse>, IPasienKey;

public record PasienSetKtpResponse(
    string PasienId,
    bool IsDifferent,
    bool IsUpdated,
    PasienSetKtpResponsePerson Original,
    PasienSetKtpResponsePerson Ktp
);

public record PasienSetKtpResponsePerson(
    string PasienName, string Gender, string TglLahir, string GolDarah,
    string TempatLahir);

public class PasienSetKtpHandler : IRequestHandler<PasienSetKtpCommand, PasienSetKtpResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelurahanRepo _kelurahanRepo;
    public PasienSetKtpHandler(IPasienRepo pasienRepo, 
        IKelurahanRepo kelurahanRepo)
    {
        _pasienRepo = pasienRepo;
        _kelurahanRepo = kelurahanRepo;
    }

    public Task<PasienSetKtpResponse> Handle(PasienSetKtpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanKtpId))
            .Match(
                onSome: x => x,
                onNone: () => throw new ArgumentException("Invalid Kelurahan KTP")); 
        
        //  BUILD
        if (request.IsForceUpdate)
            pasien.SyncFromKtp(request.PasienName, DateOnly.Parse(request.TglLahir),
                request.TempatLahir, request.Gender, request.GolDarah);
        var oriPerson = new PasienSetKtpResponsePerson(
            pasien.Person.PersonName, pasien.Person.Gender, pasien.Person.TglLahir.ToString("yyyy-MM-dd"),
            pasien.GolDarah.ToString(), pasien.TempatLahir);
        var ktpPerson = new PasienSetKtpResponsePerson(
            request.PasienName, request.Gender, request.TglLahir,
            request.GolDarah, request.TempatLahir);
        var isDifferent = oriPerson != ktpPerson;

        var alamat = new AlamatType([request.AlamtaKtp1, request.AlamatKtp2, request.AlamatKtp3], 
            request.KotaKtp, request.KodePosKtp);
        var ktp = new KtpType(request.Nik, alamat, request.Rt, request.Rw, kelurahan);
        pasien.SetDataKtp(ktp);

        //  WRITE
        _pasienRepo.SaveChanges(pasien);
        var result = new PasienSetKtpResponse(
            pasien.PasienId, isDifferent, request.IsForceUpdate,
            oriPerson, ktpPerson);
        return Task.FromResult(result);
    }
}
