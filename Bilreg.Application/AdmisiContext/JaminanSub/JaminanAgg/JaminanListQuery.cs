using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanListQuery() : IRequest<IEnumerable<JaminanListResponse>>;

public record JaminanListResponse(
    string JaminanId,
    string JaminanName,
    string CaraBayarDkName,
    string GrupJaminanName);

public class JaminanListHandler : IRequestHandler<JaminanListQuery, IEnumerable<JaminanListResponse>>
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanListHandler(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public Task<IEnumerable<JaminanListResponse>> Handle(JaminanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var listJaminan = _jaminanDal
            .ListData2()
            .Value;

        // RESPONSE
        var response = listJaminan
            .Select(x => new JaminanListResponse(
                x.JaminanId, x.JaminanName, 
                x.CaraBayarDk.CaraBayarDkName, 
                x.GrupJaminan.GrupJaminanName));
        return Task.FromResult(response);
    }
}