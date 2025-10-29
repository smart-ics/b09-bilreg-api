using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public record PasienCreateCommand(
    string PasienName,
    string TempatLahir,
    string TglLahir,
    string NickName,
    string Gender,
    string IbuKandung,
    string GolDarah) : IRequest<PasienCreateResponse>;

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
        var person = new PersonInfoType(request.PasienName, tglLahir, request.Gender,
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var pasien = _pasienFactory.CreateFromPerson(person);
        // TODO: set semua property di awal
        
        //  WRITE
        var result = _pasienRepo.SaveChanges(pasien);
        return Task.FromResult(new PasienCreateResponse(result.Value.PasienId));
    }
}