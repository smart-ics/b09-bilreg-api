using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;

public record KelasRujukanGetQuery(string KelasRujukanId) : IRequest<KelasRujukanGetResponse>, IKelasRujukanKey;
public record KelasRujukanGetResponse(string KelasRujukanId, string KelasRujukanName, decimal Nilai);
public class KelasRujukanGetHandler : IRequestHandler<KelasRujukanGetQuery, KelasRujukanGetResponse>
{
    private readonly IKelasRujukanDal _kelasRujukanDal;

    public KelasRujukanGetHandler(IKelasRujukanDal kelasRujukanDal)
    {
        _kelasRujukanDal = kelasRujukanDal;
    }

    public Task<KelasRujukanGetResponse> Handle(KelasRujukanGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var result = _kelasRujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"KelasRujukan not found: {request.KelasRujukanId}");

        // RESPONSE
        var response = new KelasRujukanGetResponse(result.KelasRujukanId, result.KelasRujukanName, result.Nilai);
        return Task.FromResult(response);
    }
}
public class KelasRujukanGetHandlerTest
{
    private readonly KelasRujukanGetHandler _sut;
    private readonly Mock<IKelasRujukanDal> _kelasRujukanDal;

    public KelasRujukanGetHandlerTest()
    {
        _kelasRujukanDal = new Mock<IKelasRujukanDal>();
        _sut = new KelasRujukanGetHandler(_kelasRujukanDal.Object);
    }

    [Fact]
    public void GivenInvalidKelasRujukanId_ThenThrowKeyNotFoundException()
    {
        // ARRANGE
        var request = new KelasRujukanGetQuery("123");
        _kelasRujukanDal.Setup(x => x.GetData(It.IsAny<IKelasRujukanKey>()))
            .Returns(null as KelasRujukanModel);

        // ACT
        Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidKelasRujukanId_ThenReturnExpected()
    {
        // ARRANGE
        var expected = KelasRujukanModel.Create("A", "Kelas A", 5m);
        var request = new KelasRujukanGetQuery("A");
        _kelasRujukanDal.Setup(x => x.GetData(It.IsAny<IKelasRujukanKey>()))
            .Returns(expected);

        // ACT
        var act = await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().BeEquivalentTo(new KelasRujukanGetResponse(expected.KelasRujukanId, expected.KelasRujukanName, expected.Nilai));
    }
}

