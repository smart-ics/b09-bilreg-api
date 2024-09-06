using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;

public record RekapCetakDkListQuery : IRequest<IEnumerable<RekapCetakDkListResponse>>;
public record RekapCetakDkListResponse(string RekapCetakDkId, string RekapCetakDkName);
public class RekapCetakDkListHandler : IRequestHandler<RekapCetakDkListQuery, IEnumerable<RekapCetakDkListResponse>>
{
    private readonly IRekapCetakDkDal _rekapCetakDkDal;

    public RekapCetakDkListHandler(IRekapCetakDkDal rekapCetakDkDal)
    {
        _rekapCetakDkDal = rekapCetakDkDal;
    }

    public Task<IEnumerable<RekapCetakDkListResponse>> Handle(RekapCetakDkListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var rekapCetakDkList = _rekapCetakDkDal.ListData();

        // RESPONSE
        var response = rekapCetakDkList.Select(rekapCetakDk =>
            new RekapCetakDkListResponse(rekapCetakDk.RekapCetakDkId, rekapCetakDk.RekapCetakDkName));

        return Task.FromResult(response);
    }
}
public class RekapCetakDkListHandlerTest
{
    private readonly Mock<IRekapCetakDkDal> _rekapCetakDkDal;
    private readonly RekapCetakDkListHandler _sut;

    public RekapCetakDkListHandlerTest()
    {
        _rekapCetakDkDal = new Mock<IRekapCetakDkDal>();
        _sut = new RekapCetakDkListHandler(_rekapCetakDkDal.Object);
    }

    [Fact]
    public async Task GivenRequest_ThenReturnExpectedList_Test()
    {
        // ARRANGE
        var expectedList = new List<RekapCetakDkModel>
        {
            new RekapCetakDkModel("AD", "Administrasi"),
            new RekapCetakDkModel("JS", "Jasa")
        };

        var expectedResponseList = expectedList.Select(x =>
            new RekapCetakDkListResponse(x.RekapCetakDkId, x.RekapCetakDkName)).ToList();

        _rekapCetakDkDal.Setup(x => x.ListData()).Returns(expectedList);

        // ACT
        var actual = await _sut.Handle(new RekapCetakDkListQuery(), CancellationToken.None);

        // ASSERT
        actual.Should().BeEquivalentTo(expectedResponseList);
    }
}

