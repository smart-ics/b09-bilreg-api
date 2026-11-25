namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class PreOpChecklistIncompleteException : InvalidOperationException
{
    public PreOpChecklistIncompleteException()
        : base("Pre-Operative Clearance tidak dapat disetujui karena checklist belum lengkap.")
    {
    }

    public PreOpChecklistIncompleteException(string message)
        : base(message)
    {
    }
}
