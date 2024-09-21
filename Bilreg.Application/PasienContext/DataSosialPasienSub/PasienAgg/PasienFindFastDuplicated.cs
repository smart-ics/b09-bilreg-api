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
    
    public async Task<IEnumerable<PasienFindFastDuplicatedResponse>> Handle(PasienFindFastDuplicated request, CancellationToken cancellationToken)
    {
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id :{request.PasienId} not found");
        
        var listPasien = _pasienDal.ListData(pasien.TglLahir)?.ToList()
            ?? throw new KeyNotFoundException($"Data Pasien not Found");
        
        var resultJw = FindSimiliarity(listPasien, pasien.PasienName);
        var resultEjaan = FindEjaanLamaBaru(listPasien, pasien.PasienName);

        var result = resultJw.Union(resultEjaan);
        
        var response = result.Select(BuildPasienResponse);
        return await Task.FromResult(response);
    }

    private static IEnumerable<PasienModel> FindEjaanLamaBaru(
        IEnumerable<PasienModel> listPasien, string pasienPasienName)
    {
        var spellingVariations = new Dictionary<string, string>
        {
            { "Dj", "J"}, 
            { "Tj", "C"}, 
            { "Sj", "Sy"},
            { "Oe", "U"},
            { "Dh", "D"},
            { "J" , "Y"}
        };
        var possibleNameVariants = new List<string> { pasienPasienName };
        possibleNameVariants.AddRange(
            from entry in spellingVariations 
            where pasienPasienName.Contains(entry.Key) 
            select pasienPasienName.Replace(entry.Key, entry.Value));

        var result = listPasien.Where(pasien =>
            possibleNameVariants.Any(
                variant => pasien.PasienName.Equals(variant, StringComparison.OrdinalIgnoreCase
                )));
        return result;
        
    }

    private static IEnumerable<PasienModel> FindSimiliarity(
        IEnumerable<PasienModel> listPasien, string name)
    {
        //  hitung nilai Jaro Winkler Value
        var listPasienJwValue = listPasien.Select(x => new
        {
            Pasien = x,
            JaroWinklerValue = x.PasienName.Similiarity(name)
        });
        //  filter yang lebih dari 0.75
        var result = listPasienJwValue
            .Where(x => x.JaroWinklerValue >= 0.75)
            .Select(x => x.Pasien);
        return result;
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


