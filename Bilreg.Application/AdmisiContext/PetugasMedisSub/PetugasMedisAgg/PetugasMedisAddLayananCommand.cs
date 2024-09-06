using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public record PetugasMedisAddLayananCommand(string PetugasMedisId, string LayananId)
    : IRequest, IPetugasMedisKey, ILayananKey;

public class PetugasMedisAddLayananHandler : IRequestHandler<PetugasMedisAddLayananCommand>
{
    private readonly IFactoryLoad<PetugasMedisModel, IPetugasMedisKey> _factory;
    private readonly IPetugasMedisWriter _writer;
    private readonly ILayananDal _layananDal;

    public PetugasMedisAddLayananHandler(IFactoryLoad<PetugasMedisModel, 
        IPetugasMedisKey> factory, ILayananDal layananDal, 
        IPetugasMedisWriter writer)
    {
        _factory = factory;
        _layananDal = layananDal;
        _writer = writer;
    }

    public Task Handle(PetugasMedisAddLayananCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PetugasMedisId);
        Guard.IsNotWhiteSpace(request.LayananId);
        var layanan = _layananDal.GetData(request)
            ?? throw new KeyNotFoundException($"Layanan {request.LayananId} not found");
        
        //  BUILD
        var petugasMedis = _factory.Load(request);
        petugasMedis.Add(layanan);
        
        //  WRITE
        _ = _writer.Save(petugasMedis);
        return Task.CompletedTask;
    }
}

public class PetugasMedisAddLayananHandlerTest
{
    private readonly PetugasMedisAddLayananHandler _sut;
    private readonly Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>> _factory;
    private readonly Mock<IPetugasMedisWriter> _writer;
    private readonly Mock<ILayananDal> _layananDal;

    public PetugasMedisAddLayananHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<PetugasMedisModel, IPetugasMedisKey>>();
        _writer = new Mock<IPetugasMedisWriter>();
        _layananDal = new Mock<ILayananDal>();
        _sut = new PetugasMedisAddLayananHandler(_factory.Object, _layananDal.Object, _writer.Object);
    }

    [Fact]
    public void GivenValidRequest_ThenShouldAddLayanan()
    {
        //  ARRANGE
        var request = new PetugasMedisAddLayananCommand("A", "B");
        _layananDal.Setup(x => x.GetData(It.IsAny<ILayananKey>()))
            .Returns(new LayananModel("L1", "L2"));
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Returns(new PetugasMedisModel("A", "B"));
        PetugasMedisModel actual = null;
        _writer.Setup(x => x.Save(It.IsAny<PetugasMedisModel>()))
            .Callback<PetugasMedisModel>(x => actual = x);
        //  ACT
        _ = _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        actual.ListLayanan.Count.Should().Be(1);
        actual.ListLayanan.First().LayananId.Should().Be("L1");
    }

    [Fact]
    public void GivenInvalidPetugMedisId_ThenShouldThrowKeyNotFoundException()
    {
        //  ARR
        var request = new PetugasMedisAddLayananCommand("A", "B");
        _layananDal.Setup(x => x.GetData(It.IsAny<ILayananKey>()))
            .Returns(new LayananModel("L1", "L2"));
        _factory.Setup(x => x.Load(It.IsAny<IPetugasMedisKey>()))
            .Throws<KeyNotFoundException>();

        //  ACT
        Action act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GivenInvalidLayananId_ThenShouldThrowKeyNotFoundException()
    {
        //  ARR
        var request = new PetugasMedisAddLayananCommand("A", "B");
        _layananDal.Setup(x => x.GetData(It.IsAny<ILayananKey>()))
            .Returns(null as LayananModel);

        //  ACT
        Action act = () => _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().Throw<KeyNotFoundException>();
    }
}
