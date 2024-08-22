using Bilreg.Domain.AdmPasienContext.StatusKawinDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.StatusKawinDkAgg
{
    public record StatusKawinDkDeleteCommand(string StatusKawinDkId) : IRequest, IStatusKawinDkKey;

    public class StatusKawinDkDeleteHandler : IRequestHandler<StatusKawinDkDeleteCommand>
    {
        private readonly IStatusKawinDkWriter _writer;

        public StatusKawinDkDeleteHandler(IStatusKawinDkWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(StatusKawinDkDeleteCommand request, CancellationToken cancellationToken)
        {
            // Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinDkId);

            //Write
            _writer.Delete(request);
            return Task.CompletedTask;
        }

    }

    public class StatusKawinDkDeleteHandlerTest
    {
        private readonly StatusKawinDkDeleteHandler _skut;
        private readonly Mock<IStatusKawinDkWriter> _writer;

        public StatusKawinDkDeleteHandlerTest()
        {
            _writer = new Mock<IStatusKawinDkWriter>();
            _skut = new StatusKawinDkDeleteHandler(_writer.Object);
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
        public void GivenEmptyStatusKawinDkId_ThenThrowEx()
        {
            //  ARRANG
            var request = new StatusKawinDkDeleteCommand("");

            //  ACT
            var act = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GivenValidRequest_ThenDeleteData()
        {
            //  ARRANG
            var request = new StatusKawinDkDeleteCommand("1");

            //  ACT
            var act = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().NotThrowAsync();
        }
    }


}




