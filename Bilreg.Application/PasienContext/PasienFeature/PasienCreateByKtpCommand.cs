using Ardalis.GuardClauses;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienCreateByKtpCommand(string PasienName, string Nik,
    string AlamatKtp, string GolDarah,
    string Gender, string TempatLahir, string TglLahir, string Agama, string StatusKawin,
    string KotaKtp, string KodePosKtp, string Rt, string Rw, string KelurahanKtpId) 
    : IRequest<PasienCreateByKtpResponse>;
public record PasienCreateByKtpResponse(string PasienId);

public class PasienCreateByKtpHandler : IRequestHandler<PasienCreateByKtpCommand, PasienCreateByKtpResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelurahanRepo _kelurahanRepo;
    private readonly IPasienFactory _pasienFactory;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    public PasienCreateByKtpHandler(IPasienRepo pasienRepo,
        IPasienFactory pasienFactory,
        IKelurahanRepo kelurahanRepo)
    {
        _pasienRepo = pasienRepo;
        _pasienFactory = pasienFactory;
        _kelurahanRepo = kelurahanRepo;
    }

    public Task<PasienCreateByKtpResponse> Handle(PasienCreateByKtpCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NullOrEmpty(request.TglLahir);

        // BUILD
        //      PERSON
        var tglLahir = DateOnly.Parse(request.TglLahir);
        var alamat = new AlamatType(
            [request.AlamatKtp],
            request.KotaKtp, request.KodePosKtp);
        var identitasKtp = IdentitasType.Ktp(request.Nik);
        var person = new PersonInfoType(request.PasienName, tglLahir, request.Gender,
            alamat, ContactType.Default, IdentitasType.Default);
        var golDarah = new GolDarahType(request.GolDarah);
        
        var pasien = _pasienFactory.CreateFromPerson(person, request.PasienName, 
            request.TempatLahir, golDarah, "-");

        //      KTP
        var kelurahan = _kelurahanRepo.LoadEntity(KelurahanType.Key(request.KelurahanKtpId))
            .Match(
                onSome: x => x,
                onNone: () => throw new ArgumentException("Invalid Kelurahan KTP"));
        var ktp = new KtpType(request.Nik, alamat, request.Rt, request.Rw, kelurahan);
        pasien.SetDataKtp(ktp);
        pasien.UpdateAdminInfo(kelurahan, IdentitasType.Default, ContactType.Default, 
            ContactType.Default, PasienKeluargaType.Default);

        //  WRITE
        var result = _pasienRepo.SaveChanges(pasien);
        return Task.FromResult(new PasienCreateByKtpResponse(result.Value.PasienId));
    }
}