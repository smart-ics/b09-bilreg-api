using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public record StatusKawinDeleteCommand(string StatusKawinId) : IRequest, IStatusKawinKey;

    public class StatusKawinDeleteHandler : IRequestHandler<StatusKawinDeleteCommand>
    {
        private readonly IStatusKawinWriter _writer;

        public StatusKawinDeleteHandler(IStatusKawinWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(StatusKawinDeleteCommand request, CancellationToken cancellationToken)
        {
            // Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinId);

            //Write
            _writer.Delete(request);
            return Task.CompletedTask;
        }

    }

    public class StatusKawinDeleteHandlerTest
    {
        private readonly StatusKawinDeleteHandler _skut;
        private readonly Mock<IStatusKawinWriter> _writer;

        public StatusKawinDeleteHandlerTest()
        {
            _writer = new Mock<IStatusKawinWriter>();
            _skut = new StatusKawinDeleteHandler(_writer.Object);
        }

        [Fact]
        public void GivenNullRequest_ThenThrowEx()
        {
            //   ACT
            var act = async () => await _skut.Handle(null!, CancellationToken.None);

            //   ASSERT
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void GivenEmptyStatusKawinId_ThenThrowEx()
        {
            //  ARRANG
            var request = new StatusKawinDeleteCommand("");

            //  ACT
            var act = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GivenValidRequest_ThenDeleteData()
        {
            //  ARRANG
            var request = new StatusKawinDeleteCommand("1");

            //  ACT
            var act = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().NotThrowAsync();
        }
    }


}




