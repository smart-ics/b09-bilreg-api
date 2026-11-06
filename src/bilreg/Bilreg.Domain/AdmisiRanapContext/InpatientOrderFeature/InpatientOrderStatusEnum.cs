namespace Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;

public enum InpatientOrderStatusEnum
{
    Requested = 0,   // Dibuat dari EMR atau pengajuan awal
    Maintained = 1,  // Telah dialokasikan bangsal dan kelas rencana
    Fulfilled = 2,   // Telah terealisasi menjadi pendaftaran rawat inap (RegId)
    Cancelled = 3    // Dibatalkan dengan pencatatan alasan wajib
}
