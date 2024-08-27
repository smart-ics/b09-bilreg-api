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
public record SatuanTugasListQuery() : IRequest<IEnumerable<SatuanTugasListResponse>>;

public record SatuanTugasListResponse(string SatuanTugasId, string SatuanTugasName, bool IsMedis);

public class SatuanTugasListHandler : IRequestHandler<SatuanTugasListQuery, IEnumerable<SatuanTugasListResponse>>
{
    private readonly ISatuanTugasDal _satuanTugasDal;

    public SatuanTugasListHandler(ISatuanTugasDal satuanTugasDal)
    {
        _satuanTugasDal = satuanTugasDal;
    }

    public Task<IEnumerable<SatuanTugasListResponse>> Handle(SatuanTugasListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _satuanTugasDal.ListData()
            ?? throw new KeyNotFoundException("SatuanTugas not found");

        // RESPONSE
        var response = result.Select(x => new SatuanTugasListResponse(x.SatuanTugasId, x.SatuanTugasName, x.IsMedis));
        return Task.FromResult(response);
    }
}
public class SatuanTugasListHandlerTest
{
    private readonly SatuanTugasListHandler _sut;
    private readonly Mock<ISatuanTugasDal> _satuanTugasDal;

    public SatuanTugasListHandlerTest()
    {
        _satuanTugasDal = new Mock<ISatuanTugasDal>();
        _sut = new SatuanTugasListHandler(_satuanTugasDal.Object);
    }

    [Fact]
    public void GivenNoData_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new SatuanTugasListQuery();
        _satuanTugasDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<SatuanTugasModel>);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        // ARRANGE
        var expected = new List<SatuanTugasModel> { SatuanTugasModel.Create("A", "B", true) };
        var request = new SatuanTugasListQuery();
        _satuanTugasDal.Setup(x => x.ListData())
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new SatuanTugasListResponse(x.SatuanTugasId, x.SatuanTugasName, x.IsMedis)));
    }
}

