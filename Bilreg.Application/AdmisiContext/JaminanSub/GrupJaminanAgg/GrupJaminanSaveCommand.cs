using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanSaveCommand(string GrupJaminanId, string GrupJaminanName, string Keterangan): IRequest, IGrupJaminanKey;

public class GrupJaminanSaveHandler: IRequestHandler<GrupJaminanSaveCommand>
{
    private readonly IGrupJaminanDal _grupJaminanDal;
    private readonly IGrupJaminanWriter _writer;

    public GrupJaminanSaveHandler(IGrupJaminanDal grupJaminanDal, IGrupJaminanWriter writer)
    {
        _grupJaminanDal = grupJaminanDal;
        _writer = writer;
    }
    
    public Task Handle(GrupJaminanSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupJaminanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupJaminanName);
        
        // BUILD
        var grupJaminan = GrupJaminanModel.Create(request.GrupJaminanId, request.GrupJaminanName, request.Keterangan);
        var existingGrupJaminan = _grupJaminanDal.GetData(request);
        if (existingGrupJaminan is not null)
        {
            if (existingGrupJaminan.IsKaryawan)
                grupJaminan.SetKaryawan();
            else
                grupJaminan.UnSetKaryawan();
        }
        
        // WRITE
        _writer.Save(grupJaminan);
        return Task.CompletedTask;
    }
}

public class GrupJaminanSaveHandlerTest
{
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly Mock<IGrupJaminanWriter> _writer;
    private readonly GrupJaminanSaveHandler _sut;

    public GrupJaminanSaveHandlerTest()
    {
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _writer = new Mock<IGrupJaminanWriter>();
        _sut = new GrupJaminanSaveHandler(_grupJaminanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupJaminanSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new GrupJaminanSaveCommand("", "B", "C");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyGrupJaminanName_ThenThrowArgumentException_Test()
    {
        var request = new GrupJaminanSaveCommand("A", "", "C");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new GrupJaminanSaveCommand("A", "B", "C");
        var expected = GrupJaminanModel.Create("A", "B", "C");
        GrupJaminanModel actual = null;
        _writer.Setup(x => x.Save(It.IsAny<GrupJaminanModel>()))
            .Callback<GrupJaminanModel>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}