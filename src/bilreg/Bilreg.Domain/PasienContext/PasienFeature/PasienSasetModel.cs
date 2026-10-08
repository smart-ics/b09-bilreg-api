namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienSasetModel
{
    public string PasienId { get; private set; }
    public string SasetId { get; private set; }
    public bool IsApprovedUpload { get; private set; }
    public DateTime TglJamApprovedUpload { get; private set; }
    public string FileGeneralConcentUpload { get; private set; }
    public bool IsApprovedView { get; private set; }
    public DateTime TglJamApprovedView { get; private set; }
    public string FileGeneralConcentView { get; private set; }

    public PasienSasetModel(string pasienId, string sasetId, 
        bool isApprovedUpload, DateTime tglJamApprovedUpload, string fileGeneralConcentUpload, 
        bool isApprovedView, DateTime tglJamApprovedView, string fileGeneralConcentView)
    {
        PasienId = pasienId;
        SasetId = sasetId;
        IsApprovedUpload = isApprovedUpload;
        TglJamApprovedUpload = tglJamApprovedUpload;
        FileGeneralConcentUpload = fileGeneralConcentUpload;
        IsApprovedView = isApprovedView;
        TglJamApprovedView = tglJamApprovedView;
        FileGeneralConcentView = fileGeneralConcentView;
    }

    public static PasienSasetModel Default(string pasienId = "-") =>
        new PasienSasetModel(pasienId, "-", false, new DateTime(3000, 1, 1), "-", false, new DateTime(3000, 1, 1), "-");

    public void ApproveUpload(DateTime approvedAt)
    {
        IsApprovedUpload = true;
        TglJamApprovedUpload = approvedAt;
    }
}
