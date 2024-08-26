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
    public record InstalasiDkDeleteCommand(string InstalasiDkId) : IRequest, IInstalasiDkKey;
    public class InstalasiDkDeleteHandler : IRequestHandler<InstalasiDkDeleteCommand>
    {
        private readonly IInstalasiDkWriter _writer;
        public InstalasiDkDeleteHandler(IInstalasiDkWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(InstalasiDkDeleteCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiDkId);

            //  WRITE
            _writer.Delete(request);
            return Task.CompletedTask;
        }
    }
    public class InstalasiDkDeleteHandlerTest {
        private readonly InstalasiDkDeleteHandler _sut;
        private readonly Mock<IInstalasiDkWriter> _writer;

        public InstalasiDkDeleteHandlerTest()
        {
            _sut = new InstalasiDkDeleteHandler(_writer.Object);
            _writer = new Mock<IInstalasiDkWriter>();

        }

        [Fact]
        public void GivenNullRequest_ThenThrowEx()
        {
            //  ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void GivenEmptySmfId_ThenThrowEx()
        {
            //  ARRANGE
            var request = new InstalasiDkDeleteCommand("");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GivenValidRequest_ThenDeleteData()
        {
            //  ARRANGE
            var request = new InstalasiDkDeleteCommand("1");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().NotThrowAsync();
        }

    }

}
