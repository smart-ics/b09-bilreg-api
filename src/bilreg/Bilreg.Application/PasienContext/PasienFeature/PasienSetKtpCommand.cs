using Ardalis.GuardClauses;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetKtpCommand(string PasienId, string PasienName, string Nik, 
    string AlamatKtp, string GolDarah,
    string Gender, string TempatLahir, string TglLahir, string Agama, string StatusKawin,
    string Rt, string Rw, string KelurahanKtpId,
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
        GuardNoKtp(request.Nik);
        Guard.Against.NullOrWhiteSpace(request.AlamatKtp);
        Guard.Against.NullOrWhiteSpace(request.TglLahir);
        Guard.Against.InvalidDateFormat(request.TglLahir, nameof(request.TglLahir));
        Guard.Against.NullOrWhiteSpace(request.Gender);
        Guard.Against.NullOrWhiteSpace(request.StatusKawin);

        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanKtpId))
            .GetValueOrThrow($"Invalid Kelurahan KTP {request.KelurahanKtpId}");

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

        var alamatKtp = new AlamatType([request.AlamatKtp], "-", "-");
        var ktp = new KtpType(request.Nik, alamatKtp, request.Rt, request.Rw, kelurahan);
        pasien.SetDataKtp(ktp);

        //  WRITE
        _pasienRepo.SaveChanges(pasien);
        var result = new PasienSetKtpResponse(
            pasien.PasienId, isDifferent, request.IsForceUpdate,
            oriPerson, ktpPerson);
        return Task.FromResult(result);
    }

    private void GuardNoKtp(string noKtp)
    {
        if (string.IsNullOrWhiteSpace(noKtp) ||
            noKtp.Length != 16 ||
            !noKtp.All(char.IsDigit))
        {
            throw new ArgumentException("NoKtp harus 16 digit angka.");
        }
    }
}
