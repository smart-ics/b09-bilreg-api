using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg
{
    public record SmfSaveCommand(string SmfId, string SmfName) : IRequest, ISmfKey;

    public class SmfSaveHandler : IRequestHandler<SmfSaveCommand>
    {
        private readonly ISmfWriter _writer;

        public SmfSaveHandler(ISmfWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(SmfSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SmfId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SmfName);

            // BUILD
            var smf = SmfModel.Create(request.SmfId, request.SmfName);

            // WRITE
            _writer.Save(smf);
            return Task.CompletedTask;
        }
    }

    public class SmfSaveHandlerTest
    {
        private SmfSaveHandler _sut;
        private Mock<ISmfWriter> _writer;

        public SmfSaveHandlerTest()
        {
            _writer = new Mock<ISmfWriter>();
            _sut = new SmfSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            // ARRANGE
            SmfSaveCommand request = null;
            // ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptySmfId_ThenThrowEx()
        {
            // ARRANGE
            var request = new SmfSaveCommand("", "Smf");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptySmfName_ThenThrowEx()
        {
            // ARRANGE
            var request = new SmfSaveCommand("Id", "");

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }

}
