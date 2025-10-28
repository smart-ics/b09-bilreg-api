using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using CommunityToolkit.Diagnostics;
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
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";

    public PasienCreateHandler(IPasienRepo pasienRepo, IPasienFactory pasienFactory)
    {
        _pasienRepo = pasienRepo;
        _pasienFactory = pasienFactory;
    }

    public Task<PasienCreateResponse> Handle(PasienCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotEmpty(request.TglLahir);
        Guard.IsTrue(request.TglLahir.IsValidTgl(FORMAT_TGL_YMD));
        
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
        var pasien = _pasienFactory.CreateFromPerson(person, request.NickName, request.TempatLahir, golDarah, request.IbuKandung);

        //  WRITE
        var result = _pasienRepo.SaveChanges(pasien);
        return Task.FromResult(new PasienCreateResponse(result.Value.PasienId));
    }
}