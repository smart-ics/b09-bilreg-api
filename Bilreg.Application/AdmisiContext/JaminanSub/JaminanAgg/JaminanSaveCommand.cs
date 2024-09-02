using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public record JaminanSaveCommand(string JaminanId, string JaminanName) : IRequest, IJaminanKey;

public class JaminanSaveHandler : IRequestHandler<JaminanSaveCommand>
{
    private readonly IJaminanDal _jaminanDal;
    private readonly IJaminanWriter _writer;
    
    public JaminanSaveHandler(IJaminanDal jaminanDal, IJaminanWriter writer)
    {
        _jaminanDal = jaminanDal;
        _writer = writer;
    }

    public Task Handle(JaminanSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JaminanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JaminanName);

        // BUILD
        var jaminan = _jaminanDal.GetData(request)
            ?? JaminanModel.Create(request.JaminanId, request.JaminanName);

        jaminan.SetName(request.JaminanName);

        // WRITE
        _writer.Save(jaminan);
        return Task.CompletedTask;
    }
}

public class JaminanSaveHandlerTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly Mock<IJaminanWriter> _writer;
    private readonly JaminanSaveHandler _sut;

    public JaminanSaveHandlerTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _writer = new Mock<IJaminanWriter>();
        _sut = new JaminanSaveHandler(_jaminanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        JaminanSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyJaminanName_ThenThrowArgumentException_Test()
    {
        var request = new JaminanSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new JaminanSaveCommand("A", "B");
        var expected = JaminanModel.Create("A", "B");
        JaminanModel actual = null;
        _writer.Setup(x => x.Save(It.IsAny<JaminanModel>()))
            .Callback<JaminanModel>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }
}