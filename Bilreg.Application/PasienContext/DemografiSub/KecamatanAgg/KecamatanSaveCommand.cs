using Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanSaveCommand(string KecamatanId, string KecamatanName, string KabupatenId): IRequest, IKabupatenKey;

public class KecamatanSaveHandler: IRequestHandler<KecamatanSaveCommand>
{
    private readonly IKabupatenDal _kabupatenDal;
    private readonly IKecamatanWriter _writer;

    public KecamatanSaveHandler(IKabupatenDal kabupatenDal, IKecamatanWriter writer)
    {
        _kabupatenDal = kabupatenDal;
        _writer = writer;
    }

    public Task Handle(KecamatanSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KecamatanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KecamatanName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KabupatenId);
        var kabupaten = _kabupatenDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kabupaten id: {request.KabupatenId} not found");

        // BUILD
        var kecamatan = KecamatanModel.Create(request.KecamatanId, request.KecamatanName);
        kecamatan.Set(kabupaten);

        // WRITE
        _writer.Save(kecamatan);
        return Task.CompletedTask;
    }
}

public class KecamatanSaveHandlerTest
{
    private readonly Mock<IKabupatenDal> _kabupatenDal;
    private readonly Mock<IKecamatanWriter> _writer;
    private readonly KecamatanSaveHandler _sut;
    
    public KecamatanSaveHandlerTest()
    {
        _kabupatenDal = new Mock<IKabupatenDal>();
        _writer = new Mock<IKecamatanWriter>();
        _sut = new KecamatanSaveHandler(_kabupatenDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowNullArgumentException_Test()
    {
        KecamatanSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKecamatanId_ThenThrowArgumentException_Test()
    {
        KecamatanSaveCommand request = new KecamatanSaveCommand("", "B", "C");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKecamatanName_ThenThrowArgumentException_Test()
    {
        KecamatanSaveCommand request = new KecamatanSaveCommand("A", "", "C");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKabupatenId_ThenThrowArgumentException_Test()
    {
        KecamatanSaveCommand request = new KecamatanSaveCommand("A", "B", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidKabupatenId_ThenThrowKeyNotFoundException_Test()
    {
        KecamatanSaveCommand request = new KecamatanSaveCommand("A", "B", "C");
        _kabupatenDal.Setup(x => x.GetData(It.IsAny<IKabupatenKey>()))
            .Returns(null as KabupatenModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var expected = KecamatanModel.Create("A", "B");
        expected.Set(KabupatenModel.Create("C", "D"));
        var request = new KecamatanSaveCommand("A", "B", "C");
        KecamatanModel actual = null;

        _kabupatenDal.Setup(x => x.GetData(It.IsAny<IKabupatenKey>()))
            .Returns(KabupatenModel.Create("C", "D"));
        _writer.Setup(x => x.Save(It.IsAny<KecamatanModel>()))
            .Callback((KecamatanModel k) => actual = k);
        
        await _sut.Handle(request, CancellationToken.None);
        
        actual.Should().BeEquivalentTo(expected);
    }
}