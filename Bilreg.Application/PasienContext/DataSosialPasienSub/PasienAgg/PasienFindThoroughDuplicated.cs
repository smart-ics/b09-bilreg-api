using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienFindThoroughDuplicated(string PasienId) : IRequest<IEnumerable<PasienFindThoroughDuplicatedResponse>>, IPasienKey;

public record PasienFindThoroughDuplicatedResponse(
    string PasienId,
    string PasienName,
    string TglLahir,
    string TglMedrec
);

public class PasienFindThoroughDuplicatedHandler : IRequestHandler<PasienFindThoroughDuplicated,
    IEnumerable<PasienFindThoroughDuplicatedResponse>>
{
    private IPasienDal _pasienDal;

    public PasienFindThoroughDuplicatedHandler(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;
    }

    public Task<IEnumerable<PasienFindThoroughDuplicatedResponse>> Handle(PasienFindThoroughDuplicated request, CancellationToken cancellationToken)
    {
        var pasien = _pasienDal.GetData(request)
                     ?? throw new KeyNotFoundException($"Pasien id :{request.PasienId} not found");
        
        var result = _pasienDal.ListData(pasien.PasienName)
                     ?? throw new KeyNotFoundException($"Data Pasien not Found");
        // Belum implementasi library Similiarity
        var response = result.Select(BuildPasienResponse);
        return Task.FromResult(response);
    }
    private PasienFindThoroughDuplicatedResponse BuildPasienResponse(PasienModel pasien)
    {
        return new PasienFindThoroughDuplicatedResponse(
            pasien.PasienId,
            pasien.PasienName,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.TglMedrec.ToString(DateFormatEnum.YMD)
        );
    }
}


