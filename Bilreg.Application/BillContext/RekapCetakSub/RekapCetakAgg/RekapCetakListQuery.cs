using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public record RekapCetakListQuery(string RekapCetakId) : IRequest<IEnumerable<RekapCetakListResponse>>;
public record RekapCetakListResponse(
    string rekapCetakId,
    string RekapCetakName,
    int NoUrut,
    bool IsGrupBaru,
    int Level,
    string GrupRekapCetakId,
    string GrupRekapCetakName,
    string RekapCetakDkId,
    string RekapCetakDkName);

public class RekapCetakListHandler : IRequestHandler<RekapCetakListQuery, IEnumerable<RekapCetakListResponse>>
{
    private readonly IRekapCetakDal _rekapCetakDal;

    public RekapCetakListHandler(IRekapCetakDal rekapCetakDal)
    {
        _rekapCetakDal = rekapCetakDal;
    }

    public Task<IEnumerable<RekapCetakListResponse>> Handle(RekapCetakListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var rekapCetakList = _rekapCetakDal.ListData()
            ?? throw new KeyNotFoundException("Rekap Cetak not found");
        
        // RESPONSE
        var response = rekapCetakList.Select(x => new RekapCetakListResponse(
            x.RekapCetakId,
            x.RekapCetakName,
            x.NoUrut,
            x.IsGrupBaru,
            x.Level,
            x.GrupRekapCetakId,
            x.GrupRekapCetakName,
            x.RekapCetakDkId,
            x.RekapCetakDkName
            ));
        return Task.FromResult(response);
    }
}

public class RekapCetakListHandlerTest
{
    private readonly Mock<IRekapCetakDal> _rekapCetakDal;
    private readonly RekapCetakListHandler _sut;

    public RekapCetakListHandlerTest()
    {
        _rekapCetakDal = new Mock<IRekapCetakDal>();
        _sut = new RekapCetakListHandler(_rekapCetakDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new RekapCetakListQuery("");
        _rekapCetakDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<RekapCetakModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();

    }
    
}