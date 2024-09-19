using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using FluentAssertions;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public record JenisTarifGetQuery(string JenisTarifId) : IRequest<JenisTarifGetResponse>, IJenisTarifKey;

public record JenisTarifGetResponse(string JenisTarifId, string JenisTarifName);

public class JenisTarifGetHandler : IRequestHandler<JenisTarifGetQuery, JenisTarifGetResponse>
{
    private readonly IJenisTarifDal _jenisTarifDal;

    public JenisTarifGetHandler(IJenisTarifDal jenisTarifDal)
    {
        _jenisTarifDal = jenisTarifDal;
    }
    
    public Task<JenisTarifGetResponse> Handle(JenisTarifGetQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var jenisTarif = _jenisTarifDal.GetData(request)
                         ?? throw new KeyNotFoundException($"Jenis tarif with Id: {request.JenisTarifId} not found");
        
        // RESPONSE
        var response = new JenisTarifGetResponse(jenisTarif.JenisTarifId, jenisTarif.JenisTarifName);
        return Task.FromResult(response);
    }
}

public class JenisTarifGetHandlerTest
{
    private readonly Mock<IJenisTarifDal> _jenisTarifDal;
    private readonly JenisTarifGetHandler _sut;

    public JenisTarifGetHandlerTest()
    {
        _jenisTarifDal = new Mock<IJenisTarifDal>();
        _sut = new JenisTarifGetHandler(_jenisTarifDal.Object);
    }

    [Fact]
    public async Task GivenInvalidJenisTarifId_ThenThrowKeyNotFoundException()
    {
        var request = new JenisTarifGetQuery("A") ;
        _jenisTarifDal.Setup(x => x.GetData(It.IsAny<IJenisTarifKey>()))
            .Returns(null as JenisTarifModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidJenisTarifId_ThenReturnExpected()
    {
        var request = new JenisTarifGetQuery("A");
        var expected = new JenisTarifModel("A", "B");
        _jenisTarifDal.Setup(x => x.GetData(It.IsAny<IJenisTarifKey>()))
            .Returns(expected);
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().BeEquivalentTo(expected);
    }

}