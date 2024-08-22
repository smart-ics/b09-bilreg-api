using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
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
    public record StatusKawinGetQuery(string StatusKawinId) : IRequest<StatusKawinGetResponse>, IStatusKawinKey;
    public record StatusKawinGetResponse(string StatusKawinId, string StatusKawinName);


    public class StatusKawinGetHandler : IRequestHandler<StatusKawinGetQuery, StatusKawinGetResponse>
    {
        private readonly IStatusKawinDal _statuskawinDal;

        public StatusKawinGetHandler(IStatusKawinDal statuskawinDal)
        {
            _statuskawinDal = statuskawinDal;
        }

        public Task<StatusKawinGetResponse> Handle(StatusKawinGetQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _statuskawinDal.GetData(request)
                ?? throw new KeyNotFoundException($"Status Kawin not found: {request.StatusKawinId}");

            //  RESPONSE
            var response = new StatusKawinGetResponse(result.StatusKawinId, result.StatusKawinName);
            return Task.FromResult(response);
        }
    }
}

public class StatusKawinGetHandlerTest
{
    private readonly StatusKawinGetHandler _skut;
    private readonly Mock<IStatusKawinDal> _statuskawinDal;

    public StatusKawinGetHandlerTest()
    {
        _statuskawinDal = new Mock<IStatusKawinDal>();
        _skut = new StatusKawinGetHandler(_statuskawinDal.Object);
    }

    [Fact]
    public void GivenInvalidStatusKawinId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new StatusKawinGetQuery("123");
        _statuskawinDal.Setup(x => x.GetData(It.IsAny<IStatusKawinKey>()))
            .Returns(null as StatusKawinModel);

        //  ACT
        Func<Task> act = () => _skut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidStatusKawinId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = StatusKawinModel.Create("A", "B");
        var request = new StatusKawinGetQuery("A");
        _statuskawinDal.Setup(x => x.GetData(It.IsAny<IStatusKawinKey>()))
            .Returns(expected);

        //  ACT
        var act = await _skut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
}
