namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IFinancialVerificationDomainService
{
    void Verify(
        TataRekeningModel tataRekening,
        string petugasVerif,
        DateTime verifiedAt,
        IEnumerable<MergeRequestModel> pendingMergeRequests);

    void RequireAdjustment(TataRekeningModel tataRekening);

    void ResetVerification(TataRekeningModel tataRekening);

    /// <summary>
    /// Validasi kelengkapan pembebanan sewa kamar (Room Charge) untuk episode rawat inap
    /// dengan interval durasi rawat yang diberikan pemanggil.
    /// </summary>
    RoomChargeCompletenessResult ValidateRoomChargeCompleteness(
        string regId,
        DateTime admissionDate,
        DateTime dischargeDate);

    /// <summary>
    /// Validasi kelengkapan pembebanan sewa kamar (Room Charge) untuk episode rawat inap
    /// dengan interval okupansi diambil dari RNA-BED (PakaiBed).
    /// </summary>
    RoomChargeCompletenessResult ValidateRoomChargeCompleteness(string regId);
}
