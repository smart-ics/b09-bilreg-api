using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg
{
    public record PekerjaanDkSaveCommand(string PekerjaanDkId, string PekerjaanDkName) : IRequest, IPekerjaanDkKey;

    public class PekerjaanDkSaveHandler : IRequestHandler<PekerjaanDkSaveCommand>
    {
        private readonly IPekerjaanDkWriter _writer;

        public PekerjaanDkSaveHandler(IPekerjaanDkWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(PekerjaanDkSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanDkId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanDkName);

            //  BUILD
            var pekerjaanDk = PekerjaanDkModel.Create(request.PekerjaanDkId, request.PekerjaanDkName);

            //  WRITE
            _writer.Save(pekerjaanDk);
            return Task.CompletedTask;
        }
    }

    public class PekerjaanDkSaveHandlerTest
    {
        private PekerjaanDkSaveHandler _sut;
        private Mock<IPekerjaanDkWriter> _writer;

        public PekerjaanDkSaveHandlerTest()
        {
            _writer = new Mock<IPekerjaanDkWriter>();
            _sut = new PekerjaanDkSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            //  ARRANGE
            PekerjaanDkSaveCommand request = null;
            //  ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyPekerjaanDkId_ThenThrowEx()
        {
            //  ARRANGE
            var request = new PekerjaanDkSaveCommand("", "Pekerjaan");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyPekerjaanDkName_ThenThrowEx()
        {
            //  ARRANGE
            var request = new PekerjaanDkSaveCommand("Id", "");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
