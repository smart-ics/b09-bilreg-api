using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public record GrupRekapCetakSaveCommand(string GrupRekapCetakId, string GrupRekapCrtakName) : IRequest, IGrupRekapCetakKey;
public class GrupRekapCetakSaveCommandHandler : IRequestHandler<GrupRekapCetakSaveCommand>
{
    private readonly IGrupRekapCetakWriter _writer;

    public GrupRekapCetakSaveCommandHandler(IGrupRekapCetakWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupRekapCetakSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.GrupRekapCetakId);
        Guard.IsNotNullOrWhiteSpace(request.GrupRekapCrtakName);

        // BUILD
        var grupRekapCetak = new GrupRekapCetakModel(request.GrupRekapCetakId, request.GrupRekapCrtakName);

        // WRITE
        _writer.Save(grupRekapCetak);
        return Task.CompletedTask;
    }
}

public class GrupRekapCetakSaveCommandHandlerTest
{
    private readonly Mock<IGrupRekapCetakWriter> _writer;
    private readonly GrupRekapCetakSaveCommandHandler _sut;

    public GrupRekapCetakSaveCommandHandlerTest()
    {
        _writer = new Mock<IGrupRekapCetakWriter>();
        _sut = new GrupRekapCetakSaveCommandHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupRekapCetakSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException_Test()
    {
        var request = new GrupRekapCetakSaveCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKomponenName_ThenThrowArgumentException_Test()
    {
        var request = new GrupRekapCetakSaveCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}
