using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Application.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanSaveCommand(string KelurahanId, string KelurahanName, string KecamatanId, string KodePos)
    : IRequest, IKecamatanKey;

public class KelurahanSaveHandler : IRequestHandler<KelurahanSaveCommand>
{
    private readonly IKecamatanDal _kecamatanDal;
    private readonly IKelurahanWriter _writer;

    public KelurahanSaveHandler(IKecamatanDal kecamatanDal, IKelurahanWriter writer)
    {
        _kecamatanDal = kecamatanDal;
        _writer = writer;
    }

    public Task Handle(KelurahanSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KelurahanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KelurahanName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KecamatanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KodePos);

        if (!request.KelurahanId[..7].Equals(request.KecamatanId))
            throw new ArgumentException(
                $"KelurahanId: {request.KelurahanId} and KecamatanId: {request.KecamatanId} inconsistent");
        
        var kecamatan = _kecamatanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kecamatan {request.KecamatanId} not found");
        
        // BUILD
        var kelurahan = KelurahanModel.Create(request.KelurahanId, request.KelurahanName, request.KodePos);
        kelurahan.Set(kecamatan);
        
        // WRITE
        _writer.Save(kelurahan);

        return Task.CompletedTask;
    }
}

public class KelurahanSaveHandlerTest
{
    private readonly Mock<IKecamatanDal> _kecamatanDal;
    private readonly Mock<IKelurahanWriter> _writer;
    private readonly KelurahanSaveHandler _sut;

    public KelurahanSaveHandlerTest()
    {
        _kecamatanDal = new Mock<IKecamatanDal>();
        _writer = new Mock<IKelurahanWriter>();
        _sut = new KelurahanSaveHandler(_kecamatanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KelurahanSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKelurahanId_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanSaveCommand("", "B", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKelurahanName_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanSaveCommand("A", "", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKecamatanId_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanSaveCommand("A", "B", "", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKodePos_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanSaveCommand("A", "B", "C", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalid7FirstCharKelurahanId_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanSaveCommand("1234567ABC", "B", "1234568", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidKecamatanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new KelurahanSaveCommand("1234567ABC", "B", "1234567", "D");
        _kecamatanDal.Setup(x => x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(null as KecamatanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new KelurahanSaveCommand("1234567ABC", "B", "1234567", "C");
        var expected = KelurahanModel.Create("1234567ABC", "B", "C");
        var kecamatan = KecamatanModel.Create("1234567", "D");
        var kabupaten = KabupatenModel.Create("1234", "E");
        var propinsi = PropinsiModel.Create("12", "F");
        kabupaten.Set(propinsi);
        kecamatan.Set(kabupaten);
        expected.Set(kecamatan);
        KelurahanModel actual = null;
        
        _kecamatanDal.Setup(x => x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(kecamatan);
        _writer.Setup(x => x.Save(It.IsAny<KelurahanModel>()))
            .Callback<KelurahanModel>(k => actual = k);

        await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
;    }
}