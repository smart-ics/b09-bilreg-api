using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryOrderRepo : IDeliveryOrderRepo
{
    private readonly IDeliveryOrderDal _deliveryOrderDal;
    private readonly IDeliveryOrderItemDal _deliveryOrderItemDal;
    private readonly IDeliveryInspectionDal _deliveryInspectionDal;

    public DeliveryOrderRepo(
        IDeliveryOrderDal deliveryOrderDal,
        IDeliveryOrderItemDal deliveryOrderItemDal,
        IDeliveryInspectionDal deliveryInspectionDal)
    {
        _deliveryOrderDal = deliveryOrderDal;
        _deliveryOrderItemDal = deliveryOrderItemDal;
        _deliveryInspectionDal = deliveryInspectionDal;
    }

    public void SaveChanges(DeliveryOrderModel model)
    {
        var headerDto = MapToHeaderDto(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _deliveryOrderDal.Update(headerDto),
                onNone: () => _deliveryOrderDal.Insert(headerDto)
            );

        var listItemDtos = model.ListItem.Select(item => MapToItemDto(model.DeliveryOrderId, item, model.Status)).ToList();
        _deliveryOrderItemDal.Delete(model);
        _deliveryOrderItemDal.Insert(listItemDtos);

        var listInspectionDtos = model.ListItem.Select(item => MapToInspectionDto(model, item)).ToList();
        _deliveryInspectionDal.Delete(model);
        _deliveryInspectionDal.Insert(listInspectionDtos);
    }

    public MayBe<DeliveryOrderModel> LoadEntity(IDeliveryOrderKey key)
    {
        var orderDto = _deliveryOrderDal.GetData(key);
        if (orderDto is null)
            return MayBe<DeliveryOrderModel>.None;

        var listItemDtos = _deliveryOrderItemDal.ListData(key)?.ToList() ?? [];
        var listInspectionDtos = _deliveryInspectionDal.ListData(key)?.ToList() ?? [];

        var items = new List<DeliveryOrderItemType>();
        foreach (var itemDto in listItemDtos)
        {
            var inspectionDto = listInspectionDtos.FirstOrDefault(x => x.NoUrut == itemDto.NoUrut);
            DeliveryInspectionType inspection;
            if (inspectionDto is not null)
            {
                inspection = new DeliveryInspectionType(
                    inspectionDto.QtyKirim,
                    inspectionDto.QtyDiterima,
                    inspectionDto.QtyDitolak,
                    inspectionDto.AlasanTolak,
                    inspectionDto.IsBonus,
                    inspectionDto.HasJaminanRetur,
                    inspectionDto.NoSuratJaminan,
                    inspectionDto.PoId,
                    inspectionDto.PoNoUrut
                );
            }
            else
            {
                inspection = new DeliveryInspectionType(
                    itemDto.Qty,
                    itemDto.Qty,
                    0m,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    orderDto.PoId,
                    0
                );
            }

            var taxPercentage = itemDto.SubTotal > 0 ? (itemDto.Tax * 100m / itemDto.SubTotal) : 0m;
            var item = new DeliveryOrderItemType(
                itemDto.NoUrut,
                new BrgReff(itemDto.BrgId, itemDto.BrgName),
                new SatuanType(itemDto.SatuanId, itemDto.SatuanName),
                itemDto.Konversi > 0 ? itemDto.Konversi : 1,
                itemDto.Harga,
                itemDto.Diskon,
                itemDto.SubTotal,
                taxPercentage,
                itemDto.Tax,
                itemDto.Total,
                itemDto.NoBatch,
                itemDto.TglEd,
                inspection
            );
            items.Add(item);
        }

        DeliveryOrderStatusEnum status;
        if (orderDto.TglVoid.Year < 3000 && orderDto.TglVoid != default)
        {
            status = DeliveryOrderStatusEnum.Voided;
        }
        else if (listInspectionDtos.Any(x => string.Equals(x.Status, nameof(DeliveryOrderStatusEnum.Confirmed), StringComparison.OrdinalIgnoreCase) || x.ConfirmedAt.HasValue))
        {
            status = DeliveryOrderStatusEnum.Confirmed;
        }
        else if (listInspectionDtos.Any(x => string.Equals(x.Status, nameof(DeliveryOrderStatusEnum.Voided), StringComparison.OrdinalIgnoreCase) || x.VoidedAt.HasValue))
        {
            status = DeliveryOrderStatusEnum.Voided;
        }
        else if (listInspectionDtos.Count == 0)
        {
            status = DeliveryOrderStatusEnum.Confirmed;
        }
        else
        {
            status = DeliveryOrderStatusEnum.Draft;
        }

        AuditInfoType confirmedAudit = AuditInfoType.Default;
        if (status == DeliveryOrderStatusEnum.Confirmed)
        {
            var firstConfirmed = listInspectionDtos.FirstOrDefault(x => x.ConfirmedAt.HasValue);
            if (firstConfirmed != null && firstConfirmed.ConfirmedAt.HasValue)
            {
                confirmedAudit = new AuditInfoType(firstConfirmed.ConfirmedBy, firstConfirmed.ConfirmedAt.Value);
            }
            else
            {
                confirmedAudit = new AuditInfoType(orderDto.PetugasId, orderDto.DoDate);
            }
        }

        var crtDate = orderDto.CrtTgl.Year < 3000 && orderDto.CrtTgl != default ? orderDto.CrtTgl : orderDto.DoDate;
        var createdAudit = new AuditInfoType(orderDto.CrtUsr, crtDate);
        var modifiedAudit = orderDto.UpdTgl.Year < 3000 && orderDto.UpdTgl != default
            ? new AuditInfoType(orderDto.UpdUsr, orderDto.UpdTgl)
            : AuditInfoType.Default;
        var voidedAudit = orderDto.TglVoid.Year < 3000 && orderDto.TglVoid != default
            ? new AuditInfoType(orderDto.PetugasVoid, orderDto.TglVoid)
            : AuditInfoType.Default;

        var auditTrail = new AuditTrailType(createdAudit, AuditInfoType.Default, AuditInfoType.Default);
        if (modifiedAudit != AuditInfoType.Default)
        {
            auditTrail.Modif(modifiedAudit.UserId, modifiedAudit.Timestamp);
        }
        if (voidedAudit != AuditInfoType.Default)
        {
            auditTrail.Batal(voidedAudit.UserId, voidedAudit.Timestamp);
        }

        var subTotal = items.Sum(x => x.SubTotal);
        var taxTotal = items.Sum(x => x.Tax);
        var diskonTotal = items.Sum(x => x.Diskon * x.Inspection.QtyDiterima);
        var total = items.Sum(x => x.Total);
        var grandTotal = orderDto.Total != 0 ? orderDto.Total : total;
        var biayaLain = grandTotal > total ? grandTotal - total : 0m;
        var diskonLain = total > grandTotal ? total - grandTotal : 0m;

        var supplier = new PartnerReff(orderDto.SupplierId, orderDto.SupplierName);
        var gudang = new LayananReff(orderDto.LayananId, orderDto.LayananName);

        var model = new DeliveryOrderModel(
            orderDto.DoId,
            orderDto.SuratJalanNo,
            orderDto.PoId,
            orderDto.DoDate,
            supplier,
            gudang,
            orderDto.Keterangan,
            status,
            subTotal,
            taxTotal,
            diskonTotal,
            total,
            biayaLain,
            diskonLain,
            grandTotal,
            confirmedAudit,
            auditTrail,
            items,
            orderDto.PoDate
        );

        return MayBe.From(model);
    }

    public void DeleteEntity(IDeliveryOrderKey key)
    {
        _deliveryOrderItemDal.Delete(key);
        _deliveryInspectionDal.Delete(key);
        _deliveryOrderDal.Delete(key);
    }

    public MayBe<IEnumerable<DeliveryOrderView>> ListData(Periode filter)
    {
        var listDto = _deliveryOrderDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x =>
        {
            DeliveryOrderStatusEnum status;
            if (x.TglVoid.Year < 3000 && x.TglVoid != default)
                status = DeliveryOrderStatusEnum.Voided;
            else if (Enum.TryParse<DeliveryOrderStatusEnum>(x.Status, true, out var parsedStatus))
                status = parsedStatus;
            else
                status = DeliveryOrderStatusEnum.Confirmed;

            return new DeliveryOrderView(
                x.DoId,
                x.SuratJalanNo,
                x.PoId,
                x.DoDate,
                new PartnerReff(x.SupplierId, x.SupplierName),
                new LayananReff(x.LayananId, x.LayananName),
                x.Keterangan,
                status,
                x.Total
            );
        });

        return MayBe.From(result);
    }

    private static DeliveryOrderDto MapToHeaderDto(DeliveryOrderModel model)
    {
        var isVoided = model.Status == DeliveryOrderStatusEnum.Voided;
        var voidDate = isVoided && model.AuditTrail.Voided.Timestamp != default
            ? model.AuditTrail.Voided.Timestamp
            : new DateTime(3000, 1, 1);
        var voidTime = isVoided && model.AuditTrail.Voided.Timestamp != default
            ? model.AuditTrail.Voided.Timestamp.ToString("HH:mm:ss")
            : "00:00:00";
        var voidUser = isVoided ? model.AuditTrail.Voided.UserId : string.Empty;

        var isModified = model.AuditTrail.Modified != null &&
                         model.AuditTrail.Modified.Timestamp != default &&
                         model.AuditTrail.Modified.Timestamp.Year < 3000;
        var updDate = isModified ? model.AuditTrail.Modified!.Timestamp : new DateTime(3000, 1, 1);
        var updUser = isModified ? model.AuditTrail.Modified!.UserId : string.Empty;

        return new DeliveryOrderDto
        {
            DoId = model.DeliveryOrderId,
            DoDate = model.DeliveryOrderDate,
            PetugasId = model.AuditTrail.Created.UserId,
            PoId = model.PoId ?? string.Empty,
            SuratJalanNo = model.SuratJalanNo ?? string.Empty,
            LayananId = model.Gudang.LayananId,
            LayananName = model.Gudang.LayananName,
            SupplierId = model.Supplier.PartnerId,
            SupplierName = model.Supplier.PartnerName,
            Total = model.GrandTotal,
            Keterangan = model.Keterangan ?? string.Empty,
            PembelianBebas = string.IsNullOrWhiteSpace(model.PoId),
            TglVoid = voidDate,
            JamVoid = voidTime,
            PetugasVoid = voidUser,
            CrtTgl = model.AuditTrail.Created.Timestamp != default ? model.AuditTrail.Created.Timestamp : DateTime.Now,
            CrtUsr = model.AuditTrail.Created.UserId,
            UpdTgl = updDate,
            UpdUsr = updUser
        };
    }

    private static DeliveryOrderItemDto MapToItemDto(string doId, DeliveryOrderItemType item, DeliveryOrderStatusEnum status)
    {
        return new DeliveryOrderItemDto
        {
            DoId = doId,
            NoUrut = item.NoUrut,
            BrgId = item.Brg.BrgId,
            BrgName = item.Brg.BrgName,
            Harga = item.Harga,
            Diskon = item.Diskon,
            Qty = item.Inspection.QtyDiterima,
            SatuanId = item.Satuan.SatuanId,
            SatuanName = item.Satuan.SatuanName,
            SubTotal = item.SubTotal,
            Tax = item.Tax,
            Total = item.Total,
            NoBatch = item.NoBatch,
            TglEd = item.TglEd,
            IsVoid = status == DeliveryOrderStatusEnum.Voided,
            Konversi = item.Konversi,
            Het = 0m,
            Biaya = 0m
        };
    }

    private static DeliveryInspectionDto MapToInspectionDto(DeliveryOrderModel model, DeliveryOrderItemType item)
    {
        var isConfirmed = model.Status == DeliveryOrderStatusEnum.Confirmed;
        var isVoided = model.Status == DeliveryOrderStatusEnum.Voided;

        DateTime? confirmedAt = isConfirmed && model.Confirmed.Timestamp != default && model.Confirmed.Timestamp.Year < 3000
            ? model.Confirmed.Timestamp
            : null;
        var confirmedBy = isConfirmed ? model.Confirmed.UserId : string.Empty;

        DateTime? voidedAt = isVoided && model.AuditTrail.Voided.Timestamp != default && model.AuditTrail.Voided.Timestamp.Year < 3000
            ? model.AuditTrail.Voided.Timestamp
            : null;
        var voidedBy = isVoided ? model.AuditTrail.Voided.UserId : string.Empty;

        var isModified = model.AuditTrail.Modified != null &&
                         model.AuditTrail.Modified.Timestamp != default &&
                         model.AuditTrail.Modified.Timestamp.Year < 3000;
        var updatedAt = isModified ? model.AuditTrail.Modified!.Timestamp : (model.AuditTrail.Created.Timestamp != default ? model.AuditTrail.Created.Timestamp : DateTime.Now);
        var updatedBy = isModified ? model.AuditTrail.Modified!.UserId : (model.AuditTrail.Created.UserId ?? string.Empty);

        return new DeliveryInspectionDto
        {
            DoId = model.DeliveryOrderId,
            NoUrut = item.NoUrut,
            BrgId = item.Brg.BrgId,
            BrgName = item.Brg.BrgName,
            PoId = item.Inspection.PoId,
            PoNoUrut = item.Inspection.PoNoUrut,
            QtyKirim = item.Inspection.QtyKirim,
            QtyDiterima = item.Inspection.QtyDiterima,
            QtyDitolak = item.Inspection.QtyDitolak,
            AlasanTolak = item.Inspection.AlasanTolak,
            IsBonus = item.Inspection.IsBonus,
            HasJaminanRetur = item.Inspection.HasJaminanRetur,
            NoSuratJaminan = item.Inspection.NoSuratJaminan,
            Status = model.Status.ToString(),
            ConfirmedAt = confirmedAt,
            ConfirmedBy = confirmedBy,
            VoidedAt = voidedAt,
            VoidedBy = voidedBy,
            CreatedAt = model.AuditTrail.Created.Timestamp != default ? model.AuditTrail.Created.Timestamp : DateTime.Now,
            CreatedBy = model.AuditTrail.Created.UserId ?? string.Empty,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };
    }
}
