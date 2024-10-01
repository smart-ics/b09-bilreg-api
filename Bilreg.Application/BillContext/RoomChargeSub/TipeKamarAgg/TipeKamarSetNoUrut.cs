using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarSetNoUrut(string TipeKamarId, int noUrut):IRequest, ITipeKamarKey ;

public class TipeKamarSetNoUrutHandler : IRequestHandler<TipeKamarSetNoUrut>
{
    private readonly ITipeKamarDal _tipeKamarDal;
    private readonly ITipeKamarWriter _writer;

    public TipeKamarSetNoUrutHandler(ITipeKamarDal tipeKamarDal, ITipeKamarWriter writer)
    {
        _tipeKamarDal = tipeKamarDal;
        _writer = writer;
    }

    public Task Handle(TipeKamarSetNoUrut request, CancellationToken cancellationToken)
    {
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TipeKamarId);

        var tipeKamarList = _tipeKamarDal.ListData()
        ?? throw new KeyNotFoundException("Tipe Kamar not Found");
        
        var list = tipeKamarList.ToList().OrderBy(x => x.NoUrut).ToList();
        var result = ReOrder(list,request.TipeKamarId,request.noUrut);

        list.ForEach(x => _writer.Save(x));
        return Task.CompletedTask;
    }
    
    private IEnumerable<TipeKamarModel> ReOrder(List<TipeKamarModel> list, string id, int target)
    {
        var targetItem = list.FirstOrDefault(x => x.NoUrut == target);
        return targetItem is null ? 
            ReplaceNoUrut(list, id, target) : 
            ReOrderNoUrut(list, id, target);
    }
    
    private IEnumerable<TipeKamarModel> ReplaceNoUrut(List<TipeKamarModel> list, string id, int noUrut)
    {
        var originItem = list.First(x => x.TipeKamarId == id);
        originItem.SetNoUrut(noUrut);
        return list;
    }
    
    private IEnumerable<TipeKamarModel> ReOrderNoUrut(List<TipeKamarModel> list, string id, int target)
    {
        var indexedRetak = list
            .OrderBy(x => x.NoUrut)
            .ThenBy(x => x.TipeKamarName)
            .Select((retak, index) => new IndexRetak
            {
                Index = index + 1,
                Retak = retak
            })
            .ToList();

        var originItem = indexedRetak.First(x => x.Retak.TipeKamarId == id);
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
    
    private class IndexRetak
    {
        public int Index { get; set; }
        public TipeKamarModel Retak { get; set; }
    }
}

