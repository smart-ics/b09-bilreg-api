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

public record RekapCetakDkGetQuery(string RekapCetakDkId) : IRequest<RekapCetakDkGetResponse>, IRekapCetakDkKey;
public record RekapCetakDkGetResponse(string RekapCetakDkId, string RekapCetakDkName);

public class RekapCetakDkGetHandler : IRequestHandler<RekapCetakDkGetQuery, RekapCetakDkGetResponse>
{
    private readonly IRekapCetakDkDal _rekapCetakDkDal;

    public RekapCetakDkGetHandler(IRekapCetakDkDal rekapCetakDkDal)
    {
        _rekapCetakDkDal = rekapCetakDkDal;
    }

    public Task<RekapCetakDkGetResponse> Handle(RekapCetakDkGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var rekapCetakDk = _rekapCetakDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rekap Cetak DK with id {request.RekapCetakDkId} not found");

        // RESPONSE
        var response = new RekapCetakDkGetResponse(rekapCetakDk.RekapCetakDkId, rekapCetakDk.RekapCetakDkName);
        return Task.FromResult(response);
    }
}
public class RekapCetakDkGetHandlerTest
{
    private readonly Mock<IRekapCetakDkDal> _rekapCetakDkDal;
    private readonly RekapCetakDkGetHandler _sut;

    public RekapCetakDkGetHandlerTest()
    {
        _rekapCetakDkDal = new Mock<IRekapCetakDkDal>();
        _sut = new RekapCetakDkGetHandler(_rekapCetakDkDal.Object);
    }

    [Fact]
    public async Task GivenInvalidRekapCetakDkId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RekapCetakDkGetQuery("XX");
        _rekapCetakDkDal.Setup(x => x.GetData(It.IsAny<IRekapCetakDkKey>()))
            .Returns(null as RekapCetakDkModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRekapCetakDkId_ThenReturnExpected_Test()
    {
        var request = new RekapCetakDkGetQuery("AD");
        var expected = new RekapCetakDkModel("AD", "Administrasi");
        var expectedResponse =
            new RekapCetakDkGetResponse(expected.RekapCetakDkId, expected.RekapCetakDkName);
        _rekapCetakDkDal.Setup(x => x.GetData(It.IsAny<IRekapCetakDkKey>()))
            .Returns(expected);

        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}

