using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public record InstalasiListQuery() : IRequest<IEnumerable<InstalasiListResponse>>;

    public record InstalasiListResponse(
        string InstalasiId,
        string InstalasiName,
        string InstalasiDkId,
        string InstalasiDkName
    );

    public class InstalasiListHandler : IRequestHandler<InstalasiListQuery, IEnumerable<InstalasiListResponse>>
    {
        private readonly IInstalasiDal _instalasiDal;

        public InstalasiListHandler(IInstalasiDal instalasiDal)
        {
            _instalasiDal = instalasiDal;
        }

        public Task<IEnumerable<InstalasiListResponse>> Handle(InstalasiListQuery request, CancellationToken cancellationToken)
        {
            // QUERY
            var result = _instalasiDal.ListData()
                ?? throw new KeyNotFoundException("Instalasi not found");

            // RESPONSE
            var response = result.Select(x
                => new InstalasiListResponse(x.InstalasiId, x.InstalasiName, x.InstalasiDkId, x.InstalasiDkName));
            return Task.FromResult(response);
        }
    }

    public class InstalasiListHandlerTest
    {
        private readonly Mock<IInstalasiDal> _instalasiDal;
        private readonly InstalasiListHandler _sut;

        public InstalasiListHandlerTest()
        {
            _instalasiDal = new Mock<IInstalasiDal>();
            _sut = new InstalasiListHandler(_instalasiDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new InstalasiListQuery();
            _instalasiDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<InstalasiModel>);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<InstalasiModel> { InstalasiModel.Create("A", "B") };
            var request = new InstalasiListQuery();
            _instalasiDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected.Select(x => new InstalasiDkListResponse(x.InstalasiId, x.InstalasiName)));
        }
    }
}
