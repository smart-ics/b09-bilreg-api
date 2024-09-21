using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisGetQuery(string KarcisId) : IRequest<KarcisGetResponse>, IKarcisKey;

public record KarcisKomponenResponse(
    string KomponenId,
    string KomponenName,
    decimal Nilai
);

public record KarcisLayananResponse(
    string LayananId,
    string LayananName
);

public record KarcisGetResponse(
    string KarcisId,
    string KarcisName,
    decimal Nilai,
    string InstalasiDkId,
    string InstalasiDkName,
    string RekapCetakId,
    string RekapCetakName,
    string TarifId,
    string TarifName,
    bool IsAktif,
    IEnumerable<KarcisKomponenResponse> ListKomponen,
    IEnumerable<KarcisLayananResponse> ListLayanan
);

public class KarcisGetHandler : IRequestHandler<KarcisGetQuery, KarcisGetResponse>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;

    public KarcisGetHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory)
    {
        _factory = factory;
    }

    public Task<KarcisGetResponse> Handle(KarcisGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var karcis = _factory.Load(request);

        // RESPONSE
        var listKomponen = karcis.ListKomponen.Select(x=>
            new KarcisKomponenResponse(
                x.KomponenId,
                x.KomponenName,
                x.Nilai
            ));

        var listLayanan = karcis.ListLayanan.Select(x => 
            new KarcisLayananResponse(
                x.LayananId, x.LayananName
            ));

        var response = new KarcisGetResponse(
            karcis.KarcisId,
            karcis.KarcisName,
            karcis.Nilai,
            karcis.InstalasiDkId,
            karcis.InstalasiDkName,
            karcis.RekapCetakId,
            karcis.RekapCetakName,
            karcis.TarifId,
            karcis.TarifName,
            karcis.IsAktif,
            listKomponen,
            listLayanan
        );

        return Task.FromResult(response);
    }
}

public class KarcisGetHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly KarcisGetHandler _sut;

    public KarcisGetHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _sut = new KarcisGetHandler(_factory.Object);
    }

    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisGetQuery("A");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}