using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using MediatR;
using FluentAssertions;
using Moq;
using Xunit;
using Bilreg.Application.AdmPasienContext.PekerjaanAgg;

namespace Bilreg.Application.AdmPasienContext.PekerjaanAgg
{
    public record PekerjaanSaveCommand(string PekerjaanId, string PekerjaanName) : IRequest, IPekerjaanKey;

    public class PekerjaanSaveHandler : IRequestHandler<PekerjaanSaveCommand>
    {
        private readonly IPekerjaanWriter _writer;

        public PekerjaanSaveHandler(IPekerjaanWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(PekerjaanSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.PekerjaanName);

            //  BUILD
            var pekerjaan = PekerjaanModel.Create(request.PekerjaanId, request.PekerjaanName);

            //  WRITE
            _writer.Save(pekerjaan);
            return Task.CompletedTask;
        }
    }

    public class PekerjaanSaveHandlerTest
    {
        private PekerjaanSaveHandler _sut;
        private Mock<IPekerjaanWriter> _writer;

        public PekerjaanSaveHandlerTest()
        {
            _writer = new Mock<IPekerjaanWriter>();
            _sut = new PekerjaanSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            //  ARRANGE
            PekerjaanSaveCommand request = null;
            //  ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyPekerjaanId_ThenThrowEx()
        {
            //  ARRANGE
            var request = new PekerjaanSaveCommand("", "Pekerjaan");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyPekerjaanName_ThenThrowEx()
        {
            //  ARRANGE
            var request = new PekerjaanSaveCommand("Id", "");

            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
