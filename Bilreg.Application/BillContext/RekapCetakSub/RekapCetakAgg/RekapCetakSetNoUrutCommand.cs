using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;

public record RekapCetakSetNoUrutCommand(string RekapCetakId, int NoUrut) : IRequest, IRekapCetakKey;

public class RekapCetakSetNoUrutHandler : IRequestHandler<RekapCetakSetNoUrutCommand>
{
    private readonly IRekapCetakDal _rekapCetakDal;
    private readonly IRekapCetakWriter _writer;

    public RekapCetakSetNoUrutHandler(IRekapCetakDal rekapCetakDal, IRekapCetakWriter writer)
    {
        _rekapCetakDal = rekapCetakDal;
        _writer = writer;
    }
    
    public Task Handle(RekapCetakSetNoUrutCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.RekapCetakId);
        
        // BUILD
        var rekapCetakList = _rekapCetakDal.ListData()
            ?? throw new KeyNotFoundException("Rekap Cetak not found");

        var list = rekapCetakList.ToList().OrderBy(x => x.NoUrut).ToList();
        var result = ReOrderJude(list, request.RekapCetakId, request.NoUrut);

        
        // WRITE
       list.ForEach(x => _writer.Save(x));
        return Task.CompletedTask;
    }

    private IEnumerable<RekapCetakModel> ReOrderAdinath(List<RekapCetakModel> list, string id, int target)
    {
        var rekapCetak = list.First(x => x.RekapCetakId == id);
        var noUrutAwal = rekapCetak.NoUrut;

        rekapCetak.SetNoUrut(target);
        var isAscending = noUrutAwal > target;
        var startIndex = target - 1;
        var endIndex = noUrutAwal - 1;
        var step = isAscending ? 1 : -1;

        for (var i = startIndex; i != endIndex; i += step)
        {
            var index = (i + list.Count) % list.Count;

            var newNoUrut = list[index].NoUrut + (isAscending ? 1 : -1);
            list[index].SetNoUrut(newNoUrut);
        }

        return list;
    }

    private IEnumerable<RekapCetakModel> ReOrderJude(List<RekapCetakModel> list, string id, int target)
    {
        var targetItem = list.FirstOrDefault(x => x.NoUrut == target);
        return targetItem is null ? 
            JustReplaceNoUrut(list, id, target) : 
            ReOrderNoUrut(list, id, target);
    }

    private IEnumerable<RekapCetakModel> ReOrderNoUrut(List<RekapCetakModel> list, string id, int target)
    {
        var indexedRetak = list
            .OrderBy(x => x.NoUrut)
            .ThenBy(x => x.RekapCetakName)
            .Select((retak, index) => new IndexRetak
            {
                Index = index + 1,
                Retak = retak
            })
            .ToList();

        var originItem = indexedRetak.First(x => x.Retak.RekapCetakId == id);
        var targetItem = indexedRetak.First(x => x.Retak.NoUrut == target);
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

        var result = indexedRetak.Select(x => x.Retak);
        return result;

        void Swap(int originIndex, int targetIndex)
        {
            var originRetak = indexedRetak.First(x => x.Index == originIndex);
            var targetRetak = indexedRetak.First(x => x.Index == targetIndex);
            (originRetak.Retak, targetRetak.Retak) = (targetRetak.Retak, originRetak.Retak);
            
            var temp = targetRetak.Retak.NoUrut;
            targetRetak.Retak.SetNoUrut(originRetak.Retak.NoUrut);
            originRetak.Retak.SetNoUrut(temp);
        }
    }

    private IEnumerable<RekapCetakModel> JustReplaceNoUrut(List<RekapCetakModel> list, string id, int noUrut)
    {
        var originItem = list.First(x => x.RekapCetakId == id);
        originItem.SetNoUrut(noUrut);
        return list;
    }

    private class IndexRetak
    {
        public int Index { get; set; }
        public RekapCetakModel Retak { get; set; }
    }
}


public class RekapCetakSetNoUrutHandlerTest
{
    private readonly Mock<IRekapCetakDal> _rekapCetakDal;
    private readonly Mock<IRekapCetakWriter> _writer;
    private readonly RekapCetakSetNoUrutHandler _sut;

