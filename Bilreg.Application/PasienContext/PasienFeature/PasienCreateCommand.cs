using Bilreg.Domain.PasienContext.PasienFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.PatternHelper;
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
    private readonly IGenderDal _genderDal;
    private readonly IPasienRepo _pasienRepo;
    
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    

    public PasienCreateHandler(IGenderDal genderDal, IPasienRepo pasienRepo)
    {
        _genderDal = genderDal;
        _pasienRepo = pasienRepo;
    }

    public Task<PasienCreateResponse> Handle(PasienCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotEmpty(request.TglLahir);
        Guard.IsTrue(request.TglLahir.IsValidTgl(FORMAT_TGL_YMD));
        
        //  BUILD
        var gender = _genderDal.GetData(request.Gender)
            .GetValueOrThrow("Gender Invalid");
        var pasien = PasienModel.CreateNew(request.PasienName,
            request.TglLahir.ToDate(), gender);
        pasien.SetPersonalInfo(request.NickName, request.TempatLahir, 
            request.IbuKandung, new GolDarahType(request.GolDarah));

        //  WRITE
        var result = _pasienRepo.SaveChanges(pasien);
        return Task.FromResult(new PasienCreateResponse(result.Value.PasienId));
    }
}

