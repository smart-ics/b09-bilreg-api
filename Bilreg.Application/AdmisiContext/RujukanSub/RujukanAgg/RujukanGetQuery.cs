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
public record RujukanGetQuery(string RujukanId) : IRequest<RujukanGetResponse>, IRujukanKey;
public record RujukanGetResponse(
    string RujukanId,
    string RujukanName,
    bool IsAktif,
    string Alamat,
    string Alamat2,
    string Kota,
    string Telepon,
    string RujukanTipeId,
    string RujukanTipeName,
    string KelasId,
    string KelasName,
    string CaraMasukDkId,
    string CaraMasukDkName
);
public class RujukanGetHandler : IRequestHandler<RujukanGetQuery, RujukanGetResponse>
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanGetHandler(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public Task<RujukanGetResponse> Handle(RujukanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var rujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        // RESPONSE
        var response = new RujukanGetResponse(
            rujukan.RujukanId,
            rujukan.RujukanName,
            rujukan.IsAktif,
            rujukan.Alamat,
            rujukan.Alamat2,
            rujukan.Kota,
            rujukan.NoTelp,
            rujukan.TipeRujukanId,
            rujukan.TipeRujukanName,
            rujukan.KelasRujukanId,
            rujukan.KelasRujukanName,
            rujukan.CaraMasukDkId,
            rujukan.CaraMasukDkName
        );

        return Task.FromResult(response);
    }
}
public class RujukanGetHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly RujukanGetHandler _sut;

    public RujukanGetHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _sut = new RujukanGetHandler(_rujukanDal.Object);
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanGetQuery("A");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}

