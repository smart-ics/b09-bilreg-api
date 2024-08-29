using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;

public record KelasRujukanListQuery() : IRequest<IEnumerable<KelasRujukanListResponse>>;

public record KelasRujukanListResponse(string KelasRujukanId, string KelasRujukanName, decimal Nilai);

public class KelasRujukanListHandler : IRequestHandler<KelasRujukanListQuery, IEnumerable<KelasRujukanListResponse>>
{
    private readonly IKelasRujukanDal _kelasRujukanDal;

    public KelasRujukanListHandler(IKelasRujukanDal kelasRujukanDal)
    {
        _kelasRujukanDal = kelasRujukanDal;
    }

    public Task<IEnumerable<KelasRujukanListResponse>> Handle(KelasRujukanListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelasRujukanDal.ListData() ?? throw new KeyNotFoundException("Kelas Rujukan Not Found");

        // RESPONSE
        var response = result.Select(x => new KelasRujukanListResponse(x.KelasRujukanId, x.KelasRujukanName, x.Nilai));
        return Task.FromResult(response);
    }
}

public class KelasRujukanListHandlerTest
{
    private readonly KelasRujukanListHandler _sut;
    private readonly Mock<IKelasRujukanDal> _kelasRujukanDal;

    public KelasRujukanListHandlerTest()
    {
        _kelasRujukanDal = new Mock<IKelasRujukanDal>();
        _sut = new KelasRujukanListHandler(_kelasRujukanDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new KelasRujukanListQuery();
        _kelasRujukanDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<KelasRujukanModel>);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenReturnExpected()
    {
        // ARRANGE
        var expected = new List<KelasRujukanModel>
        {
            KelasRujukanModel.Create("1", "Tingkat I", 5),
            KelasRujukanModel.Create("2", "Tingkat II", 4)
        };
        var request = new KelasRujukanListQuery();
        _kelasRujukanDal.Setup(x => x.ListData())
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(expected.Select(x => new KelasRujukanListResponse(x.KelasRujukanId, x.KelasRujukanName, x.Nilai)));
    }
}
