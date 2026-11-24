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
    SaleVoidDt = 13,        // DT_V
    DispenseIssue = 14,     // Pharmacy handover removal from DTU (PD-02); not SaleIssueDu
    InternalConsumptionVoid = 15, // PK_V

    // Legacy VB6 clbGenStokX1.cls comprehensive mappings
    AdjustmentPlus = 16,        // AJ_PLUS
    AdjustmentPlusVoid = 17,    // AJ_PLUS_V
    AdjustmentMinus = 18,       // AJ_MIN
    AdjustmentMinusVoid = 19,   // AJ_MIN_V
    GoodsReceiptVoid = 20,      // DO_V
    TransferOutVoid = 21,       // MT_OUT_V
    TransferInVoid = 22,        // MT_IN_V
    SalesReturnRjVoid = 23,     // RJ_V
    SalesReturnRuVoid = 24,     // RU_V
    SalesReturnRtVoid = 25,     // RT_V
    RepackSource = 26,          // RP_ASAL
    RepackSourceVoid = 27,      // RP_ASAL_V
    RepackOutput = 28,          // RP_HASIL
    RepackOutputVoid = 29,      // RP_HASIL_V
    DisposalIssue = 30,         // MN
    DisposalIssueVoid = 31,     // MN_V
    PurchaseReturn = 32,        // RB
    PurchaseReturnVoid = 33,    // RB_V
    SatelliteIssue = 34,        // DU-S
    SatelliteIssueVoid = 35,    // DU-S_V
    SatelliteReserveIn = 36,    // DU-R_IN
    SatelliteReserveInVoid = 37,// DU-R_IN_V
    SatelliteReserveOut = 38,   // DU-R_OUT
    SatelliteReserveOutVoid = 39// DU-R_OUT_V
}
