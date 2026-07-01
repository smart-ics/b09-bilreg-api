using Farinv.Domain.Shared.Helpers;

namespace Farinv.Domain.InventoryContext.StokFeature;

public record StokBukuType : IStokBukuKey
{
    public StokBukuType(string stokBukuId, int noUrut,  
        int qtyIn, int qtyOut, TrsReffType trsReff, string useCase,
        DateTime entryDate, ModelStateEnum modelState)
    {
        StokBukuId = stokBukuId;
        NoUrut = noUrut;

        QtyIn = qtyIn;
        QtyOut = qtyOut;

        TrsReff = trsReff;
        UseCase = useCase;
        EntryDate = entryDate;
        ModelState = modelState;
    }
    
    public string StokBukuId { get; init;}
    public int NoUrut { get; init;}
    public string UseCase { get; init;}
    public int QtyIn { get; init;}
    public int QtyOut { get; init;}
    public TrsReffType TrsReff { get; init;}
    public DateTime EntryDate { get; init;}
    public ModelStateEnum ModelState { get; init; }

    public static StokBukuType Default => new StokBukuType("-", 0, 0, 
        0, TrsReffType.Default, "-", new DateTime(3000,1,1), ModelStateEnum.Unchange);

    public static StokBukuType Masuk(int noUrut, int qty, 
        TrsReffType trsReff, string useCase)
    {
        var newStokBukuId = Ulid.NewUlid().ToString();
        var result = new StokBukuType(newStokBukuId, noUrut, qty, 
            0, trsReff, useCase, DateTime.Now, ModelStateEnum.Added);
        return result;
    }

    public static StokBukuType Keluar(int noUrut, int qty, 
        TrsReffType trsReff, string useCase)
    {
        var newStokBukuId = Ulid.NewUlid().ToString();
        var result = new StokBukuType(newStokBukuId, noUrut, 0, 
            qty, trsReff, useCase, DateTime.Now, ModelStateEnum.Added);
        return result;
    }
};

public interface IStokBukuKey
{
    string StokBukuId { get;  }
}
