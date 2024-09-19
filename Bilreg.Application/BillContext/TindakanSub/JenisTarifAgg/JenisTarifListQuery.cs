using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using FluentAssertions;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public record JenisTarifListQuery() : IRequest<IEnumerable<JenisTarifListResponse>>;

public record JenisTarifListResponse(string JenisTarifId, string JenisTarifName);

public class JenisTarifListHandler : IRequestHandler<JenisTarifListQuery, IEnumerable<JenisTarifListResponse>>
{
    private readonly IJenisTarifDal _jenisTarifDal;

    public JenisTarifListHandler(IJenisTarifDal jenisTarifDal)
    {
        _jenisTarifDal = jenisTarifDal;
    }
    
    public Task<IEnumerable<JenisTarifListResponse>> Handle(JenisTarifListQuery request, CancellationToken cancellationToken)
    {
        // QUERY
        var jenisTarifList = _jenisTarifDal.ListData()
            ?? throw new KeyNotFoundException("Jenis Tarif not found");
        
        // RESPONSE
        var response = jenisTarifList.Select(x => new JenisTarifListResponse(x.JenisTarifId, x.JenisTarifName));
        return Task.FromResult(response);
    }
}

public class JenisTarifListHandlerTest
{
    private readonly Mock<IJenisTarifDal> _jenisTarifDal;
    private JenisTarifListHandler _sut;

    public JenisTarifListHandlerTest()
    {
        _jenisTarifDal = new Mock<IJenisTarifDal>();
        _sut = new JenisTarifListHandler(_jenisTarifDal.Object);
    }

    [Fact]
    public async Task GivenEmptyData_ThenThrowKeyNotFoundException()
    {
        var request = new JenisTarifListQuery();
        _jenisTarifDal.Setup(x => x.ListData())
            .Returns(null as IEnumerable<JenisTarifModel>);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidData_ThenReturnExpectedResponse()
    {
        var request = new JenisTarifListQuery();
        var expected = new JenisTarifModel("A", "B");
        _jenisTarifDal.Setup(x => x.ListData())
            .Returns(new List<JenisTarifModel>() { expected });
        var actual = await _sut.Handle(request, CancellationToken.None);
        actual.Should().ContainEquivalentOf(expected);
    }
    
}