using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifSetNoUrutCommand(string TipeTarifId, decimal NoUrut) : IRequest, ITipeTarifKey;

public class TipeTarifSetNoUrutHandler : IRequestHandler<TipeTarifSetNoUrutCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifSetNoUrutHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    
    public Task Handle(TipeTarifSetNoUrutCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD
        var tipeTarifList = _tipeTarifDal.ListData()
            ?? throw new KeyNotFoundException("No tipe tarif");
        
        var List = tipeTarifList.ToList().OrderBy(x => x.NoUrut).ToList();
        var result = ReOrder(List, request.TipeTarifId, request.NoUrut);
        
        // WRITE
        List.ForEach(x => _writer.Save(x));
        return Task.CompletedTask;
    }

    private IEnumerable<TipeTarifModel> ReOrder(List<TipeTarifModel> list, string id, decimal target)
    {
        var targetItem = list.FirstOrDefault(x => x.NoUrut == target);
        return targetItem is null ?
            ReplaceNoUrut(list, id, target) :
            ReOrderNoUrut(list, id, target);
            
    }

    private IEnumerable<TipeTarifModel> ReOrderNoUrut(List<TipeTarifModel> list, string id, decimal target)
    {
        var indexedTipeTarif = list
            .OrderBy(x => x.NoUrut)
            .ThenBy(x => x.TipeTarifName)
            .Select((tipeTarif, index) => new IndexTipeTarif
            {
                Index = index + 1,
                TipeTarif = tipeTarif
            })
            .ToList();
        
        var originItem = indexedTipeTarif.First(x => x.TipeTarif.TipeTarifId == id);
        var targetItem = indexedTipeTarif.First(x => x.TipeTarif.NoUrut == target);
        var targetItemIndex = targetItem.Index;
        var currentItemIndex = originItem.Index;

        while (currentItemIndex != targetItemIndex)
        {
            if (currentItemIndex < targetItemIndex)
            {
                Swap(currentItemIndex, currentItemIndex+1);
                currentItemIndex++;
            }
            else
            {
                Swap(currentItemIndex, currentItemIndex-1);
                currentItemIndex--;
            }
        }
        
        var result = indexedTipeTarif.Select(x => x.TipeTarif);
        return result;
        
        void Swap(decimal originIndex, decimal targetIndex)
        {
            var originTipeTarif = indexedTipeTarif.First(x => x.Index == originIndex);
            var targetTipeTarif = indexedTipeTarif.First(x => x.Index == targetIndex);
            (originTipeTarif.TipeTarif, targetTipeTarif.TipeTarif) = (targetTipeTarif.TipeTarif, originTipeTarif.TipeTarif);

            var temp = targetTipeTarif.TipeTarif.NoUrut;
            targetTipeTarif.TipeTarif.SetNoUrut(originTipeTarif.TipeTarif.NoUrut);
            originTipeTarif.TipeTarif.SetNoUrut(temp);

        }
    }
    
    private IEnumerable<TipeTarifModel> ReplaceNoUrut(List<TipeTarifModel> list, string id, decimal noUrut)
    {
        var originalItem = list.First(x => x.TipeTarifId == id);
        originalItem.SetNoUrut(noUrut);
        return list;
    }
    
    private class IndexTipeTarif
    {
        public decimal Index { get; set; }
        public TipeTarifModel TipeTarif { get; set; }
    }
}

public class TipeTarifSetNoUrutHandlerTest
{
    private readonly Mock<ITipeTarifDal> _tipeTarifDal;
    private readonly Mock<ITipeTarifWriter> _writer;
    private readonly TipeTarifSetNoUrutHandler _sut;

