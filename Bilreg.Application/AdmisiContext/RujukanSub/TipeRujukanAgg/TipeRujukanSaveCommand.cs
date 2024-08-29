using Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg
{
    public record TipeRujukanSaveCommand(string TipeRujukanId, string TipeRujukanName) : IRequest , ITipeRujukanKey;
    public class TipeRujukanSaveHandler : IRequestHandler<TipeRujukanSaveCommand>
    {
        private readonly ITipeRujukanWriter _writer;

        public TipeRujukanSaveHandler(ITipeRujukanWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(TipeRujukanSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.TipeRujukanId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.TipeRujukanName);

            //  BUILD
            var tipeRujukan = TipeRujukanModel.Create(request.TipeRujukanId, request.TipeRujukanName);

            //  WRITE
            _writer.Save(tipeRujukan);
            return Task.CompletedTask;
        }
    }

    public class TipeRujukanSaveHandlerTest
    {
        private TipeRujukanSaveHandler _sut;
        private Mock<ITipeRujukanWriter> _writer;   

        public TipeRujukanSaveHandlerTest()
        {
            _writer = new Mock<ITipeRujukanWriter>();
            _sut = new TipeRujukanSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            // ARRANGE
            TipeRujukanSaveCommand request = null;

            // ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyTipeRujukanId_ThenThrowEx()
        {
            // ARRANGE
            var request = new TipeRujukanSaveCommand("Id","");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyTipeRujukanName_ThenTrowEx()
        {
            // ARRANGE
            var request = new TipeRujukanSaveCommand("", "TipeRujukan");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
