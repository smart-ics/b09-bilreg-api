using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public record GrupJaminanSetAsKaryawanCommand(string GrupJaminanId) : IRequest, IGrupJaminanKey;

public class GrupJaminanSetAsKaryawanHandler: IRequestHandler<GrupJaminanSetAsKaryawanCommand>
{
    private readonly IGrupJaminanWriter _writer;
    private readonly IGrupJaminanDal _grupJaminanDal;

    public GrupJaminanSetAsKaryawanHandler(IGrupJaminanWriter writer, IGrupJaminanDal grupJaminanDal)
    {
        _writer = writer;
        _grupJaminanDal = grupJaminanDal;
    }

    public Task Handle(GrupJaminanSetAsKaryawanCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GrupJaminanId);
        var thisGrupJaminan = _grupJaminanDal.GetData(request)
            ?? throw new KeyNotFoundException($"GrupJaminan with id {request.GrupJaminanId} not found");
        
        //  BUILD
        //      object-1
        var listAll = _grupJaminanDal.ListData();
        var currentGrupJaminanKaryawan = listAll.FirstOrDefault(x => x.IsKaryawan);
        if (currentGrupJaminanKaryawan is not null)
            currentGrupJaminanKaryawan.UnSetKaryawan();
        //      object-2
        thisGrupJaminan.SetKaryawan();

        //  WRITE
        if (currentGrupJaminanKaryawan is not null)
            _writer.Save(currentGrupJaminanKaryawan);
        _writer.Save(thisGrupJaminan);
        return Task.CompletedTask;
    }
}

public class GrupJaminanSetAsKaryawanHanderTest
{
    private readonly Mock<IGrupJaminanWriter> _writer;
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly GrupJaminanSetAsKaryawanHandler _sut;

    public GrupJaminanSetAsKaryawanHanderTest()
    {
        _writer = new Mock<IGrupJaminanWriter>();
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _sut = new GrupJaminanSetAsKaryawanHandler(_writer.Object, _grupJaminanDal.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupJaminanSetAsKaryawanCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new GrupJaminanSetAsKaryawanCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}