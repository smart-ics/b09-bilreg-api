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
public record GrupRekapCetakGetQuery(string GrupRekapCetakId) : IRequest<GrupRekapCetakGetResponse>, IGrupRekapCetakKey;
public record GrupRekapCetakGetResponse(string GrupRekapCetakId, string GrupRekapCetakName);
public class GrupRekapCetakGetHandler : IRequestHandler<GrupRekapCetakGetQuery, GrupRekapCetakGetResponse>
{
    private readonly IGrupRekapCetakDal _grupRekapCetakDal;
    public GrupRekapCetakGetHandler(IGrupRekapCetakDal grupRekapCetakDal)
    {
        _grupRekapCetakDal = grupRekapCetakDal;
    }

    public Task<GrupRekapCetakGetResponse> Handle(GrupRekapCetakGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var grupRekapCetak = _grupRekapCetakDal.GetData(request)
            ?? throw new KeyNotFoundException($"grup rekap cetak with id {request.GrupRekapCetakId} not found");

        // RESPONSE
        var response = new GrupRekapCetakGetResponse(grupRekapCetak.GrupRekapCetakId, grupRekapCetak.GrupRekapCetakName);
        return Task.FromResult(response);
    }
}

public class GrupRekapCetakGetHandlerTest
{
    private readonly Mock<IGrupRekapCetakDal> _grupRekapCetakDal;
    private readonly GrupRekapCetakGetHandler _sut;

    public GrupRekapCetakGetHandlerTest()
    {
        _grupRekapCetakDal = new Mock<IGrupRekapCetakDal>();
        _sut = new GrupRekapCetakGetHandler(_grupRekapCetakDal.Object);
    }

    [Fact]
    public async Task GivenInvalidGrupRekapCetakId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new GrupRekapCetakGetQuery("InvalidId");
        _grupRekapCetakDal.Setup(x => x.GetData(It.IsAny<IGrupRekapCetakKey>()))
            .Returns(null as GrupRekapCetakModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidGrupRekapCetakId_ThenReturnExpected_Test()
    {
        var request = new GrupRekapCetakGetQuery("ValidId");
        var expected = new GrupRekapCetakModel("ValidId", "ValidName");
        var expectedResponse =
            new GrupRekapCetakGetResponse(expected.GrupRekapCetakId, expected.GrupRekapCetakName);
        _grupRekapCetakDal.Setup(x => x.GetData(It.IsAny<IGrupRekapCetakKey>()))
            .Returns(expected);

        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expectedResponse);
    }
}

