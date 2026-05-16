using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public class OrderLabModel: IOrderLabKey
    {
        private readonly List<TarifReff> _listTarif;
        public OrderLabModel(string orderId, string reffId, 
            DateTime orderDate, OrderStatusEnum orderStatus, 
            PasienReff pasien, string clinicalNote, PegType requestingDoctor, 
            LayananReff originLayanan, List<TarifReff> items)
        {
            OrderId = orderId;
            ReffId = reffId;
            OrderDate = orderDate;
            OrderStatus = orderStatus;
            Pasien = pasien;
            ClinicalNote = clinicalNote;
            RequestingDoctor = requestingDoctor;
            OriginLayanan = originLayanan;
            _listTarif = items;
        }
        public static IOrderLabKey Key(string id)
            => new OrderLabModel(
                        id, "", DateTime.Parse("3000-01-01"),
                        OrderStatusEnum.TestOrdered,
                        new PasienReff("", "", DateOnly.Parse("3000-01-01"), ""),
                        "", PegType.Default, new LayananReff("", ""),
                        []);
        public static OrderLabModel Default 
            => new(
                orderId: "",
                reffId: "",
                orderDate: DateTime.Parse("3000-01-01"),
                orderStatus: OrderStatusEnum.TestOrdered,
                pasien: new PasienReff("", "", DateOnly.Parse("3000-01-01"), ""),
                clinicalNote: "",
                requestingDoctor: PegType.Default,
                originLayanan: new LayananReff("", ""),
                items: []
            );
        #region PROPERTIES
        public string OrderId { get; private set; } 
        public string ReffId { get; private set; } 
        public DateTime OrderDate { get; private set; }
        public OrderStatusEnum OrderStatus { get; private set; }
        public PasienReff Pasien { get; private set; }

        public string ClinicalNote { get; private set; }
        
        //perujuk
        public PegType RequestingDoctor { get; private set; }
        
        //layanan asal
        public LayananReff OriginLayanan { get; private set; }

        // List Tarif
        public List<TarifReff> Items => _listTarif;
        #endregion

        #region BEHAVIOR
        public void AddTarif(TarifReff tarif)
        {
            Guard.Against.Null(tarif, nameof(tarif));
            _listTarif.Add(tarif);
        }
        public void SetStatus(OrderStatusEnum newStatus)
        {
            Guard.Against.Null(newStatus, nameof(newStatus));
            OrderStatus = newStatus;
        }
        #endregion
    }
    public enum OrderStatusEnum
    {
        TestOrdered,
        TestCharged,
        SpecimenCollected,
        ResultRecorded,
        ResultVerified
    }
}
