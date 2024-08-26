using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public record CaraMasukDkListQuery() : IRequest<IEnumerable<CaraMasukDkListResponse>>;
    public record CaraMasukDkListResponse(string CaraMasukDkId, string CaraMasukDkName);

    public class CaraMasukDkListHandler : IRequestHandler<CaraMasukDkListQuery, IEnumerable<CaraMasukDkListResponse>>
    {
        private readonly ICaraMasukDkDal _caraMasukDkDal;
        public CaraMasukDkListHandler(ICaraMasukDkDal caraMasukDkDal)
        {
            _caraMasukDkDal = caraMasukDkDal;
        }

        public Task<IEnumerable<CaraMasukDkListResponse>> Handle(CaraMasukDkListQuery request, CancellationToken cancellationToken)
        {
            // QUERY
            var result = _caraMasukDkDal.ListData() ?? throw new KeyNotFoundException("Cara Masuk Not Found");

            // RESPONSE
            var response = result.Select(x => new CaraMasukDkListResponse(x.CaraMasukDkId, x.CaraMasukDkName));
            return Task.FromResult(response);
        }
    }

    public class CaraMasukDkListHandlerTest
    {
        private readonly CaraMasukDkListHandler _sut;
        private readonly Mock<ICaraMasukDkDal> _caraMasukDkDal;

        public CaraMasukDkListHandlerTest()
        {
            _caraMasukDkDal = new Mock<ICaraMasukDkDal>();
            _sut = new CaraMasukDkListHandler(_caraMasukDkDal.Object);
        }

        [Fact]
        public async Task GivenNoData_ThenThrowKeyNotFoundException()
        {
            // ARRANGE
            var request = new CaraMasukDkListQuery();
            _caraMasukDkDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<CaraMasukDkModel>);

            // ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            // ARRANGE
            var expected = new List<CaraMasukDkModel> { CaraMasukDkModel.Create("A", "B") };
            var request = new CaraMasukDkListQuery();
            _caraMasukDkDal.Setup(x => x.ListData())
                .Returns(expected);

            // ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            act.Should().BeEquivalentTo(expected.Select(x => new CaraMasukDkListResponse(x.CaraMasukDkId, x.CaraMasukDkName)));
        }
    }
}
