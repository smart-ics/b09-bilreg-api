// using Bilreg.Domain.BrgContext.BrgFeature;
// using Bilreg.Domain.InventoryContext.StokFeature;
// using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
//
// namespace Bilreg.Domain.InventoryContext.RequestStokFeature;
//
// public class RequestStokModel : IRequestStokKey
// {
//     #region CREATION
//     public RequestStokModel(string requestStokId, BrgReff brg, LayananReff layanan,
//         int requestQty, int receivedQty, SatuanReff satuan, 
//         GroupRekDkType groupRekDk, AuditTrailType auditTrail)
//     {
//         RequestStokId = requestStokId;
//         Brg = brg;
//         Layanan = layanan;
//         RequestQty = requestQty;
//         ReceivedQty = receivedQty;
//         Satuan = satuan;
//         GroupRekDk = groupRekDk;
//         AuditTrail = auditTrail;
//     }
//
//     public static RequestStokModel Create(IBrg brg, LayananType layanan, 
//         int requestQty, SatuanType satuan, string userId)
//     {
//         var newId = Ulid.NewUlid().ToString();
//         var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
//
//         return new RequestStokModel(newId, brg.ToReff(), layanan.ToReff(), 
//             requestQty, 0, satuan.ToReff(), brg.GroupRekDk, auditTrail);
//     }
//
//     public static IRequestStokKey Key(string id)
//     {
//         return new RequestStokModel(id, new BrgReff("-", "-"), LayananType.Default.ToReff(),
//             0, 0, SatuanType.Default.ToReff(), GroupRekDkType.Default, AuditTrailType.Default);
//     }
//
//     public static RequestStokModel Default => new("-", new BrgReff("-", "-"), 
//         LayananType.Default.ToReff(), 0, 0, SatuanType.Default.ToReff(), 
//         GroupRekDkType.Default, AuditTrailType.Default);
//     #endregion
//
//     #region PROPERTIES
//     public string RequestStokId { get; private set; }
//
//     public BrgReff Brg { get; private set; }
//     public LayananReff Layanan { get; private set; }
//     public int RequestQty { get; private set; }
//     public int ReceivedQty { get; private set; }
//     public int OutstandingQty => RequestQty - ReceivedQty;
//
//     public SatuanReff Satuan { get; private set; }
//     public GroupRekDkType GroupRekDk { get; private set; }
//
//     public DateTime LastRequestDate { get; private set; }
//     public AuditTrailType AuditTrail { get; private set; }
//     #endregion
//
//     #region BEHAVIOR
//     public void AddRequest(int qty)
//     {
//         if (qty <= 0)
//             throw new ArgumentException("Qty harus lebih besar dari 0");
//
//         RequestQty += qty;
//         LastRequestDate = DateTime.Now;
//     }
//
//     public void Receive(int qty, string userId)
//     {
//         if (qty <= 0)
//             throw new ArgumentException("Qty harus lebih besar dari 0");
//
//         ReceivedQty += qty;
//         if(ReceivedQty == RequestQty) 
//
//         AuditTrail.Modif(userId, DateTime.Now);
//     }
//
//     public void Close()
//     {
//         RequestQty = 0;
//         ReceivedQty = 0;
//     }
//     #endregion
// }
//
// public interface IRequestStokKey
// {
//     string RequestStokId { get; }
// }