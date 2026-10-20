namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Port abstraction for validating whether an insurance/guarantor type (paymentId)
/// is legitimate and registered on the patient visit registration in Admission domain.
/// </summary>
public interface IRegJaminanPolicy
{
    /// <summary>
    /// Checks whether the specified payment/guarantor ID is valid and registered for the given registration ID.
    /// </summary>
    /// <param name="regId">The registration ID.</param>
    /// <param name="paymentId">The payment/guarantor ID.</param>
    /// <returns>True if the registration is found, active, and matches the payment ID; otherwise false.</returns>
    bool IsJaminanValidForReg(string regId, string paymentId);
}
