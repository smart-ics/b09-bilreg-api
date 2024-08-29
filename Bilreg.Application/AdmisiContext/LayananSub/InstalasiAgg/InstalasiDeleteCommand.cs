using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public record InstalasiDeleteCommand(string InstalasiId): IRequest,IInstalasiKey;
    public class InstalasiDeleteHandler : IRequestHandler<InstalasiDeleteCommand>
    {
        private readonly InstalasiWriter _writer;

        public InstalasiDeleteHandler(InstalasiWriter instalasiWriter)
        {
            _writer = instalasiWriter;
        }

        public Task Handle(InstalasiDeleteCommand request, CancellationToken cancellationToken)
        {
            // Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiId);

            // Write
            _writer.Delete(request);
            return Task.CompletedTask;

        }
    }
    public class InstalasiDeleteHandlerTest
    {
        private readonly InstalasiDeleteHandler _sut;
        private readonly Mock<InstalasiWriter> _writer;

        public InstalasiDeleteHandlerTest()
        {
            _writer = new Mock<InstalasiWriter>();
            _sut = new InstalasiDeleteHandler(_writer.Object);
        }


        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            InstalasiDeleteCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiId_ThenThrowArgumentException_Test()
        {
            var request = new InstalasiDeleteCommand("");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenDeleteData_Test()
        {
            var request = new InstalasiDeleteCommand("A");
            IInstalasiKey actual = null;
            _writer.Setup(x => x.Delete(It.IsAny<IInstalasiKey>()))
                .Callback<IInstalasiKey>(y => actual = y);
            await _sut.Handle(request, CancellationToken.None);
            actual?.InstalasiId.Should().Be("A");
        }

    }
}
