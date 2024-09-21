using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienFindFastDuplicated(string PasienId) : IRequest<IEnumerable<PasienFindFastDuplicatedResponse>>, IPasienKey;

public record PasienFindFastDuplicatedResponse(
    string PasienId,
    string PasienName,
    string TglLahir,
    string TglMedrec
);

public class PasienFindDuplicatedHandler : IRequestHandler<PasienFindFastDuplicated, IEnumerable<PasienFindFastDuplicatedResponse>>
{
    private IPasienDal _pasienDal;


    public PasienFindDuplicatedHandler(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;

    }
    
    public Task<IEnumerable<PasienFindFastDuplicatedResponse>> Handle(PasienFindFastDuplicated request, CancellationToken cancellationToken)
    {
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id :{request.PasienId} not found");
        
        var result = _pasienDal.ListData(pasien.TglLahir)
            ?? throw new KeyNotFoundException($"Data Pasien not Found");
        
        var response = result.Select(BuildPasienResponse);
        return Task.FromResult(response);
    }
    private PasienFindFastDuplicatedResponse BuildPasienResponse(PasienModel pasien)
    {
        // Return respon sementara
        return new PasienFindFastDuplicatedResponse(
            pasien.PasienId,
            pasien.PasienName,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.TglMedrec.ToString(DateFormatEnum.YMD)
        );
    }
}


