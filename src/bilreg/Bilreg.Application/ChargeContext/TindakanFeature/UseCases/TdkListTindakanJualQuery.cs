using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TdkListTindakanJualQuery(string RegId) : IRequest<IEnumerable<TdkListTindakanJualResponse>>, IRegKey;
public record TdkListTindakanJualResponse(string TransaksiId, string TransaksiDate, 
    RegReff Reg, LayananReff Layanan, TdkListTdkJualDesc Diskripsi, decimal TotalNilai);

public record TdkListTdkJualDesc(string DiskripsiId, string DiskripsiName, string Tipe);

public class TdkListTindakanJualHandler : IRequestHandler<TdkListTindakanJualQuery, IEnumerable<TdkListTindakanJualResponse>>
{
    private readonly ITindakanRepo _tdkRepo;

    public TdkListTindakanJualHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
    }

    public Task<IEnumerable<TdkListTindakanJualResponse>> Handle(TdkListTindakanJualQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var listTdkJual = _tdkRepo.ListDataTdkJual(request)?.ToList() ?? [];
        var result = listTdkJual.Select(x => new TdkListTindakanJualResponse(
            x.TransaksiId, x.TransaksiDate.ToString("yyyy-MM-dd HH:mm:ss"),
            x.Reg, x.Layanan, new TdkListTdkJualDesc(x.DiskripsiId, x.DiskripsiName, x.Tipe), x.Total));

        return Task.FromResult(result);
    }
}
