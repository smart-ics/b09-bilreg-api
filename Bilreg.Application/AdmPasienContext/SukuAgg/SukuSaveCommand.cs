using Bilreg.Domain.AdmPasienContext.SukuAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.SukuAgg;

public record SukuSaveCommand(string SukuId, string SukuName) : IRequest, ISukuKey;

public class SukuSaveHandler : IRequestHandler<SukuSaveCommand>
{
    private readonly ISukuWriter _writer;

    public SukuSaveHandler(ISukuWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(SukuSaveCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SukuId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SukuName);
        
        //  BUILD
        var suku = SukuModel.Create(request.SukuId, request.SukuName);
        
        //  SAVE
        _writer.Save(suku);
        return Task.CompletedTask;
    }
}

public class SukuSaveHandlerTest
{
    private readonly SukuSaveHandler _sut;
    private readonly Mock<ISukuWriter> _writer;

    public SukuSaveHandlerTest()
    {
        _writer = new Mock<ISukuWriter>();
        _sut = new SukuSaveHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowEx()
    {
        //  ARRANGE
        SukuSaveCommand request = null!;
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        ex.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public void GivenSukuIdEmpty_ThenThrowEx()
    {
        //  ARRANGE
        var request = new SukuSaveCommand("", "B");
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        ex.Should().ThrowAsync<ArgumentException>();
    }
    [Fact]
    public void GivenSukuNameEmpty_ThenThrowEx()
    {
        //  ARRANGE
        var request = new SukuSaveCommand("A", "");
        
        //  ACT
        var ex = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        ex.Should().ThrowAsync<ArgumentException>();
    }
}