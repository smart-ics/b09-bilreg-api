using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisListQuery(): IRequest<IEnumerable<KarcisListResponse>>;

public record KarcisListResponse(
    string KarcisId,
    string KarcisName,
    decimal Nilai,
    string InstalasiDkId,
    string InstalasiDkName,
    string RekapCetakId,
    string RekapCetakName,
    string TarifId,
    string TarifName,
    bool IsAktif
);

public class KarcisListHandler: IRequestHandler<KarcisListQuery, IEnumerable<KarcisListResponse>>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisDal _karcisDal;
    
    public KarcisListHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, IKarcisDal karcisDal)
    {
        _factory = factory;
        _karcisDal = karcisDal;
    }

    public Task<IEnumerable<KarcisListResponse>> Handle(KarcisListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var karcisList = _karcisDal.ListData()
            ?? throw new KeyNotFoundException("Karcis not found");

        // RESPONSE
        var response = karcisList.Select(BuildKarcisListItem);
        return Task.FromResult(response);
    }

    private KarcisListResponse BuildKarcisListItem(IKarcisKey key)
    {
        var karcis = _factory.Load(key);
        return new KarcisListResponse(
            karcis.KarcisId,
            karcis.KarcisName,
            karcis.Nilai,
            karcis.InstalasiDkId,
            karcis.InstalasiDkName,
            karcis.RekapCetakId,
            karcis.RekapCetakName,
            karcis.TarifId,
            karcis.TarifName,
            karcis.IsAktif
        );
    }
}

public class KarcisListHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKarcisDal> _karcisDal;
    private readonly KarcisListHandler _sut;

    public KarcisListHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _karcisDal = new Mock<IKarcisDal>();
        _sut = new KarcisListHandler(_factory.Object, _karcisDal.Object);
    }

    [Fact]
    public async Task GivenNoData_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisListQuery();
        _karcisDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<KarcisModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}