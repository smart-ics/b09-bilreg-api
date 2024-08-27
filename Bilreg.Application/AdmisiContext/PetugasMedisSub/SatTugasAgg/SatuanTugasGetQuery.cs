using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg;
public record SatuanTugasGetQuery(string SatuanTugasId) : IRequest<SatuanTugasGetResponse>, ISatuanTugasKey;

public record SatuanTugasGetResponse(string SatuanTugasId, string SatuanTugasName, bool IsMedis);

public class SatuanTugasGetHandler : IRequestHandler<SatuanTugasGetQuery, SatuanTugasGetResponse>
{
    private readonly ISatuanTugasDal _satuanTugasDal;

    public SatuanTugasGetHandler(ISatuanTugasDal satuanTugasDal)
    {
        _satuanTugasDal = satuanTugasDal;
    }

    public Task<SatuanTugasGetResponse> Handle(SatuanTugasGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _satuanTugasDal.GetData(request)
            ?? throw new KeyNotFoundException($"SatuanTugas not found: {request.SatuanTugasId}");

        // RESPONSE
        var response = new SatuanTugasGetResponse(result.SatuanTugasId, result.SatuanTugasName, result.IsMedis);
        return Task.FromResult(response);
    }
}
public class SatuanTugasGetHandlerTest
{
    private readonly SatuanTugasGetHandler _sut;
    private readonly Mock<ISatuanTugasDal> _satuanTugasDal;

    public SatuanTugasGetHandlerTest()
    {
        _satuanTugasDal = new Mock<ISatuanTugasDal>();
        _sut = new SatuanTugasGetHandler(_satuanTugasDal.Object);
    }

    [Fact]
    public void GivenInvalidSatuanTugasId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new SatuanTugasGetQuery("123");
        _satuanTugasDal.Setup(x => x.GetData(It.IsAny<ISatuanTugasKey>()))
            .Returns(null as SatuanTugasModel);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidSatuanTugasId_ThenReturnExpected()
    {
        // ARRANGE
        var expected = SatuanTugasModel.Create("A", "B", true);
        var request = new SatuanTugasGetQuery("A");
        _satuanTugasDal.Setup(x => x.GetData(It.IsAny<ISatuanTugasKey>()))
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(new SatuanTugasGetResponse(expected.SatuanTugasId, expected.SatuanTugasName, expected.IsMedis));
    }
}

