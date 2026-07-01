// using Farinv.Domain.InventoryContext.StokFeature;
// using Farinv.Domain.SalesContext.AntrianFeature;
// using Farinv.Domain.SalesContext.ResepFeature;
// using Farinv.Domain.Shared.Helpers.CommonValueObjects;
// ﻿using Farinv.Domain.SalesContext.AntrianFeature;
//
// namespace Farinv.Domain.SalesContext.PenjualanFeature;
//
// public class PenjualanModel : IPenjualanKey
// {
//     private readonly List<PenjualanItemType> _listItem;
//     
//     public PenjualanModel(string penjualanId, RegReff reg, DokterReff dokter, LayananReff layanan, decimal subtotal,
//         decimal totalEmbalase, decimal totalTax, decimal totalDiskon, decimal diskonLain, decimal biayaLain,
//         decimal grandTotal, AuditTrailType auditTrail, IEnumerable<PenjualanItemType> listItem)
//     {
//         PenjualanId = penjualanId;
//         Register = reg;
//         Dokter = dokter;
//         Layanan = layanan;
//         SubTotal = subtotal;
//         TotalEmbalase = totalEmbalase;
//         TotalTax = totalTax;
//         TotalDiskon = totalDiskon;
//         DiskonLain = diskonLain;
//         BiayaLain = biayaLain;
//         GrandTotal = grandTotal;
//         AuditTrail = auditTrail;
//         _listItem = listItem.ToList();
//     }
//
//     public static PenjualanModel Key(string id) => new(id, RegType.Default.ToReff(), DokterType.Default.ToReff(),
//         LayananType.Default.ToReff(), 0, 0, 0, 0, 0, 0, 0,
//         AuditTrailType.Default, new List<PenjualanItemType>());
//
//     public string PenjualanId { get; private set; }
//     public RegReff Register { get; private set; }
//     public DokterReff Dokter { get; private set; }
//     public LayananReff Layanan { get; private set; }
//     public decimal SubTotal { get; private set; }
//     public decimal TotalEmbalase { get; private set; }
//     public decimal TotalTax { get; private set; }
//     public decimal TotalDiskon { get; private set; }
//     public decimal DiskonLain { get; private set; }
//     public decimal BiayaLain { get; private set; }
//     public decimal GrandTotal { get; private set; }
//     public AuditTrailType AuditTrail { get; private set; }
//     public IEnumerable<PenjualanItemType> ListItem => _listItem;
// }
//     #region CREATE
//     public PenjualanModel(string penjualanId, DateTime penjualanDate,
//         RegReff reg)
//     {
//         PenjualanId = penjualanId;
//         PenjualanDate = penjualanDate;
//         Reg = reg;
//     }
//
//     public static PenjualanModel Default => new("-", 
//         new DateTime(3000, 1, 1), RegType.Default.ToReff());
//     #endregion
//
//     #region PROPERTIES
//     public string PenjualanId { get; init; }
//     public DateTime PenjualanDate { get; init; }
//
//     public RegReff Reg { get; private set; }
//     #endregion
//
//     #region BEHAVIOUR
//     public PenjualanReff ToReff() => new(PenjualanId, PenjualanDate, Reg);
//     #endregion
// }
//
// public interface IPenjualanKey
// {
//     string PenjualanId { get; }
// }
//
// public record PenjualanReff(string PenjualanId, DateTime PenjualanDate, RegReff Reg);
