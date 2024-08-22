using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
using Bilreg.Application.AdmPasienContext.StatusKawinDkAgg;
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
    public record StatusKawinDkGetQuery(string StatusKawinDkId) : IRequest<StatusKawinDkGetResponse>, IStatusKawinDkKey;
    public record StatusKawinDkGetResponse(string StatusKawinDkId, string StatusKawinDkName);


    public class StatusKawinDkGetHandler : IRequestHandler<StatusKawinDkGetQuery, StatusKawinDkGetResponse>
    {
        private readonly IStatusKawinDkDal _statuskawinDkDal;

        public StatusKawinDkGetHandler(IStatusKawinDkDal statuskawinDkDal)
        {
            _statuskawinDkDal = statuskawinDkDal;
        }

        public Task<StatusKawinDkGetResponse> Handle(StatusKawinDkGetQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _statuskawinDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"Status Kawin not found: {request.StatusKawinDkId}");

            //  RESPONSE
            var response = new StatusKawinDkGetResponse(result.StatusKawinDkId, result.StatusKawinDkName);
            return Task.FromResult(response);
        }
    }
}

public class StatusKawinDkGetHandlerTest
{
    private readonly StatusKawinDkGetHandler _skut;
    private readonly Mock<IStatusKawinDkDal> _statuskawinDkDal;

    public StatusKawinDkGetHandlerTest()
    {
        _statuskawinDkDal = new Mock<IStatusKawinDkDal>();
        _skut = new StatusKawinDkGetHandler(_statuskawinDkDal.Object);
    }

    [Fact]
    public void GivenInvalidStatusKawinDkId_ThenThrowKeyNotFoundException()
    {
        //  ARRANGE
        var request = new StatusKawinDkGetQuery("123");
        _statuskawinDkDal.Setup(x => x.GetData(It.IsAny<IStatusKawinDkKey>()))
            .Returns(null as StatusKawinDkModel);

        //  ACT
        Func<Task> act = () => _skut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidStatusKawinDkId_ThenReturnExpected()
    {
        //  ARRANGE
        var expected = StatusKawinDkModel.Create("A", "B");
        var request = new StatusKawinDkGetQuery("A");
        _statuskawinDkDal.Setup(x => x.GetData(It.IsAny<IStatusKawinDkKey>()))
            .Returns(expected);

        //  ACT
        var act = await _skut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().BeEquivalentTo(expected);
    }
}
