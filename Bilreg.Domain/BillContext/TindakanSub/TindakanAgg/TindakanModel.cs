//using Bilreg.Domain.AdmisiContext.LayananFeature;
//using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
//using Bilreg.Domain.AdmisiContext.RegFeature;
//using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;
//using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
//using Bilreg.Domain.Helpers.CommonValueObjects;

//namespace Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;

//public class TindakanModel : ITindakanKey
//{
//    private readonly List<TindakanTarifModel> _listTarif;
  
//    #region  CREATION
//    public TindakanModel(string tindakanId, DateTime timeTindakan, AuditInfoType auditTindakan, 
//        RegReff reg, LayananReff layanan, string orderId, PetugasMedisReff dokterPengirim,
//        TipeTarifModel tipeTarif, IEnumerable<TindakanTarifModel> listTarif)
//    {
//        TindakanId = tindakanId;
//        TimeTindakan = timeTindakan;
//        AuditTindakan = auditTindakan;
//        Register = reg;
//        Layanan = layanan;
//        OrderId = orderId;
//        DokterPengirim = dokterPengirim;
//        TipeTarif = tipeTarif;
//        _listTarif = listTarif.ToList();
//    }

//    public static TindakanModel Default => new TindakanModel("-", new DateTime(3000, 1, 1),
//        AuditInfoType.Default, RegModel.Default.ToReff(), LayananType.Default.ToReff(), "-",
//        PetugasMedisType.Default.ToReff(), new TipeTarifModel("-", "-"), []);


//    public static ITindakanKey Key(string id) => new TindakanModel("id", new DateTime(3000, 1, 1),
//        AuditInfoType.Default, RegModel.Default.ToReff(), LayananType.Default.ToReff(), "-",
//        PetugasMedisType.Default.ToReff(), new TipeTarifModel("-", "-"), []);

//    #endregion

//    #region PROPERTIES
//    public string TindakanId { get; init; }
//    public DateTime TimeTindakan { get; init; }
//    public AuditInfoType AuditTindakan { get; init; }
//    public RegReff Register {  get; init; }
//    public LayananReff Layanan { get; init; }
//    public string OrderId { get; init; }
//    public PetugasMedisReff DokterPengirim { get; init; }
//    public TipeTarifModel TipeTarif { get; init; }
//    public long Total { get; private set; }
//    public IEnumerable<TindakanTarifModel> ListTarif => _listTarif;
//    #endregion

//    #region BEHAVIOUR

//    #endregion
//}

//public interface ITindakanKey
//{
//    string TindakanId { get; }
//}

//public class TindakanTarifModel
//{
//    private readonly List<TindakanTarifDetilModel> _listDtlTarif;

//    public TindakanTarifModel(string tindakanId, 
//        TarifReff tarif, long subTotal, 
//        IEnumerable<TindakanTarifDetilModel> listDtlTarif)
//    {
//        TindakanId = tindakanId;
//        Tarif = tarif;
//        SubTotal = subTotal;
//        _listDtlTarif = listDtlTarif.ToList();
//    }

//    public string TindakanId { get; init; }
//    public TarifReff Tarif {  get; init; }
//    public long SubTotal{ get; init; }
//    public IEnumerable<TindakanTarifDetilModel> ListDetilTarif => _listDtlTarif;
//}

//public class TindakanTarifDetilModel
//{
//    public TindakanTarifDetilModel(string tindakanId, TarifReff tarif, string detilTarifId, 
//        string detilTarifName, PetugasMedisReff petugasMedis, long nilai)
//    {
//        TindakanId = tindakanId;
//        Tarif = tarif;
//        DetilTarifId = detilTarifId;
//        DetilTarifName = detilTarifName;
//        PetugasMedis = petugasMedis;
//        Nilai = nilai;
//    }

//    public string TindakanId { get; init; }
//    public TarifReff Tarif { get; init; }
//    public string DetilTarifId { get; init; }
//    public string DetilTarifName { get; init; }
//    public PetugasMedisReff PetugasMedis { get; init; }
//    public long Nilai { get; init; }
//}