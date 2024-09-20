using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public record RekapCetakGetQuery(string RekapCetakId) : IRequest<RekapCetakGetResponse>, IRekapCetakKey;

public record RekapCetakGetResponse(
    string rekapCetakId,
    string RekapCetakName,
    int NoUrut,
    bool IsGrupBaru,
    int Level,
    string GrupRekapCetakId,
    string GrupRekapCetakName,
    string RekapCetakDkId,
    string RekapCetakDkName);

public class RekapCetakGetHandler : IRequestHandler<RekapCetakGetQuery, RekapCetakGetResponse>
{
    private readonly IRekapCetakDal _rekapCetakDal;

    public RekapCetakGetHandler(IRekapCetakDal rekapCetakDal)
    {
        _rekapCetakDal = rekapCetakDal;
    }
    
    public Task<RekapCetakGetResponse> Handle(RekapCetakGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var rekapCetak = _rekapCetakDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rekap Cetak id {request.RekapCetakId} not found");
        
        // RESPONSE
        var response = new RekapCetakGetResponse(
            rekapCetak.RekapCetakId,
            rekapCetak.RekapCetakName,
            rekapCetak.NoUrut,
            rekapCetak.IsGrupBaru,
            rekapCetak.Level,
            rekapCetak.GrupRekapCetakId,
            rekapCetak.GrupRekapCetakName,
            rekapCetak.RekapCetakDkId,
            rekapCetak.RekapCetakDkName
            );
        
        return Task.FromResult(response);
    }
}

public class RekapCetakGetHandlerTest
{
    private readonly Mock<IRekapCetakDal> _rekapCetakDal;
    private readonly RekapCetakGetHandler _sut;

    public RekapCetakGetHandlerTest()
    {
        _rekapCetakDal = new Mock<IRekapCetakDal>();
        _sut = new RekapCetakGetHandler(_rekapCetakDal.Object);
    }

    [Fact]
    public async Task GivenInvalidRekapCetakId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RekapCetakGetQuery("A");
        _rekapCetakDal.Setup(x => x.GetData(It.IsAny<RekapCetakGetQuery>()))
            .Returns(null as RekapCetakModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
}
