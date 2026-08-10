namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// S1 MovementKind INT catalog (GAP-STL-003 interim). Map legacy strings in ACL only.
/// </summary>
public enum MovementKindEnum
{
    GoodsReceipt = 1,       // DO
    TransferOut = 2,        // MT_OUT
    TransferIn = 3,         // MT_IN
    SaleIssueDb = 4,        // DB
    SaleIssueDu = 5,        // DU
    SaleIssueDt = 6,        // DT
    InternalConsumption = 7,// PK
    SalesReturnRj = 8,      // RJ
    SalesReturnRu = 9,      // RU
    SalesReturnRt = 10,     // RT
    SaleVoidDb = 11,        // DB_V
    SaleVoidDu = 12,        // DU_V
    SaleVoidDt = 13         // DT_V
}
