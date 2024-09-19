using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienListQuery(string TglMedrec) : IRequest<IEnumerable<PasienListResponse>>;
public record PasienListResponse(
    string PasienId,
    string PasienName,
    string NickName,
    string TempatLahir,
    string TglLahir,
    string Gender,
    string TglMedrec,
    string IbuKandung,
    string GolDarah
    );

public class PasienListHandler : IRequestHandler<PasienListQuery, IEnumerable<PasienListResponse>>
{
    private IPasienDal _pasienDal;

    public PasienListHandler(IPasienDal pasienDal)
    {
        _pasienDal = pasienDal;
    }

    public Task<IEnumerable<PasienListResponse>> Handle(PasienListQuery request, CancellationToken cancellationToken)
    {
        var tglMedRec = DateTime.Parse(request.TglMedrec);
        var periode = new Periode(tglMedRec);
        var result = _pasienDal.ListData(periode)
            ?? throw new KeyNotFoundException($"Data Pasien not Found");
        
        var response = result.Select(BuildPasienResponse);
        return Task.FromResult(response);
    }
    private PasienListResponse BuildPasienResponse(PasienModel pasien)
    {
        return new PasienListResponse(
            pasien.PasienId,
            pasien.PasienName,
            pasien.NickName,
            pasien.TempatLahir,
            pasien.TglLahir.ToString(DateFormatEnum.YMD),
            pasien.Gender,
            pasien.TglMedrec.ToString(DateFormatEnum.YMD),
            pasien.IbuKandung,
            pasien.GolDarah
            
        );
    }
}
    