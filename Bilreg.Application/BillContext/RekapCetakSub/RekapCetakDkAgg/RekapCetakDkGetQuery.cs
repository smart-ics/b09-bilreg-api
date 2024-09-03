using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;

public record RekapCetakDkGetQuery(string RekapCetakDkId): IRequest<RekapCetakDkGetResponse>, IRekapCetakDkKey;
public record RekapCetakDkGetResponse(string RekapCetakDkId, string RekapCetakDkName);
public class RekapCetakDkGetQueryHandler : IRequestHandler<RekapCetakDkGetQuery, RekapCetakDkGetResponse>
{
    private readonly IRekapCetakDkDal _rekapCetakDkDal;
    public RekapCetakDkGetQueryHandler(IRekapCetakDkDal rekapCetakDal)
    {
        _rekapCetakDkDal = rekapCetakDal;
    }
    public Task<RekapCetakDkGetResponse> Handle(RekapCetakDkGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _rekapCetakDkDal.GetData(request)??
        throw new NotImplementedException($"Rekap Cetak Not Found : {request.RekapCetakDkId}");

        // RESPONSE
        var response = new RekapCetakDkGetResponse(result.RekapCetakDkId, result.RekapCetakDkName);
        return Task.FromResult(response);
        
    }
}