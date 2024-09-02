using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanListQuery() : IRequest<IEnumerable<RujukanListResponse>>;
public record RujukanListResponse(
    string RujukanId,
    string RujukanName,
    string Alamat,
    string Alamat2,
    string Kota,
    bool IsAktif,
    string CaraMasukDkId,
    string CaraMasukDkName,
    string TipeRujukanId,
    string TipeRujukanName,
    string KelasRujukanId,
    string KelasRujukanName
);

public class RujukanListHandler : IRequestHandler<RujukanListQuery, IEnumerable<RujukanListResponse>>
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanListHandler(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public Task<IEnumerable<RujukanListResponse>> Handle(RujukanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var listRujukan = _rujukanDal.ListData()
            ?? throw new KeyNotFoundException("Rujukan not found");

        // RESPONSE
        var response = listRujukan.Select(x
            => new RujukanListResponse(
                x.RujukanId,
                x.RujukanName,
                x.Alamat,
                x.Alamat2,
                x.Kota,
                x.IsAktif,
                x.CaraMasukDkId,
                x.CaraMasukDkName,
                x.TipeRujukanId,
                x.TipeRujukanName,
                x.KelasRujukanId,
                x.KelasRujukanName
            ));
        return Task.FromResult(response);
    }
}

public class RujukanListHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly RujukanListHandler _sut;

    public RujukanListHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _sut = new RujukanListHandler(_rujukanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanListQuery();
        _rujukanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<RujukanModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}
