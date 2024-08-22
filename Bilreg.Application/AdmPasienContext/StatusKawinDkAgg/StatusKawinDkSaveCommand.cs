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
    public record StatusKawinDkSaveCommand(string StatusKawinDkId, string StatusKawinDkName) : IRequest, IStatusKawinDkKey;

    public class StatusKawinDkSaveHandler : IRequestHandler<StatusKawinDkSaveCommand>
    {
        private readonly IStatusKawinDkWriter _writer;

        public StatusKawinDkSaveHandler(IStatusKawinDkWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(StatusKawinDkSaveCommand request, CancellationToken cancellationToken)
        {
            //Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinDkId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinDkName);

            //  Build
            var statuskawindk = StatusKawinDkModel.Create(request.StatusKawinDkId, request.StatusKawinDkName);

            // Save
            _writer.Save(statuskawindk);
            return Task.CompletedTask;
        }

        
    }

    // Unit test

    public class StatusKawinDkSaveHandlerTest
    {
        private StatusKawinDkSaveHandler _skut;
        private Mock<IStatusKawinDkWriter> _writer;

        public StatusKawinDkSaveHandlerTest()
        {
            _writer = new Mock<IStatusKawinDkWriter>();
            _skut = new StatusKawinDkSaveHandler(_writer.Object);
        }

        [Fact]
        public void GivenNullRequest_ThenThrowEx()
        {
            //  ARRANGE
            StatusKawinDkSaveCommand? request = null;

            //  ACT
            var ex = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            ex.Should().ThrowAsync<ArgumentNullException>();
        }


        [Fact]
        public void GivenStatusKawinDkIdEmpty_ThenThrowEx()
        {
            //  Arrange
            var request = new StatusKawinDkSaveCommand("", "B");

            //  Act
            var ex = async () => await _skut.Handle(request, CancellationToken.None);

            //  Assert
            ex.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void GivenStatusKawinDkNameEmpty_ThenThrowEx()
        {
            //  ARRANGE
            var request = new StatusKawinDkSaveCommand("A", "");

            //  ACT
            var ex = async () => await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            ex.Should().ThrowAsync<ArgumentException>();
        }
    }

}
