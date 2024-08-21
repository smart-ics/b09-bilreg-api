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
    public record StatusKawinSaveCommand(string StatusKawinId, string StatusKawin) : IRequest, IStatusKawinKey;

    public class StatusKawinSaveHandler : IRequestHandler<StatusKawinSaveCommand>
    {
        private readonly IStatusKawinWriter _writer;

        public StatusKawinSaveHandler(IStatusKawinWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(StatusKawinSaveCommand request, CancellationToken cancellationToken)
        {
            //Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawinId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StatusKawin);

            //  Build
            var statuskawin = StatusKawinModel.Create(request.StatusKawinId, request.StatusKawin);

            // Save
            _writer.Save(statuskawin);
            return Task.CompletedTask;
        }

        
    }

    // Unit test

    public class StatusKawinSaveHandlerTest
    {
        private StatusKawinSaveHandler _skut;
        private Mock<IStatusKawinWriter> _writer;

        public StatusKawinSaveHandlerTest()
        {
            _writer = new Mock<IStatusKawinWriter>();
            _skut = new StatusKawinSaveHandler(_writer.Object);
        }


        [Fact]
        public void GivenStatusKawinIdEmpty_ThenThrowEx()
        {
            //  Arrange
            var request = new StatusKawinSaveCommand("", "B");

            //  Act
            var ex = async () => await _skut.Handle(request, CancellationToken.None);

            //  Assert
            ex.Should().ThrowAsync<ArgumentException>();
        }
    }

}