    public RekapCetakSetNoUrutHandlerTest()
    {
        _rekapCetakDal = new Mock<IRekapCetakDal>();
        _writer = new Mock<IRekapCetakWriter>();
        _sut = new RekapCetakSetNoUrutHandler(_rekapCetakDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThrowsArgumentNullException_Test()
    {
        RekapCetakSetNoUrutCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRekapCetakId_ThrowsArgumentException_Test()
    {
        var request = new RekapCetakSetNoUrutCommand(" ", 1 );
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    private static List<RekapCetakModel> Faker()
    {
        var retak1 = new RekapCetakModel("A", "Agus");retak1.SetNoUrut(1);
        var retak2 = new RekapCetakModel("B", "Budi");retak2.SetNoUrut(2);
        var retak3 = new RekapCetakModel("C", "Caca");retak3.SetNoUrut(3);
        var retak4 = new RekapCetakModel("D", "Dedy");retak4.SetNoUrut(4);
        var retak5 = new RekapCetakModel("E", "Etik");retak5.SetNoUrut(5);
        var listRetak = new List<RekapCetakModel>
        {
            retak1, retak2, retak3, retak4, retak5
        };
        return listRetak;
    }
    
    [Fact]
    public void GivenNoUrutMengecil_ThenOtherRecordReOrdered()
    {
        //  ARRANGE
        var listRetak = Faker();
        _rekapCetakDal.Setup(x => x.ListData()).Returns(listRetak);
        var cmd = new RekapCetakSetNoUrutCommand("D", 2);
        var actual = new List<RekapCetakModel>(); 
        _writer
            .Setup(x => x.Save(It.IsAny<RekapCetakModel>()))
            .Callback<RekapCetakModel>(arg => actual.Add(arg));

        //  ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        //  ASSERT
        actual.First(x => x.NoUrut == 1).RekapCetakName.Should().Be("Agus");
        actual.First(x => x.NoUrut == 2).RekapCetakName.Should().Be("Dedy");
        actual.First(x => x.NoUrut == 3).RekapCetakName.Should().Be("Budi");
        actual.First(x => x.NoUrut == 4).RekapCetakName.Should().Be("Caca");
        actual.First(x => x.NoUrut == 5).RekapCetakName.Should().Be("Etik");
    }
    
    [Fact]
    public void GivenNoUrutMembesar_ThenOtherRecordReOrdered()
    {
        //  ARRANGE
        var listRetak = Faker();
        _rekapCetakDal.Setup(x => x.ListData()).Returns(listRetak);
        var cmd = new RekapCetakSetNoUrutCommand("B", 4);
        var actual = new List<RekapCetakModel>(); 
        _writer
            .Setup(x => x.Save(It.IsAny<RekapCetakModel>()))
            .Callback<RekapCetakModel>(arg => actual.Add(arg));

        //  ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        //  ASSERT
        actual.First(x => x.NoUrut == 1).RekapCetakName.Should().Be("Agus");
        actual.First(x => x.NoUrut == 2).RekapCetakName.Should().Be("Caca");
        actual.First(x => x.NoUrut == 3).RekapCetakName.Should().Be("Dedy");
        actual.First(x => x.NoUrut == 4).RekapCetakName.Should().Be("Budi");
        actual.First(x => x.NoUrut == 5).RekapCetakName.Should().Be("Etik");
    }
    
    [Fact]
    public void GivenExistingDataBolong_ThenOtherRecordReOrdered()
    {
        //  ARRANGE
        var listRetak = Faker();
        listRetak.RemoveAll(x => x.RekapCetakName == "Caca");
        _rekapCetakDal.Setup(x => x.ListData()).Returns(listRetak);
        var cmd = new RekapCetakSetNoUrutCommand("D", 2);
        var actual = new List<RekapCetakModel>(); 
        _writer
            .Setup(x => x.Save(It.IsAny<RekapCetakModel>()))
            .Callback<RekapCetakModel>(arg => actual.Add(arg));

        //  ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        //  ASSERT
        actual.First(x => x.NoUrut == 1).RekapCetakName.Should().Be("Agus");
        actual.First(x => x.NoUrut == 2).RekapCetakName.Should().Be("Dedy");
        actual.FirstOrDefault(x => x.NoUrut == 3).Should().BeNull();
        actual.First(x => x.NoUrut == 4).RekapCetakName.Should().Be("Budi");
        actual.First(x => x.NoUrut == 5).RekapCetakName.Should().Be("Etik");
    }

    [Fact]
    public void GivenExistingDataDuplicated_ThenOtherRecordReOrdered()
    {
        //  ARRANGE
        var listRetak = Faker();
        listRetak.First(x => x.RekapCetakName == "Caca").SetNoUrut(2);
        _rekapCetakDal.Setup(x => x.ListData()).Returns(listRetak);
        var cmd = new RekapCetakSetNoUrutCommand("D", 2);
        var actual = new List<RekapCetakModel>(); 
        _writer
            .Setup(x => x.Save(It.IsAny<RekapCetakModel>()))
            .Callback<RekapCetakModel>(arg => actual.Add(arg));

        //  ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        //  ASSERT
        actual.First(x => x.RekapCetakName == "Agus").NoUrut.Should().Be(1);
        actual.First(x => x.RekapCetakName == "Budi").NoUrut.Should().Be(2);
        actual.First(x => x.RekapCetakName == "Caca").NoUrut.Should().Be(4);
        actual.First(x => x.RekapCetakName == "Dedy").NoUrut.Should().Be(2);
        actual.First(x => x.RekapCetakName == "Etik").NoUrut.Should().Be(5);
    }

    
    [Fact]
    public void GivenNoUrutDiluarRange_ThenDataShouldBeAtTheEnd()
    {
        //  ARRANGE
        var listRetak = Faker();
        _rekapCetakDal.Setup(x => x.ListData()).Returns(listRetak);
        var cmd = new RekapCetakSetNoUrutCommand("D", 9);
        var actual = new List<RekapCetakModel>(); 
        _writer
            .Setup(x => x.Save(It.IsAny<RekapCetakModel>()))
            .Callback<RekapCetakModel>(arg => actual.Add(arg));

        //  ACT
        _sut.Handle(cmd, CancellationToken.None);
        
        //  ASSERT
        actual.First(x => x.NoUrut == 1).RekapCetakName.Should().Be("Agus");
        actual.First(x => x.NoUrut == 2).RekapCetakName.Should().Be("Budi");
        actual.First(x => x.NoUrut == 3).RekapCetakName.Should().Be("Caca");
        actual.First(x => x.NoUrut == 5).RekapCetakName.Should().Be("Etik");
        actual.First(x => x.NoUrut == 9).RekapCetakName.Should().Be("Dedy");
    }}    
