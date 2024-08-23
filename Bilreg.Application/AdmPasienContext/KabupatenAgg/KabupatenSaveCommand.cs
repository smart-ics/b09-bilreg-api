
using Bilreg.Application.AdmPasienContext.PropinsiAgg;
using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public record KabupatenSaveCommand(
    string KabupatenId,
    string KabupatenName,
    string PropinsiId) : IRequest, IPropinsiKey;

public class KabupatenSaveHandler : IRequestHandler<KabupatenSaveCommand>
{
    private readonly IPropinsiDal _propinsiDal;
    private readonly IKabupatenWriter _writer;

    public KabupatenSaveHandler(IPropinsiDal propinsiDal, IKabupatenWriter writer)
    {
        _propinsiDal = propinsiDal;
        _writer = writer;
    }

    public Task Handle(KabupatenSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KabupatenId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KabupatenName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PropinsiId);
        var propinsi = _propinsiDal.GetData(request)
            ?? throw new KeyNotFoundException($"{request.PropinsiId} not found");
        
        //  BUILD
        var kabupaten = KabupatenModel.Create(request.KabupatenId, request.KabupatenName);
        kabupaten.Set(propinsi);
        
        //  WRITE
        _ = _writer.Save(kabupaten);
        return Task.CompletedTask;
    }
}

public class KabupatenSaveHandlerTest
{
    private readonly KabupatenSaveHandler _sut;
    private readonly Mock<IPropinsiDal> _propinsiDal;
    private readonly Mock<IKabupatenWriter> _writer;

    public KabupatenSaveHandlerTest()
    {
        _propinsiDal = new Mock<IPropinsiDal>();
        _writer = new Mock<IKabupatenWriter>();
        _sut = new KabupatenSaveHandler(_propinsiDal.Object, _writer.Object);
    }
    [Fact]
    public async Task GivenNullRequest_ThenThrowException()
    {
        KabupatenSaveCommand cmd = null;
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKabupatenId_ThenThrowEx()
    {
        var cmd = new KabupatenSaveCommand("", "B", "C");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public async Task GivenEmptyKabupatenName_ThenThrowEx()
    {
        var cmd = new KabupatenSaveCommand("A", "", "C");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyPropinsiId_ThenThrowEx()
    {
        var cmd = new KabupatenSaveCommand("A", "B", "");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidPropinsiId_ThenThrowEx()
    {
        var cmd = new KabupatenSaveCommand("A", "B", "C");
        _propinsiDal.Setup(x => x.GetData(It.IsAny<IPropinsiKey>()))
            .Returns(null as PropinsiModel);

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpetedObject()
    {
        //  ARRAGE
        var expected = KabupatenModel.Create("A", "B");
        expected.Set(PropinsiModel.Create("C", "D"));
        var cmd = new KabupatenSaveCommand("A", "B", "C");
        KabupatenModel actual = null;
        
        _propinsiDal.Setup(x => x.GetData(It.IsAny<IPropinsiKey>()))
            .Returns(PropinsiModel.Create("C", "D"));
        _writer.Setup(x => x.Save(It.IsAny<KabupatenModel>()))
            .Callback<KabupatenModel>(s => actual = s);
        
        //  ACT
        await _sut.Handle(cmd, CancellationToken.None);

        actual.Should().BeEquivalentTo(expected);
    }
}