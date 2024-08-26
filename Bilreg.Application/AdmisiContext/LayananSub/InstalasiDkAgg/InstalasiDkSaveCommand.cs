using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public record InstalasiDkSaveCommand(string InstalasiDkId, string InstalasiDkName) : IRequest, IInstalasiDkKey;

    public class InstalasiDkSaveHandler : IRequestHandler<InstalasiDkSaveCommand>
    {
        private readonly IInstalasiDkWriter _writer;

        public InstalasiDkSaveHandler(IInstalasiDkWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(InstalasiDkSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiDkId, nameof(request.InstalasiDkId));
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiDkName, nameof(request.InstalasiDkName));

            // BUILD
            var instalasiDk = InstalasiDkModel.Create(request.InstalasiDkId, request.InstalasiDkName);

            // WRITE
            _writer.Save(instalasiDk);
            return Task.CompletedTask;
        }
    }

    public class InstalasiDkSaveHandlerTest
    {
        private InstalasiDkSaveHandler _sut;
        private Mock<IInstalasiDkWriter> _writer;

        public InstalasiDkSaveHandlerTest()
        {
            _writer = new Mock<IInstalasiDkWriter>();
            _sut = new InstalasiDkSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            // ARRANGE
            InstalasiDkSaveCommand request = null;
            // ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiDkId_ThenThrowEx()
        {
            // ARRANGE
            var request = new InstalasiDkSaveCommand("", "InstalasiDk");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiDkName_ThenThrowEx()
        {
            // ARRANGE
            var request = new InstalasiDkSaveCommand("Id", "");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

}
