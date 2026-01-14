namespace Bilreg.Test.HelperContext.CodingPlayground;

/*
▐▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▌
▐ SCREEN-12 Gudang Farmasi                      ▌
▐ - Left Panel Model (list Unit & Supplier)     ▌
▐▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▌ */
public record UnitType(string UnitId, string UnitName);
public record SupplierUnitType (string UnitId, string UnitName, IEnumerable<PurchaseType> ListPurchase) : UnitType(UnitId, UnitName);
public record PurchaseType(string PurchaseId, string PurchasDate);
public record UnitListResponse(IEnumerable<UnitType> ListUnit);

/*
▐▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▌
▐ - middle Panel Model (inventory items)        ▌
▐▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▌ 

Ada 3 jeni data di list ini:
    1. Request Stok
    2. PO Item
    3. Notifikasi Barang Masuk

1. Request stok:
   Props:
    - Identitas Barang (BrgId, BrgName)
    - UnitSource  (UnitId, UnitName, Qty -availableQty-)
    - UnitDestinaton (UnitId, UnitName, Qty -approvedQty-)
    - RequestQty
   Action
    - Single Item : Mutasi, Order Brg Keluar
    - Batch       : (Repeat Single Item)

2. PO Items:
   Props:
    - Identitas Barang
    - UnitSource (UnitId, UnitName, Qty -purchaseQty)
    - UnitDestination (UnitId, UnitName, Qty -receivedQty-)
    - ExpiredDate
   Action:
    - Single Item : (no action)
    - Batch       : Trs.DU

3. Barang Masuk:
   Props:
    - Identitas Barang
    - UnitSource (UnitId, UnitName, Qty -sentQty-)
    - UnitDestination (UnitId, UnitName, Qty -receivedQty-)
   Action:
    - Single Item: Terima Barang

Request Stok : Brg, [Source], Destination, Qty Req,      [Qty Aprvove]
Purchase Item: Brg, Source,   Destination, Qty Purchase, [Qty Receive]
Brg Masuk    : Brg, Source,   Destination, Qty Kirim,    [Qty Terima]

*/

public record BrgIdentity(string BrgId, string BrgName);

public record StockUnit(string UnitId, string UnitName);

public record StockUnitSelectedState(string UnitId, string UnitName, bool IsSelected);
public record StokUnitPreset(
 StockUnitSelectedState Preset1,
 StockUnitSelectedState Preset2,
 StockUnitSelectedState Preset3,
 StockUnitSelectedState Preset4);
 

public abstract record InventoryItemBase(
 string MutationType,
 int NoUrut,
 BrgIdentity Brg,
 StockUnit Source);

public record ReqStokItem(
 int NoUrut,
 BrgIdentity Brg,
 StockUnit Source,
 int AvailableQty,
 StokUnitPreset  Destination,
 int RequestQty,
 int ApprovedQty) : InventoryItemBase("REQ", NoUrut, Brg, Source);

public record PoItems(
  int NoUrut,
  BrgIdentity Brg,
  StockUnit Source,
  string PurchaseReffNo,
  int PurchaseQty,
  int RemainingQty,
  StockUnit Destination,
  int ReceievedQty) : InventoryItemBase("PO", NoUrut, Brg, Source);

public record BrgMasukItem(
 int NoUrut,
 BrgIdentity Brg,
 StockUnit Source,
 int QtyMasuk) : InventoryItemBase("IN", NoUrut, Brg, Source);

public record InventoryItemResponse(IEnumerable<InventoryItemBase> ListItem);

/*
▐▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▌
▐ - Right Panel Model (History)                 ▌
▐▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▌ */

public record InventoryHistory(
 string Tgl,
 BrgIdentity Brg,
 string MutationType,
 StockUnit Source,
 StockUnit Destination,
 int Qty);
 
 public record InventoryHistoryResponse(IEnumerable<InventoryHistory> ListHistory);
 