using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
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
    private readonly IFactoryLoad<PasienModel, IPasienKey> _factory;
    private readonly IPasienWriter _writer;
    private readonly IKelurahanDal _kelurahanDal;
    
    private const string ACTIVITY_NAME = "PasienSetAlamat";

    public PasienSetAlamatHandler(IFactoryLoad<PasienModel, IPasienKey> factory,IKelurahanDal kelurahanDal, IPasienWriter writer)
    {
        _factory = factory;
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
        var kelurahan = _kelurahanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelurahan id {request.KelurahanId} not found");
        
        // BUILD
        var pasien = _factory.Load(request);
        var originalPasienData = PropertyChangeHelper.CloneObject<PasienModel, PasienModelSerializable>(pasien);
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
        pasien.Add(pasienLog);
        
        // WRITE
        _ = _writer.Save(pasien);
        return Task.CompletedTask;
    }
}

public class PasienSetAlamatCommandTest
{
    private readonly Mock<IFactoryLoad<PasienModel, IPasienKey>> _factory;
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly Mock<IPasienWriter> _writer;
    private readonly PasienSetAlamatHandler _sut;
        
    public PasienSetAlamatCommandTest()
    {
        _factory = new Mock<IFactoryLoad<PasienModel, IPasienKey>>();
        _kelurahanDal = new Mock<IKelurahanDal>();
        _writer = new Mock<IPasienWriter>();
        _sut = new PasienSetAlamatHandler(_factory.Object, _kelurahanDal.Object, _writer.Object);
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
