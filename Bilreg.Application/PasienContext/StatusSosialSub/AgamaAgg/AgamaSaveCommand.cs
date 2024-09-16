using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg
{
    public record AgamaSaveCommand(string AgamaId, string AgamaName) : IRequest, IAgamaKey;

    public class AgamaSaveHandler : IRequestHandler<AgamaSaveCommand>
    {
        private readonly IAgamaWriter _writer;

        public AgamaSaveHandler(IAgamaWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(AgamaSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.AgamaId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.AgamaName);

            //  BUILD
            var agama = new AgamaModel(request.AgamaId, request.AgamaName);

            //  WRITE
            _ = _writer.Save(agama);
            return Task.CompletedTask;
        }
    }

    public class AgamaSaveHandlerTest
    {
        private readonly AgamaSaveHandler _sut;
        private readonly Mock<IAgamaWriter> _writer;
        
        public AgamaSaveHandlerTest()
        {
            _writer = new Mock<IAgamaWriter>();
            _sut = new AgamaSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            //  ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);
            
            //  ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }
        
        [Fact]
        public async Task GivenEmptyAgamaId_ThenThrowEx()
        {
            //  ARRANGE
            var request = new AgamaSaveCommand("", "Agama");
            
            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);
            
            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
        
        [Fact]
        public async Task GivenEmptyAgamaName_ThenThrowEx()
        {
            //  ARRANGE
            var request = new AgamaSaveCommand("Id", "");
            
            //  ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);
            
            //  ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }
        
    }
}
