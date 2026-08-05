using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.SalesContext.AntrianFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.SalesContext.TelaahFeature;

public class TelaahModel : ITelaahKey
{
    #region CREATION
    private TelaahModel(string telaahId, DateTime telaahDate,
        string resepId, RegReff reg, LayananReff layanan, PpaReff dokter,
        string indikasi, string alergi, AuditTrailType auditTrail)
    {
        TelaahId = telaahId;
        TelaahDate = telaahDate;
        ResepId = resepId;
        Reg = reg;
        Layanan = layanan;
        Dokter = dokter;

        Administratif = new AdministratifReview();
        Farmasetik = new FarmasetikReview();
        Clinical = new ClinicalReview(indikasi, alergi);
        AuditTrail = auditTrail;
    }

    public static TelaahModel Create(string resepId, RegType reg, 
        LayananType layanan, PpaReff dokter,
        string diagnosa, string alergi, string userId)
    {
        var newId = Ulid.NewUlid().ToString();
        var audit = AuditTrailType.Create(userId, DateTime.Now);

        return new TelaahModel(newId, DateTime.Now, resepId, 
            reg.ToReff(), layanan.ToReff(), dokter, diagnosa, alergi, audit);
    }

    public static ITelaahKey Key(string id)
    {
        return new TelaahModel(id, new DateTime(3000, 1, 1), "-",
            RegType.Default.ToReff(), LayananType.Default.ToReff(), new PpaReff("-", "-"),
            "-", "-", AuditTrailType.Default);
    }

    public static TelaahModel Default => new("-", new DateTime(3000, 1, 1), "-", 
        RegType.Default.ToReff(), LayananType.Default.ToReff(), new PpaReff("-","-"),
        "-", "-", AuditTrailType.Default);
    #endregion

    #region PROPERTIES
    public string TelaahId { get; init; }
    public DateTime TelaahDate { get; init; }

    public string ResepId { get; private set; }
    public RegReff Reg { get; private set; }
    
    public LayananReff Layanan { get; private set; }
    public PpaReff Dokter { get; private set; }

    public AdministratifReview Administratif { get; private set; }
    public FarmasetikReview Farmasetik { get; private set; }
    public ClinicalReview Clinical { get; private set; }

    public AuditTrailType AuditTrail { get; private set; }

    public bool HasIssue => Administratif.HasIssue || Clinical.HasIssue || Farmasetik.HasIssue;
    #endregion

    #region BEHAVIOR
    public void SetAdministratifAppropriate(AdministratifCheckEnum check,
        bool isAppropriate, string userId)
    {
        Administratif.SetAppropriate(check, isAppropriate);
        SetModif(userId);
    }

    public void SetClinicalAppropriate(ClinicalCheckEnum check,
        bool isAppropriate, string userId)
    {
        Clinical.SetAppropriate(check, isAppropriate);
        SetModif(userId);
    }
    public void UpdateClinicalData(string diagnosa, string alergi,
        string userId)
    {
        Clinical.UpdateDiagnosa(diagnosa);
        Clinical.UpdateAlergi(alergi);
        SetModif(userId);
    }

    public void AddMedication(BrgReff brg)
    {
        Farmasetik.AddMedication(brg);
    }

    public void SetMedicationDoseAppropriate(BrgReff brg, 
        bool compliant, string userId)
    {
        Farmasetik.SetDoseAppropriate(brg, compliant);
        SetModif(userId);
    }

    public void SetMedicationEtiketAppropriate(BrgReff brg, 
        bool compliant, string userId)
    {
        Farmasetik.SetEtiketAppropriate(brg, compliant);
        SetModif(userId);
    }
    #endregion

    private void SetModif(string userId)
    {
        AuditTrail.Modif(userId, DateTime.Now);
    }
}

public interface ITelaahKey
{
    string TelaahId { get; }
}