    public TipeTarifSetNoUrutHandlerTest()
    {
        _tipeTarifDal = new Mock<ITipeTarifDal>();
        _writer = new Mock<ITipeTarifWriter>();
        _sut = new TipeTarifSetNoUrutHandler(_tipeTarifDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThrowsArgumentNullException_Test()
    {
        TipeTarifSetNoUrutCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyTipeTarifId_ThrowsArgumentException_Test()
    {
        var request = new TipeTarifSetNoUrutCommand(" ", 1);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    private static List<TipeTarifModel> Faker()
    {
        var tipeTarif1 = new TipeTarifModel("A", "ASKES");tipeTarif1.SetNoUrut(1);
        var tipeTarif2 = new TipeTarifModel("B", "ASURANSI");tipeTarif2.SetNoUrut(2);
        var tipeTarif3 = new TipeTarifModel("C", "CITO");tipeTarif3.SetNoUrut(3);
        var tipeTarif4 = new TipeTarifModel("D", "INSTANSI");tipeTarif4.SetNoUrut(4);
        var tipeTarif5 = new TipeTarifModel("E", "JAMKESMAS");tipeTarif5.SetNoUrut(5);
        var listTipeTarif = new List<TipeTarifModel>
        {
            tipeTarif1, tipeTarif2, tipeTarif3, tipeTarif4, tipeTarif5
        };
        return listTipeTarif;
    }
    
    [Fact]
    public void GivenNoUrutMengecil_ThenOtherRecordReOrdered()
    {
        // ARRANGE
        var listTipeTarif = Faker();
        _tipeTarifDal.Setup(x => x.ListData()).Returns(listTipeTarif);
        var cmd = new TipeTarifSetNoUrutCommand("D", 2);
        var actual = new List<TipeTarifModel>();
        _writer
            .Setup(x => x.Save(It.IsAny<TipeTarifModel>()))
            .Callback<TipeTarifModel>(arg => actual.Add(arg));

        // ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        // ASSERT
        actual.First(x => x.NoUrut == 1).TipeTarifName.Should().Be("ASKES");
        actual.First(x => x.NoUrut == 2).TipeTarifName.Should().Be("INSTANSI");
        actual.First(x => x.NoUrut == 3).TipeTarifName.Should().Be("ASURANSI");
        actual.First(x => x.NoUrut == 4).TipeTarifName.Should().Be("CITO");
        actual.First(x => x.NoUrut == 5).TipeTarifName.Should().Be("JAMKESMAS");
        
    }
    
    [Fact]
    public void GivenNoUrutMembesar_ThenOtherRecordReOrdered()
    {
        // ARRANGE
        var listTipeTarif = Faker();
        _tipeTarifDal.Setup(x => x.ListData()).Returns(listTipeTarif);
        var cmd = new TipeTarifSetNoUrutCommand("B", 4);
        var actual = new List<TipeTarifModel>();
        _writer
            .Setup(x => x.Save(It.IsAny<TipeTarifModel>()))
            .Callback<TipeTarifModel>(arg => actual.Add(arg));

        // ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        // ASSERT
        actual.First(x => x.NoUrut == 1).TipeTarifName.Should().Be("ASKES");
        actual.First(x => x.NoUrut == 2).TipeTarifName.Should().Be("CITO");
        actual.First(x => x.NoUrut == 3).TipeTarifName.Should().Be("INSTANSI");
        actual.First(x => x.NoUrut == 4).TipeTarifName.Should().Be("ASURANSI");
        actual.First(x => x.NoUrut == 5).TipeTarifName.Should().Be("JAMKESMAS");
        
    }
    
    [Fact]
    public void GivenExistingDataBolong_ThenOtherRecordReOrdered()
    {
        // ARRANGE
        var listTipeTarif = Faker();
        listTipeTarif.RemoveAll(x => x.TipeTarifName == "CITO");
        _tipeTarifDal.Setup(x => x.ListData()).Returns(listTipeTarif);
        var cmd = new TipeTarifSetNoUrutCommand("D", 2);
        var actual = new List<TipeTarifModel>();
        _writer
            .Setup(x => x.Save(It.IsAny<TipeTarifModel>()))
            .Callback<TipeTarifModel>(arg => actual.Add(arg));

        // ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        // ASSERT
        actual.First(x => x.NoUrut == 1).TipeTarifName.Should().Be("ASKES");
        actual.First(x => x.NoUrut == 2).TipeTarifName.Should().Be("INSTANSI");
        actual.FirstOrDefault(x => x.NoUrut == 3).Should().BeNull();
        actual.First(x => x.NoUrut == 4).TipeTarifName.Should().Be("ASURANSI");
        actual.First(x => x.NoUrut == 5).TipeTarifName.Should().Be("JAMKESMAS");
        
    }
    
    [Fact]
    public void GivenExistingDataDuplicated_ThenOtherRecordReOrdered()
    {
        // ARRANGE
        var listTipeTarif = Faker();
        listTipeTarif.First(x => x.TipeTarifName == "CITO").SetNoUrut(2);
        _tipeTarifDal.Setup(x => x.ListData()).Returns(listTipeTarif);
        var cmd = new TipeTarifSetNoUrutCommand("D", 2);
        var actual = new List<TipeTarifModel>();
        _writer
            .Setup(x => x.Save(It.IsAny<TipeTarifModel>()))
            .Callback<TipeTarifModel>(arg => actual.Add(arg));

        // ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        // ASSERT
        actual.First(x => x.TipeTarifName == "ASKES").NoUrut.Should().Be(1);
        actual.First(x => x.TipeTarifName == "ASURANSI").NoUrut.Should().Be(2);
        actual.First(x => x.TipeTarifName == "CITO").NoUrut.Should().Be(4);
        actual.First(x => x.TipeTarifName == "INSTANSI").NoUrut.Should().Be(2);
        actual.First(x => x.TipeTarifName == "JAMKESMAS").NoUrut.Should().Be(5);
    }
    
    [Fact]
    public void GivenNoUrutDiluarRange_ThenDataShouldBeAtTheEnd()
    {
        // ARRANGE
        var listTipeTarif = Faker();
        _tipeTarifDal.Setup(x => x.ListData()).Returns(listTipeTarif);
        var cmd = new TipeTarifSetNoUrutCommand("D", 9);
        var actual = new List<TipeTarifModel>();
        _writer
            .Setup(x => x.Save(It.IsAny<TipeTarifModel>()))
            .Callback<TipeTarifModel>(arg => actual.Add(arg));

        // ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        // ASSERT
        actual.First(x => x.NoUrut == 1).TipeTarifName.Should().Be("ASKES");
        actual.First(x => x.NoUrut == 2).TipeTarifName.Should().Be("ASURANSI");
        actual.First(x => x.NoUrut == 3).TipeTarifName.Should().Be("CITO");
        actual.First(x => x.NoUrut == 5).TipeTarifName.Should().Be("JAMKESMAS");
        actual.First(x => x.NoUrut == 9).TipeTarifName.Should().Be("INSTANSI");
        
    }
    
}
