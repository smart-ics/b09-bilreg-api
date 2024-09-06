using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public record GrupRekapCetakListQuery() : IRequest<IEnumerable<GrupRekapCetakListResponse>>;
public record GrupRekapCetakListResponse(string GrupRekapCetakId, string GrupRekapCetakName);
public class GrupRekapCetakListHandler : IRequestHandler<GrupRekapCetakListQuery, IEnumerable<GrupRekapCetakListResponse>>
{
    private readonly IGrupRekapCetakDal _grupRekapCetakDal;
    public GrupRekapCetakListHandler(IGrupRekapCetakDal grupRekapCetakDal)
    {
        _grupRekapCetakDal = grupRekapCetakDal;
    }

    public Task<IEnumerable<GrupRekapCetakListResponse>> Handle(GrupRekapCetakListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var grupRekapCetakanList = _grupRekapCetakDal.ListData()
            ?? throw new KeyNotFoundException("grup rekap cetak not found");

        // RESPONSE
        var response = grupRekapCetakanList.Select(x => new GrupRekapCetakListResponse(x.GrupRekapCetakId, x.GrupRekapCetakName));
        return Task.FromResult(response);
    }
}

public class GrupRekapCetakListHandlerTest
{
    private readonly Mock<IGrupRekapCetakDal> _grupRekapCetakDal;
    private readonly GrupRekapCetakListHandler _sut;

    public GrupRekapCetakListHandlerTest()
    {
        _grupRekapCetakDal = new Mock<IGrupRekapCetakDal>();
        _sut = new GrupRekapCetakListHandler(_grupRekapCetakDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
    {
        var request = new GrupRekapCetakListQuery();
        _grupRekapCetakDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<GrupRekapCetakModel>);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected_Test()
    {
        var request = new GrupRekapCetakListQuery();
        var expected = new GrupRekapCetakModel ("A", "B");
        var grupRekapCetakanList = new List<GrupRekapCetakModel>() { expected };
        _grupRekapCetakDal.Setup(x => x.ListData())
            .Returns(grupRekapCetakanList);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
}