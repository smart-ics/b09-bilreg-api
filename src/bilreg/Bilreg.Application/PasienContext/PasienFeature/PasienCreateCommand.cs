using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienCreateCommand(
    string PasienName, string TempatLahir, string TglLahir,
    string Gender, string NickName, string IbuKandung, string GolDarah,
    //
    string Alamat1, string Alamat2, string Alamat3,
    string Kota, string KodePos,
    //
    string NoTelp, string NoKtp) : IRequest<PasienCreateResponse>;

public record PasienCreateResponse(string PasienId);

public class PasienCreateHandler : IRequestHandler<PasienCreateCommand, PasienCreateResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IPasienFactory _pasienFactory;
    private readonly ITglJamProvider _tglJamProvider;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";

    public PasienCreateHandler(IPasienRepo pasienRepo, IPasienFactory pasienFactory,
        ITglJamProvider tglJamProvider)
    {
        _pasienRepo = pasienRepo;
        _pasienFactory = pasienFactory;
        _tglJamProvider = tglJamProvider;
    }

    public Task<PasienCreateResponse> Handle(PasienCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NullOrEmpty(request.PasienName);
        Guard.Against.NullOrEmpty(request.Alamat1);
        Guard.Against.NullOrEmpty(request.TglLahir);
        Guard.Against.InvalidDateFormat(request.TglLahir, nameof(request.TglLahir));
        Guard.Against.NullOrEmpty(request.NoTelp);
        Guard.Against.NullOrWhiteSpace(request.IbuKandung);
        GuardNoKtp(request.NoKtp);
        GuardAgainstDuplicate(request);

        //  BUILD
        var tglLahir = DateOnly.Parse(request.TglLahir);
        var alamat = new AlamatType(
            [request.Alamat1, request.Alamat2, request.Alamat3],
            request.Kota, request.KodePos);
        var contactPhone = new ContactType(JenisContactEnum.Phone, request.NoTelp);
        var identitasKtp = IdentitasType.Ktp(request.NoKtp);
        var person = new PersonInfoType(request.PasienName, tglLahir, request.Gender,
            alamat, contactPhone, IdentitasType.Default);
        var golDarah = new GolDarahType(request.GolDarah);
        var pasien = _pasienFactory.CreateFromPerson(person, request.NickName, request.TempatLahir,
            golDarah, request.IbuKandung, _tglJamProvider.Now);

        //  WRITE
        var result = _pasienRepo.SaveChanges(pasien);
        return Task.FromResult(new PasienCreateResponse(result.Value.PasienId));
    }
    private void GuardAgainstDuplicate(PasienCreateCommand request)
    {
        if (_pasienRepo.GetDataByNik(request.NoKtp).HasValue)
            throw new InvalidOperationException("Patient with the supplied NIK already exists.");

        var demographicMatches = _pasienRepo.SearchPasien(
                $"{request.PasienName} {request.TglLahir}")
            .Where(x =>
                string.Equals(
                    x.Person.PersonName.Trim(),
                    request.PasienName.Trim(),
                    StringComparison.OrdinalIgnoreCase)
                && x.Person.TglLahir == DateOnly.Parse(request.TglLahir)
                && string.Equals(
                    NormalizePhone(x.Person.Contact.ContactDetail),
                    NormalizePhone(request.NoTelp),
                    StringComparison.Ordinal))
            .Take(1);
        if (demographicMatches.Any())
            throw new InvalidOperationException("Patient with matching identity already exists.");
    }

    private static string NormalizePhone(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());

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
