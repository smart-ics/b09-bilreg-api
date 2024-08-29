﻿using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public record CaraMasukDkGetQuery(string CaraMasukDkId) : IRequest<CaraMasukDkGetResponse>, ICaraMasukDkKey;
    public record CaraMasukDkGetResponse(string CaraMasukDkId, string CaraMasukDkName);
    public class CaraMasukDkGetHendler : IRequestHandler<CaraMasukDkGetQuery, CaraMasukDkGetResponse>
    {
        private readonly ICaraMasukDkDal _caraMasukDkDal;

        public CaraMasukDkGetHendler(ICaraMasukDkDal caraMasukDkDal)
        {
            _caraMasukDkDal = caraMasukDkDal;
        }

        public Task<CaraMasukDkGetResponse> Handle(CaraMasukDkGetQuery request, CancellationToken cancellationToken)
        {
            //Query
            var result = _caraMasukDkDal.GetData(request) ??
                throw new KeyNotFoundException($"Cara Masuk Not Found : {request.CaraMasukDkId}");

            //RESPONSE
            var response = new CaraMasukDkGetResponse(result.CaraMasukDkId, result.CaraMasukDkName);
            return Task.FromResult(response);
        }
    }

    public class CaraMasukDkGetHandlerTest
    {
        private readonly CaraMasukDkGetHendler _sut;
        private readonly Mock<ICaraMasukDkDal> _caraMasukDkDal; // Mengubah dari 'public' menjadi 'private'

        public CaraMasukDkGetHandlerTest()
        {
            _caraMasukDkDal = new Mock<ICaraMasukDkDal>();
            _sut = new CaraMasukDkGetHendler(_caraMasukDkDal.Object);
        }

        [Fact]
        public async Task GivenInvalidCaraMasukDkId_ThenThrowKeyNotFoundException()
        {
            // ARRANGE
            var request = new CaraMasukDkGetQuery("123");
            _caraMasukDkDal.Setup(x => x.GetData(It.IsAny<ICaraMasukDkKey>()))
                .Returns(null as CaraMasukDkModel);

            // ACT
            Func<Task> act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }


        [Fact]
        public async Task GivenValidCaraMasukDkId_ThenReturnExpected()
        {
            // ARRANGE
            var expected = CaraMasukDkModel.Create("A", "B");
            var request = new CaraMasukDkGetQuery("A");
            _caraMasukDkDal.Setup(x => x.GetData(It.IsAny<ICaraMasukDkKey>()))
                .Returns(expected);

            // ACT
            var act = await _sut.Handle(request, CancellationToken.None);
        }
    }
}
