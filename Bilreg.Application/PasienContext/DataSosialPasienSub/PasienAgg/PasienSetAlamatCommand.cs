using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienSetAlamatCommand(
    string PasienId,
    string Alamat1,
    string Alamat2,
    string Alamat3,
    string Kota,
    string KodePos,
    string KelurahanId
    ): IRequest,IPasienKey,IKelurahanKey;

public class PasienSetAlamatHandler : IRequestHandler<PasienSetAlamatCommand>
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienWriter _writer;
    private readonly IKelurahanDal _kelurahanDal;

    public PasienSetAlamatHandler(IPasienDal pasienDal,IKelurahanDal kelurahanDal, IPasienWriter writer)
    {
        _pasienDal = pasienDal;
        _kelurahanDal = kelurahanDal;
        _writer = writer;
    }

    public Task Handle(PasienSetAlamatCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.PasienId);
        Guard.IsNotEmpty(request.Alamat1);
        Guard.IsNotEmpty(request.Alamat2);
        Guard.IsNotEmpty(request.Alamat3);
        Guard.IsNotEmpty(request.Kota);
        Guard.IsNotEmpty(request.KodePos);
        Guard.IsNotEmpty(request.KelurahanId);
        // BUILD
        var pasien = _pasienDal.GetData(request)
            ?? throw new KeyNotFoundException($"Pasien id {request.PasienId} not found");
        var kelurahan = _kelurahanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelurahan id {request.KelurahanId} not found");
        
        pasien.SetAddress(
            request.Alamat1,
            request.Alamat2,
            request.Alamat3,
            request.Kota,
            request.KodePos
        );
        pasien.SetKelurahan(kelurahan);
        // WRITE
        _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetAlamatCommandTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetAlamatHandler _sut;
        
    public PasienSetAlamatCommandTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _kelurahanDal = new Mock<IKelurahanDal>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetAlamatHandler(_pasienDal.Object, _kelurahanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        PasienSetAlamatCommand request = null;
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyId_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("","A","B","C","D","E","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAlamat1_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","","B","C","D","E","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyAlamat2_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","","C","D","E","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyAlamat3_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","","D","E","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKota_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","","E","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKodePos_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","D","","F");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKelurahanId_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","D","E","");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}
