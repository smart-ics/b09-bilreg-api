using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Xunit;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public record PasienSetAlamatCommand(
    string PasienId,
    string Alamat1,
    string Alamat2,
    string Alamat3,
    string Kota,
    string KodePos,
    string KelurahanId,
    string UserId
    ): IRequest,IPasienKey,IKelurahanKey;

public class PasienSetAlamatHandler : IRequestHandler<PasienSetAlamatCommand>
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienWriter _writer;
    private readonly IPasienLogWriter _logWriter;
    private readonly IKelurahanDal _kelurahanDal;
    
    private const string ACTIVITY_NAME = "PasienSetAlamat";

    public PasienSetAlamatHandler(IPasienDal pasienDal,IKelurahanDal kelurahanDal, IPasienWriter writer, IPasienLogWriter logWriter)
    {
        _pasienDal = pasienDal;
        _kelurahanDal = kelurahanDal;
        _writer = writer;
        _logWriter = logWriter;
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
        
        var originalPasienData = pasien.CloneObject();
        pasien.SetAddress(
            request.Alamat1,
            request.Alamat2,
            request.Alamat3,
            request.Kota,
            request.KodePos
        );
        pasien.SetKelurahan(kelurahan);

        var changes = PropertyChangeHelper.GetChanges(originalPasienData, pasien);
        var pasienLog = new PasienLogModel(request.PasienId, ACTIVITY_NAME, request.UserId);
        pasienLog.SetChangeLog(changes);
        
        // WRITE
        _ = _writer.Save(pasien);
        _ = _logWriter.Save(pasienLog);
        return Task.CompletedTask;
    }
}

public class PasienSetAlamatCommandTest
{
    private readonly Mock<IPasienDal> _pasienDal;
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly Mock<IPasienLogWriter> _logWriter;
    private readonly PasienSetAlamatHandler _sut;
        
    public PasienSetAlamatCommandTest()
    {
        _pasienDal = new Mock<IPasienDal>();
        _kelurahanDal = new Mock<IKelurahanDal>();
        _writer = new Mock<IPasienWriter>();
        _logWriter = new Mock<IPasienLogWriter>();
        _sut = new PasienSetAlamatHandler(_pasienDal.Object, _kelurahanDal.Object, _writer.Object, _logWriter.Object);
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
        var request = new PasienSetAlamatCommand("","A","B","C","D","E","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAlamat1_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","","B","C","D","E","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyAlamat2_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","","C","D","E","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyAlamat3_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","","D","E","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKota_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","","E","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKodePos_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","D","","F", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKelurahanId_ThenThrowArgumentException_Test()
    {
        var request = new PasienSetAlamatCommand("1","A","B","C","D","E","", "G");
        var actual = async() =>await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}
