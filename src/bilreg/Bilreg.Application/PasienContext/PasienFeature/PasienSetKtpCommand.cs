using Ardalis.GuardClauses;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienSetKtpCommand(string PasienId, string PasienName, string Nik, 
    string AlamatKtp, string GolDarah,
    string Gender, string TempatLahir, string TglLahir, string Agama, string StatusKawin,
    string Rt, string Rw, string KelurahanKtpId,
    bool IsForceUpdate) : IRequest<PasienSetKtpResponse>, IPasienKey
{
    public string UserId { get; init; } = string.Empty;
}

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
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider? _tglJamProvider;

    public PasienSetKtpHandler(IPasienRepo pasienRepo, 
        IKelurahanRepo kelurahanRepo,
        IAuditRepo auditRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _pasienRepo = pasienRepo;
        _kelurahanRepo = kelurahanRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<PasienSetKtpResponse> Handle(PasienSetKtpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
        Guard.Against.NullOrWhiteSpace(request.AlamatKtp);
        Guard.Against.NullOrWhiteSpace(request.TglLahir);
        Guard.Against.InvalidDateFormat(request.TglLahir, nameof(request.TglLahir));
        Guard.Against.NullOrWhiteSpace(request.Gender);
        Guard.Against.NullOrWhiteSpace(request.StatusKawin);

        var tglLahir = DateOnly.Parse(request.TglLahir);
        GuardNoKtp(request.Nik, tglLahir);

        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanKtpId))
            .GetValueOrThrow($"Invalid Kelurahan KTP {request.KelurahanKtpId}");

        var originalDataJson = AuditLogSnapshotJson.Serialize(pasien);

        //  BUILD
        if (request.IsForceUpdate)
            pasien.SyncFromKtp(request.PasienName, DateOnly.Parse(request.TglLahir),
                request.TempatLahir, request.Gender, request.GolDarah);

        var normalizedGolDarah = new GolDarahType(request.GolDarah ?? string.Empty).ToString();
        var oriPerson = new PasienSetKtpResponsePerson(
            pasien.Person.PersonName?.Trim() ?? string.Empty,
            pasien.Person.Gender?.Trim() ?? string.Empty,
            pasien.Person.TglLahir.ToString("yyyy-MM-dd"),
            pasien.GolDarah?.ToString().Trim() ?? string.Empty,
            pasien.TempatLahir?.Trim() ?? string.Empty);
        var ktpPerson = new PasienSetKtpResponsePerson(
            request.PasienName?.Trim() ?? string.Empty,
            request.Gender?.Trim() ?? string.Empty,
            request.TglLahir?.Trim() ?? string.Empty,
            normalizedGolDarah,
            request.TempatLahir?.Trim() ?? string.Empty);
        var isDifferent = oriPerson != ktpPerson;

        var nik = string.IsNullOrWhiteSpace(request.Nik) ? string.Empty : request.Nik.Trim();
        var alamatKtp = new AlamatType([request.AlamatKtp], "-", "-");
        var ktp = new KtpType(nik, alamatKtp, request.Rt, request.Rw, kelurahan);
        pasien.SetDataKtp(ktp);

        //  WRITE
        _pasienRepo.SaveChanges(pasien);
        var auditLog = AuditLog.Create(request.UserId, DateTime.Now, "Pasien.SetKtp", "Pasien", pasien.PasienId, null, originalDataJson);
        _auditRepo.SaveChanges(auditLog);

        var result = new PasienSetKtpResponse(
            pasien.PasienId, isDifferent, request.IsForceUpdate,
            oriPerson, ktpPerson);
        return Task.FromResult(result);
    }

    private void GuardNoKtp(string? noKtp, DateOnly tglLahir)
    {
        var now = _tglJamProvider?.Now ?? default;
        var today = (now == default || now == DateTime.MinValue)
            ? DateOnly.FromDateTime(DateTime.Today)
            : DateOnly.FromDateTime(now);

        var usia = CalculateAge(tglLahir, today);

        if (usia >= 17)
        {
            if (string.IsNullOrWhiteSpace(noKtp) ||
                noKtp.Length != 16 ||
                !noKtp.All(char.IsDigit))
            {
                throw new ArgumentException("NoKtp harus 16 digit angka.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(noKtp))
            {
                if (noKtp.Length != 16 || !noKtp.All(char.IsDigit))
                {
                    throw new ArgumentException("NoKtp harus 16 digit angka.");
                }
            }
        }
    }

    private static int CalculateAge(DateOnly birthDate, DateOnly referenceDate)
    {
        var age = referenceDate.Year - birthDate.Year;
        if (referenceDate < birthDate.AddYears(age))
            age--;
        return Math.Max(0, age);
    }
}
