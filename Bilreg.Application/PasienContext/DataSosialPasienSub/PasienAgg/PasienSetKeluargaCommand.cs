using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienSetKeluargaCommand(
    string PasienId,
    string KeluargaName,
    string KeluargaRelasi,
    string KeluargaNoTelp,
    string KeluargaAlamat1,
    string KeluargaAlamat2,
    string KeluargaKota,
    string KeluargaKodePos
) : IRequest, IPasienKey;

public class PasienSetKeluargaHandler : IRequestHandler<PasienSetKeluargaCommand>
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienWriter _writer;

    public PasienSetKeluargaHandler(IPasienDal pasienDal, IPasienWriter writer)
    {
        _pasienDal = pasienDal;
        _writer = writer;
    }

    public Task Handle(PasienSetKeluargaCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.PasienId);
        Guard.IsNotWhiteSpace(request.KeluargaName);
        Guard.IsNotWhiteSpace(request.KeluargaRelasi);
        Guard.IsNotWhiteSpace(request.KeluargaNoTelp);
        Guard.IsNotWhiteSpace(request.KeluargaAlamat1);
        Guard.IsNotWhiteSpace(request.KeluargaKota);
        Guard.IsNotWhiteSpace(request.KeluargaKodePos);

        // BUILD
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien with id: {request.PasienId} not found");
        pasien.SetKeluarga(request.KeluargaName, request.KeluargaRelasi, request.KeluargaNoTelp,
            request.KeluargaAlamat1, request.KeluargaAlamat2, request.KeluargaKota, request.KeluargaKodePos);

        // WRITE
        _ = _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetKeluargaHandlerTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetKeluargaHandler _sut;

    public PasienSetKeluargaHandlerTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetKeluargaHandler(_pasienDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        PasienSetKeluargaCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyPasienId_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("", "B", "C", "D", "E", "F", "G", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaName_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "", "C", "D", "E", "F", "G", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaRelasi_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "", "D", "E", "F", "G", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaNoTelp_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "", "E", "F", "G", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaAlamat1_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "", "F", "G", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaKota_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "", "H");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKeluargaKodePos_ThenThrowArgumentException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "G", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidPasienId_ThenThrowKeyNotFoundException()
    {
        var request = new PasienSetKeluargaCommand("A", "B", "C", "D", "E", "F", "G", "H");
        _pasienDal.Setup(x => x.GetData(It.IsAny<IPasienKey>()))
            .Returns(null as PasienModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}