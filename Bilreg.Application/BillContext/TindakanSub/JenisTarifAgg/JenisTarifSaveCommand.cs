using System.Reflection.Metadata;
using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public record JenisTarifSaveCommand(string JenisTarifId, string JenisTarifName) : IRequest, IJenisTarifKey;

public class JenisTarifSaveHandler : IRequestHandler<JenisTarifSaveCommand>
{
    private readonly IJenisTarifWriter _writer;

    public JenisTarifSaveHandler(IJenisTarifWriter writer)
    {
        _writer = writer;
    }
    
    public Task Handle(JenisTarifSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.JenisTarifId);
        Guard.IsNotWhiteSpace(request.JenisTarifName);
        
        // BUILD
        var jenisTarif = new JenisTarifModel(request.JenisTarifId, request.JenisTarifName);
        
        // WRITE
        _ = _writer.Save(jenisTarif);
        return Task.CompletedTask;
    }
}

public class JenisTarifSaveHandlerTest
{
    private readonly Mock<IJenisTarifWriter> _writer;
    private readonly JenisTarifSaveHandler _sut;

    public JenisTarifSaveHandlerTest()
    {
        _writer = new Mock<IJenisTarifWriter>();
        _sut = new JenisTarifSaveHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        JenisTarifSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyJenisTarifId_ThenThrowArgumentNullException()
    {
        var request = new JenisTarifSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyJenisTarifName_ThenThrowArgumentNullException()
    {
        var request = new JenisTarifSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}